using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Media.Capture;
using Windows.UI.Core;

using Windows.ApplicationModel;

using Windows.System.Display;
using Windows.Graphics.Display;
using Windows.Storage.Streams;
using Windows.Media.MediaProperties;
using Windows.Graphics.Imaging;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml.Media.Imaging;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Zadatak2.demo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private bool firstTimeNavigated = false;
        private static int number = 0;
        private AssignmentManager manager;
        //private List<StorageFile> pickedFiles = new List<StorageFile>();
        private List<AssignmentData> pickedFiles = new List<AssignmentData>();
        MediaCapture mediaCapture;
        bool isPreviewing =false;
        DisplayRequest displayRequest = new DisplayRequest();
        public MainPage()
        {
            this.InitializeComponent(); manager = (Application.Current as App).Manager;
            Application.Current.Suspending += Application_Suspending;
            

        }
        private async Task RemoveSelected(int id, MyUserControl1 fileDetails)
        {
            pickedFiles.RemoveAll(x => x.ID == id);
            if (fileDetails != null)
                SelectedStackPanel.Children.Remove(fileDetails);



        }

        private async void OdaberiSlike_Clicked(object sender, RoutedEventArgs e)
        {
            //OdaberiSlike.IsEnabled = false;
            FileOpenPicker fileOpenPicker = new FileOpenPicker();
            fileOpenPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            fileOpenPicker.FileTypeFilter.Add(".jpg");
            fileOpenPicker.ViewMode = PickerViewMode.Thumbnail; IReadOnlyList<StorageFile> files;
            files = await fileOpenPicker.PickMultipleFilesAsync();
            if (files != null || files.Count != 0)
            {
                List<StorageFile> distinctFiles = files.Where(a => !pickedFiles.Select(x=>x.SourceFile).ToList().Contains(a)).ToList();
                List<AssignmentData> assignmentDataList = distinctFiles.Select(x=> new AssignmentData(x)).ToList();
                await UpdateSelectedStackPanel(assignmentDataList);
                pickedFiles.AddRange(assignmentDataList);
                
            }


        }

        private async void PokreniObradu_Clicked(object sender, RoutedEventArgs e)
        {
            if (pickedFiles.Count > 0)
            {
                PokreniObradu.IsEnabled = false;
                ParametersDialog content = new ParametersDialog(pickedFiles.Count, Environment.ProcessorCount);
                ContentDialogResult result = await content.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    PokreniObradu.IsEnabled = true;
                    return;
                }
                (int numOfParallelTasks, int coresPerTask) = content.GetSelectedNumbers();

                if (numOfParallelTasks != 0 && coresPerTask != 0)
                {
                    FolderPicker folderPicker = new FolderPicker() { SuggestedStartLocation = PickerLocationId.PicturesLibrary };
                    folderPicker.FileTypeFilter.Add("*");

                    StorageFolder folder = await folderPicker.PickSingleFolderAsync();
                    if (folder != null)
                    {
                        Assignment[] array = manager.addFiles(pickedFiles, folder, coresPerTask);

                        manager.MaxConcurentAssignments = numOfParallelTasks;
                        await InitializeStackPanel(array);
                        await manager.RunAssignments();
                        SelectedStackPanel.Children.Clear();
                        pickedFiles.Clear();



                    }

                    else
                        PokreniObradu.IsEnabled = true;
                }
                else
                    PokreniObradu.IsEnabled = true;
            }

        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if(firstTimeNavigated==false)
            {
                if (manager.Assignments.Count > 0)
                {
                    ContentDialog2 menu = new ContentDialog2();
                    menu.UpdateStackPanel(manager.Assignments);
                    ContentDialogResult r = await menu.ShowAsync();
                    if (r == ContentDialogResult.Secondary)
                    {
                        manager.AssignmentsList = menu.GetAllCheckedAssignments();
                        await InitializeStackPanel(manager.AssignmentsList.ToArray());
                        await manager.RunAssignments();



                    }
                    else
                        manager.AssignmentsList.Clear();
                }
                firstTimeNavigated = true;


            }
            //await InitializeStackPanel(manager.Assignments);
        }

        private async Task InitializeStackPanel(IReadOnlyList<Assignment> assignments)
        {
            foreach (Assignment download in assignments)
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    MyUserControl2 downloadProgressControl = new MyUserControl2(download);
                    downloadProgressControl.AssignmentStarted += DownloadProgressControl_AssignmentStarted;
                    downloadProgressControl.AssignmentPaused += DownloadProgressControl_AssignmentPaused; downloadProgressControl.AssignmentCancelled += DownloadProgressControl_AssignmentCancelled;
                    downloadProgressControl.AssignmentCompleted += DownloadProgressControl_AssignmentCompleted;
                    downloadProgressControl.AssignmentRemoved += DownloadProgressControl_AssignmentRemoved;
                    AssignmentsStackPanel.Children.Add(downloadProgressControl);
                });
        }

        private async void DownloadProgressControl_AssignmentRemoved(Assignment a, object sender)
        {
            await RemoveAssignment(a, sender as MyUserControl2);
        }

        private async void DownloadProgressControl_AssignmentCompleted(Assignment a, object sender)
        {
           // await Task.Run(() =>
           //{
               if (manager.Assignments.All(a => (a.CurrentState == Assignment.AssignmentState.Done) || a.CurrentState == Assignment.AssignmentState.Error || a.CurrentState == Assignment.AssignmentState.Cancelled))
                   PokreniObradu.IsEnabled = true;
            await ToastsSender.NotifyUser(a);



           //});
            
        }

        private async void DownloadProgressControl_AssignmentCancelled(Assignment a, object sender)
        {
            if (a.Paused)
                await a.Resume();
            await a.Cancel();
        }

        private async void DownloadProgressControl_AssignmentPaused(Assignment a, object sender)
        {
            await a.Pause();
        }

        private async void DownloadProgressControl_AssignmentStarted(Assignment a, object sender)
        {
            if (a.Finished)
                await a.Reset();
            else if (a.IsPending)
                await a.Start();
            else if (a.CurrentState == Assignment.AssignmentState.Paused)
                await a.Resume();
           

        }

        private async Task RemoveAssignment(Assignment download, MyUserControl2 downloadProgressControl)
        {
            if (!download.Finished)
                await download.Cancel(true);
            manager.RemoveAssignment(download);
            if (downloadProgressControl != null)
                AssignmentsStackPanel.Children.Remove(downloadProgressControl);
        }
        private async void DisplayDialog(string message)
        {
            ContentDialog noWifiDialog = new ContentDialog()
            {
                Title = "Warning",
                Content = message,
                CloseButtonText = "Ok"
            };

            await noWifiDialog.ShowAsync();
        }

        private async void CameraButton_Clicked(object sender, RoutedEventArgs e)
        {
            CameraCaptureUI captureUI = new CameraCaptureUI();
            captureUI.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Jpeg;
            captureUI.PhotoSettings.CroppedSizeInPixels = new Size(200, 200);

            StorageFile photo = await captureUI.CaptureFileAsync(CameraCaptureUIMode.Photo);

            if (photo == null)
            {
                // User cancelled photo capture
                return;
            }
            else
            {
                StorageFolder destinationFolder =
    await ApplicationData.Current.LocalFolder.CreateFolderAsync("ProfilePhotoFolder",
        CreationCollisionOption.OpenIfExists);

                StorageFile newPhoto =await photo.CopyAsync(destinationFolder, "CameraPhoto" + number +".jpg", NameCollisionOption.GenerateUniqueName);
                await photo.DeleteAsync();
                AssignmentData assignmentData = new AssignmentData(newPhoto);
                await UpdateSelectedStackPanel(assignmentData);
                pickedFiles.Add(assignmentData);


            }
        }

        private async void Camera2Button_Click(object sender, RoutedEventArgs e)
        {

            bool sucess = await StartPreviewAsync();

            if (sucess)

            {
                FotografisiButton.IsEnabled = true;
                var myPictures = await Windows.Storage.StorageLibrary.GetLibraryAsync(Windows.Storage.KnownLibraryId.Pictures);
                StorageFile file = await myPictures.SaveFolder.CreateFileAsync("photo.jpg", CreationCollisionOption.GenerateUniqueName);

                using (var captureStream = new InMemoryRandomAccessStream())
                {
                    await mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), captureStream);

                    using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        var decoder = await BitmapDecoder.CreateAsync(captureStream);
                        var encoder = await BitmapEncoder.CreateForTranscodingAsync(fileStream, decoder);

                        var properties = new BitmapPropertySet {
            { "System.Photo.Orientation", new BitmapTypedValue(PhotoOrientation.Normal, PropertyType.UInt16) }
        };
                        await encoder.BitmapProperties.SetPropertiesAsync(properties);

                        await encoder.FlushAsync();
                    }

                    pickedFiles.Add(new AssignmentData(file));
                }
                await CleanupCameraAsync();
            }

        }
        private async Task<bool> StartPreviewAsync()
        {
            try
            {

                mediaCapture = new MediaCapture();
                await mediaCapture.InitializeAsync();

                displayRequest.RequestActive();
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
            }
            catch (UnauthorizedAccessException)
            {
                // This will be thrown if the user denied access to the camera in privacy settings
                DisplayDialog("The app was denied access to the camera");
                return false;
            }

            try
            {
                PreviewControl.Source = mediaCapture;
                await mediaCapture.StartPreviewAsync();
                isPreviewing = true;
                return true;
            }
            catch (System.IO.FileLoadException)
            {
                mediaCapture.CaptureDeviceExclusiveControlStatusChanged += _mediaCapture_CaptureDeviceExclusiveControlStatusChanged;
                return false;
            }

        }
        private async void _mediaCapture_CaptureDeviceExclusiveControlStatusChanged(MediaCapture sender, MediaCaptureDeviceExclusiveControlStatusChangedEventArgs args)
        {
            if (args.Status == MediaCaptureDeviceExclusiveControlStatus.SharedReadOnlyAvailable)
            {
                DisplayDialog("The camera preview can't be displayed because another app has exclusive access");
            }
            else if (args.Status == MediaCaptureDeviceExclusiveControlStatus.ExclusiveControlAvailable && !isPreviewing)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    await StartPreviewAsync();
                });
               
            }
        }
        private async Task CleanupCameraAsync()
        {
            if (mediaCapture != null)
            {
                if (isPreviewing)
                {
                    await mediaCapture.StopPreviewAsync();
                }

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    PreviewControl.Source = null;
                    if (displayRequest != null)
                    {
                        displayRequest.RequestRelease();
                    }

                    mediaCapture.Dispose();
                    mediaCapture = null;
                });
            }

        }
        private async void Application_Suspending(object sender, SuspendingEventArgs e)
        {
            // Handle global application events only if this page is active
            if (Frame.CurrentSourcePageType == typeof(MainPage))
            {
                var deferral = e.SuspendingOperation.GetDeferral();
                await CleanupCameraAsync();
                deferral.Complete();
            }
        }
        protected async override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (isPreviewing)
            {
                FotografisiButton.IsEnabled = false;
                await CleanupCameraAsync();
            }
        }

        private async void FotografisiButton_Click(object sender, RoutedEventArgs e)
        {
            var myPictures = await Windows.Storage.StorageLibrary.GetLibraryAsync(Windows.Storage.KnownLibraryId.Pictures);
            StorageFile file = await myPictures.SaveFolder.CreateFileAsync("photo.jpg", CreationCollisionOption.GenerateUniqueName);
            
            using (var captureStream = new InMemoryRandomAccessStream())
            {
                await mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), captureStream);

                using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                {
                    var decoder = await BitmapDecoder.CreateAsync(captureStream);
                    var encoder = await BitmapEncoder.CreateForTranscodingAsync(fileStream, decoder);

                    var properties = new BitmapPropertySet {
            { "System.Photo.Orientation", new BitmapTypedValue(PhotoOrientation.Normal, PropertyType.UInt16) }
        };
                    await encoder.BitmapProperties.SetPropertiesAsync(properties);

                    await encoder.FlushAsync();
                }
                AssignmentData assignmentData = new AssignmentData(file);
                //await UpdateSelectedStackPanel(file);
                await UpdateSelectedStackPanel(assignmentData);
                pickedFiles.Add(assignmentData);
                
            }
            IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read);
            BitmapDecoder decoder1 = await BitmapDecoder.CreateAsync(stream);
            SoftwareBitmap softwareBitmap = await decoder1.GetSoftwareBitmapAsync();

            SoftwareBitmap softwareBitmapBGR8 = SoftwareBitmap.Convert(softwareBitmap,
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied);

            SoftwareBitmapSource bitmapSource = new SoftwareBitmapSource();
            await bitmapSource.SetBitmapAsync(softwareBitmapBGR8);

            ImagePrewiev.Source = bitmapSource;

        }

        private async void KameraToggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch != null)
            {
                if (toggleSwitch.IsOn == true)
                {
                    bool sucess =await StartPreviewAsync();
                    if(sucess)
                    FotografisiButton.IsEnabled = true;
                }
                else
                {
                    if (isPreviewing)
                    {
                        FotografisiButton.IsEnabled = false;
                        await CleanupCameraAsync();
                    }

                }
            }
        }
        private async Task UpdateSelectedStackPanel(List<AssignmentData> assignmentDataList)
        {
            foreach (var assignmentData in assignmentDataList)
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    MyUserControl1 selectedControl = new MyUserControl1(assignmentData);
                    selectedControl.FileRemoved += SelectedControl_FileRemoved;
                    SelectedStackPanel.Children.Add(selectedControl);
                });
        }
        private async Task UpdateSelectedStackPanel(AssignmentData assignmentData)
        {
            
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    MyUserControl1 selectedControl = new MyUserControl1(assignmentData);
                    selectedControl.FileRemoved += SelectedControl_FileRemoved;
                    SelectedStackPanel.Children.Add(selectedControl);
                });
        }

        private async void SelectedControl_FileRemoved(int id, object sender)
        {
            await RemoveSelected(id, sender as MyUserControl1);
        }
    }
}
