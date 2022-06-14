namespace Retweety.Configuration
{
    using System.Text.Json.Serialization;

    public class BotConfig
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("iconUrl")]
        public string IconUrl { get; set; }
    }
}