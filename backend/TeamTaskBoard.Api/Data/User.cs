namespace TeamTaskBoard.Api.Data;

public sealed class User
{
    public long Id { get; set; }
    public required string FirebaseUid { get; set; }
    public string? Email { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
