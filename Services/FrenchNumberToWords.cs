using System;
using System.Text;

namespace FacturationMercuriale.Services
{
    public static class FrenchNumberToWords
    {
        public static string ToFcfaWords(decimal amount)
        {
            // On convertit le TTC (arrondi) en entier FCFA
            var n = (long)Math.Round(amount, 0, MidpointRounding.AwayFromZero);
            if (n == 0) return "ZÉRO FCFA";

            var words = ToFrench(n).Trim();
            return $"{words} FCFA".ToUpperInvariant();
        }

        public static string ToFrench(long number)
        {
            if (number < 0) return "MOINS " + ToFrench(-number);
            if (number == 0) return "zéro";

            var sb = new StringBuilder();

            void AppendWithSpace(string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return;
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(s);
            }

            // milliards, millions, mille
            if (number >= 1_000_000_000)
            {
                var billions = number / 1_000_000_000;
                AppendWithSpace(ToFrench(billions) + (billions > 1 ? " milliards" : " milliard"));
                number %= 1_000_000_000;
            }

            if (number >= 1_000_000)
            {
                var millions = number / 1_000_000;
                AppendWithSpace(ToFrench(millions) + (millions > 1 ? " millions" : " million"));
                number %= 1_000_000;
            }

            if (number >= 1000)
            {
                var thousands = number / 1000;
                if (thousands == 1) AppendWithSpace("mille");
                else AppendWithSpace(ToFrench(thousands) + " mille");
                number %= 1000;
            }

            if (number > 0)
            {
                AppendWithSpace(ConvertBelowThousand((int)number));
            }

            return sb.ToString();
        }

        private static string ConvertBelowThousand(int n)
        {
            if (n == 0) return "";

            var hundreds = n / 100;
            var rest = n % 100;

            var sb = new StringBuilder();

            if (hundreds > 0)
            {
                if (hundreds == 1) sb.Append("cent");
                else sb.Append(Unit(hundreds) + " cent");

                // plural "cents" uniquement si exact multiple de 100
                if (rest == 0 && hundreds > 1) sb.Append("s");
            }

            if (rest > 0)
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(ConvertBelowHundred(rest, hundreds));
            }

            return sb.ToString();
        }

        private static string ConvertBelowHundred(int n, int hundredsPart)
        {
            if (n < 20) return Small(n);

            int tens = n / 10;
            int unit = n % 10;

            // 70 = 60 + 10..19 ; 90 = 80 + 10..19
            if (tens == 7)
            {
                // 70..79
                return "soixante-" + ConvertBelowHundred(10 + unit, hundredsPart);
            }

            if (tens == 9)
            {
                // 90..99
                return "quatre-vingt-" + ConvertBelowHundred(10 + unit, hundredsPart);
            }

            if (tens == 8)
            {
                // 80..89
                var baseWord = "quatre-vingt";
                if (unit == 0) return baseWord + "s"; // quatre-vingts
                return baseWord + "-" + Unit(unit);
            }

            // 20..69
            var tenWord = Tens(tens);

            if (unit == 0) return tenWord;

            // règle du "et un" : 21,31,41,51,61
            if (unit == 1 && (tens == 2 || tens == 3 || tens == 4 || tens == 5 || tens == 6))
                return tenWord + " et un";

            return tenWord + "-" + Unit(unit);
        }

        private static string Small(int n) => n switch
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
            17 => "dix-sept",
            18 => "dix-huit",
            19 => "dix-neuf",
            _ => ""
        };

        private static string Unit(int n) => Small(n);

        private static string Tens(int t) => t switch
        {
            2 => "vingt",
            3 => "trente",
            4 => "quarante",
            5 => "cinquante",
            6 => "soixante",
            _ => ""
        };
    }
}
