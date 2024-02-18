using ICE_Model;
using LiZhenMySQL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using LiZhenStandard.Extensions;
using System.Diagnostics;
using System.Threading;
using System.Collections.Concurrent;

namespace ICE_BackEnd
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<MaintainFunction> MaintainFunctions { get; } = new ObservableCollection<MaintainFunction>();
        public void AddMaintainFunction(string name,string illustration, Func<object[], object> funcTask)
        {
            this.MaintainFunctions.Add(new MaintainFunction() { FunctionName = name,Illustration = illustration, Function = funcTask });
        }
        public MaintainFunction SelectedMaintainFunction { get => this.Maintain_ListView.SelectedItem as MaintainFunction; }

        public void SetMaintainFunctions()
        {
            AddMaintainFunction("资产标签管理器",
                "资产库的标签管理程序",
                new Func<object[], object>(args =>
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            Window window = new Win_AssetManager();
                            window.Show();
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.Message);
                        }
                    });
                    return null;
                }));

            AddMaintainFunction("扫描并更新资产MD5信息",
                "这个过程需要扫描所有资产文件，时间可能会比较长，具体应该会和给全部资产杀一遍毒差不多。",
                new Func<object[], object>(args =>
                {
                    try
                    {
                        Asset[] allAssets = DataBase.LoadFromDB_Where<Asset>();
                        App.SetAssetFullPath(allAssets);

                        List<string> _allFiles = new List<string>();
                        for (int i = 0; i < allAssets.Length; i++)
                        {
                            Asset asset = allAssets[i];
                            if (asset.IsFolder == true)//如果资产是目录
                            {
                                Func<string, string[]> getDirs = null;
                                getDirs = (dir) =>
                                {
                                    var np = Directory.GetFiles(dir, ".nopreview", SearchOption.TopDirectoryOnly);
                                    if (np.Any())
                                    {
                                        Console.WriteLine("跳过非预览标记：" + dir);
                                        return new string[0];
                                    }
                                    List<string> reDirs = new List<string>();
                                    reDirs.Add(dir);
                                    string[] subDirs = Directory.GetDirectories(dir);
                                    foreach (var subDir in subDirs)
                                    {
                                        reDirs.AddRange(getDirs(subDir));
                                    }
                                    return reDirs.ToArray();
                                };
                                string[] allDirs = getDirs(asset.FullPath);

                                foreach (var dir in allDirs)
                                {
                                    Console.WriteLine("正在收集资产目录中的文件：" + dir);
                                    _allFiles.AddRange(Directory.GetFiles(dir, "*", SearchOption.TopDirectoryOnly));
                                }
                            }
                            else//如果资产是文件
                            {
                                Console.WriteLine("收集资产文件：" + asset.FullPath);
                                _allFiles.Add(asset.FullPath);
                            }
                        }
                        string[] allFiles = _allFiles.GroupBy(x => x).Select(group => group.Key).ToArray();

                        List<AssetFileMd5Info> md5s = new List<AssetFileMd5Info>();
                        foreach (var file in allFiles)
                        {
                            if (!File.Exists(file))
                            {
                                Console.WriteLine("文件不存在：" + file);
                                continue;
                            }
                            long Size = new FileInfo(file).Length;
                            if (Size < 1024)
                            {
                                Console.WriteLine("跳过小于1Kb的文件：" + file);
                                continue;
                            }
                            Console.WriteLine("正在计算文件MD5值：" + file);
                            try
                            {
                                md5s.Add(new AssetFileMd5Info()
                                {
                                    Dir = Path.GetDirectoryName(file),
                                    Name = Path.GetFileName(file),
                                    Size = Size,
                                    MD5 = Path_Extensons.GetMD5FromFile(file)
                                });
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                        }

                        App.InsertAssetFileMD5(md5s.ToArray());
                        return null;
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message);
                        return null;
                    }
                }));

            AddMaintainFunction("重建资产库缓存文件",
                 "扫描资产库所有目录和文件，并制作缓存文件以供资产库客户端快速查询时调用，这个过程每隔一段时间会自动执行一次，默认间隔3小时，可在数据库中进行设置。执行该过程所需时间受资产库总大小影响，通常来说，每1TB大概需要40秒左右。",
                new Func<object[], object>(args => 
                {
                    App.CacheAssetLibrary_async();
                    return null; 
                }));

            AddMaintainFunction("备份数据库",
                 "备份数据库全部信息至PublicSettings表中的DatabaseBackupPath指定的目录",
                new Func<object[], object>(args =>
                {
                    App.DatabaseBackup_async();
                    return null;
                }));

            //AddMaintainFunction("整理数据库",
            //     "该功能将查找出数据库中已丢失连接的资产文件，并试图查找出数据库主目录已存在但没有与任何资产数据条目相关联的文件。",
            //    new Func<object[], object>(args =>
            //    {
            //        DBObjContainer<Asset> assetsList= new DBObjContainer<Asset>();
            //        assetsList.LoadFromDB_Where(null);
            //        Asset[] assets= new Asset[assetsList.Count()];
            //        App.SetAssetFullPath(assets);
            //        ConcurrentBag<Asset> re = new ConcurrentBag<Asset>();
            //        Parallel.ForEach(assets, (asset) => 
            //        {
            //            var fp = asset.FullPath;
            //            if (string.IsNullOrEmpty(Path.GetExtension(fp)))
            //            {
            //                if (!Directory.Exists(fp))
            //                    if (!File.Exists(fp))
            //                        re.Add(asset);
            //            }
            //            else
            //            {
            //                if (!File.Exists(fp))
            //                    if (!Directory.Exists(fp))
            //                        re.Add(asset);
            //            }
            //        });
            //        var file = Path.Combine(App.AssetLibraryBackEndDocumentDir, "FuncResult.txt");
            //        return null;
            //    }));
        }

        private void InvokeMaintainFunction_Button_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("确定要执行选定功能吗？", "提示", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;

            //这里未来需要优化成动态参数面板，并加入异常处理机制。
            SelectedMaintainFunction?.Invoke(null);
        }
    }

    public class MaintainFunction
    {
        public string FunctionName { get; set; }
        public string Illustration { get; set; }
        public Func<object[], object> Function { get; set; }

        public async Task<object> Invoke(params object[] arg)
        {
            object re = null;
            try
            {
                re = await System.Threading.Tasks.Task.Run(() => { return Function?.Invoke(arg); });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return re;
        }
    }
}
