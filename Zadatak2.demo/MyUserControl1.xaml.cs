using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Zadatak2.demo
{
    public sealed partial class MyUserControl1 : UserControl
    {
        public delegate void FileActionCompletedDelegate(StorageFile file, object sender);
        public event FileActionCompletedDelegate FileRemoved;
        private StorageFile file;
        public MyUserControl1(StorageFile file)
        {
            this.InitializeComponent();
            this.file = file;
            NameTextBlock.Text = file.Name;
            setImage();

        }

        public object ImagePrewiev { get; private set; }

        private async void setImage()
        {
            IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read);
            BitmapDecoder decoder1 = await BitmapDecoder.CreateAsync(stream);
            SoftwareBitmap softwareBitmap = await decoder1.GetSoftwareBitmapAsync();

            SoftwareBitmap softwareBitmapBGR8 = SoftwareBitmap.Convert(softwareBitmap,
        BitmapPixelFormat.Bgra8,
        BitmapAlphaMode.Premultiplied);

            SoftwareBitmapSource bitmapSource = new SoftwareBitmapSource();
            await bitmapSource.SetBitmapAsync(softwareBitmapBGR8);

            SelectedPrewiew.Source = bitmapSource;






        }

        private void Grid_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(sender as Grid);

        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            FileRemoved?.Invoke(file, this);
        }
    }
}
