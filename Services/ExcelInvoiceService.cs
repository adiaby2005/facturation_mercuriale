using System;
using System.IO;
using System.Linq;
using FacturationMercuriale.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace FacturationMercuriale.Services
{
    public sealed class ExcelInvoiceService
    {
        public ExcelInvoiceService()
        {
            // Configuration de la licence pour EPPlus 8 et versions ultérieures
            ExcelPackage.License.SetNonCommercialOrganization("Facturation Mercuriale");
        }

        public void Generate(string filePath, Invoice invoice, InvoiceHeader header)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Chemin de fichier invalide.", nameof(filePath));

            if (invoice == null) throw new ArgumentNullException(nameof(invoice));
            if (header == null) throw new ArgumentNullException(nameof(header));

            invoice.Recalculate();

            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Facture");

            // Configuration de la police par défaut
            worksheet.Cells.Style.Font.Name = "Calibri";
            worksheet.Cells.Style.Font.Size = 11;

            // Titre
            var titleCell = worksheet.Cells["A1:F1"];
            titleCell.Merge = true;
            titleCell.Value = $"FACTURE PROFORMA N° {header.InvoiceNumber}/{header.InvoiceDate.Year}";
            titleCell.Style.Font.Size = 16;
            titleCell.Style.Font.Bold = true;
            titleCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            titleCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            titleCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            titleCell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(52, 73, 94)); // #34495E
            titleCell.Style.Font.Color.SetColor(Color.White);
            worksheet.Row(1).Height = 30;

            // Informations client - CORRECTION : Fusionner les cellules pour Doit
            worksheet.Cells["A3"].Value = "Doit :";
            worksheet.Cells["A3"].Style.Font.Bold = true;
            worksheet.Cells["A3"].Style.Font.UnderLine = true;

            // Fusionner les cellules B3 à F3 pour le texte du client
            var doitValueCell = worksheet.Cells["B3:F3"];
            doitValueCell.Merge = true;
            doitValueCell.Value = header.Doit ?? "";
            doitValueCell.Style.WrapText = true;
            doitValueCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            // Informations objet - CORRECTION : Fusionner les cellules pour Objet
            worksheet.Cells["A4"].Value = "Objet :";
            worksheet.Cells["A4"].Style.Font.Bold = true;
            worksheet.Cells["A4"].Style.Font.UnderLine = true;

            // Fusionner les cellules B4 à F4 pour le texte de l'objet
            var objetValueCell = worksheet.Cells["B4:F4"];
            objetValueCell.Merge = true;
            objetValueCell.Value = header.Objet ?? "";
            objetValueCell.Style.WrapText = true;
            objetValueCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            worksheet.Row(3).Height = 25;
            worksheet.Row(4).Height = 25;

            // En-têtes du tableau
            int startRow = 6;
            string[] headers = { "N°", "Réf", "Désignation", "Qté", "PU HT", "Total HT" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cells[startRow, i + 1];
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(236, 240, 241)); // #ECF0F1
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }
            worksheet.Row(startRow).Height = 25;

            // Lignes d'articles
            int currentRow = startRow + 1;
            foreach (var line in invoice.Lines)
            {
                worksheet.Cells[currentRow, 1].Value = line.LineNumber;
                worksheet.Cells[currentRow, 2].Value = line.RefArticle ?? "";
                worksheet.Cells[currentRow, 3].Value = line.Designation ?? "";
                worksheet.Cells[currentRow, 4].Value = line.Quantity;
                worksheet.Cells[currentRow, 5].Value = (double)line.UnitPriceHt;
                worksheet.Cells[currentRow, 6].Value = (double)line.TotalHt;

                // Format monétaire
                worksheet.Cells[currentRow, 5].Style.Numberformat.Format = "#,##0";
                worksheet.Cells[currentRow, 6].Style.Numberformat.Format = "#,##0";

                // Bordures
                for (int col = 1; col <= 6; col++)
                {
                    worksheet.Cells[currentRow, col].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                // Alignements
                worksheet.Cells[currentRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[currentRow, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[currentRow, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                worksheet.Cells[currentRow, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                currentRow++;
            }

            // Ligne vide si pas d'articles
            if (!invoice.Lines.Any())
            {
                var emptyCell = worksheet.Cells[currentRow, 1, currentRow, 6];
                emptyCell.Merge = true;
                emptyCell.Value = "Aucun article";
                emptyCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                emptyCell.Style.Font.Italic = true;
                for (int col = 1; col <= 6; col++)
                {
                    worksheet.Cells[currentRow, col].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }
                currentRow++;
            }

            // Espacement
            currentRow += 2;

            // Tableau des totaux
            int totalRow = currentRow;
            var tvaPercent = header.TvaRate * 100;
            var irPercent = header.IrRate * 100;

            var totals = new[]
            {
                new { Label = "Total HT", Value = (double)invoice.TotalHt, Bold = true },
                new { Label = $"TVA ({tvaPercent:0.##}%)", Value = (double)invoice.TotalTva, Bold = false },
                new { Label = $"IR ({irPercent:0.##}%)", Value = (double)invoice.TotalIr, Bold = false },
                new { Label = "TTC (Montant TTC)", Value = (double)invoice.TotalTtc, Bold = true },
                new { Label = "Net à percevoir", Value = (double)invoice.NetAPayer, Bold = false }
            };

            for (int i = 0; i < totals.Length; i++)
            {
                int row = totalRow + i;

                // Label
                worksheet.Cells[row, 5].Value = totals[i].Label;
                worksheet.Cells[row, 5].Style.Font.Bold = totals[i].Bold;
                worksheet.Cells[row, 5].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                // Valeur
                worksheet.Cells[row, 6].Value = totals[i].Value;
                worksheet.Cells[row, 6].Style.Numberformat.Format = "#,##0 \"F CFA\"";
                worksheet.Cells[row, 6].Style.Font.Bold = totals[i].Bold;
                worksheet.Cells[row, 6].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                worksheet.Cells[row, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            }

            // Ligne "Net à percevoir" en plus grand
            int netRow = totalRow + 4;
            worksheet.Cells[netRow, 5].Style.Font.Size = 14;
            worksheet.Cells[netRow, 6].Style.Font.Size = 14;
            worksheet.Cells[netRow, 6].Style.Font.Color.SetColor(Color.FromArgb(39, 174, 96)); // #27AE60

            // Montant en toutes lettres
            currentRow = totalRow + 6;
            var totalWordsCell = worksheet.Cells[currentRow, 1, currentRow, 6];
            totalWordsCell.Merge = true;
            totalWordsCell.Value = $"Arrêtée la présente facture à la somme de {invoice.TotalTtcInWords}";
            totalWordsCell.Style.Font.Italic = true;
            totalWordsCell.Style.WrapText = true;

            // Fait à et signature
            currentRow += 2;
            var faitACell = worksheet.Cells[currentRow, 4, currentRow, 6];
            faitACell.Merge = true;
            var city = string.IsNullOrWhiteSpace(header.City) ? "________" : header.City.Trim();
            var dateFr = header.InvoiceDate.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("fr-FR"));
            faitACell.Value = $"Fait à {city}, le {dateFr}.";
            faitACell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

            currentRow++;
            var signCell = worksheet.Cells[currentRow, 4, currentRow, 6];
            signCell.Merge = true;
            var sign = string.IsNullOrWhiteSpace(header.Direction) ? "LA DIRECTION" : header.Direction.Trim();
            signCell.Value = sign;
            signCell.Style.Font.Bold = true;
            signCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

            // Ajustement automatique des colonnes
            worksheet.Column(1).Width = 10;  // N°
            worksheet.Column(2).Width = 15;  // Réf
            worksheet.Column(3).Width = 40;  // Désignation
            worksheet.Column(4).Width = 12;  // Qté
            worksheet.Column(5).Width = 18;  // PU HT
            worksheet.Column(6).Width = 20;  // Total HT

            // Sauvegarde
            package.SaveAs(new FileInfo(filePath));
        }
    }
}