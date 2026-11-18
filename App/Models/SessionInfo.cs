using System.Text.Json.Serialization;

namespace Saffron.Models;

// Shape written by the care login into Redis. Property names are the shared contract with the session store.
public sealed class SessionInfo
{
    [JsonPropertyName("tenant")] public string Tenant { get; set; } = "";
    [JsonPropertyName("user_id")] public int UserId { get; set; }
    [JsonPropertyName("username")] public string Username { get; set; } = "";
    [JsonPropertyName("display_name")] public string DisplayName { get; set; } = "";
    [JsonPropertyName("role")] public string Role { get; set; } = "";
    [JsonPropertyName("created")] public long Created { get; set; }
    [JsonPropertyName("last_seen")] public long LastSeen { get; set; }

    [JsonIgnore] public string Id { get; set; } = "";
}
