using System;
using System.Collections.Generic;
using System.IO;
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
    /// SimpleInputor.xaml 的交互逻辑
    /// </summary>
    public partial class SimpleInputor: Window
    {

        public bool Cancel { get; set; } = true;
        public object Value { get; private set; }
        public bool CheckFileName { get; set; }

        private bool CheckFileNameMathod(string fileName)
        {
            var InvalidPathChars = System.IO.Path.GetInvalidFileNameChars();
            foreach (var c in InvalidPathChars)
            {
                if (fileName.Contains(c))
                    return false;
            }
            return true;
        }
        public SimpleInputor(string propertyName,string defaultValue,string title = null)
        {
            InitializeComponent();
            this.Title = title ?? propertyName;
            this.PropertyName_TextBlock.Text = propertyName;
            this.Inputor_TextBox.Text = defaultValue;
            this.OK_Button.Click += (obj, e) => 
            {
                string text = this.Inputor_TextBox.Text;
                if(CheckFileName && !CheckFileNameMathod(text))
                {
                    MessageBox.Show("输入的文件名含有非法字符，请予以修改。");
                    return;
                }
                Value = text;
                Cancel = false;
                this.Hide();
            };
            this.Cancel_Button.Click += (obj, e) =>
            {
                this.Hide();
            };
        }
    }
}
