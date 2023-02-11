using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.IO;
using System.Collections.ObjectModel;
using System.Windows.Media.TextFormatting;
using Pandemonium_Classic_Mod_Manager;
using Pandemonium_Classic_Mod_Manager.Properties;
using Pandemonium_Classic_Mod_Manager.SQLiteDataBase;
using System.Diagnostics;

namespace Pandemonium_Classic_Mod_Manager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class PCUE_ModManager : Window
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public static PCUE_ModManager instance;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public ObservableCollection<Mod> Mods { get; set; } = new();

        public PCUE_Database database;

        public PCUE_ModManager()
        {
            instance = this;
            InitializeComponent();

            // Initialize settings
            GameFolder_Update(Settings.Default.gameFolder);

            if (string.IsNullOrEmpty(Settings.Default.backupFolder))
                Settings.Default.backupFolder = AppDomain.CurrentDomain.BaseDirectory + "backup";
            if (string.IsNullOrEmpty(Settings.Default.modsFolder))
                Settings.Default.modsFolder = AppDomain.CurrentDomain.BaseDirectory + "mods";

            if (!Directory.Exists(Settings.Default.backupFolder))
                Directory.CreateDirectory(Settings.Default.backupFolder);
            if (!Directory.Exists(Settings.Default.modsFolder))
                Directory.CreateDirectory(Settings.Default.modsFolder);

            backupFolder_TextBox.Text = Settings.Default.backupFolder;
            modsFolder_TextBox.Text = Settings.Default.modsFolder;
            gameFolder_TextBox.Text = Settings.Default.gameFolder;

            database = new PCUE_Database();

            /*ModV2 mod = new()
            {
                Name = "Vampire MILFs - ModV2 Test",
                Author = "Antay",
                Description = "STUPID SHIT BOOO HOOOOB",
                MainPackage = "0Main",
                Steps = new()
                {
                    new(){name = "Talia Selection", onlyOne = true, required = true,
                        options = new()
                        {
                            new Option(){ name = "Confident", description = "Fuck", folder = "Shit1", image = "confident.png"},
                            new Option(){ name = "Horny", description = "Shit", folder = "Butt2", image = "horny.png"}
                        }
                        },
                    new(){name = "Lotta Selection", onlyOne = true, required = true,
                        options = new()
                        {
                            new Option(){ name = "Confident", description = "Fuck", folder = "Shit3", image = "confident.png"},
                            new Option(){ name = "Wanker", description = "eat", folder = "Dick4", image = "fork.png"},
                            new Option(){ name = "Horny", description = "Shit", folder = "Butt5", image = "horny.png"}
                        }
                    }
                }
            };
            ModV2.WriteModV2XML(mod); */

            ModList_Update();
        }

        //
        // Main Page //
        //

        private void BackupFolder_FileBrowser_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new FolderBrowserDialog();
            {
                DialogResult result = fbd.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    backupFolder_TextBox.Text = fbd.SelectedPath;
                }
            }
        }

        private void ModsFolder_FileBrowser_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new FolderBrowserDialog();
            {
                DialogResult result = fbd.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    modsFolder_TextBox.Text = fbd.SelectedPath;
                    ModList_Update();
                }
            }
        }

        private void GameFolder_FileBrowser_Click(object sender, RoutedEventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    gameFolder_TextBox.Text = fbd.SelectedPath;
                    GameFolder_Update(fbd.SelectedPath);
                }
            }
        }

        private void ModsFolder_TextBox_OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                ModList_Update();
        }

        private void GameData_TextBox_OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                GameFolder_Update(gameFolder_TextBox.Text);
        }

        private void ModList_Update()
        {
            Settings.Default.modsFolder = modsFolder_TextBox.Text;

            modList_View.ItemsSource = null;
            Mods.Clear();

            string dir = Settings.Default.modsFolder;
            if (Directory.Exists(dir))
            {
                var ModV1Files = Directory.GetFiles(dir, "mod.xml", SearchOption.AllDirectories);
                var ModV2Files = Directory.GetFiles(dir, "modV2.xml", SearchOption.AllDirectories);
                foreach (string file in ModV1Files)
                {
                    ModV1 mod = new(file);
                    if (mod.Name != null) Mods.Add(mod);
                }
                foreach (string file in ModV2Files)
                {
                    ModV2? mod = ModV2.LoadModXML(file);
                    if (mod != null) Mods.Add(mod);
                }
            }
            Mods = new (Mods.OrderBy(t => t.Name));
            database.Mods_UpdateRecords();
            modList_View.ItemsSource = Mods;
            UpdatePreview();
        }

        private void GameFolder_Update(string folder)
        {
            Settings.Default.gameFolder = folder;
            Settings.Default.gameDataFolder = System.IO.Path.Combine(folder + "\\Pandemonium Classic - Unity Edition_Data");

            if (!System.IO.File.Exists(folder + "\\Pandemonium Classic - Unity Edition.exe"))
            {
                System.Windows.MessageBox.Show("'Pandemonium Classic - Unity Edition.exe' not found! Make sure your game directory is set to the right path!",
                    "Game Directory Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ModList_View_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (modList_View.SelectedIndex != -1
                && modList_View.SelectedIndex < modList_View.Items.Count
                && modList_View.Items.Count != 0)
            {
                Mod mod = (Mod)modList_View.SelectedItem;
                UpdatePreview(mod);
            }
        }

        private void UpdatePreview(Mod? mod = null)
        {
            modAuthor_TextBox.Content = "";
            modDescription_TextBox.Text = "";
            modPreviewBox.Source = null;
            if (mod != null)
            {
                modAuthor_TextBox.Content = mod.Name + (mod.Author != "" ? " : By " + mod.Author : "");
                modDescription_TextBox.Text = mod.Description;
                modPreviewBox.Source = mod.thumbnail;
            }
            else modList_View.UnselectAll();
        }

        public void Backup_CheckBox_Changed()
        {
            //Properties.Settings.Default.backup = (bool)backup_CheckBox.IsChecked;
        }

        private void InstallButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(Settings.Default.modsFolder)
                && !string.IsNullOrEmpty(Settings.Default.gameDataFolder))
            {
                foreach (Mod mod in modList_View.SelectedItems)
                {
                    if (mod != null)
                    {
                        List<string> fileList = new();

                        if (mod is ModV1) // =========== PCUEMOD_V1
                        {
                            string mod_xml = ((ModV1)mod).xmlPath;
                            if (string.IsNullOrEmpty(mod_xml))
                            {
                                System.Windows.MessageBox.Show("ERROR: string 'toInstall' is NULL or empty", "FilePathError",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                            else if (!System.IO.File.Exists(mod_xml))
                            {
                                System.Windows.MessageBox.Show("ERROR: 'mod.xml' is missing", "MissingXMLError",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            PCUEMOD_V1 pcuemod = new((ModV1)mod);
                            if (pcuemod.showWindow)
                            {
                                if (!pcuemod.error)
                                {
                                    pcuemod.ShowDialog();
                                    if (pcuemod.earlyExit)
                                        return;
                                }
                                else return;
                            }
                            fileList = pcuemod.fileList;
                        }
                        else if (mod is ModV2) // =========== PCUEMOD_V2
                        {
                            PCUEMOD_V2 pcuemod = new((ModV2)mod);

                            if (pcuemod.showWindow)
                            {
                                if (!pcuemod.error)
                                {
                                    pcuemod.ShowDialog();
                                    if (pcuemod.earlyExit)
                                        return;
                                }
                                else return;
                            }
                            fileList = pcuemod.fileList;
                        }
                        Installer installer = new();
                        installer.FileList = fileList;
                        installer.ShowDialog();
                        if (installer.installed)
                        {
                            mod.Installed = "+";
                            mod.BackUp = Settings.Default.backup;

                            database.Mods_SetInstalled(mod, true, mod.BackUp);
                            database.Files_AddRecords(mod.Name, installer.LocalFileList.ToArray());
                        }
                        else System.Windows.MessageBox.Show("Installation could not be completed!", "", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                }
                UpdatePreview();
            }
            else
            {
                System.Windows.MessageBox.Show("Game or Mod folders are not declared.", "Empty Directory",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        private void UninstallButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(Settings.Default.gameDataFolder)
                && !string.IsNullOrEmpty(Settings.Default.backupFolder))
            {
                foreach (Mod mod in modList_View.SelectedItems)
                {
                    if (mod != null && mod.Installed != "")
                    {
                        Installer installer = new() { uninstall = true, _mod = mod };
                        installer.ShowDialog();
                    }
                }
            }
            ModList_Update();
        }

        private void RefreshButton_OnClick(object sender, RoutedEventArgs e)
        {
            ModList_Update();
        }

        private void uninstallAllButton_Click(object sender, RoutedEventArgs e)
        {
            var result = System.Windows.MessageBox.Show("Uninstall all mods?", "Confirmation",
                            MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.OK)
            {
                foreach (Mod mod in Mods)
                {
                    if (mod != null && mod.Installed != "")
                    {
                        Installer installer = new() { uninstall = true, _mod = mod };
                        installer.ShowDialog();
                    }
                }
            }
        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(Settings.Default.gameFolder + "\\Pandemonium Classic - Unity Edition.exe");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}