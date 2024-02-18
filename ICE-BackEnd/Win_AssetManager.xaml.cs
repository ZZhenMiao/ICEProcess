using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using LiZhenMySQL;
using ICE_Model;
using LiZhenStandard.Extensions;
using System.Diagnostics;
using LiZhenWPF;
using System.Collections.ObjectModel;

namespace ICE_BackEnd
{
    /// <summary>
    /// Win_AssetManager.xaml 的交互逻辑
    /// </summary>
    public partial class Win_AssetManager : Window
    {
        DBObjContainer<AssetLabelGroup> AssetLabelGroups { get; } = new DBObjContainer<AssetLabelGroup>();
        DBObjContainer<AssetLabel> AssetLabels { get; } = new DBObjContainer<AssetLabel>();
        DBObjContainer<AssetLabelGroup> ASS_AssetLabelGroups { get; } = new DBObjContainer<AssetLabelGroup>();
        ObservableCollection<CheckingItem> ASS_AssetLabelGroups_Show { get; } = new ObservableCollection<CheckingItem>();
        DBObjContainer<AssetType> AssetTypes { get; } = new DBObjContainer<AssetType>();
        ObservableCollection<CheckingTreeItem> AssetTypes_Show { get; } = new ObservableCollection<CheckingTreeItem>();
        DBObjContainer<Project> Projects { get; } = new DBObjContainer<Project>();
        ObservableCollection<CheckingItem> Projects_Show { get; } = new ObservableCollection<CheckingItem>();
  
        ObservableCollection<AssetDirectory> AssetDirectories { get; } = new ObservableCollection<AssetDirectory>();
        DBObjContainer<AssetLabelGroup> AssetLabelGroups_Dir { get; } = new DBObjContainer<AssetLabelGroup>();
        DBObjContainer<AssetLabel> AssetLabels_Dir { get; } = new DBObjContainer<AssetLabel>();
        ObservableCollection<CheckingItem> AssetLabel_Show_Dir { get; } = new ObservableCollection<CheckingItem>();
        DBObjContainer<AssetType> AssetTypes_Dir { get; } = new DBObjContainer<AssetType>();
        ObservableCollection<CheckingTreeItem> AssetTypes_Show_Dir { get; } = new ObservableCollection<CheckingTreeItem>();
        DBObjContainer<Project> Projects_Dir { get; } = new DBObjContainer<Project>();
        ObservableCollection<CheckingItem> Projects_Show_Dir { get; } = new ObservableCollection<CheckingItem>();

        AssetDirectory SelectedDir { get => (AssetDirectory)AssetDirectory_TreeView.SelectedItem; }
        AssetLabelGroup SelectedLabelGroup_Dir { get => (AssetLabelGroup)Ass_AutoAS_LabelGroup_ListView.SelectedItem; }
        AutoArchiveScheme AutoArchiveScheme { get; set; }
        AssetLabelGroup SelectedLabelGroup { get => (AssetLabelGroup)AssetLabelGroups_ListView.SelectedItem; }
        AssetLabel SelectedLabel { get => (AssetLabel)AssetLabels_ListView.SelectedItem; }

        public Win_AssetManager()
        {
            InitializeComponent();
            LoadLabelGroups();
            this.AssetLabelGroups_ListView.SelectionChanged += (obj, e) => 
            {
                LoadLabelBySelectedGroup();
                LoadProjectsBySelectedLabelGroup();
                LoadAssettypeBySelectedLabelGroup();
            };
            this.AssetLabels_ListView.SelectionChanged += (obj, e) => 
            {
                LoadAss_AssetLabelGroupsBySelectedLabel();
            };
            this.TabControl.SelectionChanged += (obj, e) =>
             {
                 System.Collections.IList items = e.AddedItems;
                 if (items.Count < 1)
                     return;
                 var item = items[0];
                 if (item is not TabItem)
                     return;
                 TabItem tabitem = (TabItem)item;
                 if (tabitem != AssetDirectory_TabItem)
                     return;
                 LoadAssetDirectories();
             };
            this.AssetDirectory_TreeView.SelectedItemChanged += (obj, e) =>
            {
                if (SelectedDir is null)
                    return;
                AutoArchiveScheme = DataBase.LoadFromDB_Where<AutoArchiveScheme>($"id_assetdirectory = {SelectedDir.ID}").FirstOrDefault();
                if (AutoArchiveScheme is null)
                {
                    AutoArchiveScheme = new AutoArchiveScheme() { ID_AssetDirectory = SelectedDir.ID }; 
                }
                LoadLabelGroups_Dir();
                LoadLabels_Dir();
                LoadAssetType_Dir();
            };
            this.Ass_AutoAS_LabelGroup_ListView.SelectionChanged += (obj, e) =>
            {
                LoadLabels_Dir();
            };
        }
        void LoadAssetType_Dir()
        {
            AssetTypes_Show_Dir.Clear();
            AssetType[] all = DBTree<AssetType>.LoadRootsFromDB(LoadChildren: true);
            CheckingTreeItem[] treeAll = CheckingTreeItem.MakeTreeItem(all);
            AssetTypes_Show_Dir.AddRange(treeAll);
            Extensions.AutoSetBinding(Ass_AutoAS_AssetType_TreeView, AssetTypes_Show_Dir, null);
            if (AutoArchiveScheme.ID is null)
                return;
            AssetTypes_Dir.LoadFromDB_Chain(AutoArchiveScheme);
            SelectTreeItem(AssetTypes_Dir, AssetTypes_Show_Dir);
        }
        void LoadLabels_Dir()
        {
            AssetLabel_Show_Dir.Clear();
            if (SelectedLabelGroup_Dir is null)
                return;
            AssetLabel[] all = DataBase.LoadFromDB_Where<AssetLabel>("id in(select id_assetlabel from ass_assetlabel_assetlabelgroup where id_assetlabelgroup = {0})",SelectedLabelGroup_Dir.ID);
            AssetLabel_Show_Dir.AddRange(from a in all select new CheckingItem() { Content = a });
            Extensions.AutoSetBinding(Ass_AutoAS_Label_ListView, AssetLabel_Show_Dir,null);
            if (AutoArchiveScheme.ID is null)
                return;
            AssetLabels_Dir.LoadFromDB_Chain(AutoArchiveScheme);
            foreach (CheckingItem Item in AssetLabel_Show_Dir)
            {
                AssetLabel finded = AssetLabels_Dir.Find(a => a.ID.Equals(Item.Content.ID));
                if (finded is not null)
                    Item.IsSelected = true;
            }
        }
        void LoadLabelGroups_Dir()
        {
            if (AssetLabelGroups_Dir.Any())
                return;
            this.AssetLabelGroups_Dir.LoadFromDB_Where(null);
            Extensions.AutoSetBinding(this.Ass_AutoAS_LabelGroup_ListView,this.AssetLabelGroups_Dir,null);
        }
        void LoadAssetDirectories()
        {
            AssetDirectory[] loaded = DBTree<AssetDirectory>.LoadRootsFromDB(LoadChildren: true);
            AssetDirectories.Clear();
            this.AssetDirectories.AddRange(loaded);
            Extensions.AutoSetBinding(AssetDirectory_TreeView,this.AssetDirectories,null);
        }
        void LoadLabelGroups()
        {
            AssetLabelGroups.LoadFromDB_Where(null);
            Extensions.AutoSetBinding(AssetLabelGroups_ListView, AssetLabelGroups, null);
        }
        void LoadLabelBySelectedGroup()
        {
            IDbObject obj = (IDbObject)AssetLabelGroups_ListView.SelectedItem;
            if (obj.IsNotNull())
                AssetLabels.LoadFromDB_Chain(obj);
            Extensions.AutoSetBinding(AssetLabels_ListView, AssetLabels, null);
        }
        void LoadAss_AssetLabelGroupsBySelectedLabel()
        {
            ASS_AssetLabelGroups_Show.Clear();

            AssetLabel obj = (AssetLabel)AssetLabels_ListView.SelectedItem;
            if (obj is null)
                return;

            ASS_AssetLabelGroups.LoadFromDB_Where($"id in (select id_assetlabelgroup from ass_assetlabelgroup_assetlabel where id_assetlabel = {obj.ID})");
            ASS_AssetLabelGroups_Show.AddRange(from x in AssetLabelGroups select new CheckingItem() { Content = x } );

            DBObjContainer<AssetLabelGroup> fathers = new DBObjContainer<AssetLabelGroup>();
            fathers.LoadFromDB_Chain(obj);
            List<CheckingItem>  fatherItems = new List<CheckingItem>();
            foreach (AssetLabelGroup father in fathers)
            {
                CheckingItem fatherItem = ASS_AssetLabelGroups_Show.FirstOrDefault(a=>a.Content.ID.Equals(father.ID));
                if (fatherItem.IsNotNull())
                    fatherItem.IsEnabled = false;
            }

            foreach (AssetLabelGroup item in ASS_AssetLabelGroups)
            {
                CheckingItem finded = ASS_AssetLabelGroups_Show.FirstOrDefault(a => a.Content.ID.Equals(item.ID));
                if (finded.IsNotNull())
                    finded.IsSelected = true;
            }

            SortItem(ASS_AssetLabelGroups_Show);
            Extensions.AutoSetBinding(ASS_AssetLabelGroups_ListView, ASS_AssetLabelGroups_Show, null);
        }
        void LoadAssettypeBySelectedLabelGroup()
        {
            AssetTypes_Show.Clear();
            AssetLabelGroup obj = (AssetLabelGroup)AssetLabelGroups_ListView.SelectedItem;
            if (obj is null)
                return;

            AssetTypes.LoadFromDB_Chain(obj);
            //AssetType[] allAssetTypes = DBTree<AssetType>.LoadRootsFromDB(LoadChildren:true);
            AssetType[] assetTypes = DataBase.LoadFromDB_Where<AssetType>();
            AssetType[] allAssetTypes = IDBTree<AssetType>.MakeTree(assetTypes);
            AssetTypes_Show.AddRange(CheckingTreeItem.MakeTreeItem(allAssetTypes));

            //foreach (AssetType item in AssetTypes)
            //{
            //    CheckingItem finded = AssetTypes_Show.FirstOrDefault(a => a.Content.ID.Equals(item.ID));
            //    if (finded.IsNotNull())
            //        finded.IsSelected = true;
            //}
            SelectTreeItem(AssetTypes, AssetTypes_Show);
        
            SortItem(AssetTypes_Show);
            
            Assettypes_ListView.IsEnabled = !obj.AllAssetType;
            if (obj.AllAssetType)
                SelectAllItem(AssetTypes_Show);
           
            Extensions.AutoSetBinding(Assettypes_ListView, AssetTypes_Show, null);
        }

        void LoadProjectsBySelectedLabelGroup()
        {
            Projects_Show.Clear();
            AssetLabelGroup obj = (AssetLabelGroup)AssetLabelGroups_ListView.SelectedItem;
            if (obj is null)
                return;

            Projects.LoadFromDB_Chain(obj);
            DBObjContainer<Project> allProjects = new DBObjContainer<Project>();
            allProjects.LoadFromDB_Where(null);
            Projects_Show.AddRange(from x in allProjects select new CheckingItem() { Content = x });
            foreach (Project item in Projects)
            {
                CheckingItem finded = Projects_Show.FirstOrDefault(a => a.Content.ID.Equals(item.ID));
                if (finded.IsNotNull() || obj.AllProject)
                    finded.IsSelected = true;
            }
            SortItem(Projects_Show);
           
            Projects_ListView.IsEnabled = !obj.AllProject;
            if (obj.AllProject)
                SelectAllItem(Projects_Show); 

            Extensions.AutoSetBinding(Projects_ListView, Projects_Show, null);
        }

        void SelectAllItem<T>(ObservableCollection<T> checkingItems)where T: ICheckingItem
        {
            foreach (T item in checkingItems)
            {
                item.IsSelected = true;
            }
        }
        void SortItem<T>(ObservableCollection<T> checkingItems) where T: ICheckingItem
        {
            for (int i = 0; i < checkingItems.Count; i++)
            {
                ICheckingItem item = checkingItems[i];
                if (item.IsSelected)
                    checkingItems.Move(i, 0);
            }
        }
        void SelectTreeItem<T>(IEnumerable<T> ts, ObservableCollection<CheckingTreeItem> treeItems) where T:IDbObject
        {
            foreach (CheckingTreeItem treeitem in treeItems)
            {
                T finded = ts.FirstOrDefault(a => a.ID.Equals(treeitem.Content?.ID));
                if (finded.IsNotNull())
                    treeitem.IsSelected = true;
                if (treeitem.Children.Any())
                    SelectTreeItem(ts, treeitem.Children);
            }
        }

        //List<CheckingTreeItem> MakeTreeItem<T>(IEnumerable<T> tree)where T:DBTree<T>
        //{
        //    List<CheckingTreeItem> re = new List<CheckingTreeItem>();
        //    foreach (T item in tree)
        //    {
        //        CheckingTreeItem treeItem = new CheckingTreeItem() { Content = item };
        //        re.Add(treeItem);
        //        if (item.Children.Any())
        //            treeItem.Children.AddRange(MakeTreeItem(item.Children));
        //    }
        //    return re;
        //}

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            ICheckingItem item = (ICheckingItem)cb.Content;
            var prt = Extensions.GetVisualTreeParent((DependencyObject)sender,typeof(ListView));
            if(prt is null)
                prt = Extensions.GetVisualTreeParent((DependencyObject)sender, typeof(TreeView));

            if (prt == ASS_AssetLabelGroups_ListView || prt == Assettypes_ListView || prt == Projects_ListView)
                CheckOrUncheckItem_LabelOption(item);
            else if (prt == Ass_AutoAS_Label_ListView || prt == Ass_AutoAS_AssetType_TreeView || prt == Ass_AutoAS_Project_TreeView)
                CheckOrUncheckItem_DirectoryOption(item);
        }
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            ICheckingItem item = (ICheckingItem)cb.Content;
            var prt = Extensions.GetVisualTreeParent((DependencyObject)sender, typeof(ListView));
            if (prt is null)
                prt = Extensions.GetVisualTreeParent((DependencyObject)sender, typeof(TreeView));

            if (prt == ASS_AssetLabelGroups_ListView || prt == Assettypes_ListView || prt == Projects_ListView)
                CheckOrUncheckItem_LabelOption(item);
            else if (prt == Ass_AutoAS_Label_ListView || prt == Ass_AutoAS_AssetType_TreeView || prt == Ass_AutoAS_Project_TreeView)
                CheckOrUncheckItem_DirectoryOption(item);
        }
        private void CheckOrUncheckItem_LabelOption(ICheckingItem item)
        {
            IDbObject obj = item.Content;
            bool check = item.IsSelected;
            if (obj is AssetLabelGroup a)
            {
                MySql.Data.MySqlClient.MySqlConnection con = DataBase.GetMySqlConnection(typeof(AssetLabelGroup));
                if (check)
                {
                    DataBase.CommandNonQuery($"insert into `ass_assetlabelgroup_assetlabel`(`id_assetlabel`,`id_assetlabelgroup`) values ({SelectedLabel.ID},{obj.ID})", con);
                    
                }
                else
                {
                    DataBase.CommandNonQuery($"delete from `ass_assetlabelgroup_assetlabel` where id_assetlabel = {SelectedLabel.ID} and id_assetlabelgroup = {obj.ID}", con);
                }
            }
            if (obj is AssetType b)
            {
                if (check)
                {
                    DataBase.InsertToDB_Chain(SelectedLabelGroup, obj);
                }
                else
                {
                    DataBase.DeleteFromDB_Chain(SelectedLabelGroup, obj);
                }
            }
            if (obj is Project c)
            {
                if (check)
                {
                    DataBase.InsertToDB_Chain(SelectedLabelGroup, obj);
                }
                else
                {
                    DataBase.DeleteFromDB_Chain(SelectedLabelGroup, obj);
                }
            }
        }
        private void CheckOrUncheckItem_DirectoryOption(ICheckingItem item)
        {
            if (AutoArchiveScheme is null)
                return;
            if (AutoArchiveScheme.ID is null)
                AutoArchiveScheme.InsertThisToDB();
            Debug.WriteLine(AutoArchiveScheme.ID);
            IDbObject obj = item.Content;
            bool check = item.IsSelected;
            if (obj is AssetLabel a)
            {
                if (check)
                {
                    DataBase.InsertToDB_Chain(AutoArchiveScheme, obj);
                }
                else
                {
                    DataBase.DeleteFromDB_Chain(AutoArchiveScheme, obj);
                }
            }
            if (obj is AssetType b)
            {
                if (check)
                {
                    DataBase.InsertToDB_Chain(AutoArchiveScheme, obj);
                }
                else
                {
                    DataBase.DeleteFromDB_Chain(AutoArchiveScheme, obj);
                }
            }
            if (obj is Project c)
            {
                if (check)
                {
                    DataBase.InsertToDB_Chain(AutoArchiveScheme, obj);
                }
                else
                {
                    DataBase.DeleteFromDB_Chain(AutoArchiveScheme, obj);
                }
            }
        }

        private void CheckBox_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            CheckBox obj = (CheckBox)sender;
            IDbObject content = ((ICheckingItem)obj.Content).Content;
            if (content is AssetLabelGroup)
            {
                DBObjContainer<AssetLabel> labels = new DBObjContainer<AssetLabel>();
                labels.LoadFromDB_Chain(content);
                obj.ToolTip = labels.AllToString();
            }
        }
    }
}
