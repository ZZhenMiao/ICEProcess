using Microsoft.Win32;
using System;
using System.Windows;

namespace ICE_AssetLibrary
{
    /// <summary>
    /// Win_Options.xaml 的交互逻辑
    /// </summary>
    public partial class Win_Options : Window
    {
        public Win_Options()
        {
            InitializeComponent();
            this.DataContext = App.Options;
            PreviewToolPath_TextBox.Text = App.Options.PreviewToolPath;
            PreviewToolRunParameterPrefix_TextBox.Text = App.Options.PreviewToolRunParameterPrefix;
            PreviewToolRunParameterSuffix_TextBox.Text = App.Options.PreviewToolRunParameterSuffix;


            this.PreviewToolPath_Button.Click += (obj, e) =>
            {
                var dialog = new OpenFileDialog();
                dialog.Title = "选择预览工具";
                dialog.Filter = "可执行文件|*.exe";
                dialog.Multiselect = false;
                var re = dialog.ShowDialog();
                if (re != true)
                    return;
                PreviewToolPath_TextBox.Text = dialog.FileName;
            };
            this.OK_Button.Click += (obj, e) =>
            {
                App.Options.PreviewToolPath = PreviewToolPath_TextBox.Text;
                App.Options.PreviewToolRunParameterPrefix = PreviewToolRunParameterPrefix_TextBox.Text;
                App.Options.PreviewToolRunParameterSuffix = PreviewToolRunParameterSuffix_TextBox.Text;
                App.SaveOptions();
                this.Close();
            };
            this.Cancel_Button.Click += (obj, e) =>
            {
                this.Close();
            };
        }
    }

    [Serializable]
    public class OptionsInfo
    {
        public string PreviewToolPath { get; set; }
        public string PreviewToolRunParameterPrefix { get; set; }
        public string PreviewToolRunParameterSuffix { get; set; }
    }
}
