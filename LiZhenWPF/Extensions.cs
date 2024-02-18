using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Reflection;
using LiZhenStandard.Extensions;
using System.Globalization;
using LiZhenStandard;
using System.Windows.Threading;
using System.Diagnostics;

namespace LiZhenWPF
{
    public static class Extensions
    {
        private static Style clickTextBlockStyle = null;
        private static Style uPropertyNameTextBlockStyle = null;
        private static Style uSingleTextBoxStyle = null;
        private static Style uButtonStyle = null;
        private static Style uComboBoxItemStyle = null;

        public static T GetElementUnderMouse<T>() where T : UIElement
        {
            return FindVisualParent<T>(Mouse.DirectlyOver as UIElement);
        }
        public static T FindVisualParent<T>(UIElement element) where T : UIElement
        {
            UIElement parent = element;
            while (parent != null)
            {
                var correctlyTyped = parent as T;

                if (correctlyTyped != null)
                {
                    return correctlyTyped;
                }

                parent = VisualTreeHelper.GetParent(parent) as UIElement;
            }

            return null;
        }
        /// <summary>
        /// 在一个ItemCollection中查找一个包含特定Content的Item。
        /// </summary>
        /// <param name="items">要查找的ItemCollection</param>
        /// <param name="content">目标Content对象</param>
        /// <returns>查找到的Item</returns>
        public static object FindItemByContent(this ItemCollection items, object content)
        {
            foreach (object item in items)
            {
                Debug.WriteLine(item.GetType().Name);
                if (item != null && item is ComboBoxItem)
                {
                    var itm = item as ComboBoxItem;
                    if (itm.Content.Equals(content))
                        return itm;
                }
                if (item != null && item is ListViewItem)
                {
                    var itm = item as ListViewItem;
                    if (itm.Content.Equals(content))
                        return itm;
                }
            }
            return null;
        }
        /// <summary>
        /// 一个支持点击的TextBlock样式。
        /// </summary>
        public static Style ClickTextBlockStyle
        {
            get
            {
                if (clickTextBlockStyle is null)
                {
                    clickTextBlockStyle = new Style(typeof(TextBlock));
                    var MouseEnterEvent = EventManager.GetRoutedEvents().First(a => a.Name == "MouseEnter");
                    var MouseLeaveEvent = EventManager.GetRoutedEvents().First(a => a.Name == "MouseLeave");
                    var MouseDownEvent = EventManager.GetRoutedEvents().First(a => a.Name == "MouseDown");
                    var MouseUpEvent = EventManager.GetRoutedEvents().First(a => a.Name == "MouseUp");
                    var MouseEnterHandler = new MouseEventHandler((sender, e) => { ((TextBlock)sender).Foreground = new SolidColorBrush(Colors.Blue); });
                    var MouseLeaveHandler = new MouseEventHandler((sender, e) => { ((TextBlock)sender).Foreground = new SolidColorBrush(Colors.Black); });
                    var MouseDownHandler = new MouseButtonEventHandler((sender, e) => { ((TextBlock)sender).Foreground = new SolidColorBrush(Colors.DodgerBlue); });
                    var MouseUpHandler = new MouseButtonEventHandler((sender, e) => { ((TextBlock)sender).Foreground = new SolidColorBrush(Colors.Blue); });
                    clickTextBlockStyle.Setters.Add(new EventSetter() { Event = MouseEnterEvent, Handler = MouseEnterHandler });
                    clickTextBlockStyle.Setters.Add(new EventSetter() { Event = MouseLeaveEvent, Handler = MouseLeaveHandler });
                    clickTextBlockStyle.Setters.Add(new EventSetter() { Event = MouseDownEvent, Handler = MouseDownHandler });
                    clickTextBlockStyle.Setters.Add(new EventSetter() { Event = MouseUpEvent, Handler = MouseUpHandler });
                    return clickTextBlockStyle;
                }
                else
                    return clickTextBlockStyle;
            }
        }
        public static Style UPropertyNameTextBlockStyle
        {
            get
            {
                if (uPropertyNameTextBlockStyle is null)
                {
                    uPropertyNameTextBlockStyle = new Style(typeof(TextBlock));
                    uPropertyNameTextBlockStyle.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Right));
                    uPropertyNameTextBlockStyle.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
                    uPropertyNameTextBlockStyle.Setters.Add(new Setter(TextBlock.FontSizeProperty, (double)14));
                    return uPropertyNameTextBlockStyle;
                }
                else
                    return uPropertyNameTextBlockStyle;
            }
        }
        public static Style USingleTextBoxStyle
        {
            get
            {
                if (uSingleTextBoxStyle is null)
                {
                    uSingleTextBoxStyle = new Style(typeof(TextBox));
                    uSingleTextBoxStyle.Setters.Add(new Setter(TextBox.VerticalAlignmentProperty, VerticalAlignment.Center));
                    uSingleTextBoxStyle.Setters.Add(new Setter(TextBox.VerticalContentAlignmentProperty, VerticalAlignment.Center));
                    uSingleTextBoxStyle.Setters.Add(new Setter(TextBox.FontSizeProperty, (double)14));
                    uSingleTextBoxStyle.Setters.Add(new Setter(TextBox.AcceptsReturnProperty, false));
                    return uSingleTextBoxStyle;
                }
                else
                    return uSingleTextBoxStyle;
            }
        }
        public static Style UButtonStyle
        {
            get
            {
                if (uButtonStyle is null)
                {
                    uButtonStyle = new Style(typeof(Button));
                    uButtonStyle.Setters.Add(new Setter(Button.WidthProperty, 72.0));
                    uButtonStyle.Setters.Add(new Setter(Button.HeightProperty, 24.0));
                    uButtonStyle.Setters.Add(new Setter(Button.VerticalAlignmentProperty, VerticalAlignment.Center));
                    uButtonStyle.Setters.Add(new Setter(Button.HorizontalAlignmentProperty, HorizontalAlignment.Center));
                    uButtonStyle.Setters.Add(new Setter(Button.FontSizeProperty, 14.0));
                    return uButtonStyle;
                }
                else
                    return uButtonStyle;
            }
        }
        public static Style UComboBoxItemStyle
        {
            get
            {
                if (uComboBoxItemStyle is null)
                {
                    uComboBoxItemStyle = new Style(typeof(ComboBoxItem));
                    uComboBoxItemStyle.Setters.Add(new Setter(ComboBoxItem.HorizontalContentAlignmentProperty, HorizontalAlignment.Left));
                    uComboBoxItemStyle.Setters.Add(new Setter(ComboBoxItem.VerticalContentAlignmentProperty, VerticalAlignment.Center));
                    return uComboBoxItemStyle;
                }
                else
                    return uComboBoxItemStyle;
            }
        }

        public static void SetListViewEditTemplate<T>(ListView listView,IList<T> bindingList,Func<string,T> makeT, PropertyInfo textProperty,UIElement addButton,UIElement removeButton,UIElement upButton,UIElement downButton)
        {
            addButton.GetType().GetEvent("Click").AddEventHandler(addButton, new RoutedEventHandler((obj, e) =>
             {
                BindingOperations.ClearBinding(listView, ListView.ItemsSourceProperty);
                listView.Items.Clear();
                TextBox tb = new() { MinWidth = 65, Focusable = true };
                foreach (var item in bindingList)
                {
                    listView.Items.Add(item);
                }
                tb.LostKeyboardFocus += (obj, e) =>
                {
                    e.Handled = false;
                    if (!string.IsNullOrWhiteSpace(tb.Text))
                    {
                        T tElement = makeT(tb.Text);
                        bindingList.Add(tElement);
                    }
                    listView.Items.Clear();
                    listView.SetBinding(ListView.ItemsSourceProperty, new Binding() { Source = bindingList });
                };
                tb.KeyDown += (obj, e) =>
                {
                    e.Handled = false;
                    if (e.Key == Key.Return || e.Key == Key.Enter)
                        tb.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
                };
                listView.Items.Add(tb);
                listView.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                (Action)(() => { Keyboard.Focus(tb); }));
            }));
            listView.GetType().GetEvent("MouseDoubleClick").AddEventHandler(listView, new MouseButtonEventHandler((obj, e) =>
            {
                if (listView.SelectedItem is null)
                    return;
                T selectedItem = (T)listView.SelectedItem;
                int selectedIndex = listView.SelectedIndex;
                BindingOperations.ClearBinding(listView, ListView.ItemsSourceProperty);
                listView.Items.Clear();
                TextBox tb = new() { MinWidth = 65, Focusable = true,Text = textProperty.GetValue(selectedItem)?.ToString() };
                for (int i = 0; i < bindingList.Count; i++)
                {
                    T item = bindingList[i];
                    if (i == selectedIndex)
                        listView.Items.Add(tb);
                    else
                        listView.Items.Add(item);
                }
                tb.LostKeyboardFocus += (obj, e) =>
                {
                    e.Handled = false;
                    if (!string.IsNullOrWhiteSpace(tb.Text))
                    {
                        textProperty.SetValue(selectedItem, tb.Text);
                    }
                    listView.Items.Clear();
                    listView.SetBinding(ListView.ItemsSourceProperty, new Binding() { Source = bindingList });
                };
                tb.KeyDown += (obj, e) =>
                {
                    e.Handled = false;
                    if (e.Key == Key.Return || e.Key == Key.Enter)
                        tb.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
                };
                listView.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                (Action)(() => { Keyboard.Focus(tb); }));


            }));
            removeButton.GetType().GetEvent("Click").AddEventHandler(removeButton, new RoutedEventHandler((obj, e) =>
            {
                if (listView.SelectedItem is null)
                    return;
                int selectedIndex = listView.SelectedIndex;
                bindingList.Remove((T)listView.SelectedItem);
                if (bindingList.Count >= selectedIndex + 1)
                    listView.SelectedIndex = selectedIndex;
                else
                    listView.SelectedIndex = selectedIndex-1;
            }));
            upButton.GetType().GetEvent("Click").AddEventHandler(upButton, new RoutedEventHandler((obj, e) =>
            {
                if (listView.SelectedItem is null)
                    return;
                T item = (T)listView.SelectedItem;
                int selectedIndex = listView.SelectedIndex;
                int insertIndex = selectedIndex < 1 ? 0 : selectedIndex - 1;
                bindingList.Remove(item);
                bindingList.Insert(insertIndex, item);
                listView.SelectedIndex = insertIndex;
            }));
            downButton.GetType().GetEvent("Click").AddEventHandler(downButton, new RoutedEventHandler((obj, e) =>
            {
                if (listView.SelectedItem is null)
                    return;
                T item = (T)listView.SelectedItem;
                int selectedIndex = listView.SelectedIndex;
                int insertIndex = selectedIndex >= bindingList.Count-1 ? selectedIndex : selectedIndex+1;
                bindingList.Remove(item);
                bindingList.Insert(insertIndex, item);
                listView.SelectedIndex = insertIndex;
            }));
        }
        public static void SetListViewCheckBoxTemplate<T>(this ListView listView,IList<T> sourceItems,IList<T> selectedItems,RoutedEventHandler checkHandler, RoutedEventHandler unCheckHandler)
        {
            listView.Items.Clear();
            for (int i = 0; i < sourceItems.Count; i++)
            {
                T item = sourceItems[i];
                CheckBox box = new() { Content = item };
                if (selectedItems.Contains(item))
                    box.IsChecked = true;
                listView.Items.Add(box);
                box.Checked += (obj, e) => { checkHandler?.Invoke(obj,e); };
                box.Unchecked += (obj, e) => { unCheckHandler?.Invoke(obj,e); };
            }
        }
        public static ChildType FindVisualChild<ChildType>(DependencyObject obj) where ChildType : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child.GetType() == typeof(ChildType))
                {
                    return child as ChildType;
                }
                else
                {
                    ChildType childOfChildren = FindVisualChild<ChildType>(child);
                    if (childOfChildren != null)
                    {
                        return childOfChildren;
                    }
                }
            }
            return null;
        }

        public static void ShowTip(string tipContent)
        {
            MessageBox.Show(tipContent, "温馨提示");
        }
        public static bool ShowChoice(string content)
        {
            var re = MessageBox.Show(content, "温馨提示",MessageBoxButton.YesNo);
            return re == MessageBoxResult.Yes;
        }

        public static void AutoBinding(this Window window, object bindingObj)
        {
            if (window is null)
                return;
            if (window.Content is null)
                return;

            UIElement[] Elements = GetAllControls(window.Content);
            for (int i = 0; i < Elements?.Length; i++)
            {
                UIElement element = Elements[i];
                string name = (string)element.GetPropertyValue("Name");
                if (string.IsNullOrEmpty(name.ToString()))
                    continue;
                if (name.IsMatch(@"(?<=abd\d*_).+", out string abdName))
                    AutoSetBinding(element, bindingObj, abdName);
            }
        }
        public static void AutoSetBinding(UIElement uiElement, object bindingObj, string propertyName)
        {
            PropertyInfo objPPT = bindingObj.GetType().GetProperty(propertyName??"");
            //if (objPPT is null)
            //    return;
            bool CanWrite = objPPT is null ? false : objPPT.CanWrite;
            DependencyProperty dp = null;
            IValueConverter ivc = null;

            if (uiElement is TextBlock)
                dp = TextBlock.TextProperty;
            else if (uiElement is TextBox)
                dp = TextBox.TextProperty;
            else if (uiElement is Label)
                dp = Label.ContentProperty;
            else if (uiElement is DatePicker)
                dp = DatePicker.TextProperty;
            else if (uiElement is CheckBox)
                dp = CheckBox.IsCheckedProperty;
            else if (uiElement is ComboBox)
            {
                dp = ComboBox.ItemsSourceProperty;
                ivc = new ComboBoxEnumItemSourceConverter();

                ((FrameworkElement)uiElement).SetBinding(ComboBox.SelectedItemProperty, new Binding(propertyName)
                {
                    Source = bindingObj,
                    Mode = CanWrite ? BindingMode.TwoWay : BindingMode.OneWay,
                    Converter = new EnumValueConverter()
                });
            }
            else if (uiElement is ListView)
                dp = ListView.ItemsSourceProperty;
            else if (uiElement is ListBox)
                dp = ListBox.ItemsSourceProperty;
            else if (uiElement is TreeView)
                dp = TreeView.ItemsSourceProperty;
            else if (uiElement is DataGrid)
                dp = DataGrid.ItemsSourceProperty;
            else
                dp = null;

            if (dp != null)
            {
                //((FrameworkElement)uiElement).SetBinding(dp, "");
                ((FrameworkElement)uiElement).SetBinding(dp, new Binding(propertyName)
                {
                    Source = bindingObj,
                    Mode = CanWrite ? BindingMode.TwoWay : BindingMode.OneWay,
                    Converter = ivc
                });
            } 
        }
        public static UIElement[] GetAllControls(object uiElement)
        {
            if (uiElement is null)
                return null;

            List<UIElement> re = new List<UIElement>();

            var ppt = uiElement.GetType().GetProperty("Children");
            if (ppt is null)
                return null;

            UIElementCollection uiec = (UIElementCollection)ppt.GetValue(uiElement);
            for (int i = 0; i < uiec.Count; i++)
            {
                object item = uiec[i];
                var ctrls = GetAllControls(item);
                if (!(ctrls is null))
                    re.AddRange(ctrls);

                re.Add((UIElement)item);

            }
            return re.ToArray();
        }

        public static UIElement[] GetAllTagControls(object tag, object uiElement)
        {
            if (uiElement is null)
                return null;

            List<UIElement> re = new List<UIElement>();

            var ppt = uiElement.GetType().GetProperty("Children");
            if (ppt is null)
                return null;

            UIElementCollection uiec = (UIElementCollection)ppt.GetValue(uiElement);
            for (int i = 0; i < uiec.Count; i++)
            {
                object item = uiec[i];
                var ctrls = GetAllTagControls(tag, item);
                if (!(ctrls is null))
                    re.AddRange(ctrls);

                var value = item.GetPropertyValue("Tag");
                if (!(value is null))
                    if (value.Equals(tag))
                        re.Add((UIElement)item);
            }
            return re.ToArray();
        }
        public static void SetTagControlsProperty(this Window window, object tag, DependencyProperty dp, object value)
        {
            SetTagControlsProperty(window.Content, tag, dp, value);
        }
        public static void SetTagControlsProperty(object uiElement, object tag, DependencyProperty dp, object value)
        {
            UIElement[] allEmt = GetAllTagControls(tag, uiElement);
            SetControlsProperty(dp, value, allEmt);
        }
        public static void SetAllUniversalStyle(this Window window)
        {
            UIElement[] allEmt = GetAllTagControls("_UStyle", window.Content);
            UIElement[] alltxb = GetAllTagControls("_ClickStyle", window.Content);
            foreach (UIElement ui in allEmt)
            {
                if (ui is TextBlock uiTBlock)
                {
                    SetControlsProperty(TextBlock.StyleProperty, UPropertyNameTextBlockStyle, uiTBlock);
                }
                else if (ui is TextBox uiTBox)
                {
                    SetControlsProperty(TextBox.StyleProperty, USingleTextBoxStyle, uiTBox);
                }
                else if (ui is Button uiBtn)
                {
                    SetControlsProperty(Button.StyleProperty, UButtonStyle, uiBtn);
                }
                else if (ui is ComboBox uiComb)
                {
                    SetControlsProperty(ComboBox.ItemContainerStyleProperty, UComboBoxItemStyle, uiComb);
                }
            }
            foreach (UIElement ui in alltxb)
            {
                if (ui is TextBlock uiTBlock)
                {
                    SetControlsProperty(TextBlock.StyleProperty, ClickTextBlockStyle, uiTBlock);
                }
            }
        }
        public static void SetControlsProperty(DependencyProperty dp, object value, params UIElement[] uiElements)
        {
            foreach (var item in uiElements)
            {
                try
                {
                    item.SetValue(dp, value);
                }
                catch { }
            }
        }
        public static void ShowOrHideControls(bool showOrHide, params UIElement[] uiElements)
        {
            Visibility v = showOrHide ? Visibility.Visible : Visibility.Hidden;
            foreach (var item in uiElements)
            {
                item.Visibility = v;
            }
        }
        public static void EnableOrNotControls(bool isEnabled, params UIElement[] uiElements)
        {
            foreach (var item in uiElements)
            {
                item.IsEnabled = isEnabled;
            }
        }
        public static void ShowAndEnableControls(bool showAndEnable, params UIElement[] uiElements)
        {
            ShowOrHideControls(showAndEnable, uiElements);
            EnableOrNotControls(showAndEnable, uiElements);
        }

        /// <summary>
        /// 获取一个ListView中所有被选中的对象。（这个ListView的Item必须全部是CheckBox类型，且CheckBox的Content必须与该函数泛型类型一致。）
        /// </summary>
        /// <typeparam name="T">该ListView的Items，CheckBox的Content的类型</typeparam>
        /// <param name="listView">要执行该函数的ListView</param>
        /// <returns>被选中的对象</returns>
        public static T[] GetSelectedItemContents<T>(this ListView listView)
        {
            List<T> re = new List<T>();
            foreach (var item in listView.Items)
            {
                if (item is CheckBox itembox)
                {
                    if (itembox.IsChecked.Value)
                        try
                        {
                            re.Add((T)itembox.Content);
                        }
                        catch { }
                }
                else
                    break;
            }
            return re.ToArray();
        }

        public static void MakePropertyViewGrid(this ScrollViewer sv, object obj)
        {
            if (obj is null)
                return;
            Grid grid = new();
            Type t = obj.GetType();
            PropertyInfo[] ppts = t.GetProperties();
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            //List<PropertyInfo> ppts = new();
            //for (int i = 0; i < _ppts.Length; i++)
            //{
            //    PropertyInfo ppt = _ppts[i];
            //    var disAttr = ppt.GetCustomAttribute<DisplayAttribute>();
            //    if (disAttr is null || ppt.GetIndexParameters().Any())
            //        continue;
            //    else
            //        ppts.Add(ppt);
            //}
            int row = 0;
            for (int i = 0; i < ppts.Length; i++)
            {
                PropertyInfo ppt = ppts[i];
                var disAttr = ppt.GetCustomAttribute<DisplayAttribute>();
                if (disAttr is null || ppt.GetIndexParameters().Any())
                    continue;
                string name = disAttr.DisplayName;
                object value = ppt.GetValue(obj);
                TextBlock name_tb = new() { Text = name + "：", FontSize = 14.0, HorizontalAlignment = HorizontalAlignment.Right };
                TextBlock pv_tb = new() { Text = value is null ? "null" : value.ToString(), FontSize = 14.0, HorizontalAlignment = HorizontalAlignment.Left };
                Grid.SetColumn(name_tb, 0);
                Grid.SetColumn(pv_tb, 1);
                Grid.SetRow(name_tb, row);
                Grid.SetRow(pv_tb, row);
                grid.Children.Add(name_tb);
                grid.Children.Add(pv_tb);
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });
                row++;
            }
            sv.Content = grid;
        }

        /// <summary>
        /// 根据提供的集合，选中一个ListView中的CheckBox所对应的项。
        /// </summary>
        /// <typeparam name="T">项目的类型（必须保持一致）</typeparam>
        /// <param name="listView">要进行操作的ListView</param>
        /// <param name="itemContents">选中项的集合</param>
        public static void SelectItemContents<T>(this ListView listView, T[] itemContents)
        {
            for (int i = 0; i < listView.Items.Count; i++)
            {
                object item = listView.Items[i];
                if (item is CheckBox box)
                    if (box.Content is T obj)
                    {
                        if (itemContents.Contains(obj))
                            box.IsChecked = true;
                    }
            }
        }

        /// <summary>
        /// 设置平板电脑的触摸方式为左手模式，主要目的是为了让弹出菜单的方向为从左向右。
        /// </summary>
        public static void SetAlignment()
        {
            //获取系统是以Left-handed（true）还是Right-handed（false）
            var ifLeft = SystemParameters.MenuDropAlignment;

            if (ifLeft)
            {
                var t = typeof(SystemParameters);
                var field = t.GetField("_menuDropAlignment", BindingFlags.NonPublic | BindingFlags.Static);
                field.SetValue(null, false);
            }
        }

        public static void ExpandAll(this TreeView treeView)
        {
            ExpandInternal(treeView);
        }
        private static void ExpandInternal(ItemsControl targetItemContainer)
        {
            if (targetItemContainer == null) return;
            if (targetItemContainer.Items == null) return;
            for (int i = 0; i < targetItemContainer.Items.Count; i++)
            {
                TreeViewItem treeItem = targetItemContainer.Items[i] as TreeViewItem;
                if (treeItem == null) continue;
                if (!treeItem.HasItems) continue;

                treeItem.IsExpanded = true;
                ExpandInternal(treeItem);
            }

        }

        public static DependencyObject GetVisualTreeParent(DependencyObject dependency,int n)
        {
            if (dependency is null)
                return null;
            DependencyObject re = VisualTreeHelper.GetParent(dependency);
            for (int i = 1; i < n; i++)
            {
                re = VisualTreeHelper.GetParent(re);
                if (re is null)
                    break;
            }
            return re;
        }
        public static DependencyObject GetVisualTreeParent(DependencyObject dependency, Type targetType)
        {
            if (dependency is null)
                return null;
            DependencyObject re = VisualTreeHelper.GetParent(dependency);
            if (re.GetType() == targetType)
                return re;
            for (int i = 0; i < 32; i++)
            {
                re = VisualTreeHelper.GetParent(re);
                if (re is null)
                    break;
                if (re.GetType() == targetType)
                    return re;
            }
            return re;
        }
    }

    [ValueConversion(typeof(Enum), typeof(string))]
    public class EnumValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Enum enumValue = value as Enum;
            if (enumValue != null)
            {
                Type t = value.GetType();
                var md = typeof(EnumDisplayAttribute).GetMethod("GetEnumDisplayName");
                if (md is null)
                    return value.ToString();
                var mdf = md.MakeGenericMethod(t);
                return mdf.Invoke(null,new object[] { value });
            }
            return string.Format("{0}", value);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //throw new NotImplementedException();
            string strValue = value as string;
            if (!string.IsNullOrWhiteSpace(strValue))
            {
                Type t = targetType;
                Array enumValues = Enum.GetValues(t);
                Dictionary<string, object> dic = new();
                //object[] valueObjs = new object[enumValues.Length];
                var md = typeof(EnumDisplayAttribute).GetMethod("GetEnumDisplayName");
                var mdf = md.MakeGenericMethod(t);
                for (int i = 0; i < enumValues.Length; i++)
                {
                    var enumVelue = enumValues.GetValue(i);
                    object key = mdf.Invoke(null, new object[] { enumVelue });
                    //Debug.Write(key);
                    dic.Add(key?.ToString(), enumVelue);
                }
                return dic[strValue];
            }
            return null;
        }
    }

    [ValueConversion(typeof(Enum), typeof(Array))]
    public class ComboBoxEnumItemSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Enum enumValue = value as Enum;
            if (enumValue != null)
            {
                Type t = value.GetType();
                var md = typeof(EnumDisplayAttribute).GetMethod("GetEnumDisplayName");
                var values = Enum.GetValues(t);

                if (md is null)
                    return values;

                object[] re = new object[values.Length];
                var mdf = md.MakeGenericMethod(t);
                for (int i = 0; i < values.Length; i++)
                {
                    object v = values.GetValue(i);
                    re[i] = mdf.Invoke(null, new object[] { v });
                }
                return re;
            }
            return null;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
