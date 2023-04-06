using Newtonsoft.Json;
using System;
using Version = Hive.Versioning.Version;

namespace PBOT.Models
{
    internal class DeltaMetadata
    {
        private static readonly Version UndefinedVerison = new(0, 0, 0);

        [JsonProperty("version")]
        public Version Version { get; set; } = UndefinedVerison;

        [JsonProperty("source")]
        public string Source { get; set; } = string.Empty;

        [JsonProperty("timestamp")]
        public DateTimeOffset Timestamp { get; set; }

        [JsonProperty("totalScore")]
        public long TotalScore { get; set; }
    }
}