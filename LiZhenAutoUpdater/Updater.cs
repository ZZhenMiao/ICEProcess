using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;

namespace LiZhenAutoUpdater
{
    /// <summary>
    /// 自动升级升级助理
    /// 创建这个类需要产品名称参数，默认可执行文件为{产品名称}.exe
    /// 默认获取升级路径的方法为弹出控制台让用户输入。
    /// </summary>
    public class Updater
    {
        const string regMainPath = @"Software\JaxonLee";
        string programFileName = null;

        static string ThisFolder { get => AppDomain.CurrentDomain.BaseDirectory; }

        string regProductionPath { get => regMainPath + @"\" + ProductionName; } 
        public string ProductionName { get; } = "common";
        public string ProgramFileName { get => programFileName is null? ProductionName+".exe":ProgramFileName; set => programFileName = value; }
        public string UpdatePath { get; set; } = string.Empty;

        public Action<Exception> UpdateFailedAction { get; set; } = delegate { };
        public Func<string> InputUpdatePathFunction { get; set; }
        public Func<bool> CheckVersonFunction { get; set; }
        public Updater(string productionName)
        {
            this.ProductionName = productionName;
            this.InputUpdatePathFunction = () =>
            {
                Console.WriteLine($"请输入应用程序{ProgramFileName}的更新路径：");
                var input = Console.ReadLine();
                var path = input?.Trim();
                return path ?? string.Empty;

                //if (Directory.Exists(path))
                //{ WriteReg(path);  return true; }
                //else { return false; }
            };
            this.CheckVersonFunction = () => 
            {
                try
                {
                    var thisFile = Path.Combine(ThisFolder, ProgramFileName);
                    var targFile = Path.Combine(GetUpdatePathFromReg(), ProgramFileName);
                    if (!File.Exists(targFile) || !File.Exists(thisFile))
                        return false;
                    var lt1 = File.GetLastWriteTime(thisFile);
                    var lt2 = File.GetLastWriteTime(targFile);

                    return Math.Abs((lt1 - lt2).TotalSeconds) > 7;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    UpdateFailedAction?.Invoke(ex);
                    return false;
                }
            };
        }
        public bool TryUpdate(out Exception exception, bool askForUpdatePath = false)
        {
            exception = null;
            try
            {
                if (!CheckUpdatePath())
                {
                    if (!askForUpdatePath)
                        return false;
                    if (!AskForUpdatePath())
                        return false;
                }
                if(!CheckVerson())
                    return false;

                var updatePath = GetUpdatePathFromReg();
                var thisFileName = Process.GetCurrentProcess().MainModule?.FileName;

                foreach (var process in Process.GetProcessesByName(ProgramFileName))
                {
                    process.Kill();
                }

                foreach (string dirPath in Directory.GetDirectories(updatePath, "*", SearchOption.AllDirectories))
                {
                    Console.WriteLine("尝试创建文件夹：" + dirPath.Replace(updatePath, ThisFolder));
                    Directory.CreateDirectory(dirPath.Replace(updatePath, ThisFolder));
                }

                foreach (string file in Directory.GetFiles(updatePath, "*.*", SearchOption.AllDirectories))
                {
                    if (Path.GetFileName(file) != Path.GetFileName(thisFileName))
                    {
                        Console.WriteLine("复制文件：" + file + " => " + file.Replace(updatePath, ThisFolder));
                        File.Copy(file, file.Replace(updatePath, ThisFolder), true);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                UpdateFailedAction?.Invoke(ex);
                exception = ex;
                return false;
            }
        }
        public void RunProgram()
        {
            Process.Start(new ProcessStartInfo(ProgramFileName) { UseShellExecute = true });
        }
        bool WriteReg()
        {
            try
            {
                var reg = new Regedit_CurrentUser();
                if (!reg.Exists(regMainPath))
                    reg.CreateItem(regMainPath);
                if (!reg.Exists(regProductionPath))
                    reg.CreateItem(regProductionPath);
                reg.CreateItem(regProductionPath);
                reg.SetKeyValue(regProductionPath, "UpdatePath", UpdatePath);
                return true;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
        string GetUpdatePathFromReg()
        {
            try
            {
                Regedit_CurrentUser reg = new Regedit_CurrentUser();
                var value = reg.GetValue(regProductionPath, "UpdatePath");
                return value;
            }
            catch (Exception ex)
            {
                UpdateFailedAction?.Invoke(ex);
                return null ?? string.Empty;
            }
        }
        public bool CheckUpdatePath()
        {
            var updatePath = GetUpdatePathFromReg();
            if(string.IsNullOrWhiteSpace(updatePath))
                return false;
            if(!Directory.Exists(updatePath))
                return false;
            return true;
        }
        bool CheckVerson() => CheckVersonFunction?.Invoke() ?? false;
        public bool AskForUpdatePath()
        {
            var updatePath = InputUpdatePathFunction?.Invoke()??string.Empty;
            if (string.IsNullOrWhiteSpace(updatePath))
                return false;
            if (!Directory.Exists(updatePath))
                return false;
            UpdatePath = updatePath;
            return WriteReg();
        }
    }

    public abstract class Regedit
    {
        protected RegistryHive RegistryHive { get; set; }
        protected RegistryKey RegistryKey { get; set; }
        public RegistryKey CreateItem(string path)
        {
            var re = RegistryKey.CreateSubKey(path);
            RegistryKey.Close();
            return re;
            //在HKEY_LOCAL_MACHINE\SOFTWARE下新建名为test的注册表项。如果已经存在则不影响！
        }
        public void SetKeyValue(string path, string keyName, object value, RegistryValueKind registryValueKind = RegistryValueKind.String)
        {
            RegistryKey software = RegistryKey.OpenSubKey(path, true); //该项必须已存在
            try
            {
                software.SetValue(keyName, value, registryValueKind);
                RegistryKey.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        public bool Exists(string keyPath)
        {
            using (RegistryKey key = RegistryKey.OpenBaseKey(RegistryHive, RegistryView.Default).OpenSubKey(keyPath))
            {
                return key != null;
            }
        }
        public string GetValue(string path, string name)
        {
            RegistryKey myreg = RegistryKey.OpenSubKey(path);
            try
            {
                string info = myreg?.GetValue(name)?.ToString();
                myreg?.Close();
                return info;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }
    }
    public class Regedit_CurrentUser : Regedit
    {
        public Regedit_CurrentUser() { this.RegistryKey = Registry.CurrentUser; RegistryHive = RegistryHive.CurrentUser; }
    }
}