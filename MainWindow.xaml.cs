using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Collections.Generic;
using FacturationMercuriale.Models;
using FacturationMercuriale.Services;
using FacturationMercuriale.ViewModels;
using FacturationMercuriale.Views;
using Microsoft.Win32;

namespace FacturationMercuriale
{
    public partial class MainWindow : Window
    {
        private readonly MercurialeService _mercuriale;
        private readonly CameroonLocationsService _locations;
        private readonly PdfInvoiceService _pdf;
        private readonly WordInvoiceService _word;
        private readonly InvoiceStorageService _storage = new();
        private readonly LicenseService _license = new();
        private readonly PricingService _pricing = new();
        private readonly MainViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();
            _mercuriale = CreateMercurialeService();
            _locations = new CameroonLocationsService();
            _pdf = new PdfInvoiceService();
            _word = new WordInvoiceService();

            try { _mercuriale.Load(); } catch { }

            _vm = new MainViewModel(_mercuriale, _locations);
            DataContext = _vm;
        }

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Column.Header.ToString() == "P.U HT" && e.Row.DataContext is InvoiceLine)
            {
                Dispatcher.BeginInvoke(new Action(() => {
                    _vm.CurrentInvoice.Recalculate();
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog { Filter = "PDF Files|*.pdf", FileName = BuildDefaultFileName("pdf") };
            if (sfd.ShowDialog() == true && sfd.FileName != null)
            {
                _pdf.Generate(sfd.FileName, _vm.CurrentInvoice, _vm.Header);
                MessageBox.Show("Facture PDF générée avec succès !");
            }
        }

        private void ExportWord_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog { Filter = "Word Files|*.docx", FileName = BuildDefaultFileName("docx") };
            if (sfd.ShowDialog() == true && sfd.FileName != null)
            {
                _word.Generate(sfd.FileName, _vm.CurrentInvoice, _vm.Header);
                MessageBox.Show("Facture Word générée avec succès !");
            }
        }

        private void SaveInvoice_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog { Filter = "JSON Files|*.json", FileName = BuildDefaultFileName("json") };
            if (sfd.ShowDialog() == true && sfd.FileName != null)
            {
                var data = new SavedInvoice
                {
                    Region = _vm.SelectedRegion ?? string.Empty,
                    City = _vm.SelectedCity ?? string.Empty,
                    InvoiceNumber = _vm.Header.InvoiceNumber ?? string.Empty,
                    InvoiceDate = _vm.Header.InvoiceDate,
                    Doit = _vm.Header.Doit ?? string.Empty,
                    Objet = _vm.Header.Objet ?? string.Empty,
                    Direction = _vm.Header.Direction ?? string.Empty,
                    Lines = _vm.CurrentInvoice.Lines.Select(l => new SavedInvoiceLine
                    {
                        RefArticle = l.RefArticle ?? string.Empty,
                        Designation = l.Designation ?? string.Empty,
                        Quantity = (double)l.Quantity,
                        UnitPriceHt = (double)l.UnitPriceHt
                    }).ToList()
                };

                _storage.Save(sfd.FileName, data);
                MessageBox.Show("Données sauvegardées.");
            }
        }

        private void OpenInvoice_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog { Filter = "JSON Files|*.json" };
            if (ofd.ShowDialog() == true && ofd.FileName != null)
            {
                var dto = _storage.Load(ofd.FileName);
                if (dto != null)
                {
                    // NOM EXACT DE TA MÉTHODE (Ligne 98 de ton fichier envoyé)
                    _vm.LoadFromStoredInvoice(dto);
                    TrySetHeaderLocation(_vm.Header, dto.Region, dto.City);
                }
            }
        }

        private void OpenAddArticle_Click(object sender, RoutedEventArgs e)
        {
            var picker = new ArticlePickerWindow(_mercuriale) { Owner = this };

            picker.ArticleChosen += (s, article) =>
            {
                if (article != null)
                {
                    _vm.AddArticleToInvoice(article);
                }
            };

            picker.ShowDialog();
        }

        private void RemoveLine_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is InvoiceLine line)
            {
                _vm.RemoveLine(line);
            }
        }

        private void EditPricing_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new PricingEditorWindow(_pricing) { Owner = this };
            dlg.ShowDialog();
        }

        private void ActivateApp_Click(object sender, RoutedEventArgs e)
        {
            string id = new ActivationService().GetMachineId();
            var dlg = new ActivationWindow(id) { Owner = this };

            if (dlg.ShowDialog() == true)
            {
                string key = dlg.LicenseKey;
                if (!string.IsNullOrEmpty(key))
                {
                    _license.SaveLicense(key);
                    _vm.CheckLicenseStatus();
                    if (!_vm.IsReadOnlyMode)
                        MessageBox.Show("Logiciel activé ! Merci de votre confiance.");
                    else
                        MessageBox.Show("Clé invalide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private MercurialeService CreateMercurialeService() => new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "mercuriale_complete.json"));

        private string BuildDefaultFileName(string ext) => $"FACTURE_{_vm.Header.InvoiceNumber ?? "PRO"}_{DateTime.Now.Year}.{ext}";

        private static void TrySetHeaderLocation(InvoiceHeader header, string? region, string? city)
        {
            if (header == null) return;
            void Set(string n, string? v)
            {
                var p = header.GetType().GetProperty(n, BindingFlags.Public | BindingFlags.Instance);
                if (p != null && p.CanWrite) p.SetValue(header, v);
            }
            Set("Region", region); Set("City", city);
        }
    }
}