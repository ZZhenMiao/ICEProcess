using LiZhenMySQL;
using System;

namespace ICE_Model
{
    /// <summary>
    /// 字段抽象基类
    /// </summary>
    public abstract class BaseField: DbNamedObject, IDbObject
    {
        public int ID_Master { get; set; }
        public FieldValueLimit FieldLimit { get; set; }
    }

    /// <summary>
    /// 字段值类型：数字，文本，比特，日期时间，时间差，任务状态，团队，职位，用户，项目，任务，自定义对象
    /// </summary>
    [Serializable]
    public enum FieldValueType 
    { Number, Text, Bit, DateTime, TimeDifference, TaskState, Team, Position, Person, 
        Project,Task, CustumObject }

    /// <summary>
    /// 输入方式：自由文本，单行文本，勾选框，单选列表，多选列表
    /// </summary>
    [Serializable]
    public enum InputMode { FreeText, SingleText, Check, SingleList, MultipleList }

    /// <summary>
    /// 字段值限定
    /// </summary>
    [Serializable]
    public struct FieldValueLimit
    {
        public FieldValueType FieldValueType { get; set; }
        public string RangeExpression { get; set; }
        public string FormatExpression { get; set; }
        public InputMode InputMode { get; set; }
        /// <summary>
        /// 唯一值
        /// </summary>
        public bool Unique { get; set; }
        public int MaxChars { get; set; }

        public Type GetFieldValueType()
        {
            return GetFieldValueType(FieldValueType);
        }
        public static Type GetFieldValueType(FieldValueType valueType)
        {
            switch (valueType)
            {
                case FieldValueType.Number: return typeof(double);
                case FieldValueType.Text: return typeof(string);
                case FieldValueType.DateTime: return typeof(DateTime);
                case FieldValueType.TimeDifference: return typeof(TimeDifference);
                case FieldValueType.TaskState: return typeof(TaskState);
                case FieldValueType.Team: return typeof(Team);
                case FieldValueType.Position: return typeof(Position);
                case FieldValueType.Person: return typeof(Person);
                case FieldValueType.CustumObject: return typeof(CustumObject);
                default:
                    return null;
            }
        }
    }

    /// <summary>
    /// 字段泛型基类
    /// </summary>
    /// <typeparam name="T">主对象类型</typeparam>
    [Serializable]
    public class Field<T> : BaseField
    {
        public T Master { get; set; }
    }

    /// <summary>
    /// 字段值抽象基类
    /// </summary>
    [Serializable]
    public abstract class FieldValue
    {
        public int ID_Field { get; set; }
        public string BaseValue { get; set; }
        public abstract string FormatExpression { get; }
        public abstract string DisplayValue { get; }

        public abstract string ToString(string format);

        //public static FieldValue MakeNullFieldValue(Type fieldType, Type valueType)
        //{
        //    System.Reflection.MethodInfo methodInfo = valueType.GetMethod("TryParse");

        //    if (methodInfo is null)
        //        throw new Exception("值对象类型不能由字符串转换。");

        //    object obj = typeof(FieldValue<,>).MakeGenericType(new Type[] { fieldType, valueType }).GetConstructor(null).Invoke(null);
        //    return (FieldValue)obj;
        //}
        //public static FieldValue MakeFieldValue(IDBObject master, BaseField field,string baseValue)
        //{
        //    Type fieldtype = typeof(Field<>).MakeGenericType(master.GetType());
        //    Type valuetype = field.FieldLimit.GetFieldValueType();
        //    FieldValue value = MakeNullFieldValue(fieldtype, valuetype);
        //    value.BaseValue_String = baseValue;
        //    value.ID_Field = field.ID;
        //    value.SetPropertyValue("Field", field);
        //    return value;
        //}
    }

    /// <summary>
    /// 字段值泛型基类
    /// </summary>
    /// <typeparam name="TField">字段类型</typeparam>
    [Serializable]
    public abstract class FieldValue<TField> : FieldValue where TField : BaseField
    {
        public TField Field { get; set; }
        public override string FormatExpression { get => Field?.FieldLimit.FormatExpression; }
        public override string DisplayValue { get => ToString(FormatExpression); }
        public override string ToString()
        {
            return BaseValue;
        }
    }

    /// <summary>
    /// 字段值
    /// </summary>
    /// <typeparam name="TField">字段类型</typeparam>
    /// <typeparam name="TValue">值类型</typeparam>
    [Serializable]
    public class FieldValue<TField, TValue> : FieldValue<TField> where TField : BaseField
    {
        public TValue Value { get => Parse(); }

        public override string ToString(string format)
        {
            Type t = typeof(TValue);
            System.Reflection.MethodInfo m = t.GetMethod("ToString", new Type[] { typeof(string) });
            object re = m?.Invoke(t, new object[] { format });
            return re.ToString();
        }
        public TValue Parse()
        {
            Type type = typeof(TValue);
            System.Reflection.MethodInfo methodInfo = type.GetMethod("TryParse");
            object[] objs = new object[2] { BaseValue, null };
            methodInfo?.Invoke(type, objs);
            return (TValue)objs[1];
        }
    }

}
