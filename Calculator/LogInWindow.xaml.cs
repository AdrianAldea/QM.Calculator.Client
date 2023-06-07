using QM.Inventory.TunnelsClient;
using System.Windows;
using Tunnels.Core.Models;

namespace Calculator {
    /// <summary>
    /// Interaction logic for LogInWindow.xaml
    /// </summary>
    public partial class LogInWindow : Window {
        public User? User;
        public LogInWindow(User user) {
            this.User = user;
            InitializeComponent();
        }

        private async void btnLogin_Click(object sender, RoutedEventArgs e) {
            User = await TunnelsClient.ValidateUsernameAndPassword(tbUsername.Text, pbPassword.Password);
            if (User?.Id == 0) {
                MessageBox.Show("Nu s-a gasit userul sau datele sunt gresite !");
            }
            else {
                tbUsername.Text = string.Empty;
                pbPassword.Password = string.Empty;
                Close();               
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            User = null;
            Close();
        }
    }
}
