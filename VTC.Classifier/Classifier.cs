using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Management;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using VTC.Common;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
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

    public static class TrajectoryClassifier
    {
        public static KeyValuePair<RegionTransition, string>? ClassifyRegionTransition(KeyValuePair<string, Common.RegionConfig.Polygon> start, KeyValuePair<string, Common.RegionConfig.Polygon> end, EventConfig eventConfiguration)
        {
            var transitionEvent = eventConfiguration.Events.FirstOrDefault(kvp => kvp.Key.InRegion == start.Key && kvp.Key.OutRegion == end.Key);
            return transitionEvent;
        }
    }

    public class YoloClassifier
    {
        private IntPtr Detector;

        public float Threshold = 0.4f;

        [DllImport("yolo_cpp_dll_no_gpu.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr Detector_no_gpu_new(String cfg_filename, String weight_filename, int gpu_id);

        [DllImport("yolo_cpp_dll_no_gpu.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr Detector_no_gpu_Destroy(IntPtr detector);

        [DllImport("yolo_cpp_dll_no_gpu.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int Detector_no_gpu_get_net_width(IntPtr detector);

        [DllImport("yolo_cpp_dll_no_gpu.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int Detector_no_gpu_get_net_height(IntPtr detector);

        [DllImport("yolo_cpp_dll_no_gpu.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Detector_no_gpu_detect_from_image_t(IntPtr detector, IntPtr img, float thresh, bool use_mean, int[] box_list, int max_detection_count);

        [DllImport("yolo_cpp_dll_no_gpu.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Detector_no_gpu_detect_from_filename(IntPtr detector, String image_filename, float thresh, bool use_mean, int[] box_list, int max_detection_count);

        [DllImport("yolo_cpp_dll_no_gpu.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Detector_no_gpu_load_image(IntPtr detector, String image_filename, IntPtr img);

        [DllImport("yolo_cpp_dll_no_gpu.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Detector_no_gpu_free_image(IntPtr img);

        [DllImport("yolo_cpp_dll.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr Detector_new(String cfg_filename, String weight_filename, int gpu_id);

        [DllImport("yolo_cpp_dll.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr Detector_Destroy(IntPtr detector);

        [DllImport("yolo_cpp_dll.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int Detector_get_net_width(IntPtr detector);

        [DllImport("yolo_cpp_dll.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int Detector_get_net_height(IntPtr detector);

        [DllImport("yolo_cpp_dll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Detector_detect_from_image_t(IntPtr detector, IntPtr img, float thresh, bool use_mean, int[] box_list, int max_detection_count);

        [DllImport("yolo_cpp_dll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Detector_detect_from_filename(IntPtr detector, String image_filename, float thresh, bool use_mean, int[] box_list, int max_detection_count);

        [DllImport("yolo_cpp_dll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Detector_load_image(IntPtr detector, String image_filename, IntPtr img);

        [DllImport("yolo_cpp_dll.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Detector_free_image(IntPtr img);

        public bool cpuMode = true;

        private static readonly Logger Logger = LogManager.GetLogger("main.form");

        public YoloClassifier()
        {
            ManagementObjectSearcher searcher
                = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");

            string graphicsCard = string.Empty;
            bool nvidiaPresent = false;
            foreach (ManagementObject mo in searcher.Get())
            {
                foreach (PropertyData property in mo.Properties)
                {
                    if (property.Name == "Description")
                    {
                        graphicsCard = property.Value.ToString();
                        if (graphicsCard.ToLower().Contains("nvidia"))
                        {
                            nvidiaPresent = true;
                        }
                    }
                }
            }

            try
            {
                if (nvidiaPresent)
                {
                    NvAPIWrapper.NVIDIA.Initialize();
                    var gpu = NvAPIWrapper.GPU.PhysicalGPU.GetPhysicalGPUs()[0];
                    var gpu0_kb = gpu.PhysicalFrameBufferSize;
                    var gpu0_gb = gpu0_kb / 1000000.0;
                    if (gpu0_gb > 3.0)
                    {
                        cpuMode = false;
                    }
                }
            }
            catch (NvAPIWrapper.Native.Exceptions.NVIDIAApiException ex)
            {
                Logger.Log(LogLevel.Error, ex.Message);
                cpuMode = true;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, ex.Message);
                cpuMode = true;
            }

            try
            {
                var defaultYoloCfgFilepath = "yolo.cfg";
                var defaultYoloWeightsFilepath = "yolo.weights";

                var selectedYoloCfgFilepath = defaultYoloCfgFilepath;
                var selectedYoloWeightsFilepath = defaultYoloWeightsFilepath;

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
                }

                if (cpuMode)
                {
                    Logger.Debug("Calling detector constructor (no GPU)...");
                    Detector = Detector_no_gpu_new(selectedYoloCfgFilepath, selectedYoloWeightsFilepath, 0);
                }
                else
                {
                    Logger.Debug("Calling detector constructor (with GPU)...");
                    Detector = Detector_new(selectedYoloCfgFilepath, selectedYoloWeightsFilepath, 0);
                }

                Logger.Debug("Classifier initialized.");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Fatal, ex.Message);
            }
        }

        ~YoloClassifier()
        {
            if (cpuMode)
            {
                Detector_no_gpu_Destroy(Detector);
            }
            else
            {
                Detector_Destroy(Detector);
            }
        }

        private const int MaxDetectionCount = 20;
        public List<Measurement> DetectFrameYolo(Image<Bgr, byte> frame)
        {
            List<Measurement> measurements = new List<Measurement>();
            int sizeOfbbox_t = 6 * 4 + 8;
            int [] bbox_t_array = new int[sizeOfbbox_t * MaxDetectionCount];
            var frame_float = frame.Convert<Bgr, float>().Mul(1.0/255.0);
            var memory = MarshalEmguImageToimage_t(frame_float);

            if (Detector != IntPtr.Zero)
            {
                if (cpuMode)
                {
                    Detector_no_gpu_detect_from_image_t(Detector, memory.pNativeDataStruct, Threshold, false,
                        bbox_t_array, MaxDetectionCount);
                }
                else
                {
                    Detector_detect_from_image_t(Detector, memory.pNativeDataStruct, Threshold, false, bbox_t_array, MaxDetectionCount);
                }

                for (int i = 0; i < MaxDetectionCount; i++)
                {
                    int h_offset = i * sizeOfbbox_t;
                    int w_offset = i * sizeOfbbox_t + sizeof(int);
                    int x_offset = i * sizeOfbbox_t + sizeof(int) + sizeof(int);
                    int y_offset = i * sizeOfbbox_t + sizeof(int) + sizeof(int) + sizeof(int);
                    int obj_id_offset = y_offset + sizeof(int);

                    if (bbox_t_array[x_offset] == 0 && bbox_t_array[y_offset] == 0) {break;}

                    var m = new Measurement();
                    m.Height = bbox_t_array[h_offset];
                    m.Width = bbox_t_array[w_offset];
                    if (m.Height > frame.Height | m.Width > frame.Width)
                    {
                        Debug.WriteLine("Detection rectangle too large.");
                        continue;
                    }
                    m.X = bbox_t_array[x_offset] + bbox_t_array[w_offset]/2;
                    m.Y = bbox_t_array[y_offset] + bbox_t_array[h_offset] / 2;
                    if ( (m.X > frame.Width) | (m.Y > frame.Height) | (m.X < 0) | (m.Y < 0))
                    {
                        Debug.WriteLine("Detection coordinates out-of-bounds.");
                        continue;
                    }

                    var color = SampleColorAtRectangle(frame, bbox_t_array[x_offset], bbox_t_array[y_offset],
                        bbox_t_array[x_offset] + bbox_t_array[w_offset],
                        bbox_t_array[y_offset] + bbox_t_array[h_offset]);

                    m.Blue = color.Blue;
                    m.Green = color.Green;
                    m.Red = color.Red;
                    m.Size = m.Width * m.Height;
                    m.ObjectClass = bbox_t_array[obj_id_offset];
                    measurements.Add(m);
                }
            }

            GC.KeepAlive(frame_float);
            GC.KeepAlive(memory);

            Marshal.FreeHGlobal(memory.pNativeDataFrame);
            Marshal.FreeHGlobal(memory.pNativeDataStruct);

            return measurements;
        }

        private Bgr SampleColorAtRectangle(Image<Bgr, byte> frame, int x1, int y1, int x2, int y2)
        {
            int width = frame.Width;
            int height = frame.Height;
            Bgr avg;
            MCvScalar MVcavg;
            frame.ROI = new Rectangle(x1, y1, x2, y2);
            frame.AvgSdv(out avg, out MVcavg);
            frame.ROI = new Rectangle(0,0,width,height); //Reset ROI
            return avg;
        }

        unsafe static byte[] GetBytes(float value) {
            var bytes = new byte[4];
            fixed (byte* b = bytes)
                *((int*)b) = *(int*)&value;

            return bytes;
        }

        private UnmanagedImageMemory MarshalEmguImageToimage_t(Image<Bgr, float> frame)
        {
            if(frame == null)
                return new UnmanagedImageMemory();

            unsafe
            {
                //Calculate sizes for allocation
                int struct_size = sizeof(float*) + 3 * sizeof(int) + 2;
                int data_size = frame.Bytes.Length;

                //Allocate
                IntPtr pNativeDataStruct = Marshal.AllocHGlobal(struct_size);
                IntPtr pNativeDataFrame = Marshal.AllocHGlobal(data_size);

                //Copy data
                Marshal.WriteInt32(pNativeDataStruct, frame.Height);
                Marshal.WriteInt32(pNativeDataStruct + 1 * sizeof(int), frame.Width);
                Marshal.WriteInt32(pNativeDataStruct + 2 * sizeof(int), frame.NumberOfChannels);
                Int64 fullPtr = pNativeDataFrame.ToInt64();
                Marshal.WriteInt64(pNativeDataStruct + 4 * sizeof(int), fullPtr);

                int frameWidth = frame.Width;
                int frameHeight = frame.Height;
                //int nChannels = frame.NumberOfChannels;
                //int[] destinationChannelIndex = { 2, 1, 0 }; //Swap R and B channels: OpenCV is Bgr, Yolo expects Rgb.

                Parallel.For(0, frameWidth, i =>
                    {
                        //Parallel.For(0, frameHeight, j =>
                        for(int j=0;j<frameHeight;j++)
                        {
                            int dst_index =
                                (i + frameWidth * j + frameWidth * frameHeight * 2) *
                                sizeof(float);
                            byte[] bytes = BitConverter.GetBytes(frame.Data[j, i, 0]);
                            IntPtr newptr = new IntPtr(pNativeDataFrame.ToInt64() + dst_index);
                            Marshal.Copy(bytes, 0, newptr, 4);

                            dst_index =
                                (i + frameWidth * j + frameWidth * frameHeight) *
                                sizeof(float);
                            bytes = BitConverter.GetBytes(frame.Data[j, i, 1]);
                            newptr = new IntPtr(pNativeDataFrame.ToInt64() + dst_index);
                            Marshal.Copy(bytes, 0, newptr, 4);

                            dst_index =
                                (i + frameWidth * j) *
                                sizeof(float);
                            bytes = BitConverter.GetBytes(frame.Data[j, i, 2]);
                            newptr = new IntPtr(pNativeDataFrame.ToInt64() + dst_index);
                            Marshal.Copy(bytes, 0, newptr, 4);
                        }
                    }
                );

                var memory = new UnmanagedImageMemory();
                memory.pNativeDataFrame = pNativeDataFrame;
                memory.pNativeDataStruct = pNativeDataStruct;
                return memory;
            }
        }
    }

    public class YoloIntegerNameMapping
    {
        private String IntegerToObjectClassFilepath = "coco.names";
        public Dictionary<int, string> IntegerToObjectClass = new Dictionary<int, string>();
        private static readonly Logger Logger = LogManager.GetLogger("main.form");

        public static string GetObjectNameFromClassInteger(int classId, Dictionary<int,string> idClassMapping)
        {
            return idClassMapping.ContainsKey(classId) ? idClassMapping[classId] : "Unknown";
        }

        public string ObjectClass(int ObjectID)
        {
            if (IntegerToObjectClass.ContainsKey(ObjectID))
            {
                return IntegerToObjectClass[ObjectID];
            }
            
            return "unknown";
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
                IntegerToObjectClass.Add(i,lines[i]);
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
