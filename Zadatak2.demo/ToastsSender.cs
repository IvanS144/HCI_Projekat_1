using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Notifications;

namespace Zadatak2.demo
{
    public static class ToastsSender
    {

        public static readonly SemaphoreSlim notificationSemaphore = new SemaphoreSlim(1);
        public static async Task<StorageFolder> GetTempFolder() => await ApplicationData.Current.LocalFolder.CreateFolderAsync("temp", CreationCollisionOption.ReplaceExisting);

        public static async Task NotifyUser(Assignment assignment)
        {
            //const int maxLengthFilename = 100;
            //const int maxLength = 256;

            await notificationSemaphore.WaitAsync();
            try
            {
                ToastContentBuilder toastContentBuilder = new ToastContentBuilder();
                toastContentBuilder.AddText("Transformation finished", AdaptiveTextStyle.Title, hintMaxLines: 1);
                toastContentBuilder.AddText(assignment.FileName);



                StorageFolder tempFolder = await GetTempFolder();
                StorageFile tempFile = await tempFolder.CreateFileAsync(assignment.DestinationName, CreationCollisionOption.ReplaceExisting);
                await assignment.SaveResultToFile(tempFile);
                //toastContentBuilder.AddHeroImage(new Uri($"ms-appdata:///local/{tempFolder.Name}/{tempFile.Name}"));


                ToastContent content = toastContentBuilder.GetToastContent();
                ToastNotification notification = new ToastNotification(content.GetXml());
                ToastNotificationManager.CreateToastNotifier().Show(notification);
            }
            catch
            { }
            finally
            {
                notificationSemaphore.Release();
            }
        }
    }
}
