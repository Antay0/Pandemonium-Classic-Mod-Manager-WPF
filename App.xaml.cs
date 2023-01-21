using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Pandemonium_Classic___Mod_Manager__WPF_
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string ManagerVersion = "V1.0.5";

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow = new PCUE_ModManager();
            MainWindow.Title = "Pandemonium Classic - Mod Manager " + ManagerVersion;
            MainWindow.Show();

        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            // Cull connections
            PCUE_ModManager.instance.database.dbConnection.Dispose();
            //PCUE_ModManager.instance.mega.Logout();

            Pandemonium_Classic___Mod_Manager__WPF_.Properties.Settings.Default.Save();
        }
    }
}