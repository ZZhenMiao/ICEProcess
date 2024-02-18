using System;
using System.Collections.Generic;
using System.Text;

namespace LiZhenStandard
{
    /// <summary>
    /// 数据对象接口
    /// </summary>
    public interface IDbObject
    {
        /// <summary>
        /// 数据库对象ID
        /// </summary>
        [SqlProperty(IsPrimaryKey = true)]
        [Display("ID")]
        object ID { get; set; }

        bool Equals(object obj);
        int GetHashCode();
    }
    /// <summary>
    /// 数据抽象对象
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    public abstract class DbObject : IDbObject
    {
        /// <summary>
        /// 数据库对象ID
        /// </summary>
        [SqlProperty(IsPrimaryKey = true)]
        [Display("ID")]
        public object ID { get; set; }
        //public object ID { get => iD; set 
        //    { 
        //        iD = value;
        //        PropertyChanged(this,new PropertyChangedEventArgs(nameof(ID)));
        //    } }

        //public event PropertyChangedEventHandler PropertyChanged;

        public override bool Equals(object obj)
        {
            if (this is null)
                return obj is null;
            if (obj is null)
                return this is null;
            if (!(obj is IDbObject))
                return false;

            if (ID is null)
                return ((IDbObject)obj).ID is null;
            else
                return obj != null && GetType() == obj.GetType() && ((IDbObject)obj).ID.Equals(ID);
        }
        public override int GetHashCode() => ID is null ? 0 : ID.GetHashCode();
    }
    /// <summary>
    /// 已命名的数据对象接口
    /// </summary>
    public interface INamedObject//:INotifyPropertyChanged
    {
        /// <summary>
        /// 名称
        /// </summary>
        [SqlProperty]
        [Display("名称")]
        string Name { get; set; }
        /// <summary>
        /// 简介
        /// </summary>
        [SqlProperty]
        [Display("说明")]
        string Illustration { get; set; }
    }
    /// <summary>
    /// 已命名的数据抽象对象
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    public abstract class DbNamedObject : INamedObject, IDbObject
    {
        private object iD;
        private string name;
        private string illustration;

        /// <summary>
        /// 数据库对象ID
        /// </summary>
        [SqlProperty(IsPrimaryKey = true)]
        [Display("ID")]
        public object ID
        {
            get => iD;
            set { iD = value; }
        }
        /// <summary>
        /// 名称
        /// </summary>
        [SqlProperty]
        [Display("名称")]
        public virtual string Name { get => name; set => name = value; }
        /// <summary>
        /// 简介
        /// </summary>
        [SqlProperty]
        [Display("说明")]
        public virtual string Illustration
        {
            get => illustration;
            set { illustration = value; }
        }

        //public event PropertyChangedEventHandler PropertyChanged;

        public override string ToString()
        {
            return Name;
        }
        public override bool Equals(object obj)
        {
            if (this is null)
                return obj is null;
            if (obj is null)
                return this is null;
            if (!(obj is DbNamedObject))
                return false;

            if (ID is null)
                return ((IDbObject)obj).ID is null;
            else
                return obj != null && GetType() == obj.GetType() && ((IDbObject)obj).ID.Equals(ID);
        }
        public override int GetHashCode() => ID is null ? 0 : ID.GetHashCode();
    }
}
