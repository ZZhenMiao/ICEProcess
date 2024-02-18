using System;
using System.Collections.ObjectModel;
using LiZhenMySQL;

namespace ICE_Model
{
    /// <summary>
    /// 团队字段（静态）
    /// </summary>
    [Serializable]
    public static class TeamFields
    {
        public static ObservableCollection<Field<Team>> Fields { get; } = new ObservableCollection<Field<Team>>();
    }
    /// <summary>
    /// 团队
    /// </summary>
    [SqlObject]
    [Serializable]
    public class Team : Tree<Team>, IDbObject, INamedObject
    {
        public ObservableCollection<Position> Positions { get; } = new ObservableCollection<Position>();
        public ObservableCollection<FieldValue<Field<Team>>> FieldValues { get; } = new ObservableCollection<FieldValue<Field<Team>>>();
    }
    /// <summary>
    /// 职位字段（静态）
    /// </summary>
    [Serializable]
    public static class PositionFields
    {
        public static ObservableCollection<Field<Position>> Fields { get; } = new ObservableCollection<Field<Position>>();
    }
    /// <summary>
    /// 职位
    /// </summary>
    [SqlObject]
    [Serializable]
    public class Position : DbNamedObject, IDbObject
    {
        public object ID_Team { get; set; }
        public Team Team { get; set; }

        public ObservableCollection<Person> People { get; } = new ObservableCollection<Person>();
        public ObservableCollection<Obligation> Obligations { get; } = new ObservableCollection<Obligation>();
        public ObservableCollection<FieldValue<Field<Position>>> FieldValues { get; } = new ObservableCollection<FieldValue<Field<Position>>>();
    }
    /// <summary>
    /// 职能
    /// </summary>
    [SqlObject]
    [Serializable]
    public class Obligation : DbNamedObject, IDbObject
    {
        [SqlProperty]
        public int AllAssetType { get; set; }
    }
    /// <summary>
    /// 人员字段
    /// </summary>
    [Serializable]
    public static class PersonFields
    {
        public static ObservableCollection<Field<Person>> Fields { get; } = new ObservableCollection<Field<Person>>();
    }
    /// <summary>
    /// 人员
    /// </summary>
    [SqlObject] [Serializable] [LoginUserClass("account", "password")]
    public class Person : DbNamedObject, IDbObject
    {
        [SqlProperty]
        public string Account { get; set; }
        [SqlProperty]
        public string Password { get; set; }

        public ObservableCollection<FieldValue<Field<Person>>> FieldValues { get; } = new ObservableCollection<FieldValue<Field<Person>>>();
    }

}
