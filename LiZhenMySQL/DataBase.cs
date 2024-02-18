using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using LiZhenStandard.Extensions;
using System.Linq;
using System.Diagnostics;


namespace LiZhenMySQL
{
    public static class DataBase
    {
        private static Dictionary<Type, Dictionary<object, TimeCacheObject>> DBCache_Auto { get; } = new();
        private static void PutCache<T>(object id, T value) where T : IDbObject
        {
            Type type = typeof(T);
            //if (DBCache_Auto.ContainsKey(type))
            //    if (DBCache_Auto[type].ContainsKey(id))
            //        DBCache_Auto[type][id] = new TimeCacheObject(value);
            //    else
            //        DBCache_Auto[type].Add(id, new TimeCacheObject(value));
            //else
            //    DBCache_Auto.Add(type, new Dictionary<object, TimeCacheObject>() { { id, new TimeCacheObject(value) } });

            if (!DBCache.ContainsKey(type))
            {
                var dic = new Dictionary<object, IDbObject>();
                dic.Add(id, value);
                DBCache.Add(type, dic);
            }
            else if (DBCache[type].ContainsKey(id))
            { 
                DBCache[type].Remove(id);
                DBCache[type].Add(id, value);
            }
            else
                DBCache[type].Add(id, value);
        }
        private class TimeCacheObject
        {
            public DateTime Time { get; set; }
            public IDbObject IDbObject { get; set; }
            public TimeCacheObject(IDbObject iDbObject)
            {
                this.Time = DateTime.Now;
                this.IDbObject = iDbObject;
            }

            public static bool CheckTimeStamp(TimeCacheObject cacheObject)
            {
                return (DateTime.Now - cacheObject.Time) < new TimeSpan(0, 0, 3) && (cacheObject.Time - DateTime.Now) > new TimeSpan(0, 0, -3);
            }
        }

        private static Dictionary<Type, MySqlConnection> TypeToConnection { get; } = new Dictionary<Type, MySqlConnection>();
        private static List<MySqlConnection> Connections { get; } = new List<MySqlConnection>();
        private static T[] MakeT<T>(TableReadInfo tableReadInfo, MemberInfo[] allmembers)
        {
            List<T> re = new();
            foreach (ItemReadInfo item in tableReadInfo)
            {
                T obj = MakeT((T)typeof(T).GetConstructor(Type.EmptyTypes).Invoke(null), item, allmembers);
                re.Add(obj);
            }
            return re.ToArray();
        }
        private static T MakeT<T>(T obj, ItemReadInfo itemReadInfo, MemberInfo[] allmembers)
        {
            foreach (MemberInfo member in allmembers)
            {
                var columnName = member.GetSqlColumnName();
                var value = itemReadInfo[columnName];
                member.SetValue(obj, value);
            }
            return obj;
        }
        private static T MakeT<T>(T oldObj,T newObj)
        {
            foreach (MemberInfo ppt in typeof(T).GetSqlProperties())
            {
                ppt.SetValue(oldObj, newObj.GetPropertyValue(ppt.Name));
            }
            return oldObj;
        }
        /// <summary>
        /// 执行非查询类SQL命令语句（注意，执行后不会关闭连接）
        /// </summary>
        /// <param name="commandStr">命令语句</param>
        /// <param name="connection">要执行命令的连接</param>
        /// <returns>受影响的行数</returns>
        public static int CommandNonQuery(string commandStr, MySqlConnection connection)
        {
            Debug.WriteLine(commandStr);
            Console.WriteLine(commandStr);
            MySqlCommand command = new MySqlCommand(commandStr, connection);
            connection.TryOpen();
            int re = command.ExecuteNonQuery();
            //connection.TryClose();
            return re;
        }

        public static MySqlConnection NewConnection(string server, string port, string database, string user, string password)
        {
            string ConnectStr = string.Format(@"server={0};port={1};database={2};user={3};password={4};Charset=utf8;ConnectionTimeout=2;DefaultCommandTimeout=3;", server, port, database, user, password);
            return new MySqlConnection(ConnectStr);
        }
        public static MySqlConnection NewConnection(string serverAndPort, string database, string user, string password)
        {
            serverAndPort.IsMatch(@".+(?=\:)", out string server);
            serverAndPort.IsMatch(@"(?<=\:).+", out string port);
            return NewConnection(server, port, database, user, password);
        }
        public static bool TryOpen(this MySqlConnection sqlConnection)
        {
            return TryOpen(sqlConnection, out _);
        }
        public static bool TryOpen(this MySqlConnection sqlConnection, out string exceptions)
        {
            exceptions = null;
            if (sqlConnection?.State == ConnectionState.Closed)
                try
                {
                    sqlConnection.Open();
                    return true;
                }
                catch (Exception e)
                {
                    exceptions = e.Message;
                    return false;
                }
            else
                return true;
        }
        public static bool TryClose(this MySqlConnection sqlConnection)
        {
            return TryClose(sqlConnection, out _);
        }
        public static bool TryClose(this MySqlConnection sqlConnection, out string exceptions)
        {
            exceptions = null;
            try
            {
                sqlConnection.Close();
                return true;
            }
            catch (Exception e)
            {
                exceptions = e.Message;
                return false;
            }
        }
        public static int CreateConnection(MySqlConnection mySqlConnection) 
        {
            Connections.Add(mySqlConnection);
            return Connections.IndexOf(mySqlConnection);
        }
        public static int CreateConnection(string server, string port, string database, string user, string password)
        {
            return CreateConnection(NewConnection(server,port,database,user,password));
        }
        public static int CreateConnection(string serverAndPort, string database, string user, string password)
        {
            return CreateConnection(NewConnection(serverAndPort, database, user, password));
        }
        public static void RegistSqlTypeConnection(Type type, MySqlConnection mySqlConnection)
        {
            TypeToConnection.Add(type,mySqlConnection);
        }
        public static void ClearAllRegistedSqlTypeConnection()
        {
            TypeToConnection.Clear();
        }
        public static MySqlConnection GetMySqlConnection(this Type type)
        {
            return TypeToConnection.ContainsKey(type) ? TypeToConnection[type] : null;
        }
        public static MySqlConnection GetMySqlConnection(this IDbObject dbObject)
        {
            Type t = dbObject.GetType();
            return GetMySqlConnection(t);
        }
        /// <summary>
        /// 登录验证；
        /// </summary>
        /// <param name="inputUserName">输入的用户名</param>
        /// <param name="inputPassWord">输入的密码</param>
        /// <returns>链接失败返回-1；登录成功返回0；用户不存在返回1；密码错误返回2；找到多名用户返回3；</returns>
        public static int LoginAuthentication<UserType>(string inputUserName, string inputPassWord,out UserType user)where UserType:IDbObject
        {
            user = default;
            Type userType = typeof(UserType);
            string userTableName = userType.GetSqlTableName();
            MySqlConnection connection = userType.GetMySqlConnection();
            LoginUserClassAttribute userAtt = userType.GetLoginUserClassAttribute();
            string userNameColumn = userAtt.UserNameColumn;
            string passwordColumn = userAtt.PasswordColumn;

            //TableReadInfo readInfo;
            UserType[] users;

            try
            {
                users = DataBase.LoadFromDB_Where<UserType>(userNameColumn + "=" + inputUserName.ToDBString());
                //readInfo = DataBase.DBReader(string.Format(@"select {0} from {1} where {2}", @"*", userTableName, userNameColumn + "=" + inputUserName.ToDBString()), connection);
            }
            catch (Exception e)
            {
                throw new LoginfailedException("连接失败，请检查网络连接或IP设置。\n" + e);
            }

            if (users.Count() == 0)
                throw new LoginfailedException("账号不存在。");
            if (users.Count() > 1)
                throw new LoginfailedException("找到多个相同的用户名，请检查数据库用户表信息。");
            user = users[0];
            object value = user.GetPropertyValue("Password");
            if (string.IsNullOrWhiteSpace(value as string))
                if (string.IsNullOrWhiteSpace(inputPassWord))
                    return 0;
                else
                    throw new LoginfailedException("密码错误。");
            else
                if (value.ToString() != inputPassWord)
                throw new LoginfailedException("密码错误。");
            else
                return 0;


            //if (readInfo.Count == 0)
            //    throw new LoginfailedException("账号不存在。");
            //if (readInfo.Count > 1)
            //    throw new LoginfailedException("找到多个相同的用户名，请检查数据库用户表信息。");
            //object value = readInfo[0][passwordColumn];
            //if (string.IsNullOrWhiteSpace(value as string))
            //    if (string.IsNullOrWhiteSpace(inputPassWord))
            //    {

            //        return 0;
            //    }
            //    else
            //        throw new LoginfailedException("密码错误。");
            //else
            //    if (value.ToString() != inputPassWord)
            //    throw new LoginfailedException("密码错误。");
            //else
            //{ 
            //    return 0; 
            //}
        }
        //ConnectionTimeout，connection timeout：连接超时等待时间，默认15s
        //DefaultCommandTimeout，command timeout：MySqlCommand 超时时间，默认 30s

        /// <summary>
        /// 从数据库中直接读取查询信息。
        /// </summary>
        /// <param name="commandStr">要执行的命令语句（只能是查询命令）</param>
        /// <param name="connection">数据库连接</param>
        /// <returns>返回读表信息，然后关闭连接。</returns>
        public static TableReadInfo DBReader(string commandStr, MySqlConnection connection)
        {
            TableReadInfo re = new();
            MySqlCommand command = new(commandStr, connection);
            Debug.WriteLine(commandStr);
            Console.WriteLine(commandStr);
            connection.TryOpen();
            MySqlDataReader dataReader = command.ExecuteReader();
            while (dataReader.Read())
            {
                ItemReadInfo item = new();
                int n = dataReader.FieldCount;
                for (int i = 0; i < n; i++)
                {
                    ColumnReadInfo fieldReadInfo = new()
                    {
                        ColumnName = dataReader.GetName(i),
                        Value = dataReader.GetValue(i)
                    };
                    item.Add(fieldReadInfo);
                }
                re.Add(item);
            }
            connection.TryClose();
            return re;
        }

        /// <summary>
        /// 为减少短时间内重复多次访问数据库，现尝试在读取本机缓存表中获取3秒之内缓存过的数据。
        /// </summary>
        /// <typeparam name="T">要获取的数据类型</typeparam>
        /// <param name="id">要获取的数据ID</param>
        /// <param name="dbObject">要获取的数据对象</param>
        /// <returns>是否找到了3秒之内新读取到的对象</returns>
        public static bool GetCache<T>(object id, out T dbObject) where T : IDbObject
        {
            Type type = typeof(T);
            dbObject = default;
            if (!DBCache_Auto.ContainsKey(type))
                return false;
            if (!DBCache_Auto[type].ContainsKey(id))
                return false;
            var tco = DBCache_Auto[type][id];
            if (!TimeCacheObject.CheckTimeStamp(tco))
                return false;
            else
            {
                dbObject = (T)tco.IDbObject;
                return true;
            }
        }
        /// <summary>
        /// 为减少短时间内重复多次访问数据库，现尝试在读取本机缓存表中获取3秒之内缓存过的数据。
        /// </summary>
        /// <typeparam name="T">要获取的数据类型</typeparam>
        /// <param name="ids">要获取的数据ID</param>
        /// <param name="dbObjects">要获取的数据对象</param>
        /// <returns>是否找到了3秒之内新读取到的对象</returns>
        public static bool GetCache_Array<T>(object[] ids, out T[] dbObjects) where T : IDbObject
        {
            List<T> re = new();
            foreach (var id in ids)
            {
                if (GetCache<T>(id, out T obj))
                    re.Add(obj);
                else
                {
                    dbObjects = null;
                    return false;
                }
            }
            dbObjects = re.ToArray();
            return true;
        }

        /// <summary>
        /// 直接根据SQL命令语句查询对象（注意：比较容易出错哦！）
        /// </summary>
        /// <typeparam name="T">要查询的对象类型</typeparam>
        /// <param name="command">SQL命令语句（必须是查询命令哦！）</param>
        /// <param name="args">SQL命令语句字符串格式参数</param>
        /// <returns>查询到的对象数组</returns>
        public static T[] LoadFromDB_Command<T>(string command, params object[] args) where T : IDbObject
        {
            MySqlConnection con = typeof(T).GetMySqlConnection();
            string commandStr = string.Format(command, args);
            TableReadInfo allItems = DBReader(commandStr, con);
            T[] re = MakeT<T>(allItems);
            for (int i = 0; i < re.Length; i++)
            {
                T item = re[i];
                PutCache(item.ID,item);
            }
            return re;
        }
        /// <summary>
        /// 根据Where条件查找对象。
        /// </summary>
        /// <typeparam name="T">要查询的对象类型</typeparam>
        /// <param name="where">Where条件字符串</param>
        /// <param name="args">字符串格式参数</param>
        /// <returns>查询到的对象数组</returns>
        public static T[] LoadFromDB_Where<T>(string where = null, params object[] args) where T : IDbObject
        {
            string whereStr;
            string tableName = typeof(T).GetSqlTableName();
            if (string.IsNullOrWhiteSpace(where))
                whereStr = "";
            else
                whereStr = string.Format(" where " + where, args);
            return LoadFromDB_Command<T>("select * from {0}{1}", tableName, whereStr);
        }
        /// <summary>
        /// 根据连接表和源对象ID查找目标类型的对象。
        /// </summary>
        /// <typeparam name="T">要查询的目标对象类型</typeparam>
        /// <param name="chainTableInfo">连接表信息</param>
        /// <param name="sourceIDs">源对象ID（组）</param>
        /// <returns>查询到的目标对象数组</returns>
        public static T[] LoadFromDB_Chain<T>(ChainTableInfo chainTableInfo,int mark = 0, params object[] sourceIDs) where T : IDbObject
        {
            //if (GetCache_Array(sourceIDs, out T[] objs))
            //    return objs;

            if (chainTableInfo is null)
                return Array.Empty<T>();

            string sourceIDColumn;
            string targetIDColumn;
            if (chainTableInfo.TypeA == typeof(T))
            {
                sourceIDColumn = chainTableInfo.TypeB_IDColumn;
                targetIDColumn = chainTableInfo.TypeA_IDColumn;
            }
            else if (chainTableInfo.TypeB == typeof(T))
            {
                sourceIDColumn = chainTableInfo.TypeA_IDColumn;
                targetIDColumn = chainTableInfo.TypeB_IDColumn;
            }
            else 
                return null;

            string sourceIDWhereStr = string.Format("{0} in ", sourceIDColumn);
            List<string> ids = new();
            for (int i = 0; i < sourceIDs.Length; i++)
            {
                object id = sourceIDs[i];
                ids.Add(id.ToDBString());
            }
            sourceIDWhereStr += string.Format("({0})",ids.AllToString());

            string markStr = string.Empty;
            if (mark != 0)
            {
                markStr = $@"and mark={mark}";
            }

            string cmdStr = string.Format(@"select {0} from {1} where id in (select {2} from {3} where {4} {5})",
                 @"*",
                typeof(T).GetSqlTableName(),
                targetIDColumn,
                chainTableInfo.ChainTableName,
                sourceIDWhereStr,
                markStr);

            return LoadFromDB_Command<T>(cmdStr, typeof(T).GetMySqlConnection());
        }
        /// <summary>
        /// 根据源对象查找目标对象。
        /// </summary>
        /// <typeparam name="T">要查询的目标对象类型</typeparam>
        /// <param name="sourceObj">源对象</param>
        /// <param name="sourceIDColumn">源对象ID在目标对象数据表中的列名</param>
        /// <returns>查询到的目标对象</returns>
        public static T[] LoadFromDB_SourceObj<T>(IDbObject sourceObj,string sourceIDColumn = null) where T : IDbObject
        {
            if (sourceObj is null)
                return Array.Empty<T>();
            string sourceIDColumnName = sourceIDColumn ?? "id_" + sourceObj.GetType().Name.ToLower();
            return LoadFromDB_Where<T>(@"{0} = {1}", sourceIDColumnName, sourceObj.ID.ToDBString());
        }
        /// <summary>
        /// 根据多个对象，在一个表中查找与这几个对象同时关联的多个对象。
        /// </summary>
        /// <typeparam name="T">要查询的目标类型</typeparam>
        /// <param name="sourceObjs">根据哪些对象查询</param>
        /// <returns>查询到的目标对象集合</returns>
        public static T[] LoadFromDB_SourceObjs<T>(IEnumerable<IDbObject> sourceObjs) where T : IDbObject
        {
            if (sourceObjs is null)
                return Array.Empty<T>();
            bool same = true;
            Type lastType = null;
            foreach (var item in sourceObjs)
            {
                if (item is null)
                    continue;
                Type t = item.GetType();
                if (lastType is null)
                    lastType = t;
                else if (lastType != t)
                { same = false; break; }
            }
            if (same)
            {
                List<string> whereStrs = new();
                var idColumn = "id";
                foreach (var obj in sourceObjs)
                {
                    if (obj is null)
                        continue;
                    var id = obj.ID.ToDBString();
                    idColumn = "id_" + obj.GetType().Name.ToLower();
                    whereStrs.Add(id);
                }
                var whereStr = string.Format("{0} in ({1})", idColumn, whereStrs.AllToString(separator: ","));
                return LoadFromDB_Where<T>(whereStr);
            }
            else
            {
                List<string> whereStrs = new();
                foreach (var obj in sourceObjs)
                {
                    if (obj is null)
                        continue;
                    Type t = obj.GetType();
                    var id = obj.ID.ToDBString();
                    var idColumn = "id_" + t.Name.ToLower();
                    whereStrs.Add(string.Format("{0}={1}", idColumn, id));
                }
                var whereStr = whereStrs.AllToString(separator: " and ");
                return LoadFromDB_Where<T>(whereStr);
            }
        }
        /// <summary>
        /// 根据目标ID查询一个目标对象。
        /// </summary>
        /// <typeparam name="T">要查询的目标对象类型</typeparam>
        /// <param name="targetID">目标对象的ID</param>
        /// <param name="targetIDColumn">目标对象ID列名</param>
        /// <returns>查询到的目标对象</returns>
        public static T LoadOneFromDB<T>(object targetID, string targetIDColumn = null) where T : IDbObject
        {
            if (GetCache(targetID, out T obj))
                return obj;

            string targetIDColumnStr = targetIDColumn ?? "id";
            if (targetID is null || targetID is DBNull)
                return default;
            else
                return LoadFromDB_Where<T>("{0} = {1}", targetIDColumnStr, targetID).FirstOrDefault();
        }
        /// <summary>
        /// 以这个对象为ID查询单个目标对象。
        /// </summary>
        /// <typeparam name="T">要查询的目标对象类型</typeparam>
        /// <param name="id">目标对象的ID</param>
        /// <param name="idColumn">目标对象ID列名</param>
        /// <returns>查询到的目标对象</returns>
        public static T LoadFromDB_ThisID<T>(this object id, string idColumn = null) where T : IDbObject
        {
            return LoadOneFromDB<T>(id, idColumn);
        }

        /// <summary>
        /// 从数据库重新加载这个对象。
        /// </summary>
        /// <typeparam name="T">目标对象类型</typeparam>
        /// <param name="dbObject">目标对象</param>
        /// <param name="targetIDColumn">目标对象ID列名</param>
        /// <returns>重新加载的新对象</returns>
        public static void ReloadFromDB<T>(this T dbObject, string targetIDColumn = null) where T : IDbObject
        {
            var wherestr = dbObject.GetSqlPKColumnValueStr();
            var newobjs = DataBase.LoadFromDB_Where<T>(where: wherestr);
            if(newobjs.Any())
                MakeT(dbObject, newobjs.First());
        }

        /// <summary>
        /// 向关联表中插入两个对象的关联关系（注意，该方法仅插入关联，并不插入对象本身）；
        /// </summary>
        /// <param name="objA">对象A</param>
        /// <param name="objB">对象B</param>
        /// <param name="doNotClose">插入之后不要关闭连接</param>
        /// <returns>插入的行数</returns>
        public static int InsertToDB_Chain(IDbObject objA, IDbObject objB,int mark = 0, bool doNotClose = false)
        {
            Type ta = objA.GetType();
            Type tb = objB.GetType();
            string chainTableName = ChainingInfo.GetChainTableInfo(ta, out string taIDColumn, tb, out string tbIDColumn, out MySqlConnection connection);
            int re = CommandNonQuery(string.Format(@"insert into {0} ({1},{2}{5}) values ({3},{4}{6})",
                chainTableName,
                taIDColumn,
                tbIDColumn,
                objA.ID.ToDBString(),
                objB.ID.ToDBString(),
                mark != 0 ? ",mark" : string.Empty,
                mark != 0 ? "," + mark.ToString(): string.Empty
                ), connection); ;
            if (!doNotClose)
                _ = connection.TryClose();
            return re;
        }
        /// <summary>
        /// 向关联表中插入与这个对象关联的多个对象的关联关系（注意，该方法仅插入关联，并不插入对象本身）
        /// </summary>
        /// <param name="objA">对象A</param>
        /// <param name="objBs">与对象A关联的对象集合</param>
        /// <returns>插入的行数</returns>
        public static int InsertToDB_Chains<T>(IDbObject objA, IEnumerable<T> objBs, int mark = 0) where T : IDbObject
        {
            int re = 0;
            MySqlConnection con = null;
            foreach (var objB in objBs)
            {
                re += InsertToDB_Chain(objA, objB, mark, true);
                if (con.IsNotNull())
                    con = ChainingInfo.GetChainConnection(objA.GetType(), objB.GetType());
            }
            _ = con.TryClose();
            return re;
        }
        /// <summary>
        /// 扩展方法：向关联表中插入与这个对象相关联的另一个对象的关联关系（注意，该方法仅插入关联，并不插入对象本身）
        /// </summary>
        /// <param name="sourceObj">该对象</param>
        /// <param name="targetObj">关联对象</param>
        /// <returns>插入的行数</returns>
        public static int InsertThisToDB_Chain(this IDbObject sourceObj,IDbObject targetObj,int mark = 0)
        {
            return InsertToDB_Chain(sourceObj, targetObj,mark);
        }
        /// <summary>
        /// 扩展方法：向关联表中插入与这个对象相关联的对象集合的关联关系（注意，该方法仅插入关联，并不插入对象本身）
        /// </summary>
        /// <param name="sourceObj">该对象</param>
        /// <param name="targetObjs">关联对象集合</param>
        /// <returns>插入的行数</returns>
        public static int InsertThisToDB_Chains<T>(this IDbObject sourceObj,IEnumerable<T> targetObjs) where T:IDbObject
        {
            return InsertToDB_Chains(sourceObj,targetObjs);
        }
        /// <summary>
        /// 将一个对象插入数据库
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="obj">要插入的对象</param>
        /// <returns>插入后为该对象分配的ID</returns>
        public static object InsertToDB<T>(T obj) where T : IDbObject
        {
            string str = obj.GetInsertStr();
            MySqlConnection con = typeof(T).GetMySqlConnection();
            _ = CommandNonQuery(str, con);
            ItemReadInfo read = DBReader("select last_insert_id()", con).FirstOrDefault();
            object reID = read[0].Value;
            obj.GetSqlPKProperties().FirstOrDefault()?.SetValue(obj, reID);
            con.TryClose();
            return reID;
        }
        /// <summary>
        /// 将一个对象集合插入数据库
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="objs">要插入的对象集合</param>
        /// <returns>插入的行数</returns>
        public static int InsertToDB<T>(IEnumerable<T> objs) where T : IDbObject
        {
            string str = ((IEnumerable<IDbObject>)objs).GetInsertStr();
            MySqlConnection con = typeof(T).GetMySqlConnection();
            int re = CommandNonQuery(str, con);
            con.TryClose();
            return re;
        }
        /// <summary>
        /// 扩展方法：将该对象插入数据库
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="obj">该对象</param>
        /// <returns>插入后为该对象分配的ID</returns>
        public static object InsertThisToDB<T>(this T obj) where T : IDbObject
        {
            return InsertToDB(obj);
        }

        /// <summary>
        /// 尝试修改一个对象在数据库中的信息，如果未找到该对象的主键，则插入该对象。
        /// </summary>
        /// <typeparam name="T">要修改的对象类型</typeparam>
        /// <param name="obj">要修改的对象</param>
        /// <returns>修改或插入的对象ID</returns>
        public static object ReplaceToDB<T>(T dbObject) where T : IDbObject
        {
            MySqlConnection con = typeof(T).GetMySqlConnection();
            con.TryOpen();
            string mysqlInsert = dbObject.GetReplaceStr();
            int re = CommandNonQuery(mysqlInsert, con);
            con.TryClose();
            return re;
        }
        /// <summary>
        /// 尝试修改这个对象在数据库中的信息，如果未找到该对象的主键，则插入该对象。
        /// </summary>
        /// <typeparam name="T">要修改的对象类型</typeparam>
        /// <param name="obj">要修改的对象</param>
        /// <returns>修改或插入的对象ID</returns>
        public static object ReplaceThisToDB<T>(this T obj) where T : IDbObject
        {
            return ReplaceToDB<T>(obj);
        }

        /// <summary>
        /// 修改一个对象在数据库中的信息
        /// </summary>
        /// <typeparam name="T">要修改的对象类型</typeparam>
        /// <param name="dbObject">要修改的对象</param>
        /// <returns>受影响的行数</returns>
        public static int ModifyToDB<T>(T dbObject) where T:IDbObject
        {
            MySqlConnection con = typeof(T).GetMySqlConnection();
            con.TryOpen();

            // 创建要执行修改的MySQL语句
            string mysqlInsert = dbObject.GetModifyStr();

            // 创建用于实现MySQL语句的对象
            //MySqlCommand mySqlCommand = new (mysqlInsert, con);

            // 执行MySQL语句进行插入
            //int re =  mySqlCommand.ExecuteNonQuery();

            int re = CommandNonQuery(mysqlInsert, con);

            con.TryClose();
            return re;
        }
        /// <summary>
        /// 修改该对象在数据库中的信息
        /// </summary>
        /// <typeparam name="T">要修改的对象类型</typeparam>
        /// <param name="dbObject">要修改的对象</param>
        /// <returns>受影响的行数</returns>
        public static int ModifyThisToDB<T>(this T dbObject) where T : IDbObject
        {
            return ModifyToDB(dbObject);
        }
        /// <summary>
        /// 修改这个对象在数据库中与另一个对象的连接关系的标记（Mark）值;
        /// </summary>
        /// <typeparam name="T">要修改的对象类型</typeparam>
        /// <param name="sourceObject">源对象</param>
        /// <param name="targetObject">目标对象</param>
        /// <param name="mark">标记值</param>
        /// <returns>成功修改的行数</returns>
        public static int ModifyChainMark<T>(this T sourceObject,IDbObject targetObject,int mark) where T : IDbObject
        {
            string chainTableName = ChainingInfo.GetChainTableInfo(sourceObject.GetType(), out string typeAIDColumn, targetObject.GetType(), out string typeBIDColumn, out MySqlConnection connection);
            connection.TryOpen();
            var ppt = typeof(IDbObject).GetProperty("ID");
            string sqlCom = string.Format("update {0} set mark={1} where {2}={3} and {4}={5}",chainTableName, mark, typeAIDColumn,sourceObject.ID.ToDBString(),typeBIDColumn,targetObject.ID.ToDBString());
            int re = CommandNonQuery(sqlCom, connection);
            connection.TryClose();
            return re;
        }

        /// <summary>
        /// 从数据库中删除一些信息
        /// </summary>
        /// <param name="connection">数据库连接</param>
        /// <param name="tableName">要删除的信息所在表名</param>
        /// <param name="wheres">条件集合</param>
        /// <returns>删除的行数</returns>
        public static int DeleteFromDB(MySqlConnection connection, string tableName, params string[] wheres)
        {
            int re = 0;
            try
            {
                connection.TryOpen();

                // 创建要执行删除的MySQL语句
                string deleteStr = string.Format(@"delete from {0} where {1}", tableName, wheres.AllToString(separator: " and "));

                // 创建用于实现MySQL语句的对象
                //MySqlCommand mySqlCommand = new(deleteStr, connection);

                // 执行MySQL语句进行插入
                //re = mySqlCommand.ExecuteNonQuery();
                re = CommandNonQuery(deleteStr, connection);
                connection.TryClose();
            }
            catch { }
            return re;
        }
        /// <summary>
        /// 从数据库中删除一个对象
        /// </summary>
        /// <typeparam name="T">要删除的对象类型</typeparam>
        /// <param name="dbObject">要删除的对象</param>
        /// <returns>删除的行数</returns>
        public static int DeleteFromDB<T>(T dbObject) where T:IDbObject
        {
            return DeleteFromDB(dbObject.GetMySqlConnection(),dbObject.GetSqlTableName(), string.Format("id={0}", dbObject.ID.ToDBString()));
        }
        /// <summary>
        /// 从数据库中删除该对象
        /// </summary>
        /// <param name="dbObject">要删除的对象</param>
        /// <returns>删除的对象数</returns>
        public static int DeleteThisFromDB(this IDbObject dbObject)
        {
            return DeleteFromDB(dbObject);
        }
        /// <summary>
        /// 从数据库中删除两个对象的连接关系（注意：该方法仅删除连接关系，并不删除对象本身）
        /// </summary>
        /// <param name="objA">对象A</param>
        /// <param name="objB">对象B</param>
        /// <returns>删除的行数</returns>
        public static int DeleteFromDB_Chain(IDbObject objA,IDbObject objB)
        {
            string chainTableName = ChainingInfo.GetChainTableInfo(objA.GetType(),out string typeAIDColumn,objB.GetType(),out string typeBIDColumn,out MySqlConnection connection);
            return DeleteFromDB(connection, chainTableName, string.Format(@"{0}={1} and {2}={3}", typeAIDColumn, objA.ID.ToDBString(), typeBIDColumn, objB.ID.ToDBString()));
        }
        /// <summary>
        /// 从数据库中删除一个对象和另一个对象集合的连接关系（注意：该方法仅删除连接关系，并不删除对象本身）
        /// </summary>
        /// <param name="objA">对象A</param>
        /// <param name="objBs">对象B集合</param>
        /// <returns>删除的行数</returns>
        public static int DeleteFromDB_Chains<T>(IDbObject objA, IEnumerable<T> objBs)where T:IDbObject
        {
            int re = 0;
            foreach (var b in objBs)
            {
                re += DeleteFromDB_Chain(objA,b);
            }
            return re;
        }

        /// <summary>
        /// 从数据库中获取所有与提供的ID有关联的ID。
        /// </summary>
        /// <typeparam name="SourceType">源类型</typeparam>
        /// <typeparam name="TargetType">目标类型</typeparam>
        /// <param name="sourceID">源对象ID</param>
        /// <returns>有关联的ID</returns>
        public static object[] GetAllChainingID<SourceType, TargetType>(object sourceID) where SourceType : IDbObject where TargetType : IDbObject
        {
            Type ta = typeof(SourceType);
            Type tb = typeof(TargetType);
            string tableName = ChainingInfo.GetChainTableInfo(ta, out var aidc, tb, out var bidc, out var con);
            TableReadInfo readed = DBReader($@"select {aidc} from {tableName} where {aidc}={sourceID.ToDBString()}", con);
            List<object> re = new();
            foreach (var item in readed)
            {
                re.Add(item["ID"]);
            }
            return re.ToArray();
        }
        /// <summary>
        /// 判断两个不同类型的ID是否有关联
        /// </summary>
        /// <typeparam name="SourceType">源对象类型</typeparam>
        /// <typeparam name="TargetType">目标对象类型</typeparam>
        /// <param name="sourceID">源对象ID</param>
        /// <param name="targetID">目标对象ID</param>
        /// <returns>是否有关联</returns>
        public static bool IsChaining<SourceType, TargetType>(object sourceID, object targetID) where SourceType : IDbObject where TargetType : IDbObject
        {
            Type ta = typeof(SourceType);
            Type tb = typeof(TargetType);
            string tableName = ChainingInfo.GetChainTableInfo(ta, out var aidc, tb, out var bidc, out var con);
            TableReadInfo readed = DBReader($@"select {aidc} from {tableName} where {aidc}={sourceID.ToDBString()} and {bidc}={targetID.ToDBString()}", con);
            return readed.Any();
        }
        /// <summary>
        /// 判断两个不同类型的ID是否有关联，必须带有关联标记。
        /// </summary>
        /// <typeparam name="SourceType">源对象类型</typeparam>
        /// <typeparam name="TargetType">目标对象类型</typeparam>
        /// <param name="sourceID">源对象ID</param>
        /// <param name="targetID">目标对象ID</param>
        /// <param name="mark"></param>
        /// <returns>是否有关联</returns>
        public static bool IsChaining_WithMark<SourceType, TargetType>(object sourceID, object targetID, out int mark) where SourceType : IDbObject where TargetType : IDbObject
        {
            mark = 0;
            Type ta = typeof(SourceType);
            Type tb = typeof(TargetType);
            string tableName = ChainingInfo.GetChainTableInfo(ta, out var aidc, tb, out var bidc, out var con);
            TableReadInfo readed = DBReader($@"select mark from {tableName} where {aidc}={sourceID.ToDBString()} and {bidc}={targetID.ToDBString()}", con);
            var re = readed.Any();
            if (re)
                mark = (int)readed[0]["mark"];
            return re;
        }
        /// <summary>
        /// 判断两个对象是否有关联
        /// </summary>
        /// <param name="sourceObj">对象A</param>
        /// <param name="targetObj">对象B</param>
        /// <returns>是否有关联</returns>
        public static bool IsChaining(this IDbObject sourceObj, IDbObject targetObj)
        {
            Type ta = sourceObj.GetType();
            Type tb = targetObj.GetType();
            string tableName = ChainingInfo.GetChainTableInfo(ta, out var aidc, tb, out var bidc, out var con);
            TableReadInfo readed = DBReader($@"select {aidc} from {tableName} where {aidc}={sourceObj.ID.ToDBString()} and {bidc}={targetObj.ID.ToDBString()}", con);
            return readed.Any();
        }
        /// <summary>
        /// 判断两个对象是否有关联，必须带有关联标记。
        /// </summary>
        /// <param name="sourceObj">对象A</param>
        /// <param name="targetObj">对象B</param>
        /// <param name="mark">返回的关联标记</param>
        /// <returns>是否有关联</returns>
        public static bool IsChaining_WithMark(this IDbObject sourceObj, IDbObject targetObj,out int mark)
        {
            mark = 0;
            Type ta = sourceObj.GetType();
            Type tb = targetObj.GetType();
            string tableName = ChainingInfo.GetChainTableInfo(ta, out var aidc, tb, out var bidc, out var con);
            TableReadInfo readed = DBReader($@"select mark from {tableName} where {aidc}={sourceObj.ID.ToDBString()} and {bidc}={targetObj.ID.ToDBString()}", con);
            var re = readed.Any();
            if (re)
                mark = Convert.ToInt32(readed[0]["mark"]);
            return re;
        }
        /// <summary>
        /// 在这一组对象中找出所有与目标对象关联的，并且关联标记为mark的对象。
        /// </summary>
        /// <typeparam name="T">源对象组的元素类型</typeparam>
        /// <param name="sourceObjs">源对象组</param>
        /// <param name="targetObj">目标对象</param>
        /// <param name="mark">关联标记值，默认为0</param>
        /// <returns>有关联的对象集合</returns>
        public static IEnumerable<T> CheckChaining<T>(this IEnumerable<T> sourceObjs,IDbObject targetObj,int mark = 0) where T:IDbObject
        {
            Type ta = sourceObjs.GetType().GetElementType();
            Type tb = targetObj.GetType();
            string tableName = ChainingInfo.GetChainTableInfo(ta, out var aidc, tb, out var bidc, out var con);
            string sourceIDs = (from item in sourceObjs select item.ID).AllToString();
            string slt = mark == 0 ? aidc : aidc + ",mark";
            TableReadInfo readed = DBReader($@"select {slt} from {tableName} where {aidc} in ({sourceIDs}) and {bidc}={targetObj.ID.ToDBString()}", con);
            List<T> re = new();
            foreach (ItemReadInfo item in readed)
            {
                if (mark != 0)
                    if (!mark.Equals(Convert.ToInt32(item["mark"])))
                        continue;
                object findid = item[aidc];
                IEnumerable<T> findObjs = from obj in sourceObjs where obj.ID.Equals(findid) select obj;
                if (findObjs.Any())
                    re.Add(findObjs.ElementAt(0));
            }
            return re;
        }

        /// <summary>
        /// 验证一个对象在数据库中是否有重复项
        /// </summary>
        /// <param name="obj">要验证的对象</param>
        /// <returns>在数据库中是否有重复项</returns>
        public static bool RepeatedVerification(IDbObject obj)
        {
            for (int i = 1; i < 10; i++)
            {
                PropertyInfo[] ckppts = obj.GetSqlCKProperties(i);
                if (ckppts.Length < 1)
                    break;
                string columnNames = (from ckppt in ckppts select ckppt.GetSqlColumnName()).AllToString();
                string conditions = (from ckppt in ckppts select ckppt.GetSqlColumnValueStr(obj)).AllToString(separator: " and ");
                TableReadInfo readInfo = DBReader(string.Format(@"select {0} from {1} where {2}", columnNames, obj.GetSqlTableName(), conditions),obj.GetMySqlConnection());
                if (readInfo.Count > 0)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 获取一个表的注释
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="connection">连接</param>
        /// <returns>注释</returns>
        public static string GetTableComment(string tableName,MySqlConnection connection)
        {
            MySqlCommand con = new MySqlCommand(string.Format("show table status like {0}", tableName.ToDBString()), connection);
            string re = string.Empty;
            connection.TryOpen();
            MySqlDataReader reader = con.ExecuteReader();
            while (reader.Read())
            {
                re = reader["Comment"].ToString();
            }
            connection.TryClose();
            return re;
        }

        /// <summary>
        /// 根据由读取数据库得到的TableReadInfo，生成特定类型的对象。
        /// </summary>
        /// <typeparam name="T">对象类型（必须是继承IDBObject接口的类型）</typeparam>
        /// <param name="tableReadInfo">数据库读表信息</param>
        /// <returns>生成的对象集合</returns>
        public static T[] MakeT<T>(TableReadInfo tableReadInfo)
        {
            return MakeT<T>(tableReadInfo, typeof(T).GetSqlMembers());
        }

        private static Dictionary<Type, Dictionary<object,IDbObject>> DBCache { get; } = new();
        public static List<T> GetCacheList<T>()
        {
            if (!DBCache.ContainsKey(typeof(T)))
                DBCache.Add(typeof(T), new Dictionary<object, IDbObject>());
            IDbObject[] arr = DBCache[typeof(T)].Values.ToArray();
            return (from a in arr select (T)a).ToList();
        }
        public static T GetDbObjectFromCache<T>(this object id) where T : IDbObject
        {
            Type type = typeof(T);
            if (DBCache.ContainsKey(type))
                if (DBCache[type].ContainsKey(id))
                    return (T)DBCache[type]?[id];
            return id.LoadFromDB_ThisID<T>();
        }
        public static T[] GetDbObjectFromCache<T>(string sqlPropertyName,object value) where T : IDbObject
        {
            Type type = typeof(T);
            if (DBCache.ContainsKey(type))
                if (DBCache[type].Values.Any())
                {
                    Dictionary<object, IDbObject>.ValueCollection values = DBCache[type].Values;
                    IEnumerable<T> allTvalues = from v in values select (T)v;
                    var reall = allTvalues.FindAllByProperty(sqlPropertyName, value);
                    if (reall.Any())
                        return reall.ToArray();
                }

            var ppt = typeof(T).GetProperty(sqlPropertyName);
            string columnName = ppt.GetSqlColumnName();
            return LoadFromDB_Where<T>($"{columnName} = {value.ToDBString()}").ToArray();
        }

        public static void ClearDBCache<T>()
        {
            Type type = typeof(T);
            if (DBCache.ContainsKey(type))
                DBCache[typeof(T)]?.Clear();
        }
        public static void ClearDBCache_All()
        {
            DBCache.Clear();
        }

    }
    /// <summary>
    /// 读表信息
    /// </summary>
    [Serializable]
    public class TableReadInfo : List<ItemReadInfo>
    {
        public object this[int index, string columnName] => (index >= Count || index < 0) ? null : this[index].Find(a => { return a.ColumnName == columnName; }).Value;
    }
    /// <summary>
    /// 行对象信息
    /// </summary>
    [Serializable]
    public class ItemReadInfo : List<ColumnReadInfo>
    {
        public object this[string columnName] => Find(a => { return a.ColumnName == columnName; }).Value;
    }
    /// <summary>
    /// 列属性信息
    /// </summary>
    [Serializable]
    public struct ColumnReadInfo
    {
        public string ColumnName { get; set; }
        public object Value { get; set; }
    }

    /// <summary>
    /// 登录失败错误;
    /// </summary>
    [Serializable]
    public class LoginfailedException : Exception
    {
        public LoginfailedException() { }
        public LoginfailedException(string message) : base(message) { }
        public LoginfailedException(string message, Exception inner) : base(message, inner) { }
        protected LoginfailedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}