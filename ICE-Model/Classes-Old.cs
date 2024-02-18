using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ICE_Model
{
    public interface IDataBaseObject
    {
        int ID { get; set; }
    }

    /// <summary>
    /// 接口：表
    /// </summary>
    public interface ITable
    {
        string TableCode { get; set; }
        IList<Field> Fields { get; }
        IList<ITableItem> Items { get; }
    }

    /// <summary>
    /// 接口：树
    /// </summary>
    public interface ITree<T>
    {
        int ID_Parent { get; set; }
        T Parent { get; set; }
        IList<T> Childs { get; }
    }

    /// <summary>
    /// 条目
    /// </summary>
    public interface ITableItem
    {
        public string TableCode { get; set; }
        public int ID { get; set; }
        public ITable Table { get; set; }
        public IList<FieldValue> FieldValues { get; }
    }

    /// <summary>
    /// 实体类
    /// </summary>
    public abstract class ICE_Class : ITable, INamedObject
    {
        public string TableCode { get; set; }
        public IList<Field> Fields { get; }
        public IList<ITableItem> Items { get; }
        public string Name { get; set; }
        public string Illustration { get; set; }
    }

    /// <summary>
    /// 模块（项目实体类）
    /// </summary>
    public abstract class Module : ICE_Class
    {
        public NameExpression NameExpression { get; set; }
    }

    /// <summary>
    /// 实体
    /// </summary>
    public abstract class ICE_Object: ITableItem,IDataBaseObject
    {
        public int ID { get; set; }
        public string TableCode { get; set; }
        public ITable Table { get; set; }
        public IList<FieldValue> FieldValues { get; }
    }

    /// <summary>
    /// 项目
    /// </summary>
    public abstract class Project : ICE_Object, INamedObject,ITree<Project>
    {
        public int ID_Parent { get; set; }
        public Project Parent { get; set; }
        public IList<Project> Childs { get; }

        private string name;
        public string Name 
        {
            get
            {
                return string.IsNullOrWhiteSpace(name) ? ((Module)Table).NameExpression.Analesize(this) : name;
            }
            set 
            {
                name = value;
            }
        }
        public string Illustration { get; set; }

        public Project(Module module)
        {
            this.Table = module;
        }
    }

    /// <summary>
    /// 团队实体类
    /// </summary>
    public abstract class Team_Class : ICE_Class
    {

    }
    /// <summary>
    /// 团队
    /// </summary>
    public abstract class Team : ICE_Object, INamedObject, ITree<Team>
    {
        public int ID_Parent { get; set; }
        public Team Parent { get; set; }
        public IList<Team> Childs { get; }

        public string Name { get; set; }
        public string Illustration { get; set; }

        public IList<Position> Positions { get; }
    }

    /// <summary>
    /// 流程
    /// </summary>
    public abstract class Process : INamedObject
    {
        public string Name { get; set; }
        public string Illustration { get; set; }
        public IList<Task> Tasks { get; }
        public IList<ModuleChain> ModuleChains { get; }
    }

    /// <summary>
    /// 任务
    /// </summary>
    public abstract class Task_Class: ICE_Class,INamedObject
    {
        public Module Module { get; set; }
    }

    public abstract class Task : ICE_Object
    {
        public Project Project { get; set; }
    }

    /// <summary>
    /// 资料实体类
    /// </summary>
    public abstract class Datum_Class : ICE_Class
    {

    }
    /// <summary>
    /// 资料
    /// </summary>
    public abstract class Datum : ICE_Object, INamedObject, ITree<Datum>
    {
        public int ID_Parent { get; set; }
        public Datum Parent { get; set; }
        public IList<Datum> Childs { get; }

        private NameExpression name;
        public string Name { get => name?.Analesize(this); set => name = value; }
        public string Illustration { get; set; }

        public string FullPath { get=>Path.Combine(Parent?.Name,Name); }
        public FileSystemInfo FileSystemInfo => Path.HasExtension(FullPath) ? (FileSystemInfo)new FileInfo(FullPath) : (FileSystemInfo)new DirectoryInfo(FullPath);
    }

    /// <summary>
    /// 命名表达式
    /// </summary>
    public abstract class NameExpression: INamedObject
    {
        private string name;
        public string Name { get => string.IsNullOrWhiteSpace(name) ? Expression : name; set => name = value; }
        public string Illustration { get; set; }
        public string Expression { get; set; }

        public abstract bool CheckFields();
        public abstract string Analesize(ICE_Object obj);

        public NameExpression(string expression)
        {
            this.Expression = expression;
        }

        public override string ToString()
        {
            return Expression;
        }
        public static implicit operator string(NameExpression nameExpression)
        {
            return nameExpression.ToString();
        }
        public static implicit operator NameExpression(string str)
        {
            Type type = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes()).First(type => type.IsSubclassOf(typeof(NameExpression)));
            ConstructorInfo con = type.GetConstructor(new Type[] { typeof(string) });
            return con.Invoke(new object[] { str }) as NameExpression;
        }
    }

    /// <summary>
    /// 命名规范表达式
    /// </summary>
    public abstract class NameRule: INamedObject
    {
        private string name;
        public string Name { get => string.IsNullOrWhiteSpace(name) ? Expression : name; set => name = value; }
        public string Illustration { get; set; }
        public string Expression { get; set; }

        public abstract bool CheckFields();
        public abstract bool Test(ICE_Object obj, string input);

        public NameRule(string expression)
        {
            this.Expression = expression;
        }

        public override string ToString()
        {
            return Expression;
        }
        public static implicit operator string(NameRule nameRule)
        {
            return nameRule.ToString();
        }
        public static implicit operator NameRule(string str)
        {
            Type type = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes()).First(type => type.IsSubclassOf(typeof(NameRule)));
            ConstructorInfo con = type.GetConstructor(new Type[] { typeof(string) });
            return con.Invoke(new object[] { str }) as NameRule;
        }
    }

    /// <summary>
    /// 职位实体类
    /// </summary>
    public abstract class Position_Class : ICE_Class
    {

    }
    /// <summary>
    /// 职位
    /// </summary>
    public abstract class Position : ICE_Object, INamedObject
    {
        public Team Team { get; set; }
        public string Name { get; set; }
        public string Illustration { get; set; }
        public IList<Obligation> Obligations { get; }
        public IList<Person> People { get; }
    }

    /// <summary>
    /// 用户实体类
    /// </summary>
    public abstract class Person_Class : ICE_Class
    {

    }
    /// <summary>
    /// 用户
    /// </summary>
    public abstract class Person : ICE_Object, INamedObject
    {
        public string Name { get; set; }
        public string Illustration { get; set; }
        public string Account { get; set; }
        public string Password { get; set; }
    }


    /// <summary>
    /// 职能
    /// </summary>
    public abstract class Obligation: INamedObject
    {
        public string Name { get; set; }
        public string Illustration { get; set; }
    }

    /// <summary>
    /// 模块关联
    /// </summary>
    public abstract class ModuleChain
    {
        public Process Process { get; set; }
        public Module SourceModule { get; set; }
        public Module TargetModule { get; set; }
        public ModuleChainType ModuleChainType { get; set; }
    }
    /// <summary>
    /// 模块关联模式：映射（一对一），父子（一对多），引用（多对多）
    /// </summary>
    public enum ModuleChainType { Parent, Quote, Mapping }

    /// <summary>
    /// 接口，具有名称和说明的对象
    /// </summary>
    public interface INamedObject
    {
        string Name { get; set; }
        string Illustration { get; set; }
    }

    /// <summary>
    /// 字段
    /// </summary>
    public abstract class Field:INamedObject
    {
        public string Name { get; set; }
        public string Illustration { get; set; }
        public string ClassCode { get; set; }
        public FieldType FieldType { get; set; }
        public FieldValueLimit FieldLimit { get; set; }
    }

    /// <summary>
    /// 字段类型：数字，文本，团队，职位，职能，用户，对象
    /// </summary>
    public enum FieldType { Number,Text,Team,Position,Obligation,User,Object}

    /// <summary>
    /// 数字范围限制：大于0，小于0，大于等于0，小于等于0，不为0
    /// </summary>
    public enum NumberRange { GTZ, LTZ, GOE, LOE, NE }
    /// <summary>
    /// 数字类型限制：整数，小数，百分比
    /// </summary>
    public enum NumberType { Integer,Decimal,Percentage }
    /// <summary>
    /// 输入方式：自由输入，单选列表，多选列表
    /// </summary>
    public enum InputMode { Free, Single, Multiple }

    /// <summary>
    /// 字段值限定
    /// </summary>
    public abstract class FieldValueLimit
    {
        public NumberRange NumberRange { get; set; }
        public NumberType NumberType { get; set; }
        public InputMode InputMode { get; set; }
        public bool Unique { get; set; }
        public int MaxChars { get; set; }
    }

    /// <summary>
    /// 接口：字段值
    /// </summary>
    public interface IFieldValue
    {
        public string BaseValue { get; set; }
        string DisplayValue { get; set; }
    }

    /// <summary>
    /// 结构：数字字段值
    /// </summary>
    public struct FieldValue_Number : IFieldValue
    {
        public string BaseValue { get; set; }
        public decimal Value { get; set; }
        public string DisplayValue { get; set; }
    }
    /// <summary>
    /// 结构：文本字段值
    /// </summary>
    public struct FieldValue_Text : IFieldValue
    {
        public string BaseValue { get; set; }
        public string Value { get; set; }
        public string DisplayValue { get; set; }
    }

    /// <summary>
    /// 字段值
    /// </summary>
    public abstract class FieldValue
    {
        public int ID_Field { get; set; }
        public string ClassCode { get; set; }
        public IFieldValue Value { get; set; }
    }
}
