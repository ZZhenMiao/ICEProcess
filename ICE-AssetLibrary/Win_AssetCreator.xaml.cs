using ICE_BackEnd;
using ICE_Model;
using LiZhenMySQL;
using LiZhenStandard.Extensions;
using LiZhenStandard.IO;
using LiZhenStandard.Sockets;
using LiZhenWPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using Path = System.IO.Path;
using Task = System.Threading.Tasks.Task;

namespace ICE_AssetLibrary
{
    /// <summary>
    /// Win_AssetCreator.xaml 的交互逻辑
    /// </summary>
    public partial class Win_AssetCreator : Window
    {
        List<CheckingTreeItem> _AllProjects_NoTree { get; } = new List<CheckingTreeItem>();
        List<CheckingTreeItem> _AllAssetTypes_NoTree { get; } = new List<CheckingTreeItem>();
        List<AssetFileMd5Info> AssetFileMd5Infos { get; } = new List<AssetFileMd5Info>();

        ObservableCollection<CheckingTreeItem> AllProjects { get; } = new ObservableCollection<CheckingTreeItem>();
        ObservableCollection<CheckingTreeItem> AllAssetTypes { get; } = new ObservableCollection<CheckingTreeItem>();
        ObservableCollection<AssetLabelGroup> LabelGroups { get; } = new ObservableCollection<AssetLabelGroup>();
        ObservableCollection<AssetLabel> Labels { get; } = new ObservableCollection<AssetLabel>();
        ObservableCollection<AssetLabel> SelectedLabels { get; } = new ObservableCollection<AssetLabel>();
        ObservableCollection<AssetLabel> LabelTip { get; } = new ObservableCollection<AssetLabel>();
        ObservableCollection<AssetDirectoryTreeItem> TargetPaths { get; } = new ObservableCollection<AssetDirectoryTreeItem>();

        bool? ForAllProjects { get => AllProjects_CheckBox.IsChecked; set => AllProjects_CheckBox.IsChecked = value; }
        bool? ShowAllLabelGroups { get => ShowAllLabelGroups_CheckBox.IsChecked; set => ShowAllLabelGroups_CheckBox.IsChecked = value; }
        bool? AutoArchive { get => AutoArchive_CheckBox.IsChecked; set => AutoArchive_CheckBox.IsChecked = value; }
        string[] SourcePaths { get; set; }
        bool CanClose { get; set; } = false;
        bool MoveFile { get { return this.MoveFile_RadioButton.IsChecked.Value; } }

        public Win_AssetCreator(string[] sourceFiles, AssetFileMd5Info[] md5infos, AssetLabel[] labels = null, Project[] projects = null, AssetType[] assetTypes = null)
        {
            InitializeComponent();
            SetEvents();
            LoadAllProjectsAndAssetTypes();
            SetBindings();
            LoadLabelGroups();
            var paths = sourceFiles;//(from info in md5infos select info.SourceAssetDir).GroupBy(x=>x).Select(group=>group.Key).ToArray();
            InitializeItems(paths, labels, projects, assetTypes);
            this.AssetFileMd5Infos.AddRange(md5infos);
            this.AllProjects_CheckBox.IsChecked = true;
            if (OnlyFileInput())
            {
                this.PushIntoFolder_CheckBox.IsEnabled = true;
                this.PushIntoFolder_CheckBox.IsChecked = true;
            }
        }
        public Win_AssetCreator(AssetFileInfo[] assetFileInfos)
        {
            InitializeComponent();
            this.Title = "修改资产类型和标签";
            SetEvents();
            LoadAllProjectsAndAssetTypes();
            SetBindings();
            this.AllProjects_CheckBox.IsChecked = true;
            LoadAssetInfo(assetFileInfos);
            LoadLabelGroups();
        }

        void LoadAssetInfo(AssetFileInfo[] assetFileInfos)
        {
            InitializeComponent();
            AssetFileInfo first = assetFileInfos.FirstOrDefault();
            this.AutoArchive = false;
            this.AutoArchive_CheckBox.IsEnabled = false;

            AssetType[] ts = new AssetType[0];
            Project[] ps = new Project[0];
            AssetLabel[] ls = new AssetLabel[0];

            if (first is not null)
            {
                SocketFunction.SendInstruct(App.Socket, "LoadAssetInfo", new object[] { first?.ID_Asset }, out object[] results, out Exception expj);
                ts = (AssetType[])results[0];
                ps = (Project[])results[1];
                ls = (AssetLabel[])results[2];
            }
            InitializeItems((from a in assetFileInfos select a.FullName).ToArray(), ls, ps, ts);
        }
        
        bool OnlyFileInput()
        {
            foreach (var path in SourcePaths)
            {
                if (string.IsNullOrEmpty(Path.GetExtension(path)))
                    if (Directory.Exists(path))
                        return false;
            }
            return true;
        }
        //获取自动命名生成的名称
        string GetAutoName()
        {
            IEnumerable<string> projects = from item in _AllProjects_NoTree where item.IsSelected select ((Project)item.Content).Name;
            IEnumerable<string> assetTypes = from item in _AllAssetTypes_NoTree where item.IsSelected select ((AssetType)item.Content).Name;
            IEnumerable<string> labels = from label in SelectedLabels select label.Name;

            var str_pj = projects.AllToString();
            var str_tp = assetTypes.AllToString();
            var str_lb = labels.AllToString();

            var re = "";
            if (!string.IsNullOrEmpty(str_pj))
                re += str_pj + "-";
            re += $"{str_tp}-{str_lb}";

            return re;
        }
        //找到相似（类型、标签、项目和所在资产目录均相同）的资产文件夹
        string[] GetSemblableFolder()
        {
            IEnumerable<ulong> assetTypeIDs = from item in _AllAssetTypes_NoTree where item.IsSelected select Convert.ToUInt64(((AssetType)item.Content).ID);
            IEnumerable<ulong> labelIDs = from label in SelectedLabels select Convert.ToUInt64(label.ID);
            IEnumerable<ulong> projectIDs = from item in _AllProjects_NoTree where item.IsSelected select Convert.ToUInt64(((Project)item.Content).ID);
            ulong? diretoryID = ((AssetDirectoryTreeItem)TargetPath_ComboBox.SelectedItem)?.LastDirID;

            SocketFunction.SendInstruct(App.Socket, "FindSimilarityAssets_ByInfos", new object[] { assetTypeIDs.ToArray(), labelIDs.ToArray(), projectIDs.ToArray(), diretoryID }, out object[] reObjs, out Exception expj);
            if (!reObjs.Any())
                return new string[0];
            if (reObjs is not Asset[])
                return new string[0];
            var assets = (Asset[])reObjs;

            List<Asset> folders = new List<Asset>();
            foreach (var asset in assets)
            {
                if (asset.IsFolder == true)
                    if (Directory.Exists(asset.FullPath))
                        folders.Add(asset);
            }
            var paths = from f in folders select f.FullPath;
            return paths.ToArray();
        }
        //将所有源路径打包至一个文件夹，再将这个文件夹做为源目录。
        void Pigeonhole()
        {
            string folder = null;
            foreach (var p in SourcePaths)
            {
                if (folder is null)
                { 
                    folder = Path.Combine(Path.GetDirectoryName(p), GetAutoName()); 
                    if(!Directory.Exists(folder))
                        IO_Extensons.CreateDirectory(folder);
                    else
                    {
                        int n = 0;
                        do
                        {
                            n++;
                            folder += n.ToString();
                        }while(Directory.Exists(folder));
                        IO_Extensons.CreateDirectory(folder);
                    }
                }
                try
                {
                    File.Move(p, Path.Combine(folder, Path.GetFileName(p)));
                }
                catch (Exception e) { Console.WriteLine(e); }
            }
            this.SourcePaths = new string[] { folder };
        }

        void InitializeItems(string[] sourcePaths, AssetLabel[] labels, Project[] projects, AssetType[] assetTypes)
        {
            this.SourcePaths = sourcePaths;
            if (labels is not null)
                this.SelectedLabels.AddRange(labels);
            if (projects is not null)
            {
                IEnumerable<ulong> inputIDs = from ipj in projects select Convert.ToUInt64(ipj.ID);
                foreach (CheckingTreeItem pj in _AllProjects_NoTree)
                {
                    if (inputIDs.Contains(Convert.ToUInt64(pj.Content.ID)))
                        pj.IsSelected = true;
                }
            }
            if (assetTypes is not null)
            {
                IEnumerable<ulong> inputIDs = from iat in assetTypes select Convert.ToUInt64(iat.ID);
                foreach (CheckingTreeItem at in _AllAssetTypes_NoTree)
                {
                    if (inputIDs.Contains(Convert.ToUInt64(at.Content.ID)))
                        at.IsSelected = true;
                }
            }
            SourcePaths_TextBox.Text = SourcePaths.AllToString(separator: "\r");
            MatchAssetDirectory();
        }
        void LoadAllProjectsAndAssetTypes()
        {
            this.AllAssetTypes.Clear();
            this._AllAssetTypes_NoTree.Clear();
            this.AllProjects.Clear();
            this._AllProjects_NoTree.Clear();

            SocketFunction.SendInstruct(App.Socket, "LoadAllProjectTree", null, out object[] resultPjs, out Exception expj);
            IEnumerable<Project> allPjs = (IEnumerable<Project>)resultPjs;
            CheckingTreeItem[] pjs = CheckingTreeItem.MakeTreeItem(allPjs);
            foreach (CheckingTreeItem pjtree in pjs)
            {
                _AllProjects_NoTree.AddRange(pjtree.GetAllTreeNode());
            }
            AllProjects.AddRange(pjs);

            SocketFunction.SendInstruct(App.Socket, "LoadAllAssetTypeTree", new object[] { (bool)AutoArchive }, out object[] resultATs, out Exception exat);
            IEnumerable<AssetType> allats = (IEnumerable<AssetType>)resultATs;
            CheckingTreeItem[] ats = CheckingTreeItem.MakeTreeItem(allats);
            foreach (CheckingTreeItem attrees in ats)
            {
                _AllAssetTypes_NoTree.AddRange(attrees.GetAllTreeNode());
            }
            AllAssetTypes.AddRange(ats);
        }
        void MatchAssetDirectory()
        {
            TargetPaths.Clear();
            if (!(AutoArchive == true))
                return;

            IEnumerable<ulong> types = from item in _AllAssetTypes_NoTree where item.IsSelected select Convert.ToUInt64(item.Content.ID);
            IEnumerable<ulong> pjs = from item in _AllProjects_NoTree where item.IsSelected select Convert.ToUInt64(item.Content.ID);
            IEnumerable<ulong> labels = from label in SelectedLabels select Convert.ToUInt64(label.ID);

            SocketFunction.SendInstruct(App.Socket, "MatchAssetDirectory",
                new object[]
                {
                    types.ToArray(),
                    pjs.ToArray(),
                    labels.ToArray()
                },
                out object[] resultDirs,
                out Exception ex);

            if (resultDirs.Length < 1)
                return;

            foreach (object resultDir in resultDirs)
            {
                AssetDirectory dir = (AssetDirectory)resultDir;
                Nas nas = dir.Nas;
                string path = Path_Extensons.SharedVolumeSeparator_Win(nas.IP) + Path.DirectorySeparatorChar.ToString() + dir.GetAllTreeNode().AllToString(separator: Path.DirectorySeparatorChar.ToString());
                //TargetPaths.Add(path);
                TargetPaths.Add(new AssetDirectoryTreeItem() { FullTree = dir });
            }
            if (TargetPaths.Any())
                TargetPath_ComboBox.SelectedItem = TargetPaths.FirstOrDefault();

            if (TargetPaths.Count >= 2)
            {
                TargetPath_ComboBox.Foreground = new SolidColorBrush(Color.FromRgb(160, 25, 25));
            }
            else
            {
                TargetPath_ComboBox.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            }
        }
        void LoadLabelGroups(bool onlyNecessary = false)
        {
            LabelGroups.Clear();

            if ((bool)AutoArchive && !TargetPaths.Any())
                onlyNecessary = true;

            List<CheckingTreeItem> allAssetTypeNodes = new List<CheckingTreeItem>();
            foreach (CheckingTreeItem item in AllAssetTypes)
            {
                allAssetTypeNodes.AddRange(CheckingTreeItem.GetAllNodes(item));
            }

            IEnumerable<ulong> SelectedProjectsID = from pj in AllProjects where pj.IsSelected select Convert.ToUInt64(pj.Content.ID);
            IEnumerable<ulong> SelectedAssetTypesID = from at in allAssetTypeNodes where at.IsSelected select Convert.ToUInt64(at.Content.ID);
            IEnumerable<ulong> SelectedAssetLabelsID = from lab in SelectedLabels select Convert.ToUInt64(lab.ID);

            SocketFunction.SendInstruct(App.Socket,
                "LoadLabelGroupByProjects_AssetTypes_SelectedLabels",
                new object[]
                {
                    ShowAllLabelGroups,
                    SelectedAssetTypesID.ToArray(),
                    ForAllProjects,
                    SelectedProjectsID.ToArray(),
                    SelectedAssetLabelsID.ToArray(),
                    onlyNecessary
                },
                out object[] result,
                out Exception ex);

            LabelGroups.AddRange(from re in result select (AssetLabelGroup)re);
        }
        bool SelectedAnyAssetTypes()
        {
            CheckingTreeItem finded = this._AllAssetTypes_NoTree.Find(a => a.IsSelected);
            return finded is not null;
        }
        void AutoSelectAssetType()
        {
            if (SelectedAnyAssetTypes())
                return;

            IEnumerable<ulong> ids = from label in SelectedLabels select Convert.ToUInt64(label.ID);

            SocketFunction.SendInstruct(App.Socket, "FindAssetTypesByLabels",
                new object[] { ids.ToArray(), (bool)AutoArchive },
                out object[] resultAssetTypes, out Exception expj);

            if (resultAssetTypes.Length < 1)
                return;

            if (resultAssetTypes.Length == 1)
            {
                AssetType type = (AssetType)resultAssetTypes[0];
                CheckingTreeItem findedItem = this._AllAssetTypes_NoTree.Find(a => a.Content.ID.Equals(type.ID));
                findedItem.IsSelected = true;
            }
            else if (resultAssetTypes.Length > 1)
            {
                Debug.WriteLine("找到了多个符合推断的资产类型！");
                Debug.Write(resultAssetTypes.AllToString());
            }
        }
        async Task LoadLabelTipAsync()
        {
            LabelTip.Clear();

            string input = SearchLabel_ComboBox.Text;
            object[] result = await Task.Run(() =>
            {
                SocketFunction.SendInstruct(App.Socket, "LoadAssetLabelByInputLabelName", new object[] { input }, out object[] result, out Exception e);
                return result;
            });

            if (!result.Any())
            {
                SearchLabel_ComboBox.IsDropDownOpen = false;
                return;
            }

            object r = result[0];
            if (r is Exception ex)
            {
                Debug.WriteLine(ex?.Message);
                return;
            }

            LabelTip.AddRange(from re in result select (AssetLabel)re);

            if (LabelTip.Any())
            {
                SearchLabel_ComboBox.IsDropDownOpen = true;
            }
        }
        void LoadLabels()
        {
            Labels.Clear();

            if (AssetLabelGroups_ListView.SelectedItem is null)
                return;
            AssetLabelGroup selectedGroup = (AssetLabelGroup)AssetLabelGroups_ListView.SelectedItem;
            SocketFunction.SendInstruct(App.Socket, "LoadLabelBySelectedLabelGroup", new object[] { selectedGroup.ID }, out object[] result, out Exception ex);

            Labels.AddRange(from re in result select (AssetLabel)re);
        }
        void SetBindings()
        {
            Extensions.AutoSetBinding(this.Projects_TreeView, this.AllProjects, null);
            Extensions.AutoSetBinding(this.AssetTypes_TreeView, this.AllAssetTypes, null);
            Extensions.AutoSetBinding(this.AssetLabelGroups_ListView, this.LabelGroups, null);
            Extensions.AutoSetBinding(this.AssetLabel_ListView, this.Labels, null);
            Extensions.AutoSetBinding(this.SelectedLabels_ListView, this.SelectedLabels, null);
            this.SearchLabel_ComboBox.SetBinding(ComboBox.ItemsSourceProperty, new Binding() { Source = LabelTip });
            this.TargetPath_ComboBox.SetBinding(ComboBox.ItemsSourceProperty, new Binding() { Source = TargetPaths });
            this.CopyFile_RadioButton.SetBinding(RadioButton.IsEnabledProperty, new Binding("IsChecked") { ElementName = "AutoArchive_CheckBox" });
            this.MoveFile_RadioButton.SetBinding(RadioButton.IsEnabledProperty, new Binding("IsChecked") { ElementName = "AutoArchive_CheckBox" });
        }
        void SetEvents()
        {
            this.PushIntoFolder_CheckBox.Checked += (obj, e) =>
            {
                this.AutoRename_CheckBox.IsChecked = true;
            };
            this.AllProjects_CheckBox.Checked += (obj, e) =>
            {
                this.Projects_TreeView.IsEnabled = false;
                foreach (var item in _AllProjects_NoTree)
                {
                    item.IsSelected = false;
                }
                LoadLabelGroups();
            };
            this.AllProjects_CheckBox.Unchecked += (obj, e) =>
            {
                this.Projects_TreeView.IsEnabled = true;
                LoadLabelGroups();
            };
            this.ShowAllLabelGroups_CheckBox.Checked += (obj, e) =>
            {
                LoadLabelGroups();
            };
            this.ShowAllLabelGroups_CheckBox.Unchecked += (obj, e) =>
            {
                LoadLabelGroups();
            };
            this.AssetLabelGroups_ListView.SelectionChanged += (obj, e) =>
            {
                LoadLabels();
            };
            this.AssetLabel_ListView.MouseDoubleClick += (obj, e) =>
            {
                AssetLabel selectedLabel = (AssetLabel)AssetLabel_ListView.SelectedItem;
                if (selectedLabel is null)
                    return;

                int i = AssetLabelGroups_ListView.SelectedIndex;

                SelectLabel(selectedLabel);

                Refresh();
                AssetLabelGroups_ListView.SelectedIndex = i;
                ListViewItem item = (ListViewItem)AssetLabelGroups_ListView.ItemContainerGenerator.ContainerFromIndex(i);
                item?.Focus();
            };
            this.SelectedLabels_ListView.MouseLeftButtonUp += (obj, e) =>
            {
                object selected = SelectedLabels_ListView.SelectedItem;
                if (selected is null)
                    return;
                UnSelectLabel((AssetLabel)selected);
            };
            this.SearchLabel_ComboBox.KeyDown += (obj, e) =>
            {
                AssetLabel select = (AssetLabel)SearchLabel_ComboBox.SelectedItem;
                if (select is null)
                    return;
                //if (ShowAllLabelGroups_CheckBox.IsChecked == false)
                //    ShowAllLabelGroups_CheckBox.IsChecked = true;

                SelectLabel(select);
                this.LabelTip.Clear();
                SearchLabel_ComboBox.Text = "";
            };
            this.AutoArchive_CheckBox.Checked += (obj, e) => 
            { 
                LoadAllProjectsAndAssetTypes(); 
                if(OnlyFileInput())
                    this.PushIntoFolder_CheckBox.IsEnabled = true;

            };
            this.AutoArchive_CheckBox.Unchecked += (obj, e) => { LoadAllProjectsAndAssetTypes(); this.TargetPaths.Clear();this.PushIntoFolder_CheckBox.IsEnabled = false; };
            this.Closing += (obj, e) =>
            {
                if (!this.CanClose)
                {
                    //var result = MessageBox.Show("资产归档操作尚未完成，该界面内容将会丢失，真的要取消吗？", "取消操作", MessageBoxButton.OKCancel);
                    //if (result == MessageBoxResult.Cancel)
                    //e.Cancel = true;
                }
            };
        }
        public void Refresh()
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke
                (
                DispatcherPriority.Background,
                new DispatcherOperationCallback(delegate (object f)
                {
                    ((DispatcherFrame)f).Continue = false;
                    return null;
                }),
                frame);
            Dispatcher.PushFrame(frame);
        }
        void UnSelectLabel(AssetLabel assetLabel, bool reloadLabelGroups = true)
        {
            SelectedLabels.Remove(assetLabel);
            if (reloadLabelGroups)
                LoadLabelGroups();
            MatchAssetDirectory();
        }
        void SelectLabel(AssetLabel assetLabel, bool reloadLabelGroups = true)
        {
            SocketFunction.SendInstruct(App.Socket, "LoadParentLabelsByAss_Label", new object[] { assetLabel.ID }, out object[] result, out Exception e);

            if (result.Any())
            {
                IEnumerable<AssetLabel> ass_labels = from item in result select (AssetLabel)item;
                if (!SelectedLabels.AnyContains(ass_labels.ToArray()))
                    foreach (AssetLabel asslabel in ass_labels)
                    {
                        if (!SelectedLabels.Contains(asslabel))
                            SelectedLabels.Add(asslabel);
                    }
            }

            if (!SelectedLabels.Contains(assetLabel))
            {
                SelectedLabels.Add(assetLabel);
                if (reloadLabelGroups)
                    LoadLabelGroups();
            }

            AutoSelectAssetType();
            MatchAssetDirectory();
        }

        private void Default_Button_Click(object sender, RoutedEventArgs e)
        {

        }
        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        async Task<bool> CopyFileAsync(string[] sourcePath, string targetPath, FileOpration opration)
        {
            bool re = await Task.Run(() =>
            {
                return IO_Shell32.FileOpration(sourcePath, targetPath, opration);
            });
            return re;
        }
        private bool InsertAssetMD5ToDatabase(string targetFolder)
        {
            if (!string.IsNullOrEmpty(targetFolder))//自动归档
                foreach (AssetFileMd5Info info in AssetFileMd5Infos)
                {
                    if (string.IsNullOrEmpty(info.MD5))
                        continue;
                    var str = info.SourceAssetDir == info.SourcePath ?
                        Path.DirectorySeparatorChar + Path.GetFileName(info.SourceAssetDir) :
                        info.SourcePath.Replace(Path.GetDirectoryName(info.SourceAssetDir), string.Empty);
                    var path = targetFolder + str;
                    info.Dir = Path.GetDirectoryName(path);
                    info.Name = Path.GetFileName(path);
                }
            else//不自动归档
                foreach (AssetFileMd5Info info in AssetFileMd5Infos)
                {
                    if (string.IsNullOrEmpty(info.MD5))
                        continue;
                    info.Dir = Path.GetDirectoryName(info.SourceAssetDir);
                    info.Name = Path.GetFileName(info.SourceAssetDir);
                }

            SocketFunction.SendInstruct(App.Socket, "InsertAssetFileMD5",
                AssetFileMd5Infos.ToArray(),
                out object[] result,
                out Exception ex);

            return true;
        }
        private bool InsertAssetToDataBase()
        {
            string[] sourceFiles = SourcePaths;
            IEnumerable<ulong> projectIDs = from item in _AllProjects_NoTree where item.IsSelected select Convert.ToUInt64(((Project)item.Content).ID);
            IEnumerable<ulong> assetTypeIDs = from item in _AllAssetTypes_NoTree where item.IsSelected select Convert.ToUInt64(((AssetType)item.Content).ID);
            IEnumerable<ulong> labelIDs = from label in SelectedLabels select Convert.ToUInt64(label.ID);
            ulong? diretoryID = ((AssetDirectoryTreeItem)TargetPath_ComboBox.SelectedItem)?.LastDirID;
            string targetFolder = null;
            if (AutoArchive == true)
            {
                targetFolder = TargetPath_ComboBox.SelectedItem.ToString();
            }

            //去掉可能的空目录
            List<string> filePaths = sourceFiles.ToList();
            for (int i = 0; i < filePaths.Count; i++)
            {
                string fileName = filePaths[i];
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    filePaths.RemoveAt(i);
                    i--;
                }
            }

            SocketFunction.SendInstruct(App.Socket, "InsertOrUpdateAsset",
                new object[]
                {
                    projectIDs.ToArray(),
                    assetTypeIDs.ToArray(),
                    labelIDs.ToArray(),
                    diretoryID,
                    targetFolder,
                    filePaths.ToArray(),
                    App.Me.ID
                },
                out object[] result,
                out Exception ex);

            if (!result.Any())
                return false;
            object re = result[0];
            if (re is Exception exre)
            { MessageBox.Show(exre.Message); return false; }

            InsertAssetMD5ToDatabase(targetFolder);

            //Asset[] re_assets = (Asset[])re;
            //string str = "模拟向数据库插入如下资产：\r";
            //foreach (Asset asset in re_assets)
            //{
            //    str += asset.ShowAllProperties();
            //    str += "\r\r";
            //}
            //MessageBox.Show(str);

            return true;
        }

        bool ReTry { get; set; }
        bool CheckBoxEventEnabled = true;
        private void AutoRename()
        {
            if (this.AutoRename_CheckBox.IsChecked == false)
                return;
            var newStr = GetAutoName();
            for (int i = 0; i < SourcePaths.Length; i++)
            {
                string path = SourcePaths[i];
                var newPath = Path.Combine(Path.GetDirectoryName(path), newStr + "_" + Path.GetFileName(path));
                try
                {
                    File.Move(path, newPath, true);
                    SourcePaths[i] = newPath;
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }
        private async Task<bool?> AskSemblableFolder_SYNC()
        {
            if (!OnlyFileInput())
                return null;
            var folders = GetSemblableFolder();
            if(!folders.Any())
                return null;
            var win = new Win_SemblableFolderSelector(folders);
            win.ShowDialog();
            var folder = win.ReFolder;
            win.Close();
            if (folder is null)
                return null;
            
            Console.WriteLine("正在将如下资产移动至：" + folder);
            Console.WriteLine(SourcePaths.AllToString(separator: "\r"));
            return await CopyFileAsync(SourcePaths, folder, MoveFile ? FileOpration.Move : FileOpration.Copy);
        }
        private async Task<bool> Present()
        {
            ReTry = false;
            IEnumerable<IDbObject> types = from item in _AllAssetTypes_NoTree where item.IsSelected select item.Content;
            if (!types.Any())
            {
                MessageBox.Show("请至少选择一种资产类型。", "资产归档异常");
                return false;
            }
            if (types.Count() > 4)
            {
                MessageBox.Show("请确定该资产是否同时属于已选中的多种资产类型，请不要为资产关联多余的资产类型。", "温馨提示");
            }
            if (AutoArchive == true && TargetPath_ComboBox.SelectedItem is null)
            {
                MessageBox.Show("无法根据当前选择的标签准确推断出归档目录，请为该资产打上满足自动归档所必要的标签，并在下拉列表中选择可能的归档目录。", "自动归档异常");
                return false;
            }
            string targetFolder = TargetPath_ComboBox.SelectedItem?.ToString();
            AutoRename();
            if (AutoArchive == true)
            {
                var toSF = await AskSemblableFolder_SYNC();
                if(toSF is not null)
                    return (bool)toSF;

                if (PushIntoFolder_CheckBox.IsChecked == true)
                    Pigeonhole();

                Console.WriteLine("正在将如下资产移动至：" + targetFolder);
                Console.WriteLine(SourcePaths.AllToString(separator: "\r"));
                bool re = await CopyFileAsync(SourcePaths, targetFolder, MoveFile ? FileOpration.Move : FileOpration.Copy);
                if (re)
                    return InsertAssetToDataBase();
                else
                {
                    MessageBoxResult reTry_box = MessageBox.Show("资产归档过程出现异常，若非用户主动取消操作，请检查目标目录可操作性。例如网络连接，权限，剩余空间等问题。是否重试？", "自动归档异常", MessageBoxButton.YesNo);
                    this.Dispatcher.Invoke(() => { this.IsEnabled = false; });
                    if (reTry_box == MessageBoxResult.Yes)
                        ReTry = true;
                    return false;
                }
            }
            else
                return InsertAssetToDataBase();
        }
        private void Lock(bool lockOrUnlock)
        {
            this.IsEnabled = !lockOrUnlock;
            //this.Cancel_Button.IsEnabled = !lockOrUnlock;
            //this.Present_Button.IsEnabled = !lockOrUnlock;
        }
        private async void Present_Button_Click(object sender, RoutedEventArgs e)
        {
            Lock(true);
            bool result = await Present();
            if (ReTry)
            {
                while (ReTry)
                {
                    result = await Present();
                }
            }
            Lock(false);
            if (result)
            {
                this.CanClose = true;
                this.Close();
            }
        }
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!CheckBoxEventEnabled)
                return;
            CheckingTreeItem Content = (CheckingTreeItem)((CheckBox)sender).Content;

            CheckBoxEventEnabled = false;

            IEnumerable<CheckingTreeItem> parents = Content.GetParentNodes();
            foreach (CheckingTreeItem node in parents)
            {
                node.IsSelected = true;
            }
            IEnumerable<CheckingTreeItem> childs = Content.GetAllChildren();
            foreach (var child in childs)
            {
                child.IsSelected = true;
            }

            CheckBoxEventEnabled = true;

            LoadLabelGroups();
            MatchAssetDirectory();
        }
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!CheckBoxEventEnabled)
                return;
            CheckingTreeItem Content = (CheckingTreeItem)((CheckBox)sender).Content;

            CheckBoxEventEnabled = false;

            IEnumerable<CheckingTreeItem> childs = Content.GetAllChildren();
            foreach (var child in childs)
            {
                child.IsSelected = false;
            }

            CheckBoxEventEnabled = true;

            var father = Content.Parent;
            if (father is not null)
                if (!father.Where(x => x.isSelected).Any())
                    father.IsSelected = false;

            LoadLabelGroups();
            MatchAssetDirectory();
        }

        private string _lastinput = null;
        private void SearchLabel_ComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!e.Changes.Any())
                return;
            if (this.SearchLabel_ComboBox.Text.Equals(_lastinput))
                return;

            _lastinput = this.SearchLabel_ComboBox.Text;

            if (string.IsNullOrWhiteSpace(_lastinput))
            {
                SearchLabel_ComboBox.SelectedIndex = -1;
                return;
            }

            if (SearchLabel_ComboBox.SelectedIndex == -1)
            {
                _ = LoadLabelTipAsync();
            }
        }
    }

    public class AssetDirectoryTreeItem
    {
        public ulong LastDirID
        {
            get
            {
                return Convert.ToUInt64(FullTree?.GetAllTreeNode()?.LastOrDefault()?.ID);
            }
        }
        public AssetDirectory FullTree { get; set; }
        public Nas Nas { get => FullTree.Nas; }

        public string Name { get => ToString(); }
        public override string ToString()
        {
            string path = Path_Extensons.SharedVolumeSeparator_Win(Nas.IP) + Path.DirectorySeparatorChar.ToString() + FullTree.GetAllTreeNode().AllToString(separator: Path.DirectorySeparatorChar.ToString());
            return path;
        }
    }

}
