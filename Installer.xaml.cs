using Pandemonium_Classic_Mod_Manager.Properties;
using SQLiteDataBase;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Schema;

namespace Pandemonium_Classic_Mod_Manager
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Installer : Window
    {
        public List<string> FileList = new(), LocalFileList = new();

        public Mod? _mod;

        public int installedFiles, totalFiles;
        public bool installed, uninstall;

        public Installer()
        {
            InitializeComponent();
        }

        private void OnActivated(object sender, EventArgs e)
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Run();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        public async Task Run()
        {
            totalFiles += FileList.Count;

            if (!uninstall) await InstallFiles();
            else await UninstallFiles();

            this.Close();
        }

        public async Task InstallFiles()
        {
            PCUE_Database database = PCUE_ModManager.instance.database;

            foreach (var file in FileList)
            {
                int i = file.IndexOf("StreamingAssets");
                if (i == -1)
                {
                    // If the indicated substring isn't found, ask whether to continue or exit the installation
                    var errMsgResult = MessageBox.Show("ERROR: substring '\\StreamingAssets' not found in file: " + file, "FilePathError",
                        MessageBoxButton.OKCancel, MessageBoxImage.Error);
                    if (errMsgResult == MessageBoxResult.Cancel)
                        return;
                }
                else
                {
                    string localPath = file.Remove(0, i);
                    LocalFileList.Add(localPath);
                }
            }


            string? conflict = database.Files_CheckForConflicts(LocalFileList.ToArray());
            if (conflict != null)
            {
                var result = MessageBox.Show("Warning! " + conflict + " has conflicting files! Continue?", "Conflict", 
                                MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Cancel)
                    return;
            }

            for (int i = 0; i < FileList.Count; i++)
            {
                var file = FileList[i];
                var localPath = LocalFileList[i];

                string newPath = System.IO.Path.Combine(Settings.Default.gameDataFolder, localPath);
                if (!File.Exists(newPath))
                {
                    MessageBox.Show("The image file: '" + file + "' does not exist!", "FilePathError",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (Settings.Default.backup)
                {
                    string bakPath = System.IO.Path.Combine(Settings.Default.backupFolder, localPath);
                    Directory.CreateDirectory(bakPath.Remove(bakPath.LastIndexOf("\\")));
                    if (!File.Exists(bakPath))
                        File.Copy(newPath, bakPath, false);
                }

                Directory.CreateDirectory(newPath.Remove(newPath.LastIndexOf("\\")));
                File.Copy(file, newPath, true);

                installedFiles++;

                fileCounter.Content = installedFiles + "/" + totalFiles;
                filePathDisplay.Content = "copying '" + localPath + "'";
                fileProgressBar.Value = (((float)installedFiles / (float)totalFiles) * 100);

                await Task.Delay(1);
            }
            installed = true;
        }

        public async Task UninstallFiles()
        {
            PCUE_Database database = PCUE_ModManager.instance.database;

            if (_mod != null && _mod.Installed != "" && _mod.BackUp)
            {
                string[] fileList = database.Files_TakeRecords(_mod.Name);
                int index = 0, total = fileList.Count();

                foreach (var file in fileList)
                {
                    var oldPath = System.IO.Path.Combine(Settings.Default.backupFolder, file);
                    var newPath = System.IO.Path.Combine(Settings.Default.gameDataFolder, file);

                    if (!File.Exists(oldPath))
                    {
                        var result = MessageBox.Show("File Not Found: '" + oldPath + "'", "File Not Found", 
                            MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                        if (result == MessageBoxResult.Cancel) return;
                    }

                    File.Move(oldPath, newPath, true);

                    index++;

                    fileCounter.Content = index + "/" + total;
                    filePathDisplay.Content = "copying '" + file + "'";
                    fileProgressBar.Value = (((float)index / (float)total) * 100);

                    await Task.Delay(1);
                }

                _mod.Installed = "";
                _mod.BackUp = false;
                database.Mods_SetInstalled(_mod, false);
            }
        }
    }
}
