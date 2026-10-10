using System.Text.Json;
using System.Text.Json.Serialization;

namespace resume_generator.Models;

public sealed class Resume
{
    [JsonPropertyName("$schema")]
    public string? Schema { get; set; }

    [JsonPropertyName("basics")]
    public Basics? Basics { get; set; }

    [JsonPropertyName("work")]
    public List<WorkExperience>? Work { get; set; }

    [JsonPropertyName("volunteer")]
    public List<VolunteerExperience>? Volunteer { get; set; }

    [JsonPropertyName("education")]
    public List<Education>? Education { get; set; }

    [JsonPropertyName("awards")]
    public List<Award>? Awards { get; set; }

    [JsonPropertyName("certificates")]
    public List<Certificate>? Certificates { get; set; }

    [JsonPropertyName("publications")]
    public List<Publication>? Publications { get; set; }

    [JsonPropertyName("skills")]
    public List<Skill>? Skills { get; set; }

    [JsonPropertyName("languages")]
    public List<SpokenLanguage>? Languages { get; set; }

    [JsonPropertyName("interests")]
    public List<Interest>? Interests { get; set; }

    [JsonPropertyName("references")]
    public List<Reference>? References { get; set; }

    [JsonPropertyName("projects")]
    public List<Project>? Projects { get; set; }

    [JsonPropertyName("meta")]
    public ResumeMeta? Meta { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}
