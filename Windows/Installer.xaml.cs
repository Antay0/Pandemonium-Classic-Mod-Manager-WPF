using Pandemonium_Classic_Mod_Manager.Properties;
using Pandemonium_Classic_Mod_Manager.SQLiteDataBase;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Pandemonium_Classic_Mod_Manager.Utilities;

namespace Pandemonium_Classic_Mod_Manager
{
    /// <summary>
    /// Interaction logic for PCUEMOD_V2.xaml
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

            LocalFileList = GeneralUtilities.GetLocalFileList(FileList);
            if (LocalFileList.Count != FileList.Count)
                return;

            string? conflict = database.Files_CheckForConflicts(LocalFileList.ToArray());
            if (conflict != null)
            {
                if (!PCUEDebug.ShowErrorOKCancel("Warning! " + conflict + " has conflicting files! Continue?"))
                    return;
            }

            for (int i = 0; i < FileList.Count; i++)
            {
                var file = FileList[i];
                var localPath = LocalFileList[i];

                string newPath = System.IO.Path.Combine(Settings.Default.gameDataFolder, localPath);
                if (!File.Exists(newPath))
                {
                    PCUEDebug.ShowError("The image file: '" + file + "' does not exist!");
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
                string[] fileList = database.Files_TakeRecords(_mod.Name, true);
                var stringList = database.Strings_GetRecords(_mod.Name, true);
                int index = 0, total = fileList.Count();

                foreach (var file in fileList)
                {
                    var oldPath = System.IO.Path.Combine(Settings.Default.backupFolder, file);
                    var newPath = System.IO.Path.Combine(Settings.Default.gameDataFolder, file);

                    if (!File.Exists(oldPath))
                    {
                        if (!PCUEDebug.ShowErrorOKCancel("File Not Found: '" + oldPath + "'")) 
                            return;
                    }

                    File.Move(oldPath, newPath, true);

                    index++;

                    fileCounter.Content = index + "/" + total;
                    filePathDisplay.Content = "copying '" + file + "'";
                    fileProgressBar.Value = (((float)index / (float)total) * 100);

                    await Task.Delay(1);
                }
                foreach (var section in stringList)
                {
                    if (section.Value.Values.Count > 0)
                    {
                        foreach (var entry in section.Value)
                        {
                            PCUE_ModManager.instance.ModdedStrings[section.Key].Remove(entry.Key);
                        }
                    }
                    else
                    {
                        PCUE_ModManager.instance.ModdedStrings.Remove(section.Key);
                    }
                }
                ModStrings.WriteModdedTextFile(PCUE_ModManager.instance.ModdedStrings);

                _mod.Installed = "";
                _mod.BackUp = false;
                database.Mods_SetInstalled(_mod, false);
            }
        }
    }
}
