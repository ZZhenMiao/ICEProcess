using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;

namespace LiZhenStandard
{
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
            }catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }
    }
    public class Regedit_LocalMachine : Regedit
    {
        public Regedit_LocalMachine() { this.RegistryKey = Registry.LocalMachine; RegistryHive = RegistryHive.LocalMachine; }
    }
    public class Regedit_ClassesRoot : Regedit
    {
        public Regedit_ClassesRoot() { this.RegistryKey = Registry.ClassesRoot; RegistryHive = RegistryHive.ClassesRoot; }
    }
    public class Regedit_Users : Regedit
    {
        public Regedit_Users() { this.RegistryKey = Registry.Users; RegistryHive = RegistryHive.Users; }
    }
    public class Regedit_CurrentUser : Regedit
    {
        public Regedit_CurrentUser() { this.RegistryKey = Registry.CurrentUser; RegistryHive = RegistryHive.CurrentUser; }
    }
}
