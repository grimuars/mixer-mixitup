using Mixer.Base.Model.Channel;
using Mixer.Base.Model.MixPlay;
using Mixer.Base.Model.User;
using MixItUp.Base.Actions;
using MixItUp.Base.Commands;
using MixItUp.Base.MixerAPI;
using MixItUp.Base.Model.API;
using MixItUp.Base.Model.Chat.Mixer;
using MixItUp.Base.Services;
using MixItUp.Base.Services.External;
using MixItUp.Base.Services.Mixer;
using MixItUp.Base.Services.Twitch;
using MixItUp.Base.Statistics;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.User;
using StreamingClient.Base.Model.OAuth;
using StreamingClient.Base.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwitchV5API = Twitch.Base.Models.V5;
using TwitchNewAPI = Twitch.Base.Models.NewAPI;

[assembly: InternalsVisibleTo("MixItUp.Desktop")]

namespace MixItUp.Base
{
    public static class ChannelSession
    {
        public const string ClientID = "5e3140d0719f5842a09dd2700befbfc100b5a246e35f2690";

        public const string DefaultOBSStudioConnection = "ws://127.0.0.1:4444";
        public const string DefaultOvrStreamConnection = "ws://127.0.0.1:8023";

        public static SecretManagerService SecretManager { get; internal set; }

        public static MixerConnectionService MixerUserConnection { get; private set; }
        public static MixerConnectionService MixerBotConnection { get; private set; }
        public static PrivatePopulatedUserModel MixerUser { get; private set; }
        public static PrivatePopulatedUserModel MixerBotUser { get; private set; }
        public static ExpandedChannelModel MixerChannel { get; private set; }

        public static TwitchConnectionService TwitchUserConnection { get; private set; }
        public static TwitchConnectionService TwitchBotConnection { get; private set; }
        public static TwitchNewAPI.Users.UserModel TwitchUser { get; set; }
        public static TwitchNewAPI.Users.UserModel TwitchBotUser { get; set; }
        public static TwitchNewAPI.Users.UserModel TwitchNewAPIChannel { get; private set; }
        public static TwitchV5API.Users.UserModel TwitchV5User { get; private set; }
        public static TwitchV5API.Channel.ChannelModel TwitchV5Channel { get; private set; }

        public static IChannelSettings Settings { get; private set; }

        public static MixPlayClientWrapper Interactive { get; private set; }
        public static ConstellationClientWrapper Constellation { get; private set; }
        public static TwitchPubSubService PubSub { get; private set; }

        public static StatisticsTracker Statistics { get; private set; }

        public static ServicesHandlerBase Services { get; private set; }

        public static List<PreMadeChatCommand> PreMadeChatCommands { get; private set; }

        public static LockedDictionary<string, double> Counters { get; private set; }

        private static CancellationTokenSource sessionBackgroundCancellationTokenSource = new CancellationTokenSource();
        private static int sessionBackgroundTimer = 0;

        public static bool IsDebug()
        {
            #if DEBUG
                return true;
            #else
                return false;
            #endif
        }

        public static bool IsElevated { get; set; }

        public static IEnumerable<PermissionsCommandBase> AllEnabledChatCommands
        {
            get
            {
                return ChannelSession.AllChatCommands.Where(c => c.IsEnabled);
            }
        }

        public static IEnumerable<PermissionsCommandBase> AllChatCommands
        {
            get
            {
                List<PermissionsCommandBase> commands = new List<PermissionsCommandBase>();
                commands.AddRange(ChannelSession.PreMadeChatCommands);
                commands.AddRange(ChannelSession.Settings.ChatCommands);
                commands.AddRange(ChannelSession.Settings.GameCommands);
                return commands;
            }
        }

        public static IEnumerable<CommandBase> AllEnabledCommands
        {
            get
            {
                return ChannelSession.AllCommands.Where(c => c.IsEnabled);
            }
        }

        public static IEnumerable<CommandBase> AllCommands
        {
            get
            {
                List<CommandBase> commands = new List<CommandBase>();
                commands.AddRange(ChannelSession.AllChatCommands);
                commands.AddRange(ChannelSession.Settings.EventCommands);
                commands.AddRange(ChannelSession.Settings.MixPlayCommands);
                commands.AddRange(ChannelSession.Settings.TimerCommands);
                commands.AddRange(ChannelSession.Settings.ActionGroupCommands);
                return commands;
            }
        }

        public static IDictionary<string, int> AllOverlayNameAndPorts
        {
            get
            {
                Dictionary<string, int> results = new Dictionary<string, int>(ChannelSession.Settings.OverlayCustomNameAndPorts);
                results.Add(ChannelSession.Services.Overlay.DefaultOverlayName, ChannelSession.Services.Overlay.DefaultOverlayPort);
                return results;
            }
        }

        public static bool IsStreamer
        {
            get
            {
                if (ChannelSession.MixerUser != null && ChannelSession.MixerChannel != null)
                {
                    return ChannelSession.MixerUser.id == ChannelSession.MixerChannel.user.id;
                }
                return false;
            }
        }

        public static void Initialize(ServicesHandlerBase serviceHandler)
        {
            try
            {
                Type mixItUpSecretsType = Type.GetType("MixItUp.Base.MixItUpSecrets");
                if (mixItUpSecretsType != null)
                {
                    ChannelSession.SecretManager = (SecretManagerService)Activator.CreateInstance(mixItUpSecretsType);
                }
            }
            catch (Exception ex) { Logger.Log(ex); }

            if (ChannelSession.SecretManager == null)
            {
                ChannelSession.SecretManager = new SecretManagerService();
            }

            ChannelSession.PreMadeChatCommands = new List<PreMadeChatCommand>();

            ChannelSession.Counters = new LockedDictionary<string, double>();

            ChannelSession.Services = serviceHandler;

            ChannelSession.Constellation = new ConstellationClientWrapper();
            ChannelSession.Interactive = new MixPlayClientWrapper();
            ChannelSession.PubSub = new TwitchPubSubService();

            ChannelSession.Statistics = new StatisticsTracker();
        }

        public static async Task<bool> ConnectStreamer()
        {
            ChannelSession.MixerUserConnection = new MixerConnectionService();
            ChannelSession.TwitchUserConnection = new TwitchConnectionService();
            if (await ChannelSession.MixerUserConnection.ConnectAsStreamer() && await ChannelSession.TwitchUserConnection.ConnectAsStreamer())
            {
                return await ChannelSession.InitializeInternal(isStreamer: true);
            }
            return false;
        }

        public static async Task<bool> Connect(IChannelSettings settings)
        {
            ChannelSession.Settings = settings;
            ChannelSession.MixerUserConnection = new MixerConnectionService();
            ChannelSession.TwitchUserConnection = new TwitchConnectionService();
            if (await ChannelSession.MixerUserConnection.Connect(ChannelSession.Settings.MixerUserOAuthToken) && await ChannelSession.TwitchUserConnection.Connect(ChannelSession.Settings.TwitchUserOAuthToken))
            {
                return await ChannelSession.InitializeInternal(ChannelSession.Settings.IsStreamer);
            }
            else
            {
                if (ChannelSession.Settings.IsStreamer)
                {
                    return await ChannelSession.ConnectStreamer();
                }
                else
                {
                    return await ChannelSession.ConnectModerator(ChannelSession.Settings.MixerChannel?.token, ChannelSession.Settings.TwitchChannel?.login);
                }
            }
        }

        public static async Task<bool> ConnectModerator(string mixerChannelName = null, string twitchChannelName = null)
        {
            ChannelSession.MixerUserConnection = new MixerConnectionService();
            ChannelSession.TwitchUserConnection = new TwitchConnectionService();
            if (await ChannelSession.MixerUserConnection.ConnectAsModerator() && await ChannelSession.TwitchUserConnection.ConnectAsModerator())
            {
                return await ChannelSession.InitializeInternal(isStreamer: false, mixerChannelName: mixerChannelName, twitchChannelName: twitchChannelName);
            }
            return false;
        }

        public static async Task<bool> ConnectBot()
        {
            ChannelSession.MixerUserConnection = new MixerConnectionService();
            ChannelSession.TwitchUserConnection = new TwitchConnectionService();
            if (await ChannelSession.MixerUserConnection.ConnectAsBot() && await ChannelSession.TwitchUserConnection.ConnectAsBot())
            {
                return await ChannelSession.InitializeBotInternal();
            }
            return false;
        }

        public static async Task<bool> ConnectBot(IChannelSettings settings)
        {
            if (settings.MixerBotOAuthToken != null)
            {
                ChannelSession.MixerUserConnection = new MixerConnectionService();
                ChannelSession.TwitchUserConnection = new TwitchConnectionService();
                if (await ChannelSession.MixerUserConnection.Connect(settings.MixerBotOAuthToken) && await ChannelSession.TwitchUserConnection.Connect(settings.TwitchBotOAuthToken))
                {
                    return await ChannelSession.InitializeBotInternal();
                }
                else
                {
                    settings.MixerBotOAuthToken = null;
                    settings.TwitchBotOAuthToken = null;
                    return await ChannelSession.ConnectBot();
                }
            }
            return false;
        }

        public static async Task DisconnectBot()
        {
            ChannelSession.MixerBotConnection = null;
            await ChannelSession.Services.Chat.MixerChatService.DisconnectBot();
        }

        public static async Task Close()
        {
            await ChannelSession.Services.Close();

            await ChannelSession.Services.Chat.MixerChatService.DisconnectUser();
            await ChannelSession.Services.Chat.TwitchChatService.DisconnectUser();
            await ChannelSession.DisconnectBot();

            await ChannelSession.Constellation.Disconnect();
            await ChannelSession.PubSub.Disconnect();
        }

        public static async Task SaveSettings()
        {
            await ChannelSession.Services.Settings.Save(ChannelSession.Settings);
        }

        public static async Task RefreshUser()
        {
            PrivatePopulatedUserModel mixerUser = await ChannelSession.MixerUserConnection.GetCurrentUser();
            if (mixerUser != null)
            {
                ChannelSession.MixerUser = mixerUser;
            }

            TwitchNewAPI.Users.UserModel twitchUser = await ChannelSession.TwitchUserConnection.GetNewAPICurrentUser();
            if (twitchUser != null)
            {
                ChannelSession.TwitchUser = twitchUser;
            }
        }

        public static async Task RefreshChannel()
        {
            if (ChannelSession.MixerUserConnection != null)
            {
                ExpandedChannelModel mixerChannel = await ChannelSession.MixerUserConnection.GetChannel(ChannelSession.MixerChannel.id);
                if (mixerChannel != null)
                {
                    ChannelSession.MixerChannel = mixerChannel;
                }
            }

            if (ChannelSession.TwitchUserConnection != null)
            {
                TwitchNewAPI.Users.UserModel newChannel = await ChannelSession.TwitchUserConnection.GetNewAPIUserByID(ChannelSession.TwitchNewAPIChannel?.id);
                if (newChannel != null)
                {
                    ChannelSession.TwitchNewAPIChannel = newChannel;
                    TwitchV5API.Channel.ChannelModel channel = await ChannelSession.TwitchUserConnection.GetV5APIChannel(ChannelSession.TwitchNewAPIChannel.id);
                    if (channel != null)
                    {
                        ChannelSession.TwitchV5Channel = channel;
                    }
                }
            }
        }

        public static UserViewModel GetCurrentUser()
        {
            UserViewModel user = ChannelSession.Services.User.GetUserByID(ChannelSession.MixerUser.id);
            if (user == null)
            {
                user = new UserViewModel(ChannelSession.MixerUser);
            }
            return user;
        }

        public static void DisconnectionOccurred(string serviceName)
        {
            Logger.Log(serviceName + " Service disconnection occurred");
            GlobalEvents.ServiceDisconnect(serviceName);
        }

        public static void ReconnectionOccurred(string serviceName)
        {
            Logger.Log(serviceName + " Service reconnection successful");
            GlobalEvents.ServiceReconnect(serviceName);
        }

        private static async Task<bool> InitializeInternal(bool isStreamer, string mixerChannelName = null, string twitchChannelName = null)
        {
            await ChannelSession.Services.InitializeTelemetryService();

            await ChannelSession.RefreshUser();
            if (ChannelSession.MixerUser != null)
            {
                if (mixerChannelName == null || isStreamer)
                {
                    ChannelSession.MixerChannel = await ChannelSession.MixerUserConnection.GetChannel(ChannelSession.MixerUser.channel.id);
                }
                else
                {
                    ChannelSession.MixerChannel = await ChannelSession.MixerUserConnection.GetChannel(mixerChannelName);
                }

                if (twitchChannelName == null || isStreamer)
                {
                    ChannelSession.TwitchNewAPIChannel = await ChannelSession.TwitchUserConnection.GetNewAPICurrentUser();
                }
                else
                {
                    ChannelSession.TwitchNewAPIChannel = await ChannelSession.TwitchUserConnection.GetNewAPIUserByLogin(twitchChannelName);
                }
                
                if (ChannelSession.MixerChannel != null)
                {
                    if (ChannelSession.Settings == null)
                    {
                        ChannelSession.Settings = await ChannelSession.Services.Settings.Create(ChannelSession.MixerChannel, isStreamer);
                    }
                    await ChannelSession.Services.Settings.Initialize(ChannelSession.Settings);

                    if (ChannelSession.Settings.DiagnosticLogging)
                    {
                        Logger.SetLogLevel(LogLevel.Debug);
                    }

                    ChannelSession.Settings.LicenseAccepted = true;

                    if (isStreamer && ChannelSession.Settings.MixerChannel != null && ChannelSession.MixerUser.id != ChannelSession.Settings.MixerChannel.userId)
                    {
                        GlobalEvents.ShowMessageBox("The account you are logged in as on Mixer does not match the account for this settings. Please log in as the correct account on Mixer.");
                        ChannelSession.Settings.MixerUserOAuthToken.accessToken = string.Empty;
                        ChannelSession.Settings.MixerUserOAuthToken.refreshToken = string.Empty;
                        ChannelSession.Settings.MixerUserOAuthToken.expiresIn = 0;
                        return false;
                    }

                    ChannelSession.Settings.TwitchChannel = ChannelSession.TwitchNewAPIChannel;

                    ChannelSession.Services.Telemetry.SetUserId(ChannelSession.Settings.TelemetryUserId);

                    await MixerChatEmoteModel.InitializeEmoteCache();

                    if (ChannelSession.IsStreamer)
                    {
                        ChannelSession.PreMadeChatCommands.Clear();
                        foreach (PreMadeChatCommand command in ReflectionHelper.CreateInstancesOfImplementingType<PreMadeChatCommand>())
                        {
#pragma warning disable CS0612 // Type or member is obsolete
                            if (!(command is ObsoletePreMadeCommand))
                            {
                                ChannelSession.PreMadeChatCommands.Add(command);
                            }
#pragma warning restore CS0612 // Type or member is obsolete
                        }

                        foreach (PreMadeChatCommandSettings commandSetting in ChannelSession.Settings.PreMadeChatCommandSettings)
                        {
                            PreMadeChatCommand command = ChannelSession.PreMadeChatCommands.FirstOrDefault(c => c.Name.Equals(commandSetting.Name));
                            if (command != null)
                            {
                                command.UpdateFromSettings(commandSetting);
                            }
                        }
                    }

                    MixerChatService mixerChatService = new MixerChatService();
                    TwitchChatService twitchChatService = new TwitchChatService();

                    if (!await mixerChatService.ConnectUser() || !await ChannelSession.Constellation.Connect() || !await twitchChatService.ConnectUser() || !await ChannelSession.PubSub.Connect())
                    {
                        return false;
                    }

                    await ChannelSession.Services.Chat.Initialize(mixerChatService, twitchChatService);

                    // Connect External Services
                    Dictionary<IExternalService, OAuthTokenModel> externalServiceToConnect = new Dictionary<IExternalService, OAuthTokenModel>();
                    if (ChannelSession.Settings.StreamlabsOAuthToken != null) { externalServiceToConnect[ChannelSession.Services.Streamlabs] = ChannelSession.Settings.StreamlabsOAuthToken; }
                    if (ChannelSession.Settings.StreamJarOAuthToken != null) { externalServiceToConnect[ChannelSession.Services.StreamJar] = ChannelSession.Settings.StreamJarOAuthToken; }
                    if (ChannelSession.Settings.TipeeeStreamOAuthToken != null) { externalServiceToConnect[ChannelSession.Services.TipeeeStream] = ChannelSession.Settings.TipeeeStreamOAuthToken; }
                    if (ChannelSession.Settings.TreatStreamOAuthToken != null) { externalServiceToConnect[ChannelSession.Services.TreatStream] = ChannelSession.Settings.TreatStreamOAuthToken; }
                    if (ChannelSession.Settings.StreamlootsOAuthToken != null) { externalServiceToConnect[ChannelSession.Services.Streamloots] = ChannelSession.Settings.StreamlootsOAuthToken; }
                    if (ChannelSession.Settings.TiltifyOAuthToken != null) { externalServiceToConnect[ChannelSession.Services.Tiltify] = ChannelSession.Settings.TiltifyOAuthToken; }
                    if (ChannelSession.Settings.JustGivingOAuthToken != null) { externalServiceToConnect[ChannelSession.Services.JustGiving] = ChannelSession.Settings.JustGivingOAuthToken; }
                    if (ChannelSession.Settings.IFTTTOAuthToken != null) { externalServiceToConnect[ChannelSession.Services.IFTTT] = ChannelSession.Settings.IFTTTOAuthToken; }
                    if (ChannelSession.Settings.ExtraLifeTeamID > 0) { externalServiceToConnect[ChannelSession.Services.ExtraLife] = new OAuthTokenModel(); }
                    if (ChannelSession.Settings.PatreonOAuthToken != null) { externalServiceToConnect[ChannelSession.Services.Patreon] = ChannelSession.Settings.PatreonOAuthToken; }
                    if (ChannelSession.Settings.DiscordOAuthToken != null) { externalServiceToConnect[ChannelSession.Services.Discord] = ChannelSession.Settings.DiscordOAuthToken; }
                    if (ChannelSession.Settings.TwitterOAuthToken != null) { externalServiceToConnect[ChannelSession.Services.Twitter] = ChannelSession.Settings.TwitterOAuthToken; }
                    if (!string.IsNullOrEmpty(ChannelSession.Settings.OBSStudioServerIP)) { externalServiceToConnect[ChannelSession.Services.OBSStudio] = null; }
                    if (ChannelSession.Settings.EnableStreamlabsOBSConnection) { externalServiceToConnect[ChannelSession.Services.StreamlabsOBS] = null; }
                    if (ChannelSession.Settings.EnableXSplitConnection) { externalServiceToConnect[ChannelSession.Services.XSplit] = null; }
                    if (!string.IsNullOrEmpty(ChannelSession.Settings.OvrStreamServerIP)) { externalServiceToConnect[ChannelSession.Services.OvrStream] = null; }
                    if (ChannelSession.Settings.EnableOverlay) { externalServiceToConnect[ChannelSession.Services.Overlay] = null; }
                    if (ChannelSession.Settings.EnableDeveloperAPI) { externalServiceToConnect[ChannelSession.Services.DeveloperAPI] = null; }

                    if (externalServiceToConnect.Count > 0)
                    {
                        Dictionary<IExternalService, Task<ExternalServiceResult>> externalServiceTasks = new Dictionary<IExternalService, Task<ExternalServiceResult>>();
                        foreach (var kvp in externalServiceToConnect)
                        {
                            if (kvp.Key is IOAuthExternalService && kvp.Value != null)
                            {
                                externalServiceTasks[kvp.Key] = ((IOAuthExternalService)kvp.Key).Connect(kvp.Value);
                            }
                            else
                            {
                                externalServiceTasks[kvp.Key] = kvp.Key.Connect();
                            }
                        }
                        await Task.WhenAll(externalServiceTasks.Values);

                        List<IExternalService> failedServices = new List<IExternalService>();
                        foreach (var kvp in externalServiceTasks)
                        {
                            if (!kvp.Value.Result.Success)
                            {
                                ExternalServiceResult result = await kvp.Key.Connect();
                                if (!result.Success)
                                {
                                    failedServices.Add(kvp.Key);
                                }
                            }
                        }

                        if (failedServices.Count > 0)
                        {
                            StringBuilder message = new StringBuilder();
                            message.AppendLine("The following services could not be connected:");
                            message.AppendLine();
                            foreach (IExternalService service in failedServices)
                            {
                                message.AppendLine(" - " + service.Name);
                            }
                            message.AppendLine();
                            message.Append("Please go to the Services page to reconnect them manually.");
                            await DialogHelper.ShowMessage(message.ToString());
                        }
                    }

                    if (ChannelSession.Settings.RemoteHostConnection != null)
                    {
                        await ChannelSession.Services.RemoteService.InitializeConnection(ChannelSession.Settings.RemoteHostConnection);
                    }

                    foreach (CommandBase command in ChannelSession.AllEnabledCommands)
                    {
                        foreach (ActionBase action in command.Actions)
                        {
                            if (action is CounterAction)
                            {
                                await ((CounterAction)action).SetCounterValue();
                            }
                        }
                    }

                    if (ChannelSession.Settings.DefaultMixPlayGame > 0)
                    {
                        IEnumerable<MixPlayGameListingModel> games = await ChannelSession.MixerUserConnection.GetOwnedMixPlayGames(ChannelSession.MixerChannel);
                        MixPlayGameListingModel game = games.FirstOrDefault(g => g.id.Equals(ChannelSession.Settings.DefaultMixPlayGame));
                        if (game != null)
                        {
                            if (await ChannelSession.Interactive.Connect(game) != MixPlayConnectionResult.Success)
                            {
                                await ChannelSession.Interactive.Disconnect();
                            }
                        }
                        else
                        {
                            ChannelSession.Settings.DefaultMixPlayGame = 0;
                        }
                    }

                    foreach (UserCurrencyViewModel currency in ChannelSession.Settings.Currencies.Values)
                    {
                        if (currency.ShouldBeReset())
                        {
                            await currency.Reset();
                        }
                    }

                    if (ChannelSession.Settings.ModerationResetStrikesOnLaunch)
                    {
                        foreach (UserDataViewModel userData in ChannelSession.Settings.UserData.Values.Where(u => u.ModerationStrikes > 0))
                        {
                            userData.ModerationStrikes = 0;
                            ChannelSession.Settings.UserData.ManualValueChanged(userData.ID);
                        }
                    }

                    ChannelSession.Services.TimerService.Initialize();

                    ChannelSession.Services.InputService.HotKeyPressed += InputService_HotKeyPressed;

                    await ChannelSession.SaveSettings();

                    await ChannelSession.Services.Settings.SaveBackup(ChannelSession.Settings);

                    await ChannelSession.Services.Settings.PerformBackupIfApplicable(ChannelSession.Settings);

                    ChannelSession.Services.Telemetry.TrackLogin(ChannelSession.MixerUser.id.ToString(), ChannelSession.IsStreamer, ChannelSession.MixerChannel.partnered);
                    if (ChannelSession.Settings.IsStreamer)
                    {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        Task.Run(async () => { await ChannelSession.Services.MixItUpService.SendUserFeatureEvent(new UserFeatureEvent(ChannelSession.MixerUser.id)); });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    }

                    GlobalEvents.OnRankChanged += GlobalEvents_OnRankChanged;

                    AsyncRunner.RunBackgroundTask(sessionBackgroundCancellationTokenSource.Token, 60000, SessionBackgroundTask);

                    return true;
                }
            }
            return false;
        }

        private static async Task<bool> InitializeBotInternal()
        {
            PrivatePopulatedUserModel user = await ChannelSession.MixerBotConnection.GetCurrentUser();
            if (user != null)
            {
                ChannelSession.MixerBotUser = user;

                await ChannelSession.Services.Chat.MixerChatService.ConnectBot();

                await ChannelSession.SaveSettings();

                return true;
            }
            return false;
        }

        private static async Task SessionBackgroundTask(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                sessionBackgroundTimer++;

                await ChannelSession.RefreshUser();

                await ChannelSession.RefreshChannel();

                if (sessionBackgroundTimer >= 5)
                {
                    await ChannelSession.SaveSettings();
                    sessionBackgroundTimer = 0;
                }
            }
        }

        private static async void GlobalEvents_OnRankChanged(object sender, UserCurrencyDataViewModel currency)
        {
            if (currency.Currency.RankChangedCommand != null)
            {
                UserViewModel user = ChannelSession.Services.User.GetUserByID(currency.User.ID);
                if (user != null)
                {
                    await currency.Currency.RankChangedCommand.Perform(user);
                }
            }
        }

        private static async void InputService_HotKeyPressed(object sender, HotKey hotKey)
        {
            if (ChannelSession.Settings.HotKeys.ContainsKey(hotKey.ToString()))
            {
                HotKeyConfiguration hotKeyConfiguration = ChannelSession.Settings.HotKeys[hotKey.ToString()];
                CommandBase command = ChannelSession.AllCommands.FirstOrDefault(c => c.ID.Equals(hotKeyConfiguration.CommandID));
                if (command != null)
                {
                    await command.Perform();
                }
            }
        }
    }
}