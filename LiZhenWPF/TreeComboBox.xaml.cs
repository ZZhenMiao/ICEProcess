using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Linq;
using System.Windows.Media;
using System.Windows.Input;
using System.Diagnostics;
using LiZhenStandard.Extensions;
using System.Windows.Markup;
using System.IO;

namespace LiZhenWPF
{
    [TemplatePart(Name = "ParentPanel", Type = typeof(Grid))]
    [TemplatePart(Name = "ActualTreeView", Type = typeof(TreeView))]
    public partial class TreeComboBox : ItemsControl
    {
        private FrameworkElementFactory _itemElement;
        private HierarchicalDataTemplate _hierarchicalDataTemplate;
        private TreeView _treeView;
        public TreeComboBox()
        {
            _hierarchicalDataTemplate = new HierarchicalDataTemplate();
            ItemTemplate = _hierarchicalDataTemplate;
        }

        public static readonly DependencyProperty NamePathProperty =
           DependencyProperty.Register("NamePath", typeof(string), typeof(TreeComboBox),
               new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, null));

        public static readonly DependencyProperty SelectedPathProperty =
           DependencyProperty.Register("SelectedPath", typeof(string), typeof(TreeComboBox),
               new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, null));

        public static readonly DependencyProperty ItemSourcePathProperty =
             DependencyProperty.Register("ItemSourcePath", typeof(string), typeof(TreeComboBox),
                 new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, null));

        public static readonly DependencyProperty TextProperty =
             DependencyProperty.Register("Text", typeof(string), typeof(TreeComboBox),
                 new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, null));

        public static readonly DependencyProperty IsSingleSelectProperty =
            DependencyProperty.Register("IsSingleSelect", typeof(bool), typeof(TreeComboBox),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, null));

        public string NamePath
        {
            get => (string)GetValue(NamePathProperty);
            set => SetValue(NamePathProperty, value);
        }
        public string SelectedPath
        {
            get => (string)GetValue(SelectedPathProperty);
            set => SetValue(SelectedPathProperty, value);
        }
        public string ItemSourcePath
        {
            get => (string)GetValue(ItemSourcePathProperty);
            set => SetValue(ItemSourcePathProperty, value);
        }
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
        public bool IsSingleSelect
        {
            get => (bool)GetValue(IsSingleSelectProperty);
            set => SetValue(IsSingleSelectProperty, value);
        }
        public override void OnApplyTemplate()
        {
            //SetXAMLStyle();
            //this.Loaded += (obj, e) => { Style = DefaultStyle; };
            CreateTreeTemplate();
            base.OnApplyTemplate();
        }
        //private void SetXAMLStyle()
        //{
            //ResourceDictionary brushes_rd = new ResourceDictionary()
            //{
            //    Source = new Uri("pack://Application:,,,/LiZhenWPF;component/ResourcesDictionary.xaml")
            //};
            //Application.Current.Resources = new ResourceDictionary()
            //{
            //    MergedDictionaries = { brushes_rd }
            //};
            //Style = (Style)Application.Current.TryFindResource("TreeComboBoxStyle");
           
            
            
            //Debug.WriteLine(brushes_rd["TreeComboBoxStyle"].GetType().ToString());
            //Debug.WriteLine(Style.ToString());
        //}
        //private static void SetStyle()
        //{
        //    Style styleA = new Style() { TargetType = typeof(TreeComboBox) };
        //    styleA.Setters.Add(new Setter() { Property = HeightProperty, Value = 20 });
        //    Setter setterA = new Setter() { Property = TemplateProperty };
        //    Grid gridA = new Grid()
        //    {
        //        ClipToBounds = false,
        //        Height = (double)new TemplateBindingExtension(HeightProperty).ProvideValue(null)
        //    };
        //    gridA.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
        //    gridA.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(20) });
        //    Popup popupA = new Popup()
        //    {
        //        StaysOpen = false,
        //        MinHeight = 50,
        //        Placement = PlacementMode.Bottom,
        //        IsOpen = (bool)new Binding("IsChecked") { ElementName = "PopuButton" }.ProvideValue(null),
        //        Width = (double)new TemplateBindingExtension(WidthProperty).ProvideValue(null)
        //    };
        //    Grid.SetColumn(popupA, 2);
        //    TreeView treeViewA = new TreeView()
        //    {
        //        Name = "ActualTreeView",
        //        ItemTemplate = (DataTemplate)new TemplateBindingExtension(ItemTemplateProperty).ProvideValue(null),
        //        ItemsSource = (IEnumerable)new TemplateBindingExtension(ItemsSourceProperty).ProvideValue(null)
        //    };
        //    treeViewA.Resources.Add("",
        //        new Func<Style>(() =>
        //        {
        //            Style style = new Style() { TargetType = typeof(TreeViewItem) };
        //            style.Setters.Add(new Setter() { Property = TreeViewItem.IsExpandedProperty, Value = true });
        //            return style;
        //        }).Invoke());
        //    ScrollViewer.SetVerticalScrollBarVisibility(treeViewA, ScrollBarVisibility.Auto);
        //    gridA.Children.Add(popupA);
        //    gridA.Children.Add(new TextBox() { IsReadOnly = true,Text = (string)new TemplateBindingExtension(TextBox.TextProperty).ProvideValue(null) });
        //    gridA.Children.Add(new Func<ToggleButton>(() =>
        //    {
        //        ToggleButton togg = new ToggleButton() { Name = "PopuButton" };
        //        togg.Style = new Func<Style>(()=> 
        //        {
        //            Style style = new Style() { TargetType = typeof(ToggleButton) };
        //            style.Setters.Add(new Setter() { Property = BackgroundProperty, Value = new SolidColorBrush(Color.FromArgb(50,0,0,0)) });
        //            style.Setters.Add(new Setter() { Property = TemplateProperty, Value = new Func<ControlTemplate>(() => 
        //            {
        //                ControlTemplate controlTemplate = new ControlTemplate() { TargetType = typeof(ToggleButton)};
        //                controlTemplate.VisualTree = new Func<FrameworkElementFactory>(() =>
        //                {
        //                    FrameworkElementFactory fef = new FrameworkElementFactory(typeof(Border));
        //                    fef.SetValue(Border.BorderBrushProperty, Colors.Gray);
        //                    fef.SetValue(Border.BorderThicknessProperty, 0.5);
        //                    fef.SetValue(Border.BackgroundProperty,(Brush)new TemplateBindingExtension() { Property = BackgroundProperty }.ProvideValue(null));
        //                    fef.AppendChild(new Func<FrameworkElementFactory>(()=> 
        //                    {

        //                        FrameworkElementFactory grid = new FrameworkElementFactory() { Type = typeof(Grid) };

        //                        ///////////<<<<<<===========在这里继续写！

        //                        return grid;
        //                    }).Invoke());
        //                    return fef;
        //                }).Invoke();
        //                return null; 
        //            }).Invoke() });

        //            return style;
        //        }).Invoke();
        //        return togg;
        //    }).Invoke());
        //    styleA.Setters.Add(setterA);
        //}

        private void CreateTreeTemplate()
        {
            if (IsSingleSelect)
            {
                _itemElement = new FrameworkElementFactory(typeof(TextBlock));
                if (!string.IsNullOrEmpty(NamePath))
                    _itemElement.SetBinding(TextBlock.TextProperty, new Binding(NamePath));
            }
            else
            {
                _itemElement = new FrameworkElementFactory(typeof(Grid));
                _itemElement.SetValue(BackgroundProperty, Brushes.Transparent);

                var checkBoxElement = new FrameworkElementFactory(typeof(CheckBox));
                checkBoxElement.SetValue(CheckBox.IsHitTestVisibleProperty, false);

                if (!string.IsNullOrEmpty(NamePath))
                    checkBoxElement.SetBinding(CheckBox.ContentProperty, new Binding(NamePath));

                if (!string.IsNullOrEmpty(SelectedPath))
                    checkBoxElement.SetBinding(CheckBox.IsCheckedProperty, new Binding(SelectedPath));

                _itemElement.AppendChild(checkBoxElement);
            }
            if (!string.IsNullOrEmpty(ItemSourcePath))
                _hierarchicalDataTemplate.ItemsSource = new Binding(ItemSourcePath);

            _itemElement.AddHandler(PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(Grid_MouseUp));

            _hierarchicalDataTemplate.VisualTree = _itemElement;
            _treeView = GetTemplateChild("ActualTreeView") as TreeView;

            if (_treeView is null)
                _treeView = new TreeView() { Background = new SolidColorBrush(Colors.Red) };

            _treeView.PreviewMouseDoubleClick += _treeView_PreviewMouseDoubleClick;
        }
        private void _treeView_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
        private void Grid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_treeView.SelectedItem == null) return;

            var itemType = _treeView.SelectedItem.GetType();

            if (IsSingleSelect)
                Text = itemType.GetProperty(NamePath)?.GetValue(_treeView.SelectedItem)?.ToString();
            else
            {
                var selectedState = GetItemSelectedState(_treeView.SelectedItem);
                SetItemSelectedState(_treeView.SelectedItem, !selectedState);
                var collections = itemType.GetProperty(ItemSourcePath)?.GetValue(_treeView.SelectedItem) as IEnumerable<object>;
                if (collections != null && collections.Count() > 0)
                {
                    SetAllChildNodeState(collections, !selectedState);
                }
                CheckSelectedBox();
            }
        }
        private void CheckSelectedBox()
        {
            Text = "";
            UpdateSelectedText(ItemsSource);
            Text = Text.Trim(',');
        }
        private void UpdateSelectedText(IEnumerable items)
        {
            foreach (object item in items)
            {
                var itemsProperty = item.GetType().GetProperty(ItemSourcePath);
                var itemsSource = itemsProperty?.GetValue(item) as IEnumerable<object>;
                if (itemsSource != null)
                {
                    UpdateSelectedText(itemsSource);
                }
                else
                {
                    if (GetItemSelectedState(item))
                    {
                        Text += item.GetType().GetProperty(NamePath).GetValue(item) + ",";
                    }
                }
            }
        }
        private bool GetItemSelectedState(object item)
        {
            var selectProperty = item.GetType().GetProperty(SelectedPath);
            if (selectProperty != null)
            {
                var value = selectProperty.GetValue(item);
                if (value is bool)
                {
                    return (bool)value;
                }
            }
            return false;
        }
        private void SetItemSelectedState(object item, bool? value)
        {
            var selectProperty = item.GetType().GetProperty(SelectedPath);

            if (selectProperty != null)
            {
                try
                {
                    selectProperty.SetValue(item, value);
                }
                catch
                {
                    selectProperty.SetValue(item, false);
                }
            }
        }
        private void SetAllChildNodeState(IEnumerable<object> nodes, bool isSelect)
        {
            foreach (var item in nodes)
            {
                var itemType = item.GetType();
                var collections = itemType.GetProperty(ItemSourcePath)?.GetValue(item) as IEnumerable<object>;
                if (collections != null && collections.Count() > 0)
                {
                    SetAllChildNodeState(collections, isSelect);
                }
                SetItemSelectedState(item, isSelect);
            }
        }
    }
}