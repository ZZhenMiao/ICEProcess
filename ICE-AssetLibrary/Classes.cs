using ICE_Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace ICE_AssetLibrary
{
    /// <summary>
    /// 查询方案
    /// </summary>
    [Serializable]
    public class QueryScheme
    {
        public string Name { get; set; }
        //public bool IsSelected { get; set; }
        public ObservableCollection<IQueryCriteria> QueryCriterias { get; } = new ObservableCollection<IQueryCriteria>();
        public AssetTypeQueryScheme AssetTypeQueryScheme { get; set; } = AssetTypeQueryScheme.Custom;
        public ObservableCollection<AssetType> AssetTypes { get; } = new ObservableCollection<AssetType>();
        public ProjectQueryScheme ProjectQueryScheme { get; set; } = ProjectQueryScheme.Custom;
        public ObservableCollection<Project> Projects { get; } = new ObservableCollection<Project>();
        public SortScheme SortScheme { get; set; }
        public bool Sort { get; set; } = true;
        //public ObservableCollection<AssetLabel> SelectedLabels { get; } = new ObservableCollection<AssetLabel>();
        public Dictionary<string, DateTime> UsageLog { get; } = new Dictionary<string, DateTime>();
        public bool LoadAllLabelGroup { get; set; } = true;
        public string DownLoadPath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        public string FilePath { get => Path.Combine(App.QuerySchemesDir, Name + ".qsc"); }
        public bool? SearchFolder { get; set; } = true;

        public Exception Save()
        {
            if (!Directory.Exists(App.QuerySchemesDir))
                Directory.CreateDirectory(App.QuerySchemesDir);
            LiZhenStandard.Extensions.Serialization_Extensons.SerializeToFile(this, FilePath, out Exception ex);
            return ex;
        }
        public Exception Delete()
        {
            if (!Directory.Exists(App.QuerySchemesDir))
                Directory.CreateDirectory(App.QuerySchemesDir);
            if (!File.Exists(FilePath))
                return null;
            try
            {
                File.Delete(FilePath);
                return null;
            }
            catch (Exception e) { return e; }
        }
        public static QueryScheme[] LoadAll()
        {
            List<QueryScheme> querySchemes = new List<QueryScheme>();
            if (!Directory.Exists(App.QuerySchemesDir))
                Directory.CreateDirectory(App.QuerySchemesDir);
            string[] allfiles = Directory.GetFiles(App.QuerySchemesDir, "*.qsc");
            foreach (var file in allfiles)
            {
                var qsc = LiZhenStandard.Extensions.Serialization_Extensons.DeserializeFromFile<QueryScheme>(file, out Exception ex);
                if (ex is null)
                    querySchemes.Add(qsc);
            }
            return querySchemes.ToArray();
        }
    }

    [Serializable]
    public enum AssetTypeQueryScheme { All, MyPosition, Custom }
    [Serializable]
    public enum ProjectQueryScheme { All, Universal, AllAndUniversal, Custom }

    [Serializable]
    public class AssetUsageLogs : List<AssetUsageLog>
    {
        public void Statistics(IEnumerable<AssetFileInfo> fileInfos)
        {
            foreach (var info in fileInfos)
            {
                info.Usage = this.Where(a => a.FilePath.Equals(info.FullName)).Count();
            }
        }
        public void Recording(AssetFileInfo fileInfo, AssetUsage usage)
        {
            fileInfo.Usage += 1;
            this.Insert(0,
                new AssetUsageLog()
                {
                    ID_Asset = Convert.ToUInt64(fileInfo.ID_Asset),
                    FilePath = fileInfo.FullName,
                    Time = DateTime.Now,
                    AssetUsage = usage
                });
        }
        public void LoadFromFile()
        {
            if (!File.Exists(App.AssetUsageLogFile))
                return;
            var re = LiZhenStandard.Extensions.Serialization_Extensons.DeserializeFromFile<AssetUsageLogs>(App.AssetUsageLogFile, out _);
            this.Clear();
            this.AddRange(re);
        }
        public void SaveToFile()
        {
            if (Directory.Exists(App.AssetLibraryDocumentDir))
                LiZhenStandard.Extensions.IO_Extensons.CreateDirectory(App.AssetLibraryDocumentDir);
            LiZhenStandard.Extensions.Serialization_Extensons.SerializeToFile(this, App.AssetUsageLogFile, out _);
        }
    }
    [Serializable]
    public struct AssetUsageLog
    {
        public ulong ID_Asset { get; set; }
        public string FilePath { get; set; }
        public DateTime Time { get; set; }
        public AssetUsage AssetUsage { get; set; }
    }
    [Serializable]
    public enum AssetUsage { Download, Open, InExplorer }

    /// <summary>
    /// 查询条件
    /// </summary>
    public interface IQueryCriteria
    {
        abstract string Name { get; set; }
        bool Enabled { get; set; }
    }
    [Serializable]
    public class QueryCriteria_Label : AssetLabel, IQueryCriteria
    {
        public QueryCriteria_Label(AssetLabel label)
        {
            this.ID_AssetLabelGroup = label.ID_AssetLabelGroup;
            this.Name = label.Name;
            this.ID = label.ID;
            this.Illustration = label.Illustration;
        }
        public bool Enabled { get; set; }
    }
    [Serializable]
    public class QueryCriteria_Code : IQueryCriteria
    {
        private string name;
        public string Name { get => string.IsNullOrEmpty(name) ? Code : name; set => name = value; }
        public bool Enabled { get; set; }
        public string Code { get; set; }
    }
    [Serializable]
    public class QueryCriteria_Keyword : IQueryCriteria
    {
        private string name;
        public string Name { get => string.IsNullOrEmpty(name) ? Keyword : name; set => name = value; }
        public bool Enabled { get; set; }
        public string Keyword { get; set; }
    }
}
