using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Akka.Actor;
using Emgu.CV;
using Emgu.CV.Structure;
using Newtonsoft.Json;
using NLog;
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

        private int _processedFramesThisBin;
        private DateTime _processedFramesStartTime = DateTime.Now;
        private bool _gotFirstFrame; //Used to re-initialize _processedFramesStartTime
        private int _processedFramesTotal;
        private Image<Bgr, byte> _mostRecentFrame;

        private Timer _broadcastBackgroundTimer = new Timer();

        private double _fps;

        public ProcessingActor()
        {
            try
            {
                //Subscribe to messages
                Receive<ProcessNextFrameMessage>(newFrameMessage =>
                    NewFrameHandler(newFrameMessage.Frame)
                );

                Receive<UpdateVideoDimensionsMessage>(message =>
                    UpdateVideoDimensionsHandler(message)
                );

                Receive<UpdateUiHandlerMessage>(message =>
                    UpdateUiHandler(message.UiDelegate)
                );

                Receive<RequestUpdateClassIDMappingMessage>(message => 
                    BroadcastClassIDMapping()
                );

                Receive<UpdateRegionConfigurationMessage>(configMessage =>
                    UpdateConfig(configMessage.Config)
                );

                Receive<LoggingActorMessage>(message =>
                    UpdateLoggingActor(message.ActorRef)
                );

                Receive<RequestBackgroundFrameMessage>(message =>
                    BroadcastBackgroundFrame()
                );

                Receive<CaptureSourceCompleteMessage>(message =>
                    RetransmitCaptureComplete()
                );

                Receive<ActorHeartbeatMessage>(message =>
                    Heartbeat()
                );

                Receive<CalculateFrameRateMessage>(message =>
                    CalculateFramerate()
                );

                Self.Tell(new ActorHeartbeatMessage());

                Context.System.Scheduler.ScheduleTellRepeatedly(5000, 5000, Self, new CalculateFrameRateMessage(), Self);

                _config = new RegionConfig();
                _vista = new Vista(640, 480,
                    _config); //TODO: Investigate what resolution Vista should be initialized to
                _loggingActor?.Tell(new LogMessage($"cpuMode: {_vista._yoloClassifier.cpuMode}", LogLevel.Debug));
                
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception in ProcessingActor initialization:" + ex.Message);
            }
        }

        private void NewFrameHandler(Image<Bgr, byte> frame)
        {
            _mostRecentFrame = frame.Clone();
            _vista?.Update(frame);
            _processedFramesThisBin++;
            _processedFramesTotal++;

            if (!_gotFirstFrame)
            {
                _gotFirstFrame = true;
                _processedFramesStartTime = DateTime.Now;
                Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromMilliseconds(1), Self, new RequestBackgroundFrameMessage(), Self);
                _broadcastBackgroundTimer.Interval = BROADCAST_BACKGROUND_TIMER_MS;
                _broadcastBackgroundTimer.Tick += (o, i) =>
                {
                    Self.Tell(new RequestBackgroundFrameMessage());
                    Self.Tell(new RequestUpdateClassIDMappingMessage());
                };
                _broadcastBackgroundTimer.Start();
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
                tui.VelocityFieldImage = frame.Clone();
                _vista.DrawVelocityField(tui.VelocityFieldImage, new Bgr(Color.White), 1);
                _updateUiDelegate?.Invoke(tui); 
                stateImage.Dispose();
                _loggingActor?.Tell(new WriteAllBinnedCountsMessage(_config.Timestep));
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
                _vista = new Vista(message.Width, message.Height, _config);
                _loggingActor?.Tell(new LogMessage($"cpuMode: {_vista._yoloClassifier.cpuMode}", LogLevel.Debug));
                _vista.UpdateRegionConfiguration(_config);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception in UpdateVideoDimensionsHandler:" + ex.Message);
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
                MessageBox.Show("Exception in UpdateUiHandler:" + ex.Message);
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
            if (fps > 0.001)
            {
                _loggingActor.Tell(new LogMessage($"FPS: {_fps}", LogLevel.Debug));
            }
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
                MessageBox.Show("Exception in UpdateConfig:" + ex.Message);
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
                MessageBox.Show("Exception in UpdateLoggingActor:" + ex.Message);
            }   
        }

        private void BroadcastBackgroundFrame()
        {
            //Note: Background no longer exists since YoloV2 has replaced movement-based detection.
            //This function remains in order to give the logging actor a picture of the intersection for report generation.
            //Rather than the actual background, the most recent frame is transmitted.

            //if (_vista?.ColorBackground != null)
            {
                _loggingActor?.Tell(new HandleUpdatedBackgroundFrameMessage(_mostRecentFrame.Clone()));
            }
        }

        private void RetransmitCaptureComplete()
        {
            _loggingActor?.Tell(new CaptureSourceCompleteMessage());
        }

        public static Props Props()
        {
            return Akka.Actor.Props.Create(() => new ProcessingActor());
        }

        private void Heartbeat()
        {
            Context.Parent.Tell(new ActorHeartbeatMessage());
            Context.System.Scheduler.ScheduleTellOnce(5000, Self, new ActorHeartbeatMessage(), Self);
        }

        private void BroadcastClassIDMapping()
        {
            {
                _loggingActor?.Tell(new HandleClassIDMappingMessage(_vista._yoloNameMapping.IntegerToObjectClass));
            }
        }
    }
}
