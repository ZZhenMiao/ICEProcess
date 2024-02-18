using LiZhenMySQL;
using System.Collections.ObjectModel;

namespace ICE_Model
{
    /// <summary>
    /// 自定义类型
    /// </summary>
    public class CustumType : DbNamedObject, IDbObject
    {
        public ObservableCollection<Field<CustumType>> Fields { get; } = new ObservableCollection<Field<CustumType>>();
    }

    /// <summary>
    /// 自定义对象
    /// </summary>
    public class CustumObject : DbNamedObject, IDbObject
    {
        public ObservableCollection<FieldValue<Field<CustumType>>> FieldValues { get; } = new ObservableCollection<FieldValue<Field<CustumType>>>();
    }

}
