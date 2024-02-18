using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICE_Model
{
    public class AssetLibraryOptions
    {
        public ObservableCollection<string> LibraryPaths{ get; set; }
        public ObservableCollection<Module> ProjectModules { get; set; }
    }
}
