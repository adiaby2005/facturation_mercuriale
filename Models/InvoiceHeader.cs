using System;

namespace FacturationMercuriale.Models
{
    public class InvoiceHeader
    {
        public string InvoiceNumber { get; set; } = "";
        public DateTime InvoiceDate { get; set; } = DateTime.Today;

        public string Doit { get; set; } = "";
        public string Objet { get; set; } = "";

        public string Region { get; set; } = "";
        public string City { get; set; } = "";

        // signataire affiché en bas du PDF
        public string Direction { get; set; } = "";

        // taux fiscaux
        public decimal TvaRate { get; set; } = 19.25m;
        public decimal IrRate { get; set; } = 5.5m;
    }
}
