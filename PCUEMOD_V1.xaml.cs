using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
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
using System.IO;
using System.Collections.ObjectModel;

namespace Pandemonium_Classic___Mod_Manager__WPF_
{
    public partial class PCUEMOD_V1 : Window
    {
        public Mod Mod;
        public bool installed;

        XDocument doc;
        XElement[] installSteps;
        int stepIndex;

        public ObservableCollection<InstallerOption> optionList { get; set; } = new();

        public List<string> fileList = new();
        public int installCount = 0;

        public bool selectOne { get; set; }
        public bool required { get; set; }

        public PCUEMOD_V1(Mod mod)
        {
            Mod = mod;
            InitializeComponent();

            this.Title = "PCUEMOD Installer V1: " + Mod.Name;

            // Gets the xml document in question to guide the installer
            doc = XDocument.Load(mod.xmlPath);

            var mainPackage = doc.Descendants().Elements("mainpackage").FirstOrDefault();
            if (mainPackage != null)
                GetMainPackage(mainPackage);

            installSteps = doc.Descendants().Elements("installstep").ToArray();
            stepIndex = 0;

            if (installSteps.Length != 0)
                RunInstallStep(0);
            else
                nextButton.Content = "Finish";
        }

        public void GetMainPackage(XElement element)
        {
            string folderPath = System.IO.Path.Combine(Mod.FolderPath, element.Value);
            fileList.AddRange(Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories).ToArray());
        }

        public void RunInstallStep(int index)
        {
            // Reset dialog
            CleanDialog();
            selectOne = false;
            required = false;
            optionList.Clear();

            XElement step = installSteps[index];
            XmlReader reader = step.CreateReader();

            reader.ReadToFollowing("installstep");
            installStep_Label.Content = reader.GetAttribute("name");
            reader.MoveToElement();

            selectOne = reader.GetAttribute("onlyone") == "true" ? true : false;
            required = reader.GetAttribute("required") == "true" ? true : false;

            optionCheckList.IsEnabled = !selectOne;
            optionCheckList.Visibility = !selectOne ? Visibility.Visible : Visibility.Hidden;

            optionRadioList.IsEnabled = selectOne;
            optionRadioList.Visibility = selectOne ? Visibility.Visible : Visibility.Hidden;

            var optionElements = step.Descendants("option").ToList();
            foreach (var element in optionElements)
            {
                var newOption = new InstallerOption();

                reader = element.CreateReader();
                reader.ReadToFollowing("option");
                string? label = reader.GetAttribute("name");
                if (label != null)
                {
                    newOption.Name = label;
                    optionList.Add(newOption);
                }
                else
                {
                    MessageBox.Show("option name is null", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                reader.MoveToElement();
                reader.ReadToDescendant("description");
                newOption.Description = reader.ReadElementContentAsString();

                // Get value from <folder> element
                string folderPath = System.IO.Path.Combine(Mod.FolderPath, reader.ReadElementContentAsString());
                newOption.Files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories).ToList();

                // Get value from <image> element
                var uri = new Uri(System.IO.Path.Combine(Mod.FolderPath, "PCUEMOD\\images", reader.ReadElementContentAsString()));
                newOption.Image = new(uri);
            }

            if (index == installSteps.Length - 1)
                nextButton.Content = "Finish";
        }
        public void CleanDialog()
        {
            installStep_Label.Content = string.Empty;
            optionList.Clear();
            optionDescription_TextBox.Text = string.Empty;
            optionPreviewBox.Source = null;
        }

        private void optionCheckList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            optionDescription_TextBox.Text = string.Empty;
            optionPreviewBox.Source = null;

            if (optionCheckList.SelectedIndex >= 0
                && optionCheckList.SelectedIndex < optionCheckList.Items.Count
                && optionList.Count != 0)
                UpdateMenu((InstallerOption)optionCheckList.SelectedItem);
        }

        private void optionRadioList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            optionDescription_TextBox.Text = string.Empty;
            optionPreviewBox.Source = null;

            if (optionRadioList.SelectedIndex >= 0
                && optionRadioList.SelectedIndex < optionRadioList.Items.Count
                && optionList.Count != 0)
                UpdateMenu((InstallerOption)optionRadioList.SelectedItem);
        }

        private void UpdateMenu(InstallerOption selected)
        {
            if (selected != null)
            {
                if (selected.Description != null)
                    optionDescription_TextBox.Text = selected.Description;
                if (selected.Image != null)
                    optionPreviewBox.Source = selected.Image;
            }
        }

        private void nextButton_Click(object sender, RoutedEventArgs e)
        {
            var checkedOptions = optionList.Where(t => t.isChecked).ToList();
            if (!required || checkedOptions.Count != 0)
            {
                // Add selected file packs to list of files to install
                foreach (var option in checkedOptions)
                    fileList.AddRange(option.Files);

                if (stepIndex < installSteps.Length - 1)
                {
                    stepIndex++;
                    RunInstallStep(stepIndex);
                }
                else InstallFiles();
            }
            else MessageBox.Show("This step requires at least one box to be checked!", "Required Segment",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void InstallFiles()
        {
            var msgResult = MessageBox.Show("Install " + fileList.Count + " files?", "Confirmation",
                        MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (msgResult == MessageBoxResult.OK)
            {
                foreach (var file in fileList)
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
                        string newPath = System.IO.Path.Combine(Properties.Settings.Default.gameDataFolder, file.Remove(0, i));
                        Directory.CreateDirectory(newPath.Remove(newPath.LastIndexOf("\\")));
                        File.Copy(file, newPath, true);

                        installCount++;
                    }
                }
            }
            ExitInstaller();
        }

        private void ExitInstaller()
        {
            MessageBox.Show(installCount + " files installed.", "PCUEMOD",
                MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }
    }

    public class InstallerOption
    {
        public string? Name { get; set; }
        public string? Description;
        public List<string> Files = new();
        public BitmapImage? Image;

        public bool isChecked { get; set; }
    }
}
