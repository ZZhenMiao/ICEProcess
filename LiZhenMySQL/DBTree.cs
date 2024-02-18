using LiZhenStandard;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiZhenStandard.Extensions;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace LiZhenMySQL
{
    public interface IDBTree<T> : ITree<T>, INamedObject, IDbObject where T : IDBTree<T>
    {
        /// <summary>
        /// 父对象ID
        /// </summary>
        object ParentID { get; set; }
        /// <summary>
        /// 将一组元素整理为树形结构。
        /// </summary>
        /// <param name="ts">待整理的元素集合</param>
        /// <returns>整理好的树结构</returns>
        static T[] MakeTree(IEnumerable<T> ts) 
        {
            List<T> re = new();
            re.AddRange(ts);
            foreach (T t in ts)
            {
                IEnumerable<T> children = ts.Where(x =>
                {
                    if (x.ParentID is not null && x.ParentID is not DBNull)
                        return x.ParentID.Equals(t.ID);
                    return false;
                });
                foreach (var child in children)
                {
                    child.Parent = t;
                    t.Add(child);
                    re.Remove(child);
                }
            }
            return re.ToArray();
        }
        IEnumerable<T> WhereInChildren(Func<T, bool> predicate);
    }

    [Serializable]
    public class DBTree<T> : DBObjContainer<T>, IDBTree<T>, INamedObject, IDbObject where T : DBTree<T>
    {
        private object parentID = null;
        private T parent;
        private string name;
        private string illustration;
        private object iD;

        /// <summary>
        /// 父节点变更事件
        /// </summary>
        public event TreeParentChangedEventHandler TreeParentChanged;

        /// <summary>
        /// 数据库对象ID
        /// </summary>
        [SqlProperty(IsPrimaryKey = true)]
        [Display("ID")]
        public object ID { get => iD; set 
            { 
                iD = value;
                OnPropertyChanged(new PropertyChangedEventArgs("ID"));
            }
        }
        /// <summary>
        /// 父对象ID
        /// </summary>
        [SqlProperty]
        public object ParentID
        {
            get => parentID; set
            {
                parentID = value;
                OnPropertyChanged(new PropertyChangedEventArgs("ParentID"));
            }
        }
        /// <summary>
        /// 父对象
        /// </summary>
        [Display("父节点")]
        public virtual T Parent
        {
            get => parent; set
            {
                parent = value;
                OnPropertyChanged(new PropertyChangedEventArgs("Parent"));
            }
        }
        /// <summary>
        /// 名称
        /// </summary>
        [SqlProperty]
        [Display("名称")]
        public virtual string Name
        {
            get => name; set
            {
                name = value;
                OnPropertyChanged(new PropertyChangedEventArgs("Name"));
            }
        }
        /// <summary>
        /// 简介
        /// </summary>
        [SqlProperty]
        [Display("说明")]
        public virtual string Illustration
        {
            get => illustration;
            set
            {
                illustration = value;
                OnPropertyChanged(new PropertyChangedEventArgs("Illustration"));
            }
        }
        /// <summary>
        /// 子对象集合
        /// </summary>
        public ObservableCollection<T> Children { get => this; }

        /// <summary>
        /// 从数据库加载子项
        /// </summary>
        /// <returns>加载的子项数目</returns>
        public int LoadChildrenFromDB(string parentColumnName = "parentid", bool LoadChildren = false)
        {
            int re = base.LoadFromDB_Where(string.Format(@"{0}={1}", parentColumnName, ID.ToDBString()));
            for (int i = 0; i < Count; i++)
            {
                T obj = this[i];
                obj.Parent = (T)this;
            }
            if (LoadChildren)
                for (int i = 0; i < Count; i++)
                {
                    re += this[i].LoadChildrenFromDB(parentColumnName, true);
                }
            return re;
        }
        /// <summary>
        /// 获取根节点
        /// </summary>
        /// <returns>根节点对象</returns>
        public T GetRoot()
        {
            return ParentID is null || ParentID is DBNull ? (T)this : Parent.GetRoot();
        }
        /// <summary>
        /// 当父节点变化时引发的事件
        /// </summary>
        /// <param name="sender">引发事件的对象</param>
        /// <param name="e">事件信息</param>
        public virtual void OnTreeParentChanged(object sender, TreeParentChangedEventArgs e)
        {
            TreeParentChanged?.Invoke(sender, e);
        }
        ///// <summary>
        ///// 将一组元素整理为树形结构。
        ///// </summary>
        ///// <param name="ts">待整理的元素集合</param>
        ///// <returns>整理好的树结构</returns>
        //public static T[] MakeTree(IEnumerable<T> ts) 
        //{
        //    List<T> re = new();
        //    foreach (var t in ts)
        //    {
        //        var f = re.Find(x => x.ID.Equals(t.ParentID));
        //        if (f is null)
        //            re.Add(t);
        //        else
        //        {
        //            f.Add(t);
        //            re.Remove(t);
        //        }
        //    }
        //    return re.ToArray();
        //}

        /// <summary>
        /// 从数据库加载该树型结构的所有根节点
        /// </summary>
        /// <param name="parentColumnName">父对象ID列名</param>
        /// <param name="LoadChildren">加载子对象</param>
        /// <returns>查询到的树集合</returns>
        public static T[] LoadRootsFromDB(string parentColumnName = "parentid", bool LoadChildren = false)
        {
            T[] nodes = DataBase.LoadFromDB_Where<T>(@"{0} is {1}", parentColumnName, @"null");
            if (LoadChildren)
                for (int i = 0; i < nodes.Length; i++)
                {
                    nodes[i].LoadChildrenFromDB(parentColumnName, true);
                }
            return nodes;
        }
        /// <summary>
        /// 获取当前节点的所有上层节点的集合
        /// </summary>
        /// <returns>父节点集合，不包括当前节点</returns>
        public IEnumerable<T> GetParentNodes()
        {
            List<T> re = new();
            re.AddRange(GetParentNodesWithSelf());
            re.Remove((T)this);
            return re;
        }
        public IEnumerable<T> GetParentNodesWithSelf()
        {
            List<T> re = new() { (T)this };
            if (parent is not null && parent is not DBNull)
                re.AddRange(parent?.GetParentNodesWithSelf());
            if (ParentID is not null && ParentID is not DBNull)
                re.AddRange(ParentID.LoadFromDB_ThisID<T>()?.GetParentNodesWithSelf());
            return re;
        }
        public IEnumerable<T> GetParentNodes_NoDataBase()
        {
            List<T> re = new();
            re.AddRange(GetParentNodesWithSelf_NoDataBase());
            re.Remove((T)this);
            return re;
        }
        public IEnumerable<T> GetParentNodesWithSelf_NoDataBase()
        {
            List<T> re = new() { (T)this };
            if (parent is not null && parent is not DBNull)
                re.AddRange(parent?.GetParentNodesWithSelf_NoDataBase());
            return re;
        }


        //public static IEnumerable<T> MakeTree(IEnumerable<T> nodes)
        //{
        //    List<T> ns = new();
        //    ns.AddRange(nodes);
        //    var roots = FindRoots(nodes);
        //    foreach (var root in roots)
        //    {
        //        ns.Remove(root);
        //    }
        //    foreach (var node in roots)
        //    {
        //        node.FindChildren(node, ns);
        //    }
        //    return roots;
        //}
        //public static IEnumerable<T> FindRoots(IEnumerable<T> nodes)
        //{
        //    List<T> re = new();
        //    foreach (var node in nodes)
        //    {
        //        if (nodes.FindByProperty("ID", node.ParentID) is null || node.ParentID is null)
        //            re.Add(node);
        //    }
        //    return re;
        //}
        public void FindChildren(T parentNode, IEnumerable<T> nodes)
        {
            List<T> ns = new();
            ns.AddRange(nodes);
            foreach (var node in nodes)
            {
                if (node.ParentID.Equals(parentNode))
                {
                    parentNode.Add(node);
                    ns.Remove(node);
                }
            }
            foreach (var node in parentNode)
            {
                node.FindChildren(node, ns);
            }
        }

        public override string ToString()
        {
            return Name;
        }
        public override bool Equals(object obj)
        {
            if (obj is not T)
                return false;
            if (obj is null && this is not null)
                return false;
            if (obj is null && this is null)
                return true;
            if (this.ID is null)
                return false;
            if (obj is null)
                return false;
            if (((IDbObject)obj).ID is null)
                return false;
            return this.ID.Equals(((IDbObject)obj).ID);
        }
        public override int GetHashCode()
        {
            if (ID is null)
                return -1;
            return ID.GetHashCode();
        }

        public IEnumerable<T> WhereInChildren(Func<T, bool> predicate) 
        {
            IEnumerable<T> all = GetAllChildrenWithSelf();
            return all.Where(predicate);
        }
        public IEnumerable<T> GetAllChildren()
        {
            List<T> re = new List<T>();
            foreach (T child in Children)
            {
                re.Add(child);
                if (child.Children.Any())
                    re.AddRange(child.GetAllChildren());
            }
            return re;
        }
        public IEnumerable<T> GetAllChildrenWithSelf()
        {
            List<T> re = new List<T>
            {
                (T)this
            };
            re.AddRange(GetAllChildren());
            return re;
        }
    }
}
