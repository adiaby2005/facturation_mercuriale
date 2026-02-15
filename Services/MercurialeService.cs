using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FacturationMercuriale.Models;

namespace FacturationMercuriale.Services
{
    public sealed class MercurialeService
    {
        private readonly string _jsonPath;
        private List<Rubrique>? _cache;

        // ✅ Compat v1 : propriété attendue "Rubriques"
        private static readonly IReadOnlyList<Rubrique> EmptyRubriques = new List<Rubrique>();

        public IReadOnlyList<Rubrique> Rubriques => _cache ?? EmptyRubriques;


        // ✅ Compat : new MercurialeService() -> Data/mercuriale_complete.json
        public MercurialeService()
        {
            _jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "mercuriale_complete.json");
        }

        // ✅ Compat : new MercurialeService("Data/mercuriale_complete.json")
        public MercurialeService(string jsonPath)
        {
            _jsonPath = jsonPath;
        }

        // ✅ Compat v1 : méthode attendue Load()
        public void Load()
        {
            _ = LoadRubriques();
        }

        // ✅ Méthode actuelle
        public List<Rubrique> LoadRubriques()
        {
            if (_cache != null) return _cache;

            var fullPath = _jsonPath;

            if (!Path.IsPathRooted(fullPath))
                fullPath = Path.Combine(AppContext.BaseDirectory, fullPath);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Fichier mercuriale introuvable : {fullPath}");

            var json = File.ReadAllText(fullPath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var root = JsonSerializer.Deserialize<MercurialeRoot>(json, options)
                       ?? new MercurialeRoot();

            _cache = root.Rubriques ?? new List<Rubrique>();
            return _cache;
        }

        // ✅ Search avec paramètre nommé "limit"
        public List<Article> Search(string query, int limit = 200)
        {
            query ??= "";
            var q = query.Trim();
            if (q.Length < 2) return new List<Article>();

            var rubs = LoadRubriques();

            var all = rubs
                .SelectMany(r => r.SousRubriques ?? new List<SousRubrique>())
                .SelectMany(sr => sr.Articles ?? new List<Article>())
                .ToList();

            var qq = q.ToLowerInvariant();

            bool Match(Article a)
            {
                var refA = (a.RefArticle ?? "").ToLowerInvariant();
                var code = (a.Code ?? "").ToLowerInvariant();
                var des = (a.DesignationFr ?? "").ToLowerInvariant();
                return refA.Contains(qq) || code.Contains(qq) || des.Contains(qq);
            }

            return all.Where(Match).Take(limit).ToList();
        }

        private sealed class MercurialeRoot
        {
            public List<Rubrique>? Rubriques { get; set; }
        }
    }
}
