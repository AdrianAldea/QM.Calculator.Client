using QM.Inventory.TunnelsClient;
using System.Diagnostics;
using System.Windows;
using Tunnels.Core.Models;

namespace Calculator {
    /// <summary>
    /// Interaction logic for LogInWindow.xaml
    /// </summary>
    public partial class LogInWindow : Window {
        public User? User;
        public LogInWindow() {
            InitializeComponent();
            Process.Start(new ProcessStartInfo { FileName = @"C:\windows\system32\osk.exe", UseShellExecute = true });
        }

        private async void btnLogin_Click(object sender, RoutedEventArgs e) {
            User = await TunnelsClient.ValidateUsernameAndPassword(tbUsername.Text, pbPassword.Password);
            if (User == null) {
                MessageBox.Show("User not found !");
            }
            else {
                Hide();
                MainWindow mainWindow = new MainWindow(User);
                mainWindow.ShowDialog();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            Close();
        }
    }
}
