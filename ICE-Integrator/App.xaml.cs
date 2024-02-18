using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ICE_Integrator.Properties;
using LiZhenMySQL;
using ICE_Model;
using System.Net.Sockets;
using LiZhenStandard.Sockets;

namespace ICE_Integrator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static Settings Settings { get => Settings.Default; }
        public static string PasswordKey { get => "ice"; }
        public static Person Me { get; set; }
        public static Socket Socket { get; set; }

        public static string ProductionInfoPath { get; set; }

        public static void Initialize(string ip, string port)
        {
            Socket = SocketFunction.ConnectTCPServer(string.Format("{0}:{1}",ip,port));
        }
    }
}
