using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MySql.Data.MySqlClient;
using LiZhenMySQL;
using LiZhenStandard.Extensions;
using LiZhenStandard.Sockets;
using ICE_Model;
using PropertyChanged;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Windows.Threading;
using System.IO;
using static System.Net.Mime.MediaTypeNames;
using ICE_Common;

namespace ICE_BackEnd
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    public partial class MainWindow : Window
    {
        public static int SelectedLocalHostIPIndex { get; set; }
        public static string LocalHostPort { get; set; }
        public static string DatabaseIP { get; set; }
        public static string DatabasePort { get; set; }
        public static string DatabaseName { get; set; }
        public static string LoginName { get; set; }
        public string Password { get => Password_PasswordBox.Password; set => Password_PasswordBox.Password = value; }

        public static IPInfomation[] LocalHostIPs { get; set; }
        public static IPInfomation SelectedLocalHostIP { get; set; }

        private bool? All { get => All_RadioButton.IsChecked; }
        private bool? Receive { get=>Receive_RadioButton.IsChecked;  }
        private bool? Query { get=>Query_RadioButton.IsChecked;  }
        private bool? Write { get=>Write_RadioButton.IsChecked;  }
        private bool? Send { get=>Send_RadioButton.IsChecked;  }
        private bool? Exception { get=>Exception_RadioButton.IsChecked;  }

        public MainWindow()
        {
            InitializeComponent();
            LocalHostIPs = SocketFunction.GetLocalHostIP();
            LoadSettings();
            SetMaintainFunctions();
            SetEvent();
            SetBinding();

            //Connect_Button_Click(this, new RoutedEventArgs());
            //StartUp_Button_Click(this, new RoutedEventArgs());
        }

        private void SetBinding()
        {
            this.LocalHostIP_ComboBox.SetBinding(ComboBox.ItemsSourceProperty, new Binding() { Source = LocalHostIPs });
            this.LocalHostIP_ComboBox.SetBinding(ComboBox.SelectedItemProperty, new Binding("SelectedLocalHostIP") { Source = this });
            this.LocalHostIP_ComboBox.SetBinding(ComboBox.SelectedIndexProperty, new Binding("SelectedLocalHostIPIndex") { Source = this });
            this.LocalHostPort_TextBox.SetBinding(TextBox.TextProperty, new Binding("LocalHostPort") { Source = this });
            this.DataBaseIP_TextBox.SetBinding(TextBox.TextProperty, new Binding("DatabaseIP") { Source = this });
            this.DataBasePort_TextBox.SetBinding(TextBox.TextProperty, new Binding("DatabasePort") { Source = this });
            this.DataBaseName_TextBox.SetBinding(TextBox.TextProperty, new Binding("DatabaseName") { Source = this });
            this.LoginName_TextBox.SetBinding(TextBox.TextProperty, new Binding("LoginName") { Source = this });
            this.ConInfo_ListView.SetBinding(DataGrid.ItemsSourceProperty, new Binding() { Source = this.ConnectProperties });
            this.Maintain_ListView.SetBinding(ListView.ItemsSourceProperty, new Binding() { Source = this.MaintainFunctions });
        }
        private void SetEvent()
        {
            this.Loaded += (obj, e) => { StartRefreshConInfoGrid(); };
            this.Closing += (obj, e) => { SaveSettings(); };
            this.Maintain_ListView.SelectionChanged += (obj, e) => 
            {
                this.MaintainFunctionIllustration_TextBlock.SetBinding(TextBlock.TextProperty, new Binding("Illustration") { Source = SelectedMaintainFunction}); 
            };
        }
        private void StartRefreshConInfoGrid()
        {
            DispatcherTimer timer = new DispatcherTimer(new TimeSpan(0, 0, 3),
                DispatcherPriority.Background,
                new EventHandler((obj, e) =>
                {
                    conInfoSelectedIndex = ConInfo_ListView.SelectedIndex;
                    ConInfo_ListView.SetBinding(DataGrid.ItemsSourceProperty, "");
                    ConInfo_ListView.SetBinding(DataGrid.ItemsSourceProperty, new Binding() { Source = this.ConnectProperties });
                    ConInfo_ListView.SelectedIndex = conInfoSelectedIndex;
                }),
               this.ConInfo_ListView.Dispatcher);
        }
        private void Connect_Button_Click(object sender, RoutedEventArgs e)
        {
            Connect();
        }
        private void StartUp_Button_Click(object sender, RoutedEventArgs e)
        {
            StartUp();
        }
        private void Stop_Button_Click(object sender, RoutedEventArgs e)
        {
            Stop();
        }
        private void OpenDir_Button_Click(object sender, RoutedEventArgs e)
        {
            //var p = new Person() { Name = "aa", Account="bb" };
            //p.InsertThisToDB<Person>();
        }
        private void SaveSettings()
        {
            //App.Settings.SelectedLocalHostIPIndex = SelectedLocalHostIPIndex;
            //App.Settings.LocalHostPort = LocalHostPort;
            //App.Settings.DatabaseIP = DatabaseIP;
            //App.Settings.DatabasePort = DatabasePort;
            //App.Settings.DatabaseName = DatabaseName;
            //App.Settings.LoginName = LoginName;
            //App.Settings.Password = Password;

            //App.Settings.Save();

            App.LoginInfo.SelectedLocalHostIPIndex = SelectedLocalHostIPIndex;
            App.LoginInfo.LocalHostPort = LocalHostPort;
            App.LoginInfo.DatabaseIP = DatabaseIP;
            App.LoginInfo.DatabasePort = DatabasePort;
            App.LoginInfo.DatabaseName = DatabaseName;
            App.LoginInfo.LoginName = LoginName;
            App.LoginInfo.Password = Password;

            if(!Directory.Exists(App.AssetLibraryBackEndDocumentDir))
                IO_Extensons.CreateDirectory(App.AssetLibraryBackEndDocumentDir);
            App.LoginInfo.SerializeToFile(App.LoginDataFile,out _);
        }
        private void LoadSettings()
        {
            if (!File.Exists(App.LoginDataFile))
            { App.LoginInfo = new LoginInfo(); return; }

            App.LoginInfo = Serialization_Extensons.DeserializeFromFile<LoginInfo>(App.LoginDataFile, out _);
            SelectedLocalHostIPIndex = App.LoginInfo.SelectedLocalHostIPIndex;
            LocalHostPort = App.LoginInfo.LocalHostPort;
            DatabaseIP = App.LoginInfo.DatabaseIP;
            DatabasePort = App.LoginInfo.DatabasePort;
            DatabaseName = App.LoginInfo.DatabaseName;
            LoginName = App.LoginInfo.LoginName;
            Password = App.LoginInfo.Password;


            //SelectedLocalHostIPIndex = App.Settings.SelectedLocalHostIPIndex;
            //LocalHostPort = App.Settings.LocalHostPort;
            //DatabaseIP = App.Settings.DatabaseIP;
            //DatabasePort = App.Settings.DatabasePort;
            //DatabaseName = App.Settings.DatabaseName;
            //LoginName = App.Settings.LoginName;
            //Password = App.Settings.Password;
        }
        private void Connect()
        {
            DataBase.ClearAllRegistedSqlTypeConnection();

            bool connectSuccessful = App.Initialize(DatabaseIP, DatabasePort, DatabaseName, LoginName, Password);
            if (!connectSuccessful)
            {
                MessageBox.Show("连接失败！", "提示");
                return;
            }
            Connect_Button.IsEnabled = false;
            Stop_Button.IsEnabled = true;
            ConInfo_TabItem.IsSelected = true;
            SaveSettings();
        }
        private void StartUp()
        {
            Connect();
            var e = App.StartUp();
            if (e.IsNotNull())
            {
                MessageBox.Show(e.Message, "未能启动指令处理服务");
                return;
            } 
            StartUp_Button.IsEnabled = false;
        }
        private void Stop()
        {
            if (MessageBox.Show("要停止后端服务吗？", "提示", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;
            App.MySqlConnection.TryClose();
            StartUp_Button.IsEnabled = true;
            Stop_Button.IsEnabled = false;
        }

    }

    [AddINotifyPropertyChangedInterface]
    public class ConnectProperty : INotifyPropertyChanged
    {
        public string Header { get; set; }
        public string Value { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }

}
