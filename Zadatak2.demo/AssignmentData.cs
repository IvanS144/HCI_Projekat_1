using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Zadatak2.demo
{
    public class AssignmentData
    {
        private static int count = 0;
        public AssignmentData(StorageFile sourceFile)
        {
            SourceFile = sourceFile;
            ID = count++;
        }

        public AssignmentData(StorageFile sourceFile, int height, int width, int angle, string effect)
        {
            SourceFile = sourceFile;
            ID = count++;
            Height = height;
            Width = width;
            Angle = angle;
            Effect = effect;
        }

        public int Height { get; set; } = -1;
        public int Width { get; set; } = -1;
        public int Angle { get; set; } = -1;
        public string Effect { get; set; } = "NoEffect";
        public StorageFile SourceFile { get; set; }
        public int ID { get; set; }
    }
}
