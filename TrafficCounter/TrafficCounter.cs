using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Windows.Forms;
using DirectShowLib;
using Emgu.CV;
using Emgu.CV.Structure;
using NLog;
using VTC.CaptureSource;
using VTC.Kernel.Video;
using VTC.Reporting;
using VTC.Reporting.ReportItems;
using VTC.Common;
using VTC.BatchProcessing;
using VTC.Common.RegionConfig;
using VTC.RegionConfiguration;
using Akka.Actor;
using Akka.Configuration;
using VTC.Actors;
using VTC.Messages;
using VTC.UI;

namespace VTC
{
   public partial class TrafficCounter : Form
   {
        #region VideoDisplays
        private VideoDisplay _mainDisplay;
        //private VideoDisplay _movementDisplay;
        //private VideoDisplay _backgroundDisplay;
        //private VideoDisplay _velocityFieldDisplay;
        private VideoDisplay _mixtureDisplay;
        private VideoDisplay _mixtureMovementDisplay;
        private VideoDisplay _3DPointsDisplay;
        private VideoMux _videoMux;
        #endregion

        #region Mode Flags
        private bool _batchMode;
        private readonly bool _unitTestsMode;
        private readonly bool _isLicensed = false;
        #endregion

        private readonly IActorRef _supervisorActor;
        private readonly ActorSystem _actorSystem;

        private const string IpCamerasFilename = "ipCameras.txt";

        private readonly DateTime _applicationStartTime;

        private readonly List<ICaptureSource> _cameras = new List<ICaptureSource>(); //List of all video input devices. Index, file location, name

        // unit tests has own settings, so need to store "pairs" (capture, settings)
       private CaptureContext[] _testCaptureContexts;

       /// <summary>
       /// Constructor.
       /// </summary>
       /// <param name="settings">Application settings.</param>
       /// <param name="isLicensed">If false, software will shut down after a few minutes</param>
       /// <param name="appArgument">Can mean different things (Local file with video, Unit tests, etc).</param>
        public TrafficCounter(bool isLicensed, string appArgument = null)
       {           
            InitializeComponent();

            _isLicensed = isLicensed;

            var tempLogger = LogManager.GetLogger("userlog"); // special logger for user messages
            if (_isLicensed)
               tempLogger.Log(LogLevel.Info, "License: Active");
            else
               tempLogger.Log(LogLevel.Info, "License: Unactivated");

            // check if app should run in unit test visualization mode
            _unitTestsMode = false;
            if ("-tests".Equals(appArgument, StringComparison.OrdinalIgnoreCase))
            {
                _unitTestsMode = DetectTestScenarios("OptAssignTest.dll");
            }

            // otherwise - run in standard mode
            if (! _unitTestsMode)
            {
                InitializeCameraSelection(appArgument);
            }

            //Disable eventhandler for the changing combobox index.
            CameraComboBox.SelectedIndexChanged -= CameraComboBox_SelectedIndexChanged;

            //Enable eventhandler for the changing combobox index.
            CameraComboBox.SelectedIndexChanged += CameraComboBox_SelectedIndexChanged;

            //Create video windows
            CreateVideoWindows();

            _applicationStartTime = DateTime.Now;            

            var config = ConfigurationFactory.ParseString(@"
                processing-bounded-mailbox {
                    mailbox-type = ""Akka.Dispatch.BoundedMailbox""
                    mailbox-capacity = 10
                    mailbox-push-timeout-time = 10s
                }
                synchronized-dispatcher {
                  type = ""SynchronizedDispatcher""
                   throughput = 10
                }
                ");

            _actorSystem = ActorSystem.Create("VTCActorSystem", config);
            _supervisorActor = _actorSystem.ActorOf(Props.Create(typeof(SupervisorActor)).WithDispatcher("synchronized-dispatcher"), "SupervisorActor");
            _supervisorActor.Tell(new CreateAllActorsMessage(UpdateUI, UpdateStatsUI, UpdateInfoBox, UpdateUIAccessoryInfo));
            _supervisorActor.Tell(new UpdateActorStatusHandlerMessage(UpdateActorStatusIndicators));

       }

        private void UpdateUI(TrafficCounterUIUpdateInfo updateInfo)
       {
           // Update image boxes
           UpdateImageBoxes(updateInfo.StateImage, updateInfo.MovementMask, updateInfo.BackgroundImage, updateInfo.Frame, updateInfo.Measurements, updateInfo.VelocityFieldImage);

           if (!trackedObjectsTextbox.IsHandleCreated)
           {
               return;
           }
           trackedObjectsTextbox.Invoke((MethodInvoker) delegate
            {
                trackedObjectsTextbox.Text = $"{updateInfo.Measurements.Length} objects currently detected";
                trackedObjectsTextbox.Text += Environment.NewLine + $"{updateInfo.StateEstimates.Length} objects currently tracked";
            });

           var activeTime = DateTime.Now - _applicationStartTime;
           if (!timeActiveTextBox.IsHandleCreated)
           {
               return;
           }
            timeActiveTextBox.Invoke((MethodInvoker) delegate
           {
               timeActiveTextBox.Text = activeTime.ToString(@"dd\.hh\:mm\:ss");
           });

           if (!fpsTextLabel.IsHandleCreated)
           {
               return;
           }
            fpsTextLabel.Invoke((MethodInvoker) delegate
               {
                   fpsTextLabel.Text = $"{Math.Round(updateInfo.Fps,1)} FPS";
               }
           );
       }

       private void UpdateUIAccessoryInfo(TrafficCounterUIAccessoryInfo accessoryInfo)
       {
           // Update image boxes
           remainingTimeBox.Invoke((MethodInvoker) delegate
           {
               remainingTimeBox.Text = accessoryInfo.EstimatedTimeRemaining.ToString(@"dd\.hh\:mm\:ss");
           });

           frameTextbox.Invoke((MethodInvoker) delegate
           {
               frameTextbox.Text = accessoryInfo.ProcessedFrames.ToString();
           });
       }

        private void UpdateStatsUI(string statString)
       {
           // Update statistics
           if (!tbVistaStats.IsHandleCreated)
           {
               return;
           }
           tbVistaStats.Invoke((MethodInvoker)delegate
           {
               tbVistaStats.Text = statString;
           });
        }

       private void CheckIfLicenseTimerExpired()
       {
           if(!_isLicensed)
               NotifyLicenseAndExit();
       }

        /// <summary>
        /// Try to detect unit tests. Play unit tests (if detected).
        /// </summary>
        /// <param name="assemblyName">Assembly name with test scenarios.</param>
        /// <returns><c>true</c> if unit tests detected.</returns>
        private bool DetectTestScenarios(string assemblyName)
       {
           var result = false;

           try
           {
               while (! string.IsNullOrWhiteSpace(assemblyName))
               {
                   //TODO: Pass test capture contexts to FrameGrabber actor
                   // ensure absolute path
                   if (! Path.IsPathRooted(assemblyName))
                   {
                       var currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                       if(currentDir != null)
                       assemblyName = Path.Combine(currentDir, assemblyName);
                   }

                   if (! File.Exists(assemblyName)) break;

                   var assembly = Assembly.LoadFile(assemblyName);
                   if (assembly == null) break;

                   _testCaptureContexts = assembly.GetTypes()
                       .Where(t => t.GetInterfaces().Contains(typeof (ICaptureContextProvider))
                                   && (t.GetConstructor(Type.EmptyTypes) != null)) // expected default constructor
                       .Select(t => Activator.CreateInstance(t) as ICaptureContextProvider)
                       .SelectMany(instance => instance.GetCaptures())
                       .ToArray();

                   foreach (var captureContext in _testCaptureContexts)
                   {
                       AddCamera(captureContext.Capture);
                   }

                   Log(LogLevel.Trace, "Test mode detected.");
                   result = true;
                   break;
               }
           }
           catch (Exception e)
           {
               Log(LogLevel.Error, e.ToString());
           }

           return result;
       }

        /// <summary>
       /// Use this function to terminate the application if a trial license timeout occurs
       /// </summary>
        private void NotifyLicenseAndExit()
       {
           MessageBox.Show(
               "This is only a trial version! Please purchase a license from roadometry.com to use software longer than 30 minutes.");
           Application.Exit();
       }

        private void heartbeatTimer_Tick(object sender, EventArgs e)
        {
            var heartbeatPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\VTC\\heartbeat";
            var heartbeatFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\VTC";

            if (!Directory.Exists(heartbeatFolderPath))
                Directory.CreateDirectory(heartbeatFolderPath);

            if (!File.Exists(heartbeatPath))
                File.Create(heartbeatPath).Close();

            File.SetLastWriteTime(heartbeatPath, DateTime.Now);
        }

        #region Camera

        private void AddCamera(ICaptureSource camera)
        {
            _cameras.Add(camera);
            CameraComboBox.Items.Add(camera.Name);
        }

        private void InitializeCameraSelection(string filename)
        {
            // Add video file as source, if provided
            if (UseLocalVideo(filename))
            {
                LoadCameraFromFilename(filename);
            }

            //List all video input devices.
            var systemCameras = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);

            //Variable to indicate the device´s index.
            int deviceIndex = 0;

            //Add every device to the global variable and to the camera combobox
            foreach (DsDevice camera in systemCameras)
            {
                //Add Device with an index and a name to the List.
                AddCamera(new SystemCamera(camera.Name, deviceIndex));

                //Increment the index.
                deviceIndex++;
            }

            if (File.Exists(IpCamerasFilename))
            {
                var ipCameraStrings = File.ReadAllLines(IpCamerasFilename);
                foreach (var split in ipCameraStrings.Select(str => str.Split(',')).Where(split => split.Length == 2))
                    AddCamera(new IpCamera(split[0], split[1]));
            }
        }

        /// <summary>
        /// Check if video file exists.
        /// </summary>
        /// <param name="filename">Pathname to check.</param>
        /// <returns><c>true</c> for existing file.</returns>
        private bool UseLocalVideo(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename)) return false;
            if (File.Exists(filename)) return true;

            Log(LogLevel.Error, $"Video file is not found ('{filename}').");

            return false;
        }

        #endregion

        #region Logging, Measurement and Export

        private void Log(LogLevel logLevel, string text)
        {
            var loggingActor = _actorSystem?.ActorSelection("akka://VTCActorSystem/user/SupervisorActor/LoggingActor");
            loggingActor?.Tell(new LogMessage(text, logLevel));
        }

        private void UserLog(string text)
        {
            var loggingActor = _actorSystem?.ActorSelection("akka://VTCActorSystem/user/SupervisorActor/LoggingActor");
            loggingActor?.Tell(new LogUserMessage(text, LogLevel.Info));
        }

       private void PushStateProcess(object sender, EventArgs e)
       {
           //TODO: Rewrite this function
           //if (_vista != null)
           //{
           //    if (_currentJob.RegionConfiguration.PushToServer)
           //    {
           //        var pushThread = new Thread(PushState);
           //        pushThread.Start();
           //    }

           //    if (!_batchMode)
           //    {
           //        var args = new TrackingEvents.TrackedObjectsEventArgs {Measurements = _vista.CurrentVehicles};
           //        TrackedObjectEvent?.Invoke(this, args);
           //    }
           //}
       }

        #endregion

        #region Batch Processing

        private VideoFileCapture LoadCameraFromFilename(string filename)
        {
            var vfc = new VideoFileCapture(filename);
            // Switched to polled source-complete boolean rather than event. May want to delete the code for events completely.
            //vfc.CaptureCompleteEvent += NotifyProcessingComplete;
            //vfc.CaptureTerminatedEvent += NotifyCaptureTerminated;
            AddCamera(vfc);
            return vfc;
        }


        //TODO: Implement 'capture terminated' in CaptureSource/ICapture by detecting null frame returned from webcam/streaming camera 
        //private void NotifyCaptureTerminated()
        //{
        //    UserLog("Capture terminated unexpectedly");
        //    _loggingActor?.Tell(new LogUserMessage("Capture terminated unexpectedly", LogLevel.Warn));
        //}
        #endregion

        #region Rendering
        private void CreateVideoWindows()
        {
            _mainDisplay = new VideoDisplay("Main", new Point(25, 25));
            _videoMux = new VideoMux();
            _videoMux.AddDisplay(_mainDisplay.ImageBox, _mainDisplay.LayerName);
            _videoMux.Show();
        }

        private void UpdateImageBoxes(Image<Bgr, byte> stateImage, Image<Gray, byte> movementMask, Image<Bgr, float> backgroundImage, Image<Bgr,byte> frame, Measurement[] measurements, Image<Bgr, byte> velocityFieldImage)
        {
            try
            {
                _mainDisplay.Update(stateImage);   
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                throw;
            }
        }
        #endregion

        #region Click Handlers

        private void TrafficCounter_FormClosed(object sender, FormClosedEventArgs e)
        {
        }

        /// <summary>
        /// Method which reacts to the change of the camera selection combobox.
        /// </summary>
        private void CameraComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Create ROI mask for test-mode videos
            if (_unitTestsMode)
            {
                var polygon = new Polygon();
                polygon.AddRange(new[]
                {
                    new Point(0, 0),
                    new Point(0, (int) 480),
                    new Point((int) 640, (int) 480),
                    new Point((int) 640, 0),
                    new Point(0, 0)
                });
                var regionConfig = new RegionConfig
                {
                    RoiMask = polygon,
                    MinObjectSize = 5
                };

                var processingActor = _actorSystem.ActorSelection("akka://VTCActorSystem/user/SupervisorActor/ProcessingActor");
                processingActor.Tell(new UpdateRegionConfigurationMessage(regionConfig));
            }

            //Change the capture device.
            var frameGrabActor = _actorSystem.ActorSelection("akka://VTCActorSystem/user/SupervisorActor/FrameGrabActor");
            frameGrabActor.Tell(new NewVideoSourceMessage(_cameras[CameraComboBox.SelectedIndex]));
            frameGrabActor.Tell(new GetNextFrameMessage());
        }

        private void btnConfigureRegions_Click(object sender, EventArgs e)
        {
            //var configurationActor = _actorSystem.ActorSelection("akka://VTCActorSystem/user/SupervisorActor/ConfigurationActor");
            var configurationActor = _actorSystem.ActorSelection("akka://VTCActorSystem/user/ConfigurationActor");
            configurationActor.Tell(new OpenRegionConfigurationScreenMessage());
        }

        private void SelectVideosButton_Click(object sender, EventArgs e)
        {
            var dr = selectVideoFilesDialog.ShowDialog();
            if (dr == DialogResult.OK)
            {
                var videoJobs = new List<BatchVideoJob>();
                var videoPathsList = selectVideoFilesDialog.FileNames.ToList();
                foreach (var job in videoPathsList.Select(p => new BatchVideoJob {VideoPath = p}))
                {
                    UserLog("Queuing batch video: " + Path.GetFileName(job.VideoPath));
                    videoJobs.Add(job);
                }

                var configurationActor = _actorSystem.ActorSelection("akka://VTCActorSystem/user/ConfigurationActor");
                configurationActor.Tell(new VideoJobsMessage(videoJobs));
                configurationActor.Tell(new CamerasMessage(_cameras));
                configurationActor.Tell(new CurrentJobMessage(videoJobs.First()));
                btnConfigureRegions_Click(null, null);
            }
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void generateReportButton_Click(object sender, EventArgs e)
        {
            var loggingActor = _actorSystem.ActorSelection("akka://VTCActorSystem/user/SupervisorActor/LoggingActor");
            loggingActor?.Tell(new GenerateReportMessage());
        }

       private void UpdateInfoBox(string text)
       {
           if (infoBox.InvokeRequired)
           {
               infoBox.Invoke((MethodInvoker) (() => infoBox.AppendText(text + Environment.NewLine)));
               return;
           }
           infoBox.AppendText(text + Environment.NewLine);
       }

        private void timer5minute_Tick(object sender, EventArgs e)
        {
            var loggingActor = _actorSystem.ActorSelection("akka://VTCActorSystem/user/SupervisorActor/LoggingActor");
            loggingActor?.Tell(new Write5MinuteBinCountsMessage());
        }

        private void timer15minute_Tick(object sender, EventArgs e)
        {
            var loggingActor = _actorSystem.ActorSelection("akka://VTCActorSystem/user/SupervisorActor/LoggingActor");
            loggingActor?.Tell(new Write15MinuteBinCountsMessage());
        }

        private void timer60minute_Tick(object sender, EventArgs e)
        {
            var loggingActor = _actorSystem.ActorSelection("akka://VTCActorSystem/user/SupervisorActor/LoggingActor");
            loggingActor?.Tell(new Write60MinuteBinCountsMessage());
        }

        private void licenseCheckTimer_Tick(object sender, EventArgs e)
        {
            CheckIfLicenseTimerExpired();
            licenseCheckTimer.Enabled = false;
        }

       private void UpdateActorStatusIndicators(Dictionary<string, DateTime> actorStatuses)
       {
            loggingIndicator.Invoke((MethodInvoker)delegate
            {
                if (actorStatuses.ContainsKey("LoggingActor"))
                {
                    var span = DateTime.Now - actorStatuses["LoggingActor"];
                    loggingIndicator.On = span < TimeSpan.FromSeconds(30);
                }
                else
                    loggingIndicator.On = false;
            });

            processingIndicator.Invoke((MethodInvoker)delegate
            {
                if (actorStatuses.ContainsKey("ProcessingActor"))
                {
                    var span = DateTime.Now - actorStatuses["ProcessingActor"];
                    processingIndicator.On = span < TimeSpan.FromSeconds(30);
                }
                else
                    processingIndicator.On = false;
            });

            framegrabbingIndicator.Invoke((MethodInvoker)delegate
            {
                if (actorStatuses.ContainsKey("FrameGrabActor"))
                {
                    var span = DateTime.Now - actorStatuses["FrameGrabActor"];
                    framegrabbingIndicator.On = span < TimeSpan.FromSeconds(30);
                }
                else
                    framegrabbingIndicator.On = false;
            });

            sequencingIndicator.Invoke((MethodInvoker)delegate
            {
                if (actorStatuses.ContainsKey("SequencingActor"))
                {
                    var span = DateTime.Now - actorStatuses["SequencingActor"];
                    sequencingIndicator.On = span < TimeSpan.FromSeconds(30);
                }
                else
                    sequencingIndicator.On = false;
            });

            configurationIndicator.Invoke((MethodInvoker)delegate
            {
                if (actorStatuses.ContainsKey("ConfigurationActor"))
                {
                    var span = DateTime.Now - actorStatuses["ConfigurationActor"];
                    configurationIndicator.On = span < TimeSpan.FromSeconds(30);
                }
                else
                    configurationIndicator.On = false;
            });
        }
    }
}