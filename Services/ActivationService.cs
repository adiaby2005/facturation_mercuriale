using System;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace FacturationMercuriale.Services
{
    public class ActivationService
    {
        public string GetMachineId()
        {
            // On inclut l'année pour que l'ID change chaque année (selon tes besoins)
            string rawId = $"{Environment.MachineName}-{GetCpuId()}-{GetVolumeSerial()}-2026";
            return ComputeHash(rawId);
        }

        private string GetCpuId()
        {
            try
            {
                string cpuInfo = string.Empty;
                using var mc = new ManagementClass("win32_processor");
                foreach (var mo in mc.GetInstances())
                {
                    cpuInfo = mo.Properties["ProcessorId"].Value?.ToString() ?? "";
                    break;
                }
                return cpuInfo;
            }
            catch { return "CPU-UNKNOWN"; }
        }

        private string GetVolumeSerial()
        {
            try
            {
                using var disk = new ManagementObject("win32_logicaldisk.deviceid=\"c:\"");
                disk.Get();
                return disk["VolumeSerialNumber"].ToString() ?? "VOL-UNKNOWN";
            }
            catch { return "VOL-UNKNOWN"; }
        }

        private string ComputeHash(string input)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            string hex = BitConverter.ToString(bytes).Replace("-", "").Substring(0, 16);

            // Insertion d'un tiret tous les 4 caractères : XXXX-XXXX-XXXX-XXXX
            return string.Join("-", Enumerable.Range(0, hex.Length / 4)
                                              .Select(i => hex.Substring(i * 4, 4)));
        }
    }
}