using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using FacturationMercuriale.Models;
using FacturationMercuriale.Services;

namespace FacturationMercuriale.Views
{
    public partial class PricingEditorWindow : Window
    {
        private readonly PricingService _service;
        public ObservableCollection<PricingRow> Rows { get; set; } = new();

        public PricingEditorWindow(PricingService service)
        {
            InitializeComponent();
            _service = service;
            LoadData();
            PricingGrid.ItemsSource = Rows;
        }

        private void LoadData()
        {
            var settings = _service.GetSettings();
            foreach (var region in settings.Data)
                foreach (var loc in region.Value)
                    Rows.Add(new PricingRow { Region = region.Key, Locality = loc.Key, Coefficient = loc.Value });
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => this.Close();

        public class PricingRow
        {
            public string Region { get; set; } = "";
            public string Locality { get; set; } = "";
            public double Coefficient { get; set; }
        }
    }
}