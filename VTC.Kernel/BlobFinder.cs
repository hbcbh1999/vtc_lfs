using System.Collections.Generic;
using System.Linq;
using Emgu.CV;
using Emgu.CV.Cvb;
using Emgu.CV.Structure;

namespace VTC.Kernel
{
    public class BlobFinder
    {
        
        public SortedList<CvBlob, int> FindBlobs(Image<Gray, byte> movementMask, int minSize)
        {
            var resultingImgBlobs = new CvBlobs();
            var bDetect = new CvBlobDetector();
            bDetect.Detect(movementMask, resultingImgBlobs);

            var areaComparer = new BlobAreaComparer();
            var blobsWithArea = new SortedList<CvBlob, int>(areaComparer);
            foreach (var targetBlob in resultingImgBlobs.Values.Where(targetBlob => targetBlob.Area > minSize))
                blobsWithArea.Add(targetBlob, targetBlob.Area);

            return blobsWithArea;
        }

        const int WidthSplitThreshold = 50;
        const int HeightSplitThreshold = 50; 
        public static int Subitize(Image<Bgr,byte> maskedBlob, Image<Gray,byte> blob, CvBlob cvblob )
        {
            if (cvblob.BoundingBox.Width > WidthSplitThreshold && cvblob.BoundingBox.Height > HeightSplitThreshold)
                return 2;

            return 1;
        }
    }
}
