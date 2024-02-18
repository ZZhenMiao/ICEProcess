using LiZhenStandard.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace LiZhenMySQL
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public sealed class SqlObjectAttribute : Attribute
    {
        //private string databaseName;
        //public static string UniversalDatabase { get; set; }
        //public string DatabaseName { get => databaseName.IsNullOrEmpty() ? MainDataBase.DataBaseName : databaseName; set => databaseName = value; }
        public string TableName { get; set; }
        //public SqlObjectAttribute(string tableName, string dataBase) : this(tableName)
        //{
        //    DatabaseName = dataBase;
        //}
        public SqlObjectAttribute(string tableName)
        {
            TableName = tableName;
        }
        public SqlObjectAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class SqlPropertyAttribute : Attribute
    {
        private bool isPrimaryKey;
        private int writeNull = -1;

        public string ColumnName { get; set; }
        public bool IsPrimaryKey { get => CompositeKeyPart == 0 && isPrimaryKey; set => isPrimaryKey = value; }
        public bool WriteNull { get => (writeNull == -1 && IsPrimaryKey) || writeNull > 0; set => writeNull = value ? 1 : 0; }
        public int CompositeKeyPart { get; set; }
        public bool IsReadOnly { get; set; }
        public SqlPropertyAttribute() : this(null, false, false, 0) { }
        public SqlPropertyAttribute(int compositeKeyPart) : this(null, false, false, compositeKeyPart) { }
        public SqlPropertyAttribute(bool isPrimaryKey = false) : this(null, false, isPrimaryKey) { }
        public SqlPropertyAttribute(string columnName, bool onlyRead = false, bool isPrimaryKey = false, int compositeKeyPart = 0)
        {
            ColumnName = columnName;
            IsReadOnly = onlyRead;
            IsPrimaryKey = isPrimaryKey;
            CompositeKeyPart = compositeKeyPart;
        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class LoginUserClassAttribute : Attribute
    {
        private string userNameColumn;
        private string passwordColumn;

        public string UserNameColumn { get => userNameColumn ?? @"name"; set => userNameColumn = value; }
        public string PasswordColumn { get => passwordColumn ?? @"password"; set => passwordColumn = value; }

        public LoginUserClassAttribute(string userNameColumn,string passwordColumn)
        {
            this.UserNameColumn = userNameColumn;
            this.PasswordColumn = passwordColumn;
        }
    }

    public static class SqlAttribute
    {
        private static SqlObjectAttribute GetSqlObjectAttribute(this Type type)
        {
            return (SqlObjectAttribute)type.GetCustomAttributes(typeof(SqlObjectAttribute), true)?.First();
        }
        private static SqlObjectAttribute GetSqlObjectAttribute(this IDbObject obj)
        {
            return obj?.GetType().GetSqlObjectAttribute();
        }


        private static SqlPropertyAttribute GetSqlPropertyAttribute(this PropertyInfo propertyInfo)
        {
            return (SqlPropertyAttribute)propertyInfo?.GetCustomAttributes(typeof(SqlPropertyAttribute), true)?.First();
        }

        public static LoginUserClassAttribute GetLoginUserClassAttribute(this Type type)
        {
            return (LoginUserClassAttribute)type.GetCustomAttributes(typeof(LoginUserClassAttribute), true)?.First();
        }

        public static string GetSqlTableName(this IDbObject obj)
        {
            return GetSqlTableName(obj.GetType());
        }
        public static string GetSqlTableName(this Type type)
        {
            return type.GetSqlObjectAttribute().TableName ?? type.Name.ToLower();
        }

        public static MemberInfo[] GetSqlMembers(this IDbObject obj,bool forInsert = false)
        {
            return GetSqlMembers(obj.GetType(), forInsert);
        }
        public static MemberInfo[] GetSqlMembers(this Type type,bool forInsert = false)
        {
            MemberInfo[] allmembers = type.GetProperties();
            IEnumerable<MemberInfo> members;
            if (!forInsert)
                members = allmembers.Where(a => { return a.GetCustomAttributes(typeof(SqlPropertyAttribute), true).Count() > 0; });
            else
            {
                members = allmembers.Where(a => 
                {
                    if (a.GetCustomAttributes(typeof(SqlPropertyAttribute), true).Count() > 0)
                        if (a is PropertyInfo ppt)
                            return !ppt.GetSqlPropertyAttribute().WriteNull;
                        else
                            return true;
                    else return false;
                });
            }
            return members.ToArray();
        }

        public static PropertyInfo[] GetSqlProperties(this IDbObject obj)
        {
            return GetSqlProperties(obj.GetType());
        }
        public static PropertyInfo[] GetSqlProperties(this Type type)
        {
            PropertyInfo[] allproperties = type.GetProperties();
            IEnumerable<PropertyInfo> properties = allproperties.Where(a => { return a.GetCustomAttributes(typeof(SqlPropertyAttribute), true).Any(); });
            return properties.ToArray();
        }

        public static FieldInfo[] GetSqlFields(this IDbObject obj)
        {
            return GetSqlFields(obj.GetType());
        }
        public static FieldInfo[] GetSqlFields(this Type type)
        {
            FieldInfo[] allFields = type.GetFields();
            IEnumerable<FieldInfo> fields = allFields.Where(a => { return a.GetCustomAttributes(typeof(SqlPropertyAttribute), true).Any(); });
            return fields.ToArray();
        }

        public static PropertyInfo[] GetSqlPKProperties(this Type type)
        {
            PropertyInfo[] ppts = type.GetSqlProperties();
            IEnumerable<PropertyInfo> pks = ppts.Where(a => { return a.GetSqlPropertyAttribute().IsPrimaryKey; });
            return pks.ToArray();
        }
        public static PropertyInfo[] GetSqlPKProperties(this IDbObject obj)
        {
            return GetSqlPKProperties(obj.GetType());
        }

        public static PropertyInfo[] GetSqlCKProperties(this Type type,int compositeKeyPart)
        {
            PropertyInfo[] ppts = type.GetSqlProperties();
            IEnumerable<PropertyInfo> pks = ppts.Where(a => { return a.GetSqlPropertyAttribute().CompositeKeyPart == compositeKeyPart; });
            return pks.ToArray();
        }
        public static PropertyInfo[] GetSqlCKProperties(this IDbObject obj, int compositeKeyPart)
        {
            return GetSqlCKProperties(obj.GetType(), compositeKeyPart);
        }

        public static string GetSqlPKColumn(this Type type)
        {
            PropertyInfo ppt = GetSqlPKProperties(type).First();
            return ppt.GetSqlColumnName();
        }
        public static string GetSqlPKColumn(this IDbObject obj)
        {
            return GetSqlPKColumn(obj.GetType());
        }

        public static string[] GetSqlPKColumns(this Type type)
        {
            PropertyInfo[] ppts = GetSqlPKProperties(type);
            return (from re in ppts select re.GetSqlColumnName()).ToArray();
        }
        public static string[] GetSqlPKColumns(this IDbObject obj)
        {
            return GetSqlPKColumns(obj.GetType());
        }

        public static string GetSqlColumnName(this MemberInfo memberInfo)
        {
            switch (memberInfo)
            {
                case PropertyInfo pptInfo:
                    return pptInfo.GetSqlPropertyAttribute().ColumnName ?? pptInfo.Name.ToLower();
                default:
                    return memberInfo.Name.ToLower();
            }
        }
        public static string GetSqlColumnValueStr(this MemberInfo memberInfo, IDbObject obj)
        {
            string name = memberInfo.GetSqlColumnName();
            string value = string.Empty;
            if (memberInfo is PropertyInfo info)
                value = info.GetValue(obj).ToDBString();
            if (memberInfo is FieldInfo info1)
                value = info1.GetValue(obj).ToDBString();
            return string.Format(@"{0}={1}", name, value);
        }
        public static string GetSqlPKColumnValueStr(this IDbObject obj)
        {
            var pkppts = obj.GetSqlPKProperties();
            IEnumerable<object> pkcs = from pkppt in pkppts select pkppt.GetSqlColumnName();
            IEnumerable<object> pkvs = from pkppt in pkppts select pkppt.GetValue(obj);
            string re = string.Empty;
            if (pkppts.Count() == 1)
                re = pkppts[0].GetSqlColumnValueStr(obj);
            else
                for (int i = 0; i < pkcs.Count(); i++)
                {
                    re += pkppts[i].GetSqlColumnValueStr(obj);
                    if (i < pkcs.Count() - 1)
                        re += " and ";
                }
            return re;
        }
        public static object GetSqlPKValue(this IDbObject obj)
        {
            var pkppts = obj.GetType().GetSqlPKProperties();
            if (pkppts.Any())
                return pkppts.ElementAt(0).GetValue(obj);
            else 
            {
                PropertyInfo idppt = obj.GetType().GetProperty("ID");
                if (idppt is null)
                    idppt = obj.GetType().GetProperty("id");
                if (idppt is null)
                    idppt = obj.GetType().GetProperty("Id");
                if (idppt is null)
                    return default;
                return idppt.GetValue(obj);
            }
        }
        public static string GetSqlNameValue(this INamedObject obj)
        {
            PropertyInfo idppt = obj.GetType().GetProperty("Name");
            if (idppt is null)
                idppt = obj.GetType().GetProperty("name");
            if (idppt is null)
                idppt = obj.GetType().GetProperty("NAME");
            if (idppt is null)
                return default;
            return idppt.GetValue(obj).ToString();
        }
        public static string GetCompositeKeyStr(this IDbObject obj, int compositeKeyPart)
        {
            var ppts = obj.GetSqlCKProperties(compositeKeyPart);
            return (from ppt in ppts select ppt.GetValue(obj)).AllToString();
        }

        public static string GetInsertStr(this IDbObject obj)
        {
            string re = string.Empty;
            IEnumerable<MemberInfo> members = obj.GetSqlMembers(forInsert:true);
            IEnumerable<string> columns = from memberInfo in members select memberInfo.GetSqlColumnName();
            IEnumerable<object> values = from memberInfo in members select memberInfo.GetValue(obj);
            re = string.Format(@"insert into {0} ({1}) values ({2});", obj.GetSqlTableName(), columns.AllToString(), values.AllToString(forDataBase: true));
            return re;
        }
        public static string GetInsertStr(this IEnumerable<IDbObject> dbObjects)
        {
            string re = string.Empty;
            Type t = typeof(IDbObject);
            IEnumerable<MemberInfo> members = t.GetSqlMembers(forInsert: true);
            IEnumerable<string> columns = from memberInfo in members select memberInfo.GetSqlColumnName();
            List<string> valuesStrs = new();
            foreach (IDbObject obj in dbObjects)
            {
                IEnumerable<object> values = from memberInfo in members select memberInfo.GetValue(obj);
                valuesStrs.Add(string.Format(@"({0})", values.AllToString(forDataBase: true)));
            }
            re = $@"insert into {t.GetSqlTableName()} ({columns.AllToString()}) values {valuesStrs.AllToString()};";
            return re;
        }
        public static string GetReplaceStr(this IDbObject obj)
        {
            MemberInfo[] mmbs = obj.GetSqlMembers();
            string pfv = string.Empty;
            foreach (var mmb in mmbs)
            {
                string value = mmb.GetValue(obj).ToDBString();
                pfv += string.Format(@"{0},", mmb.GetSqlColumnValueStr(obj));
            }
            return string.Format(@"replace into {0} set {1}; ", obj.GetSqlTableName(), pfv.Substring(0, pfv.Length - 1));
        }
        public static string GetDeleteStr(this IDbObject obj)
        {
            string re = string.Empty;
            return string.Format(@"delete from {0} where {1} ; ", obj.GetSqlTableName(),obj.GetSqlPKColumnValueStr());
        }
        public static string GetModifyStr(this IDbObject obj)
        {
            MemberInfo[] mmbs = obj.GetSqlMembers();
            string pfv = string.Empty;
            foreach (var mmb in mmbs)
            {
                string value = mmb.GetValue(obj).ToDBString();
                pfv += string.Format(@"{0},", mmb.GetSqlColumnValueStr(obj));
            }
            return string.Format(@"update {0} set {1} where {2} ; ", obj.GetSqlTableName(), pfv.Substring(0, pfv.Length - 1), obj.GetSqlPKColumnValueStr());
        }

        //public static bool SaveToDB<T>(this T obj) where T:IDbObject
        //{
        //    if(obj.ID is null)
        //        return MainDataBase.CommandNonQuery(obj.GetInsertStr()) > 0;
        //    else
        //        return MainDataBase.CommandNonQuery(obj.GetModifyStr()) > 0;
        //}
        //public static bool ReloadFromDB<T>(this T obj) where T : IDbObject
        //{
        //    T[] _objs = MainDataBase.LoadFromDB<T>(obj.ID);
        //    if (_objs.Length > 0)
        //    {
        //        foreach (PropertyInfo pptInfo in obj.GetSqlProperties())
        //        {
        //            obj.SetPropertyValue(pptInfo.Name, _objs.First().GetPropertyValue(pptInfo.Name));
        //        }
        //        return true;
        //    }
        //    else
        //        return false;
        //}
        public static string ToDBString(this object obj)
        {
            if (obj is null)
                return "NULL";
            string re = obj.ToString();
            if (string.IsNullOrWhiteSpace(re))
                return "NULL";
            if (obj is string || obj is char)
            {
                re = re.Replace(@"\", @"\\");
                re = string.Format("\"{0}\"", re); 
            }
            if (obj is DateTime)
                re = "\'" + re + "\'";
            return re;
        }
        public static object GetValue(this MemberInfo memberInfo, object obj)
        {
            if (memberInfo is PropertyInfo pptInfo)
                return pptInfo.GetValue(obj);
            else if (memberInfo is FieldInfo fieldInfo)
                return fieldInfo.GetValue(obj);
            else return null;
        }
        public static void SetValue(this MemberInfo memberInfo, object obj, object value)
        {
            if (memberInfo is PropertyInfo pptInfo)
            {
                if (!pptInfo.PropertyType.HasImplementedRawGeneric(typeof(Nullable<>)))
                    pptInfo.SetValue(obj, Convert.ChangeType(value, pptInfo.PropertyType));
                else
                    pptInfo.SetValue(obj, value is DBNull || value is null ? null : Convert.ChangeType(value, Nullable.GetUnderlyingType(pptInfo.PropertyType)));
            }
            else if (memberInfo is FieldInfo fieldInfo)
                if (!fieldInfo.FieldType.HasImplementedRawGeneric(typeof(Nullable<>)))
                    fieldInfo.SetValue(obj, Convert.ChangeType(value, fieldInfo.FieldType));
                else
                    fieldInfo.SetValue(obj, value is DBNull || value is null ? null : Convert.ChangeType(value, Nullable.GetUnderlyingType(fieldInfo.FieldType)));
        }
    }
}
