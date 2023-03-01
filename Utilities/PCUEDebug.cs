using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Pandemonium_Classic_Mod_Manager.Utilities
{
    public class PCUEDebug
    {
        public static void ShowError(Exception e, string message = "")
        {
            MessageBox.Show((message != "" ? message + "\n\n" : "")
                + e.Message + "\n --------- \n" + e.StackTrace, "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static void ShowError(string message)
        {
            MessageBox.Show(message, "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public static bool ShowErrorOKCancel(Exception e, string message = "")
        {
            var result = MessageBox.Show((message != "" ? message + "\n\n" : "")
                + e.Message + "\n --------- \n" + e.StackTrace, "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            return result == MessageBoxResult.OK ? true : false;
        }

        public static bool ShowErrorOKCancel(string message)
        {
            var result = MessageBox.Show(message, "Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            return result == MessageBoxResult.OK ? true : false;
        }
    }
}
