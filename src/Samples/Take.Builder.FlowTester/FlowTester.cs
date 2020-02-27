using Lime.Protocol.Serialization;
using NSubstitute;
using Serilog;
using SimpleInjector;
using System.IO;
using System.Reflection;
using Take.Blip.Builder;
using Take.Blip.Builder.Actions;
using Take.Blip.Builder.Diagnostics;
using Take.Blip.Builder.Hosting;
using Take.Blip.Builder.Storage;
using Take.Blip.Builder.Utils;
using Take.Blip.Client;
using Take.Blip.Client.Activation;
using Take.Blip.Client.Extensions.ArtificialIntelligence;
using Take.Blip.Client.Extensions.Broadcast;
using Take.Blip.Client.Extensions.Bucket;
using Take.Blip.Client.Extensions.Contacts;
using Take.Blip.Client.Extensions.EventTracker;
using Take.Blip.Client.Extensions.HelpDesk;
using Take.Blip.Client.Extensions.Tunnel;
using Take.Blip.Builder.Actions;
using Take.Blip.Builder.Actions.CreateTicket;
using Take.Blip.Builder.Actions.DeleteVariable;
using Take.Blip.Builder.Actions.ExecuteScript;
using Take.Blip.Builder.Actions.ForwardMessageToDesk;
using Take.Blip.Builder.Actions.ManageList;
using Take.Blip.Builder.Actions.MergeContact;
using Take.Blip.Builder.Actions.ProcessCommand;
using Take.Blip.Builder.Actions.ProcessHttp;
using Take.Blip.Builder.Actions.Redirect;
using Take.Blip.Builder.Actions.SendCommand;
using Take.Blip.Builder.Actions.SendMessage;
using Take.Blip.Builder.Actions.SendMessageFromHttp;
using Take.Blip.Builder.Actions.SendRawMessage;
using Take.Blip.Builder.Actions.SetBucket;
using Take.Blip.Builder.Actions.SetVariable;
using Take.Blip.Builder.Actions.TrackEvent;
using Take.Blip.Builder.Diagnostics;
using Take.Blip.Builder.Hosting;
using Take.Blip.Builder.Storage;
using Take.Blip.Builder.Utils;
using Take.Blip.Builder.Variables;
using Builder.Flow.Tests.Interface;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json.Linq;
using Lime.Protocol;
using Lime.Messaging.Contents;
using Take.Blip.Builder.Models;
using Lime.Messaging;
using Take.Blip.Client.Extensions;
using Lime.Protocol.Serialization.Newtonsoft;
using System.Net.Http;
using System.Collections.Generic;
using System;
using Take.Blip.Client.Extensions.Context;

namespace Take.Builder.FlowTester
{




    public class FlowTester
    {
        private readonly IConfiguration _configuration;
        private readonly IStateManager _stateManager;
        private readonly IContextProvider _contextProvider;
        private readonly INamedSemaphore _namedSemaphore;
        private IActionProvider _actionProvider;
        private readonly ISender _sender;
        private readonly IDocumentSerializer _documentSerializer;
        private readonly IEnvelopeSerializer _envelopeSerializer;
        private readonly IArtificialIntelligenceExtension _artificialIntelligenceExtension;
        private readonly IVariableReplacer _variableReplacer;
        private readonly ILogger _logger;
        private readonly ITraceManager _traceManager;
        private IUserOwnerResolver _userOwnerResolver;
        private readonly IEventTrackExtension _trackExtension;
        private readonly IBroadcastExtension _broadcastExtension;
        private readonly IContactExtension _contactExtension;
        private readonly IBucketExtension _bucketExtension;
        private readonly IHelpDeskExtension _helpDeskExtension;
        private readonly Application _application;
        private readonly Container _container;
        private readonly ITunnelExtension _tunnelExtension;
        private readonly FlowManager _flowManager;
        private IContext _context;

        private IContext GetContext(Identity userIdentity, Identity ownerIdentity, LazyInput lazyInput, Flow flow)
        {
            if (_context == null)
            {
                _context = _contextProvider.CreateContext(userIdentity, ownerIdentity, lazyInput, flow);
            }
            return _context;

        }

        public FlowTester(
            string jsonApplication,
            ISender sender = null,
            IEventTrackExtension eventTrackExtension = null,
            IBroadcastExtension broadcastExtension = null,
            IContactExtension contactExtension = null,
            IBucketExtension bucketExtension = null,
            IHelpDeskExtension helpDeskExtension = null,
            IArtificialIntelligenceExtension artificialIntelligenceExtension = null
            )
        {
            _container = new Container();
            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            _application = Application.ParseFromJson(jsonApplication);
            _configuration = new ConventionsConfiguration();
            if (sender == null)
            {
                _sender = Substitute.For<ISender>();
            }
            else _sender = sender;
            _container.RegisterSingleton(_sender);
            var httpClient = new HttpClientWrapper();//Substitute.For<IHttpClient>();
            _documentSerializer = new DocumentSerializer(new DocumentTypeResolver().WithMessagingDocuments());//Substitute.For<IDocumentSerializer>();
            _envelopeSerializer = new EnvelopeSerializer(new DocumentTypeResolver().WithBlipDocuments());//Substitute.For<IEnvelopeSerializer>();
            if (eventTrackExtension == null)
            {
                _trackExtension = Substitute.For<IEventTrackExtension>();
            }
            else _trackExtension = eventTrackExtension;
            if (broadcastExtension == null)
            {
                _broadcastExtension = Substitute.For<IBroadcastExtension>();
            }
            else _broadcastExtension = broadcastExtension;

            if (contactExtension == null)
            {
                _contactExtension = Substitute.For<IContactExtension>();
            }
            else _contactExtension = contactExtension;

            if (_bucketExtension == null)
            {
                _bucketExtension = Substitute.For<IBucketExtension>();
            }
            else _bucketExtension = bucketExtension;

            if (helpDeskExtension == null)
            {
                _helpDeskExtension = Substitute.For<IHelpDeskExtension>();
            }
            else _helpDeskExtension = helpDeskExtension;
            _container.RegisterSingleton(_documentSerializer);
            _container.RegisterSingleton(_envelopeSerializer);
            _container.RegisterSingleton(_trackExtension);
            _container.RegisterSingleton(_broadcastExtension);
            _container.RegisterSingleton(_contactExtension);
            _container.RegisterSingleton(_bucketExtension);
            _container.RegisterSingleton(_helpDeskExtension);
            var actions = new IAction[]
            {
                    new ExecuteScriptAction(_configuration),
                    new SendMessageAction(_sender),
                    new SendMessageFromHttpAction(_sender,httpClient,_documentSerializer),
                    new SendRawMessageAction(_sender,_documentSerializer),
                    new SendCommandAction(_sender),
                    new ProcessCommandAction(_sender, _envelopeSerializer),
                    new TrackEventAction(_trackExtension),
                    new ProcessHttpAction(httpClient,_logger),
                    new ManageListAction(_broadcastExtension),
                    new MergeContactAction(_contactExtension),
                    new SetVariableAction(),
                    new SetBucketAction(_bucketExtension),
                    new RedirectAction(_sender),
                    new ForwardMessageToDeskAction(_sender),
                    new CreateTicketAction(_helpDeskExtension, _application),
                    new DeleteVariableAction(),
            };

            _container.RegisterCollection<IVariableProvider>(
                new[]
                {
                    typeof(ApplicationVariableProvider),
                    typeof(BucketVariableProvider),
                    typeof(CalendarVariableProvider),
                    typeof(ConfigurationVariableProvider),
                    typeof(ContactVariableProvider),
                    typeof(InputVariableProvider),
                    typeof(RandomVariableProvider),
                    typeof(StateVariableProvider),
                    typeof(TunnelVariableProvider),
                    typeof(TicketVariableProvider),
                    typeof(ResourceVariableProvider),
                });

            _stateManager = new MemoryStateManager();
            _container.RegisterSingleton(_stateManager);
            _tunnelExtension = new TunnelExtension(_sender);
            _container.RegisterSingleton(_tunnelExtension);
            var valuesDictionary = new Dictionary<string, Document>(StringComparer.InvariantCultureIgnoreCase);
            var contextExtension = new DictionaryContextExtension(valuesDictionary);
            _container.RegisterSingleton<IContextExtension>(contextExtension);
            _contextProvider = new ContextProvider(_container);
            _namedSemaphore = Substitute.For<INamedSemaphore>();
            _actionProvider = new ActionProvider(actions);
            _container.RegisterSingleton(_actionProvider);

            if (artificialIntelligenceExtension == null)
            {
                _artificialIntelligenceExtension = Substitute.For<IArtificialIntelligenceExtension>();
            }
            else _artificialIntelligenceExtension = artificialIntelligenceExtension;
            _container.RegisterSingleton(_artificialIntelligenceExtension);
            _variableReplacer = new VariableReplacer();
            _container.RegisterSingleton(_variableReplacer);
            _logger = Substitute.For<ILogger>();
            _container.RegisterSingleton(_logger);
            _traceManager = Substitute.For<ITraceManager>();
            _container.RegisterSingleton(_traceManager);
            _userOwnerResolver = new UserOwnerResolver(_tunnelExtension, _application);
            _flowManager = new FlowManager(_configuration, _stateManager, _contextProvider,
                _namedSemaphore, _actionProvider, _sender, _documentSerializer, _envelopeSerializer,
                _artificialIntelligenceExtension, _variableReplacer, _logger, _traceManager, _userOwnerResolver, _application);

        }

        public async Task<string> TestInputAsync(Message input)
        {

            var flowJson = (JObject)_application.Settings["flow"];
            var flow = flowJson.ToObject<Take.Blip.Builder.Models.Flow>();
            var cancellationToken = new CancellationTokenSource(int.MaxValue);

            await _flowManager.ProcessInputAsync(input, flow, cancellationToken.Token);
            var (userIdentity, ownerIdentity) = await _userOwnerResolver.GetUserOwnerIdentitiesAsync(input, flow.BuilderConfiguration, default);
            var lazyInput = new LazyInput(input, userIdentity, flow.BuilderConfiguration, _documentSerializer,
    _envelopeSerializer, _artificialIntelligenceExtension, default);
            var context = GetContext(userIdentity, ownerIdentity, lazyInput, flow);
            return await _stateManager.GetStateIdAsync(context, default);
        }
    }
}
