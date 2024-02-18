using ICE_Model;
using LiZhenStandard.Sockets;
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

namespace ICE_Integrator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public List<ICEProduction> Productions { get; } = new();

        public MainWindow()
        {
            InitializeComponent();
            LoadPublicSettings();
            LoadProductions();
        }

        private void LoadProductions()
        {
            bool re = SocketFunction.SendInstruct(App.Socket, "LoadProductions", null, out object[] results, out Exception e);
            if (re)
            {
                this.Productions.AddRange(from production in results select new ICEProduction((Production)production));
                this.Productions_ListBox.SetBinding(ListBox.ItemsSourceProperty, new Binding() { Source = this.Productions });
            }
        }

        private void LoadPublicSettings()
        {
            bool re = SocketFunction.SendInstruct(App.Socket, "LoadPublicSettings", null, out object[] results, out Exception e);
            if (re)
            {
                App.ProductionInfoPath = (string)PublicSetting.GetSettingsValue(results, "ProductionInfoPath");
            }
        }

        private void Install_Button_Click(object sender, RoutedEventArgs e)
        {
            ICEProduction selectedPD = (ICEProduction)Productions_ListBox.SelectedItem;
            selectedPD?.Install();
        }
    }

    public class ICEProduction: Production
    {
        public Version Version { get; set; }
        public Version NewVersion { get; set; }
        public Image Image { get; set; }
        public bool Installed { get; set; }
        public System.IO.DirectoryInfo InstallationDirectory { get; set; }
        public string StartupParameter { get; set; }

        public ICEProduction(Production production)
        {
            this.ID = production.ID;
            this.Name = production.Name;
            this.Illustration = production.Illustration;
            this.Code = production.Code;
        }
        public void Install()
        {
            MessageBox.Show("装了个寂寞……" + App.ProductionInfoPath,"温馨提示！");
        }
        public string GetImage()
        {
            throw new Exception();
        }
        public string GetInstallationDirectory()
        {
            throw new Exception();
        }
        public bool CheckInstallation()
        {
            throw new Exception();
        }
        public Version GetVersion()
        {
            throw new Exception();
        }
        public Version GetNewVersion()
        {
            throw new Exception();
        }
        public void Uninstall()
        {
            throw new Exception();
        }
    }
}
