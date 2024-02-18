using ICE_Model;
using LiZhenStandard.Extensions;
using LiZhenStandard.Sockets;
using LiZhenWPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace ICE_AssetLibrary
{
    /// <summary>
    /// PTSelector.xaml 的交互逻辑
    /// </summary>
    public partial class PTSelector : Window
    {
        ObservableCollection<CheckingTreeItem> AllProjects { get; } = new ObservableCollection<CheckingTreeItem>();
        ObservableCollection<CheckingTreeItem> AllAssetTypes { get; } = new ObservableCollection<CheckingTreeItem>();

        public List<Project> SelectedProjects { get; } = new List<Project>();
        public List<AssetType> SelectedAssetTypes { get; } = new List<AssetType>();

        public bool Cancel { get; private set; } = true;

        //ObservableCollection<ulong> SelectedIDs { get; } = new ObservableCollection<ulong>();
        //public ulong[] Result { get; set; }

        public PTSelector(Type type)
        {
            InitializeComponent();
            LoadItem(type);
            SetBinding(type);
        }

        void LoadItem(Type type)
        {
            if (type == typeof(Project))
            {
                SocketFunction.SendInstruct(App.Socket, "LoadAllProjectTree", null, out object[] results, out Exception ex);
                //IEnumerable<CheckingTreeItem> all = from project in results select new CheckingTreeItem() { Content = (Project)project };
                var pjs = CheckingTreeItem.MakeTreeItem<Project>((IEnumerable<Project>)results);
                AllProjects.AddRange(pjs);
            }
            else if (type == typeof(AssetType))
            {
                SocketFunction.SendInstruct(App.Socket, "LoadAllAssetTypeTree", null, out object[] results, out Exception ex);
                var ats = CheckingTreeItem.MakeTreeItem<AssetType>((IEnumerable<AssetType>)results);
                AllAssetTypes.AddRange(ats);
            }
        }

        void SetBinding(Type type)
        {
            if (type == typeof(Project))
            {
                Extensions.AutoSetBinding(Container_TreeView, AllProjects, null);
            }
            else if (type == typeof(AssetType))
            {
                Extensions.AutoSetBinding(Container_TreeView, AllAssetTypes, null);
            }
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void Apply_Button_Click(object sender, RoutedEventArgs e)
        {
            List<CheckingTreeItem> pjs = new List<CheckingTreeItem>();
            foreach (var pj in AllProjects)
            {
                pjs.AddRange(CheckingTreeItem.GetAllNodes(pj));
            }

            List<CheckingTreeItem> ats = new List<CheckingTreeItem>();
            foreach (var at in AllAssetTypes)
            {
                ats.AddRange(CheckingTreeItem.GetAllNodes(at));
            }

            SelectedProjects.AddRange(from pj in pjs.Where(a => a.IsSelected) select (Project)pj.Content);
            SelectedAssetTypes.AddRange(from at in ats.Where(a => a.IsSelected) select (AssetType)at.Content);
            this.Cancel = false;
            this.Hide();
        }
    }
}
