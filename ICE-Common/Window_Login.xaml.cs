using ICE_Model;
using LiZhenMySQL;
using LiZhenStandard.Extensions;
using LiZhenStandard.Sockets;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PropertyChanged;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Windows;

namespace ICE_Common
{
    [Serializable]
    public class LoginInfo
    {
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public string? IP { get; set; }
        public string? Port { get; set; }
        public bool SavePassword { get; set; }
        public bool AutoLogin { get; set; }
    }

    /// <summary>
    /// Window_Login.xaml 的交互逻辑
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    public partial class Window_Login : Window, INotifyPropertyChanged
    {
        private string iP;
        private string port;
        private bool savePassword;
        private string loadedPassword;
        private bool autoLogin;
        private string userName;

        public string UserName
        {
            get => userName; set
            {
                userName = value;
                OnPropertyChanged(nameof(UserName));
            }
        }

        public string IP
        {
            get => iP; set
            {
                iP = value;
                OnPropertyChanged(nameof(IP));
            }
        }
        public string Port
        {
            get => port; set
            {
                port = value;
                OnPropertyChanged(nameof(Port));
            }
        }

        public bool SavePassword
        {
            get => savePassword; set
            {
                savePassword = value;
                OnPropertyChanged(nameof(SavePassword));
            }
        }
        public string LoadedPassword
        {
            get => loadedPassword; set
            {
                loadedPassword = value;
                OnPropertyChanged(nameof(LoadedPassword));
            }
        }
        public bool AutoLogin
        {
            get => autoLogin; set
            {
                autoLogin = value;
                OnPropertyChanged(nameof(AutoLogin));
            }
        }
        public bool IsClosed { get; set; } = false;

        public string LoginDataFile { get; set; }
        //public delegate void InitializeDelegate(string ip, string port);
        //public InitializeDelegate InitializeMethod { get; set; }
        public Socket Socket { get; set; }
        public delegate void LoginDelegate(Person person);
        public LoginDelegate LoginMethod { get; set; }

        public void Initialize(string ip, string port)
        {
            Socket = SocketFunction.ConnectTCPServer(string.Format("{0}:{1}", ip, port));
            Socket.SendBufferSize = 1024 * 32;
            Socket.ReceiveBufferSize = 1024 * 32;
        }
        public Window_Login(bool allowAutoLogin = true)
        {
            DataBase.ClearAllRegistedSqlTypeConnection();
            InitializeComponent();
            this.Closed += Window_Login_Closed;
            this.Loaded += (obj, e) =>
            {
                LoadSettings();
                if (AutoLogin && allowAutoLogin)
                    Login_Button_Click(this, new RoutedEventArgs());
            };
        }

        private void Window_Login_Closed(object sender, EventArgs e)
        {
            this.IsClosed = true;
        }

        private void Login_Button_Click(object sender, RoutedEventArgs e)
        {
            bool canlogin = TryLogin(out Person me);
            if (canlogin)
                Login(me);
        }
        private void SaveSettings()
        {
            if (!Directory.Exists(Path.GetDirectoryName(LoginDataFile)))
                IO_Extensons.CreateDirectory(Path.GetDirectoryName(LoginDataFile));
            LoginInfo info = new LoginInfo();
            info.UserName = UserName;
            info.IP = IP;
            info.AutoLogin = AutoLogin;
            info.SavePassword = SavePassword;
            info.Port = Port;
            if (SavePassword)
            {
                info.Password = Password_PasswordBox.Password;
            }
            info.SerializeToFile(LoginDataFile, out _);

            //App.Settings.Login_UserName = UserName;
            //App.Settings.Login_SavePassword = SavePassword;
            //if (SavePassword)
            //{
            //    App.Settings.Login_Password = Password_PasswordBox.Password.EncryptString(App.PasswordKey);
            //}
            //App.Settings.Login_AutoLogin = AutoLogin;
            //App.Settings.Login_IP = IP;
            //App.Settings.Login_Port = int.TryParse(Port, out int result) ? result : 0;
            //App.Settings.Save();
        }
        private void LoadSettings()
        {
            LoginInfo info = Serialization_Extensons.DeserializeFromFile<LoginInfo>(LoginDataFile, out Exception ex);
            if (ex is not null)
            { Console.WriteLine(ex.Message); Debug.WriteLine(ex.Message); }
            if (info is null)
                return;
            SavePassword = info.SavePassword;
            AutoLogin = info.AutoLogin;
            UserName = info.UserName;
            if (SavePassword)
            {
                LoadedPassword = info.Password;
            }
            Password_PasswordBox.Password = LoadedPassword;
            //LoadedPassword = null;
            IP = info.IP;
            Port = info.Port;


            //SavePassword = App.Settings.Login_SavePassword;
            //AutoLogin = App.Settings.Login_AutoLogin;
            //UserName = App.Settings.Login_UserName;
            //if (SavePassword)
            //{
            //    LoadedPassword = App.Settings.Login_Password.DecryptString(App.PasswordKey);
            //}
            //IP = App.Settings.Login_IP;
            //Port = App.Settings.Login_Port.ToString();
        }
        private bool TryLogin(out Person person)
        {
            person = null;
            //InitializeMethod?.Invoke(IP, Port);
            Initialize(IP, Port);
            bool re = SocketFunction.SendInstruct(Socket, "CheckLogin", new object[] { UserName, Password_PasswordBox.Password }, out object[] results, out Exception e);
            object result = results?.FirstOrDefault();
            if (!re || typeof(Exception).IsAssignableFrom(result.GetType()))
            {
                Exception ex = e ?? (Exception)result;
                MessageBox.Show(ex.Message, "登录错误");
                return false;
            }
            person = (Person)result;
            return true;
        }
        private void Login(Person me)
        {
            SaveSettings();
            LoginMethod?.Invoke(me);
            this.Close();
        }
    }
}
