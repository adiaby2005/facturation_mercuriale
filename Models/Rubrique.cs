using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FacturationMercuriale.Models
{
    public sealed class Rubrique
    {
        [JsonPropertyName("ruId")]
        public string RuId { get; set; } = "";

        [JsonPropertyName("numRubrique")]
        public int NumRubrique { get; set; }

        [JsonPropertyName("designationFr")]
        public string DesignationFr { get; set; } = "";

        [JsonPropertyName("sousRubriques")]
        public List<SousRubrique> SousRubriques { get; set; } = new();

        [JsonIgnore]
        public string Display => $"{NumRubrique:D2} - {DesignationFr}";
    }
}