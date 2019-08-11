using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Timers;
using Akka.Actor;
using Emgu.CV;
using Emgu.CV.Structure;
using Newtonsoft.Json;
using NLog;
using SharpRaven.Data;
using VTC.Classifier;
using VTC.Common;
using VTC.Common.RegionConfig;
using VTC.Kernel.Vistas;
using VTC.Messages;
using VTC.UI;

namespace VTC.Actors
{
    class ProcessingActor: ReceiveActor
    {
        private const int BROADCAST_BACKGROUND_TIMER_MS = 60000;

        private Vista _vista;

        public delegate void UpdateUIDelegate(TrafficCounterUIUpdateInfo updateInfo);
        private UpdateUIDelegate _updateUiDelegate;

        private RegionConfig _config;

        private IActorRef _loggingActor;
        private IActorRef _configurationActor;

        private UInt64 _processedFramesThisBin;
        private DateTime _processedFramesStartTime = DateTime.Now;
        private bool _gotFirstFrame; //Used to re-initialize _processedFramesStartTime
        private UInt64 _processedFramesTotal;
        private Image<Bgr, byte> _mostRecentFrame;

        private static readonly Logger Logger = LogManager.GetLogger("main.form");

        private double _fps;

        private VTC.Common.UserConfig _userConfig = new UserConfig();

        public ProcessingActor()
        {
            try
            { 
                //Subscribe to messages
                Receive<ProcessNextFrameMessage>(newFrameMessage =>
                    NewFrameHandler(newFrameMessage.Frame, newFrameMessage.Timestep)
                );

                Receive<UpdateVideoDimensionsMessage>(message =>
                    UpdateVideoDimensionsHandler(message)
                );

                Receive<UpdateUiHandlerMessage>(message =>
                    UpdateUiHandler(message.UiDelegate)
                );

                Receive<UpdateRegionConfigurationMessage>(configMessage =>
                    UpdateConfig(configMessage.Config)
                );

                Receive<LoggingActorMessage>(message =>
                    UpdateLoggingActor(message.ActorRef)
                );

                Receive<ConfigurationActorMessage>(message =>
                    UpdateConfigurationActor(message.ActorRef)
                );

                Receive<RequestBackgroundFrameMessage>(message =>
                    BroadcastBackgroundFrame()
                );

                Receive<CaptureSourceCompleteMessage>(message =>
                    RetransmitCaptureComplete()
                );

                Receive<CalculateFrameRateMessage>(message =>
                    CalculateFramerate()
                );

                Receive<ValidateConfigurationMessage>(message =>
                    CheckConfiguration()
                );

                Self.Tell(new ActorHeartbeatMessage(Self));

                Context.System.Scheduler.ScheduleTellRepeatedly(new TimeSpan(0, 0, 0), new TimeSpan(0, 0, 5), Context.Parent, new ActorHeartbeatMessage(Self), Self);

                _config = new RegionConfig();
                _vista = new Vista(640, 480, _config);

                if (_vista.CpuMode)
                {
                    MessageBox.Show(
                        "VTC is running on CPU. To process footage faster, use a CUDA-capable nVidia GPU with 6GB VRAM minimum. See hardware specs on Roadometry website for details.", "Processing Rate Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);                    
                }

                Context.System.Scheduler.ScheduleTellRepeatedly(5000, 5000, Self, new CalculateFrameRateMessage(), Self);
                Context.System.Scheduler.ScheduleTellRepeatedly(60000, 5*60000, Self, new RequestBackgroundFrameMessage(), Self);
                Context.System.Scheduler.ScheduleTellRepeatedly(60000, 5*60000, Self, new ValidateConfigurationMessage(), Self);

                _loggingActor?.Tell(new LogMessage("ProcessingActor initialized.", LogLevel.Debug, "ProcessingActor"));
            }
            catch (Exception ex)
            {
                Log("(ProcessingActor) " + ex.Message, LogLevel.Error);
            }
        }

        private void NewFrameHandler(Image<Bgr, byte> frame, double timestep)
        {
            _mostRecentFrame = frame.Clone();
            _vista?.Update(frame,timestep);
            _processedFramesThisBin++;
            _processedFramesTotal++;

            if (!_gotFirstFrame)
            {
                _gotFirstFrame = true;
                _processedFramesStartTime = DateTime.Now;
                Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromMilliseconds(1), Self, new RequestBackgroundFrameMessage(), Self);
            }

            if (_vista != null)
            {
                var stateImage = _vista.GetCurrentStateImage(frame);
                var tui = new TrafficCounterUIUpdateInfo();
                tui.Frame = frame;
                tui.BackgroundImage = _vista.GetBackgroundImage();
                tui.Fps = _fps;
                tui.Measurements = _vista.MeasurementsArray;
                tui.MovementMask = _vista.Movement_Mask;
                tui.StateImage = stateImage;
                tui.StateEstimates = _vista.CurrentVehicles.Select(v => v.StateHistory.Last()).ToArray();
                _updateUiDelegate?.Invoke(tui); 
                stateImage.Dispose();
                _loggingActor?.Tell(new FrameCountMessage(_processedFramesTotal));
                _loggingActor?.Tell(new LogDetectionsMessage(_vista.MeasurementsArray.ToList()));
                // Now update child class specific stats
                var args = new TrackingEvents.TrajectoryListEventArgs { TrackedObjects = _vista.DeletedVehicles };
                _loggingActor?.Tell(new TrackingEventMessage(args));
            }
            frame.Dispose();
        }

        private void UpdateVideoDimensionsHandler(UpdateVideoDimensionsMessage message)
        {
            try
            {
                _loggingActor?.Tell(new LogMessage("ProcessingActor received UpdateVideoDimensionsMessage.", LogLevel.Debug, "ProcessingActor"));
                _loggingActor?.Tell(new LogMessage("ProcessingActor: creating new Vista.", LogLevel.Debug, "ProcessingActor"));
                _vista._height = message.Height;
                _vista._width = message.Width;
                _vista.UpdateRegionConfiguration(_config);
                _loggingActor?.Tell(new LogMessage($"cpuMode: {_vista._yoloClassifier.cpuMode}", LogLevel.Debug, "ProcessingActor"));
            }
            catch (Exception ex)
            {
                Log("(UpdateVideoDimensionsHandler) " + ex.Message, LogLevel.Error);
            }
        }

        private void UpdateUiHandler(UpdateUIDelegate handler)
        {
            try
            {
                _updateUiDelegate = handler;
            }
            catch (Exception ex)
            {
                Log("(UpdateUiHandler) " + ex.Message, LogLevel.Error);
            }
        }

        private void CalculateFramerate()
        {
            var currentTime = DateTime.Now;
            var sampleTime = currentTime - _processedFramesStartTime;
            var fps = _processedFramesThisBin / sampleTime.TotalSeconds;
            _processedFramesStartTime = DateTime.Now;
            _processedFramesThisBin = 0;
            _fps = fps;
            _loggingActor.Tell(new LogMessage($"FPS: {_fps}", LogLevel.Debug, "ProcessingActor"));
        }

        private void UpdateConfig(RegionConfig config)
        {
            try
            {
                _config = config;
                _vista?.UpdateRegionConfiguration(config);
            }
            catch (Exception ex)
            {
                Log("(UpdateConfig) " + ex.Message, LogLevel.Error);
            }
            
        }

        private void UpdateLoggingActor(IActorRef actorRef)
        {
            try
            {
                _loggingActor = actorRef;
            }
            catch (Exception ex)
            {
                Log("(UpdateLoggingActor) " + ex.Message, LogLevel.Error);
            }   
        }

        private void UpdateConfigurationActor(IActorRef actorRef)
        {
            try
            {
                _configurationActor = actorRef;
            }
            catch (Exception ex)
            {
                Log("(UpdateConfigurationActor) " + ex.Message, LogLevel.Error);
            }   
        }

        private void BroadcastBackgroundFrame()
        {
            if (_mostRecentFrame == null)
            {
                return;
            }
            _loggingActor?.Tell(new HandleUpdatedBackgroundFrameMessage(_mostRecentFrame.Clone()));   
        }

        private void RetransmitCaptureComplete()
        {
            _loggingActor?.Tell(new CaptureSourceCompleteMessage());
        }

        public static Props Props()
        {
            return Akka.Actor.Props.Create(() => new ProcessingActor());
        }

        private void CheckConfiguration()
        {
            if (_config == null)
            {
                _loggingActor.Tell(new LogMessage("RegionConfig is null.", LogLevel.Error, "ProcessingActor"));
                _configurationActor.Tell(new RequestConfigurationMessage(Self));
            }
            else if (_config.RoiMask == null)
            {
                _loggingActor.Tell(new LogMessage("ProcessingActor: ROI mask is null.", LogLevel.Error, "ProcessingActor"));
                _configurationActor.Tell(new RequestConfigurationMessage(Self));
            }
            else if (_config.RoiMask.Count < 3)
            {
                _loggingActor.Tell(new LogMessage("ProcessingActor: ROI mask has " + _config.RoiMask.Count + " vertices; 3 or more expected.", LogLevel.Error, "ProcessingActor"));
                _configurationActor.Tell(new RequestConfigurationMessage(Self));
            }
            else if (!_config.RoiMask.PolygonClosed)
            {
                _loggingActor.Tell(new LogMessage("ProcessingActor: ROI mask is not a closed polygon.", LogLevel.Error, "ProcessingActor"));
                _configurationActor.Tell(new RequestConfigurationMessage(Self));
            }
        }

        private void Log(string text, LogLevel level)
        {
            Logger.Log(level, "ProcessingActor: " + text);
        }
    }
}
