using ICE_AssetLibrary.Properties;
using ICE_Model;
using LiZhenStandard.Extensions;
using LiZhenStandard.Sockets;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Windows;
using LiZhenStandard.CSharpCompiler;
using System.Collections.Generic;
using Microsoft.Win32;
using System.Timers;
using LiZhenWPF;
using System.Windows.Threading;

namespace ICE_AssetLibrary
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static Person Me { get; set; }
        public static Socket Socket { get; set; }
        public static string AppDir { get => AppDomain.CurrentDomain.BaseDirectory; }
        public static string MyDocumentDir { get => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); }
        public static string AssetLibraryDocumentDir { get => Path.Combine(MyDocumentDir, "AssetLibrary"); }
        public static string QuerySchemesDir { get => Path.Combine(AssetLibraryDocumentDir, Me.Name, "QuerySchemes"); }
        public static string OptionsDir { get => Path.Combine(AssetLibraryDocumentDir, "Options"); }
        public static string QueryResultsDir { get => Path.Combine(AssetLibraryDocumentDir, Me.Name, "QueryResults"); }
        public static string TemporaryFileDir { get => Path.Combine(AssetLibraryDocumentDir, Me.Name, "TemporaryFiles"); }
        public static string AssetUsageLogFile { get => Path.Combine(AssetLibraryDocumentDir, "AssetUsageLog.dat"); }
        public static string OptionsFile { get => Path.Combine(OptionsDir, "MainOptions.dat"); }
        public static string LoginDataFile { get => Path.Combine(AssetLibraryDocumentDir, "Login.dat"); }
        public static string LocalCacheFolder { get => Path.Combine(AssetLibraryDocumentDir, "Cache"); }
        public static string MyHeadPortraitFile { get; }
        public static Settings Settings { get => Settings.Default; }
        public static string PasswordKey { get => "ice"; }
        public static OptionsInfo Options { get; set; }
        public static DirCaches Caches { get; } = new DirCaches();

        public static string AssetLibraryInstructionFile { get; set; }
        public static string AssetLibraryStandardFile { get; set; }
        public static string AssetLibraryChangeLogFile { get; set; }
        public static string AssetLibraryCacheFolder { get; set; }
        public static string[] ExcludeAssetExtensions { get; set; } = { ".db" };
        public static string[] ImageEx { get; set; } = new string[] { ".jpg", ".jpeg", ".png", ".bmp", ".tif", ".tiff", ".gif", ".wdp" };
        public static string[] SoundEx { get; set; } = new string[] { ".wav", ".mp3" };
        public static string[] VideoEx { get; set; } = new string[] { ".mp4", ".mov", ".avi", ".m4v" };
        public static string[] ModelEx { get; set; } = new string[] { ".fbx", ".abc", ".obj" };

        //public static void Initialize(string ip, string port)
        //{
        //    Socket = SocketFunction.ConnectTCPServer(string.Format("{0}:{1}", ip, port));
        //    Socket.SendBufferSize = 1024 * 32;
        //    Socket.ReceiveBufferSize = 1024 * 32;
        //}
        public static void LoadPublicSettings()
        {
            bool re = SocketFunction.SendInstruct(App.Socket, "LoadPublicSettings", null, out object[] results, out Exception e);
            if (re)
            {
                try
                {
                    AssetLibraryInstructionFile = (string)PublicSetting.GetSettingsValue(results, "AssetLibraryInstructionFile");
                    AssetLibraryStandardFile = (string)PublicSetting.GetSettingsValue(results, "AssetLibraryStandardFile");
                    AssetLibraryChangeLogFile = (string)PublicSetting.GetSettingsValue(results, "AssetLibraryChangeLogFile");
                    AssetLibraryCacheFolder = (string)PublicSetting.GetSettingsValue(results, "AssetLibraryCacheFolder");
                    ExcludeAssetExtensions = ((string)PublicSetting.GetSettingsValue(results, "ExcludeAssetExtensions")).Split("|");
                    ImageEx = ((string)PublicSetting.GetSettingsValue(results, "ImageEx")).Split("|");
                    SoundEx = ((string)PublicSetting.GetSettingsValue(results, "SoundEx")).Split("|");
                    VideoEx = ((string)PublicSetting.GetSettingsValue(results, "VideoEx")).Split("|");
                    ModelEx = ((string)PublicSetting.GetSettingsValue(results, "ModelEx")).Split("|");
                }
                catch { }
            }
        }
        public static void LoadOptions()
        {
            if (!File.Exists(OptionsFile))
            {
                App.Options = new OptionsInfo();
                return;
            }
            Options = Serialization_Extensons.DeserializeFromFile<OptionsInfo>(OptionsFile, out _);
        }
        public static void SaveOptions()
        {
            if (Options is null)
                App.Options = new OptionsInfo();
            if (!Directory.Exists(OptionsDir))
                IO_Extensons.CreateDirectory(OptionsDir);
            Options.SerializeToFile(OptionsFile, out _);
        }
        public static void RefreshCache()
        {
            Caches.Clear();
            if(LocalCacheFolder is null || AssetLibraryCacheFolder is null) { return; }
            if (!Directory.Exists(LocalCacheFolder))
                IO_Extensons.CreateDirectory(LocalCacheFolder);
            if (!Directory.Exists(AssetLibraryCacheFolder)) { Console.WriteLine("未找到服务端缓存目录。"); return; }
            var serverDir = new DirectoryInfo(AssetLibraryCacheFolder);
            var files = serverDir.GetFiles();
            foreach (var file in files)
            {
                var localFile = new FileInfo(Path.Combine(LocalCacheFolder, file.Name));
                if (!localFile.Exists)
                { TryCopyFile(file, localFile, "更新缓存文件：" + localFile.Name); continue; }
                var timeA = file.LastWriteTime;
                var timeB = localFile.LastWriteTime;
                if (timeA != timeB)
                    TryCopyFile(file, localFile, "更新缓存文件：" + localFile.Name);
                else
                    Console.WriteLine($"缓存文件：{localFile.Name} 已是最新，无需更新。");
                var cache = Serialization_Extensons.DeserializeFromFile<DirCache>(localFile.FullName,out Exception ex);
                if (ex is not null) { Console.WriteLine(ex.Message); }
                else 
                {
                    Caches.Add(cache);
                    Console.WriteLine("已加载缓存文件{0}。",localFile.Name);
                };
            }
        }
        private static void TryCopyFile(FileInfo source,FileInfo target,string message = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(message))
                    Console.WriteLine(message);
                source.CopyTo(target.FullName,true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        public static bool ConsoleState { get; private set; } = true;
        public static void ShowConsole(bool showOrHide)
        {
            if (showOrHide)
            {
                LiZhenStandard.ConsoleHelper.ShowConsole();
                ConsoleState = true;
            }
            else
            {
                LiZhenStandard.ConsoleHelper.HideConsole();
                ConsoleState = false;
            }
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            //AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;

            string ag = e.Args.FirstOrDefault();//<<===调用启动参数

            //string ag = "ip:192.168.1.90 pt:5210 u:lizhen pw:yhtqlz";//<<===测试用
            //Thread.Sleep(800);

            string ip = null;
            string port = null;
            string user = null;
            string password = null;
            if (!ag.IsNullOrEmpty())
            {
                ag.IsMatch(@"(?<=ip:)[^\s]+", out ip);
                ag.IsMatch(@"(?<=pt:)[^\s]+", out port);
                ag.IsMatch(@"(?<=u:)[^\s]+", out user);
                ag.IsMatch(@"(?<=pw:)[^\s]+", out password);
            }

            if (string.IsNullOrEmpty(ip) || string.IsNullOrEmpty(port) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password))
            {
                ShowLoginWindow();
            }

            //var MainWindow = new MainWindow();
            //MainWindow.Show();
            return;
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show("发生错误！\t" + e.Exception.Message);
            Application.Current.Shutdown();
        }

        public static void ShowLoginWindow(bool allowAutoLogin = true)
        {
            var login_win = new ICE_Common.Window_Login(allowAutoLogin);
            login_win.LoginMethod = (me) => 
            {
                App.Socket = login_win.Socket;
                App.LoadPublicSettings();
                App.LoadOptions();
                App.Me = me;
                MainWindow mw = new();
                mw.Show();
            };
            //login_win.InitializeMethod = App.Initialize;
            login_win.LoginDataFile = App.LoginDataFile;
            if (!login_win.IsClosed)
                login_win.Show();
        }
        public bool Login(string ip, string port, string user, string password)
        {
            Socket = SocketFunction.ConnectTCPServer(string.Format("{0}:{1}", ip, port));
            bool re = SocketFunction.SendInstruct(Socket, "CheckLogin", new object[] { user, password }, out object[] results, out Exception oex);
            object result = results?.FirstOrDefault();
            if (!re || typeof(Exception).IsAssignableFrom(result.GetType()))
            {
                Exception ex = oex ?? (Exception)result;
                MessageBox.Show(ex.Message, "登录错误");
                return false;
            }
            Me = (Person)result;
            return true;
        }
        public static Exception ProgramStart(string appName, string par = null)
        {
            try
            {
                ProcessStartInfo processStartInfo = new ProcessStartInfo(appName);
                processStartInfo.UseShellExecute = true;
                processStartInfo.Arguments = par;
                System.Diagnostics.Process.Start(processStartInfo);
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }


        protected override void OnStartup(StartupEventArgs e)
        {
            //CheckAdministrator();
            //CodeCompile(File.ReadAllText("C:\\Users\\admin\\source\\repos\\测试4-内嵌脚本编译器\\Resources\\Code.txt")); 
            base.OnStartup(e);
        }

        /// <summary>
        /// 检查是否是管理员身份
        /// </summary>
        private void CheckAdministrator()
        {
            var wi = WindowsIdentity.GetCurrent();
            var wp = new WindowsPrincipal(wi);

            bool runAsAdmin = wp.IsInRole(WindowsBuiltInRole.Administrator);

            if (!runAsAdmin)
            {
                // It is not possible to launch a ClickOnce app as administrator directly,
                // so instead we launch the app as administrator in a new process.
                var processInfo = new ProcessStartInfo();

                // The following properties run the new process as administrator
                processInfo.UseShellExecute = true;
                processInfo.Verb = "runas";
                processInfo.WorkingDirectory = Environment.CurrentDirectory;
                processInfo.FileName = AppDomain.CurrentDomain.FriendlyName;

                // Start the new process
                try
                {
                    System.Diagnostics.Process.Start(processInfo);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                // Shut down the current process
                Environment.Exit(0);
            }
        }
        /// <summary>
        /// 编译并运行一段C#脚本代码。
        /// </summary>
        /// <param name="code"></param>
        public static void CodeCompile(string code)
        {
            var compiler = new CSharpCompiler();
            compiler.ReferencedAssemblies.Add(Assembly.GetAssembly(typeof(Person)));
            compiler.Usings.Add("ICE_Model");
            var re = compiler.CompileFunciton(code);
            Console.WriteLine(re);
        }

    }

}
