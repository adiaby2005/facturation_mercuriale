using System.Collections.Generic;

namespace FacturationMercuriale.Models
{
    public class PricingSettings
    {
        // La clé est la région, la valeur est un dictionnaire Ville -> Coefficient
        // "AUTRES LOCALITES" est utilisé comme clé par défaut pour une région.
        public Dictionary<string, Dictionary<string, double>> Data { get; set; } = new();
    }
}