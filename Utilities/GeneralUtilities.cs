using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Pandemonium_Classic_Mod_Manager.Utilities
{
    public static class GeneralUtilities
    {
        /// <summary>
        /// Returns the input file list relative to the StreamingAssets Folder
        /// </summary>
        /// <param name="fileList"></param>
        /// <returns></returns>
        public static List<string> GetLocalFileList(List<string> fileList)
        {
            var localFileList = new List<string>();
            foreach (var file in fileList)
            {
                int i = file.IndexOf("StreamingAssets");
                if (i == -1)
                {
                    PCUEDebug.ShowError("ERROR: substring '\\StreamingAssets' not found in file: " + file);
                }
                else
                {
                    localFileList.Add(file.Remove(0, i));
                }
            }
            return localFileList;
        }
    }
}
