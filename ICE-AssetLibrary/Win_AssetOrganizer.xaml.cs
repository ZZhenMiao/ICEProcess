using ICE_Model;
using LiZhenStandard.Sockets;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
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
using System.IO;
using LiZhenStandard.Extensions;

namespace ICE_AssetLibrary
{
    /// <summary>
    /// Win_AssetOrganizer.xaml 的交互逻辑
    /// </summary>
    public partial class Win_AssetOrganizer : Window,INotifyPropertyChanged
    {
        public Asset InputAsset { get; set; }
        public Asset SelectedAsset { get; set; }
        public ObservableCollection<AssetLabel> Labels { get; } = new ObservableCollection<AssetLabel>();
        public ObservableCollection<AssetType> AssetTypes { get; } = new ObservableCollection<AssetType>();
        public ObservableCollection<Project> Projects { get; } = new ObservableCollection<Project>();
        public ObservableCollection<Asset> Files { get; } = new ObservableCollection<Asset>();
        public ObservableCollection<Asset> Folders { get; } = new ObservableCollection<Asset>();
        public Asset[] SelectedFiles => AssetFiles_ListView.SelectedItems.Cast<Asset>().ToArray();
        public Asset SelectedFolder => AssetFolders_ListView.SelectedItem as Asset;
        public Win_AssetOrganizer(Asset asset)
        {
            InputAsset = asset;
            InitializeComponent();
            SetBindings();
            SetEvents();
            LoadInputAssetInfo();
            FindSimilarityAssets();
        }

        void SetBindings()
        {
            this.AssetFiles_ListView.SetBinding(ListView.ItemsSourceProperty, new Binding() { Source = this.Files });
            this.AssetFolders_ListView.SetBinding(ListView.ItemsSourceProperty, new Binding() { Source = this.Folders });
        }
        void SetEvents()
        {
            this.Cancel_Button.Click += (obj, e) => { this.Close(); };
            this.OK_Button.Click += (obj, e) => 
            {
                if (!SelectedFiles.Any() || SelectedFolder is null)
                {
                    MessageBox.Show("请选择要进行归纳的资产文件和相应的文件夹。");
                    return;
                }

                TakeIn();
                this.Close();
            };
        }

        void LoadInputAssetInfo()
        {
            SocketFunction.SendInstruct(App.Socket, "LoadAssetInfo", new object[] { InputAsset.ID }, out object[] results, out Exception ex);
            var ts = (AssetType[])results[0];
            var ps = (Project[])results[1];
            var ls = (AssetLabel[])results[2];
            var str = "";
            if (ps.Any())
                str += $"[{ps.AllToString()}] , ";
            if (ts.Any())
                str += $"[{ts.AllToString()}]";
            if (ls.Any())
                str += $" , [{ls.AllToString()}]";
            this.AssetLabels_TextBox.Text = str;
        }

        void LoadAssetInfo(Asset asset)
        {
            SocketFunction.SendInstruct(App.Socket, "LoadAssetInfo", new object[] { asset.ID }, out object[] results, out Exception ex);
            var ts = (AssetType[])results[0];
            var ps = (Project[])results[1];
            var ls = (AssetLabel[])results[2];
            var str = "";
            if (ps.Any())
                str += $"[{ps.AllToString()}] , ";
            if (ts.Any())
                str += $"[{ts.AllToString()}]";
            if (ls.Any())
                str += $" , [{ls.AllToString()}]";

            str += $"  >{asset.FullPath}";
            Console.WriteLine(str); 
        }


        void FindSimilarityAssets()
        {
            SocketFunction.SendInstruct(App.Socket, "FindSimilarityAssets_ByAsset", new object[] { InputAsset.ID }, out object[] reObjs, out Exception expj);
            if (!reObjs.Any())
                return;
            if (reObjs[0] is not Asset[])
                return;
            var assets = (Asset[])reObjs[0];
            //var types = (AssetType[])reObjs[1];
            //var labels = (AssetLabel[])reObjs[2];
            //var pjs = (Project[])reObjs[3];
            for (int i = 0; i < assets.Length; i++)
            {
                Asset asset = assets[i];
                string path = asset.FullPath;
                if (Path.GetDirectoryName(path) != Path.GetDirectoryName(InputAsset.FullPath))
                    continue;
                var hasNoExt = string.IsNullOrEmpty(Path.GetExtension(path));
                if (hasNoExt)
                    if (Directory.Exists(path))
                        Folders.Add(asset);
                    else Files.Add(asset);
                else
                    Files.Add(asset);
            }
        }

        void TakeIn()
        {

            foreach (var file in SelectedFiles)
            {
                Console.WriteLine($"{file.FullPath}  =>   {SelectedFolder.FullPath}");
            }
        } 
    }
}
