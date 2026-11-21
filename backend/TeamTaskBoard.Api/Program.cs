using Microsoft.EntityFrameworkCore;
using TeamTaskBoard.Api.Authentication;
using TeamTaskBoard.Api.Data;
using TeamTaskBoard.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddCors(options => options.AddPolicy("LocalFrontend", policy => policy
    .WithOrigins("http://127.0.0.1:5173")
    .WithMethods("GET", "POST", "PUT", "DELETE")
    .WithHeaders("Authorization", "Content-Type")));
builder.Services.AddFirebaseAuthentication(builder.Configuration);
builder.Services.AddScoped<LocalUserService>();
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddDbContext<TeamTaskBoardContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("TeamTaskBoard");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        options.UseMySQL();
        return;
    }

    options.UseMySQL(connectionString);
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors("LocalFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
