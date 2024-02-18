using System;
using System.Text;
using LiZhenStandard.Extensions;

namespace ICE_Model
{
    /// <summary>
    /// 命名规范表达式
    /// </summary>
    public abstract class NameRule : NamedObject
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


}
