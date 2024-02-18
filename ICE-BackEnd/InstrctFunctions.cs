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
    public partial class App : Application
    {
        public static object[] LoadAssetInfo(object[] objs)
        {
            ulong id_asset = Convert.ToUInt64(objs[0]);
            DBObjContainer<AssetLabel> labels = new DBObjContainer<AssetLabel>();
            DBObjContainer<AssetType> types = new DBObjContainer<AssetType>();
            DBObjContainer<Project> projects = new DBObjContainer<Project>();
            Asset asset = new Asset() { ID = id_asset };
            labels.LoadFromDB_Chain(asset);
            types.LoadFromDB_Chain(asset);
            projects.LoadFromDB_Chain(asset);

            return new object[] { types.ToArray(), projects.ToArray(), labels.ToArray() };
        }
        public static void SetAssetFullPath(Asset[] assets)
        {
            AssetDirectory[] dirList = DataBase.LoadFromDB_Where<AssetDirectory>();
            _ = IDBTree<AssetDirectory>.MakeTree(dirList);

            foreach (AssetDirectory dir in dirList)
            {
                if (dir.ID_Nas is not null)
                    dir.Nas = dir.ID_Nas.LoadFromDB_ThisID<Nas>();
            }
            foreach (Asset asset in assets)
            {
                AssetDirectory directory = dirList.FirstOrDefault(a => a.ID.Equals(asset.ID_AssetDirectory));
                var dirFullPath = directory?.GetFullPath();
                var subFolderPath = asset.Path;
                var filename = asset.Name;
                asset.FullPath = string.IsNullOrWhiteSpace(dirFullPath) ? Path.Combine(subFolderPath, filename) : Path.Combine(dirFullPath, subFolderPath, filename);
            }
        }
        public static void SetAssetPerson(Asset[] assets)
        {
            Person[] allPerson = DataBase.LoadFromDB_Where<Person>();
            foreach (Asset asset in assets)
            {
                asset.UploaderName = allPerson.FirstOrDefault(a => a.ID.Equals(asset.ID_Uploader))?.Name;
            }
        }
        public static void InsertAssetFileMD5(AssetFileMd5Info[] infos)
        {
            List<AssetFileMd5Info[]> infoArrs = new List<AssetFileMd5Info[]>();
            List<AssetFileMd5Info> list = new List<AssetFileMd5Info>();
            for (int i = 1; i <= infos.Length; i++)
            {
                list.Add(infos[i - 1]);
                if (i % 30 == 0)
                {
                    infoArrs.Add(list.ToArray());
                    list.Clear();
                }
            }
            if (list.Count > 0)
                infoArrs.Add(list.ToArray());
            foreach (AssetFileMd5Info[] infoArr in infoArrs)
            {
                _InsertAssetFileMD5(infoArr);
            }
        }
        private static void _InsertAssetFileMD5(AssetFileMd5Info[] infos)
        {
            string com = $"INSERT INTO `assetfilemd5`(`name`,`dir`,`md5`,`size`) VALUES ";
            int len = infos.Length;
            string valueStr = string.Empty;
            for (int i = 0; i < len; i++)
            {
                AssetFileMd5Info info = infos[i];
                string md5 = info.MD5;
                if (string.IsNullOrWhiteSpace(md5))
                    continue;
                string fileName = info.Name;
                string dir = info.Dir.Replace(@"\", @"\\");
                double size = info.Size;
                valueStr += $"(\"{fileName}\",\"{dir}\",\"{md5}\",{size})";
                if (i < len - 1)
                    valueStr += ",";
            }
            if (string.IsNullOrWhiteSpace(valueStr))
                return;
            com += valueStr + " ON DUPLICATE KEY UPDATE `md5`=`md5`,`size`=`size`";
            var con = DataBase.GetMySqlConnection(typeof(Asset));
            DataBase.CommandNonQuery(com, con);
            con.TryClose();
        }
        public static string[] CheckAssetFileMD5(string[] md5s)
        {
            List<string[]> md5sArrs = new List<string[]>();
            List<string> list = new List<string>();
            for (int i = 1; i <= md5s.Length; i++)
            {
                list.Add(md5s[i - 1]);
                if (i % 30 == 0)
                {
                    md5sArrs.Add(list.ToArray());
                    list.Clear();
                }
            }
            if (list.Count > 0)
                md5sArrs.Add(list.ToArray());
            List<string> re = new List<string>();
            foreach (string[] md5Arr in md5sArrs)
            {
                re.AddRange(_CheckAssetFileMD5(md5Arr));
            }
            return re.ToArray();
        }
        private static string[] _CheckAssetFileMD5(string[] md5s)
        {
            string com = $"SELECT * FROM assetfilemd5 WHERE `md5` IN ({md5s.AllToString(forDataBase: true)});";
            var con = DataBase.GetMySqlConnection(typeof(Asset));
            List<string> list = new List<string>();
            TableReadInfo readInfos = DataBase.DBReader(com, con);
            foreach (var info in readInfos)
            {
                string dir = info["dir"].ToString();
                string name = info["name"].ToString();
                string path = Path.Combine(dir, name);
                list.Add(path);
            }
            return list.ToArray();
        }
        public static void DeleteAssetFileMD5(string[] paths)
        {
            int len = paths.Length;
            var con = DataBase.GetMySqlConnection(typeof(Asset));
            for (int i = 0; i < len; i++)
            {
                string com = $"DELETE FROM `assetfilemd5` WHERE ";
                string path = paths[i];
                string fileName = Path.GetFileName(path);
                string dir = Path.GetDirectoryName(path);
                com += $"`name`=\"{fileName}\" AND dir=\"{dir}\"";
                DataBase.CommandNonQuery(com, con);
            }
            con.TryClose();
        }
        public static object[] IsThisAssetInAssetDirectory(object[] objs)
        {
            string inputPath = (string)objs[0];
            AssetDirectory[] dirs = DataBase.LoadFromDB_Where<AssetDirectory>();
            if (!dirs.Any())
                return new object[] { -1 };
            AssetDirectory[] roots = IDBTree<AssetDirectory>.MakeTree(dirs);
            foreach (AssetDirectory root in roots)
            {
                root.Nas = root.ID_Nas.LoadFromDB_ThisID<Nas>();
            }
            Func<string, object[]> func = null;
            func = new Func<string, object[]>((inputPath) =>
            {
                string parentFullPath = System.IO.Path.GetDirectoryName(inputPath);
                if (string.IsNullOrWhiteSpace(parentFullPath))
                    return new object[] { -1, null };
                string parentName = System.IO.Path.GetFileName(parentFullPath);
                AssetDirectory[] sameNameDirs = dirs.Where(a => a.Name == parentName).ToArray();
                foreach (AssetDirectory sameNameDir in sameNameDirs)
                {
                    string dirFullPath = sameNameDir.GetFullPath();
                    if (dirFullPath == parentFullPath)
                        return new object[] { sameNameDir.ID, dirFullPath };
                }
                return func.Invoke(parentFullPath);
            });
            return func.Invoke(inputPath);
        }
        public static bool InsertAss_Asset_AssetLabel(ulong[] assetIDs, ulong[] labelIDs, bool clearOld)
        {
            var con = DataBase.GetMySqlConnection(typeof(Asset));
            foreach (var assetID in assetIDs)
            {
                try
                {
                    if (clearOld)
                        DataBase.CommandNonQuery($"delete from ass_asset_assetlabel where id_asset = {assetID}", con);
                    foreach (var labelID in labelIDs)
                    {
                        DataBase.CommandNonQuery($"insert into `ass_asset_assetlabel`(`id_asset`,`id_assetlabel`) values ({assetID},{labelID})", con);
                    }
                }
                catch (Exception e) { Debug.WriteLine(e.Message); return false; }
            }
            con.TryClose();
            return true;
        }
        public static bool InsertAss_Asset_AssetType(ulong[] assetIDs, ulong[] typeIDs, bool clearOld)
        {
            var con = DataBase.GetMySqlConnection(typeof(Asset));
            foreach (var assetID in assetIDs)
            {
                try
                {
                    if (clearOld)
                        DataBase.CommandNonQuery($"delete from ass_assettype_asset where id_asset = {assetID}", con);
                    foreach (var typeID in typeIDs)
                    {
                        DataBase.CommandNonQuery($"insert into `ass_assettype_asset`(`id_asset`,`id_assettype`) values ({assetID},{typeID})", con);
                    }
                }
                catch (Exception e) { Debug.WriteLine(e.Message); return false; }
            }
            con.TryClose();
            return true;
        }
        public static bool InsertAss_Asset_Project(ulong[] assetIDs, ulong[] pjIDs, bool clearOld)
        {
            var con = DataBase.GetMySqlConnection(typeof(Asset));
            foreach (var assetID in assetIDs)
            {
                try
                {
                    if (clearOld)
                        DataBase.CommandNonQuery($"delete from ass_project_asset where id_asset = {assetID}", con);
                    foreach (var pjD in pjIDs)
                    {
                        DataBase.CommandNonQuery($"insert into `ass_project_asset`(`id_asset`,`id_project`) values ({assetID},{pjD})", con);
                    }
                }
                catch (Exception e) { Debug.WriteLine(e.Message); return false; }
            }
            con.TryClose();
            return true;
        }
        public static Asset[] FindSimilarityAssets(ulong assetID, out AssetType[] reTypes, out AssetLabel[] reLabels, out Project[] reProjects)
        {
            Asset asset = new Asset() { ID = assetID };
            DBObjContainer<AssetType> types = new DBObjContainer<AssetType>();
            DBObjContainer<AssetLabel> labels = new DBObjContainer<AssetLabel>();
            DBObjContainer<Project> projects = new DBObjContainer<Project>();
            types.LoadFromDB_Chain(asset);
            labels.LoadFromDB_Chain(asset);
            projects.LoadFromDB_Chain(asset);

            reTypes = types.ToArray();
            reLabels = labels.ToArray();
            reProjects = projects.ToArray();

            var reTypeIDs = from item in reTypes select Convert.ToUInt64(item.ID);
            var reLabelIDs = from item in reLabels select Convert.ToUInt64(item.ID);
            var reProjectIDs = from item in reProjects select Convert.ToUInt64(item.ID);

            return FindSimilarityAssets(reTypeIDs.ToArray(), reLabelIDs.ToArray(), reProjectIDs.ToArray(), Convert.ToUInt64(asset.ID_AssetDirectory));
        }
        public static Asset[] FindSimilarityAssets(ulong[] atypeIDs, ulong[] alabelIDs, ulong[] aprojectIDs, ulong diretoryID)
        {
            DBObjContainer<Asset> reList = new DBObjContainer<Asset>();

            if (!atypeIDs.Any())
                return reList.ToArray();

            string cotp = (atypeIDs.Count() < 1 ? 1 : atypeIDs.Count()).ToString();
            string colb = (alabelIDs.Count() < 1 ? 1 : alabelIDs.Count()).ToString();
            string copj = (aprojectIDs.Count() < 1 ? 1 : aprojectIDs.Count()).ToString();

            string typeIDs = "IS NULL";
            string labelIDs = "IS NULL";
            string projectIDs = "IS NULL";
            if (atypeIDs.Any())
                typeIDs = $"IN ({atypeIDs.AllToString()})";
            if (alabelIDs.Any())
                labelIDs = $"IN ({alabelIDs.AllToString()})";
            if (aprojectIDs.Any())
                projectIDs = $"IN ({aprojectIDs.AllToString()})";

            string comm = $"SELECT * FROM asset WHERE id IN (SELECT id FROM (SELECT id,colb,cotp,COUNT(id) AS copj FROM (SELECT id,colb,COUNT(id) AS cotp FROM (SELECT id,COUNT(id) AS colb FROM (asset LEFT JOIN ass_asset_assetlabel ON id = ass_asset_assetlabel.id_asset) WHERE id_assetlabel {labelIDs} GROUP BY id) AS t2 LEFT JOIN ass_assettype_asset on id = ass_assettype_asset.id_asset WHERE id_assettype {typeIDs} GROUP BY id) AS t3 LEFT JOIN ass_project_asset ON id = ass_project_asset.id_asset WHERE id_project {projectIDs} GROUP BY id) AS t4 WHERE colb = {colb} AND cotp = {cotp} AND copj = {copj}) AND id_assetdirectory = {diretoryID};";

            reList.LoadFromDB_Command(comm);
            Asset[] reArray = reList.ToArray();
            SetAssetFullPath(reArray.ToArray());

            return reArray;
        }
    }
}
