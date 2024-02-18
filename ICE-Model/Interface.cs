using System;
using System.Collections.ObjectModel;
using LiZhenMySQL;

namespace ICE_Model
{
    /// <summary>
    /// 接口：数据库对象
    /// </summary>
    //public interface IDbObject_TNS:IDbObject
    //{

    //}

    /// <summary>
    /// 接口：树
    /// </summary>
    [Serializable]
    public abstract class Tree<T>:DBTree<T> where T:Tree<T>
    {
        public ObservableCollection<T> Childs { get; } = new ObservableCollection<T>();
    }

    /// <summary>
    /// 接口，具有名称和说明的对象
    /// </summary>
    //public interface INamedObject_TNS:INamedObject
    //{

    //}

    //[Serializable]
    //public abstract class NamedObject : DbNamedObject
    //{

    //}
}
