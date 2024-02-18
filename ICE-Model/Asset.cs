using LiZhenMySQL;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using LiZhenStandard.Extensions;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using Org.BouncyCastle.Asn1;
using System.Web;

namespace ICE_Model
{
    [Serializable]
    public class UniversualExtension
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Illustration { get; set; }
        public string[] Args { get; set; }
    }

    [SqlObject]
    [Serializable]
    public class Nas : DbNamedObject, IDbObject
    {
        [SqlProperty]
        public string IP { get; set; }
    }

    [SqlObject]
    [Serializable]
    public class Production : DbNamedObject, IDbObject
    {
        [SqlProperty]
        public string Code { get; set; }
    }

    [SqlObject]
    [Serializable]
    public class PublicSetting : IDbObject
    {
        [SqlProperty]
        public object ID { get; set; }
        [SqlProperty]
        public string Name { get; set; }
        [SqlProperty]
        public string Velue { get; set; }

        public static object GetSettingsValue(object[] settings,string name)
        {
            return GetSettingsValue((PublicSetting[])settings,name);
        }
        public static object GetSettingsValue(PublicSetting[] settings,string name)
        {
            if (settings.Count() < 1)
                return null;
            return settings.Where(a=>a.Name == name).FirstOrDefault()?.Velue;
        }
    }
    [Serializable]
    public class AssetFileMd5Info
    {
        public bool Skip { get; set; }
        public string SourceAssetDir { get; set; }
        public string SourcePath { get; set; }
        public string Name { get; set; }
        public string Dir { get; set; }
        public string MD5 { get; set; }
        public double Size { get; set; }
    }


    /// <summary>
    /// 资产类型
    /// </summary>
    [SqlObject]
    [Serializable]
    public class AssetType : Tree<AssetType>, IDbObject, INamedObject
    {
        public string Code { get; set; }
        public ObservableCollection<AssetLabelGroup> AssetLabelGroups { get; } = new ObservableCollection<AssetLabelGroup>();
    }

    /// <summary>
    /// 资产
    /// </summary>
    [SqlObject]
    [Serializable]
    public class Asset : DbNamedObject, IDbObject
    {
        [SqlProperty]
        public bool IsSequence { get; set; }
        [SqlProperty]
        public object ID_AssetDirectory { get; set; }
        [SqlProperty]
        public string Path { get; set; }

        public string FullPath { get; set; }
        public int Usage { get; set; }
        [SqlProperty]
        public DateTime ArchiveTime { get; set; }
        [SqlProperty]
        public object ID_Uploader { get; set; }

        public string UploaderName { get; set; }
        public bool IsFolder
        {
            get
            {
                return string.IsNullOrWhiteSpace(FullPath) ? false : string.IsNullOrEmpty(System.IO.Path.GetExtension(FullPath));

                //if (!File.Exists(FullPath) && !Directory.Exists(FullPath))
                //    return false;
                //var att = File.GetAttributes(FullPath);
                //return (att & FileAttributes.Directory) == FileAttributes.Directory;
            }
        }
    }

    /// <summary>
    /// 自动归档方案
    /// </summary>
    [SqlObject]
    [Serializable]
    public class AutoArchiveScheme : IDbObject
    {
        [SqlProperty(IsPrimaryKey = true)]
        public object ID { get; set; }
        [SqlProperty]
        public object ID_AssetDirectory { get; set; }
    }

    /// <summary>
    /// 资产目录
    /// </summary>
    [SqlObject]
    [Serializable]
    public class AssetDirectory : Tree<AssetDirectory>, IDbObject
    {
        [SqlProperty]
        public object ID_Nas { get; set; }
        public Nas Nas { get; set; }

        public string GetFullPath()
        {
            if (Parent is null)
                return Path.Combine(Path_Extensons.SharedVolumeSeparator_Win(this.Nas.IP),this.Name);
            else
                return Path.Combine(Parent.GetFullPath(), this.Name);
        }
    }

    /// <summary>
    /// 资产标签组
    /// </summary>
    [SqlObject]
    [Serializable]
    public class AssetLabelGroup : DbNamedObject, IDbObject
    {
        [SqlProperty]
        public bool AllProject { get; set; }
        [SqlProperty]
        public bool AllAssetType { get; set; }
        public ObservableCollection<AssetLabel> AssetLabels { get; } = new ObservableCollection<AssetLabel>();
    }

    /// <summary>
    /// 资产标签
    /// </summary>
    [SqlObject]
    [Serializable]
    public class AssetLabel : DbNamedObject, IDbObject
    {
        [SqlProperty]
        public object ID_AssetLabelGroup { get; set; }
    }

    public class DirCaches : ConcurrentBag<DirCache>
    {
        public async Task<string[]> GetFileSystemEntriesFromBigMap(string searchPattern = "*")
        {
            List<string> re = new List<string>();
            foreach (var cache in this)
            {
                 re.AddRange(await cache.GetFileSystemEntriesFromBigMap(searchPattern));
            }
            return re.ToArray();
        }
        public string[] GetFileSystemEntries(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            List<string> re = new List<string>();
            foreach (var cache in this)
            {
                re.AddRange(cache.GetFileSystemEntries(path,searchPattern,searchOption));
            }
            return re.ToArray();
        }
        public string[] GetFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            List<string> re = new List<string>();
            foreach (var cache in this)
            {
                var files = cache.GetFiles(path, searchPattern, searchOption);
                if(files is not null)
                re.AddRange(files);
            }
            return re.ToArray();
        }
        public string[] GetDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            List<string> re = new List<string>();
            foreach (var cache in this)
            {
                var dirs = cache.GetDirectories(path, searchPattern, searchOption);
                if (dirs is not null)
                    re.AddRange(dirs);
            }
            return re.ToArray();
        }
    }

    [Serializable]
    public class FileSysCache
    {
        protected string fullName;
        public DirCache Parent { get; set; }
        public string Name { get; set; }
        public bool IsDirectory { get => this is DirCache; }
        public string FullName { get => fullName ?? GetFullName(); }
        protected string GetFullName()
        {
            fullName = Parent is null ? Name : Path.Combine(Parent.GetFullName(), Name);
            return fullName;
        }
        public FileSysCache(string dirName, DirCache parent) { this.Parent = parent; this.Name = dirName; }
        public override string ToString()
        {
            return GetFullName();
        }
    }

    [Serializable]
    public class DirCache: FileSysCache
    {
        public List<FileCache> Files { get; } = new List<FileCache>();
        public List<DirCache> Dirs { get; } = new List<DirCache>();

        public string[] BigMap { get; set; }

        public DirCache(string dirFullPath) : base(dirFullPath, null) { Name = dirFullPath; }
        public DirCache(string dirName, DirCache parent) : base(dirName, parent) { Parent = parent; Name = dirName; }

        public void AddFile(string fileName) { Files.Add(new FileCache(Path.GetFileName(fileName), this)); }
        public void AddFiles(IEnumerable<string> fileNames) { Files.AddRange(from fileName in fileNames select new FileCache(Path.GetFileName(fileName), this)); }
        public void AddDir(string dirName) { Dirs.Add(new DirCache(Path.GetFileName(dirName), this)); }
        public void AddDirs(IEnumerable<string> dirNams) { Dirs.AddRange(from dirName in dirNams select new DirCache(Path.GetFileName(dirName), this)); }

        public void CacheAll()
        {
            CacheSubFiles();
            CacheSubDirs();
        }

        public async Task<string[]> GetFileSystemEntriesFromBigMap(string searchPattern = ".*")
        {
            return await System.Threading.Tasks.Task.Run(() =>
            {
                if (BigMap is null)
                    BigMap = GetFileSystemEntries(this.FullName, searchOption: SearchOption.AllDirectories);

                var re = BigMap.Where(a =>
                {
                    var fileName = Path.GetFileName(a);
                    return fileName.IsMatch(searchPattern);
                });
                return re.ToArray();
            });
        }

        public void CacheSubDirs()
        {
            AddDirs(Directory.GetDirectories(GetFullName()));
            foreach (DirCache dirCache in Dirs)
            {
                dirCache.CacheSubFiles();
                dirCache.CacheSubDirs();
            }
        }
        public void CacheSubFiles()
        {
            AddFiles(Directory.GetFiles(GetFullName()));
        }

        private DirCache FindDirCache(string path)
        {
            if (!path.Contains(FullName))
                return null;

            var subPath = path.Replace(FullName, string.Empty);
            var names = subPath.Split(Path.DirectorySeparatorChar);
            DirCache dirCache= this;
            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i];
                if(string.IsNullOrEmpty(name)) continue;
                if (dirCache is null)
                    return null;
                dirCache = dirCache.Dirs.Find(x => x.Name == name);
            }
            return dirCache;
        }
        public string[] GetFileSystemEntries(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            var dirs = GetDirectories(path, searchPattern, searchOption);
            var files = GetFiles(path, searchPattern, searchOption);
            if (dirs is null)
                dirs = new string[0];
            if (files is null)
                files = new string[0];
            var all = new string[dirs.Length + files.Length];
            Array.Copy(dirs, 0, all, 0, dirs.Length);
            Array.Copy(files, 0, all, dirs.Length, files.Length);
            return all;
        }
        public string[] GetFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            var dirCache = FindDirCache(path);
            if (dirCache is null) 
                return null;
            var files = dirCache.Files;
            var dirs = dirCache.Dirs;
            ConcurrentBag<string> result = new ConcurrentBag<string>();
        
            int count = 0;
            Parallel.ForEach(files, file =>
            {
                Interlocked.Increment(ref count);
                var reg_searchPattern = String_Extensons.GetWildcardRegexString(searchPattern);
                var matched = file?.Name?.IsMatch(reg_searchPattern, out _);
                if (matched == true)
                    result.Add(file.FullName);
            });
            Interlocked.Decrement(ref count);
           
            if (searchOption == SearchOption.TopDirectoryOnly)
                return result.ToArray();
            else
            {
                count = 0;
                Parallel.ForEach(dirs, dir => 
                {
                    Interlocked.Increment(ref count);
                    var subFiles = dir.GetFiles(dir.FullName, searchPattern, searchOption);
                    result.AddRange(subFiles);
                });
                Interlocked.Decrement(ref count);
                return result.ToArray();
            }
        }
        public string[] GetDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            var dirCache = FindDirCache(path);
            if (dirCache is null) 
                return null;
            var dirs = dirCache.Dirs;
            ConcurrentBag<string> result = new ConcurrentBag<string>();

            int count = 0;
            Parallel.ForEach(dirs, dir => 
            {
                Interlocked.Increment(ref count);
                var reg_searchPattern = String_Extensons.GetWildcardRegexString(searchPattern);
                var matched = dir.Name.IsMatch(reg_searchPattern, out _);
                if (matched)
                    result.Add(dir.FullName);
            });
            Interlocked.Decrement(ref count);
            if (searchOption == SearchOption.TopDirectoryOnly)
                return result.ToArray();
            else
            {
                count = 0;
                Parallel.ForEach(dirs, dir => 
                {
                    Interlocked.Increment(ref count);
                    var subFiles = dir.GetDirectories(dir.FullName, searchPattern, searchOption);
                    result.AddRange(subFiles);
                });
                Interlocked.Decrement(ref count);
                return result.ToArray();
            }

            //foreach (var dir in dirs)
            //{
            //    var reg_searchPattern = String_Extensons.GetWildcardRegexString(searchPattern);
            //    var matched = dir.Name.IsMatch(reg_searchPattern, out _);
            //    if (matched)
            //        result.Add(dir.FullName);
            //}
            //if (searchOption == SearchOption.TopDirectoryOnly)
            //    return result.ToArray();
            //else
            //{
            //    foreach (var dir in dirs)
            //    {
            //        var subFiles = dir.GetDirectories(dir.FullName, searchPattern, searchOption);
            //        result.AddRange(subFiles);
            //    }
            //    return result.ToArray();
            //}
        }
    }

    [Serializable]
    public class FileCache : FileSysCache
    {
        public FileCache(string dirName, DirCache parent):base(dirName,parent) { this.Parent = parent; this.Name = dirName; }
    }
}
