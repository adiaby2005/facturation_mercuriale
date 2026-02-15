using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using FacturationMercuriale.Models;
using FacturationMercuriale.Services;

namespace FacturationMercuriale.ViewModels
{
    public sealed class MainViewModel : INotifyPropertyChanged
    {
        private readonly MercurialeService _mercuriale;
        private readonly CameroonLocationsService _locations;
        private readonly LicenseService _licenseService = new();
        private readonly PricingService _pricingService = new();

        // Titre dynamique de la fenêtre
        public string WindowTitle => IsReadOnlyMode
            ? $"Facturation Mercuriale ({_mercuriale.Rubriques.Count} rubriques) - VERSION D'ESSAI"
            : $"Facturation Mercuriale ({_mercuriale.Rubriques.Count} rubriques) - VERSION LICENCIÉE";

        public InvoiceHeader Header { get; } = new();
        public Invoice CurrentInvoice { get; } = new();
        public ObservableCollection<string> Regions { get; } = new();
        public ObservableCollection<string> Cities { get; } = new();

        private bool _isReadOnlyMode;
        public bool IsReadOnlyMode
        {
            get => _isReadOnlyMode;
            set
            {
                if (Set(ref _isReadOnlyMode, value))
                {
                    OnPropertyChanged(nameof(WindowTitle));
                }
            }
        }

        private string? _selectedRegion;
        public string? SelectedRegion
        {
            get => _selectedRegion;
            set
            {
                if (Set(ref _selectedRegion, value))
                {
                    ReloadCities();
                    // Mettre à jour automatiquement le Header
                    Header.Region = value ?? string.Empty;
                    OnPropertyChanged(nameof(ActiveMaxPriceHint));
                }
            }
        }

        private string? _selectedCity;
        public string? SelectedCity
        {
            get => _selectedCity;
            set
            {
                if (Set(ref _selectedCity, value))
                {
                    // Mettre à jour automatiquement le Header
                    Header.City = value ?? string.Empty;
                    OnPropertyChanged(nameof(ActiveMaxPriceHint));
                }
            }
        }

        private InvoiceLine? _selectedLine;
        public InvoiceLine? SelectedLine
        {
            get => _selectedLine;
            set
            {
                if (Set(ref _selectedLine, value))
                    OnPropertyChanged(nameof(ActiveMaxPriceHint));
            }
        }

        private decimal _tvaPercent = 19.25m;
        public decimal TvaPercent
        {
            get => _tvaPercent;
            set
            {
                if (Set(ref _tvaPercent, value))
                {
                    var rate = value / 100m;
                    CurrentInvoice.TvaRate = rate;
                    Header.TvaRate = rate;
                    CurrentInvoice.Recalculate();
                }
            }
        }

        private decimal _irPercent = 5.5m;
        public decimal IrPercent
        {
            get => _irPercent;
            set
            {
                if (Set(ref _irPercent, value))
                {
                    var rate = value / 100m;
                    CurrentInvoice.IrRate = rate;
                    Header.IrRate = rate;
                    CurrentInvoice.Recalculate();
                }
            }
        }

        public string ActiveMaxPriceHint
        {
            get
            {
                if (SelectedLine == null) return "Sélectionnez une ligne pour voir le plafond autorisé.";
                var art = _mercuriale.Rubriques.SelectMany(r => r.SousRubriques).SelectMany(s => s.Articles).FirstOrDefault(a => a.RefArticle == SelectedLine.RefArticle);
                if (art == null) return "Article non référencé.";
                double max = _pricingService.GetMaxAllowedPrice(SelectedRegion, SelectedCity, (double)art.Prix);
                return $"Plafond autorisé (Borne) : {max:N0} FCFA";
            }
        }

        public MainViewModel(MercurialeService mercuriale, CameroonLocationsService locations)
        {
            _mercuriale = mercuriale;
            _locations = locations;

            foreach (var r in _locations.GetRegions()) Regions.Add(r);
            SelectedRegion = Regions.FirstOrDefault();

            // Synchronisation initiale des taux
            Header.TvaRate = _tvaPercent / 100m;
            Header.IrRate = _irPercent / 100m;
            CurrentInvoice.TvaRate = _tvaPercent / 100m;
            CurrentInvoice.IrRate = _irPercent / 100m;

            CheckLicenseStatus();
        }

        public void ReloadCities()
        {
            Cities.Clear();
            if (!string.IsNullOrEmpty(SelectedRegion))
                foreach (var c in _locations.GetCities(SelectedRegion))
                    Cities.Add(c);
            SelectedCity = Cities.FirstOrDefault();
        }

        public void AddArticleToInvoice(Article article)
        {
            if (article == null || IsReadOnlyMode) return;
            CurrentInvoice.Lines.Add(new InvoiceLine
            {
                RefArticle = article.RefArticle,
                Designation = article.DesignationFr,
                UnitPriceHt = article.Prix,
                Quantity = 1,
                LineNumber = CurrentInvoice.Lines.Count + 1
            });
            CurrentInvoice.Recalculate();
        }

        public void ValidatePrice(InvoiceLine line)
        {
            var art = _mercuriale.Rubriques
                .SelectMany(r => r.SousRubriques)
                .SelectMany(s => s.Articles)
                .FirstOrDefault(a => a.RefArticle == line.RefArticle);

            if (art != null)
            {
                // Calculer le prix maximum autorisé via le PricingService
                double maxAllowed = _pricingService.GetMaxAllowedPrice(
                    SelectedRegion,
                    SelectedCity,
                    (double)art.Prix
                );

                // Vérifier si le prix proposé dépasse le maximum
                if ((double)line.UnitPriceHt > maxAllowed)
                {
                    MessageBox.Show(
                        $"Prix plafond dépassé !\n\n" +
                        $"Prix de base (Mercuriale) : {art.Prix:N0} FCFA\n" +
                        $"Prix maximum autorisé pour {SelectedRegion}" +
                        (!string.IsNullOrEmpty(SelectedCity) ? $" - {SelectedCity}" : "") +
                        $" : {maxAllowed:N0} FCFA\n" +
                        $"Prix saisi : {line.UnitPriceHt:N0} FCFA\n\n" +
                        $"Le prix a été automatiquement ajusté au plafond autorisé.",
                        "Contrôle des prix",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );

                    // Ajuster automatiquement au prix maximum autorisé
                    line.UnitPriceHt = (decimal)maxAllowed;
                }
            }

            // Recalculer la facture après modification
            CurrentInvoice.Recalculate();
            OnPropertyChanged(nameof(ActiveMaxPriceHint));
        }

        public void LoadFromStoredInvoice(SavedInvoice dto)
        {
            Header.InvoiceNumber = dto.InvoiceNumber;
            Header.InvoiceDate = dto.InvoiceDate;
            Header.Doit = dto.Doit;
            Header.Objet = dto.Objet;
            Header.Direction = dto.Direction;
            Header.Region = dto.Region;
            Header.City = dto.City;
            Header.TvaRate = CurrentInvoice.TvaRate;
            Header.IrRate = CurrentInvoice.IrRate;

            SelectedRegion = dto.Region;
            SelectedCity = dto.City;

            CurrentInvoice.Lines.Clear();
            foreach (var l in dto.Lines)
            {
                CurrentInvoice.Lines.Add(new InvoiceLine
                {
                    RefArticle = l.RefArticle,
                    Designation = l.Designation,
                    Quantity = (int)l.Quantity,
                    UnitPriceHt = (decimal)l.UnitPriceHt
                });
            }
            ReNumberLines();
            CurrentInvoice.Recalculate();
            OnPropertyChanged(string.Empty);
        }

        public void RemoveLine(InvoiceLine line)
        {
            CurrentInvoice.Lines.Remove(line);
            ReNumberLines();
            CurrentInvoice.Recalculate();
        }

        private void ReNumberLines()
        {
            int i = 1;
            foreach (var l in CurrentInvoice.Lines)
                l.LineNumber = i++;
        }

        public void CheckLicenseStatus() => IsReadOnlyMode = !_licenseService.IsActivated();

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
        private bool Set<T>(ref T f, T v, [CallerMemberName] string? n = null) { if (Equals(f, v)) return false; f = v; OnPropertyChanged(n); return true; }
    }
}