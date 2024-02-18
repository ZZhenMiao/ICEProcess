using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace LiZhenWPF
{
    /// <summary>
    /// SimpleProgressBarWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SimpleProgressBarWindow : Window ,INotifyPropertyChanged
    {
        public double Process
        {
            get { return (double)GetValue(ProcessProperty); }
            set { SetValue(ProcessProperty, value); PropertyChanged?.Invoke(this,new PropertyChangedEventArgs("Process")); }
        }

        // Using a DependencyProperty as the backing store for Process.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ProcessProperty =
            DependencyProperty.Register("Process", typeof(int), typeof(SimpleProgressBarWindow), new PropertyMetadata(0));

        public SimpleProgressBarWindow(string processName)
        {
            InitializeComponent();
            this.Header_TextBlock.Text = processName;
            this.ProgressBar_ProgressBar.SetBinding(ProgressBar.ValueProperty, new Binding() { Source = Process });
        }
    }

}
