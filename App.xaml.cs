using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Pandemonium_Classic_Mod_Manager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string ManagerVersion = "v1.0.5";

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MainWindow = new PCUE_ModManager();
            MainWindow.Title = "Pandemonium Classic - ModV1 Manager " + ManagerVersion;
            MainWindow.Show();

        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            // Cull connections
            PCUE_ModManager.instance.database.dbConnection.Dispose();
            //PCUE_ModManager.instance.mega.Logout();

            Pandemonium_Classic_Mod_Manager.Properties.Settings.Default.Save();
        }
    }
}