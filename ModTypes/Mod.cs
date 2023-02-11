using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;

namespace Pandemonium_Classic_Mod_Manager
{
    public class Mod
    {
        [XmlAttribute] public string Name { get; set; } = "";
        [XmlAttribute] public string Author = "";
        public string Description = "";

        [XmlIgnore] public string Installed { get { return _installed; } set { _installed = value; PCUE_ModManager.instance.modList_View.Items.Refresh(); } }
        [XmlIgnore] public string _installed = "";

        [XmlIgnore] public bool BackUp { get; set; }

        [XmlIgnore] public BitmapImage thumbnail = new ();
    }
}
