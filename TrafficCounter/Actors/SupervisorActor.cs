using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka;
using Akka.Actor;
using VTC.Common;
using VTC.Messages;
using VTC.UserConfiguration;

namespace VTC.Actors
{
    class SupervisorActor : ReceiveActor
    {
        private IActorRef _frameGrabActor;
        private IActorRef _loggingActor;
        private IActorRef _configurationActor;
        private IActorRef _processingActor;
        private IActorRef _sequencingActor;

        public delegate void UpdateActorStatusDelegate(Dictionary<string, DateTime> Statuses);
        private UpdateActorStatusDelegate _updateActorStatusDelegate;

        private Dictionary<string, DateTime> _actorStatuses = new Dictionary<string, DateTime>();

        private VTC.Common.UserConfig _userConfig = new UserConfig();

        public SupervisorActor()
        {
            Receive<FrameGrabActorMessage>(message =>
                UpdateFrameGrabActor(message.ActorRef)
            );

            Receive<LoggingActorMessage>(message =>
                UpdateLoggingActor(message.ActorRef)
            );

            Receive<NewProcessingActorMessage>(message =>
                HandleNewProcessingActor(message.ActorRef)
            );

            Receive<SequencingActorMessage>(message =>
                UpdateSequencingActor(message.ActorRef)
            );

            Receive<ConfigurationActorMessage>(message =>
                UpdateConfigurationActor(message.ActorRef)
            );

            Receive<CreateAllActorsMessage>(message =>
                CreateAllActors(message.UpdateUiDelegate, message.UpdateStatsUiDelegate, message.UpdateInfoUiDelegate, message.UpdateFrameGrabUiDelegate, message.UpdateDebugDelegate)
            );

            Receive<UpdateActorStatusHandlerMessage>(message =>
                HandleNewUpdateActorStatusDelegate(message.UpdateStatusDelegate)
            );

            Receive<ActorHeartbeatMessage>(message =>
                HandleActorHeartbeatMessage()
            );

            Receive<LoadUserConfigMessage>(message => 
                LoadUserConfig()
            );

            Self.Tell(new LoadUserConfigMessage());
        }

        void UpdateFrameGrabActor(IActorRef actor)
        {
            _frameGrabActor = actor;
        }

        void UpdateLoggingActor(IActorRef actor)
        {
            _loggingActor = actor;
        }

        void HandleNewProcessingActor(IActorRef actor)
        {
            _processingActor = actor;
        }

        void UpdateSequencingActor(IActorRef actor)
        {
            _sequencingActor = actor;
        }

        void UpdateConfigurationActor(IActorRef actor)
        {
            _configurationActor = actor;
        }

        void HandleNewUpdateActorStatusDelegate(UpdateActorStatusDelegate updateDelegate)
        {
            _updateActorStatusDelegate = updateDelegate;
        }

        void HandleActorHeartbeatMessage()
        {
            if (!_actorStatuses.ContainsKey(Sender.Path.Name))
            {
                _actorStatuses.Add(Sender.Path.Name, DateTime.Now);
                UpdateActorStatusIndicators();
            }
        }

        void IntroduceAllActors()
        {
            //Introduce actors to each other
            _frameGrabActor.Tell(new NewProcessingActorMessage(_processingActor));
            _processingActor.Tell(new LoggingActorMessage(_loggingActor));
            _frameGrabActor.Tell(new LoggingActorMessage(_loggingActor));
            _frameGrabActor.Tell(new SequencingActorMessage(_sequencingActor));
            _sequencingActor.Tell(new LoggingActorMessage(_loggingActor));
            _sequencingActor.Tell(new NewProcessingActorMessage(_processingActor));
            _sequencingActor.Tell(new FrameGrabActorMessage(_frameGrabActor));
            _sequencingActor.Tell(new ConfigurationActorMessage(_configurationActor));
            _configurationActor.Tell(new NewProcessingActorMessage(_processingActor));
            _configurationActor.Tell(new FrameGrabActorMessage(_frameGrabActor));
            _configurationActor.Tell(new SequencingActorMessage(_sequencingActor));
            _configurationActor.Tell(new LoggingActorMessage(_loggingActor));
            _configurationActor.Tell(new SupervisorActorMessage(Self));
            _loggingActor.Tell(new SequencingActorMessage(_sequencingActor));
        }

        void CreateAllActors(ProcessingActor.UpdateUIDelegate updateUiDelegate, LoggingActor.UpdateStatsUIDelegate statsUiDelegate, LoggingActor.UpdateInfoUIDelegate infoUiDelegate, FrameGrabActor.UpdateUIDelegate frameGrabUiDelegate, LoggingActor.UpdateDebugDelegate debugDelegate)
        {
            _processingActor = Context.ActorOf(Props.Create(typeof(ProcessingActor)).WithMailbox("processing-bounded-mailbox"), "ProcessingActor");
            _processingActor.Tell(new UpdateUiHandlerMessage(updateUiDelegate));
            _frameGrabActor = Context.ActorOf<FrameGrabActor>("FrameGrabActor");
            _frameGrabActor.Tell(new UpdateUiAccessoryHandlerMessage(frameGrabUiDelegate));
            _loggingActor = Context.ActorOf<LoggingActor>("LoggingActor");
            _loggingActor.Tell(new UpdateStatsUiHandlerMessage(statsUiDelegate));
            _loggingActor.Tell(new UpdateInfoUiHandlerMessage(infoUiDelegate));
            _loggingActor.Tell(new UpdateDebugHandlerMessage(debugDelegate));
            _sequencingActor = Context.ActorOf<SequencingActor>("SequencingActor");
            _configurationActor = Context.System.ActorOf(Props.Create(() => new
                ConfigurationActor()).WithDispatcher("synchronized-dispatcher"), "ConfigurationActor");

            IntroduceAllActors();
        }

        void UpdateActorStatusIndicators()
        {
            _updateActorStatusDelegate?.Invoke(_actorStatuses);
        }

        private void LoadUserConfig()
        {
            string UserConfigSavePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                                        "\\VTC\\userConfig.xml";
            IUserConfigDataAccessLayer _userConfigDataAccessLayer = new FileUserConfigDal(UserConfigSavePath);

            _userConfig = _userConfigDataAccessLayer.LoadUserConfig();
        }

    }
}
