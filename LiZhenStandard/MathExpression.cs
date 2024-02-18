using Microsoft.VisualBasic;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace LiZhenStandard
{
    public class MathExpression
    {
        object instance;
        MethodInfo method;
        public MathExpression(string expression)
        {
            if (expression.ToUpper(CultureInfo.InvariantCulture).IndexOf("RETURN") < 0) expression = "Return " + expression;
            string className = "Expression";
            string methodName = "Compute";
            CompilerParameters p = new CompilerParameters();
            p.GenerateInMemory = true;
            CompilerResults cr = new VBCodeProvider().CompileAssemblyFromSource
            (
              p,
              string.Format
              (
                @"Option Explicit Off
                Option Strict Off
                Imports System, System.Math, Microsoft.VisualBasic
                NotInheritable Class {0}
                Public Function {1}(x As Double) As Double
                {2}
                End Function
                End Class",
                className, methodName, expression
              )
            );
            if (cr.Errors.Count > 0)
            {
                string msg = "Expression(" + expression + "): \n";
                foreach (CompilerError err in cr.Errors) 
                    msg += err.ToString() + "\n";
                throw new Exception(msg);
            }
            instance = cr.CompiledAssembly.CreateInstance(className);
            method = instance.GetType().GetMethod(methodName);
        }
        public double Compute(double x)
        {
            return (double)method.Invoke(instance, new object[] { x });
        }
    }
}
