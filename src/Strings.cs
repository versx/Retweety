namespace Retweety
{
    using System.IO;

    public static class Strings
    {
        public const string BotName = "Retweety";

        public const string BotVersion = "0.1.0";

        public const string BotIconUrl = "";

        public const string ConfigFileName = "config.json";

        public const string LogsFolderName = "logs";

        public static readonly string LogsFolder = Path.Combine(
            Directory.GetCurrentDirectory(),
            LogsFolderName
        );

        public const uint ValidationIntervalM = 5;

        public const string DefaultEmbedTemplate = "{{url}}";
    }
}