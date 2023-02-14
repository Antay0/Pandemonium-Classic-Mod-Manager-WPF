using Pandemonium_Classic_Mod_Manager.Properties;
using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Serialization;

namespace Pandemonium_Classic_Mod_Manager
{
    public class ModV2 : Mod
    {
        [XmlIgnore] public string modFolder = "";

        public string MainPackage = "";
        [XmlIgnore] public List<string>mainPackageFiles = new();
        [XmlIgnore] public Dictionary<string, string> mainPackageStrings = new();

        public List<Step> Steps = new();

        public ModV2() { }

        public static ModV2? LoadModXML(string xml)
        {
            var folder = xml.Replace("PCUEMOD\\modV2.xml", "");
            try
            {
                var reader = new XmlSerializer(typeof(ModV2));

                if (!File.Exists(xml))
                {
                    PCUE_ModManager.ShowError("XML did not exist!");
                    return null;
                }

                ModV2 loadedMod;
                using (var file = new StreamReader(xml))
                {
                    loadedMod = reader.Deserialize(file) as ModV2;
                }


                if (File.Exists(folder + "/PCUEMOD/preview.png"))
                {
                    Uri uri = new(folder + "/PCUEMOD/preview.png");
                    loadedMod.thumbnail = new(uri);
                }

                if (loadedMod.MainPackage != "")
                {
                    string textPath = Path.Combine(folder, loadedMod.MainPackage, "text.xml");
                    if (File.Exists(textPath))
                        loadedMod.mainPackageStrings = ModStrings.LoadModdedText(textPath);

                    var files = Directory.GetFiles(Path.Combine(folder, loadedMod.MainPackage), "*", SearchOption.AllDirectories);
                    loadedMod.mainPackageFiles.AddRange(files.Where(t => t != textPath));
                }

                foreach (var step in loadedMod.Steps)
                {
                    foreach(var option in step.Options)
                    {
                        string textPath = Path.Combine(folder, option.folder, "text.xml");
                        if (File.Exists(textPath))
                            option.Strings = ModStrings.LoadModdedText(textPath);

                        var files = Directory.GetFiles(Path.Combine(folder, option.folder), "*", SearchOption.AllDirectories);
                        option.Files.AddRange(files.Where(t => t != textPath));

                        Uri uri = new(Path.Combine(folder, "PCUEMOD/images", option.image));
                        option.loadedImage = new(uri);
                    }
                }
                loadedMod.modFolder = folder;
                return loadedMod;
            }
            catch(Exception e)
            {
                PCUE_ModManager.ShowError(e);
            }
            return null;
        }

        public static void WriteModV2XML(ModV2 mod)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ModV2));

            using (FileStream file = File.Create(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test.xml")))
            {
                serializer.Serialize(file, mod);
            }
        }
    }

    public class Step
    {
        [XmlAttribute] public string name { get; set; } = "";
        [XmlAttribute] public bool onlyOne { get; set; } = false;
        [XmlAttribute] public bool required { get; set; } = false;

        public List<Option> Options = new();
    }

    public class Option
    {
        [XmlAttribute] public string name { get; set; } = "";
        public string description = "", folder = "", image = "";

        [XmlIgnore] public List<string> Files = new();
        [XmlIgnore] public Dictionary<string, string> Strings = new();
        [XmlIgnore] public BitmapImage? loadedImage;
        [XmlIgnore] public bool IsChecked { get; set; } = false;

        public Option() { }
    }




    public class ModStrings
    {
        public List<Entry> Strings = new();

        public ModStrings() { }

        public static Dictionary<string, string> LoadModdedText(string file)
        {
            try
            {
                var dict = new Dictionary<string, string>();
                ModStrings? loadedStrings;
                var reader = new XmlSerializer(typeof(ModStrings));
                using (var stream = new StreamReader(file))
                {
                    loadedStrings = reader.Deserialize(stream) as ModStrings;
                }
                dict = loadedStrings.Strings.ToDictionary(t => t.Key, t => t.Text);
                return dict;
            }
            catch (Exception e)
            {
                PCUE_ModManager.ShowError(e);
                return null;
            }
        }

        public static void WriteModdedTextFile(Dictionary<string, string> strings)
        {
            try
            {
                XmlWriterSettings writterSettings = new()
                {
                    Indent = true,
                    IndentChars = "\t"
                };
                using (var writer = XmlWriter.Create(Path.Combine(Settings.Default.gameFolder, "Translations", "Modded.xml"), writterSettings))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("AllStrings");

                    foreach (var pair in strings)
                    {
                        writer.WriteStartElement(pair.Key);
                        writer.WriteString(pair.Value);
                        writer.WriteEndElement();
                    }

                    writer.WriteEndDocument();
                }
            }
            catch (Exception e)
            {
                PCUE_ModManager.ShowError(e);
            }
        }
    }

    public class Entry
    {
        public string Key = "";
        public string Text = "";

        public Entry() { }
    }
}
