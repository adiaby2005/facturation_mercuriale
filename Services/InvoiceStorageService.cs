using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FacturationMercuriale.Models;

namespace FacturationMercuriale.Services
{
    public interface IInvoiceStorageService
    {
        void Save(string path, SavedInvoice invoice);
        SavedInvoice Load(string path);
    }

    public class InvoiceStorageService : IInvoiceStorageService
    {
        private readonly JsonSerializerOptions _options;

        public InvoiceStorageService()
        {
            _options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };
        }

        public void Save(string path, SavedInvoice invoice)
        {
            try
            {
                string json = JsonSerializer.Serialize(invoice, _options);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors de l'enregistrement : {ex.Message}");
            }
        }

        public SavedInvoice Load(string path)
        {
            try
            {
                if (!File.Exists(path)) throw new FileNotFoundException("Fichier introuvable.");

                string json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<SavedInvoice>(json, _options);
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors de l'ouverture du fichier : {ex.Message}");
            }
        }
    }
}