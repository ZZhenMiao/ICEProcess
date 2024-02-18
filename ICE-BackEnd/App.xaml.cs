using System;
using System.Windows;
using LiZhenMySQL;
using ICE_Model;
using ICE_BackEnd.Properties;
using MySql.Data.MySqlClient;
using LiZhenStandard.Sockets;
using System.Net.Sockets;
using System.Collections.Generic;
using LiZhenStandard.Extensions;
using System.Linq;
using System.Diagnostics;
using LiZhenStandard;
using System.IO;
using System.DirectoryServices.ActiveDirectory;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;
using System.Security.Cryptography;
using System.Security.AccessControl;
using System.Net;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using System.Threading;
using MySqlX.XDevAPI.Relational;
using Microsoft.VisualBasic;

namespace ICE_BackEnd
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static Settings Settings { get => Settings.Default; }
        public static string PasswordKey { get => "ice"; }
        public static MySqlConnection MySqlConnection { get; set; }
        public static Socket MainSocket { get; set; }

        public static string MyDocumentDir { get => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); }
        public static string AssetLibraryBackEndDocumentDir { get => Path.Combine(MyDocumentDir, "AssetLibraryBackEnd"); }
        public static string LoginDataFile { get => Path.Combine(AssetLibraryBackEndDocumentDir, "Login.dat"); }

        public static LoginInfo LoginInfo { get; set; }



        /// <summary>
        /// 读取公共设置
        /// </summary>
        public static void LoadPublicSettings()
        {
            bool re = SocketFunction.InvokeInstruct("LoadPublicSettings", null, out object[] results); ;
            if (re)
            {
                try
                {
                    AssetLibraryCacheFolder = (string)PublicSetting.GetSettingsValue(results, "AssetLibraryCacheFolder");
                    AssetLibraryAutoCacheInterval = (string)PublicSetting.GetSettingsValue(results, "AssetLibraryAutoCacheInterval");
                    DatabaseBackupServerFolder = (string)PublicSetting.GetSettingsValue(results, "DatabaseBackupFolder");
                    DatabaseAutoBackupInterval = (string)PublicSetting.GetSettingsValue(results, "DatabaseAutoBackupInterval");
                }
                catch (Exception ex) { Console.WriteLine("未能正确加载公共配置。" + ex.Message); }
            }
        }
        /// <summary>
        /// 连接数据库并初始化业务逻辑
        /// </summary>
        /// <param name="ip">数据库IP</param>
        /// <param name="port">书库库端口</param>
        /// <param name="databaseName">数据库名</param>
        /// <param name="loginName">登录名</param>
        /// <param name="password">密码</param>
        /// <returns>是否成功</returns>
        public static bool Initialize(string ip,string port,string databaseName,string loginName,string password)
        {
            MySqlConnection = DataBase.NewConnection(ip,port, databaseName, loginName, password);
            if (!MySqlConnection.TryOpen(out string e))
            {
                MessageBox.Show(e,"连接失败");
                return false;
            }
            RegistSqlTypeConnection();
            App.RegisterInstruct();
            App.LoadPublicSettings();
            FileWatcherStart();
            AssetLibraryCacherStart();
            DatabaseBackupTimerStart();

            return true;
        }
        /// <summary>
        /// 启动服务以接收前端指令
        /// </summary>
        /// <returns>是否成功</returns>
        public static Exception StartUp()
        {
            try
            {
                MainSocket = SocketFunction.MakeTCPServer(string.Format("{0}:{1}", ICE_BackEnd.MainWindow.SelectedLocalHostIP.IP, ICE_BackEnd.MainWindow.LocalHostPort));
                SocketFunction.SetMainListenSocket(MainSocket);
                SocketFunction.StartMainListenTask();
            }
            catch (Exception e)
            {
                return e;
            }
            return null;
        }
        /// <summary>
        /// 将业务模型注册为数据表
        /// </summary>
        public static void RegistSqlTypeConnection()
        {
            List<Type> types = new() 
            {
                typeof(Person),
                typeof(Position),
                typeof(Obligation),
                typeof(Nas),
                typeof(Production),
                typeof(PublicSetting),
                typeof(Asset),
                typeof(AssetLabel),
                typeof(AssetLabelGroup),
                typeof(AssetType),
                typeof(Project),
                typeof(AutoArchiveScheme),
                typeof(AssetDirectory)

            };
            foreach (Type type in types)
            {
                DataBase.RegistSqlTypeConnection(type, MySqlConnection);
            }
            ChainingInfo.RegistChainTableInfo(typeof(AssetLabelGroup), typeof(AssetLabel), "ass_assetlabel_assetlabelgroup");
            ChainingInfo.RegistChainTableInfo(typeof(AssetType), typeof(AssetLabelGroup), "ass_assettype_assetlabelgroup");
            ChainingInfo.RegistChainTableInfo(typeof(Project), typeof(AssetLabelGroup), "ass_project_assetlabelgroup");
            ChainingInfo.RegistChainTableInfo(typeof(AutoArchiveScheme), typeof(AssetType), "ass_autoarchivescheme_assettype");
            ChainingInfo.RegistChainTableInfo(typeof(AutoArchiveScheme), typeof(Project), "ass_autoarchivescheme_project");
            ChainingInfo.RegistChainTableInfo(typeof(AutoArchiveScheme), typeof(AssetLabel), "ass_autoarchivescheme_assetlabel");
            ChainingInfo.RegistChainTableInfo(typeof(Person), typeof(Position), "ass_autoarchivescheme_assetlabel");
            ChainingInfo.RegistChainTableInfo(typeof(Position), typeof(Obligation), "ass_autoarchivescheme_assetlabel");
            ChainingInfo.RegistChainTableInfo(typeof(Asset), typeof(AssetLabel), "ass_asset_assetlabel");
            ChainingInfo.RegistChainTableInfo(typeof(Asset), typeof(AssetType), "ass_assettype_asset");
            ChainingInfo.RegistChainTableInfo(typeof(Asset), typeof(Project), "ass_project_asset");

        }
    }

    public partial class App : Application
    {
        public static List<AssetDirectory> AssetLibraryRootDirs { get; } = new List<AssetDirectory>();
        public static string AssetLibraryCacheFolder { get; set; }
        public static string AssetLibraryAutoCacheInterval { get; set; } = "180";
        public static string FileWatcherLogFolder { get => Path.Combine(AssetLibraryBackEndDocumentDir, "FileWatcherLogs"); }
        public static string AssetDirCacheTimerLogFile { get => Path.Combine(AssetLibraryBackEndDocumentDir, "AssetDirCacheTimerLog.dat"); }

        private static List<FileSystemWatcher> FileWatchers { get; } = new List<FileSystemWatcher>();
        private static List<string> FileWatcherLog { get; } = new List<string>();
        private static System.Timers.Timer SaveFileWatcherLogTimer { get; } = new System.Timers.Timer(1000 * 60 * 15);
        private static System.Timers.Timer AssetLibraryCacheTimer { get; } = new System.Timers.Timer(1000 * 60 * 30);
        private static void SaveFileWatcherLog()
        {
            try
            {
                IO_Extensons.WriteTextLogFiles(FileWatcherLogFolder, "FileWatcherLog", FileWatcherLog);
                Console.WriteLine("已更新资产库文件改动监听记录至：" + FileWatcherLogFolder);
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            FileWatcherLog.Clear();
        }
        private static void GetAssetLibraryRootDirs()
        {
            Nas[] allNas = DataBase.LoadFromDB_Where<Nas>();
            AssetDirectory[] rootAssetDirs = AssetDirectory.LoadRootsFromDB();
            foreach (var dir in rootAssetDirs)
            {
                dir.Nas = allNas.FindByProperty("ID", dir.ID_Nas);
            }
            AssetLibraryRootDirs.Clear();
            AssetLibraryRootDirs.AddRange(rootAssetDirs);
        }
        private static void FileWatcherStart() 
        {
            GetAssetLibraryRootDirs();
            foreach (var dir in AssetLibraryRootDirs)
            {
                FileWatchers.Add(new FileSystemWatcher(dir.GetFullPath()));
                Console.WriteLine("资产库主目录已加入监听列表： " + dir.GetFullPath());
            }

            void fileSystemEventHandler(object sender,FileSystemEventArgs e)
            {
                DateTime now = DateTime.Now;
                string str = string.Format("{0}\t{1}\t{2}\t{3}", now.ToString("yyyy/mm/dd"), now.ToString("hh:mm:ss"), e.ChangeType.ToString(), e.FullPath)  ;
                FileWatcherLog.Add(str);
                Console.WriteLine("检测到资产库文件改动：" + str);
                if (FileWatcherLog.Count > 1024 * 3)
                    SaveFileWatcherLog();
                var watcher = sender as FileSystemWatcher;
                watcher.EnableRaisingEvents = false;
                Thread.Sleep(250);
                watcher.EnableRaisingEvents = true;
            }
            foreach (var watcher in FileWatchers)
            {
                watcher.Changed += fileSystemEventHandler;
                watcher.Created += fileSystemEventHandler;
                watcher.Deleted += fileSystemEventHandler;
                watcher.Renamed += fileSystemEventHandler;
                watcher.IncludeSubdirectories = true;
                watcher.EnableRaisingEvents = true;
            }
            SaveFileWatcherLogTimer.Elapsed += (obj, e) => 
            {
                if (FileWatcherLog.Any())
                    SaveFileWatcherLog();
            };
            SaveFileWatcherLogTimer.AutoReset= true;
            SaveFileWatcherLogTimer.Start();
        }
        private static void AssetLibraryCacherStart()
        {
            CacheAssetLibraryTimerHandler_async();
            AssetLibraryCacheTimer.Elapsed += (obj, e) => { CacheAssetLibraryTimerHandler(); };
            AssetLibraryCacheTimer.AutoReset= true;
            AssetLibraryCacheTimer.Start();
        }
        public static async void CacheAssetLibrary_async()
        {
            await Task.Run(() =>
            {
                CacheAssetLibrary();
            });
        }
        private static void CacheAssetLibrary()
        {
            GetAssetLibraryRootDirs();
            foreach (var dir in AssetLibraryRootDirs)
            {
                DirCache dirCache = new DirCache(dir.GetFullPath());
                Console.WriteLine("正在对资产库根目录进行缓存扫描。");
                DateTime a = DateTime.Now;
                dirCache.CacheAll();
                Console.WriteLine("缓存扫描完成，正在保存缓存数据。");
                string cachefile = Path.Combine(AssetLibraryCacheFolder, dir.ID.ToString() + ".dc");
                dirCache.SerializeToFile(cachefile, out _);
                DateTime b = DateTime.Now;
                Console.WriteLine("已更新资产库缓存文件：" + cachefile);
                Console.WriteLine("本次重建资产库缓存共用时：" + (a-b).ToString(@"mm\分ss\秒"));
            }
            FileInfo file = new FileInfo(AssetDirCacheTimerLogFile);
            try
            {
                file.Write(DateTime.Now.Ticks.ToString());
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }
        private static bool ShouldCacheDir(out TimeSpan difference)
        {
            bool shouldRun = false;
            var now = DateTime.Now;
            if (!File.Exists(AssetDirCacheTimerLogFile)) { difference = new TimeSpan(0);return true; }
            string text = File.ReadAllText(AssetDirCacheTimerLogFile);
            bool parsed = long.TryParse(text, out long tic);
            DateTime logedTime = new DateTime(tic);
            TimeSpan timespan = now - logedTime;
            TimeSpan interval = TimeSpan.FromMinutes(180);
            var intervalParsed = double.TryParse(App.AssetLibraryAutoCacheInterval, out double intervalDouble);
            if (intervalParsed)
                interval = TimeSpan.FromMinutes(intervalDouble);

            difference = timespan - interval;

            if (File.Exists(AssetDirCacheTimerLogFile))
            {
                if (parsed)
                {
                    if (difference > new TimeSpan(0))
                        shouldRun = true;
                }
                else
                    shouldRun = true;
            }
            else
                shouldRun = true;

            return shouldRun;
        }
        private static async void CacheAssetLibraryTimerHandler_async()
        {
            await Task.Run(() =>
            {
                CacheAssetLibraryTimerHandler();
            });
        }
        private static void CacheAssetLibraryTimerHandler()
        {
            bool shouldRun = ShouldCacheDir(out TimeSpan difference);

            if (!shouldRun)
            {
                Console.WriteLine($"缓存计时器运行中，当前不必执行缓存扫描，距下次扫描还有{difference.ToString(@"d\天hh\时mm\分ss\秒")}。");
                return; 
            }
            CacheAssetLibrary();
        }
    }

    public partial class App : Application
    {
        public static string DatabaseBackUpLocalDir { get => Path.Combine(AssetLibraryBackEndDocumentDir, "DatabaseBackUp"); }
        public static string DatabaseBackupServerFolder { get; set; }
        public static string DatabaseAutoBackupInterval { get; set; } = "1439";
        public static string DatabaseBackupTimerLogFile { get => Path.Combine(AssetLibraryBackEndDocumentDir, "DatabaseBackupTimerLog.dat"); }
        private static System.Timers.Timer DatabaseBackupTimer { get; } = new System.Timers.Timer(1000 * 60 * 45);

        public static void DatabaseBackup()
        {
            DateTime a = DateTime.Now;
            Console.WriteLine("开始备份数据库。");

            var outputFile = IO_Extensons.GetNextSequenceFile(DatabaseBackUpLocalDir, "DataBaseBackUp", ".sql", maxIndex: 66);
            TableReadInfo reader = DataBase.DBReader(@"show variables like '%basedir%'", DataBase.GetMySqlConnection(typeof(Asset)));
            var obj = reader[0, "Value"];
            if (obj is null)
            {
                Console.WriteLine("未能在数据库中获取到MySql安装目录，备份失败。");
                return;
            }
            if (string.IsNullOrEmpty(outputFile))
            {
                Console.WriteLine("未能分析到数据库备份输出文件路径，备份失败。");
                return;
            }
            string program = Path.Combine(obj.ToString(), "bin", "mysqldump.exe");

            var commandStr = $"\"{program}\" -h{LoginInfo.DatabaseIP} -u{LoginInfo.LoginName} -p{LoginInfo.Password} -R --databases iceprocess > \"{outputFile}\"";
            commandStr.CMD(waitForExit: true, showCommand: false);

            DateTime b = DateTime.Now;

            FileInfo file = new FileInfo(DatabaseBackupTimerLogFile);
            try
            {
                file.Write(DateTime.Now.Ticks.ToString());
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }

            Console.WriteLine("数据库备份完成，本次备份共用时：" + (a - b).ToString(@"mm\分ss\秒"));
 
            if (string.IsNullOrEmpty(DatabaseBackupServerFolder))
            {
                Console.WriteLine("未能获取到数据库备份位于服务器的存储目录，请检查PublicSettings表中的DatabaseBackupFolder项。");
                return;
            }
            try
            {
                var serverFile = Path.Combine(DatabaseBackupServerFolder,Path.GetFileName(outputFile));
                File.Copy(outputFile, serverFile, true);
            }catch(Exception ex) { Console.WriteLine(ex); }


        }
        public static async void DatabaseBackup_async()
        {
            await Task.Run(DatabaseBackup);
        }
        private static bool ShouldBackupDatabase(out TimeSpan difference)
        {
            bool shouldRun = false;
            var now = DateTime.Now;
            if (!File.Exists(DatabaseBackupTimerLogFile)) { difference = new TimeSpan(0); return true; }
            string text = File.ReadAllText(DatabaseBackupTimerLogFile);
            bool parsed = long.TryParse(text, out long tic);
            DateTime logedTime = new DateTime(tic);
            TimeSpan timespan = now - logedTime;
            TimeSpan interval = TimeSpan.FromMinutes(180);
            var intervalParsed = double.TryParse(App.DatabaseAutoBackupInterval, out double intervalDouble);
            if (intervalParsed)
                interval = TimeSpan.FromMinutes(intervalDouble);

            difference = timespan - interval;

            if (File.Exists(DatabaseBackupTimerLogFile))
            {
                if (parsed)
                {
                    if (difference > new TimeSpan(0))
                        shouldRun = true;
                }
                else
                    shouldRun = true;
            }
            else
                shouldRun = true;

            return shouldRun;
        }
        private static void BackupDatabaseTimerHandler()
        {
            bool shouldRun = ShouldBackupDatabase(out TimeSpan difference);

            if (!shouldRun)
            {
                Console.WriteLine($"数据库备份计时器运行中，距下次备份还有{difference.ToString(@"d\天hh\时mm\分ss\秒")}。");
                return;
            }
            DatabaseBackup();
        }
        private static async void DatabaseBackupTimerHandler_async()
        {
            await Task.Run(() =>
            {
                BackupDatabaseTimerHandler();
            });
        }
        private static void DatabaseBackupTimerStart()
        {
            DatabaseBackupTimerHandler_async();
            DatabaseBackupTimer.Elapsed += (obj, e) => { BackupDatabaseTimerHandler(); };
            DatabaseBackupTimer.AutoReset = true;
            DatabaseBackupTimer.Start();
        }

    }


    [Serializable]
    public class LoginInfo
    {
        public int SelectedLocalHostIPIndex { get; set; }
        public string LocalHostPort { get; set; }
        public string DatabaseIP { get; set; }
        public string DatabasePort { get; set; }
        public string DatabaseName { get; set; }
        public string LoginName { get; set; }
        public string Password { get; set; }
    }

}