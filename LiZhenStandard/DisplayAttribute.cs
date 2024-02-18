using System;
using System.Reflection;

namespace LiZhenStandard
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class EnumDisplayAttribute : Attribute
    {
        public string DisplayName { get; set; }
        public EnumDisplayAttribute(string name)
        {
            DisplayName = name;
        }

        /// <summary>
        /// 获取枚举项的displayName
        /// </summary>
        /// <param name="oa">枚举项</param>
        /// <returns>枚举项的displayName</returns>
        public static string GetEnumDisplayName<T>(T oa)where T :Enum
        {
            Type type = oa.GetType();
            string name = Enum.GetName(type, oa);
            FieldInfo field = type.GetField(name);
            EnumDisplayAttribute attribute = Attribute.GetCustomAttribute(field, typeof(EnumDisplayAttribute)) as EnumDisplayAttribute;
            string disName = attribute?.DisplayName;
            return disName;
        }
    }

    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Struct|
        AttributeTargets.Interface|AttributeTargets.Property|AttributeTargets.Field,
        Inherited = true, AllowMultiple = false)]
    public sealed class DisplayAttribute : Attribute
    {
        public string DisplayName { get; set; }
        public DisplayAttribute(string name)
        {
            DisplayName = name;
        }

        /// <summary>
        /// 获取对象的DisplayName
        /// </summary>
        /// <param name="obj">对象</param>
        /// <returns>对象的displayName</returns>
        public static string GetDisplayName<T>(T obj)
        {
            Type type = obj.GetType();
            var attribute = type.GetCustomAttribute<DisplayAttribute>();
            string disName = attribute?.DisplayName;
            return disName;
        }

        /// <summary>
        /// 获取属性的DisplayName
        /// </summary>
        /// <param name="propertyInfo">属性</param>
        /// <returns>属性的displayName</returns>
        public static string GetPropertyDisplayName(PropertyInfo propertyInfo)
        {
            DisplayAttribute attribute = propertyInfo.GetCustomAttribute<DisplayAttribute>();
            string disName = attribute?.DisplayName;
            return disName;
        }
    }





}