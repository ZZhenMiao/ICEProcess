using LiZhenStandard.Extensions;
using LiZhenStandard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using MySql.Data.MySqlClient;

namespace LiZhenMySQL
{
    /// <summary>
    /// 旧的： 兼容数据库的对象容器
    /// </summary>
    /// <typeparam name="T">数据库对象类型</typeparam>
    //public class DBObjContainer<T> : ObservableCollection<T> where T : IDbObject
    //{
    //    private string associationPropertyName;
    //    private IDbObject[] associationObjects;

    //    /// <summary>
    //    /// 关联表信息
    //    /// </summary>
    //    public ChainInfo? ChainInfo { get; set; }

    //    /// <summary>
    //    /// 默认查询命令字符串
    //    /// </summary>
    //    public string DefuteWhere { get; set; }
    //    /// <summary>
    //    /// 关联对象属性名
    //    /// </summary>
    //    public string AssociationPropertyNames
    //    {
    //        get =>
    //            associationPropertyName ??
    //            (from AssociationObject in AssociationObjects select AssociationObject?.GetType().Name).AllToString();
    //        set => associationPropertyName = value;
    //    }
    //    /// <summary>
    //    /// 关联对象
    //    /// </summary>
    //    public IDbObject AssociationObject { get; set; }
    //    /// <summary>
    //    /// 关联对象集合
    //    /// </summary>
    //    public IDbObject[] AssociationObjects { get => associationObjects ?? new IDbObject[] { AssociationObject }; set => associationObjects = value; }
    //    /// <summary>
    //    /// 投影目标对象表名称
    //    /// </summary>
    //    public string EpitomeTargetTableName { get; set; }

    //    /// <summary>
    //    /// 从数据库加载元素
    //    /// </summary>
    //    /// <param name="where">查询条件字符串，如果为Null，则为默认的DefuteWhere，如果后者也为Null，则按"where id_[关联对象表名]=[关联对象ID]"查询。</param>
    //    /// <param name="id">条件对象ID，如果为Null，则为默认的关联对象ID</param>
    //    /// <returns>加载的元素数</returns>
    //    public virtual int LoadFromDB(string where = null, object id = null,string cmd = null)
    //    {
    //        base.Clear();
    //        T[] re;
    //        if (!string.IsNullOrWhiteSpace(cmd))
    //            re = MainDataBase.LoadFromDB<T>(commandStr:cmd);
    //        else if (ChainInfo.HasValue)
    //            re = MainDataBase.LoadFromDB<T>(ChainInfo.Value, targetIDs: id is null ? (from obj in AssociationObjects select obj.ID).ToArray() : new object[] { id });
    //        else if (!(id is null))
    //            re = MainDataBase.LoadFromDB<T>(id);
    //        else if (!string.IsNullOrWhiteSpace(DefuteWhere))
    //            re = MainDataBase.LoadFromDB<T>(where: DefuteWhere);
    //        else if (!string.IsNullOrWhiteSpace(where))
    //            re = MainDataBase.LoadFromDB<T>(where: where);
    //        else
    //            re = MainDataBase.LoadFromDB<T>(where: string.Format("id_{0}={1}", AssociationObjects[0].GetSqlTableName(), AssociationObjects[0].ID.ToDBString()));
    //        for (int i = 0; i < re.Length; i++)
    //        {
    //            base.Add(re[i]);
    //        }
    //        AssignmentAssociated(re);
    //        return re.Length;
    //    }

    //    /// <summary>
    //    /// 获取所有元素的某个属性与其属性值相对应的Where条件格式字符串，默认以“or”连接。
    //    /// </summary>
    //    /// <param name="sqlPropertyName">属性名</param>
    //    /// <param name="targetColumnName">目标元素对应的表字段名</param>
    //    /// <returns>连接好的Where条件字符串</returns>
    //    public virtual string GetWhereStr(string sqlPropertyName, string targetColumnName)
    //    {
    //        string re = "";
    //        PropertyInfo pptInfo = typeof(T).GetProperty(sqlPropertyName ?? @"ID");
    //        List<object> finded = new List<object>();
    //        for (int i = 0; i < Count; i++)
    //        {
    //            T obj = this[i];
    //            object value = pptInfo.GetValue(obj);
    //            if (finded.Contains(value))
    //                continue;
    //            else
    //                finded.Add(value);
    //            re += string.Format(@"{0}={1}", targetColumnName ?? pptInfo.GetSqlColumnName(), pptInfo.GetValue(obj));
    //            if (i < Count - 1)
    //                re += @" or ";
    //        }
    //        if (re.Length > 4)
    //            if (re.Substring(re.Length - 4, 4) == @" or ")
    //                re = re.Substring(0, re.Length - 4);
    //        return re;
    //    }
    //    /// <summary>
    //    /// 赋值关联对象
    //    /// </summary>
    //    private void AssignmentAssociated(T[] objs)
    //    {
    //        if (string.IsNullOrWhiteSpace(AssociationPropertyNames) || AssociationObjects is null)
    //            return;
    //        var AssociationPropertyNamesArr = AssociationPropertyNames.Split(',');
    //        foreach (T obj in objs)
    //        {
    //            for (int i = 0; i < AssociationObjects.Length; i++)
    //            {
    //                obj.SetPropertyValue(AssociationPropertyNamesArr[i], AssociationObjects[i]);
    //            }
    //        }
    //    }

    //    public T Find(Predicate<T> match)
    //    {
    //        IEnumerable<T> re = this.Where(new Func<T, bool>(a => match.Invoke(a)));
    //        if (re.Count() > 0)
    //            return re.ElementAt(0);
    //        else
    //            return default(T);
    //    }
    //}

    /// <summary>
    /// 旧的： 兼容数据库的对象容器
    /// </summary>
    /// <typeparam name="T">数据库对象类型</typeparam>
    //public class DBObjContainer<T> : ObservableCollection<T> where T : IDbObject
    //{
    //    private string associationPropertyName;
    //    private IDbObject[] associationObjects;

    //    /// <summary>
    //    /// 关联表信息
    //    /// </summary>
    //    public ChainingInfo ChainInfo { get; set; }

    //    /// <summary>
    //    /// 默认查询命令字符串
    //    /// </summary>
    //    public string DefuteWhere { get; set; }
    //    /// <summary>
    //    /// 关联对象属性名，可以赋多个值，以“,”分隔。其顺序必须和关联对象的顺序相同。如果赋值，则必须赋全，其数量必须与关联对象相同。如果不赋值，则必须全不赋值。默认全部以关联对象类型名称来命名。
    //    /// </summary>
    //    public string AssociationPropertyNames
    //    {
    //        get =>
    //            associationPropertyName ??
    //            (from AssociationObject in AssociationObjects select AssociationObject?.GetType().Name).AllToString();
    //        set => associationPropertyName = value;
    //    }
    //    /// <summary>
    //    /// 关联对象，为了简便单一关联对象的特殊属性。如果有多个关联对象，请使用关联对象集合属性！
    //    /// </summary>
    //    public IDbObject AssociationObject { get; set; }
    //    /// <summary>
    //    /// 关联对象集合。
    //    /// </summary>
    //    public IDbObject[] AssociationObjects { get => associationObjects ?? new IDbObject[] { AssociationObject }; set => associationObjects = value; }
    //    ///// <summary>
    //    ///// 投影目标对象表名称
    //    ///// </summary>
    //    //public string EpitomeTargetTableName { get; set; }

    //    /// <summary>
    //    /// 从数据库加载元素
    //    /// </summary>
    //    /// <param name="where">查询条件字符串，如果为Null，则为默认的DefuteWhere，如果后者也为Null，则按"where id_[关联对象表名]=[关联对象ID]"查询。</param>
    //    /// <param name="id">条件对象ID，如果为Null，则为默认的关联对象ID</param>
    //    /// <returns>加载的元素数</returns>
    //    public virtual int LoadFromDB(string where = null, object id = null, string cmd = null)
    //    {
    //        base.Clear();
    //        T[] re;
    //        if (!string.IsNullOrWhiteSpace(cmd))
    //            re = MainDataBase.LoadFromDB<T>(commandStr: cmd);
    //        else if (ChainInfo.HasValue)
    //            re = MainDataBase.LoadFromDB<T>(ChainInfo.Value, targetIDs: id is null ? (from obj in AssociationObjects select obj.ID).ToArray() : new object[] { id });
    //        else if (!(id is null))
    //            re = MainDataBase.LoadFromDB<T>(id);
    //        else if (!string.IsNullOrWhiteSpace(DefuteWhere))
    //            re = MainDataBase.LoadFromDB<T>(where: DefuteWhere);
    //        else if (!string.IsNullOrWhiteSpace(where))
    //            re = MainDataBase.LoadFromDB<T>(where: where);
    //        else
    //            re = MainDataBase.LoadFromDB<T>(where: string.Format("id_{0}={1}", AssociationObjects[0].GetSqlTableName(), AssociationObjects[0].ID.ToDBString()));
    //        for (int i = 0; i < re.Length; i++)
    //        {
    //            base.Add(re[i]);
    //        }
    //        AssignmentAssociated(re);
    //        return re.Length;
    //    }

    //    /// <summary>
    //    /// 获取所有元素的某个属性与其属性值相对应的Where条件格式字符串，默认以“or”连接。
    //    /// </summary>
    //    /// <param name="sqlPropertyName">属性名</param>
    //    /// <param name="targetColumnName">目标元素对应的表字段名</param>
    //    /// <returns>连接好的Where条件字符串</returns>
    //    public virtual string GetWhereStr(string sqlPropertyName, string targetColumnName)
    //    {
    //        string re = "";
    //        PropertyInfo pptInfo = typeof(T).GetProperty(sqlPropertyName ?? @"ID");
    //        List<object> finded = new List<object>();
    //        for (int i = 0; i < Count; i++)
    //        {
    //            T obj = this[i];
    //            object value = pptInfo.GetValue(obj);
    //            if (finded.Contains(value))
    //                continue;
    //            else
    //                finded.Add(value);
    //            re += string.Format(@"{0}={1}", targetColumnName ?? pptInfo.GetSqlColumnName(), pptInfo.GetValue(obj));
    //            if (i < Count - 1)
    //                re += @" or ";
    //        }
    //        if (re.Length > 4)
    //            if (re.Substring(re.Length - 4, 4) == @" or ")
    //                re = re.Substring(0, re.Length - 4);
    //        return re;
    //    }
    //    /// <summary>
    //    /// 赋值关联对象
    //    /// </summary>
    //    private void AssignmentAssociated(T[] objs)
    //    {
    //        if (string.IsNullOrWhiteSpace(AssociationPropertyNames) || AssociationObjects is null)
    //            return;
    //        var AssociationPropertyNamesArr = AssociationPropertyNames.Split(',');
    //        foreach (T obj in objs)
    //        {
    //            for (int i = 0; i < AssociationObjects.Length; i++)
    //            {
    //                obj.SetPropertyValue(AssociationPropertyNamesArr[i], AssociationObjects[i]);
    //            }
    //        }
    //    }
    //    public T Find(Predicate<T> match)
    //    {
    //        IEnumerable<T> re = this.Where(new Func<T, bool>(a => match.Invoke(a)));
    //        if (re.Count() > 0)
    //            return re.ElementAt(0);
    //        else
    //            return default(T);
    //    }
    //}


    /// <summary>
    /// 兼容数据库的对象容器
    /// </summary>
    /// <typeparam name="T">对象类型（必须继承IDBObject接口）</typeparam>
    [Serializable]
    public class DBObjContainer<T> : ObservableCollection<T> where T : IDbObject
    {
        /// <summary>
        /// 从数据库加载对象到这个容器的基本方法。
        /// </summary>
        /// <param name="ts">加载到的元素数组</param>
        /// <returns>加载的元素数</returns>
        public int LoadFromDB_Base(T[] ts)
        {
            Clear();
            AddRange(ts);
            return ts.Length;
        }

        /// <summary>
        /// 只读，这个集合类型所对应的连接
        /// </summary>
        public MySqlConnection Connection { get => typeof(T).GetMySqlConnection(); }
        /// <summary>
        /// 在这个集合中查找一个对象
        /// </summary>
        /// <param name="match">查找条件</param>
        /// <returns>找到的对象，若没有找到则为T类型的默认值</returns>
        public T Find(Predicate<T> match)
        {
            IEnumerable<T> re = this.Where(new Func<T, bool>(a => match.Invoke(a)));
            if (re.Any())
                return re.ElementAt(0);
            else
                return default;
        }
        /// <summary>
        /// 将一个集合中的对象全部添加到这个集合中。
        /// </summary>
        /// <param name="objs">要添加的对象集合</param>
        public void AddRange(IEnumerable<T> objs)
        {
            foreach (var obj in objs)
            {
                Add(obj);
            }
        }
        /// <summary>
        /// 根据Where条件加载对象
        /// </summary>
        /// <param name="where">Where条件字符串</param>
        /// <param name="args">字符串格式参数</param>
        /// <returns>加载的对象数</returns>
        public int LoadFromDB_Where(string where,params object[] args)
        {
            return LoadFromDB_Base(DataBase.LoadFromDB_Where<T>(where, args));
        }
        /// <summary>
        /// 直接根据SQL命令语句加载对象（注意：比较容易出错哦！）
        /// </summary>
        /// <param name="command">SQL命令语句（必须是查询命令哦！）</param>
        /// <param name="args">SQL命令语句字符串格式参数</param>
        /// <returns>加载的对象数</returns>
        public int LoadFromDB_Command(string command, params object[] args)
        {
            return LoadFromDB_Base(DataBase.LoadFromDB_Command<T>(command, args));
        }
        /// <summary>
        /// 根据所属主对象的ID加载对象。
        /// </summary>
        /// <returns>加载的对象数</returns>
        public int LoadFromDB_SourceObj(IDbObject dbObject,string idColumn = null)
        {
            return LoadFromDB_Base(DataBase.LoadFromDB_SourceObj<T>(dbObject, idColumn));
        }
        /// <summary>
        /// 根据关联的其他对象加该集合载数据对象。
        /// </summary>
        /// <param name="sourceObj">关联的对象</param>
        /// <returns>查询到的数据集合</returns>
        public int LoadFromDB_SourceObj(IDbObject sourceObj)
        {
            return LoadFromDB_Base(DataBase.LoadFromDB_SourceObj<T>(sourceObj));
        }
        /// <summary>
        /// 根据关联的多个其他对象加该集合载数据对象。
        /// </summary>
        /// <param name="sourceObjs">关联的对象集合</param>
        /// <returns>查询到的数据集合</returns>
        public int LoadFromDB_SourceObjs(IEnumerable<IDbObject> sourceObjs)
        {
            return LoadFromDB_Base(DataBase.LoadFromDB_SourceObjs<T>(sourceObjs));
        }
        /// <summary>
        /// 根据给定的ID加载对象。
        /// </summary>
        /// <param name="ids">ID集合</param>
        /// <returns>加载的对象数</returns>
        public int LoadFromDB_IDs(IEnumerable<object> ids)
        {
            List<string> whereStrs = new();
            var idColumn = "id";
            foreach (var d in ids)
            {
                if (d is null)
                    continue;
                var id = d.ToDBString();
                idColumn = "id_" + typeof(T).Name.ToLower();
                whereStrs.Add(id);
            }
            var whereStr = string.Format("{0} in ({1})", idColumn, whereStrs.AllToString(separator: ","));
            return LoadFromDB_Where(whereStr);
        }
        /// <summary>
        /// 根据集合类型与附属主对象的ID连接表信息加载对象
        /// </summary>
        /// <returns>加载的对象数</returns>
        public int LoadFromDB_Chain(IDbObject sourceObj, int mark = 0)
        {
            Type ta = sourceObj?.GetType();
            Type tb = typeof(T);
            ChainTableInfo chainTableInfo = null;
            if(!(ta is null || tb is null))
                chainTableInfo = ChainingInfo.GetChainTableInfo(ta, tb);
            return LoadFromDB_Base(DataBase.LoadFromDB_Chain<T>(chainTableInfo, mark, sourceObj?.ID));
        }
        /// <summary>
        /// 跟据一个附属主对象，将这个集合同步到数据库。
        /// </summary>
        /// <param name="sourceObj">附属主对象</param>
        /// <returns>同步结果</returns>
        public SynchronizeResult SynchronizeListToDB_SourceObj(IDbObject sourceObj)
        {
            SynchronizeResult re = new();
            DBObjContainer<T> cache = new();
            cache.LoadFromDB_SourceObj(sourceObj);
            foreach (T item in cache)
            {
                if (!this.Contains(item))
                { item.DeleteThisFromDB(); re.DeleteNum++; }
            }
            foreach (T item in this)
            {
                if (item.ID is null)
                { item.InsertThisToDB();re.InsertsNum++; }
                else
                {item.ModifyThisToDB();re.ModifyNum++; }
            }
            return re;
        }
        /// <summary>
        /// 跟据一个附属主对象，将这个集合以及与主对象的关联关系同步到数据库。
        /// </summary>
        /// <param name="sourceObj">附属主对象</param>
        /// <returns>同步结果</returns>
        public SynchronizeResult SynchronizeListToDB_Chain(IDbObject sourceObj, int mark = 0)
        {
            SynchronizeResult re = new();
            DBObjContainer<T> cache = new();
            cache.LoadFromDB_Chain(sourceObj,mark);
            foreach (T item in cache)
            {
                if (!this.Contains(item))
                { DataBase.DeleteFromDB_Chain(item,sourceObj); item.DeleteThisFromDB(); re.DeleteNum++; }
            }
            foreach (T item in this)
            {
                if (item.ID is null)
                { item.InsertThisToDB();DataBase.InsertToDB_Chain(item,sourceObj,mark) ; re.InsertsNum++; }
                else
                { item.ModifyThisToDB(); re.ModifyNum++; }
            }
            return re;
        }

    }
    /// <summary>
    /// 同步结果
    /// </summary>
    public struct SynchronizeResult
    {
        /// <summary>
        /// 新插入的数据条目数
        /// </summary>
        public int InsertsNum { get; set; }
        /// <summary>
        /// 删除的数据条目数
        /// </summary>
        public int DeleteNum { get; set; }
        /// <summary>
        /// 修改的数据条目数
        /// </summary>
        public int ModifyNum { get; set; }
    }
}