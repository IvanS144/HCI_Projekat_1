using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Zadatak2.demo
{
    class AssignmentData
    {
        public AssignmentData(StorageFile sourceFile, int height, int width, int angle, string effect)
        {
            SourceFile = sourceFile;
            Height = height;
            Width = width;
            Angle = angle;
            Effect = effect;
        }

        public int Height { get; set; }
        public int Width { get; set; }
        public int Angle { get; set; }
        public string Effect { get; set; }
        public StorageFile SourceFile { get; set; }
    }
}
