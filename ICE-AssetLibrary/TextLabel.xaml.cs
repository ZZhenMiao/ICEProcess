using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace ICE_AssetLibrary
{
    /// <summary>
    /// ButtonTab.xaml 的交互逻辑
    /// </summary>
    public partial class TextLabel : UserControl
    {
        public string Header
        {
            get { return (string)GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register("Header", typeof(string), typeof(TextLabel), new PropertyMetadata(""));

        public Brush BackGroundColor
        {
            get { return (Brush)GetValue(BackGroundColorProperty); }
            set { SetValue(BackGroundColorProperty, value); }
        }
        public static readonly DependencyProperty BackGroundColorProperty =
            DependencyProperty.Register("BackGroundColor", typeof(Brush), typeof(TextLabel), new PropertyMetadata(new SolidColorBrush(Colors.AliceBlue)));

        public bool IsDisabled
        {
            get { return (bool)GetValue(IsDisabledProperty); }
            set
            {
                SetValue(IsDisabledProperty, value);
                if (value)
                    Disabled?.Invoke(this, new RoutedEventArgs());
                else
                    Enabled?.Invoke(this, new RoutedEventArgs());
            }
        }
        public static readonly DependencyProperty IsDisabledProperty =
            DependencyProperty.Register("IsDisabled", typeof(bool), typeof(UserControl), new PropertyMetadata(false));

        public bool IsExcluded
        {
            get { return (bool)GetValue(IsExcludedProperty); }
            set
            {
                SetValue(IsExcludedProperty, value);
                if (value)
                    Excluded?.Invoke(this, new RoutedEventArgs());
                else
                    Appointed?.Invoke(this, new RoutedEventArgs());
            }
        }
        public static readonly DependencyProperty IsExcludedProperty =
            DependencyProperty.Register("IsExcluded", typeof(bool), typeof(UserControl), new PropertyMetadata(false));

        public delegate void TextLabelEventHandler(object sender, RoutedEventArgs e);
        public event TextLabelEventHandler Disabled;
        public event TextLabelEventHandler Enabled;
        public event TextLabelEventHandler Excluded;
        public event TextLabelEventHandler Appointed;

        public event TextLabelEventHandler Removing;


        private void SetColor()
        {
            var gridBackgroundColorBinding = new Binding("BackGroundColor") { Source = this };
            BackGroundColor = new SolidColorBrush(Colors.AliceBlue);
            if (IsExcluded)
                BackGroundColor = new SolidColorBrush(Colors.LightCoral);
            if (IsDisabled)
                BackGroundColor = new SolidColorBrush(Colors.Silver);
            grid.SetBinding(Grid.BackgroundProperty, gridBackgroundColorBinding);
        }

        public TextLabel()
        {
            InitializeComponent();
            removeButton.Visibility = Visibility.Hidden;
            disableButton.Visibility = Visibility.Hidden;
            excludeButton.Visibility = Visibility.Hidden;
            Width = label.Width;
            var buttonContentBinding = new Binding("Header") { Source = this };
            var gridBackgroundColorBinding = new Binding("BackGroundColor") { Source = this };
            label.SetBinding(Button.ContentProperty, buttonContentBinding);
            grid.SetBinding(Grid.BackgroundProperty, gridBackgroundColorBinding);
            removeButton.Click += (obj, e) => { this.Removing?.Invoke(obj, e); };

            MouseEnter += (obj, e) =>
            {
                BackGroundColor = new SolidColorBrush(Colors.LightSkyBlue);
                grid.SetBinding(Grid.BackgroundProperty, gridBackgroundColorBinding);
                removeButton.Visibility = Visibility.Visible;
                //disableButton.Visibility = Visibility.Visible;
                //excludeButton.Visibility = Visibility.Visible;
            };
            MouseLeave += (obj, e) =>
            {
                SetColor();
                removeButton.Visibility = Visibility.Hidden;
                //disableButton.Visibility = Visibility.Hidden;
                //excludeButton.Visibility = Visibility.Hidden;
            };
            label.SizeChanged += (obj, e) =>
            {
                Width = double.NaN;
                Height = label.Width;
            };
            Disabled += (obj, e) => { SetColor(); };
            Enabled += (obj, e) => { SetColor(); };
            Excluded += (obj, e) => { SetColor(); };
            Appointed += (obj, e) => { SetColor(); };
            this.disableButton.Click += (obj, e) => { IsDisabled = !IsDisabled; };
            this.excludeButton.Click += (obj, e) => { IsExcluded = !IsExcluded; };
        }
    }

}
