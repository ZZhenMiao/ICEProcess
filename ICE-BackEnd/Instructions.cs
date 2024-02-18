using System;
using System.Windows;
using LiZhenMySQL;
using ICE_Model;
using ICE_BackEnd.Properties;
using MySql.Data.MySqlClient;
using LiZhenStandard.Sockets;
using System.Collections.Generic;
using LiZhenStandard.Extensions;
using System.Linq;
using System.Diagnostics;
using LiZhenStandard;
using System.IO;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations.Schema;
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
        /// <summary>
        /// 注册指令集
        /// </summary>
        public static void RegisterInstruct()
        {
            SocketFunction.AddInstruct("0", new Func<object[], object[]>((objs) =>
            {
                return null;
            }));
            SocketFunction.AddInstruct("FindSimilarityAssets_ByAsset", new Func<object[], object[]>((objs) =>
            {
                ulong assetID = Convert.ToUInt64(objs[0]);
                var re = FindSimilarityAssets(assetID, out AssetType[] types, out AssetLabel[] labels, out Project[] projects);
                return new object[] { re, types, labels, projects };
            }));
            SocketFunction.AddInstruct("FindSimilarityAssets_ByInfos", new Func<object[], object[]>((objs) =>
            {
                ulong[] atypeIDs = (ulong[])objs[0];
                ulong[] alabelIDs = (ulong[])objs[1];
                ulong[] aprojectIDs = (ulong[])objs[2];
                ulong diretoryID = Convert.ToUInt64(objs[3]);
                var re = FindSimilarityAssets(atypeIDs, alabelIDs, aprojectIDs, diretoryID);
                return re;
            }));
            SocketFunction.AddInstruct("LoadUniversualExtensions", new Func<object[], object[]>((objs) =>
            {
                TableReadInfo readInfo = DataBase.DBReader("SELECT * FROM asslib_extension", DataBase.GetMySqlConnection(typeof(Asset)));
                List<UniversualExtension> universualExtensions = new List<UniversualExtension>();
                foreach (ItemReadInfo item in readInfo)
                {
                    UniversualExtension ue = new UniversualExtension();
                    ue.Name = item["name"]?.ToString();
                    ue.Illustration = item["illustration"]?.ToString();
                    ue.Path = item["path"]?.ToString();
                    ue.Args = item["args"]?.ToString().Split("|");
                    universualExtensions.Add(ue);
                }
                return universualExtensions.ToArray();
            }));
            SocketFunction.AddInstruct("DeleteAssetFileMD5", new Func<object[], object[]>((objs) =>
            {
                string[] paths = (string[])objs;
                DeleteAssetFileMD5(paths);
                return null;
            }));
            //SocketFunction.AddInstruct("CheckAndInsertAssetFileMD5", new Func<object[], object[]>((objs) =>
            //{
            //    AssetFileMd5Info[] infos = (AssetFileMd5Info[])objs;
            //    var md5s = from info in infos select info.MD5;
            //    string[] re = CheckAssetFileMD5(md5s.ToArray());
            //    if (re.Any())
            //        return re;
            //    InsertAssetFileMD5(infos);
            //    return null;
            //}));
            SocketFunction.AddInstruct("CheckAssetFileMD5", new Func<object[], object[]>((objs) =>
            {
                var md5s = (string[])objs;
                string[] re = CheckAssetFileMD5(md5s.ToArray());
                return re;
            }));
            SocketFunction.AddInstruct("InsertAssetFileMD5", new Func<object[], object[]>((objs) =>
            {
                AssetFileMd5Info[] infos = (AssetFileMd5Info[])objs;
                InsertAssetFileMD5(infos);
                return new object[0];
            }));
            SocketFunction.AddInstruct("RenameAsset", new Func<object[], object[]>((objs) =>
            {
                ulong assetID = Convert.ToUInt64(objs[0]);
                string oldName = objs[1] as string;
                string newName = objs[2] as string;
                var con = DataBase.GetMySqlConnection(typeof(Asset));
                DataBase.CommandNonQuery($"update asset set `name` = \"{newName}\" where id = {assetID} and `name` = \"{oldName}\";", con);
                con.TryClose();
                return new object[0];
            }));
            SocketFunction.AddInstruct("DeleteAsset", new Func<object[], object[]>((objs) =>
            {
                ulong assetID = Convert.ToUInt64(objs[0]);
                var con = DataBase.GetMySqlConnection(typeof(Asset));
                DataBase.CommandNonQuery($"delete from ass_asset_assetlabel where id_asset = {assetID};", con);
                DataBase.CommandNonQuery($"delete from ass_assettype_asset where id_asset = {assetID};", con);
                DataBase.CommandNonQuery($"delete from ass_project_asset where id_asset = {assetID};", con);
                DataBase.CommandNonQuery($"delete from asset where id = {assetID};", con);
                con.TryClose();
                return new object[0];
            }));
            SocketFunction.AddInstruct("LoadAssetInfo", new Func<object[], object[]>(App.LoadAssetInfo));
            SocketFunction.AddInstruct("LoadLabelGroups", new Func<object[], object[]>((objs) =>
            {
                bool loadAllLabelGroup = (bool)objs[0];
                ulong[] assetTypeIDs = (ulong[])objs[1];
                ulong[] projectIDs = (ulong[])objs[2];
                ulong[] selectedLabelIDs = (ulong[])objs[3];

                if (selectedLabelIDs.Length < 1)
                    selectedLabelIDs = new ulong[1] { 0 };
                if (projectIDs.Length < 1)
                    projectIDs = new ulong[1] { 0 };
                if (assetTypeIDs.Length < 1)
                    loadAllLabelGroup = true;

                //AssetType[] assettypes = null;
                //assettypes = DataBase.LoadFromDB_Where<AssetType>();
                //assetTypeIDs = (from type in assettypes select Convert.ToUInt64(type.ID)).ToArray();

                //Obligation[] obligations = DataBase.LoadFromDB_Where<Obligation>($"id in (select id_obligation from ass_position_obligation where id_position in (select id_position from ass_position_person where id_person = {personID}))");
                //foreach (var oblg in obligations)
                //{
                //    if (oblg.AllAssetType >= 0)
                //    { alltypes = true; break; }
                //}
                //if (alltypes)
                //    assettypes = DataBase.LoadFromDB_Where<AssetType>();
                //else
                //    assettypes = DataBase.LoadFromDB_Where<AssetType>($"id IN (SELECT id_assettype from ass_assettype_obligation WHERE id_obligation IN (SELECT id_obligation FROM ass_position_obligation WHERE id_position IN (SELECT id_position FROM ass_position_person WHERE id_person = {personID})))");

                string whereStr = null;

                //if (!loadAllLabelGroup)
                //    whereStr = $"id IN (SELECT id_assetlabelgroup FROM ass_assettype_assetlabelgroup WHERE id_assettype IN ({assetTypeIDs.AllToString(forDataBase: true)}))  OR id IN (SELECT id_assetlabelgroup FROM ass_project_assetlabelgroup WHERE id_project IN ({projectIDs.AllToString(forDataBase: true)})) OR id IN (SELECT id_assetlabelgroup FROM ass_assetlabelgroup_assetlabel WHERE id_assetlabel IN({selectedLabelIDs.AllToString(forDataBase: true)}))";

                AssetLabelGroup[] labelgroups = DataBase.LoadFromDB_Where<AssetLabelGroup>(whereStr);
                AssetLabel[] labels = DataBase.LoadFromDB_Where<AssetLabel>();
                TableReadInfo reader = DataBase.DBReader("select * from ass_assetlabel_assetlabelgroup", DataBase.GetMySqlConnection(typeof(AssetLabel)));
                for (int i = 0; i < reader.Count; i++)
                {
                    object id_assetlabel = reader[i, "id_assetlabel"];
                    object id_assetlabelgroup = reader[i, "id_assetlabelgroup"];
                    AssetLabelGroup group = labelgroups.FirstOrDefault(a => a.ID.Equals(id_assetlabelgroup));
                    AssetLabel label = labels?.FirstOrDefault(a => a.ID.Equals(id_assetlabel));
                    AssetLabel nlabel = new AssetLabel()
                    {
                        Name = string.Format("{0} -({1})", label.Name, group.Name),
                        Illustration = label.Illustration,
                        ID = label.ID
                    };
                    group?.AssetLabels.Add(nlabel);
                }
                return labelgroups;
            }));
            SocketFunction.AddInstruct("QueryAssets", new Func<object[], object[]>((objs) =>
            {
                ulong[] assetTypeIDs = (ulong[])objs[0];
                ulong[] projectIDs = (ulong[])objs[1];
                ulong[] selectedLabelIDs = (ulong[])objs[2];
                bool loadAllAssets = (bool)objs[3];

                if (selectedLabelIDs.Length < 1)
                    selectedLabelIDs = new ulong[1] { 0 };

                //AssetType[] assettypes = null;

                string typeWhere = "";
                //if (assettypes is not null)
                typeWhere = $" AND id IN (SELECT id_asset FROM ass_assettype_asset WHERE id_assettype IN ({(assetTypeIDs.Length < 1 ? "0" : assetTypeIDs.AllToString())}))";

                string pjWhere = "";
                if (projectIDs.Any())
                    pjWhere = $" AND id IN (SELECT id_asset FROM ass_project_asset WHERE id_project IN ({projectIDs.AllToString()}))";

                string assetWhereStr = "id > 0";
                if (!loadAllAssets)
                    assetWhereStr = $"id IN (SELECT id_asset FROM (SELECT id_asset,COUNT(id_asset) cc FROM ass_asset_assetlabel WHERE id_assetlabel IN({selectedLabelIDs.AllToString()}) GROUP BY id_asset) AS a WHERE cc = {selectedLabelIDs.Length})";

                Asset[] assets = DataBase.LoadFromDB_Where<Asset>(assetWhereStr + typeWhere + pjWhere);
                SetAssetFullPath(assets);
                SetAssetPerson(assets);
                return assets;//会返回Project[]？？？
            }));
            SocketFunction.AddInstruct("InsertOrUpdateAsset", new Func<object[], object[]>((objs) =>
            {
                ulong[] projectIDs = (ulong[])objs[0];
                ulong[] assetTypeIDs = (ulong[])objs[1];
                ulong[] labelIDs = (ulong[])objs[2];
                ulong? target_AssetDirectoryID = (ulong?)objs[3];
                string target_AssetDirectoryPath = (string)objs[4];
                string[] source_FilePaths = (string[])objs[5];
                ulong id_Uploader = Convert.ToUInt64(objs[6]);

                //Debug.WriteLine($"projectIDs:{projectIDs.AllToString()}\rassetTypeIDs:{assetTypeIDs.AllToString()}\rlabelIDs:{labelIDs.AllToString()}\rdirectoryID:{directoryID}\rfullPath:{directoryPath}\rfileNames:{fileNames.AllToString()}");

                List<Asset> assets = new List<Asset>();
                foreach (string filePath in source_FilePaths)
                {
                    string fileName = Path.GetFileName(filePath);
                    string source_Folder = Path.GetDirectoryName(filePath);

                    Asset asset = new Asset() { Name = fileName, ArchiveTime = DateTime.Now, ID_Uploader = id_Uploader };
                    asset.ID = null;

                    //检查源文件是否已经在某个资产目录之中。
                    object[] checkSourceInADir_result = IsThisAssetInAssetDirectory(new object[] { filePath });
                    object sourceADirID = checkSourceInADir_result.FirstOrDefault();
                    string sourceADirPath = sourceADirID.Equals(-1) ? null : checkSourceInADir_result[1].ToString();
                    string path = sourceADirPath is null ? source_Folder : source_Folder.Replace(sourceADirPath, string.Empty);

                    if (sourceADirID is not null && !sourceADirID.Equals(-1))//如果源文件在已布局的资产目录中。
                    {
                        Asset[] source_finds = DataBase.LoadFromDB_Where<Asset>("id_assetdirectory {0} and path {1} and name = \"{2}\"", sourceADirID is null ? "is null" : " = \"" + sourceADirID + "\"", string.IsNullOrWhiteSpace(path) ? "is null" : " = \"" + path + "\"", fileName);
                        if (source_finds.Any())//如果找到了那个资产
                            asset.ID = source_finds.First().ID;
                    }
                    else//如果源文件虽然不在已布局的资产目录中，但它却是已经被记录的资产。
                    {
                        Asset[] source_finds = DataBase.LoadFromDB_Where<Asset>("path {0} and name = \"{1}\"", " = \"" + source_Folder + "\"", fileName);
                        if (source_finds.Any())//如果找到了那个资产
                            asset.ID = source_finds.First().ID;
                    }

                    if (target_AssetDirectoryID is not null)//directoryID不为空，代表执行自动归档，且给出了明确的目标资产目录ID。
                    {
                        asset.ID_AssetDirectory = target_AssetDirectoryID;
                        asset.Path = null;
                    }
                    else//directoryID为空，代表不执行自动归档，并且没有传入目标资产目录ID。
                    {
                        //即使如此，其源文件是否在资产目录之中。将查找出的资产目录ID赋值给资产。
                        asset.ID_AssetDirectory = sourceADirID.Equals(-1) ? null : sourceADirID;
                        //如果没有相应的资产目录ID，则Path为完整的源文件所在目录。如果有，则Path为截断后的源Path。
                        asset.Path = string.IsNullOrWhiteSpace(path) ? string.Empty : asset.ID_AssetDirectory is null ? path.Replace(@"\", @"\\") : Regex.Replace(path, @"^\\", string.Empty).Replace(@"\", @"\\");
                    }

                    //检查上传目标是否已经是存在于资产库内的资产，也就是覆盖已归档的现有资产。
                    Asset[] finds = DataBase.LoadFromDB_Where<Asset>("id_assetdirectory {0} and path {1} and name = \"{2}\"", asset.ID_AssetDirectory is null ? "is null" : " = \"" + asset.ID_AssetDirectory + "\"", asset.Path is null ? "is null" : " = \"" + asset.Path + "\"", asset.Name);
                    if (finds.Any())
                        asset.ID = finds.First().ID;

                    //判断是否已经在AssetDirectory中~是否修改了位置
                    if (asset.ID is null)
                    {
                        Debug.WriteLine("插入新资产：" + asset);
                        asset.InsertThisToDB();
                    }
                    else
                    {
                        Debug.WriteLine("修改已有资产：" + asset);
                        asset.ModifyThisToDB();
                    }

                    assets.Add(asset);
                }
                ulong[] assetIDs = (from asset in assets select Convert.ToUInt64(asset.ID)).ToArray();
                InsertAss_Asset_AssetType(assetIDs, assetTypeIDs, true);
                InsertAss_Asset_Project(assetIDs, projectIDs, true);
                InsertAss_Asset_AssetLabel(assetIDs, labelIDs, true);


                return new object[] { assets.ToArray() };
            }));
            SocketFunction.AddInstruct("CheckLogin", new Func<object[], object[]>((objs) =>
            {
                string user = objs[0].ToString();
                string pw = objs[1].ToString();
                DataBase.LoginAuthentication(user, pw, out Person reUser);
                return new object[] { reUser };
            }));
            SocketFunction.AddInstruct("LoadProductions", new Func<object[], object[]>((objs) =>
            {
                Production[] productions = DataBase.LoadFromDB_Where<Production>();
                return productions;
            }));
            SocketFunction.AddInstruct("LoadPublicSettings", new Func<object[], object[]>((objs) =>
            {
                PublicSetting[] publicSettings = DataBase.LoadFromDB_Where<PublicSetting>();
                return publicSettings;
            }));
            SocketFunction.AddInstruct("LoadAllProjectTree", new Func<object[], object[]>((objs) =>
            {
                Project[] projects = DataBase.LoadFromDB_Where<Project>();
                Project[] re = IDBTree<Project>.MakeTree(projects);
                return re;
            }));
            SocketFunction.AddInstruct("LoadAllAssetTypeTree", new Func<object[], object[]>((objs) =>
            {
                bool AutoArahive = objs is null ? false : (bool)objs[0];
                AssetType[] assetTypes = DataBase.LoadFromDB_Where<AssetType>("enabled = true");
                AssetType[] trees = IDBTree<AssetType>.MakeTree(assetTypes);

                if (AutoArahive)
                {
                    //assetTypes = DataBase.LoadFromDB_Where<AssetType>("id in (select id_assettype from ass_autoarchivescheme_assettype) and enabled = true");
                    TableReadInfo readInfo = DataBase.DBReader(@"select * from ass_autoarchivescheme_assettype GROUP BY id_assettype;", DataBase.GetMySqlConnection(typeof(AssetType)));
                    List<object> ids = new List<object>();
                    for (int i = 0; i < readInfo.Count; i++)
                    {
                        ids.Add(readInfo[i, "id_assettype"]);
                    }
                    IEnumerable<AssetType> types = assetTypes.Where(a => ids.Contains(a.ID));
                    List<AssetType> pre_re = new List<AssetType>();
                    foreach (var type in types)
                    {
                        IEnumerable<AssetType> prents = type.GetParentNodesWithSelf_NoDataBase();
                        foreach (var p in prents)
                        {
                            if (!pre_re.Contains(p))
                                pre_re.Add(p);
                        }
                    }
                    foreach (var p in pre_re)
                    {
                        p.Clear();
                    }
                    AssetType[] re = IDBTree<AssetType>.MakeTree(pre_re);
                    return re;
                }
                else
                    return trees;

                //AssetType[] re = IDBTree<AssetType>.MakeTree(assetTypes);
                //return re;
            }));
            SocketFunction.AddInstruct("LoadLabelGroupByProjects_AssetTypes_SelectedLabels", new Func<object[], object[]>((objs) =>
            {
                bool showAllLabelGroups = (bool)objs[0];
                ulong[] assetTypesID = (ulong[])objs[1];
                bool forAllProjects = (bool)objs[2];
                ulong[] projects = (ulong[])objs[3];
                ulong[] selectedLabels = (ulong[])objs[4];
                bool onlyNecessary = (bool)objs[5];

                if (selectedLabels.Length < 1)
                    selectedLabels = new ulong[1] { 0 };

                AssetLabelGroup[] re = null;
                //如果选择了显示全部，则查询全部标签组。
                if (showAllLabelGroups)
                {
                    re = DataBase.LoadFromDB_Where<AssetLabelGroup>();
                    return re;
                }

                //如果未选择显示全部，并未选择任何资产类型，则..。
                if (!showAllLabelGroups && assetTypesID.Length < 1)
                {
                    //re = DataBase.LoadFromDB_Where<AssetLabelGroup>("id in (select id_assetlabelgroup from ass_assettype_assetlabelgroup where id_assettype > 0)");
                    return new AssetLabelGroup[0];
                }

                //List<AssetType> types = new List<AssetType>();
                //foreach (ulong typeid in assetTypesID)
                //{
                //    AssetType type = typeid.LoadFromDB_ThisID<AssetType>();
                //    if (type is null)
                //        continue;
                //    IEnumerable<AssetType> parents = type.GetParentNodesWithSelf();
                //    types.AddRange(parents);
                //}
                IEnumerable<ulong> allAssetTypesID = assetTypesID;//= from type in types select Convert.ToUInt64(type.ID);

                //如果选择了通用资产，或未选择任何项目，则排除所有项目标签组。
                if (forAllProjects || projects.Length < 1)
                {
                    re = DataBase.LoadFromDB_Command<AssetLabelGroup>("select * from assetlabelgroup where id in (select id_assetlabelgroup from ass_assettype_assetlabelgroup where id_assettype in ({0})) OR id IN (SELECT id_assetlabelgroup from ass_assetlabelgroup_assetlabel WHERE id_assetlabel IN ({1})) AND id NOT IN (Select id_assetlabelgroup From ass_project_assetlabelgroup)", allAssetTypesID.AllToString(), selectedLabels.AllToString());
                    return re;
                }

                //如果请求只返回自动归档方案必要标签组
                if (onlyNecessary)
                {
                    string pjwhere = projects.Length < 1 ? null : $"and id_autoarchivescheme in ( select id_autoarchivescheme from ass_autoarchivescheme_project where id_project in ({projects.AllToString()}))";
                    re = DataBase.LoadFromDB_Command<AssetLabelGroup>("select * from assetlabelgroup where id in (select id_assetlabelgroup from ass_assetlabel_assetlabelgroup where id_assetlabel in ( select id_assetlabel from ass_autoarchivescheme_assetlabel where id_autoarchivescheme in ( select id_autoarchivescheme from ass_autoarchivescheme_assettype where id_assettype in ({0}))" + pjwhere + "))", assetTypesID.AllToString());
                    return re;
                }

                //若非上述任何情况，则按照正常检索条件（关联的资产类型，关联的项目，已选择的标签）查询。
                re = DataBase.LoadFromDB_Command<AssetLabelGroup>("select * from assetlabelgroup where id in (select id_assetlabelgroup from ass_assettype_assetlabelgroup where id_assettype in ({0})) OR id IN (SELECT id_assetlabelgroup from ass_assetlabelgroup_assetlabel WHERE id_assetlabel IN ({1})) OR id IN (Select id_assetlabelgroup From ass_project_assetlabelgroup Where id_project In ({2}))", assetTypesID.AllToString(), selectedLabels.AllToString(), projects.AllToString());
                return re;
            }));
            SocketFunction.AddInstruct("LoadLabelBySelectedLabelGroup", new Func<object[], object[]>((objs) =>
            {
                ulong groupID = Convert.ToUInt64(objs[0]);
                AssetLabel[] re = DataBase.LoadFromDB_Where<AssetLabel>("id in (select id_assetlabel from ass_assetlabel_assetlabelgroup where id_assetlabelgroup = {0})", groupID);
                return re;
            }));
            SocketFunction.AddInstruct("LoadAssetLabelByInputLabelName", new Func<object[], object[]>((objs) =>
            {
                string input = (string)objs[0];
                AssetLabel[] re = DataBase.LoadFromDB_Where<AssetLabel>("`name` LIKE \'%{0}%\'", input);
                return re;
            }));
            SocketFunction.AddInstruct("LoadParentLabelsByAss_Label", new Func<object[], object[]>((objs) =>
            {
                ulong ID_Label = Convert.ToUInt64(objs[0]);

                Func<ulong, AssetLabel[]> func = null;
                func = (id) =>
                {
                    List<AssetLabel> re = new List<AssetLabel>();
                    AssetLabel[] labels = DataBase.LoadFromDB_Where<AssetLabel>("id in (select id_assetlabel from ass_assetlabelgroup_assetlabel where id_assetlabelgroup in (select id_assetlabelgroup from ass_assetlabel_assetlabelgroup where id_assetlabel = {0}))", id);
                    re.AddRange(labels);
                    if (labels.Length > 0)
                        foreach (AssetLabel item in labels)
                        {
                            re.AddRange(func.Invoke(Convert.ToUInt64(item.ID)));
                        }
                    return re.ToArray();
                };

                List<AssetLabel> re = new List<AssetLabel>();
                AssetLabel[] labels = func.Invoke(ID_Label);

                re.AddRange(labels);
                return re.ToArray();
            }));
            SocketFunction.AddInstruct("FindAssetTypesByLabels", new Func<object[], object[]>((objs) =>
            {
                ulong[] ids = (ulong[])objs[0];
                bool autoArchive = (bool)objs[1];
                AssetType[] types = DataBase.LoadFromDB_Command<AssetType>("select * from assettype where id in (select id_assettype from ass_assettype_assetlabelgroup where id_assetlabelgroup in (select id from assetlabelgroup where id in (select id_assetlabelgroup from ass_assetlabel_assetlabelgroup where id_assetlabel in ({0}))))", ids.AllToString());
                List<AssetType> re_autoAc = new List<AssetType>();
                List<AssetType> re = new List<AssetType>();

                if (!types.Any())
                    return types;

                TableReadInfo reader = DataBase.DBReader($@"select * from ass_autoarchivescheme_assettype where id_assettype in ({(from type in types select Convert.ToUInt64(type.ID)).AllToString()})", App.MySqlConnection);
                IEnumerable<ulong> autoArcTypeIDs = from item in reader select Convert.ToUInt64(item["id_assettype"]);
                foreach (AssetType type in types)
                {
                    if (autoArcTypeIDs.Contains(Convert.ToUInt64(type.ID)))
                        re_autoAc.Add(type);
                    else
                        re.Add(type);
                }
                if (autoArchive == true)
                    return re_autoAc.ToArray();
                else
                    return re.ToArray();
            }));
            SocketFunction.AddInstruct("MatchAssetDirectory", new Func<object[], object[]>((objs) =>
            {
                ulong[] TypesID = (ulong[])objs[0];
                ulong[] ProjectsID = (ulong[])objs[1];
                ulong[] labelsID = (ulong[])objs[2];

                string typesStr = TypesID.Length > 0 ? $@"in ({TypesID.AllToString()})" : "is null";
                string pjsStr = ProjectsID.Length > 0 ? $@"in ({ProjectsID.AllToString()})" : "is null";
                string labelsStr = labelsID.Length > 0 ? $@"in ({labelsID.AllToString()})" : "is null";

                AssetDirectory[] dirs = DataBase.LoadFromDB_Command<AssetDirectory>($@"Call func_matchassetdirectory('{typesStr}','{pjsStr}','{labelsStr}')");
                if (!dirs.Any())
                    return dirs;

                List<AssetDirectory> reDirTree = new List<AssetDirectory>();
                foreach (AssetDirectory dir in dirs)
                {
                    List<AssetDirectory> parentNodes = dir.GetParentNodesWithSelf().ToList();
                    foreach (AssetDirectory node in parentNodes)
                    {
                        if (node.ID_Nas is not null && node.ID_Nas is not DBNull)
                            node.Nas = node.ID_Nas.LoadFromDB_ThisID<Nas>();
                    }
                    AssetDirectory[] dirTrees = IDBTree<AssetDirectory>.MakeTree(parentNodes);
                    AssetDirectory dirTree = dirTrees.FirstOrDefault();
                    reDirTree.Add(dirTree);
                }
                return reDirTree.ToArray();
            }));
            SocketFunction.AddInstruct("IsThisAssetInAssetDirectory", new Func<object[], object[]>((objs) =>
            {
                return IsThisAssetInAssetDirectory(objs);
            }));
        }
    }
}
