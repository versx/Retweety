namespace Retweety.Configuration
{
    using System;
    using System.IO;
    using System.Text.Json.Serialization;

    using Retweety.Extensions;

    public class Config
    {
        #region Properties

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("consumerKey")]
        public string ConsumerKey { get; set; }

        [JsonPropertyName("consumerSecret")]
        public string ConsumerSecret { get; set; }

        [JsonPropertyName("accessToken")]
        public string AccessToken { get; set; }

        [JsonPropertyName("accessTokenSecret")]
        public string AccessTokenSecret { get; set; }

        [JsonPropertyName("accounts")]
        public RetweetConfig TwitterAccounts { get; set; } = new();

        [JsonPropertyName("bot")]
        public BotConfig Bot { get; set; } = new();

        #endregion

        #region Public Methods

        /// <summary>
        /// Save the current configuration object
        /// </summary>
        /// <param name="filePath">Path to save the configuration file</param>
        public void Save(string filePath)
        {
            var data = this.ToJson();
            File.WriteAllText(filePath, data);
        }

        /// <summary>
        /// Load the configuration from a file
        /// </summary>
        /// <param name="filePath">Path to load the configuration file from</param>
        /// <returns>Returns the deserialized configuration object</returns>
        public static Config Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Config not loaded because file not found.", filePath);
            }
            var config = filePath.LoadFromFile<Config>();
            return config;
        }

        #endregion
    }
}