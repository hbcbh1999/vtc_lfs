using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Messaging;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using Akka.Actor;
using NLog;
using VTC.BatchProcessing;
using VTC.CaptureSource;
using VTC.Common;
using VTC.Common.RegionConfig;
using VTC.Kernel.Video;
using VTC.Messages;

namespace VTC.Actors
{
    class SequencingActor : ReceiveActor
    {
        private readonly List<ICaptureSource> _cameras = new List<ICaptureSource>(); //List of all video input devices. Index, file location, name
        private List<BatchVideoJob> _videoJobs;
        private BatchVideoJob _currentJob;
        private IActorRef _loggingActor;
        private IActorRef _frameGrabActor;
        private IActorRef _processingActor;
        private IActorRef _configActor;
        private Dictionary<VideoProcessingRequestMessage, BatchVideoJob> _automationRequestJobs;
        private MessageQueue _automationProcessingCompleteMessageQueue;
        private string _currentVideoName = "";

        public SequencingActor()
        {
            _videoJobs = new List<BatchVideoJob>();
            _automationRequestJobs = new Dictionary<VideoProcessingRequestMessage, BatchVideoJob>();
            try
            {
                _automationProcessingCompleteMessageQueue = new MessageQueue(@".\private$\vtcvideoprocesscompletenotification");
                _automationProcessingCompleteMessageQueue.Formatter = new XmlMessageFormatter(new Type[] { typeof(VideoProcessingCompleteNotificationMessage) });
            }
            catch (MessageQueueException ex)
            {
            }

            Receive<LoggingActorMessage>(message =>
                UpdateLoggingActor(message.ActorRef)
            );

            Receive<NewProcessingActorMessage>(message =>
                HandleNewProcessingActor(message.ActorRef)
            );

            Receive<FrameGrabActorMessage>(message =>
                HandleNewFrameGrabActor(message.ActorRef)
            );

            Receive<ConfigurationActorMessage>(message =>
                HandleNewConfigurationActor(message.ActorRef)
            );

            Receive<CaptureSourceCompleteMessage>(message =>
                SendNotificationForLastAndDequeue()
            );

            Receive<VideoJobsMessage>(message =>
                ResetAndEnqueueVideos(message.VideoJobsList)
            );

            Receive<ActorHeartbeatMessage>(message =>
                Heartbeat()
            );

            Receive<CheckVideoProcessingRequestsIPCMessage>(message =>
                CheckVideoProcessingRequestsIPC()
            );

            Receive<RegionConfigLookupResponseMessage>(message =>
                AssociateJobWithConfiguration(message.Configuration, message.JobGuid)
            );

            Self.Tell(new ActorHeartbeatMessage());
        }

        private void SendNotificationForLastAndDequeue()
        {
            var vprm = new VideoProcessingCompleteNotificationMessage();
            vprm.JobGuid = _currentJob.JobGuid;
            vprm.ConfigurationName = _currentJob.RegionConfiguration.Title;
            vprm.VideoFilePath = _currentJob.VideoPath;
            vprm.OutputFolderPath = VTC.Common.VTCPaths.FolderPath(_currentVideoName);
            vprm.ManualCountsPath = _currentJob.GroundTruthPath;
            _automationProcessingCompleteMessageQueue.Send(vprm);
            DequeueVideo();
        }

        private void DequeueVideo()
        {
            if (_videoJobs.Count > 0)
            {
                _loggingActor?.Tell(new LogUserMessage("Loading video from batch", LogLevel.Info));
                _currentJob = _videoJobs.First();

                if (_currentJob.RegionConfiguration != null)
                {
                    var captureSource = LoadCameraFromFilename(_currentJob.VideoPath);
                    _frameGrabActor.Tell(new NewVideoSourceMessage(captureSource));
                    _currentVideoName = captureSource.Name;
                    _frameGrabActor.Tell(new UpdateVideoPropertiesMessage(captureSource.FrameRate, captureSource.FrameCount));
                    _loggingActor.Tell(new NewVideoSourceMessage(captureSource));
                    DateTime videoTime = File.GetCreationTime(_currentJob.VideoPath);
                    _loggingActor.Tell(new FileCreationTimeMessage(videoTime));
                    _processingActor.Tell(new UpdateRegionConfigurationMessage(_currentJob.RegionConfiguration));
                    _loggingActor.Tell(new UpdateRegionConfigurationMessage(_currentJob.RegionConfiguration));
                    _loggingActor.Tell(new CopyGroundtruthMessage(_currentJob.GroundTruthPath));
                    var vm = VideoMetadata.ExtractFromVideo(_currentJob.VideoPath);
                    _loggingActor.Tell(new VideoMetadataMessage(vm));
                    _frameGrabActor.Tell(new GetNextFrameMessage());
                }
                else
                {
                    Log(LogLevel.Warn, $"Ignoring job without configuration ({_currentJob.VideoPath})");
                    UserLog("Ignoring job without configuration");
                }

                _videoJobs.Remove(_currentJob);

            }
            else
            {
                UserLog("Batch complete.");
                UserLog("Movement counts saved to desktop.");
            }
        }

        private VideoFileCapture LoadCameraFromFilename(string filename)
        {
            var vfc = new VideoFileCapture(filename);
            AddCamera(vfc);
            return vfc;
        }

        private void AddCamera(ICaptureSource camera)
        {
            _cameras.Add(camera);
        }

        private void Log(LogLevel logLevel, string text)
        {
            _loggingActor?.Tell(new LogMessage(text, logLevel));
        }

        private void UserLog(string text)
        {
            _loggingActor?.Tell(new LogUserMessage(text, LogLevel.Info));
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

        private void HandleNewProcessingActor(IActorRef actorRef)
        {
            _processingActor = actorRef;
        }

        private void HandleNewFrameGrabActor(IActorRef actorRef)
        {
            _frameGrabActor = actorRef;
        }

        private void HandleNewConfigurationActor(IActorRef actorRef)
        {
            _configActor = actorRef;
        }

        private void Heartbeat()
        {
            Context.Parent.Tell(new ActorHeartbeatMessage());
            CheckVideoProcessingRequestsIPC();
            Context.System.Scheduler.ScheduleTellOnce(5000, Self, new ActorHeartbeatMessage(), Self);
            Self.Tell(new CheckVideoProcessingRequestsIPCMessage());
        }

        private void EnqueueVideoJobs(List<BatchVideoJob> newVideoJobs)
        {
            _videoJobs.AddRange(newVideoJobs);
        }

        private void ResetAndEnqueueVideos(List<BatchVideoJob> newVideoJobs)
        {
            _cameras.Clear();
            _videoJobs.AddRange(newVideoJobs);
            DequeueVideo();
        }

        private void CheckVideoProcessingRequestsIPC()
        {
            if (!IsMSMQInstalled())
            {
                return;
            }

            if (!VideoProcessingRequestQueueExists())
            {
                return;
            }

            GetVideoProcessingRequestsMSMQ();
        }

        private bool IsMSMQInstalled()
        {
            List<ServiceController> services = ServiceController.GetServices().ToList();
            ServiceController msQue = services.Find(o => o.ServiceName == "MSMQ");
            if (msQue?.Status == ServiceControllerStatus.Running)
            {
                return true;
            }

            return false;
        }

        private bool VideoProcessingRequestQueueExists()
        {
            return MessageQueue.Exists(@".\private$\VTCVideoProcessRequest");
        }

        private void GetVideoProcessingRequestsMSMQ()
        {
            if (_configActor == null)
            {
                return;
            }

            try
            {
                using (MessageQueue msmq = new MessageQueue(@".\private$\VTCVideoProcessRequest"))
                {
                    System.Messaging.Message[] messages = msmq.GetAllMessages();
                    for (int i = 0; i < messages.Count(); i++)
                    {
                        var msg = msmq.Receive(new TimeSpan(0));
                        if (msg == null) continue;
                        msg.Formatter =
                            new XmlMessageFormatter(new Type[1] { typeof(VideoProcessingRequestMessage) });
                        
                        var vprm = (VideoProcessingRequestMessage) msg.Body;
                        MessageBox.Show("Video processing request: " + vprm.VideoFilePath + ", " + vprm.ConfigurationName + ", " + vprm.ManualCountsPath);

                        //At this point we don't have the RegionConfig object associated with this file. We need
                        //to convert the ConfigurationName (String) into a RegionConfig object. 

                        var bvj = new BatchVideoJob();
                        bvj.JobGuid = vprm.JobGuid;
                        bvj.VideoPath = vprm.VideoFilePath;
                        bvj.GroundTruthPath = vprm.ManualCountsPath;
                        _automationRequestJobs.Add(vprm,bvj);

                        var rcnlm = new RegionConfigNameLookupMessage(vprm.ConfigurationName,bvj.JobGuid);
                        _configActor.Tell(rcnlm);
                    }
                }
            }
            catch (MessageQueueException e)
            {
            }   
        }

        private void AssociateJobWithConfiguration(RegionConfig config, Guid jobGuid)
        {
            bool eachHasConfiguration = true;
            Console.WriteLine("Got config:" + config.Title);
            foreach (var bvj in _automationRequestJobs.Values)
            {
                if (bvj.JobGuid == jobGuid)
                {
                    bvj.RegionConfiguration = config;
                    EnqueueVideoJobs(new List<BatchVideoJob> { bvj });
                }

                if (bvj.RegionConfiguration == null)
                {
                    eachHasConfiguration = false;
                }
            }

            if (!eachHasConfiguration) return;

            //When all videos have recieved their configuration callbacks, begin processing
            _cameras.Clear();
            DequeueVideo();
        }
    }

    [Serializable]
    public class VideoProcessingRequestMessage
    {
        public string VideoFilePath = "";
        public string ConfigurationName = "";
        public string ManualCountsPath = "";
        public Guid JobGuid;
    }

    [Serializable]
    public class VideoProcessingCompleteNotificationMessage
    {
        public string VideoFilePath = "";
        public string OutputFolderPath = "";
        public string ConfigurationName = "";
        public string ManualCountsPath = "";
        public Guid JobGuid;
    }
}
