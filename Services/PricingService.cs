using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FacturationMercuriale.Models;

namespace FacturationMercuriale.Services
{
    public class PricingService
    {
        private readonly string _configPath;
        private PricingSettings _settings;

        public PricingService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folder = Path.Combine(appData, "FacturationMercuriale");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            _configPath = Path.Combine(folder, "pricing.json");

            // Forcer la mise à jour au démarrage avec les versions "Usine"
            _settings = CreateDefaultSettings();
            SaveSettings(_settings);
        }

        public PricingSettings GetSettings() => _settings;

        public void SaveSettings(PricingSettings settings)
        {
            _settings = settings;
            string json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configPath, json);
        }

        public double GetMaxAllowedPrice(string? region, string? city, double basePrice)
        {
            if (string.IsNullOrEmpty(region)) return basePrice;

            double coeff = 1.0;
            string regUpper = region.ToUpper().Trim();

            if (_settings.Data.TryGetValue(regUpper, out var localites))
            {
                bool found = false;

                // Chercher d'abord une correspondance de ville exacte ou partielle
                if (!string.IsNullOrEmpty(city))
                {
                    string cityUpper = city.ToUpper().Trim();

                    // Parcourir toutes les clés de localités pour cette région
                    foreach (var key in localites.Keys)
                    {
                        // La clé peut contenir plusieurs villes séparées par des virgules
                        var villes = key.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var v in villes)
                        {
                            if (v.Trim().Equals(cityUpper, StringComparison.OrdinalIgnoreCase) ||
                                cityUpper.Contains(v.Trim()) || v.Trim().Contains(cityUpper))
                            {
                                coeff = localites[key];
                                found = true;
                                break;
                            }
                        }
                        if (found) break;
                    }
                }

                // Si aucune ville trouvée, utiliser le coefficient par défaut de la région
                if (!found && localites.TryGetValue("AUTRES LOCALITES", out double regionDefault))
                {
                    coeff = regionDefault;
                }
            }

            return basePrice * coeff;
        }

        private PricingSettings CreateDefaultSettings()
        {
            var s = new PricingSettings();
            // Valeurs mises à jour
            s.Data.Add("CENTRE", new Dictionary<string, double> { { "AUTRES LOCALITES", 1.04 } });
            s.Data.Add("LITTORAL", new Dictionary<string, double> { { "AUTRES LOCALITES", 1.04 } });
            s.Data.Add("ADAMAOUA", new Dictionary<string, double> { { "NGAOUNDERE", 1.04 }, { "AUTRES LOCALITES", 1.2 } });
            s.Data.Add("EST", new Dictionary<string, double> { { "BERTOUA, BELABO", 1.05 }, { "AUTRES LOCALITES", 1.1 } });
            s.Data.Add("EXTREME-NORD", new Dictionary<string, double> { { "MAROUA", 1.1 }, { "AUTRES LOCALITES", 1.2 } });
            s.Data.Add("NORD", new Dictionary<string, double> { { "GAROUA", 1.07 }, { "AUTRES LOCALITES", 1.2 } });
            s.Data.Add("OUEST", new Dictionary<string, double> { { "BAFOUSSAM, BANGANGTE, BANDJOUN, BAHAM, MBOUDA, BAFANG", 1.03 }, { "AUTRES LOCALITES", 1.05 } });
            s.Data.Add("SUD", new Dictionary<string, double> { { "EBOLOWA, SANGMELIMA, KRIBI", 1.03 }, { "AUTRES LOCALITES", 1.05 } });
            s.Data.Add("NORD-OUEST", new Dictionary<string, double> { { "BAMENDA, NDOP", 1.05 }, { "AUTRES LOCALITES", 1.1 } });
            s.Data.Add("SUD-OUEST", new Dictionary<string, double> { { "BUEA, LIMBE, KUMBA", 1.02 }, { "AUTRES LOCALITES", 1.04 } });
            return s;
        }
    }
}