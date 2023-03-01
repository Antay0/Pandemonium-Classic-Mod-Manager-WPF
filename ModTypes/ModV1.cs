using Pandemonium_Classic_Mod_Manager.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Xml;

namespace Pandemonium_Classic_Mod_Manager
{
    /// <summary>
    /// Acts as a representation of a PCUEMOD
    /// </summary>
    public class ModV1 : Mod
    {
        public string FolderPath = ""; // Path to base folder that contains all other components of the mod.
        public string xmlPath = ""; // Path to mod.xml

        public ModV1(string? filePath)
        {
            if (filePath != null)
            {
                try
                {
                FolderPath = filePath.Replace("PCUEMOD\\mod.xml", "");
                xmlPath = filePath;

                    using (XmlReader reader = XmlReader.Create(filePath))
                    {
                        reader.ReadToFollowing("mod");

                        var tmp = reader.GetAttribute("name");
                        if (tmp != null)
                            Name = tmp;
                        else Name = "null";

                        reader.ReadToDescendant("description");
                        Description = reader.ReadElementContentAsString();

                        var uri = new Uri(System.IO.Path.Combine(FolderPath, "PCUEMOD\\preview.png"));
                        thumbnail = new(uri);
                    }
                }
                catch(Exception e)
                {
                    PCUEDebug.ShowError(e);
                }
            }
        }
    }
}
