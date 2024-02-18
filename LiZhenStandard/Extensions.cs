using LiZhenStandard;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace LiZhenStandard.Extensions
{
    public static class Tree_Extensons
    {
        /// <summary>
        /// 根据属性名称和值，查找树型结构的某一节点。
        /// </summary>
        /// <typeparam name="T">树类型</typeparam>
        /// <param name="tree">要查找的树结构</param>
        /// <param name="propertyName">属性名</param>
        /// <param name="propertyValue">属性值</param>
        /// <returns>找到的节点</returns>
        public static T GetTreeNode<T>(this ITree<T> tree, string propertyName, object propertyValue) where T : ITree<T>
        {
            bool? finded = tree?.GetPropertyValue(propertyName)?.Equals(propertyValue);
            T re = default;
            if (finded is null || finded == false)
                foreach (var item in tree)
                {
                    re = item.GetTreeNode(propertyName, propertyValue);
                    if (re != null)
                        return re;
                }
            else
            {
                return (T)tree;
            }
            return re;
        }
        /// <summary>
        /// 返回一个树型结构的所有节点数组，将忽略树型结构。
        /// </summary>
        /// <typeparam name="T">树类型</typeparam>
        /// <param name="tree">树型对象</param>
        /// <returns>所有节点数组</returns>
        public static T[] GetAllTreeNode<T>(this ITree<T> tree,bool withSelf = true)
        {
            List<T> re = new List<T>();
            if (withSelf)
                re.Add((T)tree);
            for (int i = 0; i < tree.Count(); i++)
            {
                T node = tree[i];
                re.Add(node);
                ITree<T> nodet = (ITree<T>)node;
                if (nodet.Any())
                { 
                    re.AddRange(nodet.GetAllTreeNode<T>(false));
                }
            }
            return re.ToArray();
        }
    }

    public static class Collection_Extensons
    {
        /// <summary>
        /// 判断一个集合是否有且只有这一个值
        /// </summary>
        /// <typeparam name="T">集合类型</typeparam>
        /// <param name="array">集合</param>
        /// <param name="theOne">唯一值</param>
        /// <returns>是否只有这一个值</returns>
        public static bool OnlyOne<T>(this Array array, out T theOne)
        {
            theOne = default;
            if (array.Length == 1)
            {
                theOne = (T)array.GetValue(0);
                return true;
            }
            else
                return false;
        }
        /// <summary>
        /// 将一个集合中所有对象全部转为字符串，并连接成一个字符串
        /// </summary>
        /// <typeparam name="T">集合类型</typeparam>
        /// <param name="enumerable">集合</param>
        /// <param name="propertyOrMethodName">要展示的属性或方法</param>
        /// <param name="separator">分隔符</param>
        /// <param name="forDataBase">以数据库格式表达</param>
        /// <returns></returns>
        public static string AllToString<T>(this IEnumerable<T> enumerable, string propertyOrMethodName = null, string separator = ",", bool forDataBase = false)
        {
            string re = "";
            for (int i = 0; i < enumerable.Count(); i++)
            {
                string str = "";
                if (string.IsNullOrWhiteSpace(propertyOrMethodName))
                    str = enumerable.ElementAt(i)?.ToString();
                else
                {
                    if (typeof(T).GetProperty(propertyOrMethodName) is null)
                    {
                        MethodInfo m = null;
                        IEnumerable<MethodInfo> ms = typeof(T).GetMethods().Where(a => { return a.Name == propertyOrMethodName && a.GetParameters().Length == 0; });
                        if (ms.Count() == 0)
                            ms = Reflection_Extensons.GetExtensionMethods(typeof(T)).Where(a => { return a.Name == propertyOrMethodName; });
                        if (ms.Count() != 0)
                            m = ms.ElementAt(0);
                        str = m.IsStatic ? m.Invoke(enumerable.ElementAt(i), new object[] { enumerable.ElementAt(i) }).ToString() : m.Invoke(enumerable.ElementAt(i), null).ToString();
                    }
                    else
                        str = typeof(T).GetProperty(propertyOrMethodName).GetValue(enumerable.ElementAt(i)).ToString();
                }
                if (forDataBase)
                {
                    object emt = enumerable.ElementAt(i);
                    if (emt is null)
                        str = "null";
                    if (emt is string || emt is char)
                        str = string.Format("\"{0}\"", str);
                    if (emt is DateTime)
                        str = string.Format("\'{0}\'", str);
                }
                if (i != enumerable.Count() - 1)
                    str += separator;
                //if (i != enumerable.Count() - 1)
                //    if (i + 1 < enumerable.Count())
                //    {
                //        object elm = enumerable.ElementAt(i + 1);
                //        if (!(elm is null))
                //            str += separator;
                //    }
                //    else
                //        str += separator;
                re += str;
            }
            return re;
        }
        /// <summary>
        /// 在一个集合中通过属性值查找一个对象
        /// </summary>
        /// <typeparam name="T">集合类型</typeparam>
        /// <param name="enumerable">要查找的集合</param>
        /// <param name="propertyName">要查找的属性名</param>
        /// <param name="propertyValue">要查找的值</param>
        /// <returns>被查找出的对象</returns>
        public static T FindByProperty<T>(this IEnumerable<T> enumerable, string propertyName, object propertyValue)
        {
            if (enumerable is null || string.IsNullOrWhiteSpace(propertyName) || propertyValue is null)
                return default;
            var t = typeof(T);
            var property = t.GetProperty(propertyName);
            if (property is null)
                return default;
            for (int i = 0; i < enumerable.Count(); i++)
            {
                object element = enumerable.ElementAt(i);
                if (property.GetValue(element)?.Equals(propertyValue) == true)
                    return (T)element;
            }
            return default;
        }
        /// <summary>
        /// 在一个集合中通过属性值查找全部对象
        /// </summary>
        /// <typeparam name="T">集合类型</typeparam>
        /// <param name="enumerable">要查找的集合</param>
        /// <param name="propertyName">要查找的属性名</param>
        /// <param name="propertyValue">要查找的值</param>
        /// <returns>被查找出的对象集合</returns>
        public static T[] FindAllByProperty<T>(this IEnumerable<T> enumerable, string propertyName, object propertyValue)
        {
            if (enumerable is null || string.IsNullOrWhiteSpace(propertyName) || propertyValue is null)
                return default;
            var t = typeof(T);
            var property = t.GetProperty(propertyName);
            if (property is null)
                return default;

            List<T> re = new List<T>();
            for (int i = 0; i < enumerable.Count(); i++)
            {
                object element = enumerable.ElementAt(i);
                if (property.GetValue(element) == propertyValue)
                    re.Add((T)element);
            }
            return re.ToArray();
        }
        /// <summary>
        /// 转换一个集合中的所有元素到另一个集合
        /// </summary>
        /// <typeparam name="InputT">输入的集合类型</typeparam>
        /// <typeparam name="ResultT">返回的集合类型</typeparam>
        /// <param name="input">输入的集合</param>
        /// <param name="convertFunc">转换方法</param>
        /// <returns>转换之后的集合</returns>
        public static ResultT[] ConvertAll<InputT, ResultT>(this IEnumerable<InputT> input, Func<InputT, ResultT> convertFunc)
        {
            ResultT[] re = new ResultT[input.Count()];
            for (int i = 0; i < input.Count(); i++)
            {
                re[i] = convertFunc(input.ElementAt(i));
            }
            return re;
        }
        /// <summary>
        /// 判断一个集合中是否包含一个组中的全部对象
        /// </summary>
        /// <param name="from">源集合</param>
        /// <param name="ts">要查找的对象</param>
        /// <returns>是否包含一个组中的全部对象</returns>
        public static bool AllContains<T>(this IEnumerable<T> from, params T[] ts)
        {
            if (ts.Length == 0 || from.Count() == 0)
                return false;

            bool finded = true;
            for (int i = 0; i < ts.Length; i++)
            {
                if (!from.Contains(ts[i]))
                { finded = false; break; }
            }
            return finded;
        }
        /// <summary>
        /// 判断一个集合中是否包含一个组中的任意对象
        /// </summary>
        /// <param name="from">源集合</param>
        /// <param name="ts">要查找的对象</param>
        /// <returns>是否包含一个组中的任意对象</returns>
        public static bool AnyContains<T>(this IEnumerable<T> from, params T[] ts)
        {
            if (ts.Length == 0 || from.Count() == 0)
                return false;

            bool finded = false;
            for (int i = 0; i < ts.Length; i++)
            {
                Debug.WriteLine(ts[i]);
                if (from.Contains(ts[i]))
                {
                    finded = true;
                    break; 
                }
            }
            return finded;
        }

        /// <summary>
        /// 判断一个集合是否为空或空集合
        /// </summary>
        /// <typeparam name="T">集合类型</typeparam>
        /// <param name="ts">集合</param>
        /// <returns>是否为空或空集合</returns>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> ts)
        {
            if (ts is null)
                return true;
            else if (ts.Count() == 0)
                return true;
            else
                return false;
        }
        /// <summary>
        /// 判断一个条件集合是否全部为真
        /// </summary>
        /// <param name="vs">条件集合</param>
        /// <returns>是否全部为真</returns>
        public static bool JudgeAll(params bool[] vs)
        {
            for (int i = 0; i < vs.Length; i++)
            {
                if (!vs[i])
                    return false;
            }
            return true;
        }
        public static void AddRange<T>(this ObservableCollection<T> ts,IEnumerable<T> ts_in)
        {
            if (ts_in is null)
                return;
            foreach (var item in ts_in)
            {
                ts?.Add(item);
            }
        }
        public static void AddRange<T>(this ConcurrentBag<T> ts, IEnumerable<T> ts_in,bool multithreading = true) 
        {
            if (ts_in is null)
                return;
            if (multithreading)
                Parallel.ForEach(ts_in, item =>
                {
                    ts.Add(item);
                });
            else
                foreach (T item in ts_in)
                {
                    ts.Add(item);
                }
        }
    }

    public static class Serialization_Extensons
    {
        public static void XmlSerializer(this object obj, string filePath)
        {
            using (StringWriter sw = new StringWriter())
            {
                XmlSerializer xz = new XmlSerializer(obj.GetType());
                xz.Serialize(sw, obj);
                File.WriteAllText(filePath, sw.ToString());
            }
        }
        public static T XmlDeserializer<T>(string filePath)
        {
            var str = File.ReadAllText(filePath);
            using (StringReader sr = new StringReader(str))
            {
                XmlSerializer xz = new XmlSerializer(typeof(T));
                return (T)xz.Deserialize(sr);
            }
        }

        public static byte[] StreamToBytes(this Stream stream)
        {
            //byte[] bytes = new byte[stream.Length];
            //stream.Read(bytes, 0, bytes.Length);
            //stream.Seek(0, SeekOrigin.Begin);
            //return bytes;
            return (stream as MemoryStream).ToArray();
        }
        public static byte[] Serialize(this object obj)
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream stream = new MemoryStream();
            bf.Serialize(stream, obj);
            return stream.StreamToBytes();
        }
        public static T Deserialize<T>(this byte[] bytes)
        {
            BinaryFormatter bf = new BinaryFormatter();
            object obj;
            try
            {
                obj = bf.Deserialize(new MemoryStream(bytes));
            }
            catch { return default; }
            return (T)obj;
        }
        public static string SerializeToString(this object obj)
        {
            byte[] bytes = obj.Serialize();
            string str = Encoding.Unicode.GetString(bytes);
            return str;
        }
        public static T DeserializeFromString<T>(this string str)
        {
            byte[] byteArray = Encoding.Unicode.GetBytes(str);
            T obj = Deserialize<T>(byteArray);
            return obj;
        }
        public static bool SerializeToFile(this object obj, string filePath, out Exception ex)
        {
            ex = null;
            FileInfo file = new FileInfo(filePath);
            try
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream fs = new FileStream(file.FullName, FileMode.OpenOrCreate);
                bf.Serialize(fs,obj);
                fs.Close();

                //file.Write(obj.SerializeToString(),Encoding.Unicode);
                return true;
            }
            catch (Exception e)
            {
                ex = e; return false;
            }
        }
        public static T DeserializeFromFile<T>(string filePath,out Exception ex)
        {
            ex = null;
            FileInfo file = new FileInfo(filePath);
            try
            {
                //var str = File.ReadAllText(file.FullName, Encoding.Unicode);
                BinaryFormatter bf = new BinaryFormatter();
                FileStream fs = new FileStream(file.FullName,FileMode.OpenOrCreate);
                object re = bf.Deserialize(fs);
                fs.Close();
                return (T)re;
                //return DeserializeFromString<T>(str);
            }
            catch (Exception e)
            {
                ex = e; return default(T);
            }
        }
    }

    public static class IO_Extensons
    {
        private static readonly string[] suffixes = new string[] { " B", " KB", " MB", " GB", " TB", " PB" };
        /// <summary>
        /// 获取文件大小的显示字符串
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static string BytesToReadableValue(this long number)
        {
            double last = 1;
            for (int i = 0; i < suffixes.Length; i++)
            {
                var current = Math.Pow(1024, i + 1);
                var temp = number / current;
                if (temp < 1)
                {
                    return (number / last).ToString("n2") + suffixes[i];
                }
                last = current;
            }
            return number.ToString();
        }
        public static DirectoryInfo CreateDirectory(string path)
        {
            var parent = Path.GetDirectoryName(path);
            if (!Directory.Exists(parent))
                CreateDirectory(parent);
            if (!Directory.Exists(path))
                return Directory.CreateDirectory(path);
            else
                return new DirectoryInfo(path);
        }
        public static void Write(this FileInfo file, string content, Encoding encoding)
        {
            var fullpath = file.FullName;
            _ = CreateDirectory(Path.GetDirectoryName(fullpath));
            File.WriteAllText(fullpath, content, encoding);
        }
        public static void Write(this FileInfo file, string content)
        {
            Write(file, content, Encoding.Unicode);
        }
        public static void WriteLine(this FileInfo file, IEnumerable<string> lines)
        {
            StreamWriter sw = new StreamWriter(file.FullName, true);
            foreach (var line in lines)
            {
                sw.WriteLine(line);
            }
            sw.Close();
        }
        public static string GetNextSequenceFile(string directory, string fileName, string fileExtension = ".txt", string indexFormat = "00", int maxIndex = int.MaxValue)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(directory);
            if (!directoryInfo.Exists)
                CreateDirectory(directory);
            FileInfo[] files = new DirectoryInfo(directoryInfo.FullName).GetFiles($"{fileName}_*{fileExtension}", SearchOption.TopDirectoryOnly);
            string lastFilePath;
            int index = 1;
            if (files.Any())
            {
                Array.Sort(files, (a, b) =>
                {
                    return a.LastWriteTime > b.LastWriteTime ? 1 : -1;
                });
                lastFilePath = files.LastOrDefault().FullName;

                string Name = Path.GetFileNameWithoutExtension(lastFilePath);
                string id_str = Name.Replace($"{fileName}_", string.Empty);
                int.TryParse(id_str, out index);
                if (index < maxIndex)
                    index++;
                else
                    index = 1;
            }
            lastFilePath = Path.Combine(directory, $"{fileName}_{index.ToString(indexFormat)}{fileExtension}");
            return lastFilePath;
        }
        public static void WriteTextLogFiles(string logDirectory, string logFileName, IEnumerable<string> logs, int fileLimitKB = 1024 * 3)
        {
            DirectoryInfo directoryInfo= new DirectoryInfo(logDirectory);
            if (!directoryInfo.Exists)
                CreateDirectory(logDirectory);
            string[] files = Directory.GetFiles(directoryInfo.FullName,$"{logFileName}_*.txt");
            Array.Sort(files);
            var lastFilePath = files.LastOrDefault();
            int index = 1;

            FileInfo lastFile;
            if (lastFilePath != null)
            {
                do
                {
                    lastFile = new FileInfo(lastFilePath);
                    if (lastFile.Length > fileLimitKB * 1024)
                    {
                        string Name = Path.GetFileNameWithoutExtension(lastFile.FullName);
                        string id_str = Name.Replace($"{logFileName}_", string.Empty);
                        int.TryParse(id_str, out index);
                        index = index + 1;
                    }
                    lastFile = new FileInfo(Path.Combine(logDirectory, $"{logFileName}_{index}.txt"));
                    lastFilePath = lastFile.FullName;
                } while (lastFile.Exists && lastFile.Length > fileLimitKB * 1024);
            }
            else
            {
                lastFile = new FileInfo(Path.Combine(logDirectory, $"{logFileName}_{index}.txt"));
            }

            //if (!(lastFile is null))
            //{
            //    if (lastFile.Length < fileLimitKB * 1024)
            //    {
            //        lastFile.WriteLine(logs);
            //        return;
            //    }
            //    else
            //    {
            //        string Name = Path.GetFileNameWithoutExtension(lastFile.FullName);
            //        string id_str = Name.Replace($"{logFileName}_", string.Empty);
            //        int.TryParse(id_str, out index);
            //        index = index + 1;
            //        lastFile = new FileInfo(Path.Combine(logDirectory, $"{logFileName}_{index}.txt"));
            //    }
            //}

            lastFile.WriteLine(logs);
        }

        public static string RoboCopy(this DirectoryInfo source, string target, string file = "*.*", string args = "/XO /R:0 /NJH /NJS /NFL /NDL /NP /MT")
        {
            if (string.IsNullOrEmpty(file))
                file = "*.*";
            if (string.IsNullOrEmpty(args))
                args = "/XO /R:0 /NJH /NJS /NFL /NDL /NP /MT";

            var sourcePath = source.FullName;
            string argument = $"{args} \"{sourcePath}\" \"{target}\" {file}";
            Console.WriteLine($"从：{sourcePath} 复制到：{target} 文件：{file} 参数：{args}");
            Process process = new Process();
            var StartInfo = new ProcessStartInfo();
            process.StartInfo = StartInfo;
            StartInfo.FileName = "RoboCopy.exe";
            StartInfo.UseShellExecute = false;
            StartInfo.CreateNoWindow = true;
            StartInfo.Arguments = argument;
            StartInfo.RedirectStandardError = true;
            StartInfo.RedirectStandardOutput = true;
            process.Start();
            process?.WaitForExit();
            var op = process.StandardOutput.ReadToEnd();
            var ex = process.StandardError.ReadToEnd();
            return ex +"\r\n"+ op;
        }
    }

    public static class Reflection_Extensons
    {
        /// <summary>
        /// 获得扩展方法
        /// </summary>
        /// <param name="extendedType">扩展方法的返回类型</param>
        /// <returns>找到的扩展方法集合</returns>
        public static IEnumerable<MethodInfo> GetExtensionMethods(Type extendedType)
        {
            return from type in Assembly.GetExecutingAssembly().GetTypes()
                   where !type.IsGenericType && !type.IsNested
                   from method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                   where method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
                   where method.GetParameters()[0].ParameterType == extendedType
                   select method;
        }
        /// <summary>
        /// 尝试为一个属性赋值
        /// </summary>
        /// <param name="propertyInfo">属性对象</param>
        /// <param name="obj">要赋值的对象</param>
        /// <param name="value">要赋予的值</param>
        public static void SetValuePlus(this PropertyInfo propertyInfo, object obj, object value)
        {
            Type tProp = propertyInfo.PropertyType;
            if (value is null)
                return;
            Type tValue = value.GetType();
            if (tProp != tValue)
            {
                string v = value.ToString();
                if (tProp == typeof(string))
                    propertyInfo.SetValue(obj, v);
                else if (tProp == typeof(bool))
                    propertyInfo.SetValue(obj, bool.Parse(v));
                else if (tProp == typeof(int))
                    propertyInfo.SetValue(obj, int.Parse(v));
                else if (tProp == typeof(double))
                    propertyInfo.SetValue(obj, double.Parse(v));
                else if (tProp == typeof(long))
                    propertyInfo.SetValue(obj, long.Parse(v));
                else if (tProp == typeof(float))
                    propertyInfo.SetValue(obj, float.Parse(v));
                else if (tProp == typeof(decimal))
                    propertyInfo.SetValue(obj, decimal.Parse(v));
                else if (tProp == typeof(DateTime))
                    propertyInfo.SetValue(obj, DateTime.Parse(v));
                else if (tProp == typeof(char))
                    propertyInfo.SetValue(obj, char.Parse(v));
                else
                {
                    try
                    {
                        propertyInfo.SetValue(obj, v);
                    }
                    catch { }
                }
            }
            else
                propertyInfo.SetValue(obj, value);
        }
        /// <summary>
        /// 通过反射执行一个对象中的方法
        /// </summary>
        /// <param name="obj">对象</param>
        /// <param name="methodName">方法名称</param>
        /// <param name="pars">方法的参数</param>
        /// <returns>返回值</returns>
        public static object RefExecuteMethod(this object obj, string methodName, params object[] pars)
        {
            if (obj is null || string.IsNullOrWhiteSpace(methodName))
                return null;
            Type[] parTypes = Type.EmptyTypes;
            if (pars.IsNullOrEmpty())
                parTypes = (from par in pars select par.GetType()).ToArray();
            return obj.GetType()?.GetMethod(methodName, parTypes)?.Invoke(obj, parTypes);
        }
        /// <summary>
        /// 获取一个对象的某个属性值
        /// </summary>
        /// <param name="obj">要获取值的对象</param>
        /// <param name="propertyName">要获取的属性名</param>
        /// <returns>属性的值</returns>
        public static object GetPropertyValue(this object obj, string propertyName)
        {
            var allppts = obj.GetType().GetProperties();
            var lowname = propertyName.ToLower();
            var ppts = allppts.Where(a => { return a.Name.ToLower() == lowname; });
            if (ppts.Count() > 0)
                return ppts.ElementAt(0).GetValue(obj);
            else
                return null;
        }
        /// <summary>
        /// 获取一个对象的某个属性值
        /// </summary>
        /// <param name="obj">要获取值的对象</param>
        /// <param name="propertyName">要获取的属性名</param>
        /// <returns>属性的值</returns>
        public static T GetPropertyValue<T>(this object obj, string propertyName)
        {
            object re = obj.GetPropertyValue(propertyName);
            if (!(re is null))
                if (typeof(T).IsAssignableFrom(re.GetType()))
                    return (T)re;
            return default;
        }
        /// <summary>
        /// 设置一个对象的值
        /// </summary>
        /// <param name="obj">要设置的对象</param>
        /// <param name="propertyName">属性名</param>
        /// <param name="value">要赋予的值</param>
        public static void SetPropertyValue(this object obj, string propertyName, object value)
        {
            var allppts = obj.GetType().GetProperties();
            var lowname = propertyName.ToLower();
            var ppts = allppts.Where(a => { return a.Name.ToLower() == lowname; });
            ppts.FirstOrDefault()?.SetValuePlus(obj, value);
        }
        /// <summary>
        /// 判断指定的类型 <paramref name="type"/> 是否是指定泛型类型的子类型，或实现了指定泛型接口。
        /// </summary>
        /// <param name="type">需要测试的类型。</param>
        /// <param name="generic">泛型接口类型，传入 typeof(IXxx&lt;&gt;)</param>
        /// <returns>如果是泛型接口的子类型，则返回 true，否则返回 false。</returns>
        public static bool HasImplementedRawGeneric(this Type type, Type generic)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (generic == null) throw new ArgumentNullException(nameof(generic));

            // 测试接口。
            var isTheRawGenericType = type.GetInterfaces().Any(IsTheRawGenericType);
            if (isTheRawGenericType) return true;

            // 测试类型。
            while (type != null && type != typeof(object))
            {
                isTheRawGenericType = IsTheRawGenericType(type);
                if (isTheRawGenericType) return true;
                type = type.BaseType;
            }

            // 没有找到任何匹配的接口或类型。
            return false;

            // 测试某个类型是否是指定的原始接口。
            bool IsTheRawGenericType(Type test)
                => generic == (test.IsGenericType ? test.GetGenericTypeDefinition() : test);
        }
        /// <summary>
        /// 显示一个对象所有属性的值
        /// </summary>
        /// <param name="obj">要显示的对象</param>
        /// <returns>所有属性值的字符串</returns>
        public static string ShowAllProperties(this object obj)
        {
            var re = "";
            var all = obj.GetType().GetProperties();
            foreach (var item in all)
            {
                var n = item.Name;
                var v = item.GetValue(obj);
                Debug.WriteLine(n + " = " + v);
                string vstr = v?.ToString();
                if (v != null)
                    if (HasImplementedRawGeneric(v.GetType(), typeof(IEnumerable<>)))
                    {
                        foreach (var itm in (IEnumerable)v)
                        {
                            vstr += item.ToString() + ",";
                        }
                        if (vstr != null)
                            if (vstr.Length > 0)
                            {
                                vstr.Substring(0, vstr.Length - 1);
                            }
                    }
                re += string.Format("{0} = {1}\n", n, v);
            }
            Debug.WriteLine(re);
            return re;
        }
        /// <summary>
        /// 判断一个对象是否不为空（纯粹是为了方便而已）
        /// </summary>
        /// <param name="obj">对象</param>
        /// <returns>是否不为空</returns>
        public static bool IsNotNull(this object obj)
        {
            return !(obj is null);
        }
        /// <summary>
        /// 将一个对象按照特定的格式转换为其字符串表示形式，前提是该对象必须支持这种格式。
        /// </summary>
        /// <param name="obj">要转换的对象</param>
        /// <param name="pars">ToString的参数</param>
        /// <returns>转换后的字符串</returns>
        public static string ToString(this object obj, params string[] pars)
        {
            IEnumerable<MethodInfo> ms = obj.GetType().GetMethods().Where(a => { return a.Name == "ToString" && a.GetParameters().Length == pars.Length; });
            if (ms.Count() > 0)
            {
                for (int i = 0; i < ms.Count(); i++)
                {
                    bool err = false;
                    MethodInfo m = ms.ElementAt(i);
                    ParameterInfo[] mps = m.GetParameters();
                    for (int n = 0; n < mps.Length; n++)
                    {
                        var ptype = mps[n].ParameterType;
                        if (mps[n].ParameterType == typeof(string))
                            continue;
                        if (String_Extensons.Parse(mps[n].ParameterType, pars[n]) == null)
                        {
                            err = true;
                            break;
                        }
                    }
                    if (err)
                        continue;
                    else
                    {
                        return m.Invoke(obj, pars).ToString();
                    }
                }
            }
            return string.Empty;
        }

    }

    public static class Path_Extensons
    {
        /// <summary>
        /// 判断字符串是否可以用于文件名和路径
        /// </summary>
        /// <param name="str">要判断的字符串</param>
        /// <returns>可以则为True，否则为False</returns>
        public static bool CanUseToFileName(this string str)
        {
            char[] cannotchars = new char[Path.GetInvalidFileNameChars().Length + Path.GetInvalidPathChars().Length];
            Path.GetInvalidFileNameChars().CopyTo(cannotchars, 0);
            Path.GetInvalidPathChars().CopyTo(cannotchars, Path.GetInvalidFileNameChars().Length);
            return !cannotchars.AllContains(str.ToCharArray());
        }
        /// <summary>
        /// 将一个目录对象合并为一个新的路径
        /// </summary>
        /// <param name="directory">目录对象</param>
        /// <param name="names">路径名</param>
        /// <returns>新路径字符串</returns>
        private static string Combine(DirectoryInfo directory, params string[] names)
        {
            string re = directory.FullName;
            for (int i = 0; i < names.Length; i++)
            {
                re = Path.Combine(re, names[i]);
            }
            return re;
        }
        /// <summary>
        /// 合并为一个目录对象
        /// </summary>
        /// <param name="directory">目录对象</param>
        /// <param name="names">目录名</param>
        /// <returns>目录对象</returns>
        public static DirectoryInfo CombineToDir(this DirectoryInfo directory, params string[] names)
        {
            return new DirectoryInfo(Combine(directory, names));
        }
        /// <summary>
        /// 合并为一个文件对象
        /// </summary>
        /// <param name="directory">目录对象</param>
        /// <param name="names">文件名</param>
        /// <returns>文件对象</returns>
        public static FileInfo CombineToFile(this DirectoryInfo directory, params string[] names)
        {
            return new FileInfo(Combine(directory, names));
        }
        /// <summary>
        /// 获取一个文件的MD5值
        /// </summary>
        /// <param name="fileInfo">文件对象</param>
        /// <returns>MD5值字符串</returns>
        public static string GetMD5(this FileInfo fileInfo)
        {
            return GetMD5FromFile(fileInfo.FullName);
        }
        /// <summary>
        /// 获取一个文件的MD5值
        /// </summary>
        /// <param name="fileName">文件全名</param>
        /// <returns>MD5值字符串</returns>
        public static string GetMD5FromFile(string fileName)
        {
            try
            {
                FileStream file = new FileStream(fileName, FileMode.Open);
                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(file);
                file.Close();

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("GetMD5HashFromFile() fail,error:" + ex.Message);
            }
        }
        /// <summary>
        /// 获取共享目录分隔符,或将给定的地址加上分隔符返回
        /// </summary>
        /// <param name="address">地址</param>
        /// <returns>分隔符或加上分隔符的地址</returns>
        public static string SharedVolumeSeparator_Win(string address = null)
        {
            string sep = Path.DirectorySeparatorChar.ToString() + Path.DirectorySeparatorChar.ToString();
            if (address is null)
                return sep;
            else
                return sep + address;
        }
    }

    public static class String_Extensons
    {
        /// <summary>
        /// 创建字符串加密的Key
        /// </summary>
        /// <returns>Key</returns>
        public static string GenerateKey()
        {
            DES desCrypto = DES.Create();
            return ASCIIEncoding.ASCII.GetString(desCrypto.Key);
        }
        /// <summary>
        /// 通过Key加密字符串
        /// </summary>
        /// <param name="sInputString">输入的字符串</param>
        /// <param name="sKey">Key</param>
        /// <returns>已加密的字符串</returns>
        public static string EncryptString(this string sInputString, string sKey)
        {
            byte[] data = Encoding.UTF8.GetBytes(sInputString);
            DESCryptoServiceProvider DES = new DESCryptoServiceProvider();
            char[] keychar = new char[8];
            for (int i = 0; i < 8; i++)
            {
                if (i < sKey.Length)
                    keychar[i] = sKey[i];
                else
                    keychar[i] = '0';
            }
            string key = keychar.AllToString(separator:"");
            DES.Key = Encoding.ASCII.GetBytes(key);
            DES.IV = DES.Key;
            ICryptoTransform desencrypt = DES.CreateEncryptor();
            byte[] result = desencrypt.TransformFinalBlock(data, 0, data.Length);
            return BitConverter.ToString(result);
        }
        /// <summary>
        /// 通过Key解密字符串
        /// </summary>
        /// <param name="sInputString">要解密的字符串</param>
        /// <param name="sKey">Key</param>
        /// <returns>已解密的字符串</returns>
        public static string DecryptString(this string sInputString, string sKey)
        {
            string[] sInput = sInputString.Split("-".ToCharArray());
            byte[] data = new byte[sInput.Length];
            for (int i = 0; i < sInput.Length; i++)
            {
                data[i] = byte.Parse(sInput[i], NumberStyles.HexNumber);
            }
            DESCryptoServiceProvider DES = new DESCryptoServiceProvider();
            char[] keychar = new char[8];
            for (int i = 0; i < 8; i++)
            {
                if (i < sKey.Length)
                    keychar[i] = sKey[i];
                else
                    keychar[i] = '0';
            }
            string key = keychar.AllToString(separator: "");
            DES.Key = Encoding.ASCII.GetBytes(key);
            DES.IV = Encoding.ASCII.GetBytes(key);
            ICryptoTransform desencrypt = DES.CreateDecryptor();
            byte[] result = desencrypt.TransformFinalBlock(data, 0, data.Length);
            return Encoding.UTF8.GetString(result);
        }
        /// <summary>
        /// 移除一个字符串数组中的所有空白和空值
        /// </summary>
        /// <param name="vs">要移除的字符串数组</param>
        /// <returns>整理后的字符串数组</returns>
        public static string[] RemoveEmptyOrSpace(this string[] vs)
        {
            List<string> re = new List<string>();
            for (int i = 0; i < vs.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(vs[i]))
                    re.Add(vs[i]);
            }
            return re.ToArray();
        }
        /// <summary>
        /// 给字符串添加一行
        /// </summary>
        /// <param name="sourceStr">源字符串</param>
        /// <param name="newStr">要添加的字符串</param>
        /// <returns>新字符串</returns>
        public static string AddLine(this string sourceStr, string newStr, params object[] args)
        {
            return sourceStr += string.Format(newStr, args) + "\n";
        }
        /// <summary>
        /// 尝试将一个字符串转换为相应的类型
        /// </summary>
        /// <param name="type">要转换的类型</param>
        /// <param name="str">要转换的字符串</param>
        /// <returns>转换后的对象</returns>
        public static object Parse(Type type, string str)
        {
            try
            {
                MethodInfo ms = type.GetMethods().FirstOrDefault(a => { return a.Name == "Parse" && a.GetParameters().Length == 1; });
                return ms is null ? str : ms.Invoke(null, new string[1] { str });
            }
            catch
            { return str; }
        }
        /// <summary>
        /// 正则匹配
        /// </summary>
        /// <param name="str">源字符串</param>
        /// <param name="pattern">正则表达式</param>
        /// <param name="value">匹配到的值</param>
        /// <returns>是否找到了匹配</returns>
        public static bool IsMatch(this string str, string pattern, out string value)
        {
            value = String.Empty;
            if (string.IsNullOrEmpty(str))
                return false;
            else
            {
                if (Regex.IsMatch(str, pattern))
                { value = Regex.Match(str, pattern).Value; return true; }
                else
                    return false;

            }
        }
        /// <summary>
        /// 正则匹配
        /// </summary>
        /// <param name="str">源字符串</param>
        /// <param name="pattern">正则表达式</param>
        /// <returns>是否找到了匹配</returns>
        public static bool IsMatch(this string str, string pattern)
        {
            return str.IsMatch(pattern, out string _);
        }
        /// <summary>
        /// 尝试将这个字符串转化为布尔格式。
        /// </summary>
        /// <param name="str">字符串</param>
        /// <param name="output">输出值</param>
        /// <returns>是否为真</returns>
        public static bool TryParseToBoolean(this string str, out bool output)
        {
            string[] oks = new string[] { "是", "y", "yes", "ok", "好", "可以", "可", "行", "好的", "get" };
            for (int i = 0; i < oks.Length; i++)
            {
                if (str.ToLower() == oks[i])
                {
                    output = true;
                    return true;
                }
            }
            return Boolean.TryParse(str, out output);
        }
        /// <summary>
        /// 判断一个字符串中是否包含一个字符串组中的全部内容
        /// </summary>
        /// <param name="from">源字符串</param>
        /// <param name="ts">要查找的字符串</param>
        /// <returns>是否包含一个组中的全部内容</returns>
        public static bool AllContains(this string from, params string[] ts)
        {
            if (ts.Length == 0 || from.Count() == 0)
                return false;

            bool finded = true;
            for (int i = 0; i < ts.Length; i++)
            {
                if (!from.Contains(ts[i]))
                { finded = false; break; }
            }
            return finded;
        }
        /// <summary>
        /// 判断一个字符串中是否包含一个字符串组中的全部内容（支持正则表达式）
        /// </summary>
        /// <param name="from">源字符串</param>
        /// <param name="ts">要查找的字符串</param>
        /// <returns>是否包含一个组中的全部内容</returns>
        public static bool AllMatches(this string from, params string[] ts)
        {
            if (ts.Length == 0 || from.Count() == 0)
                return false;

            bool finded = true;
            for (int i = 0; i < ts.Length; i++)
            {
                if (!from.IsMatch(ts[i]))
                { finded = false; break; }
            }
            return finded;
        }
        /// <summary>
        /// 将一个IP和Port合并成一段字符串。
        /// </summary>
        /// <param name="ip">IP字符串</param>
        /// <param name="port">Port值</param>
        /// <returns>合并后的字符串</returns>
        public static string IPAndPort_Merge(this string ip,string port)
        {
            return ip + ":" + port;
        }
        /// <summary>
        /// 将一段表示IP和Port的字符串拆分成两段分别表示IP和Port的字符串。
        /// </summary>
        /// <param name="ipAndPort">表示IP和Port的字符串</param>
        /// <param name="port">返回的Port值</param>
        /// <returns>IP字符串</returns>
        public static string IPAndPort_Split(this string ipAndPort, out string port)
        {
            port = string.Empty;
            if (ipAndPort.IsNullOrEmpty() || !ipAndPort.IsMatch(":"))
                return string.Empty;

            string[] alltxt = ipAndPort.Split(':');
            port = alltxt.Last();
            return alltxt.First();
        }
        /// <summary>
        /// 将通配符字符串转换成等价的正则表达式
        /// 这可以用正则表达式来实现通配符匹配
        /// </summary>
        public static string GetWildcardRegexString(string wildcardStr)
        {
            Regex replace = new Regex("[.$^{\\[(|)*+?\\\\]");
            return replace.Replace(wildcardStr,
                 delegate (Match m)
                 {
                     switch (m.Value)
                     {
                         case "?":
                             return ".?";
                         case "*":
                             return ".*";
                         default:
                             return "\\" + m.Value;
                     }
                 });
        }
    }

    public static class System_Extensons
    {
        public static void CMD(this string command,bool useShellExecute = false,bool waitForExit = false,bool showCommand = true)
        {
            if (showCommand)
                Console.WriteLine($"执行CMD命令：{command}");
            Process process = new Process();
            var StartInfo = new ProcessStartInfo();
            process.StartInfo = StartInfo;
            StartInfo.FileName = "CMD.exe";
            StartInfo.Arguments = string.Format("/c ({0})", command);
            StartInfo.UseShellExecute = useShellExecute;
            process.Start();
            if (waitForExit)
            {
                process.WaitForExit();
            }
            process.Close();
        }
    }
}