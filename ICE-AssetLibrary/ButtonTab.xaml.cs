using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace ICE_AssetLibrary
{
    /// <summary>
    /// ButtonTab.xaml 的交互逻辑
    /// </summary>
    public partial class ButtonTab : UserControl
    {
        public Brush BackgroundColor { get { return IsSelected ? SelectedColor : UnselectedColor; } }

        public Brush UnselectedColor
        {
            get { return (Brush)GetValue(UnSelectedColorProperty); }
            set { SetValue(UnSelectedColorProperty, value); }
        }
        public static readonly DependencyProperty UnSelectedColorProperty =
            DependencyProperty.Register("UnSelectedColor", typeof(Brush), typeof(ButtonTab), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(221, 221, 221))));

        public Brush SelectedColor
        {
            get { return (Brush)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }
        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register("SelectedColor", typeof(Brush), typeof(ButtonTab), new PropertyMetadata(new SolidColorBrush(Colors.Silver)));

        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); if (value) { OnSelected?.Invoke(this, new RoutedEventArgs()); } else { UnSelected?.Invoke(this, new RoutedEventArgs()); } }
        }
        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register("IsSelected", typeof(bool), typeof(ButtonTab), new PropertyMetadata(false));

        public string Header
        {
            get { return (string)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register("Header", typeof(string), typeof(ButtonTab), new PropertyMetadata(""));

        public delegate void OnSelectedEventHandler(object sender, RoutedEventArgs e);
        public event OnSelectedEventHandler OnSelected;
        public delegate void UnSelectedEventHandler(object sender, RoutedEventArgs e);
        public event UnSelectedEventHandler UnSelected;

        public ButtonTab()
        {
            InitializeComponent();
            var backgroundBinding = new Binding("BackgroundColor") { Source = this };
            var buttonContentBinding = new Binding("Header") { Source = this };
            this.grid.SetBinding(Grid.BackgroundProperty, backgroundBinding);
            this.button.SetBinding(Button.ContentProperty, buttonContentBinding);
            this.button.Click += (obj, e) => { if (!IsSelected && IsEnabled) { IsSelected = true; } };
            this.OnSelected += (obj, e) => { this.grid.SetBinding(Grid.BackgroundProperty, backgroundBinding); };
            this.UnSelected += (obj, e) => { this.grid.SetBinding(Grid.BackgroundProperty, backgroundBinding); };
        }
    }

}
