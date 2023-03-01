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
    [XmlRoot("PCUEMODV2")] public class ModV2 : Mod
    {
        [XmlIgnore] public string modFolder = "";

        public string MainPackage = "";
        [XmlIgnore] public List<string>mainPackageFiles = new();
        [XmlIgnore] public Dictionary<string, Dictionary<string, string>> mainPackageStrings = new();

        [XmlElement("InstallSection")] public List<Step> Steps = new();

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

        /// <summary>
        /// Writes an xml for testing purposes
        /// </summary>
        /// <param name="mod"></param>
        public static void WriteModV2XML()
        {
            ModV2 mod = new()
            {
                Name = "Vampire MILFs - ModV2 Test",
                Author = "Antay",
                Description = "STUPID SHIT BOOO HOOOOB",
                MainPackage = "0Main",
                Steps = new()
                {
                    new(){name = "Talia Selection", onlyOne = true, required = true,
                        Options = new()
                        {
                            new Option(){ name = "Confident", description = "Fuck", folder = "Shit1", image = "confident.png"},
                            new Option(){ name = "Horny", description = "Shit", folder = "Butt2", image = "horny.png"}
                        }
                        },
                    new(){name = "Lotta Selection", onlyOne = true, required = true,
                        Options = new()
                        {
                            new Option(){ name = "Confident", description = "Fuck", folder = "Shit3", image = "confident.png"},
                            new Option(){ name = "Wanker", description = "eat", folder = "Dick4", image = "fork.png"},
                            new Option(){ name = "Horny", description = "Shit", folder = "Butt5", image = "horny.png"}
                        }
                    }
                }
            };

            XmlSerializer serializer = new XmlSerializer(typeof(ModV2));

            using (FileStream file = File.Create(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "testModV2.xml")))
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

        [XmlElement("Option")] public List<Option> Options = new();
    }

    public class Option
    {
        [XmlAttribute] public string name { get; set; } = "";
        public string description = "", folder = "", image = "";

        [XmlIgnore] public List<string> Files = new();
        [XmlIgnore] public Dictionary<string, Dictionary<string, string>> Strings = new();
        [XmlIgnore] public BitmapImage? loadedImage;
        [XmlIgnore] public bool IsChecked { get; set; } = false;

        public Option() { }
    }


    [XmlRoot("ModStrings")] public class ModStrings
    {
        [XmlElement("Section")] public List<StringSection> AllStrings = new();

        public ModStrings() { }

        public static void WriteModdedTextFile(Dictionary<string, Dictionary<string, string>> strings)
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

                    foreach (var section in strings)
                    {
                        writer.WriteStartElement(section.Key);
                        foreach (var entry in section.Value)
                        {
                            writer.WriteStartElement(entry.Key);
                            writer.WriteString(entry.Value);
                            writer.WriteEndElement();
                        }
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

        public static Dictionary<string, Dictionary<string, string>> LoadModdedText(string file)
        {
            try
            {
                var dict = new Dictionary<string, Dictionary<string, string>>();
                var pair = new Dictionary<string, string>();
                ModStrings? loadedStrings;
                var reader = new XmlSerializer(typeof(ModStrings));
                using (var stream = new StreamReader(file))
                {
                    loadedStrings = reader.Deserialize(stream) as ModStrings;
                }
                foreach (var section in loadedStrings.AllStrings)
                {
                    if (!dict.ContainsKey(section.Name))
                    {
                        dict.Add(section.Name, new());
                    }
                    foreach (var entry in section.Strings)
                    {
                        if (!dict[section.Name].ContainsKey(entry.Key))
                        {
                            dict[section.Name].Add(entry.Key, entry.Text);
                        }
                        else
                        {
                            dict[section.Name][entry.Key] = entry.Text;
                        }
                    }
                }
                return dict;
            }
            catch (Exception e)
            {
                PCUE_ModManager.ShowError(e);
                return null;
            }
        }

        /// <summary>
        /// Writes an xml for testing purposes
        /// </summary>
        /// <param name="mod"></param>
        public static void WriteTextXML()
        {
            ModStrings modStrings = new ModStrings()
            {
                AllStrings = new()
                {
                    new()
                    {
                        Name = "undineStrings",
                        Strings = new()
                        {
                            new()
                            {
                                Key = "TRANSFORM_PLAYER_1",
                                Text = "I am testing this system"
                            },
                            new()
                            {
                                Key = "TRANSFORM_PLAYER_2",
                                Text = "{TransformerName} disrobes you gently, and begins rubbing your back, yes, {TransformerName}. She gradually moves down, rubbing below your breasts and bottom, and above your waist as well with sensual skill. A cool feeling of relaxation spreads wherever she touches you. You don't need to worry any more."
                            }
                        }
                    },
                    new()
                    {
                        Name = "bitchStrings",
                        Strings = new()
                        {
                            new()
                            {
                                Key = "TRANSFORM_NPC_1",
                                Text = "SQUEEEEEE"
                            },
                            new()
                            {
                                Key = "TRANSFORM_NPC_2",
                                Text = "BAAAASF"
                            }
                        }
                    }
                }
            };

            XmlSerializer serializer = new XmlSerializer(typeof(ModStrings));

            using (FileStream file = File.Create(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "testText.xml")))
            {
                serializer.Serialize(file, modStrings);
            }
        }
    }

    public class StringSection
    {
        [XmlAttribute("name")] public string Name = "";
        [XmlElement("String")] public List<StringEntry> Strings = new();
    }


    public class StringEntry
    {
        [XmlAttribute("key")] public string Key = "";
        [XmlAttribute("text")] public string Text = "";

        public StringEntry() { }
    }
}
