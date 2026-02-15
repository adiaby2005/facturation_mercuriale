using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Reflection;

namespace FacturationMercuriale.Models
{
    public sealed class Invoice : INotifyPropertyChanged
    {
        public ObservableCollection<InvoiceLine> Lines { get; } = new();

        private decimal _tvaRate = 0.1925m; // 19.25%
        private decimal _irRate = 0.055m;  // 5.5%

        private decimal _totalHt;
        private decimal _totalTva;
        private decimal _totalIr;
        private decimal _totalTtc;
        private decimal _netAPayer;

        public decimal TvaRate
        {
            get => _tvaRate;
            set { if (_tvaRate == value) return; _tvaRate = value; OnPropertyChanged(); Recalculate(); }
        }

        public decimal IrRate
        {
            get => _irRate;
            set { if (_irRate == value) return; _irRate = value; OnPropertyChanged(); Recalculate(); }
        }

        public decimal TotalHt
        {
            get => _totalHt;
            private set { _totalHt = value; OnPropertyChanged(); }
        }

        public decimal TotalTva
        {
            get => _totalTva;
            private set { _totalTva = value; OnPropertyChanged(); }
        }

        public decimal TotalIr
        {
            get => _totalIr;
            private set { _totalIr = value; OnPropertyChanged(); }
        }

        public decimal TotalTtc
        {
            get => _totalTtc;
            private set { _totalTtc = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalTtcInWords)); }
        }

        public decimal NetAPayer
        {
            get => _netAPayer;
            private set { _netAPayer = value; OnPropertyChanged(); }
        }

        // ✅ Compat v1 : attendu par ton code
        public string TotalTtcInWords => $"{ToFrenchWordsMoney(TotalTtc)} F CFA";

        public Invoice()
        {
            Lines.CollectionChanged += Lines_CollectionChanged;
        }

        private void Lines_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (var item in e.NewItems.OfType<INotifyPropertyChanged>())
                    item.PropertyChanged += Line_PropertyChanged;

            if (e.OldItems != null)
                foreach (var item in e.OldItems.OfType<INotifyPropertyChanged>())
                    item.PropertyChanged -= Line_PropertyChanged;

            Recalculate();
        }

        private void Line_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            Recalculate();
        }

        // ✅ Compat : attendu Invoice.Recalculate()
        public void Recalculate()
        {
            var ht = Lines.Sum(l => l.TotalHt);

            TotalHt = ht;
            TotalTva = Math.Round(ht * TvaRate, 0, MidpointRounding.AwayFromZero);
            TotalIr = Math.Round(ht * IrRate, 0, MidpointRounding.AwayFromZero);

            // TTC = HT * (1 + TVA)
            TotalTtc = Math.Round(ht * (1m + TvaRate), 0, MidpointRounding.AwayFromZero);

            // Net à percevoir = HT - (HT * IR)
            NetAPayer = Math.Round(ht - (ht * IrRate), 0, MidpointRounding.AwayFromZero);

            OnPropertyChanged(nameof(TotalHt));
            OnPropertyChanged(nameof(TotalTva));
            OnPropertyChanged(nameof(TotalIr));
            OnPropertyChanged(nameof(TotalTtc));
            OnPropertyChanged(nameof(NetAPayer));
            OnPropertyChanged(nameof(TotalTtcInWords));
        }

        // ===== Words (robuste) =====
        private static string ToFrenchWordsMoney(decimal amount)
        {
            // On ne gère que l'entier (F CFA) pour l'instant
            long value = (long)Math.Round(amount, 0, MidpointRounding.AwayFromZero);
            if (value < 0) return "moins " + ToFrenchWordsMoney(-value);

            // 1) essaie d'utiliser ta classe existante si elle existe (FrenchNumberToWords.cs)
            //    sans imposer sa signature (reflection).
            var fromReflection = TryCallExistingFrenchConverter(value);
            if (!string.IsNullOrWhiteSpace(fromReflection))
                return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(fromReflection.Trim());

            // 2) fallback interne (simple mais fiable)
            var fallback = FrenchNumberFallback(value);
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(fallback.Trim());
        }

        private static string? TryCallExistingFrenchConverter(long value)
        {
            try
            {
                // Essaye plusieurs namespaces probables
                string[] typeNames =
                {
                    "FacturationMercuriale.Services.FrenchNumberToWords",
                    "FacturationMercuriale.FrenchNumberToWords"
                };

                foreach (var tn in typeNames)
                {
                    var t = Type.GetType(tn);
                    if (t == null)
                    {
                        // Cherche dans l'assembly courant
                        t = Assembly.GetExecutingAssembly().GetTypes().FirstOrDefault(x => x.FullName == tn);
                    }
                    if (t == null) continue;

                    // Méthodes possibles : Convert(long/int), ToWords(long/int)
                    var m = t.GetMethods(BindingFlags.Public | BindingFlags.Static)
                             .FirstOrDefault(mi =>
                                 (mi.Name.Equals("Convert", StringComparison.OrdinalIgnoreCase) ||
                                  mi.Name.Equals("ToWords", StringComparison.OrdinalIgnoreCase)) &&
                                 mi.GetParameters().Length == 1);

                    if (m == null) continue;

                    var p = m.GetParameters()[0].ParameterType;
                    object arg = p == typeof(int) ? (object)checked((int)value) : value;

                    var res = m.Invoke(null, new[] { arg })?.ToString();
                    return res;
                }
            }
            catch
            {
                // ignore
            }

            return null;
        }

        // Fallback: 0..999 999 999 999 (suffisant pour facturation)
        private static string FrenchNumberFallback(long n)
        {
            if (n == 0) return "zéro";

            string Unit(long x)
            {
                return x switch
                {
                    0 => "zéro",
                    1 => "un",
                    2 => "deux",
                    3 => "trois",
                    4 => "quatre",
                    5 => "cinq",
                    6 => "six",
                    7 => "sept",
                    8 => "huit",
                    9 => "neuf",
                    10 => "dix",
                    11 => "onze",
                    12 => "douze",
                    13 => "treize",
                    14 => "quatorze",
                    15 => "quinze",
                    16 => "seize",
                    _ => ""
                };
            }

            string Tens(long x)
            {
                if (x < 17) return Unit(x);
                if (x < 20) return "dix-" + Unit(x - 10);

                long d = x / 10;
                long u = x % 10;

                string baseTen = d switch
                {
                    2 => "vingt",
                    3 => "trente",
                    4 => "quarante",
                    5 => "cinquante",
                    6 => "soixante",
                    7 => "soixante",
                    8 => "quatre-vingt",
                    9 => "quatre-vingt",
                    _ => ""
                };

                if (d == 7 || d == 9)
                {
                    // 70 = soixante-dix, 90 = quatre-vingt-dix
                    var rest = 10 + u;
                    if (rest == 11) return baseTen + " et onze";
                    return baseTen + "-" + Tens(rest);
                }

                if (u == 0)
                {
                    // 80 prend un "s" : quatre-vingts
                    if (d == 8) return baseTen + "s";
                    return baseTen;
                }

                if (u == 1 && (d == 2 || d == 3 || d == 4 || d == 5 || d == 6))
                    return baseTen + " et un";

                return baseTen + "-" + Unit(u);
            }

            string UnderThousand(long x)
            {
                if (x < 100) return Tens(x);

                long h = x / 100;
                long r = x % 100;

                string hundred = h == 1 ? "cent" : Unit(h) + " cent";
                if (r == 0)
                {
                    // "deux cents" prend un s
                    if (h > 1) hundred += "s";
                    return hundred;
                }
                return hundred + " " + Tens(r);
            }

            string Group(long x, string labelSing, string labelPlur, bool omitOne = false)
            {
                if (x == 0) return "";
                if (x == 1 && omitOne) return labelSing;
                var words = UnderThousand(x);
                return words + " " + (x > 1 ? labelPlur : labelSing);
            }

            long billions = n / 1_000_000_000;
            n %= 1_000_000_000;
            long millions = n / 1_000_000;
            n %= 1_000_000;
            long thousands = n / 1000;
            long rest = n % 1000;

            var parts = new System.Collections.Generic.List<string>();

            if (billions > 0) parts.Add(Group(billions, "milliard", "milliards"));
            if (millions > 0) parts.Add(Group(millions, "million", "millions"));
            if (thousands > 0)
            {
                // 1 000 = "mille" (pas "un mille")
                parts.Add(Group(thousands, "mille", "mille", omitOne: true));
            }
            if (rest > 0) parts.Add(UnderThousand(rest));

            return string.Join(" ", parts);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
