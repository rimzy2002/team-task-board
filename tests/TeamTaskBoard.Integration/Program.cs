using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;
using TeamTaskBoard.Api.Data;

const string project = "demo-team-task-board-tests";
const string alice = "fictional-seed-owner-a";
const string bob = "fictional-seed-owner-b";
const string reportPath = "/api/reports/overdue-by-user";
var root = Directory.GetCurrentDirectory();
var apiRoot = Path.Combine(root, "backend", "TeamTaskBoard.Api");
if (!File.Exists(Path.Combine(root, "database", "schema.sql")))
    throw new InvalidOperationException("Run the integration tests from the repository root.");
var connection = new MySqlConnectionStringBuilder(Environment.GetEnvironmentVariable("TEAMBOARD_TEST_MYSQL") ?? "");
if (connection.Server != "127.0.0.1" || connection.Database.Length != 0)
    throw new InvalidOperationException("Set TEAMBOARD_TEST_MYSQL to a loopback MySQL test server with no Database value.");
var schema = "teamboardtest" + Guid.NewGuid().ToString("N");
await using var admin = new MySqlConnection(connection.ConnectionString);
await admin.OpenAsync();
await Execute(admin, $"CREATE DATABASE `{schema}` CHARACTER SET utf8mb4");
try
{
    connection.Database = schema;
    connection.AllowUserVariables = true;
    await using var db = new MySqlConnection(connection.ConnectionString);
    await db.OpenAsync();
    await Execute(db, await File.ReadAllTextAsync(Path.Combine(root, "database", "schema.sql")));
    var seed = await File.ReadAllTextAsync(Path.Combine(root, "database", "seed.sql"));
    for (var attempt = 0; attempt < 2; attempt++)
    {
        await Execute(db, seed);
        Check(2L, await Count(db, "SELECT COUNT(*) FROM users"), "Seed users");
        Check(3L, await Count(db, "SELECT COUNT(*) FROM projects JOIN users ON users.id=projects.owner_id"), "Seed projects");
        Check(9L, await Count(db, "SELECT COUNT(*) FROM tasks JOIN projects ON projects.id=tasks.project_id"), "Seed tasks");
        Check(0L, await Count(db, "SELECT COUNT(*) FROM users WHERE email IS NOT NULL"), "Fictional identities only");
    }
    await RejectSql(1062, "INSERT INTO users (firebase_uid,created_at_utc) VALUES ('fictional-seed-owner-a',UTC_TIMESTAMP(6))");
    await Execute(db, "INSERT INTO users (firebase_uid,created_at_utc) VALUES ('Fictional-seed-owner-a',UTC_TIMESTAMP(6))");
    await RejectSql(1452, "INSERT INTO projects (owner_id,name,created_at_utc,updated_at_utc) VALUES (-1,'Bad owner',UTC_TIMESTAMP(6),UTC_TIMESTAMP(6))");
    await RejectSql(3819, "UPDATE tasks SET status='todo' WHERE id=(SELECT MIN(id) FROM (SELECT id FROM tasks) AS ids)");
    await RejectSql(3819, "UPDATE tasks SET priority='Urgent' WHERE id=(SELECT MIN(id) FROM (SELECT id FROM tasks) AS ids)");
    Check(1L, await Count(db, "SELECT FIND_IN_SET('ONLY_FULL_GROUP_BY',@@sql_mode)>0"), "Strict grouping");
    Console.WriteLine("PASS: schema, seed twice, foreign key, UID uniqueness/case, status and priority constraints.");

    async Task RejectSql(int number, string sql)
    {
        try { await Execute(db, sql); }
        catch (MySqlException exception) when (exception.Number == number) { return; }
        throw new InvalidOperationException($"Expected MySQL constraint error {number}.");
    }

    using var rsa = RSA.Create(2048);
    var certRequest = new CertificateRequest("CN=Local test JWT fixture", rsa,
        HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    using var certificate = certRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddHours(1));
    const string kid = "local-fixture-key";
    var keyFeed = JsonSerializer.Serialize(new Dictionary<string, string>
    {
        [kid] = certificate.ExportCertificatePem(),
    });
    var signingKey = new RsaSecurityKey(rsa) { KeyId = kid };
    string Token(string uid, string audience = project)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return new JsonWebTokenHandler().CreateToken(JsonSerializer.Serialize(new
        {
            iss = "https://securetoken.google.com/" + project, aud = audience, sub = uid,
            iat = now - 30, auth_time = now - 60, exp = now + 300,
        }), new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256));
    }

    Environment.SetEnvironmentVariable("ASPNETCORE_TEST_CONTENTROOT_TEAMTASKBOARD_API", apiRoot);
    Environment.SetEnvironmentVariable("Firebase__ProjectId", project);
    Environment.SetEnvironmentVariable("ConnectionStrings__TeamTaskBoard", connection.ConnectionString);
    using var bootstrap = new WebApplicationFactory<TeamTaskBoardContext>();
    using var host = bootstrap.WithWebHostBuilder(builder =>
    {
        builder.UseEnvironment("Production");
        builder.ConfigureServices(services =>
        {
            services.AddHttpClient("FirebaseSigningKeys").ConfigurePrimaryHttpMessageHandler(() => new PublicKeyFeed(keyFeed));
            services.AddLogging(logging => logging.ClearProviders());
        });
    });
    using var client = host.CreateClient();
    using (var scope = host.Services.CreateScope())
    {
        var model = scope.ServiceProvider.GetRequiredService<TeamTaskBoardContext>();
        Check(false, model.Database.HasPendingModelChanges(), "EF model matches migration");
        Check(0, (await model.Database.GetPendingMigrationsAsync()).Count(), "Schema export records initial migration");
    }

    var tokenA = Token(alice);
    var tokenB = Token(bob);
    foreach (var invalid in new[] { null, "not-a-token", Token(alice, "another-project") })
        await Call(HttpMethod.Get, reportPath, 401, invalid);
    var projectsA = await Call(HttpMethod.Get, "/api/projects", 200, tokenA);
    var projectsB = await Call(HttpMethod.Get, "/api/projects", 200, tokenB);
    Check(2, projectsA.GetArrayLength(), "Owner A projects");
    Check(1, projectsB.GetArrayLength(), "Owner B projects");
    var projectA = projectsA.EnumerateArray().Single(p => p.GetProperty("name").GetString() == "Campus open day").GetProperty("id").GetInt64();
    var projectB = projectsB[0].GetProperty("id").GetInt64();
    var tasksA = $"/api/projects/{projectA}/tasks";
    var tasksB = $"/api/projects/{projectB}/tasks";
    await Call(HttpMethod.Get, tasksA, 404, tokenB);
    await Call(HttpMethod.Get, tasksB, 404, tokenA);
    await Call(HttpMethod.Post, tasksA, 400, tokenA, new { title = " " });
    await Call(HttpMethod.Post, tasksA, 400, tokenA, new { title = "Old due date", dueDate = "2000-01-01" });
    await Call(HttpMethod.Post, tasksA, 400, tokenA, new { title = "Owner injection", ownerId = -1 });
    await Call(HttpMethod.Get, tasksA + "?page=0", 400, tokenA);
    var filters = await Call(HttpMethod.Get, tasksA + "?status=ToDo&priority=High&search=handout&page=1", 200, tokenA);
    Check("Draft visitor handout", filters.GetProperty("items")[0].GetProperty("title").GetString(), "Combined filters");
    for (var i = 0; i < 11; i++)
        await Call(HttpMethod.Post, tasksA, 201, tokenA, new { title = $"Pagination fixture {i}" });
    var page = await Call(HttpMethod.Get, tasksA + "?search=Pagination&page=2", 200, tokenA);
    Check(11, page.GetProperty("totalCount").GetInt32(), "Pagination count");
    Check(1, page.GetProperty("items").GetArrayLength(), "Pagination remainder");
    Check("Pagination fixture 0", page.GetProperty("items")[0].GetProperty("title").GetString(), "Stable ordering");

    await VerifyReport(tokenA, "Ada:2", "[unassigned]:1");
    await VerifyReport(tokenB, "Riley:1");
    await VerifyReport(Token("fictional-empty-test-owner"));
    var overdue = filters.GetProperty("items")[0];
    var taskPath = $"/api/tasks/{overdue.GetProperty("id").GetInt64()}";
    await Call(HttpMethod.Delete, taskPath, 404, tokenB);
    await Call(HttpMethod.Put, taskPath, 200, tokenA, new
    {
        title = "Draft visitor handout", status = "Done", priority = "High",
        dueDate = overdue.GetProperty("dueDate").GetString(), assignee = "Ada",
    });
    await VerifyReport(tokenA, "Ada:1", "[unassigned]:1");
    var other = await Call(HttpMethod.Get, tasksA + "?search=room%20capacity", 200, tokenA);
    await Call(HttpMethod.Delete, $"/api/tasks/{other.GetProperty("items")[0].GetProperty("id").GetInt64()}", 204, tokenA);
    await VerifyReport(tokenA, "Ada:1");
    await VerifyReport(tokenB, "Riley:1");
    Console.WriteLine("PASS: real JWT middleware, owner isolation both ways, task validation/filtering/pagination, overdue-date updates and report grouping.");

    var example = await File.ReadAllTextAsync(Path.Combine(root, "api-tests", "team-task-board.http"));
    var examples = Regex.Matches(example, @"(?m)^### .+?· (\d+)\r?\n# @name (\w+)\r?\n(GET|POST|PUT|DELETE) ([^\r\n]+)");
    Check(9, examples.Count, "HTTP file request coverage");
    var sampleIds = new Dictionary<string, long>();
    foreach (Match entry in examples)
    {
        var name = entry.Groups[2].Value;
        var target = entry.Groups[4].Value.Replace("{{baseUrl}}", "");
        foreach (var (variable, id) in sampleIds) target = target.Replace("{{" + variable + "}}", id.ToString());
        var remainder = example[entry.Index..].Split("\n### ", 2)[0];
        var json = Regex.Match(remainder, @"(?ms)^\{.*?^\}");
        var reply = await Call(new HttpMethod(entry.Groups[3].Value), target, int.Parse(entry.Groups[1].Value),
            name == "health" ? null : tokenA, rawJson: json.Success ? json.Value : null);
        if (name is "createProject" or "createTask")
            sampleIds[name == "createProject" ? "projectId" : "taskId"] = reply.GetProperty("id").GetInt64();
    }
    Console.WriteLine("PASS: all nine .http examples executed against the API.");

    async Task<JsonElement> Call(HttpMethod method, string path, int status, string? token, object? body = null, string? rawJson = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body);
        if (rawJson is not null) request.Content = new StringContent(rawJson, Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(request);
        Check(status, (int)response.StatusCode, $"{method} {path}");
        return response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NoContent
            ? await response.Content.ReadFromJsonAsync<JsonElement>() : default;
    }

    async Task VerifyReport(string token, params string[] expected)
    {
        var rows = await Call(HttpMethod.Get, reportPath, 200, token);
        var groups = rows.EnumerateArray().Select(row =>
        {
            Check(2, row.EnumerateObject().Count(), "Safe report DTO shape");
            return (row.GetProperty("assigneeName").GetString() ?? "[unassigned]") + ":" +
                row.GetProperty("overdueTaskCount").GetInt64();
        });
        Check(string.Join('|', expected), string.Join('|', groups), "Owner-scoped report groups");
    }
}
finally
{
    // This identifier was generated above, and CREATE DATABASE succeeded before entering the test.
    MySqlConnection.ClearAllPools();
    await Execute(admin, $"DROP DATABASE `{schema}`");
}

static async Task Execute(MySqlConnection db, string sql)
{
    await using var cmd = db.CreateCommand();
    cmd.CommandText = sql;
    await cmd.ExecuteNonQueryAsync();
}

static async Task<long> Count(MySqlConnection db, string sql)
{
    await using var cmd = db.CreateCommand();
    cmd.CommandText = sql;
    return Convert.ToInt64(await cmd.ExecuteScalarAsync());
}

static void Check<T>(T expected, T actual, string label)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{label}: expected {expected}, got {actual}.");
}

sealed class PublicKeyFeed(string keys) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(keys, Encoding.UTF8, "application/json"),
            Headers = { CacheControl = new CacheControlHeaderValue { MaxAge = TimeSpan.FromMinutes(5) } },
        });
}
