using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Forms;
using Akka.Actor;
using Emgu.CV;
using Emgu.CV.Structure;
using NLog;
using SharpRaven;
using SharpRaven.Data;
using VTC.Classifier;
using VTC.Common;
using VTC.Common.RegionConfig;
using VTC.Kernel;
using VTC.Messages;
using VTC.Remote;
using VTC.Reporting;

namespace VTC.Actors
{
    public class LoggingActor : ReceiveActor
    {
        private UInt64 _nFramesProcessed = 0;
        private double _fps = 24.0;
        //private DateTime _lastLogVideoTime;
        //private DateTime _last5MinbinTime;
        //private DateTime _last15MinbinTime;
        //private DateTime _last60MinbinTime;

        private DateTime _next5MinBinTime;
        private DateTime _next15MinBinTime;
        private DateTime _next60MinBinTime;

        //private bool _5minSampleBinWritten = false;
        //private bool _15minSampleBinWritten = false;
        //private bool _60minSampleBinWritten = false;

        private bool _liveMode = true;

        private readonly MovementCount _turnStats = new MovementCount();
        private readonly MovementCount _5MinTurnStats = new MovementCount();
        private readonly MovementCount _15MinTurnStats = new MovementCount();
        private readonly MovementCount _60MinTurnStats = new MovementCount();

        private static readonly Logger Logger = LogManager.GetLogger("main.form");
        private static readonly Logger UserLogger = LogManager.GetLogger("userlog"); // special logger for user messages

        private RegionConfig _regionConfig;
        private EventConfig _eventConfig;
        private string _currentVideoName = "Unknown video";
        private DateTime _videoStartTime = DateTime.Now;
        private string _currentOutputFolder;

        private MultipleTrajectorySynthesizer mts;

        private Dictionary<int, string> _classIdMapping = new Dictionary<int, string>();

        private IActorRef _sequencingActor;
        private Image<Bgr, byte> _background;

        public delegate void UpdateStatsUIDelegate(string statString);
        private UpdateStatsUIDelegate _updateStatsUiDelegate;

        public delegate void UpdateInfoUIDelegate(string infoString);
        private UpdateInfoUIDelegate _updateInfoUiDelegate;

        public delegate void UpdateDebugDelegate(string debugString);
        private UpdateDebugDelegate _updateDebugDelegate;

        private YoloIntegerNameMapping _yoloNameMapping = new YoloIntegerNameMapping();

        RavenClient ravenClient = new RavenClient("https://5cdde3c580914972844fda3e965812ae@sentry.io/1248715");

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

            Receive<GenerateDailyReportMessage>(message =>
                GenerateDailyReport()
            );

            Receive<UpdateStatsUiHandlerMessage>(message =>
                UpdateStatsUiHandler(message.StatsUiDelegate)
            );

            Receive<UpdateInfoUiHandlerMessage>(message =>
                UpdateInfoUiHandler(message.InfoUiDelegate)
            );

            Receive<UpdateDebugHandlerMessage>(message =>
                UpdateDebugHandler(message.DebugDelegate)
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

            Receive<FrameCountMessage>(message => 
                UpdateFrameCount(message.Count)
            );

            //Receive<LogPerformanceMessage>(message =>
            //    Heartbeat()
            //);

            Self.Tell(new ActorHeartbeatMessage());

            //Context.System.Scheduler.ScheduleTellRepeatedly(new TimeSpan(1,0,0),new TimeSpan(1,0,0),Self, new GenerateDailyReportMessage(), Self);
        }

        private void UpdateFileCreationTime(DateTime dt)
        {
            _videoStartTime = dt;
            SetAllNextBinTime(dt);
        }

        private void SetNext5MinBinTime(DateTime tnow)
        {
            _next5MinBinTime = RoundUp(tnow, TimeSpan.FromMinutes(5));   
        }

        private void SetNext15MinBinTime(DateTime tnow)
        { 
            _next15MinBinTime = RoundUp(tnow, TimeSpan.FromMinutes(15));
        }

        private void SetNext60MinBinTime(DateTime tnow)
        { 
            _next60MinBinTime = RoundUp(tnow, TimeSpan.FromMinutes(60));
        }

        private void SetAllNextBinTime(DateTime tnow)
        { 
            SetNext5MinBinTime(tnow);
            SetNext15MinBinTime(tnow);
            SetNext60MinBinTime(tnow);
        }

        //This function taken from user 'dtb' on StackOverflow
        //https://stackoverflow.com/questions/7029353/how-can-i-round-up-the-time-to-the-nearest-x-minutes
        private DateTime RoundUp(DateTime dt, TimeSpan d)
        {
            return new DateTime((dt.Ticks + d.Ticks - 1) / d.Ticks * d.Ticks, dt.Kind);
        }

        private void WriteBinnedCounts()
        {
            if(_regionConfig == null)
            {
                return; 
            }

            try
            {
                var tnow = VideoTime();

                if (tnow >= _next5MinBinTime)
                {
                    WriteBinnedMovements5Min(tnow, _5MinTurnStats);
                    SetNext5MinBinTime(tnow);
                }

                if (tnow >= _next15MinBinTime)
                {
                    WriteBinnedMovements15Min(tnow, _15MinTurnStats);
                    SetNext15MinBinTime(tnow);
                }

                if (tnow >= _next60MinBinTime)
                {
                    WriteBinnedMovements60Min(tnow, _60MinTurnStats);
                    SetNext60MinBinTime(tnow);
                }
            }
            catch(NullReferenceException ex)
            {
                Logger.Log(LogLevel.Error, ex.Message);
                ravenClient.Capture(new SentryEvent(ex));
            }
            
        }

        private string GenerateCountFilename(int minutes, ObjectType className)
        {
            string filename = $"{minutes}-minute binned counts [{className.ToString().ToLower()}].csv";
            //TODO: Figure out how to access video name here
            string folderPath = _currentOutputFolder;
            string filepath = Path.Combine(folderPath, filename);
            return filepath;
        }

        private long _totalCountsWrittenTo5MinCsv = 0;
        private void WriteBinnedMovements5Min(DateTime timestamp, Dictionary<Movement, long> turnStats)
        {            
            {
                foreach(var detectionClass in DetectionClasses.ClassDetectionWhitelist)
                {
                    var filepath = GenerateCountFilename(5, detectionClass);
                    Dictionary<Movement,long> filteredTurnStats = turnStats.Where(kvp => kvp.Key.TrafficObjectType == detectionClass).ToDictionary(kvp => kvp.Key,kvp => kvp.Value);
                    WriteBinnedMovementsToFile(filepath, filteredTurnStats, timestamp, detectionClass);    
                }

                foreach (KeyValuePair<Movement, long> countpair in turnStats)
                {
                    _totalCountsWrittenTo5MinCsv += countpair.Value;
                }

                _5MinTurnStats.Clear();
            }
        }

        private void WriteBinnedMovements15Min(DateTime timestamp, Dictionary<Movement, long> turnStats)
        {
            //TODO: Figure out how to get video name
            {
                foreach(var detectionClass in DetectionClasses.ClassDetectionWhitelist)
                {
                    var filepath = GenerateCountFilename(15,detectionClass);
                    Dictionary<Movement,long> filteredTurnStats = turnStats.Where(kvp => kvp.Key.TrafficObjectType == detectionClass).ToDictionary(kvp => kvp.Key,kvp => kvp.Value);
                    WriteBinnedMovementsToFile(filepath, filteredTurnStats, timestamp, detectionClass);
                }
                
                _15MinTurnStats.Clear();
            }
        }

        private void WriteBinnedMovements60Min(DateTime timestamp, Dictionary<Movement, long> turnStats)
        {
            //TODO: Figure out how to get video name
            {
                foreach(var detectionClass in DetectionClasses.ClassDetectionWhitelist)
                {
                    var filepath = GenerateCountFilename(60,detectionClass);
                    Dictionary<Movement,long> filteredTurnStats = turnStats.Where(kvp => kvp.Key.TrafficObjectType == detectionClass).ToDictionary(kvp => kvp.Key,kvp => kvp.Value);
                    WriteBinnedMovementsToFile(filepath, filteredTurnStats, timestamp, detectionClass);
                }
                
                _60MinTurnStats.Clear();
            }
        }

        private void CreateOrReplaceOutputFolderIfExists()
        {
            //Create output folder
            //TODO: Figure out how to get _selectedCamera.Name value here
            var folderPath = VTC.Common.VTCPaths.FolderPath(_currentVideoName,_videoStartTime);
            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, true);
            }
            Directory.CreateDirectory(folderPath);

            _turnStats.Clear();
            _5MinTurnStats.Clear();
            _15MinTurnStats.Clear();
            _60MinTurnStats.Clear();

            var filepath = Path.Combine(folderPath, "Movements.json");
            File.Create(filepath);     
            _currentOutputFolder = folderPath;
        }

        private void WriteBinnedMovementsToFile(string path, Dictionary<Movement, long> turnStats, DateTime timestamp, ObjectType objectType)
        {
            try
            {
                //Pad turnStats with non-present movements with count equal to zero.
                //1. Get full list of possible movements
                foreach(var m in mts.TrajectoryPrototypes)
                {   
                    //var modified_prototype = m;
                    var modified_prototype = new Movement(m.Approach, m.Exit, m.TurnType, objectType, m.StateEstimates, 0);
                    //2. Check which keys are not present
                    if(!turnStats.Keys.Contains(modified_prototype))
                    { 
                        //3. Add the non-present keys 
                        //3a. Only pad with crossing-movements if we're looking at Person stats
                        if((modified_prototype.TurnType == Turn.Crossing) && objectType == ObjectType.Person)
                        { 
                            turnStats.Add(modified_prototype,0);    
                        }
                        
                        //3a. Only pad with road movements if we're looking at vehicle (anything other than Person) stats
                        if((modified_prototype.TurnType != Turn.Crossing) && objectType != ObjectType.Person)
                        { 
                            turnStats.Add(modified_prototype,0);    
                        }
                    }
                }
            }
            catch(System.ArgumentException ex)
            {
                Logger.Log(LogLevel.Error, ex.Message);
                ravenClient.Capture(new SentryEvent(ex));
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
                ravenClient.Capture(new SentryEvent(ex));
            }

        }


        private void LogDetections(List<Measurement> detections)
        {
            try
            {
                var filename = "Detections";
                filename = filename.Replace("file-", "");
                var folderPath = _currentOutputFolder;
                var filepath = Path.Combine(folderPath, filename);
                var dl = new DetectionLogger(detections);
                dl.LogToJsonfile(filepath);
            }
            catch (NullReferenceException e)
            {
                Logger.Log(LogLevel.Error, e);
                ravenClient.Capture(new SentryEvent(e));
            }
        }

        private void LogAssociations(Dictionary<Measurement,TrackedObject> associations)
        {
            try
            {
                var filename = "Associations";
                filename = filename.Replace("file-", "");
                var folderPath = _currentOutputFolder;
                var filepath = Path.Combine(folderPath, filename);
                var al = new AssociationLogger(_regionConfig, associations);
                al.LogToTextfile(filepath);
            }
            catch (NullReferenceException e)
            {
                Logger.Log(LogLevel.Error, e);
                ravenClient.Capture(new SentryEvent(e));
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
            if (_liveMode)
                WriteBinnedMovements5Min(DateTime.Now, _5MinTurnStats);
        }

        private void Log15MinBinCounts()
        {
            if (_liveMode)
                WriteBinnedMovements15Min(DateTime.Now, _15MinTurnStats);
        }

        private void Log60MinBinCounts()
        {
            if (_liveMode)
                WriteBinnedMovements60Min(DateTime.Now, _60MinTurnStats);
        }

        private void GenerateReport()
        {
            try
            {
                var folderPath = _currentOutputFolder;
                GenerateRegionsLegendImage(folderPath);

                var tnow = VideoTime();

                if(_5MinTurnStats.TotalCount() > 0)
                {
                    WriteBinnedMovements5Min(tnow, _5MinTurnStats); 
                }

                if(_15MinTurnStats.TotalCount() > 0)
                {
                    WriteBinnedMovements15Min(tnow, _15MinTurnStats);
                }
                
                if(_60MinTurnStats.TotalCount() > 0)
                {
                    WriteBinnedMovements60Min(tnow, _60MinTurnStats);     
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
                    if(type != ObjectType.Unknown)
                    {
                        SummaryReportGenerator.GenerateSummaryReportHTML(folderPath, _currentVideoName, tnow, type.ToString().ToLower());
                    }
                }

                SummaryReportGenerator.GenerateAllVehiclesSummaryReportHTML(folderPath, _currentVideoName, tnow);

                _sequencingActor?.Tell(new CaptureSourceCompleteMessage(folderPath));

                var ev = new SentryEvent("Report generated");
                 ev.Level = ErrorLevel.Info;
                 ev.Tags.Add("Path", folderPath);
                 ravenClient.Capture(ev);
            }
            catch (NullReferenceException e)
            {
                Logger.Log(LogLevel.Error, e);
            }

        }

        private void GenerateDailyReport()
        {
            if(!_liveMode)
            {
                return;
            }

            Logger.Log(LogLevel.Info, "Generating daily report...");

            GenerateReport();

            _videoStartTime = DateTime.Now;
            CreateOrReplaceOutputFolderIfExists();

            Logger.Log(LogLevel.Info, "Daily report generated. Starting new day.");
        }

        private void GenerateRegionsLegendImage(string folderPath)
        {
            try
            {
                var maskedBackground = _background.Clone();
                foreach (var p in _regionConfig.Regions)
                {
                    var mask = p.Value.GetMask(_background.Width, _background.Height, new Bgr(60, 60, 60)).Convert<Bgr, byte>();
                    maskedBackground = maskedBackground.Add(mask);
                }

                var g = Graphics.FromImage(maskedBackground.Bitmap);
                foreach (var p in _regionConfig.Regions)
                {
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
                Logger.Log(LogLevel.Error, ex);
            }
        }

        private void UpdateVideoSourceInfo(NewVideoSourceMessage message)
        {
            _currentVideoName = message.CaptureSource.Name;
            CreateOrReplaceOutputFolderIfExists();

            var ev = new SentryEvent("New video source");
            ev.Level = ErrorLevel.Info;
            ev.Tags.Add("Name", message.CaptureSource.Name);
            ravenClient.Capture(ev);
            _videoStartTime = DateTime.Now;

            _liveMode = message.CaptureSource.IsLiveCapture();
            _fps = message.CaptureSource.FPS();

            if(_liveMode)
            {
                UpdateFileCreationTime(DateTime.Now);
            }
        }

        private void UpdateConfig(RegionConfig config)
        {
            _regionConfig = config;

            const string filename = "Synthetic Trajectories";
            var folderPath = _currentOutputFolder;
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
                            _yoloNameMapping.IntegerToObjectName);
                    var validity = TrajectorySimilarity.ValidateTrajectory(d,  _regionConfig.MinPathLength, _regionConfig.MissRatioThreshold, _regionConfig.PositionCovarianceThreshold);
                    if(validity.valid == false)
                    {
                        _updateDebugDelegate?.Invoke(validity.description);
                        continue;
                    }
                    var movement = TrajectorySimilarity.MatchNearestTrajectory(d, mostLikelyClassType, _regionConfig.MinPathLength, mts.TrajectoryPrototypes);
                    if (movement == null) continue;
                    var uppercaseClassType = CommonFunctions.FirstCharToUpper(mostLikelyClassType);
                    var edited_movement = new Movement(movement.Approach, movement.Exit, movement.TurnType, (ObjectType) Enum.Parse(typeof(ObjectType),uppercaseClassType), d.StateHistory, d.FirstDetectionFrame);
                    IncrementTurnStatistics(edited_movement);
                    var tl = new TrajectoryLogger(edited_movement);
                    var folderPath = _currentOutputFolder;
                    const string filename = "Movements";
                    var filepath = Path.Combine(folderPath, filename);
                    tl.Save(filepath);

                    if(Properties.Settings.Default.RemoteServerUpload)
                    {
                        var rs = new RemoteServer();
                        var rsr = rs.SendMovement(edited_movement, Properties.Settings.Default.SiteToken, Properties.Settings.Default.ServerURL).Result;
                        if (rsr != HttpStatusCode.OK)
                        {
                            Log("Movement POST failed:" + rsr, LogLevel.Error);
                        }
                    }

                    _totalTrajectoriesCounted++;
                }

                var stats = GetStatString();
                _updateStatsUiDelegate?.Invoke(stats);

                WriteBinnedCounts();
            }
            catch(Exception ex)
            { 
                Logger.Log(LogLevel.Error,ex.Message);    
            }

            
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

        private DateTime VideoTime()
        { 
            if(_liveMode)
            {
                return DateTime.Now;
            }

            DateTime videoTime = _videoStartTime.AddSeconds(_nFramesProcessed/_fps);
            return videoTime;
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

            if (Properties.Settings.Default.RemoteServerUpload)
            {
                var rs = new RemoteServer();
                var rsr = rs.SendImage(_background.Bitmap,Properties.Settings.Default.SiteToken,Properties.Settings.Default.ServerURL).Result;

                if (rsr != HttpStatusCode.Found)
                { 
                    Log("Image-upload failed:" + rsr, LogLevel.Error);
                } 
            }
        }

        public string GetStatString()
        {
            var sb = new StringBuilder();
            var totalObjects = 0;

            if(Properties.Settings.Default.SimplifiedCountDisplay)
            { 
               sb.AppendLine(PerApproachClassCount(_turnStats, "Approach 1"));
               sb.AppendLine(PerApproachClassCount(_turnStats, "Approach 2"));
               sb.AppendLine(PerApproachClassCount(_turnStats, "Approach 3"));
               sb.AppendLine(PerApproachClassCount(_turnStats, "Approach 4"));
                foreach (var kvp in _turnStats)
                {
                    totalObjects += (int)kvp.Value;
                }
            }
            else
            {
                foreach (var kvp in _turnStats)
                {
                    sb.AppendLine(kvp.Key + ":  " + kvp.Value);
                    totalObjects += (int)kvp.Value;
                }
            }

            sb.AppendLine("");
            sb.AppendLine("Total objects counted: " + totalObjects);

            return sb.ToString();
        }

        private string PerApproachClassCount(MovementCount count, string approachName)
        { 
            var sb = new StringBuilder();

            long carCount = 0;
            long busCount = 0;
            long bicycleCount = 0;
            long motorcycleCount = 0;
            long truckCount = 0;
            long personCount = 0;
            long unknownCount = 0;

            foreach (var kvp in _turnStats)
            {
                if(kvp.Key.Approach == approachName)
                {
                    switch(kvp.Key.TrafficObjectType)
                    {
                        case ObjectType.Car:
                            carCount += kvp.Value;
                            break;
                        case ObjectType.Bus:
                            busCount += kvp.Value;
                            break;
                        case ObjectType.Bicycle:
                            bicycleCount += kvp.Value;
                            break;
                        case ObjectType.Motorcycle:
                            motorcycleCount += kvp.Value;
                            break;
                        case ObjectType.Truck:
                            truckCount += kvp.Value;
                            break;
                        case ObjectType.Person:
                            personCount += kvp.Value;
                            break;
                        case ObjectType.Unknown:
                            unknownCount += kvp.Value;
                            break;
                    }
                }
            }

            sb.AppendLine(approachName + "\tCar: " + carCount + Environment.NewLine
                 + "\t\tTruck: " + truckCount + Environment.NewLine 
                 + "\t\tBus: " + busCount + Environment.NewLine 
                 + "\t\tBicycle: " + bicycleCount + Environment.NewLine 
                 + "\t\tMotorcycle: " + motorcycleCount + Environment.NewLine 
                 + "\t\tPerson: " + personCount + Environment.NewLine
                );

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

        private void UpdateDebugHandler(UpdateDebugDelegate handler)
        {
            try
            {
                _updateDebugDelegate = handler;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception in UpdateDebugDelegate:" + ex.Message);
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
            try
            {
                if (groundTruthPath == null)
                {
                    Log("Logging actor: Ground truth file path is null.", LogLevel.Info);
                    return;
                }
                var folderPath = _currentOutputFolder;
                var filepath = Path.Combine(folderPath, "Manual counts.json");
                    File.Copy(groundTruthPath, filepath, true);
            }
            catch(Exception ex)
            {
                Logger.Log(LogLevel.Error, ex);
            }
            
        }

        private void LogVideoMetadata(VideoMetadata vm)
        {
            var folderPath = _currentOutputFolder;
            if (!Directory.Exists(folderPath)) return;

            using (var outputFile = new StreamWriter(folderPath + @"\video_metadata.json", false))
            {
                var ser = new DataContractJsonSerializer(typeof(VideoMetadata));  
                ser.WriteObject(outputFile.BaseStream, vm);  
            }
        }

        private void UpdateFrameCount(UInt64 count)
        { 
            _nFramesProcessed = count;    
        }
    }
}