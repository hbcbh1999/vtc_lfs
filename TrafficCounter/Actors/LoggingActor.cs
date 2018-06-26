using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Forms;
using Akka.Actor;
using Emgu.CV;
using Emgu.CV.Structure;
using NLog;
using VTC.Classifier;
using VTC.Common;
using VTC.Common.RegionConfig;
using VTC.Kernel;
using VTC.Messages;
using VTC.Reporting;

namespace VTC.Actors
{
    public class LoggingActor : ReceiveActor
    {
        private DateTime _videoTime;
        private DateTime _lastLogVideoTime;
        private DateTime _last5MinbinTime;
        private DateTime _last15MinbinTime;
        private DateTime _last60MinbinTime;

        private bool _5minSampleBinWritten = false;
        private bool _15minSampleBinWritten = false;
        private bool _60minSampleBinWritten = false;

        private bool _batchMode = true;

        private readonly MovementCount _turnStats = new MovementCount();
        private readonly MovementCount _5MinTurnStats = new MovementCount();
        private readonly MovementCount _15MinTurnStats = new MovementCount();
        private readonly MovementCount _60MinTurnStats = new MovementCount();

        private static readonly Logger Logger = LogManager.GetLogger("main.form");
        private static readonly Logger UserLogger = LogManager.GetLogger("userlog"); // special logger for user messages

        private RegionConfig _regionConfig;
        private EventConfig _eventConfig;
        private string _currentVideoName = "Unknown video";
        private int _width = 640;
        private int _height = 480;

        private MultipleTrajectorySynthesizer mts;

        private Dictionary<int, string> _classIdMapping = new Dictionary<int, string>();

        private IActorRef _sequencingActor;
        private Image<Bgr, byte> _background;

        public delegate void UpdateStatsUIDelegate(string statString);
        private UpdateStatsUIDelegate _updateStatsUiDelegate;

        public delegate void UpdateInfoUIDelegate(string infoString);
        private UpdateInfoUIDelegate _updateInfoUiDelegate;

        private YoloIntegerNameMapping _yoloNameMapping = new YoloIntegerNameMapping();

        public LoggingActor()
        {
            InitializeEventConfig();

            mts = new MultipleTrajectorySynthesizer();

            Receive<FileCreationTimeMessage>(message =>
                UpdateFileCreationTime(message.FileCreationTime)
            );

            Receive<LogMessage>(message =>
                Log(message.Text, message.Level)
            );

            Receive<LogUserMessage>(message =>
                LogUser(message.Text, message.Level)
            );

            Receive<GenerateReportMessage>(message =>
                GenerateReport()
            );

            Receive<Write5MinuteBinCountsMessage>(message =>
                Log5MinBinCounts()
            );

            Receive<Write15MinuteBinCountsMessage>(message =>
                Log15MinBinCounts()
            );

            Receive<Write60MinuteBinCountsMessage>(message =>
                Log60MinBinCounts()
            );

            Receive<NewVideoSourceMessage>(message =>
                UpdateVideoSourceInfo(message)
            );

            Receive<WriteAllBinnedCountsMessage>(message =>
                WriteBinnedCounts(message.Timestep)
            );

            Receive<TrackingEventMessage>(message =>
                TrajectoryListHandler(message.EventArgs)
            );

            Receive<UpdateRegionConfigurationMessage>(message =>
                UpdateConfig(message.Config)
            );

            Receive<HandleUpdatedBackgroundFrameMessage>(message =>
                UpdateBackgroundFrame(message.Frame)
            );

            Receive<CaptureSourceCompleteMessage>(message =>
                GenerateReport()
            );

            Receive<UpdateStatsUiHandlerMessage>(message =>
                UpdateStatsUiHandler(message.StatsUiDelegate)
            );

            Receive<UpdateInfoUiHandlerMessage>(message =>
                UpdateInfoUiHandler(message.InfoUiDelegate)
            );

            Receive<ActorHeartbeatMessage>(message =>
                Heartbeat()
            );

            Receive<SequencingActorMessage>(message =>
                UpdateSequencingActor(message.ActorRef)
            );

            Receive<HandleClassIDMappingMessage>(message =>
                UpdateClassIDMapping(message.ClassIdMapping)
            );

            Receive<LogDetectionsMessage>(message =>
                LogDetections(message.Detections)
            );

            Receive<LogAssociationsMessage>(message =>
                LogAssociations(message.Associations)
            );

            Receive<CopyGroundtruthMessage>(message =>
                CopyGroundTruth(message.GroundTruthPath)
            );

            Receive<VideoMetadataMessage>(message => 
                LogVideoMetadata(message.VM)
            );

            //Receive<LogPerformanceMessage>(message =>
            //    Heartbeat()
            //);

            Self.Tell(new ActorHeartbeatMessage());
        }

        private void UpdateFileCreationTime(DateTime dt)
        {
            _videoTime = dt;
            _lastLogVideoTime = dt;
            _last5MinbinTime = dt;
            _last15MinbinTime = dt;
            _last60MinbinTime = dt;
        }

        private void WriteBinnedCounts(double timestep)
        {
            if(_regionConfig == null)
            {
                return; 
            }

            try
            {
                _videoTime += TimeSpan.FromSeconds(timestep);

                if (_videoTime - _lastLogVideoTime > TimeSpan.FromMilliseconds(_regionConfig.StateUploadIntervalMs))
                {
                    _lastLogVideoTime = _videoTime;
                }

                if (_videoTime - _last5MinbinTime > TimeSpan.FromMinutes(5))
                {
                    WriteBinnedMovements5Min(_videoTime, _5MinTurnStats);
                    _last5MinbinTime = _videoTime;
                }

                if (_videoTime - _last15MinbinTime > TimeSpan.FromMinutes(15))
                {
                    WriteBinnedMovements15Min(_videoTime, _15MinTurnStats);
                    _last15MinbinTime = _videoTime;
                }

                if (_videoTime - _last60MinbinTime > TimeSpan.FromMinutes(60))
                {
                    WriteBinnedMovements60Min(_videoTime, _60MinTurnStats);
                    _last60MinbinTime = _videoTime;
                }
            }
            catch(NullReferenceException ex)
            {
                Logger.Log(LogLevel.Error, ex.Message);
            }
            
        }

        private string GenerateCountFilename(int minutes, string className)
        {
            string filename = $"{minutes}-minute binned counts [{className}].csv";
            //TODO: Figure out how to access video name here
            string folderPath = VTCPaths.FolderPath(_currentVideoName);
            string filepath = Path.Combine(folderPath, filename);
            return filepath;
        }

        private long _totalCountsWrittenTo5MinCsv = 0;
        private void WriteBinnedMovements5Min(DateTime timestamp, Dictionary<Movement, long> turnStats)
        {
            //TODO: Figure out how to get video name
            {
                foreach(var detectionClass in DetectionClasses.ClassDetectionWhitelist)
                {
                    var filepath = GenerateCountFilename(5, detectionClass);
                    Dictionary<Movement,long> filteredTurnStats = turnStats.Where(kvp => kvp.Key.TrafficObjectType.ToString().ToLower() == detectionClass).ToDictionary(kvp => kvp.Key,kvp => kvp.Value);
                    WriteBinnedMovementsToFile(filepath, filteredTurnStats, timestamp);    
                }

                foreach (KeyValuePair<Movement, long> countpair in turnStats)
                {
                    _totalCountsWrittenTo5MinCsv += countpair.Value;
                }

                _5MinTurnStats.Clear();
                _5minSampleBinWritten = true;
            }
        }

        private void WriteBinnedMovements15Min(DateTime timestamp, Dictionary<Movement, long> turnStats)
        {
            //TODO: Figure out how to get video name
            {
                foreach(var detectionClass in DetectionClasses.ClassDetectionWhitelist)
                {
                    var filepath = GenerateCountFilename(15,detectionClass);
                    Dictionary<Movement,long> filteredTurnStats = turnStats.Where(kvp => kvp.Key.TrafficObjectType.ToString().ToLower() == detectionClass).ToDictionary(kvp => kvp.Key,kvp => kvp.Value);
                    WriteBinnedMovementsToFile(filepath, filteredTurnStats, timestamp);
                }
                
                _15MinTurnStats.Clear();
                _15minSampleBinWritten = true;
            }
        }

        private void WriteBinnedMovements60Min(DateTime timestamp, Dictionary<Movement, long> turnStats)
        {
            //TODO: Figure out how to get video name
            {
                foreach(var detectionClass in DetectionClasses.ClassDetectionWhitelist)
                {
                    var filepath = GenerateCountFilename(60,detectionClass);
                    Dictionary<Movement,long> filteredTurnStats = turnStats.Where(kvp => kvp.Key.TrafficObjectType.ToString().ToLower() == detectionClass).ToDictionary(kvp => kvp.Key,kvp => kvp.Value);
                    WriteBinnedMovementsToFile(filepath, filteredTurnStats, timestamp);
                }
                
                _60MinTurnStats.Clear();
                _60minSampleBinWritten = true;
            }
        }

        private void CreateOrReplaceOutputFolderIfExists()
        {
            //Create output folder
            //TODO: Figure out how to get _selectedCamera.Name value here
            var folderPath = VTC.Common.VTCPaths.FolderPath(_currentVideoName);
            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, true);
            }
            Directory.CreateDirectory(folderPath);

            _turnStats.Clear();
            _5MinTurnStats.Clear();
            _15MinTurnStats.Clear();
            _60MinTurnStats.Clear();
        }

        private void WriteBinnedMovementsToFile(string path, Dictionary<Movement, long> turnStats, DateTime timestamp)
        {
            try
            {
                //Pad turnStats with non-present movements with count equal to zero.
                //1. Get full list of possible movements
                foreach(var m in mts.TrajectoryPrototypes)
                {            
                    //2. Check which keys are not present
                    if(!turnStats.Keys.Contains(m))
                    { 
                        //3. Add the non-present keys 
                        turnStats.Add(m,0);
                    }
                }
            }
            catch(System.ArgumentException ex)
            {
                Logger.Log(LogLevel.Error, ex.Message);
            }

            
            //Sort turnStats so that columns are consistent in output CSV
            var sd = new SortedDictionary<Movement,long>();
            foreach(var ts in turnStats)
            {
                try
                {
                    sd.Add(ts.Key,ts.Value);
                }
                catch(ArgumentException ex)
                { 
                    Logger.Log(LogLevel.Error, ex);
                }
            }

            try
            {
                using (var sw = File.AppendText(path))
                {
                    var binString = "";
                    binString += timestamp + ",";
                    foreach (var stat in sd)
                    {
                        binString += stat.Key.Approach + " to " + stat.Key.Exit + "," + stat.Key.TurnType + "," + stat.Value + ",";
                    }
                    sw.WriteLine(binString);
                }
            }
            catch (FileNotFoundException ex)
            {
                Logger.Log(LogLevel.Error, ex.Message);
            }

        }

        private void LogDetections(List<Measurement> detections)
        {
            try
            {
                var filename = "Detections";
                filename = filename.Replace("file-", "");
                var folderPath = VTCPaths.FolderPath(_currentVideoName);
                var filepath = Path.Combine(folderPath, filename);
                var dl = new DetectionLogger(detections);
                dl.LogToJsonfile(filepath);
            }
            catch (NullReferenceException e)
            {
                Logger.Log(LogLevel.Error, e);
            }
        }

        private void LogAssociations(Dictionary<Measurement,TrackedObject> associations)
        {
            try
            {
                var filename = "Associations";
                filename = filename.Replace("file-", "");
                var folderPath = VTCPaths.FolderPath(_currentVideoName);
                var filepath = Path.Combine(folderPath, filename);
                var al = new AssociationLogger(_regionConfig, associations);
                al.LogToTextfile(filepath);
            }
            catch (NullReferenceException e)
            {
                Logger.Log(LogLevel.Error, e);
            }
        }

        private void Log(string text, LogLevel level)
        {
            Logger.Log(level, text);
        }

        private void LogUser(string text, LogLevel level)
        {
            UserLogger.Log(level, text);
            _updateInfoUiDelegate.Invoke(text);
        }

        private void Log5MinBinCounts()
        {
            if (!_batchMode)
                WriteBinnedMovements5Min(DateTime.Now, _5MinTurnStats);
        }

        private void Log15MinBinCounts()
        {
            if (!_batchMode)
                WriteBinnedMovements15Min(DateTime.Now, _15MinTurnStats);
        }

        private void Log60MinBinCounts()
        {
            if (!_batchMode)
                WriteBinnedMovements60Min(DateTime.Now, _60MinTurnStats);
        }

        private void GenerateReport()
        {
            try
            {
                var folderPath = VTCPaths.FolderPath(_currentVideoName);
                GenerateRegionsLegendImage(folderPath);

                if(_5MinTurnStats.TotalCount() > 0)
                {
                    WriteBinnedMovements5Min(_videoTime, _5MinTurnStats); 
                }

                if(_15MinTurnStats.TotalCount() > 0)
                {
                    WriteBinnedMovements15Min(_videoTime, _15MinTurnStats);
                }
                
                if(_60MinTurnStats.TotalCount() > 0)
                {
                    WriteBinnedMovements60Min(_videoTime, _60MinTurnStats);     
                }

                //Check that accumulated totals from different sources make sense
                if (_totalCountsWrittenTo5MinCsv != _totalTrajectoriesCounted)
                {
                    Logger.Log(LogLevel.Error, "Accumulated total mismatch in GenerateReport()");
                }

                using (StreamWriter outputFile = new StreamWriter(folderPath + @"\Version.txt", true))
                {
                    outputFile.WriteLine(Assembly.GetExecutingAssembly().GetName().Version);
                }

                SummaryReportGenerator.CopyAssetsToExportFolder(folderPath);
                foreach(var type in Enum.GetValues(typeof(ObjectType)).Cast<ObjectType>())
                {
                    SummaryReportGenerator.GenerateSummaryReportHTML(folderPath, _currentVideoName, _videoTime, type.ToString().ToLower()); 
                }

                _sequencingActor?.Tell(new CaptureSourceCompleteMessage(folderPath));
            }
            catch (NullReferenceException e)
            {
                Logger.Log(LogLevel.Error, e);
            }

        }

        private void GenerateRegionsLegendImage(string folderPath)
        {
            try
            {
                var maskedBackground = _background.Clone();
                foreach (var p in _regionConfig.Regions)
                {
                    //if (!p.Key.Contains("Approach")) continue;
                    var mask = p.Value.GetMask(_background.Width, _background.Height, new Bgr(60, 60, 60)).Convert<Bgr, byte>();
                    maskedBackground = maskedBackground.Add(mask);
                }

                var g = Graphics.FromImage(maskedBackground.Bitmap);
                foreach (var p in _regionConfig.Regions)
                {
                    //if (!p.Key.Contains("Approach")) continue;
                    if (p.Value.Count <= 2) continue;
                    var x = p.Value.Centroid.X;
                    var y = p.Value.Centroid.Y;
                    var font = new Font(FontFamily.GenericSansSerif, (float)14.0);
                    var size = TextRenderer.MeasureText(p.Key, font);
                    g.DrawString(p.Key, font,
                        new SolidBrush(Color.Black), x - size.Width / 2, y);
                }

                maskedBackground.Save(Path.Combine(folderPath,
                    "RegionsLegend.png"));
            }
            catch (Exception ex)
            {
                //TODO: Deal with exception
            }
        }

        private void UpdateVideoSourceInfo(NewVideoSourceMessage message)
        {
            _currentVideoName = message.CaptureSource.Name;
            CreateOrReplaceOutputFolderIfExists();
        }

        private void UpdateConfig(RegionConfig config)
        {
            _regionConfig = config;

            const string filename = "Synthetic Trajectories";
            var folderPath = VTCPaths.FolderPath(_currentVideoName);
            var filepath = Path.Combine(folderPath, filename);

            mts.GenerateSyntheticTrajectories(_regionConfig, filepath);
        }

        private int _totalTrajectoriesCounted = 0;
        private void TrajectoryListHandler(TrackingEvents.TrajectoryListEventArgs args)
        {
            try
            {
                foreach (var d in args.TrackedObjects)
                {
                    var mostLikelyClassType =
                        YoloIntegerNameMapping.GetObjectNameFromClassInteger(d.StateHistory.Last().MostFrequentClassId(),
                            _yoloNameMapping.IntegerToObjectClass);
                    var movement = MatchNearestTrajectory(d,mostLikelyClassType);
                    if (movement == null) continue;
                    var uppercaseClassType = CommonFunctions.FirstCharToUpper(mostLikelyClassType);
                    movement.TrafficObjectType = (ObjectType) Enum.Parse(typeof(ObjectType),uppercaseClassType);
                    movement.Timestamp = _videoTime;
                    movement.StateEstimates = d.StateHistory;
                    IncrementTurnStatistics(movement);
                    var tl = new TrajectoryLogger(movement);
                    var folderPath = VTCPaths.FolderPath(_currentVideoName);
                    const string filename = "Movements";
                    var filepath = Path.Combine(folderPath, filename);
                    tl.Save(filepath);
                    _totalTrajectoriesCounted++;
                }

                var stats = GetStatString();
                _updateStatsUiDelegate?.Invoke(stats);
            }
            catch(Exception ex)
            { 
                Logger.Log(LogLevel.Error,ex.Message);    
            }

            
        }

        private Movement MatchNearestTrajectory(TrackedObject d, string classType)
        {
            var distance = d.DistanceTravelled();
            if (distance < _regionConfig.MinPathLength) return null;

            var matchedTrajectoryName = TrajectorySimilarity.BestMatchTrajectory(d.StateHistory, mts.TrajectoryPrototypes, classType);
            return matchedTrajectoryName;
        }

        private void IncrementTurnStatistics(Movement tp)
        {
            if (!_turnStats.ContainsKey(tp)) _turnStats[tp] = 0;
            _turnStats[tp]++;

            if (!_5MinTurnStats.ContainsKey(tp)) _5MinTurnStats[tp] = 0;
            _5MinTurnStats[tp]++;

            if (!_15MinTurnStats.ContainsKey(tp)) _15MinTurnStats[tp] = 0;
            _15MinTurnStats[tp]++;

            if (!_60MinTurnStats.ContainsKey(tp)) _60MinTurnStats[tp] = 0;
            _60MinTurnStats[tp]++;
        }

        private void InitializeEventConfig()
        {
            //Hard-coded configuration for a standard intersection. TODO: Load configuration from eventConfig.xml 
            _eventConfig = new EventConfig();
            _eventConfig.Events.Add(new RegionTransition(1, 1, false), "straight");
            _eventConfig.Events.Add(new RegionTransition(2, 2, false), "straight");
            _eventConfig.Events.Add(new RegionTransition(3, 3, false), "straight");
            _eventConfig.Events.Add(new RegionTransition(4, 4, false), "straight");

            _eventConfig.Events.Add(new RegionTransition(1, 2, false), "left");
            _eventConfig.Events.Add(new RegionTransition(2, 3, false), "left");
            _eventConfig.Events.Add(new RegionTransition(3, 4, false), "left");
            _eventConfig.Events.Add(new RegionTransition(4, 1, false), "left");

            _eventConfig.Events.Add(new RegionTransition(1, 4, false), "right");
            _eventConfig.Events.Add(new RegionTransition(2, 1, false), "right");
            _eventConfig.Events.Add(new RegionTransition(3, 2, false), "right");
            _eventConfig.Events.Add(new RegionTransition(4, 3, false), "right");

            _eventConfig.Events.Add(new RegionTransition(1, 3, false), "uturn");
            _eventConfig.Events.Add(new RegionTransition(2, 4, false), "uturn");
            _eventConfig.Events.Add(new RegionTransition(3, 1, false), "uturn");
            _eventConfig.Events.Add(new RegionTransition(4, 2, false), "uturn");

            _eventConfig.Events.Add(new RegionTransition(1, 2, true), "crossing" );
            _eventConfig.Events.Add(new RegionTransition(2, 1, true), "crossing" );

            _eventConfig.Events.Add(new RegionTransition(2, 3, true), "crossing" );
            _eventConfig.Events.Add(new RegionTransition(3, 2, true), "crossing" );

            _eventConfig.Events.Add(new RegionTransition(3, 4, true), "crossing" );
            _eventConfig.Events.Add(new RegionTransition(4, 3, true), "crossing" );

            _eventConfig.Events.Add(new RegionTransition(4, 1, true), "crossing" );
            _eventConfig.Events.Add(new RegionTransition(1, 4, true), "crossing" );
        }

        private void UpdateBackgroundFrame(Image<Bgr, byte> image)
        {
            _background = image.Clone();
            image.Dispose();
        }

        public string GetStatString()
        {
            var sb = new StringBuilder();

            var totalObjects = 0;
            foreach (var kvp in _turnStats)
            {
                sb.AppendLine(kvp.Key + ":  " + kvp.Value);
                totalObjects += (int)kvp.Value;
            }

            sb.AppendLine("");
            sb.AppendLine("Total objects counted: " + totalObjects);

            return sb.ToString();
        }

        private void UpdateStatsUiHandler(UpdateStatsUIDelegate handler)
        {
            try
            {
                _updateStatsUiDelegate = handler;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception in UpdateStatsUiHandler:" + ex.Message);
            }
        }

        private void UpdateInfoUiHandler(UpdateInfoUIDelegate handler)
        {
            try
            {
                _updateInfoUiDelegate = handler;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception in UpdateInfoUiHandler:" + ex.Message);
            }
        }

        private void Heartbeat()
        {
            Context.Parent.Tell(new ActorHeartbeatMessage());
            Context.System.Scheduler.ScheduleTellOnce(5000, Self, new ActorHeartbeatMessage(), Self);
        }

        private void UpdateSequencingActor(IActorRef actorRef)
        {
            try
            {
                _sequencingActor = actorRef;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception in UpdateSequencingActor:" + ex.Message);
            }
        }

        private void UpdateClassIDMapping(Dictionary<int, string> classIdMapping)
        {
            _classIdMapping = classIdMapping;
        }

        private void CopyGroundTruth(string groundTruthPath)
        {
            if (groundTruthPath == null)
            {
                Log("Logging actor: Ground truth file path is null.", LogLevel.Info);
                return;
            }
            var folderPath = VTCPaths.FolderPath(_currentVideoName);
            var filepath = Path.Combine(folderPath, "Manual counts.json");
                File.Copy(groundTruthPath, filepath, true);
        }

        private void LogVideoMetadata(VideoMetadata vm)
        {
            var folderPath = VTC.Common.VTCPaths.FolderPath(_currentVideoName);
            if (!Directory.Exists(folderPath)) return;

            using (var outputFile = new StreamWriter(folderPath + @"\video_metadata.json", false))
            {
                var ser = new DataContractJsonSerializer(typeof(VideoMetadata));  
                ser.WriteObject(outputFile.BaseStream, vm);  
            }
        }
    }
}