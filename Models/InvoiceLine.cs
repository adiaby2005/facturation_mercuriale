using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FacturationMercuriale.Models
{
    public sealed class InvoiceLine : INotifyPropertyChanged
    {
        private int _lineNumber;
        private int _quantity;
        private decimal _unitPriceHt;
        private string _designation = "";
        private string _unit = "";

        public string RefArticle { get; set; } = "";

        public int LineNumber
        {
            get => _lineNumber;
            set { _lineNumber = value; OnPropertyChanged(); }
        }

        public string Designation
        {
            get => _designation;
            set { _designation = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalHt)); }
        }

        // ✅ Nouvelle propriété
        public string Unit
        {
            get => _unit;
            set { _unit = value; OnPropertyChanged(); }
        }

        public int Quantity
        {
            get => _quantity;
            set { _quantity = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalHt)); }
        }

        public decimal UnitPriceHt
        {
            get => _unitPriceHt;
            set { _unitPriceHt = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalHt)); }
        }

        public decimal TotalHt => UnitPriceHt * Quantity;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
