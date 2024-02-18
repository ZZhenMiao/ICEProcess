using System.Windows.Controls;

namespace ICE_AssetLibrary
{
    /// <summary>
    /// AssetIconPanel.xaml 的交互逻辑
    /// </summary>
    public partial class AssetIconPanel : UserControl, System.Windows.Markup.IAddChild
    {
        public UIElementCollection Children { get => wrapPanel.Children; }

        public AssetIconPanel()
        {
            InitializeComponent();
            //SetBinding(ContentProperty, new Binding("Children") { Source = this.wrapPanel });
        }
    }
}
