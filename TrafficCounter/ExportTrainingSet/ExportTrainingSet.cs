using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using System.IO;
using Emgu.CV.Cvb;
using VTC.Common;
using VTC.Common.RegionConfig;

namespace VTC.ExportTrainingSet
{
    public class ExportTrainingSet
    {
        private readonly RegionConfig _regionConfig;
        private readonly Image<Bgr, float> _frame;
        private readonly Image<Gray, byte> _movementMask;
        private readonly PictureBox _picture = new PictureBox();

        public ExportTrainingSet(RegionConfig regionConfig, Image<Bgr, float> frame,
            Image<Gray, byte> movementMask)
        {
            _regionConfig = regionConfig;
            _frame = new Image<Bgr, float>(frame.Width, frame.Height);
            //_movementMask = new Image<Gray, byte>(movementMask.Width, movementMask.Height);
            CopySubimage(frame, _frame);
            //CopySubimage(movementMask, _movementMask);

            _picture.Width = _frame.Width;
            _picture.Height = _frame.Height;
            _picture.Image = _frame.ToBitmap();
        }
         
        private void CopySubimage(Image<Bgr, float> source, Image<Bgr, float> destination)
        {
            var xOffset = (destination.Width - source.Width)/2 - 1;
            var yOffset = (destination.Height - source.Height)/2 - 1;
            for (var i = 0; i < source.Width; i++)
                for (var j = 0; j < source.Height; j++)
                {
                    if (j + yOffset < 0 || j + yOffset > destination.Width)
                        continue;

                    if (i + xOffset < 0 || i + xOffset > source.Width)
                        continue;

                    if (j < 0 || j > source.Height)
                        continue;

                    if (i < 0 || i > source.Width)
                        continue;

                    destination.Data[j + yOffset, i + xOffset, 0] = source.Data[j, i, 0];
                    destination.Data[j + yOffset, i + xOffset, 1] = source.Data[j, i, 1];
                    destination.Data[j + yOffset, i + xOffset, 2] = source.Data[j, i, 2];
                }
        }

        private void CopySubimage(Image<Gray, byte> source, Image<Gray, byte> destination)
        {
            var xOffset = (destination.Width - source.Width)/2 - 1;
            var yOffset = (destination.Height - source.Height)/2 - 1;
            for (var i = 0; i < source.Width; i++)
                for (var j = 0; j < source.Height; j++)
                {

                    if (j + yOffset < 0 || j + yOffset > destination.Width)
                        continue;

                    if (i + xOffset < 0 || i + xOffset > source.Width)
                        continue;

                    if (j < 0 || j > source.Height)
                        continue;

                    if (i < 0 || i > source.Width)
                        continue;

                    destination.Data[j + yOffset, i + xOffset, 0] = source.Data[j, i, 0];
                }
        }

        private Image<Bgr, float> ExtractSubImage(Measurement m)
        {
            Rectangle bb = new Rectangle((int) (m.X - m.Width/2), (int) (m.Y - m.Height/2), (int) m.Width, (int) m.Height);
            var subimage = _frame.GetSubRect(bb);
            return subimage;
        }

        private Image<Gray, byte> ExtractSubMask(Measurement m)
        {
            Rectangle bb = new Rectangle((int)(m.X - m.Width / 2), (int)(m.Y - m.Height / 2), (int)m.Width, (int)m.Height);
            var subimageUnscaled = _movementMask.GetSubRect(bb);
            return subimageUnscaled;
        }

        private string SaveExampleImage(Image<Bgr, float> image, string classString)
        {
            string examplePath = ConstructExamplePath(classString);
            image.Save(examplePath);
            return examplePath;
        }

        private void SaveBoundingBoxInfo(Measurement m, string filename, int frame)
        {
            string path = BoundingBoxInfoFilePath();

            if (!File.Exists(path))
            {
                using (StreamWriter sw = File.CreateText(path))
                {
                    string measurementLog = "Filename:\"" + filename + "\" X:" + Math.Round(m.X,1) + " Y:" + Math.Round(m.Y,1) + " Width:" + m.Width + " Height:" + m.Height + " Frame:" + frame;
                    sw.WriteLine(measurementLog);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(path))
                {
                    string measurementLog = "Filename:\"" + filename + "\" X:" + Math.Round(m.X, 1) + " Y:" + Math.Round(m.Y, 1) + " Width:" + m.Width + " Height:" + m.Height + " Frame:" + frame;
                    sw.WriteLine(measurementLog);
                }
            }
            
        }

        private static string BoundingBoxInfoFilePath()
        {
            string examplesDirectory = CreateExamplesDirectoryIfNotExists();
            string classDirectory = CreateClassDirectoryIfNotExists("MovingObjectSubimages", examplesDirectory);
            
            var examplePath = classDirectory + "\\..\\" + "BoundingBoxInfo.txt";
            return examplePath;
        }


        private static string ConstructExamplePath(string classString)
        {
            string examplesDirectory = CreateExamplesDirectoryIfNotExists();
            string classDirectory = CreateClassDirectoryIfNotExists(classString, examplesDirectory);
            var filenamesToNumbers =
                Directory.GetFiles(classDirectory)
                    .ToList()
                    .Select(s => Convert.ToInt64(Path.GetFileNameWithoutExtension(s)))
                    .ToList();
            filenamesToNumbers.Add(0);
            filenamesToNumbers.Sort();
            var newExampleNum = filenamesToNumbers.Last() + 1;
            var examplePath = classDirectory + "\\" + newExampleNum + ".bmp";
            return examplePath;
        }

        private static string CreateClassDirectoryIfNotExists(string classString, string examplesDirectory)
        {
            string classDirectory = examplesDirectory + "\\" + classString;
            var classDirectoryExists = Directory.Exists(classDirectory);
            if (!classDirectoryExists)
                Directory.CreateDirectory(classDirectory);

            return classDirectory;
        }

        private static string CreateExamplesDirectoryIfNotExists()
        {
            string examplesDirectory = Directory.GetCurrentDirectory() + "\\examples";
            var examplesDirectoryExists = Directory.Exists(examplesDirectory);
            if (!examplesDirectoryExists)
                Directory.CreateDirectory(examplesDirectory);
            return examplesDirectory;
        }

        public void AutoExportDualImages(Measurement[] measurements, int framenumber)
        {
            foreach (var m in measurements)
            {
                bool bad = false;

                if (m.X - m.Width/2 <= 0 ||m.X + m.Width/2 >= _frame.Width)
                    bad = true;

                if (m.Y - m.Height/2 <= 0 || m.Y +  m.Height/2 >= _frame.Height)
                    bad = true;

                if (m.X <= 0 || m.X >= _frame.Width)
                    bad = true;

                if (m.Y <= 0 || m.Y >= _frame.Height)
                    bad = true;

                if (!bad)
                {
                    var subimage2 = ExtractSubImage(m);
                    var subimage = ExtractSubMask(m).Convert<Bgr, float>();
                    var joined = new Image<Bgr, float>(
                        subimage.Width + subimage2.Width, subimage.Height)
                    {
                        ROI = new Rectangle(0, 0, subimage.Width, subimage.Height)
                    };
                    subimage.CopyTo(joined);
                    joined.ROI = new Rectangle(subimage.Width, 0, subimage2.Width, subimage.Height);
                    subimage2.CopyTo(joined);
                    joined.ROI = new Rectangle(0, 0, subimage.Width + subimage2.Width, joined.Height);

                    var imagePath = SaveExampleImage(joined, "MovingObjectSubimages"); //TODO: Make path a configuration item    
                    SaveBoundingBoxInfo(m, imagePath, framenumber);
                }
                else
                    Debug.WriteLine("Skipped export on bad bounding box.");
            }
        }
    }
}
    

