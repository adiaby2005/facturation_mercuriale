using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FacturationMercuriale.Services
{
    public class LicenseService
    {
        private readonly string _appDataFolder;
        private readonly string _licensePath;
        private readonly string _trialPath;
        private const int MaxTrialExports = 5;

        public LicenseService()
        {
            // Définit le chemin : C:\Users\NomUtilisateur\AppData\Roaming\FacturationMercuriale
            _appDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FacturationMercuriale"
            );

            // Création automatique du dossier s'il n'existe pas encore
            if (!Directory.Exists(_appDataFolder))
            {
                Directory.CreateDirectory(_appDataFolder);
            }

            // Chemins vers les fichiers de données
            _licensePath = Path.Combine(_appDataFolder, "license.dat");
            _trialPath = Path.Combine(_appDataFolder, "trial.dat");
        }

        // --- Logique de Calcul (Strictement identique à l'activateur Android) ---
        public string GenerateKey(string machineId)
        {
            if (string.IsNullOrWhiteSpace(machineId)) return "";

            // Nettoyage strict : minuscules, pas d'espaces, pas de tirets
            string cleanId = machineId.Replace("-", "").Replace(" ", "").ToLowerInvariant().Trim();

            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(cleanId));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }

                // Extraction des 16 premiers caractères
                string rawKey = builder.ToString().Substring(0, 16).ToUpper();

                // Formatage XXXX-XXXX-XXXX-XXXX
                return $"{rawKey.Substring(0, 4)}-{rawKey.Substring(4, 4)}-{rawKey.Substring(8, 4)}-{rawKey.Substring(12, 4)}";
            }
        }

        // --- Gestion de la Licence ---
        public bool IsActivated()
        {
            if (!File.Exists(_licensePath)) return false;

            try
            {
                string savedKey = File.ReadAllText(_licensePath).Trim();
                // Vérifie la clé par rapport à l'ID de la machine actuelle
                string currentMachineId = new ActivationService().GetMachineId();
                return savedKey == GenerateKey(currentMachineId);
            }
            catch
            {
                return false;
            }
        }

        public void SaveLicense(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            File.WriteAllText(_licensePath, key.Trim().ToUpper());
        }

        // --- Gestion de la Période d'Essai (Exports) ---
        public bool CanExport()
        {
            if (IsActivated()) return true;
            return GetExportCount() < MaxTrialExports;
        }

        public int GetExportCount()
        {
            if (!File.Exists(_trialPath)) return 0;

            try
            {
                string val = File.ReadAllText(_trialPath);
                return int.TryParse(val, out int count) ? count : 0;
            }
            catch
            {
                return 0;
            }
        }

        public void IncrementExportCount()
        {
            if (IsActivated()) return;

            int current = GetExportCount();
            File.WriteAllText(_trialPath, (current + 1).ToString());
        }
    }
}