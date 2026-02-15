using System.Windows;

namespace FacturationMercuriale.Views
{
    public partial class ActivationWindow : Window
    {
        public string LicenseKey { get; private set; } = string.Empty;

        public ActivationWindow(string machineId)
        {
            InitializeComponent();
            TxtMachineId.Text = machineId;
        }

        private void Activate_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(TxtLicenseKey.Text))
            {
                LicenseKey = TxtLicenseKey.Text.Trim();
                this.DialogResult = true;
            }
            else
            {
                MessageBox.Show("Veuillez entrer une clé valide.", "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}