namespace Retweety.Net.Webhooks.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    using Retweety.Extensions;

    public class DiscordWebhookMessage
    {
        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("avatar_url")]
        public string AvatarUrl { get; set; }

        [JsonPropertyName("embeds")]
        public List<DiscordEmbedMessage> Embeds { get; set; }

        public DiscordWebhookMessage()
        {
            Embeds = new List<DiscordEmbedMessage>();
        }

        public string Build()
        {
            return this.ToJson();
        }
    }
}