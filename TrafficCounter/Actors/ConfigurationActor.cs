using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Akka.Actor;
using NLog;
using VTC.BatchProcessing;
using VTC.CaptureSource;
using VTC.Common.RegionConfig;
using VTC.Kernel.Video;
using VTC.Messages;
using VTC.RegionConfiguration;
using Emgu.CV;
using Emgu.CV.Structure;

namespace VTC.Actors
{
    class ConfigurationActor : ReceiveActor
    {
        IActorRef _frameGrabActor;
        IActorRef _processingActor;
        IActorRef _loggingActor;
        IActorRef _sequencingActor;
        private IActorRef _supervisorActor;

        private BatchVideoJob _currentJob;
        private List<ICaptureSource> _cameras = new List<ICaptureSource>(); //List of all video input devices. Index, file location, name
        private List<BatchVideoJob> _videoJobs = new List<BatchVideoJob>();
        private Image<Bgr,byte> _backgroundFrame;

        private static readonly string RegionConfigSavePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                                                              "\\VTC\\regionConfig.xml";
        private readonly IRegionConfigDataAccessLayer _regionConfigDataAccessLayer = new FileRegionConfigDal(RegionConfigSavePath);
        private List<RegionConfig> _regionConfigs;

        public ConfigurationActor()
        {
            _regionConfigs = _regionConfigDataAccessLayer.LoadRegionConfigList();

            Receive<FrameGrabActorMessage>(message =>
                UpdateFrameGrabActor(message.ActorRef)
            );

            Receive<NewProcessingActorMessage>(message =>
                UpdateProcessingActor(message.ActorRef)
            );

            Receive<LoggingActorMessage>(message =>
                UpdateLoggingActor(message.ActorRef)
            );

            Receive<SequencingActorMessage>(message =>
                UpdateSequencingActor(message.ActorRef)
            );

            Receive<SupervisorActorMessage>(message =>
                UpdateSupervisorActor(message.ActorRef)
            );

            Receive<OpenRegionConfigurationScreenMessage>(message =>
                ConfigureRegions()
            );

            Receive<CurrentJobMessage>(message =>
                UpdateCurrentJob(message.CurrentJob)
            );

            Receive<CamerasMessage>(message =>
                UpdateCameras(message.Cameras)
            );

            Receive<VideoJobsMessage>(message =>
                UpdateVideoJobs(message.VideoJobsList)
            );

            Receive<RegionConfigNameLookupMessage>(message =>
                LookupRegionConfig(message.RegionConfigName, message.JobGuid)
            );

            Receive<ActorHeartbeatMessage>(message =>
                Heartbeat()
            );

            Receive<FrameMessage>(message =>
                ReceiveNewBackground(message.Frame)
            );

            Self.Tell(new ActorHeartbeatMessage());
        }

        private void UpdateFrameGrabActor(IActorRef actorref)
        {
            _frameGrabActor = actorref;
        }

        private void UpdateProcessingActor(IActorRef actorref)
        {
            _processingActor = actorref;
        }

        private void UpdateLoggingActor(IActorRef actorref)
        {
            _loggingActor = actorref;
        }

        private void UpdateSequencingActor(IActorRef actorref)
        {
            _sequencingActor = actorref;
        }

        private void UpdateSupervisorActor(IActorRef actorref)
        {
            _supervisorActor = actorref;
        }

        private void UpdateCameras(List<ICaptureSource> cameras)
        {
            _cameras = cameras;
        }

        private void UpdateCurrentJob(BatchVideoJob job)
        {
            _currentJob = job;
        }

        private void UpdateVideoJobs(List<BatchVideoJob> jobs)
        {
            _videoJobs = jobs;
        }

        private void ConfigureRegions()
        {
            try
            {
                if (_videoJobs.Count == 0)
                {
                    RegionEditor regionEditor;
                    if(_backgroundFrame != null)
                    {
                        regionEditor = new RegionEditor(_backgroundFrame, "Live", _regionConfigDataAccessLayer);
                    }
                    else if (_currentJob == null)
                    {
                        regionEditor = new RegionEditor(_cameras, _regionConfigDataAccessLayer);
                    }
                    else
                    {
                        regionEditor = new RegionEditor(_cameras, _regionConfigDataAccessLayer, _currentJob.RegionConfiguration);
                    }

                    if (regionEditor.ShowDialog() == DialogResult.OK)
                    {
                        _processingActor.Tell(new UpdateRegionConfigurationMessage(regionEditor.SelectedRegionConfig));
                        _loggingActor.Tell(new UpdateRegionConfigurationMessage(regionEditor.SelectedRegionConfig));
                        if (_currentJob != null)
                        {
                            _currentJob.RegionConfiguration = regionEditor.SelectedRegionConfig;
                        }
                    }
            }
                else
                {
                var captureSourceLookup = _videoJobs.ToDictionary(v => v, v => new VideoFileCapture(v.VideoPath));
                captureSourceLookup.Values.ToList().ForEach(c => c.Init());

                //Reload configs to ensure they're fresh, in case a new RegionConfig has been saved since the ConfigurationActor started.
                _regionConfigs = _regionConfigDataAccessLayer.LoadRegionConfigList();

                var view = new RegionConfigSelectorView(_regionConfigDataAccessLayer);
                var model = new RegionConfigSelectorModel(captureSourceLookup.Values.ToList<ICaptureSource>(), _regionConfigs);
                view.SetModel(model);

                if (view.ShowDialog() == DialogResult.OK)
                {
                    // Get results
                    var results = view.GetRegionConfigSelections();

                    foreach (var job in _videoJobs)
                    {
                        var captureSource = captureSourceLookup[job];
                        job.RegionConfiguration = results[captureSource];
                    }

                    _sequencingActor.Tell(new VideoJobsMessage(_videoJobs));
                    }
                return;
            }
            // Update regionconfigs with any possible changes
            _regionConfigs = _regionConfigDataAccessLayer.LoadRegionConfigList();
            _loggingActor.Tell(new LogUserMessage("Region configuration updated.", LogLevel.Info));
            }
            catch (Exception ex)
            {
                _loggingActor.Tell(new LogMessage("Exception in btnConfigureRegions_Click: " + ex.Message, LogLevel.Error));
#if DEBUG
                MessageBox.Show("Region configuration failed: " + ex.Message);
                throw;
#else
                MessageBox.Show("Region configuration failed");
#endif
            }
        }

        private void Heartbeat()
        {
            _supervisorActor?.Tell(new ActorHeartbeatMessage());
            Context.System.Scheduler.ScheduleTellOnce(5000, Self, new ActorHeartbeatMessage(), Self);
        }

        private void LookupRegionConfig(string regionName, Guid jobGuid)
        {
            foreach (var rc in _regionConfigs)
            {
                if (string.Equals(rc.Title, regionName, StringComparison.CurrentCultureIgnoreCase))
                {
                    var rlrm = new RegionConfigLookupResponseMessage(rc,jobGuid);
                    _sequencingActor.Tell(rlrm);
                    return;
                }
            }
        }

        private void ReceiveNewBackground(Image<Bgr,byte> image)
        { 
            _backgroundFrame = image.Clone();
        }
    }
}
