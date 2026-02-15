using System.Collections.Generic;
using System.Linq;

namespace FacturationMercuriale.Services
{
    public sealed class CameroonLocationsService
    {
        // Liste initiale (non exhaustive) — on pourra la remplacer par un JSON complet ensuite.
        private readonly Dictionary<string, List<string>> _data = new()
        {
            ["Adamaoua"] = new() { "Ngaoundéré", "Meiganga", "Tibati", "Tignère", "Banyo" },
            ["Centre"] = new() { "Yaoundé", "Obala", "Mfou", "Mbalmayo", "Bafia", "Akonolinga" },
            ["Est"] = new() { "Bertoua", "Abong-Mbang", "Batouri", "Yokadouma", "Garoua-Boulaï" },
            ["Extrême-Nord"] = new() { "Maroua", "Kousséri", "Mora", "Kaélé", "Yagoua", "Mokolo" },
            ["Littoral"] = new() { "Douala", "Edéa", "Loum", "Nkongsamba", "Dizangué", "Manjo" },
            ["Nord"] = new() { "Garoua", "Guider", "Pitoa", "Figuil", "Poli" },
            ["Nord-Ouest"] = new() { "Bamenda", "Kumbo", "Wum", "Ndop", "Fundong" },
            ["Ouest"] = new() { "Bafoussam", "Dschang", "Foumban", "Mbouda", "Bandjoun", "Bangangté" },
            ["Sud"] = new() { "Ebolowa", "Kribi", "Sangmélima", "Ambam", "Campo" },
            ["Sud-Ouest"] = new() { "Buea", "Limbe", "Kumba", "Tiko", "Mamfe", "Idenau" },
        };

        public List<string> GetRegions()
            => _data.Keys.OrderBy(x => x).ToList();

        public List<string> GetCities(string? region)
        {
            if (string.IsNullOrWhiteSpace(region)) return new List<string>();
            return _data.TryGetValue(region, out var cities)
                ? cities.OrderBy(x => x).ToList()
                : new List<string>();
        }
    }
}
