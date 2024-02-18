using ICE_Model;
using LiZhenStandard.Extensions;
using LiZhenStandard.IO;
using LiZhenStandard.Sockets;
using LiZhenWPF;
using Microsoft.WindowsAPICodePack.Dialogs;
using Org.BouncyCastle.Crmf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;
using Extensions = LiZhenWPF.Extensions;
using Path = System.IO.Path;
using Task = System.Threading.Tasks.Task;
using Type = System.Type;

namespace ICE_AssetLibrary
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        Person Me { get => App.Me; }
        ObservableCollection<QueryScheme> QuerySchemes { get; } = new ObservableCollection<QueryScheme>();
        QueryScheme ActiveQueryScheme { get; set; }
        ObservableCollection<AssetLabelGroup> LabelGroups { get; } = new ObservableCollection<AssetLabelGroup>();
        ObservableCollection<AssetLabel> Labels { get; } = new ObservableCollection<AssetLabel>();
        ObservableCollection<Asset> Assets { get; } = new ObservableCollection<Asset>();
        ObservableCollection<AssetFileInfo> Files { get; } = new ObservableCollection<AssetFileInfo>();
        ObservableCollection<AssetFileInfo> Files_Page { get; } = new ObservableCollection<AssetFileInfo>();

        List<CheckingTreeItem> _AllProjects_NoTree { get; } = new List<CheckingTreeItem>();
        List<CheckingTreeItem> _AllAssetTypes_NoTree { get; } = new List<CheckingTreeItem>();
        ObservableCollection<CheckingTreeItem> AllProjects { get; } = new ObservableCollection<CheckingTreeItem>();
        ObservableCollection<CheckingTreeItem> AllAssetTypes { get; } = new ObservableCollection<CheckingTreeItem>();

        AssetUsageLogs AssetUsageLogs { get; } = new AssetUsageLogs();

        private bool useCache = true;
        public bool UseCache
        {
            get => useCache; set
            {
                useCache = value;
                OnPropertyChanged(nameof(UseCache));
            }
        }
        bool StopQueryProcess = true;
        bool StopSetMD5Process = true;
        int Page { get; set; } = 1;
        int NumItemsPerPage
        {
            get
            {
                return Dispatcher.Invoke(() =>
                {
                    switch (NumItemsPerPage_ComboBox.SelectedIndex)
                    {
                        case -1: return 50;
                        case 0: return 50;
                        case 1: return 100;
                        case 2: return 200;
                        case 3: return 500;
                        case 4: return 1000;
                        default: return 50;
                    }
                });
            }
        }

        AssetShowMode AssetShowMode
        {
            get
            {
                if ((bool)this.IconMode_RadioButton.IsChecked)
                    return AssetShowMode.Icon;
                if ((bool)this.ListMode_RadioButton.IsChecked)
                    return AssetShowMode.List;
                return AssetShowMode.Icon;
            }
        }
        Queue<CancellationTokenSource> TokenSources { get; } = new Queue<CancellationTokenSource>();
        double _size = 0;
        bool _exit = true;
        public double AssetIconWidth
        {
            get { return _size * 10; }
            set
            {
                _size = value;
            }
        }
        public double AssetIconHeight
        {
            get { return _size * 10 * (9 / 16); }
            set { _size = value; }
        }
        public double AssetFontSize
        {
            get { return _size; }
            set
            {
                _size = value;
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs("AssetIconWidth"));
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs("AssetIconHeight"));
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs("AssetFontSize"));
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            LoadUniversualExtensions();
            LoadQuerySchemes();
            this.AssetUsageLogs.LoadFromFile();
            LoadLabelGroups();
            LoadAllProjectsAndAssetTypes();

            _ = QueryAssets_Async();

            //BindingOperations.EnableCollectionSynchronization(Files, new object());
            SetBinding();
            SetEvents();
            LoadHead();
            SetAssetListViewItemTemplate();
            BigMapQueryTimer.Elapsed += (obj, e) => { QueryAssetsInBigMap(); };
            App.RefreshCache();
            App.ShowConsole(false);
        }
        void SetBinding()
        {
            this.MyName_TextBlock.SetBinding(TextBlock.TextProperty, new Binding() { Source = this.Me });
            this.QuerySchemeName_TextBlock.SetBinding(TextBlock.TextProperty, new Binding("Name") { Source = this.ActiveQueryScheme });
            Extensions.AutoSetBinding(LabelGroups_ListView, this.LabelGroups, null);
            Extensions.AutoSetBinding(Labels_ListView, this.Labels, null);
            this.SelectedLabels_ListView.SetBinding(ListView.ItemsSourceProperty, new Binding() { Source = this.ActiveQueryScheme.QueryCriterias });
            this.Assets_ListView.SetBinding(ListView.ItemsSourceProperty, new Binding() { Source = this.Files_Page, IsAsync = true });
            this.SearchFolder_CheckBox.SetBinding(CheckBox.IsCheckedProperty, new Binding("SearchFolder") { Source = this.ActiveQueryScheme });
            this.UseCache_CheckBox.SetBinding(CheckBox.IsCheckedProperty, new Binding("UseCache") { Source = this, Mode = BindingMode.TwoWay });
            Extensions.AutoSetBinding(AssetType_TreeView, this.AllAssetTypes, null);
            Extensions.AutoSetBinding(Project_TreeView, this.AllProjects, null);
            Extensions.AutoSetBinding(QueryScheme_ListView, this.QuerySchemes, null);
            GetAssetTypeQueryScheme();
            GetProjectQueryScheme();
        }
        void SetEvents()
        {
            SetQuerySchemeMenuItemEvents();
            this.TakeInFolder_MenuItem.Click += (obj, e) => 
            {
                var assetInfo = Assets_ListView.SelectedItem as AssetFileInfo;
                if (assetInfo.ID_Asset is null || assetInfo.Asset is null)
                {
                    MessageBox.Show("暂时不支持对暴力查询结果进行资产信息编辑的操作。");
                    return;
                }
                var asset = assetInfo.Asset;
                var win = new Win_AssetOrganizer(asset);
                win.ShowDialog();
            };
            this.ShowOrHideConsole_MenuItem.Click += (obj, e) =>
            {
                App.ShowConsole(!App.ConsoleState);
            };

            this.Query_Button.Click += (obj, e) =>
            {
                _ = QueryKeyWordsAsync();
            };
            this.ClearQueryCriterias_MenuItem.Click += (obj, e) =>
            {
                this.ActiveQueryScheme.QueryCriterias.Clear();
            };
            this.UsePreviewTool_MenuItem.Click += (obj, e) =>
            {
                RunPreviewToll();
            };
            this.Options_MenuItem.Click += (obj, e) =>
            {
                var win = new Win_Options();
                win.ShowDialog();
            };
            this.RefreshLabelGroups_MenuItem.Click += (obj, e) =>
            {
                LoadLabelGroups();
            };
            this.Assets_ListView.SelectionChanged += (obj, e) =>
            {
                SetAssetListViewInfo();
            };
            this.NumItemsPerPage_ComboBox.SelectionChanged += (obj, e) =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    SetPage();
                });
            };
            this.LastPage_Button.Click += (obj, e) =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    if (Page <= 1)
                        return;
                    this.Page = Page <= 0 ? 0 : Page - 1;
                    SetPage(Page);

                });
            };
            this.NextPage_Button.Click += (obj, e) =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    if (NumItemsPerPage * Page >= Files.Count)
                        return;
                    this.Page += 1;
                });
                SetPage(Page);
            };
            this.AssetIconSize_Slider.MouseUp += (obj, e) =>
            {
                this.Assets_ListView.Focus();
            };
            this.IconMode_RadioButton.Checked += (obj, e) =>
            {
                SetAssetListViewItemTemplate();
            };
            this.ListMode_RadioButton.Checked += (obj, e) =>
            {
                SetAssetListViewItemTemplate();
            };
            this.ListMode_RadioButton.Checked += (obj, e) =>
            {
                SetAssetListViewItemTemplate();
            };
            this.RefreshCacheAndRequery_MenuItem.Click += (obj, e) =>
            {
                App.RefreshCache();
            };
            this.DeleteAsset_MenuItem.Click += (obj, e) =>
            {
                DeleteAssets();
            };
            this.Rename_MenuItem.Click += (obj, e) =>
            {
                var assetInfo = Assets_ListView.SelectedItem as AssetFileInfo;
                if (assetInfo.ID_Asset is null || assetInfo.Asset is null)
                {
                    MessageBox.Show("暂时不支持对暴力查询结果进行资产信息编辑的操作。");
                    return;
                }
                if (assetInfo is null)
                    return;
                var win = new SimpleInputor("新名称", assetInfo.Name);
                win.ShowDialog();
                if (win.Cancel)
                    return;
                if (string.IsNullOrEmpty(win.Value.ToString()))
                    return;
                string newName = win.Value.ToString();
                if (!Path_Extensons.CanUseToFileName(newName))
                    return;
                string newFullName = Path.GetDirectoryName(assetInfo.FullName) + Path.DirectorySeparatorChar + newName;

                var re = IO_Shell32.FileOpration(new string[] { assetInfo.FullName }, newFullName, FileOpration.ReName);
                if (!re)
                { MessageBox.Show("对不起，文件操作失败。但不清楚是为啥T。T..", "不好意思~"); return; }

                if (assetInfo.IsRealAsset)
                    SocketFunction.SendInstruct(App.Socket, "RenameAsset",
                        new object[]
                        {
                        assetInfo.ID_Asset,
                        assetInfo.Name,
                        newName
                        },
                        out object[] resultATs,
                        out Exception exat);

                assetInfo.FileInfo = new FileInfo(newFullName);
            };
            this.QueryScheme_ListView.MouseDoubleClick += (obj, e) =>
            {
                var item = QueryScheme_ListView.SelectedItem as QueryScheme;
                if (item is null)
                    return;
                SetActiveQueryScheme(item);
            };
            this.ResetLabel_MenuItem.Click += (obj, e) =>
            {
                var items = this.Assets_ListView.SelectedItems;
                if (items is null)
                    return;
                if (this.Assets_ListView.SelectedItem is null)
                    return;

                var boxre = MessageBoxResult.Yes;
                foreach (var item in items)
                {
                    var info = (AssetFileInfo)item;
                    if (info.Asset is null)
                    {
                        MessageBox.Show("暂时不支持对暴力查询结果进行资产信息编辑的操作。");
                        return;
                    }
                    if (!info.IsRealAsset)
                    {
                        boxre = MessageBox.Show("这些文件是隶属于一个资产文件夹中的子文件，您是要修改资产文件夹的标签吗？\r  是：我要修改其所在文件夹的标签。\r  否：只修改这些文件的标签。\r  取消：我点错了。", "一个可能会另你感到困惑的问题……", MessageBoxButton.YesNoCancel);
                        break;
                    }
                }

                if (boxre == MessageBoxResult.Cancel)
                    return;
                List<AssetFileInfo> infos = new List<AssetFileInfo>();
                if (boxre == MessageBoxResult.No)
                {
                    foreach (var item in items)
                    {
                        infos.Add((AssetFileInfo)item);
                    }
                }
                else if (boxre == MessageBoxResult.Yes)
                {
                    List<Asset> assets = new List<Asset>();
                    foreach (var item in items)
                    {
                        var info = (AssetFileInfo)item;
                        if (info.Asset is not null)
                            if (!assets.Contains(info.Asset))
                                assets.Add(info.Asset);
                    }
                    infos.AddRange(from ass in assets select new AssetFileInfo(ass.FullPath) { Asset = ass });
                }
                var win = new Win_AssetCreator(infos.ToArray());
                win.Show();
            };
            this.Instructions_MenuItem.Click += (obj, e) =>
            {
                App.ProgramStart(App.AssetLibraryInstructionFile);
            };
            this.Standard_MenuItem.Click += (obj, e) =>
            {
                App.ProgramStart(App.AssetLibraryStandardFile);
            };
            this.ChangeLog_MenuItem.Click += (obj, e) =>
            {
                App.ProgramStart(App.AssetLibraryChangeLogFile);
            };
            this.QuerySchemeEdit_Button.Click += (obj, e) =>
            {
                this.QuerySchemeEdit_Button.ContextMenu.IsOpen = true;
            };
            this.Logout_MenuItem.Click += (obj, e) =>
            {
                _exit = false;
                App.ShowLoginWindow(false);
                this.Close();
            };
            this.InExplore_MenuItem.Click += (obj, e) =>
            {
                //var re = MessageBox.Show("  您正在试图直接访问资产库，我们强烈不建议这么做。若您执意如此，请不要在其中直接修改任何文件名或路径，也不要直接复制、移动或删除文件。因为这样可能会造成数据库出现问题。\n  您依然要在资源管理器中访问资产库吗？","温馨提示",MessageBoxButton.YesNo);
                //if (re == MessageBoxResult.Yes)
                OpenFiles(1);
            };
            this.OpenBy_MenuItem.Click += (obj, e) =>
            {
                OpenFiles(2);
            };
            this.DownLoadTo_MenuItem.Click += (obj, e) =>
            {
                DownLoadFiles(true);
            };
            this.DownLoad_MenuItem.Click += (obj, e) =>
            {
                DownLoadFiles(false);
            };
            this.NewQS_MenuItem.Click += (obj, e) =>
            {
                string name = "查询方案";
                int t = 1;
                var win = new SimpleInputor("名称：", name);
                win.ShowDialog();
                if (!win.Cancel && !string.IsNullOrEmpty(win.Value.ToString()))
                {
                    name = win.Value.ToString();
                }
                while (QuerySchemes.FirstOrDefault(a => a.Name == name) is not null)
                {
                    name += t.ToString();
                }
                win.Close();
                QueryScheme newQS = new QueryScheme() { Name = name };
                if (ActiveQueryScheme is not null)
                    ActiveQueryScheme.Save();
                QuerySchemes.Add(newQS);
                SetActiveQueryScheme(newQS);
                this.QuerySchemes_MenuItem.Items.Add(MakeQuerySchemeMenuItem(newQS));
            };
            this.DeleteQS_MenuItem.Click += (obj, e) =>
            {
                QuerySchemes.Remove(ActiveQueryScheme);
                ActiveQueryScheme.Delete();
                MenuItem item = null;
                for (int i = 0; i < QuerySchemes_MenuItem.Items.Count; i++)
                {
                    var ob = QuerySchemes_MenuItem.Items[i];
                    if (ob is not MenuItem)
                        continue;
                    var itm = (MenuItem)ob;
                    var qs = (QueryScheme)itm.DataContext;
                    if (qs is null)
                        continue;
                    if (qs.Name == ActiveQueryScheme.Name)
                        item = itm;
                }
                if (item is not null)
                    this.QuerySchemes_MenuItem.Items.Remove(item);
                if (QuerySchemes.Count < 1)
                {
                    QueryScheme qs = new QueryScheme() { Name = "查询方案" };
                    QuerySchemes.Add(qs);
                    this.QuerySchemes_MenuItem.Items.Add(MakeQuerySchemeMenuItem(qs));
                    SetActiveQueryScheme(qs, false);
                }
                else
                {
                    QueryScheme qs = QuerySchemes.First();
                    SetActiveQueryScheme(qs, false);
                }
            };
            this.Assets_ListView.ContextMenuOpening += (obj, e) =>
            {
                if (Assets_ListView.SelectedItems.Count < 1)
                {
                    e.Handled = true;
                }
            };
            this.Sorting_ComboBox.SelectionChanged += (obj, e) =>
            {
                int ind = Sorting_ComboBox.SelectedIndex;
                switch (ind)
                {
                    case 0: ActiveQueryScheme.SortScheme = SortScheme.Usage; break;
                    case 1: ActiveQueryScheme.SortScheme = SortScheme.FileName; break;
                    case 2: ActiveQueryScheme.SortScheme = SortScheme.ArchiveDate; break;
                    case 3: ActiveQueryScheme.SortScheme = SortScheme.CreationTime; break;
                    case 4: ActiveQueryScheme.SortScheme = SortScheme.LastWriteTime; break;
                    case 5: ActiveQueryScheme.SortScheme = SortScheme.LastAccessTime; break;
                }
                SortFiles(Files);
            };
            this.Sorting_Button.Click += (obj, e) =>
            {
                ActiveQueryScheme.Sort = !ActiveQueryScheme.Sort;
                SortFiles(Files);
            };
            this.ShowAllLabelGroups_MenuItem.Click += async (obj, e) =>
            {
                this.ActiveQueryScheme.LoadAllLabelGroup = true;
                await QueryAssets_Async();
            };
            this.ShowLabelGroupsByAbout_MenuItem.Click += async (obj, e) =>
            {
                this.ActiveQueryScheme.LoadAllLabelGroup = false;
                await QueryAssets_Async();
            };
            QueryBox_TextBox.TextChanged += (obj, e) =>
            {
                boxStr = this.QueryBox_TextBox.Text;
                BigMapQueryTimer.Stop();
                FindLabelByKeyWords(boxStr);
                var ats = ActiveQueryScheme.AssetTypes;
                var pjs = ActiveQueryScheme.Projects;
                IEnumerable<IQueryCriteria> lbs = ActiveQueryScheme.QueryCriterias.Where(a => a is QueryCriteria_Label);
                if (ats.Any() || pjs.Any() | lbs.Any() || string.IsNullOrWhiteSpace(boxStr))
                    return;
                try { new Regex(boxStr); } catch { return; }
                BigMapQueryTimer.Start();
            };
            this.Labels_ListView.KeyDown += (obj, e) =>
            {
                if (e.Key == Key.Enter || e.Key == Key.Return)
                {
                    SelectLabel();
                }
            };
            this.AssetIconSize_Slider.ValueChanged += (obj, e) =>
            {
                var item = Assets_ListView.SelectedItem;
                if (item is null)
                    return;
                double count = Assets_ListView.Items.Count;
                double index = Assets_ListView.Items.IndexOf(item);
                double extent = this.Assets_ScrollViewer.ExtentHeight;
                double scroll = this.Assets_ScrollViewer.ScrollableHeight;
                double n = (index / count) * extent;
                double size = AssetIconSize_Slider.Value;
                size = Math.Pow((double)size - 7, 1.7) * 16;
                double Width = this.Assets_ScrollViewer.ActualWidth;
                int c = (int)(Width / size);
                c = c < 1 ? 1 : c;
                double x = (Width - size) / c;
                x = x <= 0 ? 0 : x;
                //Console.WriteLine("{3} !  {0}-{1}={2}", Width, size, x, c);
                this.Assets_ScrollViewer.ScrollToVerticalOffset(n - x);
            };
            QueryBox_TextBox.KeyDown += async (obj, e) =>
            {
                if (e.Key == Key.Enter || e.Key == Key.Return)
                {
                    if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                    {
                        await QueryKeyWordsAsync();
                        //string str = QueryBox_TextBox.Text;
                        //string[] strs = Regex.Split(str,@"\s+");
                        //foreach (string s in strs)
                        //{
                        //    this.ActiveQueryScheme.QueryCriterias.Add(new QueryCriteria_Keyword() { Keyword = s });
                        //}
                        //QueryBox_TextBox.Text = ""; 
                        //if(OnlyKeywords())
                        //    await QueryAssets_Async(true);
                        //else
                        //    await QueryAssets_Async();
                    }
                    else
                    {
                        if (this.Labels_ListView.Items.Count > 0)
                        {
                            Labels_ListView.SelectedIndex = 0;
                            Labels_ListView.Focus();
                        }
                    }
                }
            };
            this.KeyDown += (obj, e) =>
            {
                if (e.Key == Key.V && Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    if (Clipboard.ContainsText())
                    {
                        var text = Clipboard.GetText();
                        try
                        {
                            var lines = Regex.Split(text, @"\s");
                            var longName = lines.FirstOrDefault();
                            var fileName = longName is null ? "未命名文本" : longName.Length > 16 ? longName.Substring(0, 16) : longName;
                            var dlg = new SimpleInputor("文件名：", fileName, "请为该文本（.txt）资产命名：");
                            dlg.CheckFileName = true;
                            dlg.ShowDialog();
                            if (dlg.Cancel) return;
                            fileName = dlg.Value?.ToString();
                            IO_Extensons.CreateDirectory(App.TemporaryFileDir);
                            var filePath = Path.Combine(App.TemporaryFileDir, fileName + ".txt");
                            File.WriteAllText(filePath, text);
                            this._temporaryFile = filePath;
                            this.Assets_ListView_Drop(this, null);
                            this._temporaryFile = null;
                        }
                        catch (Exception ex) { MessageBox.Show(ex.Message); }
                    }
                    if (Clipboard.ContainsImage())
                    {
                        BitmapSource image = Clipboard.GetImage();
                        try
                        {
                            var fileName = "未命名图片";
                            var dlg = new SimpleInputor("文件名：", fileName, "请为该图片（.jpg）资产命名：");
                            dlg.CheckFileName = true;
                            dlg.ShowDialog();
                            if (dlg.Cancel) return;
                            fileName = dlg.Value?.ToString();
                            IO_Extensons.CreateDirectory(App.TemporaryFileDir);
                            var filePath = Path.Combine(App.TemporaryFileDir, fileName + ".jpg");
                            SaveImageToFile(image, filePath);
                            this._temporaryFile = filePath;
                            this.Assets_ListView_Drop(this, null);
                            this._temporaryFile = null;
                        }
                        catch (Exception ex) { MessageBox.Show(ex.Message); }
                    }
                }

                if (e.Key == Key.D && Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    this.ActiveQueryScheme.QueryCriterias.Clear();
                }

                if (e.Key == Key.S && Keyboard.IsKeyDown(Key.LeftCtrl))
                {
                    //MessageBox.Show(" 假装打开了添加标签的界面！^-^"); 
                }

                if (e.Key == Key.A && Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.LeftShift))
                {
                    RunPreviewToll();
                }

                if (e.Key == Key.Escape)
                {
                    if (!StopQueryProcess || !StopSetMD5Process)
                    {
                        Console.WriteLine("用户请求取消当前进程。");
                        this.AssetListInfo_TextBlock.Text = "正在尝试中止当前操作，请稍后...";
                        this.AssetListInfo_TextBlock.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(158, 50, 150, 0));
                        StopQueryProcess = true;
                        StopSetMD5Process = true;
                    }
                }

            };
            this.LabelGroups_ListView.SelectionChanged += (obj, e) => { LoadAssetLabels(); };
            this.Labels_ListView.MouseDoubleClick += (obj, e) => { SelectLabel(); };
            this.Closing += (obj, e) =>
            {
                this.AssetUsageLogs.SaveToFile();
                foreach (var qs in QuerySchemes)
                {
                    qs.Save();
                }
            };
        }
        void LoadHead()
        {
            Head_Image.Source = AssetIcon.GetImageSouce(Properties.Resources.Head03s);
        }
        string ParseParameterExpressions(string parameter)
        {
            string newParamter = parameter;
            if (newParamter.IsMatch(@"\{[^\{]+?\}", out string value))
            {
                switch (value)
                {
                    case "{PersonName}":
                        newParamter = newParamter.Replace("{PersonName}", App.Me.Name);
                        break;
                }
            }
            if (newParamter == parameter)
                return newParamter;
            else
                return ParseParameterExpressions(newParamter);
        }

        void LoadUniversualExtensions()
        {
            SocketFunction.SendInstruct(App.Socket, "LoadUniversualExtensions", null, out object[] result, out Exception ex);
            foreach (UniversualExtension ue in result)
            {
                MenuItem menuItem = new MenuItem();
                menuItem.Header = ue.Name;
                menuItem.ToolTip = ue.Illustration;
                menuItem.Click += (obj, e) =>
                {
                    App.ShowConsole(true);
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.UseShellExecute = true;
                    startInfo.FileName = ue.Path;
                    startInfo.Verb = "runas";
                    foreach (var arg in ue.Args)
                    {
                        startInfo.ArgumentList.Add(ParseParameterExpressions(arg));
                    }
                    try
                    {
                        var process = System.Diagnostics.Process.Start(startInfo);
                        process.EnableRaisingEvents = true;
                        process.Exited += (obj, e) =>
                        {
                            App.ShowConsole(false);
                        };
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        Console.WriteLine(ex.Message);
                    }
                };
                this.UniversualExtension_MenuItem.Items.Add(menuItem);
            }
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

            SocketFunction.SendInstruct(App.Socket, "LoadAllAssetTypeTree", new object[] { false }, out object[] resultATs, out Exception exat);
            IEnumerable<AssetType> allats = (IEnumerable<AssetType>)resultATs;
            CheckingTreeItem[] ats = CheckingTreeItem.MakeTreeItem(allats);
            foreach (CheckingTreeItem attrees in ats)
            {
                _AllAssetTypes_NoTree.AddRange(attrees.GetAllTreeNode());
            }
            AllAssetTypes.AddRange(ats);
        }

        void SetAssetListViewInfo()
        {
            this.Dispatcher.Invoke(() =>
            {
                this.AssetListInfo_TextBlock.Background = null;
                this.AssetListInfo_TextBlock.Text = $"共计{Files.Count}个文件，当前页显示第{_minN}-{_maxN}个，已选中{Assets_ListView.SelectedItems.Count}个。";
            });
        }
        int _minN = 0;
        int _maxN = 0;
        async void SetPage(int page = 1)
        {
            this.Dispatcher.Invoke(() =>
            {
                Files_Page.Clear();
            });
            int max = Files.Count;
            int ia = (page - 1) * NumItemsPerPage;
            int ib = page * NumItemsPerPage;
            if (ia >= max)
                return;
            ib = ib > max ? max : ib;
            _minN = ia + 1;
            _maxN = ib;
            this.Dispatcher.Invoke(() =>
            {
                for (int i = ia; i < ib; i++)
                {
                    Files_Page.Add(Files[i]);
                }
            });
            var tokenSource = new CancellationTokenSource();
            this.TokenSources.Enqueue(tokenSource);
            SetAssetListViewInfo();
            await ShowAssetsPreview(tokenSource);
        }
        void Assets_ListView_PreviewMouseWheel(object obj, MouseWheelEventArgs e)
        {
            e.Handled = true;
            var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
            eventArg.RoutedEvent = UIElement.MouseWheelEvent;
            eventArg.Source = obj;
            var parent = ((Control)obj).Parent as UIElement;
            parent.RaiseEvent(eventArg);
        }
        async void SetAssetListViewItemTemplate()
        {
            if (AssetShowMode == AssetShowMode.Icon)
            {
                if (TokenSources.Count > 0)
                    TokenSources.Dequeue().Cancel();
                //this.Assets_ListView.ItemTemplate = (DataTemplate)this.Resources["AssetIconTemplate"];
                this.Assets_ScrollViewer.Style = null;
                this.Assets_ListView.Style = (Style)this.Resources["AssetIconTemplateStyle"];
                var tokenSource = new CancellationTokenSource();
                this.TokenSources.Enqueue(tokenSource);
                await ShowAssetsPreview(tokenSource);
                this.Assets_ListView.PreviewMouseWheel += Assets_ListView_PreviewMouseWheel;
            }
            if (AssetShowMode == AssetShowMode.List)
            {
                this.Assets_ListView.PreviewMouseWheel -= Assets_ListView_PreviewMouseWheel;
                this.Assets_ScrollViewer.Style = (Style)this.Resources["AssetListTemplateScrollViewerStyle"];
                //this.Assets_ListView.Style = (DataTemplate)this.Resources["AssetListTemplate"];
                this.Assets_ListView.Style = (Style)this.Resources["AssetListTemplateStyle"];
                if (TokenSources.Any())
                    this.TokenSources.Dequeue().Cancel();
            }
        }
        async void SetActiveQueryScheme(QueryScheme queryScheme, bool save = true)
        {
            if (save)
                this.ActiveQueryScheme.Save();
            this.ActiveQueryScheme = queryScheme;
            SetBinding();
            await QueryAssets_Async();
        }
        //判断是否为序列文件夹
        bool IsSequenceFolder(string dirPath)
        {
            if (!Directory.Exists(dirPath))
                return false;

            if (!Directory.Exists(dirPath)) return false;
            bool notsequence =  File.Exists(Path.Combine(dirPath, ".notsequence"));
            if (notsequence)
                return false;
            string[] files = GetFiles(dirPath, "*.*", SearchOption.TopDirectoryOnly);
            IEnumerable<string> fileNames = from file in files select Path.GetFileNameWithoutExtension(file);
            if (!fileNames.Any())
                return false;
            if (fileNames.Count() < 2)
                return false;

            List<string> nameStrs = new List<string>();
            int times = 0;
            int noTimes = 0;
            int i = 0;
            foreach (string name in fileNames)
            {
                bool ismatch = name.IsMatch(@"\d{4,}$", out string value);
                if (ismatch)
                {
                    string nameStr = Regex.Replace(name, @"\d{4,}$", string.Empty);
                    string last = nameStrs.Any() ? nameStrs.Last() : null;
                    if (i > 0)
                        if (nameStr.Equals(last))
                        {
                            times++;
                        }
                        else
                            noTimes++;
                    nameStrs.Add(nameStr);
                }
                if (times >= 2)
                {
                    Console.WriteLine(dirPath + " 可能是序列文件夹。");
                    return true;
                }
                if (noTimes >= 3)
                    return false;
                i++;
            }
            return false;
        }
        //判断是否有非预览标记
        bool IsNoPreviewFolder(string dirPath)
        {
            if (!Directory.Exists(dirPath)) return false;
            return File.Exists(Path.Combine(dirPath, ".nopreview"));
            //var files = Directory.GetFiles(dirPath, ".nopreview", SearchOption.TopDirectoryOnly);
            //if (files.Any())
            //    return true;
            //return false;
        }
        //判断是否有递归预览标记
        bool IsRecursion(string dirPath)
        {
            if (!Directory.Exists(dirPath)) return false;
            return File.Exists(Path.Combine(dirPath, ".recursive"));
            //var files = Directory.GetFiles(dirPath, ".recursive", SearchOption.TopDirectoryOnly);
            //if(files.Any())
            //    return true;
            //return false;
        }

        //获得文件MD5
        void SetAssetFileMd5Infos(AssetFileMd5Info[] sourceInfos)
        {
            //List<AssetFileMd5Info> md5s = new List<AssetFileMd5Info>();
            StopSetMD5Process = false;
            foreach (AssetFileMd5Info info in sourceInfos)
            {
                if (StopSetMD5Process) { Console.WriteLine("计算过程被用户中止。"); break; }//++++++++ =》》》 检查是否要中止
                Dispatcher.Invoke(() =>
                {
                    this.AssetListInfo_TextBlock.Text = "正在计算MD5值(按ESC中止)：" + info.SourcePath;
                });
                if (info.Skip)
                    continue;
                try
                {
                    info.MD5 = Path_Extensons.GetMD5FromFile(info.SourcePath ?? info.SourceAssetDir);
                    info.Size = new FileInfo(info.SourcePath ?? info.SourceAssetDir).Length;
                    info.Dir = Path.GetDirectoryName(info.SourcePath ?? info.SourceAssetDir);
                    info.Name = Path.GetFileName(info.SourcePath ?? info.SourceAssetDir);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
            }
            StopSetMD5Process = true;
        }
        //查重
        List<AssetFileMd5Info> _md5Infos = new List<AssetFileMd5Info>();
        async Task<string[]> CheckFileMD5(AssetFileMd5Info[] sourceFiles)
        {
            return await Task.Run<string[]>(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    this.AssetListInfo_TextBlock.Text = "正在查重，此过程受要上传的文件数量、总大小和资产库内已有资产数影响，请稍候...";
                    this.AssetListInfo_TextBlock.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(158, 255, 215, 0));
                    LockOrUnlockInterface(true);
                });

                object[] results = null;

                SetAssetFileMd5Infos(sourceFiles);
                IEnumerable<string> md5s = from info in _md5Infos select info.MD5;

                Dispatcher.Invoke(() =>
                {
                    this.AssetListInfo_TextBlock.Text = "正在将计算好的MD5信息提交给服务器进行比对，并等待结果...";
                    this.AssetListInfo_TextBlock.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(158, 255, 215, 0));
                });

                SocketFunction.SendInstruct(App.Socket,
                    "CheckAssetFileMD5",
                    md5s.ToArray(),
                    out results,
                    out Exception ex);

                Dispatcher.Invoke(() =>
                {
                    this.AssetListInfo_TextBlock.Text = "";
                    this.AssetListInfo_TextBlock.Background = null;
                    LockOrUnlockInterface(false);
                });
                var re = from r in results select r.ToString();
                return re.ToArray();
            });
        }

        private string[] GetFileSystemEntries(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            string[] files = null;
            if (useCache)
                files = App.Caches.GetFileSystemEntries(path, searchPattern, searchOption);
            else
                files = Directory.GetFileSystemEntries(path, searchPattern, searchOption);
            return files;
        }
        private string[] GetFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            string[] files;
            if (useCache)
                files = App.Caches.GetFiles(path, searchPattern, searchOption); 
            else
                files = Directory.GetFiles(path, searchPattern, searchOption);
            return files;
        }
        private string[] GetDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            string[] directories;
            if (useCache)
                directories = App.Caches.GetDirectories(path, searchPattern, searchOption);
            else
                directories = Directory.GetDirectories(path, searchPattern, searchOption);
            return directories;
        }

        void DownLoadFiles(bool setPath)
        {
            if (Assets_ListView.SelectedItems.Count < 1)
                return;

            List<string> files = new List<string>();
            foreach (var item in Assets_ListView.SelectedItems)
            {
                AssetFileInfo file = (AssetFileInfo)item;
                if (File.Exists(file.FullName) || Directory.Exists(file.FullName))
                {
                    this.AssetUsageLogs.Recording(file, AssetUsage.Download);
                    files.Add(file.FullName);
                }
            }
            if (setPath)
            {
                CommonOpenFileDialog dialog = new CommonOpenFileDialog();
                dialog.IsFolderPicker = true;
                var dialogRe = dialog.ShowDialog();
                if (dialogRe == CommonFileDialogResult.Cancel)
                    return;
                if (string.IsNullOrWhiteSpace(dialog.FileName))
                    return;
                ActiveQueryScheme.DownLoadPath = dialog.FileName;
            }
            string topath = ActiveQueryScheme.DownLoadPath;
            var ok = IO_Shell32.FileOpration(files.ToArray(), topath, FileOpration.Copy);
            if (!ok)
                return;
            var targetfile = files.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(targetfile))
                return;
            string target = Path.Combine(topath, Path.GetFileName(targetfile));
            App.ProgramStart("explorer.exe", "/select, " + target);
        }
        void OpenFiles(int inExplorer = 0)
        {
            if (Assets_ListView.SelectedItems.Count < 1)
                return;

            if (Assets_ListView.SelectedItems.Count > 3)
            {
                var mre = MessageBox.Show($"您选中了{Assets_ListView.SelectedItems.Count}个文件,确定要一次同时打开这么多文件吗？", "温馨提示", button: MessageBoxButton.YesNo);
                if (mre == MessageBoxResult.No)
                    return;
            }

            for (int i = 0; i < Assets_ListView.SelectedItems.Count; i++)
            {
                object item = Assets_ListView.SelectedItems[i];
                AssetFileInfo file = (AssetFileInfo)item;
                if (File.Exists(file.FullName) || Directory.Exists(file.FullName))
                    if (inExplorer == 0)
                    {
                        App.ProgramStart(file.FullName);
                        this.AssetUsageLogs.Recording(file, AssetUsage.Open);
                    }
                    else if (inExplorer == 1)
                    {
                        string rg = "/select," + file.FullName;
                        App.ProgramStart("explorer.exe", rg);
                        this.AssetUsageLogs.Recording(file, AssetUsage.InExplorer);
                    }
                    else if (inExplorer == 2)
                    {
                        System.Diagnostics.Process proc = new System.Diagnostics.Process();
                        proc.EnableRaisingEvents = false;
                        proc.StartInfo.FileName = "Rundll32.exe";
                        proc.StartInfo.Arguments = $"Shell32,OpenAs_RunDLL {file.FullName}";
                        proc.Start();
                        this.AssetUsageLogs.Recording(file, AssetUsage.Open);

                    }
            }
        }
        async Task CreateShortcuts_Async()
        {
            await Task.Run(() =>
            {
                if (this.ActiveQueryScheme is null)
                    return;
                string dir = Path.Combine(App.QueryResultsDir, this.ActiveQueryScheme.Name);
                if (!Directory.Exists(dir))
                    IO_Extensons.CreateDirectory(dir);
                string[] files = Directory.GetFiles(dir);
                for (int i = 0; i < files.Length; i++)
                {
                    string file = files[i];
                    if (Path.GetExtension(file) == ".lnk")
                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {
                            continue;
                        }
                }
                for (int i = 0; i < Files.Count; i++)
                {
                    AssetFileInfo fileinfo = Files[i];
                    string shortcutPath = Path.Combine(dir, i.ToString() + "_" + Path.GetFileNameWithoutExtension(fileinfo.FullName) + ".lnk");
                    try
                    {
                        WPF_IO.CreateShortcut(fileinfo.FullName, shortcutPath);
                    }
                    catch
                    {
                        continue;
                    }
                }
            });
        }

        bool OnlyKeywords()
        {
            if (ActiveQueryScheme.QueryCriterias.Any())
            {
                var finded = ActiveQueryScheme.QueryCriterias.FirstOrDefault(a => a is QueryCriteria_Label);
                if (finded is null)
                    return true;
            }
            return false;
        }
        async void RunPreviewToll()
        {
            if (this.ActiveQueryScheme is null) return;
            if (string.IsNullOrWhiteSpace(App.Options.PreviewToolPath))
            {
                MessageBox.Show("您还没有配置第三方预览工具，请在'程序-选项'中进行配置。", "温馨提示！");
                return;
            }
            string dir = Path.Combine(App.QueryResultsDir, this.ActiveQueryScheme.Name);
            ProcessStartInfo process = new ProcessStartInfo();
            process.UseShellExecute = true;
            process.FileName = App.Options.PreviewToolPath;
            process.Arguments = App.Options.PreviewToolRunParameterPrefix + " \"" + dir + "\" " + App.Options.PreviewToolRunParameterSuffix;
            System.Diagnostics.Process.Start(process);
            await CreateShortcuts_Async();
        }
        async Task QueryKeyWordsAsync()
        {
            string str = QueryBox_TextBox.Text;
            if (!string.IsNullOrWhiteSpace(str))
            {
                string[] strs = Regex.Split(str, @"\s+");
                foreach (string s in strs)
                {
                    this.ActiveQueryScheme.QueryCriterias.Add(new QueryCriteria_Keyword() { Keyword = s });
                }
            }
            QueryBox_TextBox.Text = string.Empty;
            await QueryAssets_Async();
        }
        /// <summary>
        /// 保存图片到文件
        /// </summary>
        /// <param name="image">图片数据</param>
        /// <param name="filePath">保存路径</param>
        private void SaveImageToFile(BitmapSource image, string filePath)
        {
            BitmapEncoder encoder = GetBitmapEncoder(filePath);
            encoder.Frames.Add(BitmapFrame.Create(image));

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(stream);
            }
        }
        /// <summary>
        /// 根据文件扩展名获取图片编码器
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>图片编码器</returns>
        private BitmapEncoder GetBitmapEncoder(string filePath)
        {
            var extName = Path.GetExtension(filePath).ToLower();
            if (extName.Equals(".png"))
            {
                return new PngBitmapEncoder();
            }
            else
            {
                return new JpegBitmapEncoder();
            }
        }

        void FindLabelByKeyWords(string str)
        {
            for (int i = 0; i < LabelGroups.Count; i++)
            {
                AssetLabelGroup group = LabelGroups[i];
                if (group.ID.Equals(-1))
                {
                    LabelGroups.Remove(group);
                    i--;
                }
            }
            if (string.IsNullOrEmpty(str))
            { return; }

            AssetLabelGroup LabelTipGroup = new AssetLabelGroup() { ID = -1, Name = "找到的标签" };
            foreach (AssetLabelGroup group in LabelGroups)
            {
                IEnumerable<AssetLabel> labels = group.AssetLabels.Where(a => a.Name.ToLower().Contains(str.ToLower()));
                if (labels.Any())
                {
                    LabelTipGroup.AssetLabels.AddRange(labels);
                }
            }
            if (LabelTipGroup.AssetLabels.Any())
            {
                LabelGroups.Insert(0, LabelTipGroup);
                this.LabelGroups_ListView.SelectedIndex = 0;
            }
        }
        async void SelectLabel()
        {
            var selectedLabel = Labels_ListView.SelectedItem;
            if (selectedLabel is null)
                return;
            var label = selectedLabel as AssetLabel;
            IEnumerable<IQueryCriteria> same = ActiveQueryScheme.QueryCriterias.Where(a => { return a is QueryCriteria_Label la ? la.ID.Equals(label.ID) : false; });
            if (!same.Any())
            {
                this.ActiveQueryScheme.QueryCriterias.Add(new QueryCriteria_Label(label));
                var ind = this.LabelGroups_ListView.SelectedIndex;
                await QueryAssets_Async();
                if (this.LabelGroups_ListView.Items.Count > ind)
                    this.LabelGroups_ListView.SelectedIndex = ind;
            }

        }

        void LoadAssetLabels()
        {
            this.Labels.Clear();

            var selectedItem = this.LabelGroups_ListView.SelectedItem;
            if (selectedItem is null)
                return;

            AssetLabelGroup group = (AssetLabelGroup)selectedItem;
            this.Labels.AddRange(group.AssetLabels);


            //ulong group = Convert.ToUInt64(((AssetLabelGroup)selectedItem).ID);
            //SocketFunction.SendInstruct(App.Socket,
            //    "LoadLabelBySelectedLabelGroup",
            //    new object[]
            //    {
            //        group
            //    },
            //    out object[] result,
            //    out Exception ex);

            //AssetLabel[] re_Labels = (AssetLabel[])result;
            //this.Labels.AddRange(re_Labels);
        }
        void SetQuerySchemeMenuItemEvents()
        {
            this.AssetTypeQueryScheme_All_MenuItem.Click += async (obj, e) =>
            {
                this.ActiveQueryScheme.AssetTypeQueryScheme = AssetTypeQueryScheme.All;
                GetAssetTypeQueryScheme();
                await QueryAssets_Async();
            };
            this.AssetTypeQueryScheme_MyPosition_MenuItem.Click += async (obj, e) =>
            {
                this.ActiveQueryScheme.AssetTypeQueryScheme = AssetTypeQueryScheme.MyPosition;
                GetAssetTypeQueryScheme();
                await QueryAssets_Async();
            };
            this.AssetTypeQueryScheme_Custom_MenuItem.Click += async (obj, e) =>
            {
                var win = new PTSelector(typeof(AssetType));
                win.ShowDialog();
                if (!win.Cancel && win.SelectedAssetTypes.Any())
                {
                    ActiveQueryScheme.AssetTypes.Clear();
                    ActiveQueryScheme.AssetTypes.AddRange(win.SelectedAssetTypes);
                    this.ActiveQueryScheme.AssetTypeQueryScheme = AssetTypeQueryScheme.Custom;
                    GetAssetTypeQueryScheme();
                    await QueryAssets_Async();
                }
                win.Close();
            };
            this.ProjectQueryScheme_All_MenuItem.Click += async (obj, e) =>
            {
                this.ActiveQueryScheme.ProjectQueryScheme = ProjectQueryScheme.All;
                GetProjectQueryScheme();
                await QueryAssets_Async();
            };
            this.ProjectQueryScheme_Universal_MenuItem.Click += async (obj, e) =>
            {
                this.ActiveQueryScheme.ProjectQueryScheme = ProjectQueryScheme.Universal;
                GetProjectQueryScheme();
                await QueryAssets_Async();
            };
            this.ProjectQueryScheme_AllAndUniversal_MenuItem.Click += async (obj, e) =>
            {
                this.ActiveQueryScheme.ProjectQueryScheme = ProjectQueryScheme.AllAndUniversal;
                GetProjectQueryScheme();
                await QueryAssets_Async();
            };
            this.ProjectQueryScheme_Custom_MenuItem.Click += async (obj, e) =>
            {
                var win = new PTSelector(typeof(Project));
                win.ShowDialog();
                if (!win.Cancel && win.SelectedProjects.Any())
                {
                    ActiveQueryScheme.Projects.Clear();
                    ActiveQueryScheme.Projects.AddRange(win.SelectedProjects);
                    this.ActiveQueryScheme.ProjectQueryScheme = ProjectQueryScheme.Custom;
                    GetProjectQueryScheme();
                    await QueryAssets_Async();
                }
                win.Close();
            };
        }

        void LoadQuerySchemes()
        {
            this.QuerySchemes.AddRange(QueryScheme.LoadAll());
            if (!QuerySchemes.Any())
            {
                QueryScheme query = new QueryScheme() { Name = "查询方案" };
                QuerySchemes.Add(query);
            }
            if (string.IsNullOrEmpty(App.Settings.LastQSName))
                ActiveQueryScheme = QuerySchemes.First();
            else
            {
                var qs = QuerySchemes.FirstOrDefault(a => a.Name == App.Settings.LastQSName);
                if (qs is not null)
                    ActiveQueryScheme = qs;
                else
                    ActiveQueryScheme = QuerySchemes.First();
            }
            foreach (QueryScheme qs in QuerySchemes)
            {
                MenuItem item = MakeQuerySchemeMenuItem(qs);
                this.QuerySchemes_MenuItem.Items.Add(item);
            }
        }
        MenuItem MakeQuerySchemeMenuItem(QueryScheme queryScheme)
        {
            MenuItem item = new MenuItem() { DataContext = queryScheme };
            item.Header = queryScheme.Name;
            item.Click += (obj, e) =>
            {
                MenuItem item = (MenuItem)e.OriginalSource;
                QueryScheme qs = (QueryScheme)item.DataContext;
                SetActiveQueryScheme(qs);
            };
            return item;
        }

        void GetAssetTypeQueryScheme()
        {
            //if (ActiveQueryScheme.AssetTypeQueryScheme == AssetTypeQueryScheme.All)
            //    AssetTypeQueryScheme_TextBox.Text = "查询全部资产类型";
            //if (ActiveQueryScheme.AssetTypeQueryScheme == AssetTypeQueryScheme.MyPosition)
            //    AssetTypeQueryScheme_TextBox.Text = "仅查询我职位关注的资产类型";
            //if (ActiveQueryScheme.AssetTypeQueryScheme == AssetTypeQueryScheme.Custom)
            //    AssetTypeQueryScheme_TextBox.Text = ActiveQueryScheme.AssetTypes.AllToString();

            CheckBoxEventEnabled = false;
            foreach (CheckingTreeItem item in _AllAssetTypes_NoTree)
            {
                if (ActiveQueryScheme.AssetTypes.Contains(item.Content))
                    item.IsSelected = true;
                else
                    item.IsSelected = false;
            }
            CheckBoxEventEnabled = true;
        }
        void GetProjectQueryScheme()
        {
            //if (ActiveQueryScheme.ProjectQueryScheme == ProjectQueryScheme.All)
            //    ProjectQueryScheme_TextBox.Text = "查询所有项目资产";
            //if (ActiveQueryScheme.ProjectQueryScheme == ProjectQueryScheme.Universal)
            //    ProjectQueryScheme_TextBox.Text = "仅查询通用资产";
            //if (ActiveQueryScheme.ProjectQueryScheme == ProjectQueryScheme.AllAndUniversal)
            //    ProjectQueryScheme_TextBox.Text = "查询所有项目或通用资产";
            //if (ActiveQueryScheme.ProjectQueryScheme == ProjectQueryScheme.Custom)
            //    ProjectQueryScheme_TextBox.Text = ActiveQueryScheme.Projects.AllToString();

            CheckBoxEventEnabled = false;
            foreach (var item in _AllProjects_NoTree)
            {
                if (ActiveQueryScheme.Projects.Contains(item.Content))
                    item.IsSelected = true;
                else
                    item.IsSelected = false;
            }
            CheckBoxEventEnabled = true;
        }

        async Task QueryAssets_Async()
        {
            LockOrUnlockInterface(true);
            StopQueryProcess = false;
            this.Files_Page.Clear();
            this.Files.Clear();
            this.AssetListInfo_TextBlock.Text = "正在查询，请稍候(按ECS取消)...";
            this.AssetListInfo_TextBlock.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(158, 255, 0, 0));
            await Task.Run(() => { Query(); });
            if (this.AssetListInfo_TextBlock.Text == "正在查询，请稍候(按ECS取消)...")
                this.AssetListInfo_TextBlock.Text = "查询已完成。";
            this.AssetListInfo_TextBlock.Background = null;
            StopQueryProcess = true;
            LockOrUnlockInterface(false);
        }
        void LockOrUnlockInterface(bool locked)
        {
            Control[] controls = new Control[]
            {
                AssetType_TreeView,
                Project_TreeView,
                Query_Button,
                LastPage_Button,
                NextPage_Button,
                SelectedLabels_ListView,
                Labels_ListView,
                QueryScheme_ListView
            };
            foreach (var crl in controls)
            {
                crl.IsEnabled = !locked;
            }
        }

        System.Timers.Timer BigMapQueryTimer { get;} = new System.Timers.Timer(1000) { AutoReset = false };
        string boxStr;

        void DeleteAssets()
        {
            var canDeleteFile = Keyboard.IsKeyDown(Key.LeftShift);
            var canBatchDelete = Keyboard.IsKeyDown(Key.LeftCtrl);
            Func<object, bool, bool,bool> DeleteOne = (item, noAlert, deleteFile) =>
            {
                AssetFileInfo assetInfo = item as AssetFileInfo;
                if (assetInfo is null) return false;
                MessageBoxResult mre = default;
                if (!noAlert)
                {
                    mre = MessageBox.Show("    确定要删除这个资产吗？\r " + assetInfo.FullName, "重要提示！", MessageBoxButton.YesNo);
                    if (mre == MessageBoxResult.No) return false;
                }

                if (assetInfo.IsRealAsset)
                {
                    SocketFunction.SendInstruct(App.Socket, "DeleteAsset",
                            new object[]
                            {
                            assetInfo.ID_Asset,
                            },
                            out object[] results,
                            out Exception expj);
                }

                if (deleteFile)
                {
                    Console.WriteLine("删除资产！ " + assetInfo.FullName);
                    try
                    {
                        if (assetInfo.IsFolder)
                            Directory.Delete(assetInfo.FullName);
                        else
                            File.Delete(assetInfo.FullName);
                    }
                    catch (Exception ex) 
                    { 
                        Console.WriteLine(ex.Message);
                        return false;
                    }
                }
                else
                    OpenFiles(1);

                Files.Remove(assetInfo);
                Files_Page.Remove(assetInfo);
                return true;
            };
            Action BatchDelete = () =>
            {
                List<object> dItems = new List<object>();
                foreach (var item in Assets_ListView.SelectedItems)
                {
                    dItems.Add(item);
                }
                
                for (int i = 0; i < dItems.Count; i++)
                {
                    object item = dItems[i];
                    var re = DeleteOne(item, canDeleteFile, true);
                }
            };

            if (Assets_ListView.SelectedItems.Count > 1)
            {
                if (canBatchDelete)
                {
                    BatchDelete.Invoke();
                }
                else
                {
                    var re = MessageBox.Show("    保险起见，在完成大部分开发测试之前，暂不支持批量删除...\r若您对此有任何意见或建议请点击 '是' ", "抱歉！", MessageBoxButton.YesNo);
                    if (re == MessageBoxResult.Yes)
                    {
                        MessageBox.Show("  您的意见被驳回，您可以直接去向振振描述您的问题。~ ㄟ(▔▽▔)ㄏ ", "ㄟ(◑‿◐ )ㄏ");
                    }
                    return;
                }
            }
            else
            {
                DeleteOne(Assets_ListView.SelectedItem,false,canDeleteFile);
            }
        }
        async void QueryAssetsInBigMap()
        {
            if (!App.Caches.Any())
                return;
            if (string.IsNullOrEmpty(boxStr))
                return;
            var reg = boxStr;
            ConcurrentBag<AssetFileInfo> re_files = new ConcurrentBag<AssetFileInfo>();
            var timeA = DateTime.Now;
            Console.WriteLine("正在进行暴力查询");
            Dispatcher.Invoke(() =>
            {
                this.AssetListInfo_TextBlock.Text = "正在进行暴力查询！（未选择任何资产类型和标签）";
                this.AssetListInfo_TextBlock.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(158, 255, 0, 0));
            });
            var files = await App.Caches.GetFileSystemEntriesFromBigMap(reg);
            if (!files.Any())
            {
                Dispatcher.Invoke(() =>
                {
                    Files.Clear();
                    Files_Page.Clear();
                    this.AssetListInfo_TextBlock.Background = null;
                    this.AssetListInfo_TextBlock.Text = "未找到任何符合条件的文件名。";
                });
                return;
            }
            await Parallel.ForEachAsync(files, async (file, token) =>
            {
                await Task.Run(() => { re_files.Add(new AssetFileInfo(file,true)); }); 
            });
            this.AssetUsageLogs.Statistics(re_files);
            SortFiles(re_files);
            var timeB = DateTime.Now;
            Console.WriteLine("本次查询共用时：" + (timeB - timeA).ToString(@"mm\:ss\:fff")); ;
            if (useCache)
                Console.WriteLine("**注意：以上查询基于本地缓存文件**");
        }
        Asset[] QueryAssets(bool loadAllAssets = false)
        {
            if (ActiveQueryScheme is null)
                return new Asset[0];
            this.Assets.Clear();

            IEnumerable<ulong> assetTypeIDs = from at in ActiveQueryScheme.AssetTypes select Convert.ToUInt64(at?.ID);
            IEnumerable<ulong> projectIDs = from pj in ActiveQueryScheme.Projects select Convert.ToUInt64(pj?.ID);
            IEnumerable<IQueryCriteria> selectedLabels = ActiveQueryScheme.QueryCriterias.Where(a => a is QueryCriteria_Label);
            IEnumerable<ulong> selectedLabelIDs = from lab in selectedLabels select Convert.ToUInt64(((QueryCriteria_Label)lab).ID);

            SocketFunction.SendInstruct(App.Socket,
                "QueryAssets",
                new object[]
                {
                    assetTypeIDs.ToArray(),
                    projectIDs.ToArray(),
                    selectedLabelIDs.ToArray(),
                    loadAllAssets
                },
                out object[] result,
                out Exception ex);

            Asset[] re_Assets = (Asset[])result;//Unable to cast object of type 'ICE_Model.Project[]' to type 'ICE_Model.Asset[]'.

            this.Assets.AddRange(re_Assets);
            return re_Assets;
        }
        //void Query_(bool loadAllAssets = false)
        //{
        //    //if (ActiveQueryScheme is null)
        //    //    return;
        //    //var timeA = DateTime.Now;
        //    //this.Assets.Clear();

        //    //IEnumerable<ulong> assetTypeIDs = from at in ActiveQueryScheme.AssetTypes select Convert.ToUInt64(at?.ID);
        //    //IEnumerable<ulong> projectIDs = from pj in ActiveQueryScheme.Projects select Convert.ToUInt64(pj?.ID);
        //    //IEnumerable<IQueryCriteria> selectedLabels = ActiveQueryScheme.QueryCriterias.Where(a => a is QueryCriteria_Label);
        //    //IEnumerable<ulong> selectedLabelIDs = from lab in selectedLabels select Convert.ToUInt64(((QueryCriteria_Label)lab).ID);

        //    //SocketFunction.SendInstruct(App.Socket,
        //    //    "QueryAssets",
        //    //    new object[]
        //    //    {
        //    //        assetTypeIDs.ToArray(),
        //    //        projectIDs.ToArray(),
        //    //        selectedLabelIDs.ToArray(),
        //    //        loadAllAssets
        //    //    },
        //    //    out object[] result,
        //    //    out Exception ex);

        //    //Asset[] re_Assets = (Asset[])result;//Unable to cast object of type 'ICE_Model.Project[]' to type 'ICE_Model.Asset[]'.

        //    //this.Assets.AddRange(re_Assets);
        //    var timeA = DateTime.Now;
        //    Asset[] re_Assets = QueryAssets();

        //    List<AssetFileInfo> re_files = new List<AssetFileInfo>();
        //    IEnumerable<IQueryCriteria> keywords = this.ActiveQueryScheme.QueryCriterias.Where(a => a is QueryCriteria_Keyword);
        //    string sp = "*"; string reg = ".*";
        //    foreach (IQueryCriteria keyword in keywords)
        //    {
        //        sp += keyword.Name + "*";
        //        reg += keyword.Name + ".*";
        //    }
        //    re_files = AnalysisAssets(re_Assets,sp,reg);
        //    re_files.RemoveAll(a => a is null);
        //    re_files = re_files.GroupBy(x => x).Select(group => group.Key).ToList();
        //    re_files.RemoveAll(x => App.ExcludeAssetExtensions.Contains(Path.GetExtension(x.FullName)));

        //    this.AssetUsageLogs.Statistics(re_files);
        //    SortFiles(re_files);
        //    var timeB = DateTime.Now;
        //    Console.WriteLine("本次查询共用时：" + (timeB - timeA).ToString(@"mm\:ss\:fff")); ;
        //    if (useCache)
        //        Console.WriteLine("**注意：以上查询基于本地缓存文件**");
        //}
        //List<AssetFileInfo> AnalysisAssets_(Asset[] assets, string sp, string reg)
        //{
        //    List<AssetFileInfo> re_files = new List<AssetFileInfo>();
        //    foreach (Asset asset in assets)
        //    {
        //        var timeA = DateTime.Now;

        //        Console.WriteLine("开始分析资产：" + asset.Name);
        //        if (StopQueryProcess) { Console.WriteLine("查询过程被用户中止。"); break; }//++++++++ =》》》 检查是否要中止
        //        if (asset.IsFolder == true)//资产是文件夹
        //        {
        //            if (Directory.Exists(asset.FullPath)) //资产文件夹存在
        //            {
        //                //是否为序列文件夹
        //                bool isSequenceFolder = IsSequenceFolder(asset.FullPath);
        //                //关键字是否匹配资产名称
        //                bool assetNameMatched = Regex.IsMatch(asset.Name, reg);
        //                if (isSequenceFolder || ActiveQueryScheme.SearchFolder == true && Regex.IsMatch(asset.Name, reg)) //查找结果包含文件夹
        //                {
        //                    if (assetNameMatched)
        //                        re_files.Add(new AssetFileInfo(asset.FullPath)
        //                        {
        //                            Asset = asset,
        //                            ArchiveTime = asset.ArchiveTime,
        //                            Usage = asset.Usage,
        //                            IsRealAsset = true
        //                        });
        //                }
        //                if (isSequenceFolder) //如果这个资产文件夹是序列文件夹
        //                {
        //                    Console.WriteLine("找到序列文件夹：" + asset.FullPath);
        //                    continue;
        //                }
        //                if (IsNoPreviewFolder(asset.FullPath)) //如果这个资产文件夹有非预览标记
        //                {
        //                    Console.WriteLine("跳过非预览标记：" + asset.FullPath);
        //                    continue;
        //                }

        //                //是否有递归预览标记
        //                bool recursive = IsRecursion(asset.FullPath);
        //                if (recursive) //如果这个资产文件夹有递归预览标记
        //                {
        //                    Console.WriteLine("递归查询：" + asset.FullPath);
        //                }

        //                List<string> matchedNameSqDirs = new List<string>();
        //                List<string> sqDirs = new List<string>();
        //                List<string> matchedNameDirs = new List<string>();
        //                List<string> dirs = new List<string>();
        //                List<string> noPreviewDirs = new List<string>();
        //                string[] alldirs = GetDirectories(asset.FullPath, recursive ? "*" : sp, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        //                for (int i = 0; i < alldirs.Length; i++)
        //                {
        //                    if (StopQueryProcess) { Console.WriteLine("查询过程被用户中止。"); break; }//++++++++ =》》》 检查是否要中止
        //                    string dir = alldirs[i];
        //                    if (IsSequenceFolder(dir) || IsNoPreviewFolder(dir))
        //                    {
        //                        bool dirNameMatched = Regex.IsMatch(dir, reg);
        //                        if (dirNameMatched)
        //                            matchedNameSqDirs.Add(dir);
        //                        else
        //                            sqDirs.Add(dir);
        //                    }
        //                    else
        //                    {
        //                        bool dirNameMatched = Regex.IsMatch(dir, reg);
        //                        if (dirNameMatched)
        //                            matchedNameDirs.Add(dir);
        //                        else
        //                            dirs.Add(dir);
        //                    }
        //                }

        //                List<string> files = new List<string>();

        //                if (assetNameMatched)//如果资产名称匹配
        //                {
        //                    //如果有递归标记则收集所有子目录中的文件，如果没有则只收集第一层文件。
        //                    string[] subFiles = GetFiles(asset.FullPath, "*.*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        //                    files.AddRange(subFiles);


        //                    if (ActiveQueryScheme.SearchFolder == true)//查找结果包含文件夹
        //                    {
        //                        files.AddRange(matchedNameDirs);
        //                        files.AddRange(dirs);
        //                    }

        //                    re_files.AddRange(from sqDir in sqDirs
        //                                      select new AssetFileInfo(sqDir)
        //                                      {
        //                                          Asset = asset,
        //                                          ArchiveTime = asset.ArchiveTime,
        //                                          IsSequenceFolder = true
        //                                      });
        //                }
        //                else//如果资产名称不匹配
        //                {
        //                    if (ActiveQueryScheme.SearchFolder == true && matchedNameDirs.Any())//如果查找结果包含文件夹
        //                        files.AddRange(matchedNameDirs);//添加所有名字匹配的子目录

        //                    //先在第一层中收集匹配关键字的文件
        //                    string[] subFiles = GetFiles(asset.FullPath, sp, SearchOption.TopDirectoryOnly);
        //                    files.AddRange(subFiles);

        //                    //在所有名称匹配的子目录中收集所有文件
        //                    foreach (string dir in matchedNameDirs)
        //                    {
        //                        if (StopQueryProcess) { Console.WriteLine("查询过程被用户中止。"); break; }//++++++++ =》》》 检查是否要中止
        //                        string[] subFiles2 = GetFiles(dir, "*", SearchOption.TopDirectoryOnly);
        //                        files.AddRange(subFiles2);
        //                    }

        //                    //在所有名称不匹配的子目录中收集名称匹配的文件
        //                    foreach (string dir in dirs)
        //                    {
        //                        if (StopQueryProcess) { Console.WriteLine("查询过程被用户中止。"); break; }//++++++++ =》》》 检查是否要中止
        //                        string[] subFiles2 = GetFiles(dir, sp, SearchOption.TopDirectoryOnly);
        //                        files.AddRange(subFiles2);
        //                    }
        //                }

        //                re_files.AddRange(from file in files
        //                                  select new AssetFileInfo(file)
        //                                  {
        //                                      Asset = asset,
        //                                      ArchiveTime = asset.ArchiveTime
        //                                  });
        //                re_files.AddRange(from sqDir in matchedNameSqDirs
        //                                  select new AssetFileInfo(sqDir)
        //                                  {
        //                                      Asset = asset,
        //                                      ArchiveTime = asset.ArchiveTime,
        //                                      IsSequenceFolder = true
        //                                  });
        //            }
        //            else
        //            {
        //                re_files.Add(new AssetFileInfo(asset.FullPath)
        //                {
        //                    Asset = asset,
        //                    ArchiveTime = asset.ArchiveTime,
        //                    Usage = asset.Usage,
        //                    IsRealAsset = false
        //                });
        //            }
        //        }
        //        else //资产是文件
        //            if (Regex.IsMatch(asset.Name, reg))
        //            re_files.Add(new AssetFileInfo(asset.FullPath)
        //            {
        //                Asset = asset,
        //                ArchiveTime = asset.ArchiveTime,
        //                Usage = asset.Usage,
        //                IsRealAsset = true
        //            });
           
        //        var timeB = DateTime.Now;
        //        Console.WriteLine("  -资产：" + asset.Name + " 分析完成，用时" + (timeB - timeA).ToString(@"mm\:ss\:fff"));
        //    }
        //    return re_files;
        //}
        List<AssetFileInfo> AnalysisAssets(Asset[] assets,string sp,string reg, string[] sws)
        {
            ConcurrentBag<AssetFileInfo> re_files = new ConcurrentBag<AssetFileInfo>();
            Parallel.ForEach(assets, (asset, ParallelLoopState) => 
            {
                var timeA = DateTime.Now;

                List<AssetFileInfo> newFileInfos = new List<AssetFileInfo>();
                Console.WriteLine("开始分析资产：" + asset.Name);
                //检查是否要中止分析过程
                if (StopQueryProcess) { Console.WriteLine("查询过程被用户中止。"); ParallelLoopState.Stop();return; }
                //如果资产是文件夹
                if (asset.IsFolder == true)
                {
                    if (asset.Exists(UseCache)) //资产文件夹存在
                    {
                        //是否为序列文件夹
                        bool isSequenceFolder = IsSequenceFolder(asset.FullPath);
                        //关键字是否匹配资产名称或上传者
                        bool assetNameMatched = Regex.IsMatch(asset.Name.ToLower(), reg.ToLower()) || sp.Contains(asset.UploaderName);
                        //如果选定了“查找结果包含文件夹”
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(asset.Name + "  " + reg + "  " + Regex.IsMatch(asset.Name, reg).ToString());
                        Console.ForegroundColor = ConsoleColor.Gray;

                        if (isSequenceFolder || (ActiveQueryScheme.SearchFolder == true && Regex.IsMatch(asset.Name.ToLower(), reg.ToLower())))
                        {
                            if (assetNameMatched)
                            {
                                newFileInfos.Add(new AssetFileInfo(asset.FullPath, UseCache)
                                {
                                    Asset = asset,
                                    ArchiveTime = asset.ArchiveTime,
                                    Usage = asset.Usage,
                                    IsRealAsset = true
                                });
                            }
                        }
                        if (isSequenceFolder) //如果这个资产文件夹是序列文件夹
                        {
                            Console.WriteLine("找到序列文件夹：" + asset.FullPath);
                            re_files.AddRange(newFileInfos);
                            var timeB_ = DateTime.Now;
                            Console.WriteLine("  -资产：" + asset.Name + " 分析完成，用时" + (timeB_ - timeA).ToString(@"mm\:ss\:fff"));
                            return;
                        }
                        if (IsNoPreviewFolder(asset.FullPath)) //如果这个资产文件夹有非预览标记
                        {
                            Console.WriteLine("跳过非预览标记：" + asset.FullPath);
                            re_files.AddRange(newFileInfos);
                            var timeB_ = DateTime.Now;
                            Console.WriteLine("  -资产：" + asset.Name + " 分析完成，用时" + (timeB_ - timeA).ToString(@"mm\:ss\:fff"));
                            return;
                        }

                        //是否有递归预览标记
                        bool recursive = IsRecursion(asset.FullPath);
                        if (recursive) //如果这个资产文件夹有递归预览标记
                            Console.WriteLine("递归查询：" + asset.FullPath);

                        List<string> matchedNameSqDirs = new List<string>();
                        List<string> sqDirs = new List<string>();
                        List<string> matchedNameDirs = new List<string>();
                        List<string> dirs = new List<string>();
                        List<string> noPreviewDirs = new List<string>();
                        string[] alldirs = GetDirectories(asset.FullPath, recursive ? "*" : sp, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                        for (int i = 0; i < alldirs.Length; i++)
                        {
                            if (StopQueryProcess) { Console.WriteLine("查询过程被用户中止。"); break; }//++++++++ =》》》 检查是否要中止
                            string dir = alldirs[i];
                            if (IsSequenceFolder(dir) || IsNoPreviewFolder(dir))
                            {
                                bool dirNameMatched = Regex.IsMatch(dir.ToLower(), reg.ToLower());
                                if (dirNameMatched)
                                    matchedNameSqDirs.Add(dir);
                                else
                                    sqDirs.Add(dir);
                            }
                            else
                            {
                                bool dirNameMatched = Regex.IsMatch(dir.ToLower(), reg.ToLower());
                                if (dirNameMatched)
                                    matchedNameDirs.Add(dir);
                                else
                                    dirs.Add(dir);
                            }
                        }

                        List<string> files = new List<string>();
                        if (assetNameMatched)//如果资产名称匹配
                        {
                            //如果有递归标记则收集所有子目录中的文件，如果没有则只收集第一层文件。
                            string[] subFiles = GetFiles(asset.FullPath, "*.*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                            files.AddRange(subFiles);


                            if (ActiveQueryScheme.SearchFolder == true)//查找结果包含文件夹
                            {
                                files.AddRange(matchedNameDirs);
                                files.AddRange(dirs);
                            }

                            newFileInfos.AddRange(from sqDir in sqDirs
                                                  select new AssetFileInfo(sqDir, UseCache)
                                                  {
                                                      Asset = asset,
                                                      ArchiveTime = asset.ArchiveTime,
                                                      IsSequenceFolder = true
                                                  });
                        }
                        else//如果资产名称不匹配
                        {
                            if (ActiveQueryScheme.SearchFolder == true && matchedNameDirs.Any())//如果查找结果包含文件夹
                                files.AddRange(matchedNameDirs);//添加所有名字匹配的子目录

                            //先在第一层中收集匹配关键字的文件
                            string[] subFiles = GetFiles(asset.FullPath, sp, SearchOption.TopDirectoryOnly);
                            files.AddRange(subFiles);

                            //在名称匹配的子目录中收集所有文件
                            foreach (string dir in matchedNameDirs)
                            {
                                if (StopQueryProcess) { Console.WriteLine("查询过程被用户中止。"); break; }//++++++++ =》》》 检查是否要中止
                                string[] subFiles2 = GetFiles(dir, "*", SearchOption.TopDirectoryOnly);
                                files.AddRange(subFiles2);
                            }

                            //在名称不匹配的子目录中收集名称匹配的文件
                            foreach (string dir in dirs)
                            {
                                if (StopQueryProcess) { Console.WriteLine("查询过程被用户中止。"); break; }//++++++++ =》》》 检查是否要中止
                                string[] subFiles2 = GetFiles(dir, sp, SearchOption.TopDirectoryOnly);
                                files.AddRange(subFiles2);
                            }
                        }

                        newFileInfos.AddRange(from file in files
                                              select new AssetFileInfo(file, UseCache)
                                              {
                                                  Asset = asset,
                                                  ArchiveTime = asset.ArchiveTime
                                              });
                        newFileInfos.AddRange(from sqDir in matchedNameSqDirs
                                              select new AssetFileInfo(sqDir, UseCache)
                                              {
                                                  Asset = asset,
                                                  ArchiveTime = asset.ArchiveTime,
                                                  IsSequenceFolder = true
                                              });
                    }
                    else
                    {
                        newFileInfos.Add(new AssetFileInfo(asset.FullPath, UseCache)
                        {
                            Asset = asset,
                            ArchiveTime = asset.ArchiveTime,
                            Usage = asset.Usage,
                            IsRealAsset = false
                        });
                    }
                }
                //如果资产是文件
                else if (Regex.IsMatch(asset.Name.ToLower(), reg.ToLower()))
                {
                    newFileInfos.Add(new AssetFileInfo(asset.FullPath, UseCache)
                    {
                        Asset = asset,
                        ArchiveTime = asset.ArchiveTime,
                        Usage = asset.Usage,
                        IsRealAsset = true
                    });
                }

                ConcurrentBag<AssetFileInfo> newFileInfos_sws = new ConcurrentBag<AssetFileInfo>();
                if (sws.Any())
                    Parallel.ForEach(newFileInfos, (nfi) =>
                    {
                        bool matched = false;
                        foreach (var sw in sws)
                        {
                            if (nfi.GetProperties().Contains(sw))
                                matched = true;
                        }
                        if(matched)
                            newFileInfos_sws.Add(nfi);
                    });

                re_files.AddRange(sws.Any() ? newFileInfos_sws: newFileInfos);
                var timeB = DateTime.Now;
                Console.WriteLine("  -资产：" + asset.Name + " 分析完成，用时" + (timeB - timeA).ToString(@"mm\:ss\:fff"));
            });
            return re_files.ToList();
        }

        //void QueryAssets(bool loadAllAssets = false)
        //{
        //    if (ActiveQueryScheme is null)
        //        return;

        //    LabelGroups.Clear();
        //    ulong personID = Convert.ToUInt64(Me.ID);
        //    IEnumerable<ulong> assetTypeIDs = from at in ActiveQueryScheme.AssetTypes select Convert.ToUInt64(at?.ID);
        //    AssetTypeQueryScheme assetTypeQueryScheme = ActiveQueryScheme.AssetTypeQueryScheme;
        //    IEnumerable<ulong> projectIDs = from pj in ActiveQueryScheme.Projects select Convert.ToUInt64(pj?.ID);
        //    ProjectQueryScheme projectQueryScheme = ActiveQueryScheme.ProjectQueryScheme;
        //    IEnumerable<IQueryCriteria> selectedLabels = ActiveQueryScheme.QueryCriterias.Where(a => a is QueryCriteria_Label);
        //    IEnumerable<ulong> selectedLabelIDs = from lab in selectedLabels select Convert.ToUInt64(((QueryCriteria_Label)lab).ID);

        //    SocketFunction.SendInstruct(App.Socket,
        //        "LoadLabelGroupsAndAssets",
        //        new object[]
        //        {
        //            personID,
        //            assetTypeIDs.ToArray(),
        //            (int)assetTypeQueryScheme,
        //            projectIDs.ToArray(),
        //            (int)projectQueryScheme,
        //            selectedLabelIDs.ToArray(),
        //            ActiveQueryScheme.LoadAllLabelGroup,
        //            loadAllAssets
        //        },
        //        out object[] result,
        //        out Exception ex);

        //    AssetLabelGroup[] re_LabelGroups = (AssetLabelGroup[])result[0];
        //    Asset[] re_Assets = (Asset[])result[1];

        //    this.LabelGroups.Clear();
        //    this.LabelGroups.AddRange(re_LabelGroups);
        //    this.Assets.Clear();
        //    this.Assets.AddRange(re_Assets);

        //    List<AssetFileInfo> re_files = new List<AssetFileInfo>();
        //    IEnumerable<IQueryCriteria> keywords = this.ActiveQueryScheme.QueryCriterias.Where(a => a is QueryCriteria_Keyword);
        //    string sp = "*"; string reg = ".*";
        //    foreach (var keyword in keywords)
        //    {
        //        sp += keyword.Name + "*";
        //        reg += keyword.Name + ".*";
        //    }
        //    foreach (Asset asset in re_Assets)
        //    {
        //        string ext = Path.GetExtension(asset.FullPath);
        //        if (string.IsNullOrEmpty(ext))//资产是文件夹
        //            if (Directory.Exists(asset.FullPath)) //资产文件夹存在
        //            {

        //                if (ActiveQueryScheme.SearchFolder == true && Regex.IsMatch(asset.Name, reg)) //查找结果包含文件夹
        //                {
        //                    re_files.Add(new AssetFileInfo(asset.FullPath)
        //                    {
        //                        ID_Asset = asset.ID,
        //                        ArchiveTime = asset.ArchiveTime,
        //                        Usage = asset.Usage,
        //                        IsRealAsset = true
        //                    });
        //                    if (IsSequenceFolder(asset.FullPath))//如果这个资产文件夹是序列文件夹
        //                        continue;
        //                }

        //                List<string> sqDirs = new List<string>();
        //                List<string> dirs = new List<string>();
        //                string[] alldirs = Directory.GetDirectories(asset.FullPath, sp, SearchOption.AllDirectories);
        //                for (int i = 0; i < alldirs.Length; i++)
        //                {
        //                    string dir = alldirs[i];
        //                    if (IsSequenceFolder(dir))
        //                        sqDirs.Add(dir);
        //                    else
        //                        dirs.Add(dir);
        //                }

        //                List<string> files = new List<string>();
        //                if (ActiveQueryScheme.SearchFolder == true) //查找结果包含文件夹
        //                {
        //                    files.AddRange(dirs);
        //                    //files = Directory.GetFileSystemEntries(asset.FullPath, sp, SearchOption.AllDirectories);
        //                }
        //                //else
        //                //{ 
        //                //  files = Directory.GetFiles(asset.FullPath, sp, SearchOption.AllDirectories); 
        //                //}
        //                files.AddRange(Directory.GetFiles(asset.FullPath, sp, SearchOption.TopDirectoryOnly));
        //                foreach (var dir in dirs)
        //                {
        //                    files.AddRange(Directory.GetFiles(dir, sp, SearchOption.TopDirectoryOnly));
        //                }

        //                re_files.AddRange(from file in files
        //                                  select new AssetFileInfo(file)
        //                                  {
        //                                      ID_Asset = asset.ID,
        //                                      ArchiveTime = asset.ArchiveTime
        //                                  });

        //                re_files.AddRange(from file in sqDirs
        //                                  select new AssetFileInfo(file)
        //                                  {
        //                                      ID_Asset = asset.ID,
        //                                      ArchiveTime = asset.ArchiveTime,
        //                                      IsSequenceFolder = true
        //                                  });
        //            }
        //            else { }
        //        else //资产是文件
        //            if (Regex.IsMatch(asset.Name, reg))
        //            re_files.Add(new AssetFileInfo(asset.FullPath)
        //            {
        //                ID_Asset = asset.ID,
        //                ArchiveTime = asset.ArchiveTime,
        //                Usage = asset.Usage,
        //                IsRealAsset = true
        //            });
        //    }
        //    re_files.RemoveAll(x => App.ExcludeAssetExtensions.Contains(Path.GetExtension(x.FullName)));

        //    //IEnumerable<IQueryCriteria> keywords = this.ActiveQueryScheme.QueryCriterias.Where(a => a is QueryCriteria_Keyword);
        //    //if (keywords.Any())
        //    //{
        //    //    for (int i = 0; i < re_files.Count; i++)
        //    //    {
        //    //        AssetFileInfo file = re_files[i];
        //    //        string name = file.Name;
        //    //        bool matched = true;
        //    //        foreach (IQueryCriteria qc in keywords)
        //    //        {
        //    //            string word = ((QueryCriteria_Keyword)qc).Keyword;
        //    //            if (!name.Contains(word))
        //    //                matched = false;
        //    //        }
        //    //        if (!matched)
        //    //        {
        //    //            re_files.RemoveAt(i);
        //    //            i--;
        //    //        }
        //    //    }
        //    //}

        //    //this.Files.Clear();
        //    //this.Files.AddRange(re_files);
        //    this.AssetUsageLogs.Statistics(re_files);
        //    SortFiles(re_files);
        //    //await ShowAssetsPreview();
        //}
        void LoadLabelGroups(bool AllLabelGroups = true)
        {
            if (ActiveQueryScheme is null)
                return;

            LabelGroups.Clear();
            IEnumerable<ulong> assetTypeIDs = from at in ActiveQueryScheme.AssetTypes select Convert.ToUInt64(at?.ID);
            IEnumerable<ulong> projectIDs = from pj in ActiveQueryScheme.Projects select Convert.ToUInt64(pj?.ID);
            IEnumerable<IQueryCriteria> selectedLabels = ActiveQueryScheme.QueryCriterias.Where(a => a is QueryCriteria_Label);
            IEnumerable<ulong> selectedLabelIDs = from lab in selectedLabels select Convert.ToUInt64(((QueryCriteria_Label)lab).ID);

            SocketFunction.SendInstruct(App.Socket,
                "LoadLabelGroups",
                new object[]
                {
                    AllLabelGroups,
                    assetTypeIDs.ToArray(),
                    projectIDs.ToArray(),
                    selectedLabelIDs.ToArray(),
                },
                out object[] result,
                out Exception ex);

            AssetLabelGroup[] re_LabelGroups = (AssetLabelGroup[])result;

            var indexA = this.LabelGroups_ListView.SelectedIndex;
            var indexB = this.Labels_ListView.SelectedIndex;
            this.LabelGroups.Clear();
            this.LabelGroups.AddRange(re_LabelGroups);
            this.LabelGroups_ListView.SelectedIndex = indexA;
            this.Labels_ListView.SelectedIndex = indexB;
        }
        void SortFiles(IEnumerable<AssetFileInfo> fileInfos)
        {
            if (TokenSources.Any())
                this.TokenSources.Dequeue().Cancel();
            this.Dispatcher.Invoke(() =>
            {
                this.Sorting_Button.Content = ActiveQueryScheme.Sort ? "正" : "倒";
            });
            List<AssetFileInfo> sorted = new List<AssetFileInfo>();
            if (ActiveQueryScheme.SortScheme == SortScheme.Usage)
            {
                if (ActiveQueryScheme.Sort)
                    sorted = fileInfos.OrderBy(a => a.Usage).ToList();
                else
                    sorted = fileInfos.OrderByDescending(a => a.Usage).ToList();
            }
            if (ActiveQueryScheme.SortScheme == SortScheme.FileName)
            {
                if (ActiveQueryScheme.Sort)
                    sorted = fileInfos.OrderBy(a => a.Name).ToList();
                else
                    sorted = fileInfos.OrderByDescending(a => a.Name).ToList();
            }
            if (ActiveQueryScheme.SortScheme == SortScheme.Extensions)
            {
                if (ActiveQueryScheme.Sort)
                    sorted = fileInfos.OrderBy(a => a.Extension).ToList();
                else
                    sorted = fileInfos.OrderByDescending(a => a.Extension).ToList();
            }
            if (ActiveQueryScheme.SortScheme == SortScheme.ArchiveDate)
            {
                if (ActiveQueryScheme.Sort)
                    sorted = fileInfos.OrderBy(a => a.ArchiveTime).ToList();
                else
                    sorted = fileInfos.OrderByDescending(a => a.ArchiveTime).ToList();
            }
            if (ActiveQueryScheme.SortScheme == SortScheme.CreationTime)
            {
                if (ActiveQueryScheme.Sort)
                    sorted = fileInfos.OrderBy(a => a.CreationTime).ToList();
                else
                    sorted = fileInfos.OrderByDescending(a => a.CreationTime).ToList();
            }
            if (ActiveQueryScheme.SortScheme == SortScheme.LastAccessTime)
            {
                if (ActiveQueryScheme.Sort)
                    sorted = fileInfos.OrderBy(a => a.LastAccessTime).ToList();
                else
                    sorted = fileInfos.OrderByDescending(a => a.LastAccessTime).ToList();
            }
            if (ActiveQueryScheme.SortScheme == SortScheme.LastWriteTime)
            {
                if (ActiveQueryScheme.Sort)
                    sorted = fileInfos.OrderBy(a => a.LastWriteTime).ToList();
                else
                    sorted = fileInfos.OrderByDescending(a => a.LastWriteTime).ToList();
            }
            Files.Clear();
            Files.AddRange(sorted);
            SetPage();
        }
        async Task ShowAssetsPreview(CancellationTokenSource tokenSource)
        {
            //List<TimeSpan> ts = new List<TimeSpan>();
            await Task.Run(async () =>
                {
                    Thread.Sleep(370);
                    for (int i = 0; i < Assets_ListView.Items.Count; i++)
                    {
                        if (tokenSource.IsCancellationRequested)
                        {
                            break;
                        }
                        //DateTime t1 = DateTime.Now;

                        object item = Assets_ListView.Items[i];
                        DependencyObject container = this.Assets_ListView.ItemContainerGenerator.ContainerFromItem(item);
                        await App.Current.Dispatcher.InvokeAsync(() =>
                        {
                            AssetIcon icon = FindVisualChild<AssetIcon>(container);
                            icon?.Dispatcher.InvokeAsync(() => { icon?.BindingPreview(); }, System.Windows.Threading.DispatcherPriority.SystemIdle);
                        }, System.Windows.Threading.DispatcherPriority.SystemIdle);

                        //DateTime t2 = DateTime.Now;
                        //ts.Add((t2 - t1));
                        //Console.WriteLine(t1.ToString() + "  -   " + t2.ToString() + "   =   " + (t2 - t1).ToString());
                    }
                });
            //TimeSpan at = TimeSpan.Zero;
            //foreach (var t in ts)
            //{
            //    at += t;
            //}
            //Console.WriteLine("AllTimeSpan:" +  at);

        }
        //public async Task<BitmapImage> UriToImageAsync(string uri)
        //{
        //    if (string.IsNullOrEmpty(uri))
        //        return new BitmapImage();
        //    BitmapImage re = await System.Threading.Tasks.Task.Run(() =>
        //    {
        //        try
        //        {
        //            BitmapImage image = new BitmapImage();
        //            image.BeginInit();
        //            image.CacheOption = BitmapCacheOption.None;
        //            Stream stream = new FileStream(uri, FileMode.Open);
        //            image.StreamSource = stream;
        //            image.DecodePixelHeight = (int)100;
        //            image.EndInit();
        //            stream.Close();
        //            stream.Dispose();
        //            return image;
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine(ex.Message + " >" + uri);
        //            return new BitmapImage();
        //        }
        //    });
        //    return re;
        //}

        private ChildType FindVisualChild<ChildType>(DependencyObject obj) where ChildType : DependencyObject
        {
            if (obj is null)
                return default(ChildType);
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is ChildType)
                {
                    return child as ChildType;
                }
                else
                {
                    ChildType childOfChildren = FindVisualChild<ChildType>(child);
                    if (childOfChildren != null)
                    {
                        return childOfChildren;
                    }
                }
            }
            return null;
        }

        private void CreatAsset_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var win = new Win_AssetCreator(
                new string[0],
                new AssetFileMd5Info[] { },
                new AssetLabel[] { },
                new Project[] { },
                new AssetType[] { }
            );
            win.Show();
        }
        private void TestFunc_MenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private async Task<string[]> CheckDrop(string[] files)
        {
            List<string> messageslist = new List<string>();

            bool havesubfolder = false;
            foreach (string path in files)
            {
                var isfolder = (File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory;
                if (!isfolder)
                    continue;
                if (IsRecursion(path) || IsNoPreviewFolder(path))
                    continue;
                var subdirs = Directory.GetDirectories(path);
                if (subdirs.Any())
                {
                    havesubfolder = true;
                    break;
                }
            }
            if (havesubfolder)
                messageslist.Add("要上传的资产包含子目录，其子目录中的内容不会被查询和预览;");

            //List<AssetFileMd5Info> allfiles = new List<AssetFileMd5Info>();
            _md5Infos.Clear();
            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i];
                if ((File.GetAttributes(file) & FileAttributes.Directory) == FileAttributes.Directory)//如果资产是目录
                {
                    Func<string, string[]> getDirs = null;
                    getDirs = (dir) =>
                    {
                        var np = Directory.GetFiles(dir, ".nopreview", SearchOption.TopDirectoryOnly);
                        if (np.Any())
                        {
                            Console.WriteLine("跳过非预览标记：" + dir);
                            _md5Infos.Add(new AssetFileMd5Info() { SourceAssetDir = file, SourcePath = file, Skip = true });
                            return new string[0];
                        }
                        List<string> reDirs = new List<string>
                        {
                            dir
                        };
                        string[] subDirs = Directory.GetDirectories(dir);
                        foreach (var subDir in subDirs)
                        {
                            reDirs.AddRange(getDirs(subDir));
                        }
                        return reDirs.ToArray();
                    };
                    string[] allDirs = getDirs(file);

                    foreach (var dir in allDirs)
                    {
                        Console.WriteLine("正在收集资产目录中的文件：" + dir);
                        string[] getedFiles = Directory.GetFiles(dir, "*", SearchOption.TopDirectoryOnly);
                        //_md5Infos.AddRange(from f in getedFiles select new AssetFileMd5Info() { SourceAssetDir = file, SourcePath = f });
                        foreach (var f in getedFiles)
                        {
                            var len = new FileInfo(f).Length;
                            if (len > 1024)
                                _md5Infos.Add(new AssetFileMd5Info() { SourceAssetDir = file, SourcePath = f });
                            else
                                Console.WriteLine("跳过小于1Kb的文件：" + f);
                        }
                    }
                }
                else//如果资产是文件
                {
                    Console.WriteLine("收集资产文件：" + file);
                    _md5Infos.Add(new AssetFileMd5Info() { SourceAssetDir = file, SourcePath = file });
                }
            }
            var _repetitives = await CheckFileMD5(_md5Infos.ToArray());
            if (_repetitives.Any())
            {
                List<string> repetitives = _repetitives.ToList();
                for (int i = 0; i < repetitives.Count; i++)
                {
                    string rep = repetitives[i];
                    if (!File.Exists(rep))
                    {
                        repetitives.RemoveAt(i);
                        i--;
                    }
                }
                if (repetitives.Any())
                {
                    var repNames = from p in repetitives select Path.GetFileName(p) + " 在 " + Path.GetDirectoryName(p) + " 中;";
                    messageslist.Add("下列资产已存在于资产库中：\r\t" + repNames.AllToString(separator: "\r\t"));
                }
            }


            return messageslist.ToArray();
        }
        private string _temporaryFile = null;
        private async void Assets_ListView_Drop(object sender, DragEventArgs e)
        {
            string[] files = null;
            if (e is null)
                if (_temporaryFile is not null)
                    files = new string[] { _temporaryFile };
                else { return; }
            else
                files = (string[])e.Data.GetData(DataFormats.FileDrop);

            var messages = await CheckDrop(files);
            if (messages.Any())
            {
                string str = "在拖入的资产中发现如下问题：\r";
                for (int i = 0; i < messages.Length; i++)
                {
                    string message = messages[i];
                    str += "  " + (i + 1).ToString() + "、" + message + "\r";
                }
                str += "是否仍要继续？";
                var boxre = MessageBox.Show(str, "温馨提示", MessageBoxButton.YesNo);
                if (boxre == MessageBoxResult.No)
                    return;
            }
            var win = new Win_AssetCreator(files, _md5Infos.ToArray(), new AssetLabel[] { }, new Project[] { }, new AssetType[] { });
            win.Show();
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            if (_exit)
                Environment.Exit(0);
        }

        IQueryCriteria _mouceFocusSelectedLabel = null;
        private async void TextLabel_Removing(object sender, RoutedEventArgs e)
        {
            if (_mouceFocusSelectedLabel is null)
                return;
            this.ActiveQueryScheme.QueryCriterias.Remove(_mouceFocusSelectedLabel);
            await QueryAssets_Async();
        }
        private void TextLabel_MouseEnter(object sender, MouseEventArgs e)
        {
            var item = Extensions.GetElementUnderMouse<ListViewItem>();
            if (item is null)
                return;
            _mouceFocusSelectedLabel = (IQueryCriteria)item.Content;
        }
        private void Func01_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("  功能研发中，敬请期待！", "温馨提示");
        }

        bool CheckBoxEventEnabled = true;

        private async void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!CheckBoxEventEnabled)
                return;
            CheckBoxEventEnabled = false;
            CheckingTreeItem Content = (CheckingTreeItem)((CheckBox)sender).Content;
            SelectOrRemoveAssettypeOrProject(Content, true);
            //IEnumerable<CheckingTreeItem> parents = Content.GetParentNodes();
            //foreach (CheckingTreeItem node in parents)
            //{
            //    node.IsSelected = true;
            //    SelectOrRemoveAssettypeOrProject(node, true);
            //}
            //IEnumerable<CheckingTreeItem> childs = Content.GetAllChildren();
            //foreach (var child in childs)
            //{
            //    child.IsSelected = true;
            //    SelectOrRemoveAssettypeOrProject(child, true);
            //}
            CheckBoxEventEnabled = true;
            //await QueryAssets_Async();
        }
        private async void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!CheckBoxEventEnabled)
                return;
            CheckBoxEventEnabled = false;
            CheckingTreeItem Content = (CheckingTreeItem)((CheckBox)sender).Content;
            SelectOrRemoveAssettypeOrProject(Content, false);
            IEnumerable<CheckingTreeItem> childs = Content.GetAllChildren();
            foreach (var child in childs)
            {
                child.IsSelected = false;
                SelectOrRemoveAssettypeOrProject(child, false);
            }
            CheckBoxEventEnabled = true;
            //await QueryAssets_Async();
        }
        void SelectOrRemoveAssettypeOrProject(CheckingTreeItem item, bool select)
        {
            if (item.Content is AssetType type)
            {
                if (select)
                {
                    if (!this.ActiveQueryScheme.AssetTypes.Contains(type))
                        this.ActiveQueryScheme.AssetTypes.Add(type);
                }
                else
                {
                    var findeds = this.ActiveQueryScheme.AssetTypes.Where(x => x.ID.Equals(type.ID)).ToArray();
                    foreach (var finded in findeds)
                    {
                        this.ActiveQueryScheme.AssetTypes.Remove(finded);
                    }
                }
                //var at = from x in _AllAssetTypes_NoTree.Where(a => a.IsSelected) select (AssetType)x.Content;
                //this.ActiveQueryScheme.AssetTypes.Clear();
                //this.ActiveQueryScheme.AssetTypes.AddRange(at);
            }
            if (item.Content is Project pj)
            {
                if (select)
                {
                    if (!this.ActiveQueryScheme.Projects.Contains(pj))
                        this.ActiveQueryScheme.Projects.Add(pj);
                }
                else
                {
                    var findeds = this.ActiveQueryScheme.Projects.Where(x => x.ID.Equals(pj.ID)).ToArray();
                    foreach (var finded in findeds)
                    {
                        this.ActiveQueryScheme.Projects.Remove(finded);
                    }
                }
                //var at = from x in _AllProjects_NoTree.Where(a => a.IsSelected) select (Project)x.Content;
                //this.ActiveQueryScheme.Projects.Clear();
                //this.ActiveQueryScheme.Projects.AddRange(at);
            }
        }

        private void AssetIcon_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = Assets_ListView.SelectedItem as AssetFileInfo;
            if (item is null) return;
            if (File.Exists(item.FullName) || Directory.Exists(item.FullName))
                OpenFiles();
        }
        //private async void AssetIcon_Loaded(object sender, RoutedEventArgs e)
        //{
        //await Task.Run(() => 
        //{
        //    Application.Current.Dispatcher.Invoke(() =>
        //    {
        //        ((AssetIcon)e.Source).BindingPreview_Out();
        //    });
        //});
        //this.Dispatcher.BeginInvoke(() => 
        //{
        //((AssetIcon)e.Source).BindingPreview_Out();
        //});
        //}
    }
    public enum AssetShowMode { Icon, List }
    public class AssetFileInfo : INotifyPropertyChanged
    {
        private int usage;
        private readonly string fullName;
        public string FullName { get => FileInfo?.FullName ?? fullName; }
        public string Name { get => FileInfo?.Name ?? Path.GetFileName(fullName); }
        public string Size
        {
            get
            {
                if (!File.Exists(FullName) || FileInfo is null) return string.Empty;
                return FileInfo is System.IO.FileInfo ? ((System.IO.FileInfo)FileInfo).Length.BytesToReadableValue() : string.Empty;
            }
        }
        public DateTime? CreationTime { get => FileInfo?.CreationTime; }
        public DateTime? LastWriteTime { get => FileInfo?.LastWriteTime; }
        public DateTime? LastAccessTime { get => FileInfo?.LastAccessTime; }
        public DateTime ArchiveTime { get; set; }
        public string Extension
        {
            get
            {
                //string ex = FileInfo?.Extension ?? Path.GetExtension(fullName);
                if (IsSequenceFolder)
                    return "[Sequence]";
                if (IsFolder)
                    return "[Folder]";
                return FileInfo?.Extension ?? Path.GetExtension(fullName);
            }
        }
        public int Usage
        {
            get => usage; set
            {
                usage = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Usage)));
            }
        }
        public bool IsRealAsset { get; set; } = false;
        public bool IsFolder
        {
            get
            {
                if (FileInfo is null)
                    if (string.IsNullOrWhiteSpace(Path.GetExtension(FullName)))
                        return true;
                    else
                        return false;
                else
                    return FileInfo is DirectoryInfo;
            }
        }
        public AssetFileInfo(string filePath, bool simpleMode = false) 
        {
            if (simpleMode)
            {
                fullName = filePath;
            }
            else
            {
                if (!File.Exists(filePath) && !Directory.Exists(filePath))
                    FileInfo = new FileInfo(filePath);
                else if ((File.GetAttributes(filePath) & FileAttributes.Directory) == FileAttributes.Directory)
                    FileInfo = new DirectoryInfo(filePath);
                else
                    FileInfo = new FileInfo(filePath);
            }
        }
        public AssetFileInfo(Asset asset) 
        {
            this.Asset = asset;
            this.FileInfo = new FileInfo(asset.FullPath);
        }
        public string[] GetProperties()
        {
            SetProperties();
            return Properties.ToArray();
        }
        public void SetProperties()
        {
            if (IsRealAsset)
                if (!Exists)
                    Properties.Add("missing");
        }
        public bool Exists { get => File.Exists(FullName) || Directory.Exists(FullName); }
        public bool IsSequenceFolder { get; set; }
        public AssetPreviewType AssetPreviewType
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FullName))
                    return AssetPreviewType.Unfound;
                if (!Exists)
                    return AssetPreviewType.Unfound;
                //if ((File.GetAttributes(FullName) & FileAttributes.Directory) == FileAttributes.Directory && Directory.Exists(FullName))
                if (IsFolder)
                    return AssetPreviewType.Folder;

                string exn = Path.GetExtension(FullName).ToLower();
                if (App.ImageEx.Contains(exn))
                {
                    return AssetPreviewType.Image;
                }
                if (App.ModelEx.Contains(exn))
                {
                    return AssetPreviewType.Model;
                }
                if (App.SoundEx.Contains(exn))
                {
                    return AssetPreviewType.Sound;
                }
                if (App.VideoEx.Contains(exn))
                {
                    return AssetPreviewType.Video;
                }
                return AssetPreviewType.Document;
            }
        }
        public Asset Asset { get; set; }
        private List<string> Properties { get; }= new List<string>();

        private FileSystemInfo fileSystemInfo;
        public FileSystemInfo FileInfo
        {
            get => fileSystemInfo; set
            {
                fileSystemInfo = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FileInfo"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FullName"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Name"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Size"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CreationTime"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("LastWriteTime"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("LastAccessTime"));
            }
        }
        public object ID_Asset { get => Asset?.ID; }
        public string UploaderName { get => Asset?.UploaderName; }
        public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString()
        {
            return FullName;
        }
        public override bool Equals(object obj)
        {
            if (obj is not AssetFileInfo)
                return false;
            var af = obj as AssetFileInfo;
            if (af is null)
                return false;
            return af.FullName.Equals(FullName);
        }

        public override int GetHashCode()
        {
            if (string.IsNullOrEmpty(FullName))
                return 0;
            return FullName.GetHashCode();
        }
    }

    public enum SortScheme
    {
        Usage, FileName, Extensions, ArchiveDate, CreationTime, LastAccessTime, LastWriteTime
    }

    public class Project_ComboBoxTreeItem : Project
    {
        public bool IsSelected { get; set; }
    }

    [ValueConversion(typeof(AssetPreviewType), typeof(Bitmap))]
    public class AssetExpresion_IconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var tvalue = (AssetPreviewType)value;
            switch (tvalue)
            {
                case AssetPreviewType.Other:
                    return AssetIcon.GetImageSouce(Properties.Resources.icon_17);
                case AssetPreviewType.Folder:
                    return AssetIcon.GetImageSouce(Properties.Resources.icon_24);
                case AssetPreviewType.Model:
                    return AssetIcon.GetImageSouce(Properties.Resources.icon_17);
                case AssetPreviewType.Sound:
                    return AssetIcon.GetImageSouce(Properties.Resources.icon_22);
                case AssetPreviewType.Image:
                    return AssetIcon.GetImageSouce(Properties.Resources.icon_16);
                case AssetPreviewType.Video:
                    return AssetIcon.GetImageSouce(Properties.Resources.icon_08);
                case AssetPreviewType.Document:
                    return AssetIcon.GetImageSouce(Properties.Resources.icon_17);
                case AssetPreviewType.Unfound:
                    return AssetIcon.GetImageSouce(Properties.Resources.icon_02);
                default:
                    return AssetIcon.GetImageSouce(Properties.Resources.icon_17);
            }

        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Properties.Resources.icon_17;
        }
    }

    [ValueConversion(typeof(double), typeof(double))]
    public class AssetFontSize_WidthValueConverter : IValueConverter
    {
        int c = 16;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Math.Pow((double)value - 7, 1.7) * c;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (double)value / c;
        }
    }
    [ValueConversion(typeof(double), typeof(double))]
    public class AssetFontSize_HeightValueConverter : IValueConverter
    {
        int c = 9;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Math.Pow((double)value - 7, 1.7) * c;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (double)value / c;
        }
    }
}
