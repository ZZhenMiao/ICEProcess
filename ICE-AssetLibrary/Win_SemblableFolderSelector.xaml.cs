using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;
using LiZhenStandard.Extensions;

namespace ICE_BackEnd
{
    /// <summary>
    /// Win_SemblableFolderSelector.xaml 的交互逻辑
    /// </summary>
    public partial class Win_SemblableFolderSelector : Window
    {
        ObservableCollection<string> Folders { get; } = new ObservableCollection<string>();
        public string ReFolder { get; set; } = null;
        public Win_SemblableFolderSelector(string[] folders)
        {
            InitializeComponent();
            Folders.AddRange(folders);
            this.ListBox.SetBinding(ListBox.ItemsSourceProperty, new Binding() { Source = this.Folders });
            this.ListBox.SelectedIndex = 0;


            this.Cancel_Button.Click += (obj, e) =>
            {
                var bre = MessageBox.Show("啊？你确定？？？？", "来自超哥的凝视…",MessageBoxButton.OKCancel);
                if (bre == MessageBoxResult.OK) 
                    this.Hide();
            };

            this.OK_Button.Click += (obj, e) =>
            {
                var folder = this.ListBox.SelectedItem;
                if (folder is not null)
                { ReFolder = folder.ToString(); this.Hide(); }
                else
                    MessageBox.Show("请至少选择一个目标文件夹。", "提示");
            };
        }

    }
}
