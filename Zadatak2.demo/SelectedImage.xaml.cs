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
    public sealed partial class SelectedImage : UserControl
    {
        public delegate void FileActionCompletedDelegate(int id, object sender);
        public int ID { get; set; }
        public event FileActionCompletedDelegate FileRemoved;
        private StorageFile file;
        private AssignmentData assignmentData;
        public SelectedImage(AssignmentData assignmentData)
        {
            this.assignmentData = assignmentData;
            this.InitializeComponent();
            this.file = assignmentData.SourceFile;
            ID = assignmentData.ID;
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
            FileRemoved?.Invoke(ID, this);
        }

        private async void PropertiesButton_Click(object sender, RoutedEventArgs e)
        {
            EncoderDialog enc = new EncoderDialog(this.assignmentData);
            ContentDialogResult result = await enc.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                (assignmentData.Angle, assignmentData.Height, assignmentData.Width, assignmentData.Effect) = enc.getData();
            }
        }
    }
}
