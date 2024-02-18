using LiZhenMySQL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ICE_Model
{
    /// <summary>
    /// 命名表达式
    /// </summary>
    public class NameExpression : DbNamedObject
    {
        private string name;
        public override string Name { get => string.IsNullOrWhiteSpace(name) ? Expression : name; set => name = value; }
        public string Expression { get; set; }

        public bool CheckFields() 
        {
            throw new NotImplementedException();
        }
        public string Analesize(Project project)
        {
            throw new NotImplementedException();
        }

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
    public class NameRule : DbNamedObject
    {
        private string name;
        public override string Name { get => string.IsNullOrWhiteSpace(name) ? Expression : name; set => name = value; }
        public string Expression { get; set; }

        public bool CheckFields() 
        {
            throw new NotImplementedException();
        }
        public bool Test(Project pj, string input) 
        {
            throw new NotImplementedException();
        }

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
}
