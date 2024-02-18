using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ICE_AssetLibrary
{
    /// <summary>
    /// TextLabelContainer.xaml 的交互逻辑
    /// </summary>
    public partial class TextLabelContainer : UserControl, System.Windows.Markup.IAddChild
    {
        public string GroupName
        {
            get { return (string)GetValue(GroupNameProperty); }
            set { SetValue(GroupNameProperty, value); nameBlock.Text = value; }
        }
        public static readonly DependencyProperty GroupNameProperty =
            DependencyProperty.Register("GroupName", typeof(string), typeof(TextLabelContainer), new PropertyMetadata("新条件组"));

        public UIElementCollection Children
        {
            get { return wrapPanel.Children; }
        }

        public TextLabelContainer()
        {
            InitializeComponent();
            this.nameBlock.SetBinding(TextBlock.TextProperty, new Binding("GroupName") { Source = this });
        }
    }
}
