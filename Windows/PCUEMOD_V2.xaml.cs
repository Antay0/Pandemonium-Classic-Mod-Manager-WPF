using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
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
using System.Xml.Linq;

namespace Pandemonium_Classic_Mod_Manager
{
    /// <summary>
    /// Interaction logic for PCUEMOD_V2.xaml
    /// </summary>
    public partial class PCUEMOD_V2 : Window
    {
        public ModV2 Mod;
        public bool installed, showWindow = true, earlyExit = true, error;
        public int stepIndex = 0;

        public bool OnlyOne { get; set; }
        public bool Required { get; set; }

        public ObservableCollection<Option> OptionList { get; set; } = new();

        public List<string> fileList = new();
        public Dictionary<string, string> stringList = new();

        public PCUEMOD_V2(ModV2 mod)
        {
            InitializeComponent();

            Mod = mod;
            Title = "PCUEMOD Installer V2: " + Mod.Name;

            fileList.AddRange(mod.mainPackageFiles);
            stringList = new (mod.mainPackageStrings);

            if (Mod.Steps.Count != 0)
            {
                RunInstallStep(Mod.Steps[0]);
                showWindow = true;
            }
            else
            {
                showWindow = false;
            }
        }

        public void RunInstallStep(Step step)
        {
            // Reset dialog
            CleanDialog();
            OnlyOne = step.onlyOne;
            Required = step.required;

            if (!OnlyOne)
                optionListBox.ItemTemplate = this.FindResource("CheckBoxTemplate") as DataTemplate;
            else
                optionListBox.ItemTemplate = this.FindResource("RadioButtonTemplate") as DataTemplate;
            optionListBox.ApplyTemplate();

            installStep_Label.Content = step.name;

            foreach (var option in step.Options)
                OptionList.Add(option);

            if (stepIndex == Mod.Steps.Count - 1)
                nextButton.Content = "Finish";
        }

        public void CleanDialog()
        {
            installStep_Label.Content = string.Empty;
            OptionList.Clear();
            optionDescription_TextBox.Text = string.Empty;
            optionPreviewBox.Source = null;
        }

        public void NextButton_Click(object sender, RoutedEventArgs e)
        {
            var checkedOptions = OptionList.Where(t => t.IsChecked).ToList();
            if (!Required || checkedOptions.Count != 0)
            {
                // Add selected file packs to list of files to install
                foreach (var option in checkedOptions)
                {
                    fileList.AddRange(option.Files);
                    foreach(var text in option.Strings)
                    {
                        if (!stringList.Any(it => it.Key == text.Key))
                            stringList.Add(text.Key, text.Value);
                        else
                            stringList[text.Key] = text.Value;
                    }
                }

                if (stepIndex < Mod.Steps.Count - 1)
                {
                    stepIndex++;
                    RunInstallStep(Mod.Steps[stepIndex]);
                }
                else ExitInstaller();
            }
            else MessageBox.Show("This step requires at least one box to be checked!", "Required Segment",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void OptionListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (optionListBox.SelectedIndex >= 0
                && optionListBox.SelectedIndex < optionListBox.Items.Count
                && OptionList.Count != 0)
                UpdateMenu((Option)optionListBox.SelectedItem);
        }

        private void OptionCheckBox_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Option option;
                if (!OnlyOne) option = (Option)((CheckBox)e.Source).DataContext;
                else option = (Option)((RadioButton)e.Source).DataContext;
                optionListBox.SelectedIndex = optionListBox.Items.IndexOf(option);
                UpdateMenu(option);
            }
            catch (Exception ex)
            {
                PCUE_ModManager.ShowError(ex);
            }
        }

        private void UpdateMenu(Option selected)
        {
            optionDescription_TextBox.Text = string.Empty;
            optionPreviewBox.Source = null;

            if (selected != null)
            {
                if (selected.description != null)
                    optionDescription_TextBox.Text = selected.description;
                if (selected.loadedImage != null)
                    optionPreviewBox.Source = selected.loadedImage;
            }
        }

        public void ExitInstaller()
        {
            earlyExit = false;
            this.Close();
        }
    }
}
