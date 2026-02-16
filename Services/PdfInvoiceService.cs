using System;
using System.Globalization;
using System.IO;
using System.Linq;
using FacturationMercuriale.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FacturationMercuriale.Services
{
    public sealed class PdfInvoiceService
    {
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

            var fr = CultureInfo.GetCultureInfo("fr-FR");
            var number = string.IsNullOrWhiteSpace(header.InvoiceNumber) ? "—" : header.InvoiceNumber.Trim();
            var year = header.InvoiceDate.Year;
            var city = string.IsNullOrWhiteSpace(header.City) ? "________" : header.City.Trim();
            var dateFr = header.InvoiceDate.ToString("dd MMMM yyyy", fr);
            var sign = string.IsNullOrWhiteSpace(header.Direction) ? "LA DIRECTION" : header.Direction.Trim();

            // Convertir les taux en pourcentages pour l'affichage
            var tvaPercent = header.TvaRate * 100;
            var irPercent = header.IrRate * 100;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);

                    // Marges globales (Gauche/Droite)
                    page.MarginLeft(35);
                    page.MarginRight(35);

                    // ✅ ESPACE RÉSERVÉ POUR L'EN-TÊTE DU PAPIER (ex: 3.5cm)
                    page.Header().Height(100);

                    // ✅ ESPACE RÉSERVÉ POUR LE PIED DE PAGE DU PAPIER (ex: 2.5cm)
                    page.Footer().Height(70);

                    page.DefaultTextStyle(x => x.FontFamily("Calibri").FontSize(11));

                    page.Content().PaddingTop(10).Column(col =>
                    {
                        // Titre
                        col.Item().AlignCenter().Text($"FACTURE PROFORMA N° {number}/{year}")
                            .FontSize(14).SemiBold();

                        col.Item().PaddingTop(10);

                        // Doit
                        col.Item().Text(t =>
                        {
                            t.Span("Doit : ").SemiBold().Underline();
                            t.Span(header.Doit ?? "");
                        });

                        col.Item().PaddingTop(6);

                        // Objet
                        col.Item().Text(t =>
                        {
                            t.Span("Objet : ").SemiBold().Underline();
                            t.Span(header.Objet ?? "");
                        });

                        col.Item().PaddingTop(12);

                        // Tableau lignes
                        col.Item().Element(e => BuildLinesTable(e, invoice));

                        col.Item().PaddingTop(10);

                        // Tableau totaux avec taux en pourcentage
                        col.Item().AlignRight().Element(e => BuildTotalsTable(e, invoice, tvaPercent, irPercent));

                        col.Item().PaddingTop(10);

                        // Arrêtée + montant en lettres
                        col.Item().PaddingTop(20).Text(t =>
                        {
                            t.Span("Arrêtée la présente facture à la somme de "); // Texte normal sans ":"
                            t.Span(invoice.TotalTtcInWords).Bold(); // Montant en gras
                        });

                        col.Item().PaddingTop(20);

                        // Fait à... + Signataire
                        col.Item().AlignRight().Text($"Fait à {city}, le {dateFr}.");
                        col.Item().AlignRight().Text(sign).SemiBold();
                    });
                });
            })
            .GeneratePdf(filePath);
        }

        private static void BuildLinesTable(IContainer container, Invoice invoice)
        {
            var fr = CultureInfo.GetCultureInfo("fr-FR");

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);   // N°
                    columns.ConstantColumn(80);   // Réf
                    columns.RelativeColumn();     // Désignation
                    columns.ConstantColumn(35);   // Qté
                    columns.ConstantColumn(60);   // PU HT
                    columns.ConstantColumn(70);   // Total HT
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).AlignCenter().Text("N°").SemiBold();
                    header.Cell().Element(HeaderCell).Text("Réf").SemiBold();
                    header.Cell().Element(HeaderCell).Text("Désignation").SemiBold();
                    header.Cell().Element(HeaderCell).AlignRight().Text("Qté").SemiBold();
                    header.Cell().Element(HeaderCell).AlignRight().Text("PU HT").SemiBold();
                    header.Cell().Element(HeaderCell).AlignRight().Text("Total HT").SemiBold();
                });

                int i = 1;
                foreach (var l in invoice.Lines)
                {
                    table.Cell().Element(BodyCell).AlignCenter().Text(i.ToString(fr));
                    table.Cell().Element(BodyCell).Text(l.RefArticle ?? "");
                    table.Cell().Element(BodyCell).Text(l.Designation ?? "");
                    table.Cell().Element(BodyCell).AlignRight().Text(l.Quantity.ToString(fr));
                    table.Cell().Element(BodyCell).AlignRight().Text(FormatMoney(l.UnitPriceHt));
                    table.Cell().Element(BodyCell).AlignRight().Text(FormatMoney(l.TotalHt));
                    i++;
                }

                if (!invoice.Lines.Any())
                {
                    table.Cell().ColumnSpan(6).Element(BodyCell).AlignCenter().Text("Aucun article");
                }
            });

            static IContainer HeaderCell(IContainer c) => c.Border(1).BorderColor(Colors.Grey.Medium).Background(Colors.Grey.Lighten3).Padding(4);
            static IContainer BodyCell(IContainer c) => c.Border(1).BorderColor(Colors.Grey.Medium).Padding(4);
        }

        private static void BuildTotalsTable(IContainer container, Invoice invoice, decimal tvaPercent, decimal irPercent)
        {
            var fr = CultureInfo.GetCultureInfo("fr-FR");

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(220);
                    columns.ConstantColumn(120);
                });

                void Row(string label, string value, bool bold = false)
                {
                    table.Cell().Element(Cell).Text(label).SemiBold(bold);
                    table.Cell().Element(Cell).AlignRight().Text(value).SemiBold(bold);
                }

                Row("Total HT", $"{FormatMoney(invoice.TotalHt)} F CFA", bold: true);
                Row($"TVA ({tvaPercent.ToString("0.##", fr)}%)", $"{FormatMoney(invoice.TotalTva)} F CFA");
                Row($"IR ({irPercent.ToString("0.##", fr)}%)", $"{FormatMoney(invoice.TotalIr)} F CFA");
                Row("TTC (Montant TTC)", $"{FormatMoney(invoice.TotalTtc)} F CFA", bold: true);
                Row("Net à percevoir", $"{FormatMoney(invoice.NetAPayer)} F CFA");
            });

            static IContainer Cell(IContainer c) => c.Border(1).BorderColor(Colors.Grey.Medium).Padding(5);
        }

        private static string FormatMoney(decimal v) => v.ToString("N0", CultureInfo.GetCultureInfo("fr-FR"));
    }

    internal static class QuestPdfTextExtensions
    {
        public static TextSpanDescriptor SemiBold(this TextSpanDescriptor d, bool enabled) => enabled ? d.SemiBold() : d;
    }
}