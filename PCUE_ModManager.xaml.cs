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

namespace Pandemonium_Classic___Mod_Manager__WPF_
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class PCUE_ModManager : Window
    {
        public ObservableCollection<Mod> Mods { get; set; } = new();

        public PCUE_ModManager()
        {
            InitializeComponent();
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
            Properties.Settings.Default.modsFolder = modsFolder_TextBox.Text;
            Properties.Settings.Default.Save();

            string dir = Properties.Settings.Default.modsFolder;
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
            Properties.Settings.Default.gameDataFolder = gameData_TextBox.Text;
            Properties.Settings.Default.Save();
        }

        private void modList_View_Selected(object sender, RoutedEventArgs e)
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

        /*private void modList_View_Selected(object sender, RoutedEventArgs e)
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
        }*/
    }


    /// 
    /// Mod Class
    /// 

    public class Mod
    {
        public string? Name { get; set; }
        public string? Description;

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

            reader.ReadToDescendant("description");
            Description = reader.ReadElementContentAsString();

            var uri = new Uri(System.IO.Path.Combine(FolderPath, "PCUEMOD\\preview.png"));
            preview = new(uri);
        }
    }
}
