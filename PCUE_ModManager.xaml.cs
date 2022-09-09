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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.IO;
using System.Collections.ObjectModel;
using System.Windows.Media.TextFormatting;
using System.Data.SQLite;
using Pandemonium_Classic___Mod_Manager__WPF_;
using Pandemonium_Classic___Mod_Manager__WPF_.Properties;
using SQLiteDataBase;
using System.Drawing.Text;
using System.ComponentModel;
using System.Data.Entity;
using static System.Net.WebRequestMethods;

namespace Pandemonium_Classic___Mod_Manager__WPF_
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
            modsFolder_TextBox.Text = Settings.Default.modsFolder;
            gameData_TextBox.Text = Settings.Default.gameDataFolder;

            if (string.IsNullOrEmpty(Settings.Default.backupFolder))
            {
                string backupDir = AppDomain.CurrentDomain.BaseDirectory + "backup";
                
                Settings.Default.backupFolder = backupDir;
            }
            if (!Directory.Exists(Settings.Default.backupFolder))
                Directory.CreateDirectory(Settings.Default.backupFolder);

            database = new PCUE_Database();

            ModsFolder_Update();
        }

        private void ModsFolder_FileBrowser_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new FolderBrowserDialog();
            {
                DialogResult result = fbd.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    modsFolder_TextBox.Text = fbd.SelectedPath;
                    ModsFolder_Update();
                }
            }
        }

        private void GameData_FileBrowser_Click(object sender, RoutedEventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    gameData_TextBox.Text = fbd.SelectedPath;
                    GameData_Update();
                }
            }
        }

        private void ModsFolder_TextBox_OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                ModsFolder_Update();
        }

        private void GameData_TextBox_OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                GameData_Update();
        }

        private void ModsFolder_Update()
        {
            Settings.Default.modsFolder = modsFolder_TextBox.Text;

            Mods.Clear();

            string dir = Settings.Default.modsFolder;
            if (Directory.Exists(dir))
            {
                var Files = Directory.GetFiles(dir, "mod.xml", SearchOption.AllDirectories);

                foreach (string file in Files)
                {
                    Mod mod = new(file);
                    if (mod.Name != null)
                        Mods.Add(mod);
                }
            }

            database.Mods_UpdateRecords();
        }

        private void GameData_Update()
        {
            Settings.Default.gameDataFolder = gameData_TextBox.Text;
        }

        private void ModList_View_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            modDescription_TextBox.Text = string.Empty;
            modPreviewBox.Source = null;
            if (modList_View.SelectedIndex != -1
                && modList_View.SelectedIndex < modList_View.Items.Count
                && modList_View.Items.Count != 0)
            {
                Mod mod = (Mod)modList_View.SelectedItem;

                modDescription_TextBox.Text = mod.Description;
                modPreviewBox.Source = mod.preview;
            }
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
                var mod = (Mod)modList_View.SelectedItem;
                if (mod != null)
                {
                    string mod_xml = mod.xmlPath;
                    if (string.IsNullOrEmpty(mod_xml))
                    {
                        var result = System.Windows.MessageBox.Show("ERROR: string 'toInstall' is NULL or empty", "FilePathError",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    PCUEMOD_V1 installer = new(mod);
                    installer.ShowDialog();
                    if (installer.installed)
                    {
                        installer.Mod.Installed = "+";
                        database.Mods_SetInstalled(installer.Mod);
                        database.Files_AddRecords(installer.Mod.Name, installer.fileList.ToArray());
                    }
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Game Data or Mod folders are not declared.", "Empty Directory",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        private void UninstallButton_OnClick(object sender, RoutedEventArgs e)
        {

            if (!string.IsNullOrEmpty(Settings.Default.gameDataFolder)
                && !string.IsNullOrEmpty(Settings.Default.backupFolder))
            {
                var mod = (Mod)modList_View.SelectedItem;
                if (mod != null && mod.Installed != "")
                {
                    var result = System.Windows.MessageBox.Show("Uninstall '" + mod.Name + "'?", "Confirmation",
                        MessageBoxButton.OKCancel, MessageBoxImage.Question);
                    if (result == MessageBoxResult.OK)
                    {
                        string[] fileList = database.Files_TakeRecords(mod.Name);
                        foreach (var file in fileList)
                        {
                            int i = file.IndexOf("StreamingAssets");
                            string localPath = file.Remove(0, i);
                            var oldPath = System.IO.Path.Combine(Settings.Default.backupFolder, localPath);
                            var newPath = System.IO.Path.Combine(Settings.Default.gameDataFolder, localPath);

                            System.IO.File.Move(oldPath, newPath, true);
                        }
                        mod.Installed = "";
                        System.Windows.MessageBox.Show("Done!", "Manager Notification", MessageBoxButton.OK);
                    }
                }
            }
        }
    }


    /// <summary>
    /// Acts as a representation of a PCUEMOD
    /// </summary>

    public class Mod
    {
        public string Name { get; set; } = "";
        public string? Description;

        public string Installed { get { return _installed; } set { _installed = value; PCUE_ModManager.instance.modList_View.Items.Refresh(); } }
        public string _installed = "";

        public int installerVersion = 0;

        public BitmapImage? preview;

        public string FolderPath = ""; // Path to base folder that contains all other components of the mod.
        public string xmlPath = ""; // Path to mod.xml

        public Mod(string? filePath)
        {
            if (filePath != null)
            {
                FolderPath = filePath.Replace("PCUEMOD\\mod.xml", "");
                xmlPath = filePath;

                XmlReader reader = XmlReader.Create(filePath);

                reader.ReadToFollowing("mod");

                var tmp = reader.GetAttribute("name");
                if (tmp != null)
                    Name = tmp;
                else Name = "null";

                string? iv = reader.GetAttribute("installerversion");
                if (!string.IsNullOrEmpty(iv))
                    installerVersion = int.Parse(iv);

                reader.ReadToDescendant("description");
                Description = reader.ReadElementContentAsString();

                var uri = new Uri(System.IO.Path.Combine(FolderPath, "PCUEMOD\\preview.png"));
                preview = new(uri);
            }
        }
    }
}

namespace SQLiteDataBase
{
    public class PCUE_Database
    {
        public SQLiteConnection dbConnection;
        SQLiteCommand command;
        SQLiteDataReader? reader;
        string sqlCommand;
        string dbFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mods.db");

        public PCUE_Database()
        {
            //// these are just to get the console to shut the fuck up
            dbConnection = new();
            command = new();
            sqlCommand = string.Empty;
            ////

            if (CreateDbFile())
            {
                CreateDbConnection();
                Mods_CreateTable();
                Mods_FillTable();
                Files_CreateTable();
                Files_FillTable();
            }
            else
            {
                CreateDbConnection();
                Mods_UpdateRecords();
            }
            
        }

        public bool CreateDbFile()
        {
            if (!System.IO.File.Exists(dbFilePath))
            {
                SQLiteConnection.CreateFile(dbFilePath);
                return true;
            }
            return false;
        }

        public string CreateDbConnection()
        {
            string strCon = string.Format("Data Source={0};", dbFilePath);
            dbConnection = new SQLiteConnection(strCon);
            dbConnection.Open();
            command = dbConnection.CreateCommand();
            return strCon;
        }

        public bool CheckIfExist(string tableName)
        {
            command.CommandText = "SELECT name FROM sqlite_master WHERE name='" + tableName + "'";
            var result = command.ExecuteScalar();

            return result != null && result.ToString() == tableName;
        }

        public void ExecuteQuery(string sqlCommand)
        {
            SQLiteCommand triggerCommand = dbConnection.CreateCommand();
            triggerCommand.CommandText = sqlCommand;
            triggerCommand.ExecuteNonQuery();
        }

        public bool CheckIfTableContainsData(string tableName)
        {
            command.CommandText = "SELECT COUNT(*) FROM " + tableName;
            var result = command.ExecuteScalar();

            return Convert.ToInt32(result) > 0;
        }

        public void Mods_CreateTable()
        {
            if (!CheckIfExist("mods"))
            {
                sqlCommand = "CREATE TABLE mods(name TEXT, installed INT)";
                ExecuteQuery(sqlCommand);
            }
        }

        public void Mods_FillTable()
        {
            if (!CheckIfTableContainsData("mods"))
            {
                sqlCommand = "BEGIN TRANSACTION";
                command = new SQLiteCommand(sqlCommand, dbConnection);
                command.ExecuteNonQuery();

                foreach (Mod mod in PCUE_ModManager.instance.Mods)
                {
                    sqlCommand = "INSERT INTO mods(name, installed) values ('" + mod.Name + "', 0)";
                    command = new SQLiteCommand(sqlCommand, dbConnection);
                    ExecuteQuery(sqlCommand);
                }

                sqlCommand = "COMMIT";
                command = new SQLiteCommand(sqlCommand, dbConnection);
                command.ExecuteNonQuery();
            }
        }

        public void Mods_CompareRecords()
        {
            var modList = PCUE_ModManager.instance.Mods;

            sqlCommand = "SELECT * FROM mods WHERE installed = '1'";
            command = new SQLiteCommand(sqlCommand, dbConnection);
            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var mod = modList.Where(t => t.Name == (string)reader["name"]).FirstOrDefault();
                if (mod != null)
                {
                    mod.Installed = "+";
                }
                else
                {
                    mod = new(null)
                    {
                        Name = (string)reader["name"],
                        Installed = "*"
                    };
                    modList.Add(mod);
                }
            }
        }

        public void Mods_UpdateRecords()
        {
            Mods_CompareRecords();

            sqlCommand = "BEGIN TRANSACTION";
            command = new SQLiteCommand(sqlCommand, dbConnection);
            command.ExecuteNonQuery();

            sqlCommand = "SELECT * FROM mods";
            command = new SQLiteCommand(sqlCommand, dbConnection);
            reader = command.ExecuteReader();

            // Delete Missing And Uninstalled Records
            while (reader.Read())
            {
                string modName = (string)reader["name"];
                if (PCUE_ModManager.instance.Mods.Where(t => t.Name == modName).ToArray().Length == 0)
                {
                    if ((int)reader["installed"] == 0)
                    {
                        sqlCommand = "DELETE FROM mods WHERE name = '" + modName + "'";
                        command = new SQLiteCommand(sqlCommand, dbConnection);
                        command.ExecuteNonQuery();
                    }
                }
            }

            // Add New Records
            foreach (Mod mod in PCUE_ModManager.instance.Mods)
            {
                sqlCommand = "SELECT COUNT(name) FROM mods WHERE name = '" + mod.Name + "'";
                command = new SQLiteCommand(sqlCommand, dbConnection);
                var result = Convert.ToInt32(command.ExecuteScalar());

                if (result == 0)
                {
                    sqlCommand = "INSERT INTO mods (name, installed) values ('" + mod.Name + "', 0)";
                    command = new SQLiteCommand(sqlCommand, dbConnection);
                    command.ExecuteNonQuery();
                }
            }

            sqlCommand = "COMMIT";
            command = new SQLiteCommand(sqlCommand, dbConnection);
            command.ExecuteNonQuery();
        }

        public void Mods_SetInstalled(Mod mod)
        {
            sqlCommand = "UPDATE mods SET installed = 1 WHERE name = '" + mod.Name + "'";
            command = new SQLiteCommand(sqlCommand, dbConnection);
            command.ExecuteNonQuery();
        }

        public void Files_CreateTable()
        {
            if (!CheckIfExist("files"))
            {
                sqlCommand = "CREATE TABLE files(mod TEXT, path TEXT)";
                ExecuteQuery(sqlCommand);
            }

        }

        public void Files_FillTable()
        {
            if (!CheckIfTableContainsData("files"))
            {
                sqlCommand = "BEGIN TRANSACTION";
                command = new SQLiteCommand(sqlCommand, dbConnection);
                command.ExecuteNonQuery();

                var files = Directory.GetFiles(Settings.Default.backupFolder);

                foreach (string file in files)
                {
                    sqlCommand = "INSERT INTO files (mod, path) values ('none', '" + file + "')";
                    command = new SQLiteCommand(sqlCommand, dbConnection);
                    command.ExecuteNonQuery();
                }

                sqlCommand = "COMMIT";
                command = new SQLiteCommand(sqlCommand, dbConnection);
                command.ExecuteNonQuery();
            }
        }

        public void Files_AddRecords(string modName, string[] files)
        {
            sqlCommand = "BEGIN TRANSACTION";
            command = new SQLiteCommand(sqlCommand, dbConnection);
            command.ExecuteNonQuery();

            foreach (string file in files)
            {
                sqlCommand = "INSERT INTO files (mod, path) values ('" + modName + "', '" + file + "')";
                command = new SQLiteCommand(sqlCommand, dbConnection);
                command.ExecuteNonQuery();
            }

            sqlCommand = "COMMIT";
            command = new SQLiteCommand(sqlCommand, dbConnection);
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Gets the files for the chosen mod and removes them from the table
        /// </summary>
        /// <param name="modName"></param>
        /// <returns></returns>
        
        public string[] Files_TakeRecords(string modName)
        {
            sqlCommand = "BEGIN TRANSACTION";
            command = new SQLiteCommand(sqlCommand, dbConnection);
            command.ExecuteNonQuery();

            sqlCommand = "SELECT * FROM files WHERE mod = '" + modName + "'";
            command = new SQLiteCommand(sqlCommand, dbConnection);
            var reader = command.ExecuteReader();

            var resultList = new List<string>();
            while (reader.Read())
            {
                resultList.Add((string)reader["path"]);
            }

            sqlCommand = "DELETE FROM files WHERE mod = '" + modName + "'";
            command = new SQLiteCommand(sqlCommand, dbConnection);
            command.ExecuteNonQuery();

            sqlCommand = "COMMIT";
            command = new SQLiteCommand(sqlCommand, dbConnection);
            command.ExecuteNonQuery();

            return resultList.ToArray();
        }
    }
}
