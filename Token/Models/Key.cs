using System.Text.Json.Serialization;

namespace API.Models
{
    public class Key
    {
        [JsonPropertyName("Key")]
        public string? Value { get; set; }
    }
}
