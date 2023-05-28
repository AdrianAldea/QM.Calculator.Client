using Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Windows;

namespace Calculator {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            DatabaseFacade databaseFacade = new DatabaseFacade(new DatabaseContext());
            databaseFacade.EnsureCreatedAsync();
        }
    }
}
