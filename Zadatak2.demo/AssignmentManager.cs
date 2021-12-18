using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;

namespace Zadatak2.demo
{
    class AssignmentManager
    {

        public int MaxConcurentAssignments { get; set; } = Environment.ProcessorCount - 1;

        private List<Assignment> assignments;
        public IReadOnlyList<Assignment> Assignments => assignments;
        public List<Assignment> AssignmentsList { get => assignments; set => assignments = value; }
        private AssignmentManager(List<Assignment> a)
        {

            this.assignments = a;
        }

        private AssignmentManager()
        {




            assignments = new List<Assignment>();

        }




public async Task RunAssignments()
        {
            int activeAssignments = assignments.Where(a => a.CurrentState == Assignment.AssignmentState.Processing).Count();

            List<Assignment> pendingAssignments = assignments.Where(a => a.CurrentState == Assignment.AssignmentState.Pending && a.IsInitialized).Take(MaxConcurentAssignments - activeAssignments).ToList();
            foreach (var a in pendingAssignments)
                await a.Start(true);



        } public Assignment [] addFiles (List<StorageFile> files, StorageFolder folder, int numOfCores)
        {
            //var fal = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;
            //fal.AddOrReplace(folder.Path, folder);
            List<Assignment> assignments = new List<Assignment>();
            foreach(var file in files)
            {
                //StorageFile destinationFile =await folder.CreateFileAsync(file.Name, CreationCollisionOption.GenerateUniqueName);

                Assignment a = new Assignment(file, folder, file.Name, numOfCores);
                a.IsInitialized = true;
                a.CurrentState = Assignment.AssignmentState.Pending;

                a.ProgressChanged += AssignmentManager_ProgressChanged;

                this.assignments.Add(a);
                assignments.Add(a);


            }
            return assignments.ToArray();





        }

        private async void AssignmentManager_ProgressChanged(double progress, Assignment.AssignmentState assignmentState)
        {
            if (assignmentState == Assignment.AssignmentState.Cancelled || assignmentState == Assignment.AssignmentState.Error || assignmentState == Assignment.AssignmentState.Done)
                await RunAssignments();
        }

        public void RemoveAssignment(Assignment assignment) => assignments.Remove(assignment); private static async Task<StorageFile> GetSerializationFileForLoad() => await ApplicationData.Current.LocalFolder.CreateFileAsync("assignments.xml", CreationCollisionOption.OpenIfExists);
        private static async Task<StorageFile> GetSerializationFileSave() => await ApplicationData.Current.LocalFolder.CreateFileAsync("assignments.xml", CreationCollisionOption.ReplaceExisting);
        public static async Task<StorageFolder> GetTempFolder() => await ApplicationData.Current.LocalFolder.CreateFolderAsync("temp2", CreationCollisionOption.ReplaceExisting);
        public async Task Save()
        {
            List<Assignment> unfinishedAssignments = assignments.Where(a => a.CurrentState != Assignment.AssignmentState.Done && a.CurrentState != Assignment.AssignmentState.Error && a.CurrentState != Assignment.AssignmentState.Cancelled).ToList();
            StorageFolder tempFolder = await GetTempFolder();
            

            var fal = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;
            foreach(var a in assignments)
            {
                if(!a.Finished && !a.Saving)
                {
                    
                    StorageFile tempFile = await tempFolder.CreateFileAsync(a.SourceFile.Name, CreationCollisionOption.GenerateUniqueName);
                    await a.SaveSourceToFile(tempFile);
                    a.SourceToken=fal.Add(tempFile);
                    a.DestDirToken=fal.Add(a.DestinationFolder);
                }


            }
            XElement xml = new XElement(nameof(AssignmentManager), unfinishedAssignments.Select(x => x.GetParameters()).Select(x => new XElement(nameof(Assignment), new XAttribute("sourceToken", x.sourceToken), new XAttribute("destinationToken", x.destDirToken), new XAttribute("name", x.name), new XAttribute("numOfCores", x.numOfCores))));
            StorageFile file = await GetSerializationFileSave();

            using (Stream stream = await file.OpenStreamForWriteAsync())
                xml.Save(stream);
        }
        public static async Task<AssignmentManager> Load()
        {
            try
            {
                StorageFile file = await GetSerializationFileForLoad();

                XElement xml;
                using (Stream stream = await file.OpenStreamForReadAsync())
                    xml = XElement.Load(stream);
                var b = xml.Elements();
                List<String> strings1 = xml.Elements().Select(x => x.Attribute("sourceToken").Value).ToList();
                List<String> strings2 = xml.Elements().Select(x => x.Attribute("destinationToken").Value).ToList();
                List<String> strings3 = xml.Elements().Select(x => x.Attribute("name").Value).ToList();
                List<String> strings4 = xml.Elements().Select(x => x.Attribute("numOfCores").Value).ToList();
                //List<Assignment> downloads = b.Select(x => new Assignment(x.Attribute("sourcePath").Value, x.Attribute("destinationPath").Value, x.Attribute("name").Value, int.Parse(xml.Attribute("numOfCores").Value))).ToList();

                List<Assignment> detectedAssignments = new List<Assignment>();
                for (int i = 0; i < strings1.Count; ++i)
                {
                    detectedAssignments.Add(new Assignment(strings1.ElementAt(i), strings2.ElementAt(i), strings3.ElementAt(i), int.Parse(strings4.ElementAt(i)), 0));


                }
                var fal = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;
                foreach (Windows.Storage.AccessCache.AccessListEntry entry in fal.Entries)
                {
                    string token = entry.Token;

                    detectedAssignments.Where(x => x.SourceToken.Equals(token)).ToList().ForEach(async x => x.SourceFile = await fal.GetFileAsync(token));
                    detectedAssignments.Where(x => x.DestDirToken.Equals(token)).ToList().ForEach(async x => x.DestinationFolder = await fal.GetFolderAsync(token));




                }

                return new AssignmentManager(detectedAssignments);
            }
            catch
            {
                return new AssignmentManager();
            }
            //return new AssignmentManager();

        }

    }
}
