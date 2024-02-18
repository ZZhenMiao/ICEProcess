using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace LiZhenStandard.CSharpCompiler
{
    public class CSharpCompiler
    {
        public string NameSpace { get; set; } = "NameSpace";
        public string ClassName { get; set; } = "Program";
        public string MethodName { get; set; } = "Main";
        public List<string> Usings { get; } = new List<string>() 
        {
            "System",
            "System.IO",
            "System.Linq",
            "System.Reflection",
            "System.Windows",
            "System.Diagnostics",
            "System.Collections.Generic",
            "System.Text",
            "System.Text.RegularExpressions",
            "System.Threading",
            "System.Threading.Tasks",
            "System.Windows.Controls",
            "System.Windows.Data",
            "System.Windows.Input",
            "System.Windows.Media",
            "System.Drawing",
            "System.Globalization",
            "Microsoft.Win32"
        };
        public List<Assembly> ReferencedAssemblies { get; } = new List<Assembly>()
        {
            typeof(object).Assembly,
            typeof(Console).Assembly,
            Assembly.Load(new AssemblyName("System.Runtime")),
        };
        public bool AddCurrentDomainAssemblies { get; set; } = true;
        public void CompileAction(string code)
        {
            _ = CompileFunciton(code, null);
        }
        public object CompileFunciton(string code, params object[] args)
        {
            var assembly = Compile(code);
            if (assembly is null)
            {
                Console.WriteLine($"编译失败！");
                return null;
            }
            var personType = assembly?.GetType($"{NameSpace}.{ClassName}");
            if (personType != null)
            {
                var method = personType.GetMethod(MethodName);
                if (method != null)
                {
                    try
                    {
                        return method?.Invoke(null, new object[] { args });
                    }catch (Exception ex) 
                    {
                        Console.WriteLine("已编译的程序在执行过程中因遇到错误而被中止！");
                        Console.WriteLine(ex.ToString()); return null; 
                    }
                }
                else
                {
                    Console.WriteLine($"找不到方法:{NameSpace}.{ClassName}.{MethodName}");
                    return null;
                }
            }
            else
            { 
                Console.WriteLine($"找不到类:{NameSpace}.{ClassName}");
                return null;
            }
        }
        public Assembly Compile(string code)
        {
            var usingStr = string.Empty;
            foreach (var us in Usings)
            {
                usingStr += "using " + us + ";" + "\r\n";
            }
            var inCode = string.Format("{0}\r\nnamespace {1}\r\n{{\r\npublic class {2}\r\n{{\r\npublic static object {3}(object[] args)\r\n{{\r\n{4}\r\nreturn null;}}\r\n}}\r\n}}", usingStr, NameSpace, ClassName, MethodName, code);
            var Assemblies = new List<Assembly>();
            Assemblies.AddRange(ReferencedAssemblies);
            if (AddCurrentDomainAssemblies)
                Assemblies.AddRange(AppDomain.CurrentDomain.GetAssemblies());
            return PreCompile(inCode, Assemblies.ToArray());
        }
        public static Assembly PreCompile(string code, params Assembly[] referencedAssemblies)
        {
            var references = referencedAssemblies.Select(it => MetadataReference.CreateFromFile(it.Location));
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var assemblyName = "_" + Guid.NewGuid().ToString("D");
            var syntaxTrees = new SyntaxTree[] { CSharpSyntaxTree.ParseText(code) };
            var compilation = CSharpCompilation.Create(assemblyName, syntaxTrees, references, options);
            using (var stream = new MemoryStream())
            {
                var compilationResult = compilation.Emit(stream);
                if (compilationResult.Success)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    return Assembly.Load(stream.ToArray());
                }
                else
                {
                    foreach (var item in compilationResult.Diagnostics)
                    {
                        Console.WriteLine(item);
                    }
                    return null;
                }
            }
        }
    }
}
