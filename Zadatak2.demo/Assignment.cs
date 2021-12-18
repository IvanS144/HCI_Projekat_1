using SimpleImageEditing;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Zadatak2.demo
{
    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]


    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }


    public class Assignment
    {
        public int Height { get; set; }
        public int Width { get; set; }
        public int Angle { get; set; }
        public string Effect { get; set; }
        private int numberOfCores;


        public enum AssignmentState { Pending, Processing, Pausing, Paused, Resuming, Cancelling, Cancelled, Error, Done, Saving };

        public delegate void ProgressReportedDelegate(double progress, AssignmentState assignmentState);

        public delegate void SavingDelegate(bool value, string name);

        public event ProgressReportedDelegate ProgressChanged;

        public event SavingDelegate SavingInProgress;

        private Task processingTask;

        private CancellationTokenSource cancellationTokenSource;

        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);
        private SemaphoreSlim semaphore5 = new SemaphoreSlim(1);
        static SemaphoreSlim semaphoreForSaving = new SemaphoreSlim(1); private readonly SemaphoreSlim signalizationSemaphore = new SemaphoreSlim(1);

        private readonly SemaphoreSlim pauseSemaphore = new SemaphoreSlim(1);

        private StorageFile destinationFile;
        private StorageFolder destinationFolder;
        private StorageFile sourceFile;

        private AssignmentState currentState = AssignmentState.Pending;
        private string fileName;
        private bool isInitialised = true;
        public bool Finished => CurrentState == AssignmentState.Done || CurrentState == AssignmentState.Error || CurrentState == AssignmentState.Cancelled;
        public bool IsPending => CurrentState == AssignmentState.Pending;
        public bool Paused => CurrentState == Assignment.AssignmentState.Paused;

        public bool Saving => CurrentState == Assignment.AssignmentState.Saving;

        internal AssignmentState CurrentState { get => currentState; set => currentState = value; }
        public string FileName { get => fileName; set => fileName = value; }
        public bool IsInitialized { get => isInitialised; set => isInitialised = value; }
        //public StorageFile DestinationFile { get => destinationFile; set => destinationFile = value; }
        public StorageFolder DestinationFolder { get => destinationFolder; set => destinationFolder = value; }
        public StorageFile SourceFile { get => sourceFile; set => sourceFile = value; }

        //private SoftwareBitmap bitmapImage;

        public string SourcePath { get; set; }
        //public string DestinationPath { get; set; }
        public string DestinationFolderPath { get; set; }
        public string DestinationName { get; set; }
       public string SourceToken { get; set; }
       public string DestDirToken { get; set; }
        private async void SaveSoftwareBitmapToFile(SoftwareBitmap softwareBitmap, StorageFolder destDir, string name)
        {
            SavingInProgress?.Invoke(true, "a");
            //var fal = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;
            //StorageFolder folder=null;
            //StorageFile destinationFile=null; ;
            //foreach (Windows.Storage.AccessCache.AccessListEntry entry in fal.Entries)
            //{
            //    if (entry.Token == "DESTFOLDER")
            //    {
            //        folder = await fal.GetFolderAsync(entry.Token);
            //        destinationFile = await folder.CreateFileAsync("destination.jpg", CreationCollisionOption.ReplaceExisting);
            //        break;

            //    }

            //}
            StorageFile destinationFile = await destDir.CreateFileAsync(name, CreationCollisionOption.GenerateUniqueName);
            this.DestinationName = destinationFile.Name;
            this.destinationFile = destinationFile;
            
            using (IRandomAccessStream stream = await destinationFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                // Create an encoder with the desired format
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);

                // Set the software bitmap

                encoder.SetSoftwareBitmap(softwareBitmap);

                // Set additional encoding parameters, if needed
                //encoder.BitmapTransform.ScaledWidth = 320;
                //encoder.BitmapTransform.ScaledHeight = 240;
                //encoder.BitmapTransform.Rotation = Windows.Graphics.Imaging.BitmapRotation.Clockwise90Degrees;
                //encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
                encoder.IsThumbnailGenerated = true;

                try
                {
                    await encoder.FlushAsync();
                    SavingInProgress?.Invoke(false, destinationFile.Name);
                    


                }
                catch (Exception err)
                {
                    const int WINCODEC_ERR_UNSUPPORTEDOPERATION = unchecked((int)0x88982F81);
                    switch (err.HResult)
                    {
                        case WINCODEC_ERR_UNSUPPORTEDOPERATION:
                            // If the encoder does not support writing a thumbnail, then try again
                            // but disable thumbnail generation.
                            encoder.IsThumbnailGenerated = false;
                            break;
                        default:
                            throw;
                    }
                }

                if (encoder.IsThumbnailGenerated == false)
                {
                    await encoder.FlushAsync();
                    SavingInProgress?.Invoke(false, destinationFile.Name);
                    


                }
                


            }
        }

        //public void Initialize(StorageFile sourceFile, StorageFile destinationFile)
        //{
        //    CurrentState = AssignmentState.Pending;
        //    this.DestinationFile = destinationFile;
        //    IsInitialized = true;
        //}

        private static unsafe int GetBytes(SoftwareBitmap bitmapImage)
        {
            byte* dataInBytes;
            uint capacity;
            using (BitmapBuffer buffer = bitmapImage.LockBuffer(BitmapBufferAccessMode.Write))
            {

                using (var reference = buffer.CreateReference())
                {

                    ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacity);
                }
            }


            int br = Convert.ToInt32(capacity); return br;

        }

        private static unsafe int GetBytes(BitmapBuffer buffer)
        {
            byte* dataInBytes;
            uint capacity;


            using (var reference = buffer.CreateReference())
            {

                ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacity);
            }



            int br = Convert.ToInt32(capacity); return br;

        }

        private unsafe void ChangeOnePixel(BitmapBuffer buffer, int i, int j)
        {



            using (var reference = buffer.CreateReference())
            {
                byte* dataInBytes;
                uint capacity;
                ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacity);

                // Fill-in the BGRA plane
                BitmapPlaneDescription bufferLayout = buffer.GetPlaneDescription(0);



                dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 0] = (byte)(255 - dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 0]);
                dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 1] = (byte)(255 - dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 1]);
                dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 2] = (byte)(255 - dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 2]);
                dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 3] = (byte)255;

            }


        }

        private unsafe void ChangeOnePixel(BitmapPlane plane, BitmapPlaneDescription bufferLayout, int i, int j)
        {


            plane.dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 0] = (byte)(255 - plane.dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 0]);
            plane.dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 1] = (byte)(255 - plane.dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 1]);
            plane.dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 2] = (byte)(255 - plane.dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 2]);
            plane.dataInBytes[bufferLayout.StartIndex + bufferLayout.Stride * i + 4 * j + 3] = (byte)255;




        }


        //private async Task ProcessImage(CancellationToken cancellationToken)
        //{

        //    CurrentState = AssignmentState.Processing;
        //    SoftwareBitmap bitmap;
        //    using (IRandomAccessStream stream = await SourceFile.OpenAsync(FileAccessMode.Read))
        //    {
        //        // Create the decoder from the stream
        //        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

        //        // Get the SoftwareBitmap representation of the file
        //        bitmap = await decoder.GetSoftwareBitmapAsync();
        //    }

        //    SoftwareBitmap supportedBitmap = SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
        //    int totalBytes = GetBytes(supportedBitmap);
        //    int bytesProcessed = 0;
        //    using (SoftwareBitmapEditor editor = new SoftwareBitmapEditor(supportedBitmap))
        //    {

        //        //try
        //        //{
        //        //    Parallel.For(0, editor.height, async i =>
        //        //     {
        //        //         if (cancellationToken.IsCancellationRequested)
        //        //             cancellationToken.ThrowIfCancellationRequested();
        //        //         if (CurrentState == AssignmentState.Pausing)
        //        //         {
        //        //             CurrentState = AssignmentState.Paused;
        //        //             double prog = bytesProcessed / totalBytes; ProgressChanged?.Invoke(prog, CurrentState);
        //        //             await pauseSemaphore.WaitAsync();
        //        //             pauseSemaphore.Release();
        //        //             CurrentState = AssignmentState.Processing;
        //        //         }

        //        //         {

        //        //             for (uint j = 0; j < editor.width; ++j)
        //        //             {
        //        //                 if (cancellationToken.IsCancellationRequested)
        //        //                     cancellationToken.ThrowIfCancellationRequested();

        //        //                 if (CurrentState == AssignmentState.Pausing)
        //        //                 {
        //        //                     CurrentState = AssignmentState.Paused;
        //        //                     double prog = bytesProcessed / totalBytes; ProgressChanged?.Invoke(prog, CurrentState);
        //        //                     await pauseSemaphore.WaitAsync();
        //        //                     pauseSemaphore.Release();
        //        //                     CurrentState = AssignmentState.Processing;
        //        //                 }
        //        //                 uint br = Convert.ToUInt32(i);
        //        //                 SoftwareBitmapPixel pixel = editor.getPixel(br, j);
        //        //                 editor.setPixel(br, j, (byte)(255 - pixel.r), (byte)(255 - pixel.b), (byte)(255 - pixel.g));
        //        //                 bytesProcessed += 4;
        //        //                 double progress = bytesProcessed / totalBytes; ProgressChanged?.Invoke(progress, CurrentState);





        //        //             }










        //        //         }









        //        //     });
        //        //    SaveSoftwareBitmapToFile(supportedBitmap, DestinationFile);

        //        //    CurrentState = AssignmentState.Done;
        //        //    ProgressChanged?.Invoke(1.0, CurrentState);
        //        //}

        //        //catch (OperationCanceledException)
        //        //{
        //        //    CurrentState = AssignmentState.Cancelled;
        //        //    ProgressChanged?.Invoke(0.0, CurrentState);
        //        //}
        //        //catch
        //        //{
        //        //    CurrentState = AssignmentState.Error;
        //        //    ProgressChanged?.Invoke(0.0, CurrentState);
        //        //}

        //        SaveSoftwareBitmapToFile(supportedBitmap, DestinationFile);

        //        CurrentState = AssignmentState.Done;
        //        ProgressChanged?.Invoke(1.0, CurrentState);



        //    }




        //}
        public Assignment(StorageFile sourceFile, StorageFolder folder, string name, int numOfCores)
        {
            this.SourceFile = sourceFile;
            //this.DestinationFile = file;
            this.DestinationFolder = folder;
            this.fileName = name;
            SourcePath = SourceFile.Path;
            //DestinationPath = DestinationFile.Path;
            DestinationFolderPath = folder.Path;
            numberOfCores = numOfCores;



        }

        //public Assignment(StorageFile file, AssignmentState state, string name)
        //{
        //    this.DestinationFile = file;
        //    this.currentState = state;
        //    this.fileName = name;



        //}


        public Assignment(AssignmentState state, string name)
        {
            currentState = state;
            fileName = name;



        }
        //public Assignment(String sourcePath, String destinationPath, AssignmentState state, String name)
        //{
        //    SourcePath = sourcePath;
        //    DestinationPath = destinationPath;
        //    CurrentState = state;
        //    FileName = name;




        //}

        public async Task Start(bool silent = false)
        {
            await semaphore.WaitAsync();
            try
            {
                if (CurrentState == AssignmentState.Pending || !IsInitialized)
                {
                    cancellationTokenSource = new CancellationTokenSource();
                    processingTask = Task.Factory.StartNew(async () => await ProcessImageParalel(cancellationTokenSource.Token), cancellationTokenSource.Token);
                }
                //else if (!silent)
                //    throw new InvalidOperationException("The task is already started.");
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task Reset()
        {
            await semaphore.WaitAsync();
            try
            {
                if (Finished)
                {
                    CurrentState = AssignmentState.Pending;
                    ProgressChanged?.Invoke(0.0, CurrentState);
                }
                
                //else
                //    throw new InvalidOperationException("Cannot reset an active task.");
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task Cancel(bool silent = false)
        {
            await semaphore.WaitAsync();
            try
            {
                if (CurrentState == AssignmentState.Pending)
                    CurrentState = AssignmentState.Cancelled;
                else if (CurrentState == AssignmentState.Processing || CurrentState == AssignmentState.Pausing || CurrentState == AssignmentState.Paused || CurrentState == AssignmentState.Pending)
                {
                    CurrentState = AssignmentState.Cancelling;
                    cancellationTokenSource.Cancel();
                }
                //else if (!silent)
                //    throw new InvalidOperationException("The task cannot be cancelled.");
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task Pause()
        {
            await semaphore.WaitAsync();
            try
            {
                if (CurrentState == AssignmentState.Processing)
                {
                    CurrentState = AssignmentState.Pausing;
                    await pauseSemaphore.WaitAsync();
                }
                //else
                //    throw new InvalidOperationException("Only a Processing task can be paused.");
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task Resume()
        {
            await semaphore.WaitAsync();
            try
            {
                if (CurrentState == AssignmentState.Paused || CurrentState == AssignmentState.Pausing)
                {
                    CurrentState = AssignmentState.Resuming;
                    pauseSemaphore.Release();
                }
                else
                    throw new InvalidOperationException("Only a Processing task can be paused.");
            }
            finally
            {
                semaphore.Release();
            }
        }

        //public async Task SaveToFile(StorageFile destinationFile) => await this.DestinationFile.CopyAndReplaceAsync(destinationFile);

        //public (StorageFile sourceFile, AssignmentState state, StorageFile destinationFile) GetParameters() => (SourceFile, CurrentState switch
        //{
        //    AssignmentState.Cancelling or AssignmentState.Cancelled => AssignmentState.Cancelled,
        //    AssignmentState.Error or AssignmentState.Done => CurrentState,
        //    _ => AssignmentState.Pending
        //}, DestinationFile);
        //public (String path, AssignmentState state, String name) GetParameters() => (SourceFile.Path, CurrentState switch
        //{
        //    AssignmentState.Cancelling or AssignmentState.Cancelled => AssignmentState.Cancelled,
        //    AssignmentState.Error or AssignmentState.Done => CurrentState,
        //    _ => AssignmentState.Pending
        //}, FileName);
        public (String path, String destinationFolderPath, String name, String numOfCores, string sourceToken, string destDirToken) GetParameters() => (SourceFile.Path, destinationFolder.Path, FileName, numberOfCores.ToString(), SourceToken, DestDirToken);
        //public Assignment(StorageFile sourceFile, AssignmentState state, StorageFile destinationFile)
        //{
        //    this.sourceFile = sourceFile;
        //    this.DestinationFile = destinationFile;
        //    this.currentState = state;
        //    this.fileName = destinationFile.Name;



        //}
        public Assignment(string sourcePath, string destDirPath, string name, int numOfCores)
        {
            this.SourcePath = sourcePath;
            this.DestinationFolderPath = destDirPath;
            this.FileName = name;
            this.numberOfCores = numOfCores;




        }
        private async Task ProcessImage5(CancellationToken cancellationToken)
        {
            

         
             CurrentState = AssignmentState.Processing;
             SoftwareBitmap bitmap;
             using (IRandomAccessStream stream = await SourceFile.OpenAsync(FileAccessMode.Read))
             {

                 BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                 bitmap = await decoder.GetSoftwareBitmapAsync();
             }

             SoftwareBitmap supportedBitmap = SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
             //int bytesProcessed = 0;
             BitmapPlaneDescription bufferLayout;
             using (BitmapBuffer buffer = supportedBitmap.LockBuffer(BitmapBufferAccessMode.Write))
             {

                 BitmapPlane bitmapPlane = new BitmapPlane(buffer);
                 bufferLayout = buffer.GetPlaneDescription(0);
                int totalBytes = Convert.ToInt32(bitmapPlane.capacity);
                int oneFourth = totalBytes / 4;
                 
                     ParallelOptions po = new ParallelOptions();
                     po.CancellationToken = cancellationToken;
                     po.MaxDegreeOfParallelism = numberOfCores;

                     try
                 {
                         Parallel.For(0, bufferLayout.Height,po, async i =>
                           {
                               if (i % oneFourth == 0)
                               {
                                   await signalizationSemaphore.WaitAsync();
                                   try
                                   {

                                       ProgressChanged?.Invoke(0.22, CurrentState);
                                   }
                                   finally
                                   {
                                       signalizationSemaphore.Release();
                                   }


                               }



                               //if (cancellationToken.IsCancellationRequested)
                               //    cancellationToken.ThrowIfCancellationRequested();
                               if (CurrentState == AssignmentState.Pausing)
                               {
                                   CurrentState = AssignmentState.Paused;
                                   //double prog = bytesProcessed / totalBytes; ProgressChanged?.Invoke(prog, CurrentState);
                                   pauseSemaphore.Wait();
                                   pauseSemaphore.Release();
                                   CurrentState = AssignmentState.Processing;
                               }


                               for (int j = 0; j < bufferLayout.Width; ++j)
                               {
                                   //if (cancellationToken.IsCancellationRequested)
                                   //    cancellationToken.ThrowIfCancellationRequested();

                                   if (CurrentState == AssignmentState.Pausing)
                                   {
                                       CurrentState = AssignmentState.Paused;
                                       //double prog = bytesProcessed / totalBytes; ProgressChanged?.Invoke(prog, CurrentState);
                                       await pauseSemaphore.WaitAsync();
                                       pauseSemaphore.Release();
                                       CurrentState = AssignmentState.Processing;
                                   }
                                   uint br = Convert.ToUInt32(i);
                                   unsafe
                                   {
                                       ChangeOnePixel(bitmapPlane, bufferLayout, i, j);
                                   }

                               }



                           });
                         await semaphoreForSaving.WaitAsync();
                         try
                         {  SaveSoftwareBitmapToFile(supportedBitmap, DestinationFolder, sourceFile.Name); }
                         catch
                         {


                         }
                         finally
                         {

                             semaphoreForSaving.Release();
                         }

                         
                         CurrentState = AssignmentState.Done;
                         ProgressChanged?.Invoke(1.0, CurrentState);

                         //Task.Delay(20000).Wait();
                         //await semaphoreForSaving.WaitAsync();
                         //try
                         //{ SaveSoftwareBitmapToFile(supportedBitmap, DestinationFile); }
                         //finally
                         //{

                         //    semaphoreForSaving.Release();
                         //}


                    }

                    catch (OperationCanceledException)
                 {
                     CurrentState = AssignmentState.Cancelled;
                     ProgressChanged?.Invoke(0.0, CurrentState);
                 }
                 catch
                 {
                     CurrentState = AssignmentState.Error;
                     ProgressChanged?.Invoke(0.0, CurrentState);
                 }


                }


            
        }
        public Assignment(string sourceToken, string destDirToken, string name, int numOfCores, int i)
        {
            this.SourceToken = sourceToken;
            this.DestDirToken = destDirToken;
            this.FileName = name;
            this.numberOfCores = numOfCores;




        }

        private async Task ProcessImageParalel(CancellationToken cancellationToken)
        {

            //await Task.Run(async () => {
            double temp = 0.0;
                CurrentState = AssignmentState.Processing;
                SoftwareBitmap bitmap;
                using (IRandomAccessStream stream = await SourceFile.OpenAsync(FileAccessMode.Read))
                {

                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                    bitmap = await decoder.GetSoftwareBitmapAsync();
                }

                SoftwareBitmap supportedBitmap = SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                int bytesProcessed = 0;
                BitmapPlaneDescription bufferLayout;
                using (BitmapBuffer buffer = supportedBitmap.LockBuffer(BitmapBufferAccessMode.Write))
                {

                    BitmapPlane bitmapPlane = new BitmapPlane(buffer);
                    bufferLayout = buffer.GetPlaneDescription(0);
                     int totalBytes = Convert.ToInt32(bitmapPlane.capacity);
                int oneEight = totalBytes / 8;

                    ParallelOptions poOuter = new ParallelOptions();
                    poOuter.CancellationToken = cancellationToken;
                    poOuter.MaxDegreeOfParallelism = 1;

                    ParallelOptions poInner = new ParallelOptions();
                    poInner.MaxDegreeOfParallelism = numberOfCores;

                    
                try
                    {
                        Parallel.For(0, bufferLayout.Height, poOuter, i =>
                        {
    
                            

                            if (CurrentState == AssignmentState.Pausing)
                            {
                                CurrentState = AssignmentState.Paused;
                                ProgressChanged?.Invoke(temp, CurrentState);
                                pauseSemaphore.Wait();
                                pauseSemaphore.Release();
                                CurrentState = AssignmentState.Processing;
                            }

                            if (i % 100 == 0)
                                Task.Delay(50).Wait();


                            Parallel.For(0, bufferLayout.Width, poInner, j =>
                              {
                                  {


                                      
                                      unsafe
                                      {
                                          ChangeOnePixel(bitmapPlane, bufferLayout, i, j);
                                      }

                                  }
                              });
                            bytesProcessed += 4 * bufferLayout.Width;
                            if(i%oneEight ==0)
                            {
                                temp += 0.125;
                                ProgressChanged?.Invoke(temp, CurrentState);

                            }
                            



                        });
                        CurrentState = Assignment.AssignmentState.Saving;
                        //SavingInProgress?.Invoke(true, "a");
                        await semaphoreForSaving.WaitAsync();
                        try
                        { SaveSoftwareBitmapToFile(supportedBitmap, DestinationFolder, sourceFile.Name); }
                        catch
                        {


                        }
                        finally
                        {

                            semaphoreForSaving.Release();
                        //SavingInProgress?.Invoke(false);
                        }


                        CurrentState = AssignmentState.Done;
                        ProgressChanged?.Invoke(1.0, CurrentState);


                    }

                    catch (OperationCanceledException)
                    {
                        CurrentState = AssignmentState.Cancelled;
                        ProgressChanged?.Invoke(0.0, CurrentState);
                    }
                   catch
                    {
                        CurrentState = AssignmentState.Error;
                        ProgressChanged?.Invoke(0.0, CurrentState);
                    }


                }

            //});

        }


        private async Task ProcessImageParalel5(CancellationToken cancellationToken)
        {
           

            //await Task.Run(async () => {

            CurrentState = AssignmentState.Processing;
            SoftwareBitmap bitmap;
            using (IRandomAccessStream stream = await SourceFile.OpenAsync(FileAccessMode.Read))
            {

                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                bitmap = await decoder.GetSoftwareBitmapAsync();
            }

            SoftwareBitmap supportedBitmap = SoftwareBitmap.Convert(bitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            int bytesProcessed = 0;
            BitmapPlaneDescription bufferLayout;
            using (BitmapBuffer buffer = supportedBitmap.LockBuffer(BitmapBufferAccessMode.Write))
            {

                BitmapPlane bitmapPlane = new BitmapPlane(buffer);
                bufferLayout = buffer.GetPlaneDescription(0);
                int totalBytes = Convert.ToInt32(bitmapPlane.capacity);

                ParallelOptions poOuter = new ParallelOptions();
                poOuter.CancellationToken = cancellationToken;
                poOuter.MaxDegreeOfParallelism = 1;

                ParallelOptions poInner = new ParallelOptions();
                poInner.MaxDegreeOfParallelism = numberOfCores;


                try
                {
                    for(int i=0; i<bufferLayout.Height; ++i)
                    {

                        if (cancellationToken.IsCancellationRequested)
                            cancellationToken.ThrowIfCancellationRequested();

                        if (CurrentState == AssignmentState.Pausing)
                        {
                            CurrentState = AssignmentState.Paused;
                            ProgressChanged?.Invoke((bytesProcessed / totalBytes) * 100, CurrentState);
                            pauseSemaphore.Wait();
                            pauseSemaphore.Release();
                            CurrentState = AssignmentState.Processing;
                        }


                        Parallel.For(0, bufferLayout.Width, poInner, j =>
                        {
                            {



                                unsafe
                                {
                                    ChangeOnePixel(bitmapPlane, bufferLayout, i, j);
                                }

                            }
                        });
                        bytesProcessed += 4 * bufferLayout.Width;
                       
                            Task.Delay(50).Wait();

                            ProgressChanged?.Invoke((bytesProcessed / totalBytes) * 100, CurrentState);
                        



                    }
                    await semaphoreForSaving.WaitAsync();
                    try
                    { SaveSoftwareBitmapToFile(supportedBitmap, DestinationFolder, sourceFile.Name); 
                    }
                    catch
                    {
                        


                    }
                    finally
                    {

                        semaphoreForSaving.Release();
                    }


                    CurrentState = AssignmentState.Done;
                    ProgressChanged?.Invoke(100, CurrentState);


                }

                catch (OperationCanceledException)
                {
                    CurrentState = AssignmentState.Cancelled;
                    ProgressChanged?.Invoke(100, CurrentState);
                }
                catch
                {
                    CurrentState = AssignmentState.Error;
                    ProgressChanged?.Invoke(0.0, CurrentState);
                }


            }

            //});

        }
        public async Task SaveResultToFile(StorageFile destinationFile) => await this.destinationFile.CopyAndReplaceAsync(destinationFile);
        public async Task SaveSourceToFile(StorageFile destinationFile) => await this.SourceFile.CopyAndReplaceAsync(destinationFile);


    }



public class BitmapPlane
    {
        public unsafe byte* dataInBytes;
        public uint capacity;

        public unsafe BitmapPlane(BitmapBuffer buffer)
        {
            using (var reference = buffer.CreateReference())
            {
                
                ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacity);
            }





            }


    }
}
