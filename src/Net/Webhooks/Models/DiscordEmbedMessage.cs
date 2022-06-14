namespace Retweety.Net.Webhooks.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class DiscordEmbedMessage
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("color")]
        public int Color { get; set; }

        [JsonPropertyName("fields")]
        public List<DiscordEmbedField> Fields { get; set; } = new();

        [JsonPropertyName("footer")]
        public DiscordEmbedFooter Footer { get; set; } = new();
    }
}