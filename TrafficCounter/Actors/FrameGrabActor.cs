﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VTC.Kernel.Video;
using VTC.Messages;
using Akka;
using Akka.Actor;
using Emgu.CV.CvEnum;
using Timer = System.Threading.Timer;
using NLog;
using VTC.UI;

namespace VTC.Actors
{
    class FrameGrabActor : ReceiveActor
    {
        private ICaptureSource CaptureSource;
        private IActorRef ProcessingActor;
        private IActorRef LoggingActor;
        private IActorRef SequencingActor;

        private int FramesProcessed = 0;
        private double TotalFramesInVideo = 0;
        private DateTime ProcessingStartTime;
        private bool LiveCapture = false;
        private int LowFpsCount = 0;
        private const double LowFpsThreshold = 0.5;
        private const int LowFpsCountThreshold = 2;

        public delegate void UpdateUIDelegate(TrafficCounterUIAccessoryInfo accessoryInfo);
        private UpdateUIDelegate _updateUiDelegate;

        private const int FRAME_DELAY_MS = 1;

        private bool _loggingActorNeedsBackgroundUpdate = false;

        public FrameGrabActor()
        {
            Receive<NewVideoSourceMessage>(message =>
                HandleNewVideoSource(message.CaptureSource)
            );

            Receive<NewProcessingActorMessage>(message =>
                HandleNewProcessingActor(message.ActorRef)
            );

            Receive<GetNextFrameMessage>(message =>
                GetNewFrame()
            );

            Receive<LoggingActorMessage>(message =>
                UpdateLoggingActor(message.ActorRef)
            );

            Receive<SequencingActorMessage>(message =>
                UpdateSequencingActor(message.ActorRef)
            );

            Receive<ActorHeartbeatMessage>(message =>
                Heartbeat()
            );

            Receive<UpdateUiAccessoryHandlerMessage>(message =>
                UpdateUiHandler(message.UiDelegate)
            );

            Receive<UpdateVideoPropertiesMessage>(message =>
                UpdateVideoProperties(message.Fps, message.TotalFrames)
            );

            Self.Tell(new ActorHeartbeatMessage());
        }

        private void HandleNewVideoSource(ICaptureSource captureSource)
        {
            if (captureSource == CaptureSource) return;

            if (captureSource == null)
            {
                CaptureSource = null;
                return;
            }

            CaptureSource?.Destroy();
            CaptureSource = captureSource;
            CaptureSource.Init();
            
            
            FramesProcessed = 0;
            ProcessingStartTime = DateTime.Now;

            //Use hard-coded VGA resolution instead of captureSource.Width / captureSource.Height
            ProcessingActor.Tell(new UpdateVideoDimensionsMessage(640, 480));
            LoggingActor.Tell(new LogMessage($"CaptureSource: {captureSource.Name}", LogLevel.Debug));
        }

        private void GetNewFrame()
        {
            var frame = CaptureSource?.QueryFrame();
            var fps = CaptureSource?.FPS();

            if (frame != null && fps != null)
            {
                var timestep = (fps.Value == 0.0) ? 0.1 : 1.0/fps.Value;
                var cloned = frame.Clone();
                { ProcessingActor?.Tell(new ProcessNextFrameMessage(cloned,timestep)); }
                if(FramesProcessed < 10)
                { 
                    var clone2 = frame.Clone();
                    var configurationActor = Context.ActorSelection("akka://VTCActorSystem/user/ConfigurationActor");
                    configurationActor.Tell(new FrameMessage(clone2));
                }
                FramesProcessed++;
                Context.System.Scheduler.ScheduleTellOnce(FRAME_DELAY_MS, Self, new GetNextFrameMessage(), Self);

                if (fps < LowFpsThreshold)
                {
                    LowFpsCount++;
                }

                if (LowFpsCount > LowFpsCountThreshold)
                {
                    LoggingActor.Tell(new LogMessage("Frame-rate low, performing error-recovery.", LogLevel.Debug));
                    CaptureSource.ErrorRecovery();
                }
            }

            var completed = CaptureSource?.CaptureComplete();
            var isLive = CaptureSource?.IsLiveCapture();
            if (completed.HasValue && completed.Value)
            {
                if(isLive.HasValue && isLive.Value)
                {
                    LoggingActor.Tell(new LogMessage("Capture has stopped, performing error-recovery.", LogLevel.Debug));
                    CaptureSource.ErrorRecovery();
                    return; //Don't terminate frame-grab process during live acquisition, even if CaptureComplete indicates that the capture is complete.
                }

                ProcessingActor?.Tell(new RequestBackgroundFrameMessage());
                ProcessingActor?.Tell(new CaptureSourceCompleteMessage());
                LoggingActor?.Tell(new LogUserMessage("Video complete", LogLevel.Info));
            }
        }

        private void HandleNewProcessingActor(IActorRef actorRef)
        {
            ProcessingActor = actorRef;
        }

        public static Props Props()
        {
            return Akka.Actor.Props.Create(() => new FrameGrabActor());
        }

        private void UpdateLoggingActor(IActorRef actorRef)
        {
            try
            {
                LoggingActor = actorRef;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception in UpdateLoggingActor:" + ex.Message);
            }
        }

        private void UpdateSequencingActor(IActorRef actorRef)
        {
            try
            {
                SequencingActor = actorRef;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception in UpdateSequencingActor:" + ex.Message);
            }
        }

        private void Heartbeat()
        {
            Context.Parent.Tell(new ActorHeartbeatMessage());
            Context.System.Scheduler.ScheduleTellOnce(5000, Self, new ActorHeartbeatMessage(), Self);

            if (TotalFramesInVideo == 0)
                return;

            var totalProcessingTime = DateTime.Now - ProcessingStartTime;
            var framesProcessedPerSecond = FramesProcessed / totalProcessingTime.TotalSeconds;
            var estimatedRemainingTimeSeconds = (TotalFramesInVideo - FramesProcessed) / framesProcessedPerSecond;

            if (estimatedRemainingTimeSeconds < 0)
                return;

            if (estimatedRemainingTimeSeconds == Double.PositiveInfinity)
                return;

            //TODO: Investigate how estimatedRemainingTimeSeconds is becoming NaN
            if (estimatedRemainingTimeSeconds == Double.NaN)
            {
                estimatedRemainingTimeSeconds = 0.0;
            }

            var remainingTime = TimeSpan.FromSeconds(estimatedRemainingTimeSeconds);

            var tui = new TrafficCounterUIAccessoryInfo();
            tui.EstimatedTimeRemaining = remainingTime;
            tui.ProcessedFrames = FramesProcessed;
            _updateUiDelegate?.Invoke(tui);
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

        private void UpdateVideoProperties(double fps, double totalFrames)
        {
            TotalFramesInVideo = totalFrames;
        }
    }
}
