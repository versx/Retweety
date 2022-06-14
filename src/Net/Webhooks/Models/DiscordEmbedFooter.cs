namespace Retweety.Net.Webhooks.Models
{
    using System;
    using System.Text.Json.Serialization;

    public class DiscordEmbedFooter
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("icon_url")]
        public string IconUrl { get; set; }

        public DiscordEmbedFooter()
        {
        }

        public DiscordEmbedFooter(string text, string iconUrl = null)
        {
            Text = text;
            IconUrl = iconUrl;
        }
    }
}