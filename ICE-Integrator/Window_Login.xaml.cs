using LiZhenMySQL;
using System;
using System.Windows;
using LiZhenStandard.Extensions;
using PropertyChanged;
using ICE_Model;
using LiZhenStandard.Sockets;
using System.Threading;
using System.Linq;

namespace ICE_Integrator
{
    /// <summary>
    /// Window_Login.xaml 的交互逻辑
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    public partial class Window_Login : Window
    {
        public string UserName { get; set; }

        public string IP { get; set; }
        public string Port { get; set; }

        public bool SavePassword { get; set; }
        public string LoadedPassword { get; set; }
        public bool AutoLogin { get; set; }

        public Window_Login()
        {
            DataBase.ClearAllRegistedSqlTypeConnection();
            LoadSettings();
            InitializeComponent();
            Password_PasswordBox.Password = LoadedPassword;
            LoadedPassword = null;
            if (AutoLogin)
                Login_Button_Click(this,new RoutedEventArgs());
        }

        private void Login_Button_Click(object sender, RoutedEventArgs e)
        {
            bool canlogin = TryLogin(out Person me);
            if (canlogin)
                Login(me);
        }
        private void SaveSettings()
        {
            App.Settings.Login_UserName = UserName;
            App.Settings.Login_SavePassword = SavePassword;
            if (SavePassword)
            {
                App.Settings.Login_Password = Password_PasswordBox.Password.EncryptString(App.PasswordKey);
            }
            App.Settings.Login_AutoLogin = AutoLogin;
            App.Settings.Login_IP = IP;
            App.Settings.Login_Port = int.TryParse(Port, out int result) ? result : 0;
            App.Settings.Save();
        }
        private void LoadSettings()
        {
            SavePassword = App.Settings.Login_SavePassword;
            AutoLogin = App.Settings.Login_AutoLogin;
            UserName = App.Settings.Login_UserName;
            if (SavePassword)
            {
                LoadedPassword = App.Settings.Login_Password.DecryptString(App.PasswordKey);
            }
            IP = App.Settings.Login_IP;
            Port = App.Settings.Login_Port.ToString();
        }
        private bool TryLogin(out Person person)
        {
            person = null;
            App.Initialize(IP,Port);
            bool re = SocketFunction.SendInstruct(App.Socket, "CheckLogin", new object[] { UserName, Password_PasswordBox.Password }, out object[] results, out Exception e);
            object result = results?.FirstOrDefault();
            if (!re || typeof(Exception).IsAssignableFrom(result.GetType()))
            {
                Exception ex = e ?? (Exception)result;
                MessageBox.Show(ex.Message,"登录错误");
                return false;
            }
            person = (Person)result;
            return true;
        }
        private void Login(Person me)
        {
            SaveSettings();
            App.Me = me;
            MainWindow mw = new();
            mw.Show();
            this.Close();
        }
    }
}
