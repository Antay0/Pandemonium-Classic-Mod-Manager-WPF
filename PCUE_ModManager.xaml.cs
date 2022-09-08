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
using DataBase;

namespace Pandemonium_Classic___Mod_Manager__WPF_
{
    // Pandemonium_Classic___Mod_Manager__WPF_
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class PCUE_ModManager : Window
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public static PCUE_ModManager instance;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public ObservableCollection<Mod> Mods { get; set; } = new();

        //public PCUE_Database database;

        public PCUE_ModManager()
        {
            instance = this;
            InitializeComponent();

            modsFolder_TextBox.Text = Settings.Default.modsFolder;
            modsFolder_Update();
            gameData_TextBox.Text = Settings.Default.gameDataFolder;

            if (string.IsNullOrEmpty(Settings.Default.backupFolder))
            {
                string backupDir = AppDomain.CurrentDomain.BaseDirectory + "\\backup";
                if (!Directory.Exists(backupDir))
                    Directory.CreateDirectory(backupDir);
                Settings.Default.backupFolder = AppDomain.CurrentDomain.BaseDirectory + "\\backup";
            }

            //database = new PCUE_Database();
        }

        private void modsFolder_FileBrowser_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new FolderBrowserDialog();
            {
                DialogResult result = fbd.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    modsFolder_TextBox.Text = fbd.SelectedPath;
                    modsFolder_Update();
                }
            }
        }

        private void gameData_FileBrowser_Click(object sender, RoutedEventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    gameData_TextBox.Text = fbd.SelectedPath;
                    gameData_Update();
                }
            }
        }

        private void modsFolder_TextBox_OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                modsFolder_Update();
        }

        private void gameData_TextBox_OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                gameData_Update();
        }

        private void modsFolder_Update()
        {
            Settings.Default.modsFolder = modsFolder_TextBox.Text;
            Settings.Default.Save();

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
        }

        private void gameData_Update()
        {
            Settings.Default.gameDataFolder = gameData_TextBox.Text;
            Settings.Default.Save();
        }

        private void modList_View_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

        private void installButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(Settings.Default.modsFolder)
                && !string.IsNullOrEmpty(Settings.Default.gameDataFolder))
            {
                Mod mod = (Mod)modList_View.SelectedItem;
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

                }
            }
            else
            {
                System.Windows.MessageBox.Show("Game Data or Mod folders are not declared.", "Empty Directory",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }
    }


    /// 
    /// Mod Class
    /// 

    public class Mod
    {
        public string? Name { get; set; }
        public string? Description;

        public int installerVersion;

        public BitmapImage preview;

        public string FolderPath; // Path to base folder that contains all other components of the mod.
        public string xmlPath; // Path to mod.xml

        public Mod(string filePath)
        {
            FolderPath = filePath.Replace("PCUEMOD\\mod.xml", "");
            xmlPath = filePath;

            XmlReader reader = XmlReader.Create(filePath);

            reader.ReadToFollowing("mod");
            Name = reader.GetAttribute("name");

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

namespace DataBase
{
    ///
    /// Remember App.Application_Exit() for dbConnection disposal
    ///

    /*public class PCUE_Database
    {
        public SQLiteConnection dbConnection;
        SQLiteCommand command;
        SQLiteDataReader? reader;
        string sqlCommand;
        string dbPath = AppDomain.CurrentDomain.BaseDirectory;
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
                mods_CreateTable();
                mods_FillTable();
                files_CreateTable();
                files_FillTable();
            }
            else
            {
                CreateDbConnection();
                mods_UpdateRecords();
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

        public void mods_CreateTable()
        {
            if (!CheckIfExist("mods"))
            {
                sqlCommand = "CREATE TABLE mods(name TEXT, installed BOOLEAN NOT NULL CHECK (mods IN (0, 1))";
                ExecuteQuery(sqlCommand);
            }
        }
        public void files_CreateTable()
        {
            if (!CheckIfExist("files"))
            {
                sqlCommand = "CREATE TABLE files(path TEXT)";
                ExecuteQuery(sqlCommand);
            }

        }

        public bool CheckIfExist(string tableName)
        {
            command.CommandText = "SELECT name FROM sqlite_master WHERE name='" + tableName + "'";
            var result = command.ExecuteScalar();

            return result != null && result.ToString() == tableName ? true : false;
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

            return Convert.ToInt32(result) > 0 ? true : false;
        }


        public void mods_FillTable()
        {
            if (!CheckIfTableContainsData("mods"))
            {
                sqlCommand = "BEGIN TRANSACTION";
                command = new SQLiteCommand(sqlCommand, dbConnection);
                command.ExecuteNonQuery();

                foreach (Mod mod in PCUE_ModManager.instance.Mods)
                {
                    sqlCommand = "INSERT INTO mods (name, installed) values ('" + mod.Name + "', 0";
                    command = new SQLiteCommand(sqlCommand, dbConnection);
                    ExecuteQuery(sqlCommand);
                }

                sqlCommand = "COMMIT";
                command = new SQLiteCommand(sqlCommand, dbConnection);
                command.ExecuteNonQuery();
            }
        }
        public void files_FillTable()
        {
            if (!CheckIfTableContainsData("files"))
            {
                sqlCommand = "BEGIN TRANSACTION";
                command = new SQLiteCommand(sqlCommand, dbConnection);
                command.ExecuteNonQuery();

                var files = Directory.GetFiles(Pandemonium_Classic___Mod_Manager__WPF.Properties.Settings.Default.backupFolder);

                foreach (string file in files)
                {
                    sqlCommand = "INSERT INTO files (path) values ('" + file + "')";
                    command = new SQLiteCommand(sqlCommand, dbConnection);
                    command.ExecuteNonQuery();
                }

                sqlCommand = "COMMIT";
                command = new SQLiteCommand(sqlCommand, dbConnection);
                command.ExecuteNonQuery();
            }
        }

        public void mods_UpdateRecords()
        {
            sqlCommand = "BEGIN TRANSACTION";
            command = new SQLiteCommand(sqlCommand, dbConnection);
            command.ExecuteNonQuery();

            sqlCommand = "SELECT * FROM mods";
            command = new SQLiteCommand(sqlCommand, dbConnection);
            command.ExecuteNonQuery();

            reader = command.ExecuteReader();

            // Delete Missing Records
            while (reader.Read())
            {
                string modName = (string)reader["name"];
                if (PCUE_ModManager.instance.Mods.Where(t => t.Name == modName).ToArray().Count() == 0)
                {
                    sqlCommand = "DELETE FROM mods WHERE name = '" + modName + "'";
                    command = new SQLiteCommand(sqlCommand, dbConnection);
                    command.ExecuteNonQuery();
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
                    sqlCommand = "INSERT INTO mods (name, installed) values ('" + mod.Name + "', 0";
                    command = new SQLiteCommand(sqlCommand, dbConnection);
                    command.ExecuteNonQuery();
                }
            }

            sqlCommand = "COMMIT";
            command = new SQLiteCommand(sqlCommand, dbConnection);
            command.ExecuteNonQuery();
        }

        public void mods_SetInstalled(Mod mod)
        {
            sqlCommand = "UPDATE mods SET installed = 1 WHERE name = '" + mod.Name + "'";
            command = new SQLiteCommand(sqlCommand, dbConnection);
            command.ExecuteNonQuery();
        }
    }*/
}
