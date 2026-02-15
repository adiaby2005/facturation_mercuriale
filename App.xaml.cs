using System.Windows;
using QuestPDF.Infrastructure;

namespace FacturationMercuriale
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // ✅ Obligatoire sinon QuestPDF affiche l'écran de licence
            QuestPDF.Settings.License = LicenseType.Community;

            base.OnStartup(e);
        }
    }
}
