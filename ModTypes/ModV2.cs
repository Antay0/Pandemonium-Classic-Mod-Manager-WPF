using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;

namespace Pandemonium_Classic_Mod_Manager
{
    public class ModV2 : Mod
    {
        [XmlIgnore] public string modFolder = "";

        public string MainPackage = "";
        [XmlIgnore] public List<string>mainPackageFiles = new();

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
                    var files = Directory.GetFiles(Path.Combine(folder, loadedMod.MainPackage), "*", SearchOption.AllDirectories);
                    loadedMod.mainPackageFiles.AddRange(files);
                }

                foreach (var step in loadedMod.Steps)
                {
                    foreach(var option in step.Options)
                    {
                        var files = Directory.GetFiles(Path.Combine(folder, option.folder), "*", SearchOption.AllDirectories);
                        option.Files.AddRange(files);

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
        [XmlIgnore] public BitmapImage? loadedImage;
        [XmlIgnore] public bool IsChecked { get; set; } = false;

        public Option() { }
    }
}
