using System.Text.Json.Serialization;

namespace FormBuilderAPI.DTOs
{
    public class AccessPatchDto
    {
        [JsonPropertyName("access")]
        public string Access { get; set; } = "Open";   // "Open" | "Restricted"
    }

    public class StatusPatchDto
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = "Draft";  // "Draft" | "Published"
    }
}