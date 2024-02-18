using ICE_Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICE_AssetLibrary
{
    public static class Asset_Extend
    {
        public static bool Exists(this Asset asset, bool useCache)
        {
            if (useCache)
                return true;
            else
                return Directory.Exists(asset.FullPath) || File.Exists(asset.FullPath);

        }
        public static bool IsFolder(this Asset asset)
        {
            return string.IsNullOrEmpty(Path.GetExtension(asset.FullPath));
        }
    }
}
