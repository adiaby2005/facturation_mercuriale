using System.Text.Json.Serialization;

namespace FacturationMercuriale.Models
{
    public sealed class Article
    {
        [JsonPropertyName("refArticle")]
        public string RefArticle { get; set; } = "";

        [JsonPropertyName("code")]
        public string Code { get; set; } = "";

        [JsonPropertyName("designationFr")]
        public string DesignationFr { get; set; } = "";

        [JsonPropertyName("prix")]
        public decimal Prix { get; set; }

        [JsonPropertyName("conditionnement")]
        public string Conditionnement { get; set; } = "";

        [JsonPropertyName("millesime")]
        public string Millesime { get; set; } = "";

        [JsonIgnore]
        public string Display => $"{RefArticle} - {ShortDesignation}";

        [JsonIgnore]
        public string ShortDesignation
        {
            get
            {
                var s = (DesignationFr ?? "").Replace("\r", "").Replace("\n", " ").Trim();
                if (s.Length <= 80) return s;
                return s[..77] + "...";
            }
        }
    }
}
