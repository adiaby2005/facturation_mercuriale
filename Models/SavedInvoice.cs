using System;
using System.Collections.Generic;

namespace FacturationMercuriale.Models
{
    public class SavedInvoice
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public string Doit { get; set; } = string.Empty;
        public string Objet { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty;

        public double TvaRate { get; set; }
        public double IrRate { get; set; }

        public List<SavedInvoiceLine> Lines { get; set; } = new List<SavedInvoiceLine>();

        public DateTime LastSavedAt { get; set; } = DateTime.Now;
    }

    public class SavedInvoiceLine
    {
        public string RefArticle { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public double Quantity { get; set; }
        public double UnitPriceHt { get; set; }
        public string Conditionnement { get; set; } = string.Empty;
        public string Millesime { get; set; } = string.Empty;
    }
}