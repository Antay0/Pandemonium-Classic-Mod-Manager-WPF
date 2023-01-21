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
using System.Data.Entity;

namespace Pandemonium_Classic___Mod_Manager__WPF_
{
    public partial class PCUEMOD_V1 : Window
    {
        public Mod Mod;
        public bool installed, showWindow, earlyExit = true;

        XDocument doc;
        XElement[] installSteps;
        int stepIndex;

        public ObservableCollection<InstallerOption> OptionList { get; set; } = new();

        public List<string> fileList = new();
        public List<string> localFileList = new();
        public int installCount = 0;

        public bool SelectOne { get; set; }
        public bool Required { get; set; }

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
            {
                RunInstallStep(0);
                showWindow = true;
            }
            else
            {
                nextButton.Content = "Finish";
                showWindow = false;
            }
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
            SelectOne = false;
            Required = false;
            OptionList.Clear();

            XElement step = installSteps[index];
            XmlReader reader = step.CreateReader();

            reader.ReadToFollowing("installstep");
            installStep_Label.Content = reader.GetAttribute("name");
            reader.MoveToElement();

            SelectOne = reader.GetAttribute("onlyone") == "true";
            Required = reader.GetAttribute("required") == "true";

            optionCheckList.IsEnabled = !SelectOne;
            optionCheckList.Visibility = !SelectOne ? Visibility.Visible : Visibility.Hidden;

            optionRadioList.IsEnabled = SelectOne;
            optionRadioList.Visibility = SelectOne ? Visibility.Visible : Visibility.Hidden;

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
                    OptionList.Add(newOption);
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
            OptionList.Clear();
            optionDescription_TextBox.Text = string.Empty;
            optionPreviewBox.Source = null;
        }

        private void OptionCheckList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            optionDescription_TextBox.Text = string.Empty;
            optionPreviewBox.Source = null;

            if (optionCheckList.SelectedIndex >= 0
                && optionCheckList.SelectedIndex < optionCheckList.Items.Count
                && OptionList.Count != 0)
                UpdateMenu((InstallerOption)optionCheckList.SelectedItem);
        }

        private void OptionRadioList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            optionDescription_TextBox.Text = string.Empty;
            optionPreviewBox.Source = null;

            if (optionRadioList.SelectedIndex >= 0
                && optionRadioList.SelectedIndex < optionRadioList.Items.Count
                && OptionList.Count != 0)
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

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            var checkedOptions = OptionList.Where(t => t.IsChecked).ToList();
            if (!Required || checkedOptions.Count != 0)
            {
                // Add selected file packs to list of files to install
                foreach (var option in checkedOptions)
                    fileList.AddRange(option.Files);

                if (stepIndex < installSteps.Length - 1)
                {
                    stepIndex++;
                    RunInstallStep(stepIndex);
                }
                else ExitInstaller();
            }
            else MessageBox.Show("This step requires at least one box to be checked!", "Required Segment",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void ExitInstaller()
        {
            earlyExit = false;
            this.Close();
        }
    }

    public class InstallerOption
    {
        public string? Name { get; set; }
        public string? Description;
        public List<string> Files = new();
        public BitmapImage? Image;

        public bool IsChecked { get; set; }
    }
}
