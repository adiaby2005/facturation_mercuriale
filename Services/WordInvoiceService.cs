using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using W = DocumentFormat.OpenXml.Wordprocessing; // Utilisation de l'alias pour éviter les conflits
using FacturationMercuriale.Models;
using System;
using System.Globalization;
using System.Linq;

namespace FacturationMercuriale.Services
{
    public sealed class WordInvoiceService
    {
        // A4 portrait (twips)
        private const uint PageWidthTwips = 11906;   // 8.27"
        private const uint PageHeightTwips = 16838;  // 11.69"

        // Marges (twips)
        private const int MarginLeftTwips = 1440;
        private const int MarginRightTwips = 1440;
        private const int MarginTopTwips = 1800;
        private const int MarginBottomTwips = 1800;

        private const uint HeaderTwips = 900;
        private const uint FooterTwips = 900;

        public void Generate(string filePath, Invoice invoice, InvoiceHeader header)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Chemin de fichier invalide.", nameof(filePath));

            if (invoice == null) throw new ArgumentNullException(nameof(invoice));
            if (header == null) throw new ArgumentNullException(nameof(header));

            invoice.Recalculate();

            using var doc = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document);
            var main = doc.AddMainDocumentPart();
            main.Document = new W.Document(new W.Body());

            EnsureStyles(main);

            var body = main.Document.Body!;

            body.Append(CreateSectionProperties());
            body.Append(ParagraphEmptyLines(1));

            var year = header.InvoiceDate.Year;
            var number = string.IsNullOrWhiteSpace(header.InvoiceNumber) ? "—" : header.InvoiceNumber.Trim();
            body.Append(ParagraphCenter($"FACTURE PROFORMA N° {number}/{year}", bold: true, fontSizeHalfPoints: 28));

            body.Append(ParagraphEmptyLines(1));

            // ✅ Suppression du ParagraphEmptyLines après Doit et Objet
            body.Append(ParagraphLabelAndValue("Doit : ", header.Doit, boldUnderlineLabel: true));
            body.Append(ParagraphLabelAndValue("Objet : ", header.Objet, boldUnderlineLabel: true));

            body.Append(ParagraphEmptyLines(1));

            var linesTable = CreateLinesTable(invoice);
            body.Append(linesTable);

            body.Append(ParagraphEmptyLines(1));

            var totalsTable = CreateTotalsTable(invoice, header);
            body.Append(totalsTable);

            body.Append(ParagraphEmptyLines(1));

            var totalInWordsPara = new W.Paragraph();

            // 1. Le texte d'introduction (Normal)
            // Utilisation du nom complet pour SpaceProcessingModeValues
            totalInWordsPara.Append(new W.Run(new W.Text("Arrêtée la présente facture à la somme de ")
            { Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve }));

            // 2. Le montant (Gras)
            var runWords = new W.Run();
            runWords.Append(new W.RunProperties(new W.Bold()));

            // Utilisation de ?? "" pour corriger l'erreur "Existence possible d'un retour de référence null"
            runWords.Append(new W.Text(invoice.TotalTtcInWords ?? "")
            { Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve });

            totalInWordsPara.Append(runWords);

            body.Append(totalInWordsPara);

            body.Append(ParagraphEmptyLines(1));

            var city = string.IsNullOrWhiteSpace(header.City) ? "________" : header.City.Trim();
            var dateFr = header.InvoiceDate.ToString("dd MMMM yyyy", CultureInfo.GetCultureInfo("fr-FR"));

            // ✅ Suppression du ParagraphEmptyLines après Fait à...
            body.Append(ParagraphRight($"Fait à {city}, le {dateFr}."));

            var sign = string.IsNullOrWhiteSpace(header.Direction) ? "LA DIRECTION" : header.Direction.Trim();
            body.Append(ParagraphRight(sign, bold: true));

            main.Document.Save();
        }

        private static void EnsureStyles(MainDocumentPart main)
        {
            var stylesPart = main.StyleDefinitionsPart ?? main.AddNewPart<StyleDefinitionsPart>();

            if (stylesPart.Styles == null)
                stylesPart.Styles = new W.Styles();

            stylesPart.Styles.DocDefaults = CreateDocDefaults();
            stylesPart.Styles.Save();
        }

        private static W.DocDefaults CreateDocDefaults()
        {
            var runPropsDefault = new W.RunPropertiesDefault(
                new W.RunPropertiesBaseStyle(
                    new W.RunFonts { Ascii = "Calibri", HighAnsi = "Calibri", EastAsia = "Calibri", ComplexScript = "Calibri" },
                    new W.FontSize { Val = "22" },
                    new W.FontSizeComplexScript { Val = "22" }
                )
            );

            var paraPropsDefault = new W.ParagraphPropertiesDefault(
                new W.ParagraphPropertiesBaseStyle(
                    new W.SpacingBetweenLines { Line = "240", LineRule = W.LineSpacingRuleValues.Auto, After = "120" }
                )
            );

            return new W.DocDefaults(runPropsDefault, paraPropsDefault);
        }

        private static W.SectionProperties CreateSectionProperties()
        {
            return new W.SectionProperties(
                new W.PageSize
                {
                    Width = (UInt32Value)PageWidthTwips,
                    Height = (UInt32Value)PageHeightTwips,
                    Orient = W.PageOrientationValues.Portrait
                },
                new W.PageMargin
                {
                    Top = MarginTopTwips,
                    Bottom = MarginBottomTwips,
                    Left = (UInt32Value)MarginLeftTwips,
                    Right = (UInt32Value)MarginRightTwips,
                    Header = HeaderTwips,
                    Footer = FooterTwips,
                    Gutter = 0U
                }
            );
        }

        private static W.Table CreateLinesTable(Invoice invoice)
        {
            var table = new W.Table();

            table.AppendChild(new W.TableProperties(
                new W.TableWidth { Type = W.TableWidthUnitValues.Pct, Width = "5000" },
                CreateTableBorders(),
                new W.TableLook { Val = "04A0" }
            ));

            var headerRow = new W.TableRow();
            headerRow.Append(
                HeaderCell("N°", 700),
                HeaderCell("Réf", 2000),
                HeaderCell("Désignation", 5200),
                HeaderCell("Qté", 900),
                HeaderCell("PU HT", 1500),
                HeaderCell("Total HT", 1700)
            );
            table.Append(headerRow);

            int i = 1;
            foreach (var l in invoice.Lines)
            {
                var row = new W.TableRow();
                row.Append(
                    CellText(i.ToString(), 700, W.JustificationValues.Center),
                    CellText(l.RefArticle ?? "", 2000, W.JustificationValues.Left),
                    CellText(l.Designation ?? "", 5200, W.JustificationValues.Left),
                    CellText(l.Quantity.ToString(CultureInfo.InvariantCulture), 900, W.JustificationValues.Right),
                    CellText(FormatMoney(l.UnitPriceHt), 1500, W.JustificationValues.Right),
                    CellText(FormatMoney(l.TotalHt), 1700, W.JustificationValues.Right)
                );
                table.Append(row);
                i++;
            }

            if (!invoice.Lines.Any())
            {
                var row = new W.TableRow();
                row.Append(
                    CellText("", 700, W.JustificationValues.Center),
                    CellText("", 2000, W.JustificationValues.Left),
                    CellText("Aucun article", 5200, W.JustificationValues.Left),
                    CellText("", 900, W.JustificationValues.Right),
                    CellText("", 1500, W.JustificationValues.Right),
                    CellText("", 1700, W.JustificationValues.Right)
                );
                table.Append(row);
            }

            return table;
        }

        private static W.Table CreateTotalsTable(Invoice invoice, InvoiceHeader header)
        {
            var table = new W.Table();

            table.AppendChild(new W.TableProperties(
                new W.TableWidth { Type = W.TableWidthUnitValues.Pct, Width = "5000" },
                CreateTableBorders(),
                new W.TableLook { Val = "04A0" }
            ));

            table.Append(new W.TableRow(
                CellText("Total HT", 6000, W.JustificationValues.Left, bold: true),
                CellText(FormatMoney(invoice.TotalHt) + " F CFA", 2000, W.JustificationValues.Right, bold: true)
            ));

            // Convertir les taux en pourcentages pour l'affichage
            var tvaPercent = header.TvaRate * 100;
            var irPercent = header.IrRate * 100;

            var tvaPct = tvaPercent.ToString("0.##", CultureInfo.GetCultureInfo("fr-FR"));
            var irPct = irPercent.ToString("0.##", CultureInfo.GetCultureInfo("fr-FR"));

            table.Append(new W.TableRow(
                CellText($"TVA ({tvaPct}%)", 6000, W.JustificationValues.Left),
                CellText(FormatMoney(invoice.TotalTva) + " F CFA", 2000, W.JustificationValues.Right)
            ));

            table.Append(new W.TableRow(
                CellText($"IR ({irPct}%)", 6000, W.JustificationValues.Left),
                CellText(FormatMoney(invoice.TotalIr) + " F CFA", 2000, W.JustificationValues.Right)
            ));

            table.Append(new W.TableRow(
                CellText("TTC (Montant TTC)", 6000, W.JustificationValues.Left, bold: true),
                CellText(FormatMoney(invoice.TotalTtc) + " F CFA", 2000, W.JustificationValues.Right, bold: true)
            ));

            table.Append(new W.TableRow(
                CellText("Net à percevoir", 6000, W.JustificationValues.Left),
                CellText(FormatMoney(invoice.NetAPayer) + " F CFA", 2000, W.JustificationValues.Right)
            ));

            return table;
        }

        private static W.TableBorders CreateTableBorders()
        {
            var borderSize = (UInt32Value)8U;
            const string borderColor = "000000";

            return new W.TableBorders(
                new W.TopBorder { Val = W.BorderValues.Single, Size = borderSize, Color = borderColor },
                new W.LeftBorder { Val = W.BorderValues.Single, Size = borderSize, Color = borderColor },
                new W.BottomBorder { Val = W.BorderValues.Single, Size = borderSize, Color = borderColor },
                new W.RightBorder { Val = W.BorderValues.Single, Size = borderSize, Color = borderColor },
                new W.InsideHorizontalBorder { Val = W.BorderValues.Single, Size = borderSize, Color = borderColor },
                new W.InsideVerticalBorder { Val = W.BorderValues.Single, Size = borderSize, Color = borderColor }
            );
        }

        private static W.TableCell HeaderCell(string text, int widthTwips)
        {
            return new W.TableCell(
                new W.TableCellProperties(
                    new W.TableCellWidth { Type = W.TableWidthUnitValues.Dxa, Width = widthTwips.ToString(CultureInfo.InvariantCulture) },
                    new W.Shading { Val = W.ShadingPatternValues.Clear, Fill = "EDEDED" },
                    new W.TableCellVerticalAlignment { Val = W.TableVerticalAlignmentValues.Center }
                ),
                new W.Paragraph(
                    new W.ParagraphProperties(new W.Justification { Val = W.JustificationValues.Center }),
                    new W.Run(RunProps(bold: true), new W.Text(text ?? "") { Space = SpaceProcessingModeValues.Preserve })
                )
            );
        }

        private static W.TableCell CellText(string text, int widthTwips, W.JustificationValues justify, bool bold = false)
        {
            return new W.TableCell(
                new W.TableCellProperties(
                    new W.TableCellWidth { Type = W.TableWidthUnitValues.Dxa, Width = widthTwips.ToString(CultureInfo.InvariantCulture) },
                    new W.TableCellVerticalAlignment { Val = W.TableVerticalAlignmentValues.Center }
                ),
                new W.Paragraph(
                    new W.ParagraphProperties(new W.Justification { Val = justify }),
                    new W.Run(RunProps(bold: bold), new W.Text(text ?? "") { Space = SpaceProcessingModeValues.Preserve })
                )
            );
        }

        private static W.RunProperties RunProps(bool bold = false, bool underline = false)
        {
            var rp = new W.RunProperties(
                new W.RunFonts { Ascii = "Calibri", HighAnsi = "Calibri", EastAsia = "Calibri", ComplexScript = "Calibri" },
                new W.FontSize { Val = "22" },
                new W.FontSizeComplexScript { Val = "22" }
            );

            if (bold) rp.Append(new W.Bold());
            if (underline) rp.Append(new W.Underline { Val = W.UnderlineValues.Single });

            return rp;
        }

        private static W.Paragraph ParagraphLabelAndValue(string label, string? value, bool boldUnderlineLabel)
        {
            var p = new W.Paragraph(new W.ParagraphProperties(new W.Justification { Val = W.JustificationValues.Left }));

            p.Append(new W.Run(
                RunProps(bold: boldUnderlineLabel, underline: boldUnderlineLabel),
                new W.Text(label ?? "") { Space = SpaceProcessingModeValues.Preserve }
            ));

            p.Append(new W.Run(
                RunProps(),
                new W.Text(value ?? "") { Space = SpaceProcessingModeValues.Preserve }
            ));

            return p;
        }

        private static W.Paragraph ParagraphLeft(string text, bool italic = false)
        {
            var p = new W.Paragraph(new W.ParagraphProperties(new W.Justification { Val = W.JustificationValues.Left }));
            var r = new W.Run();
            if (italic) r.Append(new W.RunProperties(new W.Italic()));
            r.Append(new W.Text(text ?? "") { Space = SpaceProcessingModeValues.Preserve });
            p.Append(r);
            return p;
        }

        private static W.Paragraph ParagraphRight(string text, bool bold = false)
        {
            var p = new W.Paragraph(new W.ParagraphProperties(new W.Justification { Val = W.JustificationValues.Right }));
            p.Append(new W.Run(RunProps(bold: bold), new W.Text(text ?? "") { Space = SpaceProcessingModeValues.Preserve }));
            return p;
        }

        private static W.Paragraph ParagraphCenter(string text, bool bold, int fontSizeHalfPoints)
        {
            var val = fontSizeHalfPoints.ToString(CultureInfo.InvariantCulture);

            var p = new W.Paragraph(new W.ParagraphProperties(new W.Justification { Val = W.JustificationValues.Center }));
            var rp = new W.RunProperties(
                new W.RunFonts { Ascii = "Calibri", HighAnsi = "Calibri", EastAsia = "Calibri", ComplexScript = "Calibri" },
                new W.FontSize { Val = val },
                new W.FontSizeComplexScript { Val = val }
            );
            if (bold) rp.Append(new W.Bold());

            p.Append(new W.Run(rp, new W.Text(text ?? "") { Space = SpaceProcessingModeValues.Preserve }));
            return p;
        }

        private static W.Paragraph ParagraphEmptyLines(int count)
        {
            var p = new W.Paragraph();
            var after = (count * 240).ToString(CultureInfo.InvariantCulture);
            p.ParagraphProperties = new W.ParagraphProperties(
                new W.SpacingBetweenLines { Before = "0", After = after, Line = "240", LineRule = W.LineSpacingRuleValues.Auto }
            );
            p.Append(new W.Run(new W.Text("") { Space = SpaceProcessingModeValues.Preserve }));
            return p;
        }

        private static string FormatMoney(decimal v)
            => v.ToString("N0", CultureInfo.GetCultureInfo("fr-FR"));
    }
}