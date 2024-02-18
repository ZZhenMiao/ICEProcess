using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using LiZhenStandard.Extensions;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Reflection;
using System.Collections;
using System.Threading;

namespace LiZhenStandard.Sockets
{
    public static class SocketFunction
    {
        public static Socket MakeTCPServer(string ipAndPort)
        {
            return MakeTCPServer(ipAndPort, out _);
        }
        public static Socket MakeTCPServer(string ipAndPort,out Exception exception, int backlog = 256)
        {
            exception = null;
            Socket skt = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            string ip = ipAndPort.IPAndPort_Split(out string port);
            try
            {
                if (int.TryParse(port, out int intp))
                    skt.Bind(new IPEndPoint(IPAddress.Parse(ip), intp));
                skt.Listen(backlog);
            }
            catch (Exception e) { exception = e; }
            return skt;
        }

        public static Socket ConnectTCPServer(string ipAndPort)
        {
            return ConnectTCPServer(ipAndPort,out _);
        }
        public static Socket ConnectTCPServer(string ipAndPort, out Exception exception)
        {
            exception = null;
            Socket skt = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            string ip = ipAndPort.IPAndPort_Split(out string port);
            try
            {
                if (int.TryParse(port, out int intp))
                    skt.Connect(new IPEndPoint(IPAddress.Parse(ip), intp));
            }catch(Exception e) { exception = e; }
            return skt;
        }

        public static int Send(this Socket socket,object obj)
        {
            byte[] str = obj.Serialize();

            //Console.WriteLine($"正在向 {socket.RemoteEndPoint}({socket.ProtocolType}) 发送 {str.Length} 字节的数据。");
            //var len = socket.Send(str);
            //return len;

            int strLen = str.Length;
            Console.WriteLine($"正在向 {socket.RemoteEndPoint}({socket.ProtocolType}) 发送 {strLen} 字节的数据。");
            int len = 0;
            int max = 1024 * 128;
            try
            {
                do
                {
                    int a = strLen - len;
                    var flag = SocketFlags.None;
                    if (strLen > max)
                        Console.Write("分批发送：" + len + " / " + a + " : " + flag.ToString());
                    var sended = socket.Send(str, len, a < max ? a : max, flag);
                    Console.WriteLine($" 已发送 {sended} 字节的数据。");
                    len += sended;
                    if (strLen > max)
                        Thread.Sleep(60);
                } while (len < strLen);
            }
            catch (Exception e)
            { Console.WriteLine(e.Message); }
            return len;
        }
        public static int Send(this Socket socket, object[] obj)
        {
            try
            {
                return SocketFunction.Send(socket, (object)obj);
            }
            catch(Exception e) { Debug.WriteLine(e.Message); }
            return -1;
        }

        public static int Send(this Socket socket, string content, Encoding encoding)
        {
            return Send(socket, content, encoding, out _);
        }
        public static int Send(this Socket socket, string content, Encoding encoding, out Exception exception)
        {
            return Send(socket, encoding.GetBytes(content), out exception);
        }
        public static int Send(this Socket socket, byte[] vs)
        {
            return Send(socket, vs, out _);
        }
        public static int Send(this Socket socket,byte[] vs, out Exception exception)
        {
            exception = null;
            try
            {
                return socket.Send(vs);
            }
            catch (Exception e) { exception = e; }
            return -1;
        }
        public static T Receive<T>(this Socket socket)
        {
            return socket.Receive<T>(out _);
        }
        public static T Receive<T>(this Socket socket, out Exception exception)
        {
            byte[] bytes = socket.Receive(out exception);
            T re = default;
            try
            {
                re = bytes.Deserialize<T>();
            }
            catch(Exception e) { exception = e; }
            return re;
        }
        public static string Receive(this Socket socket, Encoding encoding)
        {
            return socket.Receive(encoding,out _);
        }
        public static string Receive(this Socket socket,Encoding encoding,out Exception exception)
        {
            return encoding.GetString(socket.Receive(out exception));
        }
        public static string Receive(this Socket socket, int bufferLength,Encoding encoding)
        {
            return Receive(socket, bufferLength, encoding,out _);
        }
        public static string Receive(this Socket socket,int bufferLength, Encoding encoding, out Exception exception)
        {
            byte[] bs = socket.Receive(bufferLength, out int len, out exception);
            return encoding.GetString(bs,0,len);
        }
        public static byte[] Accept(this Socket socket)
        {
            return socket.Receive(out _);
        }
        public static byte[] Receive(this Socket socket, out Exception exception)
        {
            List<byte> re = new List<byte>();
            int len;
            int max = 1024 * 128;
            do
            {
                re.AddRange(socket.Receive(max, out len, out exception));
                Console.WriteLine($"从 {socket.RemoteEndPoint} 收到 {len} 字节的数据。");
                if (exception != null)
                    Console.WriteLine(exception.Message);
            }
            while (len >= max);
            return re.ToArray();
        }
        public static byte[] Receive(this Socket socket, int bufferLength, out int receiveLength)
        {
            return socket.Receive(bufferLength,out receiveLength, out _);
        }
        public static byte[] Receive(this Socket socket, int bufferLength,out int receiveLength, out Exception exception)
        {
            exception = null;
            receiveLength = 0;
            byte[] buffer = new byte[bufferLength];
            try
            {
                receiveLength = socket.Receive(buffer);
            }
            catch (Exception e) { exception = e; }
            return buffer;
        }

        private static Dictionary<string, Func<object[], object[]>> InstructionDictionary { get; } = new Dictionary<string, Func<object[], object[]>>();
        private static List<Socket> Sockets { get; } = new List<Socket>();
        private static List<Task> ConnectionTasks { get; } = new List<Task>();
        private static Socket MainListenSocket = null;
        private static Task MainListenTask { get; } = new Task(Listen);
        private static void Listen()
        {
            while (true)
            {
                Socket skt = MainListenSocket.Accept();
                skt.ReceiveBufferSize = 1024 * 32;
                skt.SendBufferSize = 1024 * 32;
                Sockets.Add(skt);
                Task task = new Task(new Action<object>(ReceiveInstruction), skt);
                ConnectionTasks.Add(task);
                task.Start();
            }
        }

        /// <summary>
        /// 启动监听主进程
        /// </summary>
        public static void StartMainListenTask()
        {
            MainListenTask.Start();
        }
        /// <summary>
        /// 添加指令
        /// </summary>
        /// <param name="instructCode">指令代码</param>
        /// <param name="func">指令方法</param>
        public static void AddInstruct(string instructCode, Func<object[], object[]> func)
        {
            InstructionDictionary.Add(instructCode,func);
        }
        /// <summary>
        /// 根据指令代码和参数组查找并执行指令，返回执行结果组和是否成功执行指令。
        /// </summary>
        /// <param name="instructCode">指令代码</param>
        /// <param name="Parameters">要执行的指令所需参数</param>
        /// <param name="results">若执行成功将返回执行结果对象组，若执行失败则返回异常信息</param>
        /// <returns>是否成功执行指令</returns>
        public static bool InvokeInstruct(string instructCode,object[] Parameters,out object[] results)
        {
            results = null;
            bool get = InstructionDictionary.TryGetValue(instructCode,out var func);
            if (get)
                try
                {
                    results = func.Invoke(Parameters);
                    return true;
                }
                catch (Exception e) { Debug.WriteLine(e.Message); results = new object[] { e }; return false; }
            else
            { results = new object[] { new InvalidInstructionException() }; return false; }
        }
        /// <summary>
        /// 设置要监听连接的套接字
        /// </summary>
        /// <param name="socket">要监听的套接字</param>
        public static void SetMainListenSocket(Socket socket)
        {
            MainListenSocket = socket;
        }
        /// <summary>
        /// 监听并执行指令，然后返回执行结果。
        /// </summary>
        /// <param name="socket">要执行监听的套接字</param>
        public static void ReceiveInstruction(this Socket socket)
        {
            while (true)
            {
                if (!socket.Connected)
                    break;
                try
                {
                    Instruction Instruction = socket.Receive<Instruction>();
                    InvokeInstruct(Instruction?.Code, Instruction?.Parameters, out object[] results);
                    socket.Send(results);
                    //Debug.WriteLine(socket.RemoteEndPoint);
                }
                catch (Exception e) { Debug.WriteLine(e.Message); break; }
            }
        }
        private static void ReceiveInstruction(this object socket)
        {
            var skt = (Socket)socket;
            ReceiveInstruction(skt);
        }
        /// <summary>
        /// 发送指令和其参数，并等待回传指令结果或是否成功。
        /// </summary>
        /// <param name="InstructCode">指令代码</param>
        /// <param name="Parameters">指令参数</param>
        /// <param name="results">返回指令结果</param>
        /// <returns>是否执行成功</returns>
        public static bool SendInstruct(this Socket socket, string InstructCode, object[] Parameters, out object[] results,out Exception e)
        {
            Instruction ins = new Instruction() { Code = InstructCode, Parameters = Parameters };
            socket.Send(ins);
            results = socket.Receive<object[]>(out e);
            if (e is null)
            {
                //Console.WriteLine("收到服务器回传信息：" + results?.ToString() + "Type:" + results?.GetType());
                //Debug.WriteLine("收到服务器回传信息：" + results?.ToString() + "Type:" + results?.GetType());
                return true;
            }
            else
            {
                //Console.WriteLine("错误，无效指令！");
                return false;
            }
        }

        public static IPInfomation[] GetLocalHostIP()
        {
            List<IPInfomation> re = new List<IPInfomation>();
            foreach (NetworkInterface netif in NetworkInterface.GetAllNetworkInterfaces()
             .Where(a => a.SupportsMulticast)
             .Where(a => a.OperationalStatus == OperationalStatus.Up)
             .Where(a => a.NetworkInterfaceType != NetworkInterfaceType.Loopback)
             .Where(a => a.GetIPProperties().GetIPv4Properties() != null)
             .Where(a => a.GetIPProperties().UnicastAddresses.Any(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork))
             .Where(a => a.GetIPProperties().UnicastAddresses.Any(ua => ua.IsDnsEligible))
            )
            {
                //Console.WriteLine("Network Interface: {0}", netif.Name);
                IPInterfaceProperties properties = netif.GetIPProperties();
                foreach (IPAddressInformation unicast in properties.UnicastAddresses)
                {
                    string ip = unicast.Address.ToString();
                    if (ip.IsMatch(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}"))
                        re.Add(new IPInfomation() { NetName = netif.Name, IP = ip });
                    //Console.WriteLine("\tUniCast: {0}", unicast.Address); 
                }
            }
            return re.ToArray();
        }
    }
    public struct IPInfomation
    {
        public string DisplayName { get => string.Format("{0}:{1}",NetName,IP); }
        public string NetName { get; set; }
        public string IP { get; set; }

        public override string ToString()
        {
            return IP;
        }

        public static implicit operator string(IPInfomation iPInfomation)
        {
            return iPInfomation.ToString();
        }
    }

    [Serializable]
    public class Instruction
    {
        public string Code { get; set; }
        public object[] Parameters { get; set; }
        public bool HaveParameter { get => Parameters is null; }
        public override string ToString()
        {
            return string.Format("{0}({1})", Code,Parameters.AllToString());
        }
    }


    [Serializable]
    public class InvalidInstructionException : Exception
    {
        public InvalidInstructionException() { }
        public InvalidInstructionException(string message) : base(message) { }
        public InvalidInstructionException(string message, Exception inner) : base(message, inner) { }
        protected InvalidInstructionException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}