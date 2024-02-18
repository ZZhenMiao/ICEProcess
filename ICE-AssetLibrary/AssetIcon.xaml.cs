using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Image = System.Windows.Controls.Image;

namespace ICE_AssetLibrary
{
    /// <summary>
    /// AssetIcon.xaml 的交互逻辑
    /// </summary>
    public partial class AssetIcon : UserControl, INotifyPropertyChanged
    {
        public static Dictionary<string, BitmapImage> Bitmaps { get; } = new Dictionary<string, BitmapImage>();

        public string AssetName
        {
            get { return (string)GetValue(AssetNameProperty); }
            set { SetValue(AssetNameProperty, value); OnAssetNameChanged("AssetName"); }
        }
        public static readonly DependencyProperty AssetNameProperty =
            DependencyProperty.Register("AssetName", typeof(string), typeof(AssetIcon), new PropertyMetadata("未命名资产"));

        private void OnAssetNameChanged(string value)
        {
            this.nameTextBlock.Text = value;
        }

        public string FullPath
        {
            get { return (string)GetValue(FullPathProperty); }
            set
            {
                SetValue(FullPathProperty, value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FullPath"));
            }
        }

        // Using a DependencyProperty as the backing store for FullPath.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FullPathProperty =
            DependencyProperty.Register("FullPath", typeof(string), typeof(AssetIcon), new PropertyMetadata(""));

        public AssetPreviewType AssetPreviewType
        {
            get { return (AssetPreviewType)GetValue(AssetPreviewTypeProperty); }
            set { SetValue(AssetPreviewTypeProperty, value); OnAssetNameChanged("AssetPreviewType"); }
        }
        public static readonly DependencyProperty AssetPreviewTypeProperty =
            DependencyProperty.Register("AssetPreviewType", typeof(AssetPreviewType), typeof(AssetIcon), new PropertyMetadata(AssetPreviewType.Other));

        //public bool IsSelected
        //{
        //    get { return (bool)GetValue(IsSelectedProperty); }
        //    set
        //    {
        //        SetValue(IsSelectedProperty, value);
        //        if (value)
        //            OnSelected?.Invoke(this, new RoutedEventArgs());
        //        else
        //            UnSelected?.Invoke(this, new RoutedEventArgs());

        //    }
        //}
        //public static readonly DependencyProperty IsSelectedProperty =
        //    DependencyProperty.Register("IsSelected", typeof(bool), typeof(AssetIcon), new PropertyMetadata(false));

        //public delegate void SelectedEventHandler(object sender, RoutedEventArgs e);
        //public event SelectedEventHandler OnSelected;
        //public event SelectedEventHandler UnSelected;

        //public AssetPreviewType AssetPreviewType 
        //{ 
        //    get 
        //    {
        //        if (string.IsNullOrWhiteSpace(FullPath))
        //            return AssetPreviewType.Unfound;
        //        if (string.IsNullOrWhiteSpace(System.IO.Path.GetExtension(FullPath)) && Directory.Exists(FullPath))
        //        {
        //            return AssetPreviewType.Folder;
        //        }
        //        if (!File.Exists(FullPath))
        //        {
        //            return AssetPreviewType.Unfound;
        //        }
        //        string exn = System.IO.Path.GetExtension(FullPath).ToLower();
        //        if (App.ImageEx.Contains(exn))
        //        {
        //            return AssetPreviewType.Image;
        //        }
        //        if (App.ModelEx.Contains(exn))
        //        {
        //            return AssetPreviewType.Model;
        //        }
        //        if (App.SoundEx.Contains(exn))
        //        {
        //            return AssetPreviewType.Sound;
        //        }
        //        if (App.VideoEx.Contains(exn))
        //        {
        //            return AssetPreviewType.Video;
        //        }
        //        return AssetPreviewType.Document;
        //    }
        //}
        public static BitmapSource GetImageSouce(Bitmap bitmap)
        {
            BitmapSource img;
            IntPtr hBitmap;

            hBitmap = bitmap.GetHbitmap();
            img = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            return img;
        }

        //public void BindingPreview_Out()
        //{
        //    BindingPreview(FullPath);
        //    if (AssetPreviewType == AssetPreviewType.Video && Media_MediaElement.Source is not null)
        //    {
        //        Media_MediaElement.Play(); Media_MediaElement.Pause();
        //    }
        //}

        public async Task<BitmapImage> UriToImageAsync(string uri)
        {
            if (string.IsNullOrEmpty(uri))
                return new BitmapImage();
            return await System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    // 读取图片源文件到byte[]中 
                    var filestream = File.Open(uri, FileMode.Open);
                    BinaryReader binReader = new BinaryReader(filestream);
                    FileInfo fileInfo = new FileInfo(uri);
                    byte[] bytes = binReader.ReadBytes((int)fileInfo.Length);
                    binReader.Close();
                    // 将图片字节赋值给BitmapImage 
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.DecodePixelHeight = (int)100;
                    bitmap.BeginInit();
                    bitmap.StreamSource = new MemoryStream(bytes);
                    bitmap.EndInit();
                    filestream.Close();
                    bitmap.Freeze();
                    return bitmap;


                    //BitmapImage image = new BitmapImage();
                    //image.BeginInit();
                    //image.CacheOption = BitmapCacheOption.None;
                    //image.UriSource = new Uri(uri);
                    //image.DecodePixelHeight = (int)100;
                    //image.EndInit();
                    //image.Freeze();
                    //return image;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + " >" + uri);
                    return null;
                }
            });
        }

        //public async Task<BitmapImage> UriToImageAsync(string uri)
        //{
        //    if (string.IsNullOrEmpty(uri))
        //        return new BitmapImage();
        //    return await System.Threading.Tasks.Task.Run(() =>
        //    {
        //        try
        //        {
        //            BitmapImage image = new BitmapImage();
        //            image.BeginInit();
        //            image.CacheOption = BitmapCacheOption.None;
        //            image.UriSource = new Uri(uri);
        //            image.DecodePixelHeight = (int)100;
        //            image.EndInit();
        //            image.Freeze();
        //            return image;
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine(ex.Message + " >" + uri);
        //            return null;
        //        }
        //    });
        //}


        //public BitmapImage UriToImage(string uri)
        //{
        //    if (string.IsNullOrEmpty(uri))
        //        return new BitmapImage();
        //    try
        //    {
        //        BitmapImage image = new BitmapImage();
        //        image.BeginInit();
        //        image.CacheOption = BitmapCacheOption.None;
        //        image.UriSource = new Uri(uri);
        //        //Stream stream = new FileStream(uri, FileMode.Open);
        //        //image.StreamSource = stream;
        //        image.DecodePixelHeight = (int)100;
        //        image.EndInit();
        //        //stream.Close();
        //        //stream.Dispose();
        //        return image;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message + " >" + uri);
        //        return new BitmapImage();
        //    }
        //}

        public void SetImageSource(BitmapImage bitmapImage)
        {
            //Image image = new Image();
            //image.Source = bitmapImage;
            //this.Dispatcher.Invoke(() => { MainGrid_Grid.Children.Add(image); });
            //UriToImageAsync(FullPath);
            //Image_Image.Source = _imageCache;

            //Image_Image.SetBinding(Image.SourceProperty, new Binding("FullPath") { Source = this, Converter = new Converter_String_ImageSource() });
            //Image_Image.Source = bitmapImage; UriToImage(FullPath);
            //Image_Image.Source = _imageCache;

        }
        public async void BindingPreview()
        {
            switch (AssetPreviewType)
            {
                case AssetPreviewType.Image:
                    BitmapImage image = await UriToImageAsync(FullPath);
                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        //Image_Image.Source = image;
                        Image_Image.SetBinding(Image.SourceProperty, new Binding() { Source = image, IsAsync = true });
                        //Image_Image.SetBinding(Image.SourceProperty, new Binding("FullPath") { Source = this, Converter = new Converter_String_ImageSource(), IsAsync = true });
                    }, DispatcherPriority.SystemIdle);
                    break;
                case AssetPreviewType.Video:
                    Image_Image.Visibility = Visibility.Hidden;
                    Media_MediaElement.Visibility = Visibility.Visible;
                    Media_MediaElement.SetBinding(MediaElement.SourceProperty, new Binding("FullPath") { Source = this, Converter = new Converter_String_Uri(), IsAsync = true });
                    if (Media_MediaElement.Source is not null)
                    {
                        Media_MediaElement.Play(); Media_MediaElement.Pause();
                    }
                    break;
                case AssetPreviewType.Sound:
                    Image_Image.Source = ByteArrayToBitmapImage(Properties.Resources.Sound01);
                    Media_MediaElement.Visibility = Visibility.Visible;
                    Media_MediaElement.SetBinding(MediaElement.SourceProperty, new Binding("FullPath") { Source = this, Converter = new Converter_String_Uri(), IsAsync = true });
                    break;
                case AssetPreviewType.Model:
                    Image_Image.Source = GetImageSouce(Properties.Resources.ThreeD01);
                    break;
                case AssetPreviewType.Document:
                    Image_Image.Source = ByteArrayToBitmapImage(Properties.Resources.Document01);
                    break;
                case AssetPreviewType.Folder:
                    Image_Image.Source = GetImageSouce(Properties.Resources.Folder01);
                    break;
                case AssetPreviewType.Unfound:
                    Image_Image.Source = ByteArrayToBitmapImage(Properties.Resources.Unfounded02);
                    break;
                case AssetPreviewType.Other:
                    Image_Image.Source = ByteArrayToBitmapImage(Properties.Resources.Unfounded02);
                    break;
                default:
                    Image_Image.Source = ByteArrayToBitmapImage(Properties.Resources.Unfounded02);
                    break;
            }

            //string exn = System.IO.Path.GetExtension(fullpath).ToLower();
            //if (string.IsNullOrWhiteSpace(System.IO.Path.GetExtension(fullpath)) && Directory.Exists(fullpath))
            //{
            //    Image_Image.Source = GetImageSouce(Properties.Resources.Folder01);
            //    return;
            //}
            //if (!File.Exists(fullpath))
            //{
            //    Image_Image.Source = ByteArrayToBitmapImage(Properties.Resources.Unfounded02);
            //    return;
            //}
            //if (ImageEx.Contains(exn))
            //{
            //    //var map = await UriToImageAsync(fullpath);
            //    //Image_Image.Source = map;;
            //    //Debug.WriteLine(map);
            //    //Image_Image.SetBinding(Image.SourceProperty, new Binding() { Source = cache, IsAsync = true });
            //    Image_Image.SetBinding(Image.SourceProperty, new Binding("FullPath") { Source = this, Converter = new Converter_String_ImageSource(),IsAsync = true });
            //    this.AssetPreviewType = AssetPreviewType.Image;
            //    return;
            //}
            //if (ModelEx.Contains(exn))
            //{
            //    Image_Image.Source = GetImageSouce(Properties.Resources.ThreeD01);
            //    this.AssetPreviewType = AssetPreviewType.Model;
            //    return;
            //}
            //if (SoundEx.Contains(exn))
            //{
            //    Image_Image.Source = ByteArrayToBitmapImage(Properties.Resources.Sound01);
            //    Media_MediaElement.Visibility = Visibility.Visible;
            //    Media_MediaElement.SetBinding(MediaElement.SourceProperty, new Binding("FullPath") { Source = this, Converter = new Converter_String_Uri(),IsAsync = true });
            //    this.AssetPreviewType = AssetPreviewType.Sound;
            //    return;
            //}
            //if (VideoEx.Contains(exn))
            //{
            //    Image_Image.Visibility = Visibility.Visible;
            //    Media_MediaElement.Visibility = Visibility.Visible;
            //    Media_MediaElement.SetBinding(MediaElement.SourceProperty, new Binding("FullPath") { Source = this, Converter = new Converter_String_Uri() , IsAsync = true });
            //    this.AssetPreviewType = AssetPreviewType.Video;
            //    return;
            //}
            //Image_Image.Source = ByteArrayToBitmapImage(Properties.Resources.Document01);
        }

        public static BitmapImage ByteArrayToBitmapImage(byte[] byteArray)
        {
            BitmapImage bmp = null;
            try
            {
                bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.StreamSource = new MemoryStream(byteArray);
                bmp.EndInit();
            }
            catch
            {
                bmp = null;
            }
            return bmp;
        }

        bool playing = false;
        public AssetIcon()
        {
            InitializeComponent();
            nameTextBlock.SetBinding(TextBlock.TextProperty, new Binding("AssetName") { Source = this });
            //this.Loaded += (obj, e) =>
            //{
            //    BindingPreview(FullPath);
            //};
            //this.Media_MediaElement.Loaded += (obj, e) =>
            //{
            //    if (Media_MediaElement.Source is not null)
            //    {
            //        Media_MediaElement.Play(); Media_MediaElement.Pause();
            //    }
            //};
            this.Media_MediaElement.MediaEnded += (obj, e) =>
            {
                Media_MediaElement.Position = new TimeSpan(1);
            };
            //this.MouseDoubleClick += (obj, e) =>
            //{
            //    if (File.Exists(FullPath) || Directory.Exists(FullPath))
            //        App.ProgramStart(FullPath);
            //};
            this.MouseDown += (obj, e) =>
            {
                if (AssetPreviewType == AssetPreviewType.Video)
                {
                    if (playing == false)
                    {
                        Media_MediaElement.Play();
                        playing = true;
                    }
                    else
                    {
                        Media_MediaElement.Pause();
                        playing = false;
                    }
                }
            };
            this.MouseEnter += (obj, e) =>
            {
                if (AssetPreviewType == AssetPreviewType.Sound)
                {
                    if (playing == false)
                    {
                        Media_MediaElement.Play();
                        playing = true;
                    }
                }

            };
            this.MouseLeave += (obj, e) =>
            {
                if (AssetPreviewType == AssetPreviewType.Sound)
                {
                    if (playing == true)
                    {
                        Media_MediaElement.Stop();
                        playing = false;
                    }
                }
            };

            //OnSelected += (obj, e) => { Background = new SolidColorBrush(Colors.LightSkyBlue); };
            //UnSelected += (obj, e) => { Background = null; };
            //MouseLeftButtonDown += (obj, e) => { IsSelected = !IsSelected; };
            //MouseEnter += (obj, e) => 
            //{ 
            //    if (Mouse.LeftButton == MouseButtonState.Pressed)
            //    { 
            //        IsSelected = Keyboard.IsKeyDown(Key.LeftShift)?true:false; 
            //    }
            //};
        }
    }
    public enum AssetPreviewType { Image, Video, Sound, Model, Document, Folder, Unfound, Other }

    public class Converter_String_ImageSource : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string)
                return new BitmapImage();
            string uri = (string)value;
            if (string.IsNullOrEmpty(uri))
                return new BitmapImage();
            try
            {
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.None;
                //image.UriSource = new Uri(uri);
                Stream stream = new FileStream(uri, FileMode.Open);
                image.StreamSource = stream;
                //image.DecodePixelHeight = (int)100;
                image.EndInit();
                //stream.Close();
                //stream.Dispose();
                return image;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new BitmapImage();
            }
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class Converter_String_Uri : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string)
                return new Uri("");
            string uri = (string)value;
            try
            {
                return new Uri(uri);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new Uri("");
            }
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
