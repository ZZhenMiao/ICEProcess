using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using LiZhenStandard.Extensions;

namespace LiZhenMySQL
{
    //旧的
    //public static class MainDataBase
    //{
    //    public static SqlConnection Connection { get; set; }
    //    public static string DataBaseName { get; set; }

    //    public static TableReadInfo Reader(string commandStr)
    //    {
    //        TableReadInfo re = new();
    //        MySqlCommand command = new(commandStr, Connection.Connection);
    //        Connection.Open();
    //        Debug.WriteLine(commandStr);
    //        MySqlDataReader dataReader = command.ExecuteReader();
    //        while (dataReader.Read())
    //        {
    //            ItemReadInfo item = new ItemReadInfo();
    //            int n = dataReader.FieldCount;
    //            for (int i = 0; i < n; i++)
    //            {
    //                ColumnReadInfo fieldReadInfo = new ColumnReadInfo()
    //                {
    //                    ColumnName = dataReader.GetName(i),
    //                    Value = dataReader.GetValue(i)
    //                };
    //                item.Add(fieldReadInfo);
    //            }
    //            re.Add(item);
    //        }
    //        return re;
    //    }
    //    public static int CommandNonQuery(string commandStr)
    //    {
    //        MySqlCommand command = new MySqlCommand(commandStr, Connection.Connection);
    //        Connection.Open();
    //        //Console.WriteLine(commandStr);
    //        return command.ExecuteNonQuery();
    //    }
    //    public static T[] LoadFromDB<T>(object id, string tableName = null,string idColumnName = null) where T : IDbObject
    //    {
    //        return id is DBNull ? null : LoadFromDB<T>(string.Format(@"{0}={1}", idColumnName, id.ToDBString()), tableName);
    //    }
    //    public static T[] LoadFromDB<T>(string where = null, string tableName = null) where T : IDbObject
    //    {
    //        MemberInfo[] allmembers = typeof(T).GetSqlMembers();
    //        tableName = tableName ?? typeof(T).GetSqlTableName();
    //        string memberNames = typeof(IEpitome).IsAssignableFrom(typeof(T)) ? (from mmbName in allmembers select mmbName.Name).AllToString() : @"*";
    //        TableReadInfo allItems = string.IsNullOrWhiteSpace(where)
    //            ? Reader(string.Format(@"select {1} from {0}", tableName, memberNames))
    //            : Reader(string.Format(@"select {2} from {0} where {1}", tableName, where, memberNames));
    //        Connection.Close();
    //        return MakeT<T>(allItems, allmembers);
    //    }
    //    public static T[] LoadFromDB<T>(string commandStr,params object[] args)
    //    {
    //        string str = string.Format(commandStr, args);
    //        MemberInfo[] allmembers = typeof(T).GetSqlMembers();
    //        var tableName = typeof(T).GetSqlTableName();
    //        string memberNames = typeof(IEpitome).IsAssignableFrom(typeof(T)) ? (from mmbName in allmembers select mmbName.Name).AllToString() : @"*";
    //        TableReadInfo allItems = Reader(string.Format(@"select {1} from {0} {2}", tableName, memberNames, str));
    //        Connection.Close();
    //        return MakeT<T>(allItems, allmembers);
    //    }
    //    public static T[] LoadFromDB<T>(ChainingInfo chainInfo, object[] targetIDs) where T : IDbObject
    //    {
    //        string targetIDWhereStr = "";
    //        for (int i = 0; i < targetIDs.Length; i++)
    //        {
    //            object id = targetIDs[i];
    //            targetIDWhereStr += string.Format(@"{0}={1}", chainInfo.TargetIDColumnName, id.ToDBString());
    //            if (i < targetIDs.Length - 1)
    //                targetIDWhereStr += @" or ";
    //        }
    //        MemberInfo[] allmembers = typeof(T).GetSqlMembers();
    //        string memberNames = typeof(IEpitome).IsAssignableFrom(typeof(T)) ? (from mmbName in allmembers select mmbName.Name).AllToString(forDataBase:true) : @"*";
    //        TableReadInfo allItems = Reader(
    //            string.Format(@"select {0} from {1} where id in (select {2} from {3} where {4})",
    //            typeof(T).IsAssignableFrom(typeof(IEpitome)) ? memberNames : @"*",
    //            typeof(T).GetSqlTableName(),
    //            chainInfo.SourceIDColumnName,
    //            chainInfo.ChainTableName,
    //            targetIDWhereStr
    //            ));
    //        Connection.Close();
    //        return MakeT<T>(allItems, allmembers);
    //    }
    //    public static T LoadOneFromDB<T>(object id, string tableName = null, string idColumnName = null) where T : IDbObject
    //    {
    //        return id is DBNull ? default : LoadFromDB<T>(string.Format(@"{0}={1}", idColumnName, id.ToDBString()), tableName).FirstOrDefault();
    //    }
    //    public static T ReloadThisFromDB<T>(this T dbobject) where T : IDbObject
    //    {
    //        return MainDataBase.LoadFromDB<T>(id: dbobject.ID).FirstOrDefault();
    //    }
    //    private static T[] MakeT<T>(TableReadInfo allItems, MemberInfo[] allmembers)
    //    {
    //        List<T> re = new List<T>();
    //        foreach (ItemReadInfo item in allItems)
    //        {
    //            T obj = (T)typeof(T).GetConstructor(Type.EmptyTypes).Invoke(null);
    //            foreach (MemberInfo member in allmembers)
    //            {
    //                var columnName = member.GetSqlColumnName();
    //                var value = item[columnName];
    //                member.SetValue(obj, value);
    //            }
    //            re.Add(obj);
    //        }
    //        return re.ToArray();
    //    }
    //    public static object InsertToDB<T>(T obj) where T : IDbObject
    //    {
    //        string str = obj.GetInsertStr();
    //        CommandNonQuery(str);
    //        ItemReadInfo read;
    //        read = Reader("select last_insert_id()").FirstOrDefault();
    //        object reID = read[0].Value;
    //        obj.GetSqlPKProperties().FirstOrDefault()?.SetValue(obj, reID);
    //        Connection.Close();
    //        return reID;
    //    }
    //    public static object InsertThisToDB<T>(this T obj)where T : IDbObject
    //    {
    //        return InsertToDB(obj);
    //    }
    //    public static object Insert(string tableName, params object[] values)
    //    {
    //        if (Connection.State == ConnectionState.Closed)
    //            Connection.Open();

    //        // 创建要插入的MySQL语句
    //        string mysqlInsert = string.Format(@"insert into {0} values ({1})", tableName, values.AllToString(forDataBase: true));

    //        // 创建用于实现MySQL语句的对象
    //        MySqlCommand mySqlCommand = new MySqlCommand(mysqlInsert, Connection.Connection);

    //        // 执行MySQL语句进行插入
    //        mySqlCommand.ExecuteNonQuery();

    //        mySqlCommand.CommandText = @"select last_insert_id()";
    //        MySqlDataReader reader = mySqlCommand.ExecuteReader();
    //        object re = null;
    //        if (reader.Read())
    //            re = reader.GetValue(0);
    //        Connection.Close();
    //        return re;
    //    }
    //    public static int Delete(out string e, string tableName, params string[] wheres)
    //    {
    //        try
    //        {
    //            if (Connection.State == ConnectionState.Closed)
    //                Connection.Open();

    //            // 创建要执行删除的MySQL语句
    //            string mysqlDelete = string.Format(@"delete from {0} where {1}", tableName, wheres.AllToString(separator: ""));

    //            // 创建用于实现MySQL语句的对象
    //            MySqlCommand mySqlCommand = new MySqlCommand(mysqlDelete, Connection.Connection);

    //            // 执行MySQL语句进行插入
    //            e = null;
    //            return mySqlCommand.ExecuteNonQuery();
    //        }
    //        catch (Exception ex)
    //        {
    //            e = ex.Message;
    //            return 0;
    //        }
    //    }
    //    public static int Delete(string tableName, params string[] wheres)
    //    {
    //        return Delete(out _, tableName, wheres);
    //    }
    //    public static int DeleteThisFromDB(IDbObject dbObject)
    //    {
    //        return Delete(dbObject.GetSqlTableName(),string.Format("id={0}",dbObject.ID.ToDBString()));
    //    }
    //    public static int Modify(string tableName, string[] fieldAndValues, string[] wheres)
    //    {
    //        if (Connection.State == ConnectionState.Closed)
    //            Connection.Open();

    //        // 创建要执行修改的MySQL语句
    //        string mysqlInsert = string.Format(@"update {0} set {1} where {2} ; ", tableName, fieldAndValues.AllToString(), wheres.AllToString(separator: ""));

    //        // 创建用于实现MySQL语句的对象
    //        MySqlCommand mySqlCommand = new MySqlCommand(mysqlInsert, Connection.Connection);

    //        // 执行MySQL语句进行插入
    //        return mySqlCommand.ExecuteNonQuery();
    //    }
    //    public static int Modify(IDbObject dbObject)
    //    {
    //        if (Connection.State == ConnectionState.Closed)
    //            Connection.Open();

    //        // 创建要执行修改的MySQL语句
    //        string mysqlInsert = dbObject.GetModifyStr();

    //        // 创建用于实现MySQL语句的对象
    //        MySqlCommand mySqlCommand = new MySqlCommand(mysqlInsert, Connection.Connection);

    //        //Console.WriteLine(mySqlCommand.CommandText);
    //        //return 0;

    //        // 执行MySQL语句进行插入
    //        return mySqlCommand.ExecuteNonQuery();
    //    }
    //    public static int ModifyThisToDB(this IDbObject dbObject)
    //    {
    //        return Modify(dbObject);
    //    }
    //    //public static string GetTableComment(string tableName)
    //    //{
    //    //    MySqlCommand con = new MySqlCommand(string.Format("show table status like {0}", tableName.ToDBString()), Connection.Connection);
    //    //    Connection.Open();
    //    //    MySqlDataReader reader = con.ExecuteReader();
    //    //    while (reader.Read())
    //    //    {
    //    //        return reader["Comment"].ToString();
    //    //    }
    //    //    return string.Empty;
    //    //}
    //    //public static ChainInfo[] GetChainInfos(string tableName)
    //    //{
    //    //    string tableComment = GetTableComment(tableName);
    //    //    List<ChainInfo> re = new List<ChainInfo>();
    //    //    tableComment.IsMatch(@"(?<=[Cc][Hh][Aa][Ii][Nn]\[).+(?=\])", out string value);
    //    //    string[] all = value.Split(',');
    //    //    for (int i = 0; i < all.Length; i++)
    //    //    {
    //    //        all[i].IsMatch(@".+?(?=\(.+\))", out string targetTableName);
    //    //        all[i].IsMatch(@"(?<=\().+?(?=\))", out string targetFieldName);

    //    //        re.Add(new ChainInfo() { TargetTableName = targetTableName, SourceIDColumnName = targetFieldName });
    //    //    }
    //    //    return re.ToArray();
    //    //}
    //    public static bool RepeatedVerification(IDbObject obj)
    //    {
    //        for (int i = 1; i < 10; i++)
    //        {
    //            PropertyInfo[] ckppts = obj.GetSqlCKProperties(i);
    //            if (ckppts.Length < 1)
    //                break;
    //            string columnNames = (from ckppt in ckppts select ckppt.GetSqlColumnName()).AllToString();
    //            string conditions = (from ckppt in ckppts select ckppt.GetSqlColumnValueStr(obj)).AllToString(separator:" and ");
    //            TableReadInfo readInfo = Reader(string.Format(@"select {0} from {1} where {2}", columnNames, obj.GetSqlTableName(), conditions));
    //            if (readInfo.Count > 0)
    //                return false;
    //        }
    //        return true;
    //    }
    //}

    /// <summary>
    /// 连接信息
    /// </summary>
    public class ChainingInfo
    {
        private static List<ChainTableInfo> ChainTableInfoList { get; } = new();
        public static void RegistChainTableInfo(Type typeA, Type typeB)
        {
            RegistChainTableInfo(typeA, null, typeB, null, typeA.Name.ToLower() + "_" + typeB.Name.ToLower());
        }
        public static void RegistChainTableInfo(Type typeA, Type typeB, string chainTableName)
        {
            RegistChainTableInfo(typeA, null, typeB, null, chainTableName);
        }
        public static void RegistChainTableInfo(Type typeA, string typeA_IDColumn, Type typeB, string typeB_IDColumn, string chainTableName)
        {
            ChainTableInfoList.Add(new ChainTableInfo() { TypeA = typeA, TypeA_IDColumn = typeA_IDColumn, TypeB = typeB, TypeB_IDColumn = typeB_IDColumn, ChainTableName = chainTableName });
        }
        public static string GetChainTableName(Type typeA, Type typeB)
        {
            return GetChainTableInfo(typeA, out _, typeB, out _,out _);
        }
        public static string GetChainTableInfo(Type typeA, out string typeAIDColumn, Type typeB, out string typeBIDColumn)
        {
            return GetChainTableInfo(typeA, out typeAIDColumn, typeB, out typeBIDColumn, out _);
        }
        public static string GetChainTableInfo(Type typeA, out string typeAIDColumn, Type typeB, out string typeBIDColumn,out MySqlConnection connection)
        {
            ChainTableInfo find = null;
            find = ChainTableInfoList.Find(a => a.TypeA == typeA && a.TypeB == typeB);
            if(find.IsNotNull())
            {
                typeAIDColumn = find.TypeA_IDColumn;
                typeBIDColumn = find.TypeB_IDColumn;
                connection = find.Connection;
                return find.ChainTableName;
            }
            find = ChainTableInfoList.Find(a => a.TypeA == typeB && a.TypeB == typeA);
            if (find.IsNotNull())
            {
                typeAIDColumn = find.TypeB_IDColumn;
                typeBIDColumn = find.TypeA_IDColumn;
                connection = find.Connection;
                return find.ChainTableName;
            }
            typeAIDColumn = null;
            typeBIDColumn = null;
            connection = null;
            return null;
        }
        public static ChainTableInfo GetChainTableInfo(Type typeA, Type typeB)
        {
            ChainTableInfo find = null;
            find = ChainTableInfoList.Find(a => a.TypeA == typeA && a.TypeB == typeB);
            if (find.IsNotNull())
                return find;
            find = ChainTableInfoList.Find(a => a.TypeA == typeB && a.TypeB == typeA);
            if (find.IsNotNull())
                return find;
            return null;
        }
        public static MySqlConnection GetChainConnection(Type typeA, Type typeB)
        {
            _ = GetChainTableInfo(typeA, out _, typeB, out _, out MySqlConnection connection);
            return connection;
        }
    }

    /// <summary>
    /// 连接表扩展方法
    /// </summary>
    public static class ChainTableExtensions
    {
        public static string GetChainTableName(this Type type, Type targetType)
        {
            return ChainingInfo.GetChainTableName(type, targetType);
        }
        public static string GetChainIDColumn(this Type type, Type targetType)
        {
            _ = ChainingInfo.GetChainTableInfo(type, out string re, targetType, out _, out _);
            return re;
        }
        public static T[] LoadFromDB<T>(this ChainTableInfo chainTableInfo, int mark = 0, params object[] sourceIDs) where T : IDbObject
        {
            return DataBase.LoadFromDB_Chain<T>(chainTableInfo, mark, sourceIDs);
        }
    }
    /// <summary>
    /// 连接表信息
    /// </summary>
    public class ChainTableInfo
    {
        private string typeA_IDColumn;
        private string typeB_IDColumn;
        private MySqlConnection connection;

        public MySqlConnection Connection { get => connection ?? TypeA.GetMySqlConnection(); set => connection = value; }
        public Type TypeA { get; set; }
        public Type TypeB { get; set; }
        public string ChainTableName { get; set; }
        public string TypeA_IDColumn { get => typeA_IDColumn ?? "id_" + TypeA.Name.ToLower(); set => typeA_IDColumn = value; }
        public string TypeB_IDColumn { get => typeB_IDColumn ?? "id_" + TypeB.Name.ToLower(); set => typeB_IDColumn = value; }

        public override string ToString()
        {
            return string.Format(@"{0}({1}<=>{2})", ChainTableName, TypeA_IDColumn, TypeB_IDColumn);
        }
    }

}