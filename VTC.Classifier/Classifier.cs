using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using VTC.Common;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Alturos.Yolo;
using Alturos.Yolo.Model;
using Darknet;
using GeoAPI.CoordinateSystems;
using NLog;

namespace VTC.Classifier
{
    //Below structs just copied for reference from darknet code
    //internal struct bbox_t
    //{
    //    public int x, y, w, h;    // (x,y) - top-left corner, (w, h) - width & height of bounded box
    //    public float prob;                 // confidence - probability that the object was found correctly
    //    public int obj_id;        // class of object - from range [0, classes-1]
    //    public int track_id;      // tracking id for video (0 - untracked, 1 - inf - tracked object)
    //};

    //internal struct image_t
    //{
    //    public int h;                      // height
    //    public int w;                      // width
    //    public int c;                      // number of chanels (3 - for RGB)
    //    public float* data;                // pointer to the image data
    //};

    [DataContract]
    public class EventConfig
    {
        [DataMember] public Dictionary<RegionTransition, string> Events;

        public EventConfig()
        {
            Events = new Dictionary<RegionTransition, string>();
        }
    }

    public class YoloClassifier
    {
        private const int MaxObjects = 1000;
        [StructLayout(LayoutKind.Sequential)]
        public struct bbox_t
        {
            public UInt32 x, y, w, h;    // (x,y) - top-left corner, (w, h) - width & height of bounded box
            public float prob;           // confidence - probability that the object was found correctly
            public UInt32 obj_id;        // class of object - from range [0, classes-1]
            public UInt32 track_id;      // tracking id for video (0 - untracked, 1 - inf - tracked object)
            public UInt32 frames_counter;
            public float x_3d, y_3d, z_3d;  // 3-D coordinates, if there is used 3D-stereo camera
        };

        private YoloWrapper Detector;

        public bool cpuMode = true;

        private static readonly Logger Logger = LogManager.GetLogger("main.form");

        public YoloClassifier()
        {
            var gpuDetector = new GPUDetector();
            if(gpuDetector.HasGPU && gpuDetector.MB_VRAM > 3000)
            {
                cpuMode = false;
            }

            try
            {
                var defaultYoloCfgFilepath = "yolo.cfg";
                var defaultYoloWeightsFilepath = "yolo.weights";

                var selectedYoloCfgFilepath = defaultYoloCfgFilepath;
                var selectedYoloWeightsFilepath = defaultYoloWeightsFilepath;

                var defaultYoloNamesFilepath = "yolo.names";
                var selectedYoloNamesFilepath = defaultYoloNamesFilepath;

                var YoloCfgDropinFilepaths = Directory.EnumerateFiles("./DetectionNetwork/");
                foreach (var p in YoloCfgDropinFilepaths)
                {
                    var ext = Path.GetExtension(p);
                    if (ext == null) continue;
                    if (ext.Contains("cfg"))
                    {
                        selectedYoloCfgFilepath = p;
                        Logger.Debug("Selected YoloCfg:" + selectedYoloCfgFilepath);
                        Console.WriteLine("Selected YoloCfg:" + selectedYoloCfgFilepath);
                    }

                    if (ext.Contains("weights"))
                    {
                        selectedYoloWeightsFilepath = p;
                        Logger.Debug("Selected YoloWeights:" + selectedYoloWeightsFilepath);
                        Console.WriteLine("Selected YoloWeights:" + selectedYoloWeightsFilepath);
                    }

                    if (ext.Contains("names"))
                    {
                        selectedYoloNamesFilepath = p;
                        Logger.Debug("Selected YoloNames:" + selectedYoloNamesFilepath);
                        Console.WriteLine("Selected YoloNames:" + selectedYoloNamesFilepath);
                    }
                }

                Detector = new YoloWrapper(selectedYoloCfgFilepath, selectedYoloWeightsFilepath, selectedYoloNamesFilepath, 0, cpuMode);
                //var cfg = new Alturos.Yolo.YoloConfiguration(selectedYoloCfgFilepath, selectedYoloWeightsFilepath, selectedYoloNamesFilepath);
                //Detector = new YoloWrapper(cfg);

                //if (cpuMode)
                //{
                //    Logger.Debug("Calling detector constructor (no GPU)...");
                //    DetectorNoGPU = new Darknet.YoloWrapperNoGPU(selectedYoloCfgFilepath, selectedYoloWeightsFilepath, 0);
                //}
                //else
                //{
                //    Logger.Debug("Calling detector constructor (with GPU)...");
                //    Detector = new Darknet.YoloWrapper(selectedYoloCfgFilepath, selectedYoloWeightsFilepath, 0);
                //}

                Logger.Debug("Classifier initialized.");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Fatal, ex.Message);
            }
        }

        ~YoloClassifier()
        {
            Detector?.Dispose();
        }

        public List<Measurement> DetectFrameYolo(Image<Bgr, byte> frame)
        {
            List<Measurement> measurements = new List<Measurement>();
            IEnumerable<YoloItem> detectionArray = new List<YoloItem>(MaxObjects);
            var converter = new ImageConverter();
            var convertedBytes = (byte[]) converter.ConvertTo(frame.ToBitmap(), typeof(byte[]));
            
            if(Detector != null)
            {
                detectionArray = Detector.Detect(convertedBytes);
            }

            for (int i = 0; i < detectionArray.Count(); i++)
            {
                var detection = detectionArray.ElementAt(i);
                if (detection.X == 0 && detection.Y == 0) {break;}

                var m = new Measurement
                {
                    Height = detection.Height,
                    Width = detection.Width,
                    X = detection.X + detection.Width / 2,
                    Y = detection.Y + detection.Height / 2
                };

                if (m.Height > frame.Height | m.Width > frame.Width)
                {
                    Debug.WriteLine("Detection rectangle too large.");
                    continue;
                }

                if ( (m.X > frame.Width) | (m.Y > frame.Height) | (m.X < 0) | (m.Y < 0))
                {
                    Debug.WriteLine("Detection coordinates out-of-bounds.");
                    continue;
                }

                //Select a region smaller than the entire detection rectangle. Divide height and width by 2.
                // 

                int xColor1 = (int) m.X - Convert.ToInt32(detection.Width / 2);
                int yColor1 = (int) m.Y - Convert.ToInt32(detection.Height / 2);

                int xColor2 = (int) m.X + Convert.ToInt32(detection.Width/2);
                int yColor2 = (int) m.Y + Convert.ToInt32(detection.Height/2);

                var color = SampleColorAtRectangle(frame, xColor1, yColor1,
                    xColor2,
                    yColor2);

                m.Blue = color.Blue;
                m.Green = color.Green;
                m.Red = color.Red;
                m.Size = m.Width * m.Height;

                var stringToIntMap = new Dictionary<string,int>();
                stringToIntMap.Add("car",0);
                stringToIntMap.Add("truck", 1);
                stringToIntMap.Add("bus", 2);
                stringToIntMap.Add("person", 3);
                stringToIntMap.Add("bicycle", 4);
                stringToIntMap.Add("motorcycle", 5);

                m.ObjectClass = stringToIntMap[detection.Type];
                measurements.Add(m);
            }
            

            return measurements;
        }

        private Bgr SampleColorAtRectangle(Image<Bgr, byte> frame, int x1, int y1, int x2, int y2)
        {
            int width_original = frame.Width;
            int height_original = frame.Height;
            Bgr avg;
            MCvScalar MVcavg;
            int width = x2 - x1;
            int height = y2 - y1;
            frame.ROI = new Rectangle(x1, y1, width, height);
            frame.AvgSdv(out avg, out MVcavg);
            frame.ROI = new Rectangle(0,0, width_original, height_original); //Reset ROI
            return avg;
        }
    }

    public class YoloIntegerNameMapping
    {
        private String IntegerToObjectClassFilepath = "coco.names";
        public Dictionary<int, ObjectType> IntegerToObjectType = new Dictionary<int, ObjectType>();
        public Dictionary<int, string> IntegerToObjectName = new Dictionary<int, string>();
        private static readonly Logger Logger = LogManager.GetLogger("main.form");

        public static string GetObjectNameFromClassInteger(int classId, Dictionary<int,string> idClassMapping)
        {
            return idClassMapping.ContainsKey(classId) ? idClassMapping[classId] : "Unknown";
        }

        public static ObjectType GetObjectTypeFromClassInteger(int classId, Dictionary<int,ObjectType> idClassMapping)
        {
            return idClassMapping.ContainsKey(classId) ? idClassMapping[classId] : ObjectType.Unknown;
        }

        public ObjectType YoloIntegerToObjectType(int ObjectID)
        {
            if (IntegerToObjectType.ContainsKey(ObjectID))
            {
                return IntegerToObjectType[ObjectID];
            }
            
            return ObjectType.Unknown;
        }

        public YoloIntegerNameMapping()
        {
            //Look for network definition files
            var YoloCfgDropinFilepaths = Directory.EnumerateFiles("./DetectionNetwork/");

            //Find the integer->string mapping defition file
            foreach (var p in YoloCfgDropinFilepaths)
            {
                var ext = Path.GetExtension(p);
                if (ext == null) continue;
                if (ext.Contains("names"))
                {
                    IntegerToObjectClassFilepath = p;
                    Logger.Debug("Selected YoloNames:" + IntegerToObjectClassFilepath);
                    Console.WriteLine("Selected YoloNames:" + IntegerToObjectClassFilepath);
                }
            }

            var lines = File.ReadAllLines(IntegerToObjectClassFilepath);
            for(int i=0; i < lines.Length; i++)
            {
                foreach(var c in DetectionClasses.ClassDetectionWhitelist)
                {
                    if(c.ToString().ToLower() == lines[i])
                    {
                        IntegerToObjectName.Add(i,lines[i]);       
                        IntegerToObjectType.Add(i,c);       
                    }
                }
            }
        }
    }

    class UnmanagedImageMemory
    {
        public IntPtr pNativeDataStruct;
        public IntPtr pNativeDataFrame;

        public UnmanagedImageMemory()
        {
            pNativeDataFrame = IntPtr.Zero;
            pNativeDataStruct = IntPtr.Zero;
        }
    }

}
