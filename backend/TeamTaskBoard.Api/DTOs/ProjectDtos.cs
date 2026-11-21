using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TeamTaskBoard.Api.DTOs;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CreateProjectRequest : IValidatableObject
{
    public string? Name { get; init; }
    public string? Description { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult("Enter a project name.", [nameof(Name)]);
        }
        else if (Name.Trim().Length > 200)
        {
            yield return new ValidationResult("Use 200 characters or fewer for the name.", [nameof(Name)]);
        }

        if (Description?.Trim().Length > 2000)
        {
            yield return new ValidationResult("Use 2,000 characters or fewer for the description.", [nameof(Description)]);
        }
    }
}

public sealed record ProjectDto(long Id, string Name, string? Description, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
