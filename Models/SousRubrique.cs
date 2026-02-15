using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FacturationMercuriale.Models
{
    public sealed class SousRubrique
    {
        [JsonPropertyName("srId")]
        public string SrId { get; set; } = "";

        [JsonPropertyName("numSousRubrique")]
        public int NumSousRubrique { get; set; }

        [JsonPropertyName("designationFr")]
        public string DesignationFr { get; set; } = "";

        [JsonPropertyName("nbArticles")]
        public int NbArticles { get; set; }

        [JsonPropertyName("articles")]
        public List<Article> Articles { get; set; } = new();

        [JsonIgnore]
        public string Display => $"{NumSousRubrique:D3} - {DesignationFr} ({NbArticles})";
    }
}
