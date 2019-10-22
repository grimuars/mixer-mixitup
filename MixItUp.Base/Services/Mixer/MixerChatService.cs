using Mixer.Base.Clients;
using Mixer.Base.Model.Chat;
using Mixer.Base.Model.User;
using MixItUp.Base.Commands;
using MixItUp.Base.Model.Chat;
using MixItUp.Base.Model.User;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.Chat;
using MixItUp.Base.ViewModel.Chat.Mixer;
using MixItUp.Base.ViewModel.User;
using StreamingClient.Base.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace MixItUp.Base.Services.Mixer
{
    public interface IMixerChatService
    {
        event EventHandler<MixerChatMessageViewModel> OnMessageOccurred;
        event EventHandler<MixerSkillChatMessageViewModel> OnSkillOccurred;
        event EventHandler<Tuple<Guid, UserViewModel>> OnDeleteMessageOccurred;
        event EventHandler OnClearMessagesOccurred;

        event EventHandler<IEnumerable<UserViewModel>> OnUsersJoinOccurred;
        event EventHandler<UserViewModel> OnUserUpdateOccurred;
        event EventHandler<IEnumerable<UserViewModel>> OnUsersLeaveOccurred;
        event EventHandler<Tuple<UserViewModel, UserViewModel>> OnUserPurgeOccurred;
        event EventHandler<UserViewModel> OnUserBanOccurred;

        event EventHandler<ChatPollEventModel> OnPollEndOccurred;

        bool IsBotConnected { get; }

        Task<bool> ConnectUser();
        Task DisconnectUser();

        Task<bool> ConnectBot();
        Task DisconnectBot();

        Task Initialize();

        Task SendMessage(string message, bool sendAsStreamer = false);
        Task Whisper(string username, string message, bool sendAsStreamer = false);
        Task<ChatMessageEventModel> WhisperWithResponse(string username, string message, bool sendAsStreamer = false);

        Task<IEnumerable<ChatMessageEventModel>> GetChatHistory(uint maxMessages);
        Task DeleteMessage(ChatMessageViewModel message);
        Task ClearMessages();

        Task PurgeUser(string username);
        Task TimeoutUser(string username, uint durationInSeconds);

        Task BanUser(UserViewModel user);
        Task UnbanUser(UserViewModel user);
        Task ModUser(UserViewModel user);
        Task UnmodUser(UserViewModel user);

        Task StartPoll(string question, IEnumerable<string> answers, uint lengthInSeconds);
    }

    public class MixerChatService : MixerWebSocketServiceBase, IMixerChatService
    {
        public event EventHandler<MixerChatMessageViewModel> OnMessageOccurred = delegate { };
        public event EventHandler<MixerSkillChatMessageViewModel> OnSkillOccurred = delegate { };
        public event EventHandler<Tuple<Guid, UserViewModel>> OnDeleteMessageOccurred = delegate { };
        public event EventHandler OnClearMessagesOccurred = delegate { };

        public event EventHandler<IEnumerable<UserViewModel>> OnUsersJoinOccurred = delegate { };
        public event EventHandler<UserViewModel> OnUserUpdateOccurred = delegate { };
        public event EventHandler<IEnumerable<UserViewModel>> OnUsersLeaveOccurred = delegate { };
        public event EventHandler<Tuple<UserViewModel, UserViewModel>> OnUserPurgeOccurred = delegate { };
        public event EventHandler<UserViewModel> OnUserBanOccurred = delegate { };

        public event EventHandler<ChatPollEventModel> OnPollEndOccurred = delegate { };

        private ChatClient userClient;
        private ChatClient botClient;

        private const int userJoinLeaveEventsTotalToProcess = 25;
        private SemaphoreSlim userJoinLeaveEventsSemaphore = new SemaphoreSlim(1);
        private Dictionary<uint, ChatUserEventModel> userJoinEvents = new Dictionary<uint, ChatUserEventModel>();
        private Dictionary<uint, ChatUserEventModel> userLeaveEvents = new Dictionary<uint, ChatUserEventModel>();

        private CancellationTokenSource cancellationTokenSource;

        public MixerChatService() { }

        #region Interface Methods

        public bool IsBotConnected { get { return this.botClient != null && this.botClient.Connected; } }

        public async Task<bool> ConnectUser()
        {
            return await this.AttemptConnect(async () =>
            {
                if (ChannelSession.MixerUserConnection != null)
                {
                    this.cancellationTokenSource = new CancellationTokenSource();

                    this.userClient = await this.ConnectAndAuthenticateChatClient(ChannelSession.MixerUserConnection);
                    if (this.userClient != null)
                    {
                        this.userClient.OnClearMessagesOccurred += ChatClient_OnClearMessagesOccurred;
                        this.userClient.OnDeleteMessageOccurred += ChatClient_OnDeleteMessageOccurred;
                        this.userClient.OnMessageOccurred += ChatClient_OnMessageOccurred;
                        this.userClient.OnPollEndOccurred += ChatClient_OnPollEndOccurred;
                        this.userClient.OnPollStartOccurred += ChatClient_OnPollStartOccurred;
                        this.userClient.OnPurgeMessageOccurred += ChatClient_OnPurgeMessageOccurred;
                        this.userClient.OnUserJoinOccurred += ChatClient_OnUserJoinOccurred;
                        this.userClient.OnUserLeaveOccurred += ChatClient_OnUserLeaveOccurred;
                        this.userClient.OnUserUpdateOccurred += ChatClient_OnUserUpdateOccurred;
                        this.userClient.OnSkillAttributionOccurred += Client_OnSkillAttributionOccurred;
                        this.userClient.OnDisconnectOccurred += StreamerClient_OnDisconnectOccurred;
                        if (ChannelSession.Settings.DiagnosticLogging)
                        {
                            this.userClient.OnPacketSentOccurred += WebSocketClient_OnPacketSentOccurred;
                            this.userClient.OnMethodOccurred += WebSocketClient_OnMethodOccurred;
                            this.userClient.OnReplyOccurred += WebSocketClient_OnReplyOccurred;
                            this.userClient.OnEventOccurred += WebSocketClient_OnEventOccurred;
                        }

                        AsyncRunner.RunBackgroundTask(this.cancellationTokenSource.Token, 300000, this.ChatterRefreshBackground);

                        AsyncRunner.RunBackgroundTask(this.cancellationTokenSource.Token, 2500, this.ChatterJoinLeaveBackground);

                        return true;
                    }
                }
                await this.DisconnectUser();
                return false;
            });
        }

        public async Task DisconnectUser()
        {
            await this.RunAsync(async () =>
            {
                if (this.userClient != null)
                {
                    this.userClient.OnClearMessagesOccurred -= ChatClient_OnClearMessagesOccurred;
                    this.userClient.OnDeleteMessageOccurred -= ChatClient_OnDeleteMessageOccurred;
                    this.userClient.OnMessageOccurred -= ChatClient_OnMessageOccurred;
                    this.userClient.OnPollEndOccurred -= ChatClient_OnPollEndOccurred;
                    this.userClient.OnPollStartOccurred -= ChatClient_OnPollStartOccurred;
                    this.userClient.OnPurgeMessageOccurred -= ChatClient_OnPurgeMessageOccurred;
                    this.userClient.OnUserJoinOccurred -= ChatClient_OnUserJoinOccurred;
                    this.userClient.OnUserLeaveOccurred -= ChatClient_OnUserLeaveOccurred;
                    this.userClient.OnUserUpdateOccurred -= ChatClient_OnUserUpdateOccurred;
                    this.userClient.OnSkillAttributionOccurred -= Client_OnSkillAttributionOccurred;
                    this.userClient.OnDisconnectOccurred -= StreamerClient_OnDisconnectOccurred;
                    if (ChannelSession.Settings.DiagnosticLogging)
                    {
                        this.userClient.OnPacketSentOccurred -= WebSocketClient_OnPacketSentOccurred;
                        this.userClient.OnMethodOccurred -= WebSocketClient_OnMethodOccurred;
                        this.userClient.OnReplyOccurred -= WebSocketClient_OnReplyOccurred;
                        this.userClient.OnEventOccurred -= WebSocketClient_OnEventOccurred;
                    }

                    await this.RunAsync(this.userClient.Disconnect());
                }

                this.userClient = null;
                if (this.cancellationTokenSource != null)
                {
                    this.cancellationTokenSource.Cancel();
                    this.cancellationTokenSource = null;
                }
            });
        }

        public async Task<bool> ConnectBot()
        {
            if (ChannelSession.MixerBotConnection != null)
            {
                return await this.AttemptConnect(async () =>
                {
                    if (ChannelSession.MixerBotConnection != null)
                    {
                        this.botClient = await this.ConnectAndAuthenticateChatClient(ChannelSession.MixerBotConnection);
                        if (this.botClient != null)
                        {
                            this.botClient.OnMessageOccurred += BotChatClient_OnMessageOccurred;
                            this.botClient.OnDisconnectOccurred += BotClient_OnDisconnectOccurred;
                            if (ChannelSession.Settings.DiagnosticLogging)
                            {
                                this.botClient.OnPacketSentOccurred += WebSocketClient_OnPacketSentOccurred;
                                this.botClient.OnMethodOccurred += WebSocketClient_OnMethodOccurred;
                                this.botClient.OnReplyOccurred += WebSocketClient_OnReplyOccurred;
                                this.botClient.OnEventOccurred += WebSocketClient_OnEventOccurred;
                            }
                            return true;
                        }
                        return false;
                    }
                    await this.DisconnectBot();
                    return false;
                });
            }
            return true;
        }

        public async Task DisconnectBot()
        {
            await this.RunAsync(async () =>
            {
                if (this.botClient != null)
                {
                    this.botClient.OnMessageOccurred -= BotChatClient_OnMessageOccurred;
                    this.botClient.OnDisconnectOccurred -= BotClient_OnDisconnectOccurred;
                    if (ChannelSession.Settings.DiagnosticLogging)
                    {
                        this.botClient.OnPacketSentOccurred -= WebSocketClient_OnPacketSentOccurred;
                        this.botClient.OnMethodOccurred -= WebSocketClient_OnMethodOccurred;
                        this.botClient.OnReplyOccurred -= WebSocketClient_OnReplyOccurred;
                        this.botClient.OnEventOccurred -= WebSocketClient_OnEventOccurred;
                    }

                    await this.RunAsync(this.botClient.Disconnect());
                }
                this.botClient = null;
            });
        }

        public async Task Initialize()
        {
            await ChannelSession.MixerUserConnection.GetChatUsers(ChannelSession.MixerChannel, (users) =>
            {
                foreach (ChatUserModel user in users)
                {
                    this.ChatClient_OnUserJoinOccurred(this, new ChatUserEventModel()
                    {
                        id = user.userId.GetValueOrDefault(),
                        username = user.userName,
                        roles = user.userRoles,
                    });
                }
                return Task.FromResult(0);
            }, uint.MaxValue);
        }

        public async Task SendMessage(string message, bool sendAsUser = false)
        {
            await this.RunAsync(async () =>
            {
                ChatClient client = this.GetChatClient(sendAsUser);
                if (client != null)
                {
                    message = this.SplitLargeMessage(message, out string subMessage);

                    await client.SendMessage(message);

                    // Adding delay to prevent messages from arriving in wrong order
                    await Task.Delay(250);

                    if (!string.IsNullOrEmpty(subMessage))
                    {
                        await this.SendMessage(subMessage, sendAsUser: sendAsUser);
                    }
                }
            });
        }

        public async Task Whisper(string username, string message, bool sendAsUser = false)
        {
            await this.RunAsync(async () =>
            {
                ChatClient client = this.GetChatClient(sendAsUser);
                if (client != null)
                {
                    if (!string.IsNullOrEmpty(username))
                    {
                        message = this.SplitLargeMessage(message, out string subMessage);

                        await client.Whisper(username, message);

                        // Adding delay to prevent messages from arriving in wrong order
                        await Task.Delay(250);

                        if (!string.IsNullOrEmpty(subMessage))
                        {
                            await this.Whisper(username, subMessage, sendAsUser: sendAsUser);
                        }
                    }
                }
            });
        }

        public async Task<ChatMessageEventModel> WhisperWithResponse(string username, string message, bool sendAsUser = false)
        {
            return await this.RunAsync(async () =>
            {
                ChatClient client = this.GetChatClient(sendAsUser);
                if (client != null)
                {
                    message = this.SplitLargeMessage(message, out string subMessage);

                    ChatMessageEventModel firstChatMessage = await client.WhisperWithResponse(username, message);

                    // Adding delay to prevent messages from arriving in wrong order
                    await Task.Delay(250);

                    if (!string.IsNullOrEmpty(subMessage))
                    {
                        await this.WhisperWithResponse(username, subMessage, sendAsUser: sendAsUser);
                    }

                    return firstChatMessage;
                }
                return null;
            });
        }

        public async Task<IEnumerable<ChatMessageEventModel>> GetChatHistory(uint maxMessages)
        {
            return await this.RunAsync(async () =>
            {
                if (this.userClient != null)
                {
                    return await this.userClient.GetChatHistory(maxMessages);
                }
                return new List<ChatMessageEventModel>();
            });
        }

        public async Task DeleteMessage(ChatMessageViewModel message)
        {
            await this.RunAsync(async () =>
            {
                Logger.Log(LogLevel.Debug, string.Format("Deleting Message - {0}", message.PlainTextMessage));

                await this.userClient.DeleteMessage(Guid.Parse(message.ID));
            });
        }

        public async Task ClearMessages()
        {
            await this.RunAsync(async () =>
            {
                await this.userClient.ClearMessages();
            });
        }

        public async Task PurgeUser(string username)
        {
            await this.RunAsync(async () =>
            {
                await this.userClient.PurgeUser(username);
            });
        }

        public async Task TimeoutUser(string username, uint durationInSeconds)
        {
            await this.RunAsync(async () =>
            {
                await this.userClient.TimeoutUser(username, durationInSeconds);
            });
        }

        public async Task BanUser(UserViewModel user)
        {
            await this.RunAsync(async () =>
            {
                await ChannelSession.MixerUserConnection.AddUserRoles(ChannelSession.MixerChannel, user.GetModel(), new List<MixerRoleEnum>() { MixerRoleEnum.Banned });
                await user.RefreshDetails(true);
            });
        }

        public async Task UnbanUser(UserViewModel user)
        {
            await this.RunAsync(async () =>
            {
                await ChannelSession.MixerUserConnection.RemoveUserRoles(ChannelSession.MixerChannel, user.GetModel(), new List<MixerRoleEnum>() { MixerRoleEnum.Banned });
                await user.RefreshDetails(true);
            });
        }

        public async Task ModUser(UserViewModel user)
        {
            await this.RunAsync(async () =>
            {
                await ChannelSession.MixerUserConnection.AddUserRoles(ChannelSession.MixerChannel, user.GetModel(), new List<MixerRoleEnum>() { MixerRoleEnum.Mod });
                await user.RefreshDetails(true);
            });
        }

        public async Task UnmodUser(UserViewModel user)
        {
            await this.RunAsync(async () =>
            {
                await ChannelSession.MixerUserConnection.RemoveUserRoles(ChannelSession.MixerChannel, user.GetModel(), new List<MixerRoleEnum>() { MixerRoleEnum.Mod });
                await user.RefreshDetails(true);
            });
        }

        public async Task StartPoll(string question, IEnumerable<string> answers, uint lengthInSeconds)
        {
            await this.RunAsync(async () =>
            {
                await this.userClient.StartVote(question, answers, lengthInSeconds);
            });
        }

        private ChatClient GetChatClient(bool sendAsStreamer = false) { return (this.botClient != null && !sendAsStreamer) ? this.botClient : this.userClient; }

        private async Task<ChatClient> ConnectAndAuthenticateChatClient(MixerConnectionService connection)
        {
            ChatClient client = await this.RunAsync(ChatClient.CreateFromChannel(connection.Connection, ChannelSession.MixerChannel));
            if (client != null)
            {
                if (await this.RunAsync(client.Connect()) && await this.RunAsync(client.Authenticate()))
                {
                    return client;
                }
                else
                {
                    Logger.Log("Failed to connect & authenticate Chat client");
                }
            }
            return null;
        }

        private string SplitLargeMessage(string message, out string subMessage)
        {
            subMessage = null;
            if (message.Length > 360)
            {
                string message360 = message.Substring(0, 360);
                int splitIndex = message360.LastIndexOf(' ');
                if (splitIndex > 0 && (splitIndex + 1) < message.Length)
                {
                    subMessage = message.Substring(splitIndex + 1);
                    message = message.Substring(0, splitIndex);
                }
            }
            return message;
        }

        #endregion Interface Methods

        #region Refresh Methods

        private async Task ChatterJoinLeaveBackground(CancellationToken cancellationToken)
        {
            List<ChatUserEventModel> joinsToProcess = new List<ChatUserEventModel>();
            await this.userJoinLeaveEventsSemaphore.WaitAndRelease(() =>
            {
                for (int i = 0; i < userJoinLeaveEventsTotalToProcess && i < this.userJoinEvents.Count(); i++)
                {
                    ChatUserEventModel chatUser = this.userJoinEvents.Values.First();
                    joinsToProcess.Add(chatUser);
                    this.userJoinEvents.Remove(chatUser.id);
                }
                return Task.FromResult(0);
            });

            if (joinsToProcess.Count > 0)
            {
                List<UserViewModel> processedUsers = new List<UserViewModel>();
                foreach (ChatUserEventModel chatUser in joinsToProcess)
                {
                    UserViewModel user = await ChannelSession.Services.User.AddOrUpdateUser(chatUser);
                    if (user != null)
                    {
                        processedUsers.Add(user);
                    }
                }
                this.OnUsersJoinOccurred(this, processedUsers);
            }

            List<ChatUserEventModel> leavesToProcess = new List<ChatUserEventModel>();
            await this.userJoinLeaveEventsSemaphore.WaitAndRelease(() =>
            {
                for (int i = 0; i < userJoinLeaveEventsTotalToProcess && i < this.userLeaveEvents.Count(); i++)
                {
                    ChatUserEventModel chatUser = this.userLeaveEvents.Values.First();
                    leavesToProcess.Add(chatUser);
                    this.userLeaveEvents.Remove(chatUser.id);
                }
                return Task.FromResult(0);
            });

            if (leavesToProcess.Count > 0)
            {
                List<UserViewModel> processedUsers = new List<UserViewModel>();
                foreach (ChatUserEventModel chatUser in leavesToProcess)
                {
                    UserViewModel user = await ChannelSession.Services.User.RemoveUser(chatUser);
                    if (user != null)
                    {
                        processedUsers.Add(user);
                    }
                }
                this.OnUsersLeaveOccurred(this, processedUsers);
            }
        }

        private async Task ChatterRefreshBackground(CancellationToken cancellationToken)
        {
            List<ChatUserModel> chatUsers = new List<ChatUserModel>();
            await ChannelSession.MixerUserConnection.GetChatUsers(ChannelSession.MixerChannel, (users) =>
            {
                chatUsers.AddRange(users);
                return Task.FromResult(0);
            }, uint.MaxValue);

            chatUsers = chatUsers.Where(u => u.userId.HasValue).ToList();
            List<uint> chatUserIDs = new List<uint>(chatUsers.Select(u => u.userId.GetValueOrDefault()));

            IEnumerable<UserViewModel> existingUsers = ChannelSession.Services.User.GetAllUsers();
            List<uint> existingUsersIDs = new List<uint>(existingUsers.Select(u => u.MixerID));

            Dictionary<uint, ChatUserModel> usersToAdd = new Dictionary<uint, ChatUserModel>();
            foreach (ChatUserModel user in chatUsers)
            {
                usersToAdd[user.userId.GetValueOrDefault()] = user;
            }

            List<uint> usersToRemove = new List<uint>();
            foreach (uint userID in existingUsersIDs)
            {
                usersToAdd.Remove(userID);
                if (!chatUserIDs.Contains(userID))
                {
                    usersToRemove.Add(userID);
                }
            }

            foreach (ChatUserModel user in usersToAdd.Values)
            {
                this.ChatClient_OnUserJoinOccurred(this, new ChatUserEventModel()
                {
                    id = user.userId.GetValueOrDefault(),
                    username = user.userName,
                    roles = user.userRoles,
                });
            }

            foreach (uint userID in usersToRemove)
            {
                this.ChatClient_OnUserLeaveOccurred(this, new ChatUserEventModel() { id = userID });
            }
        }

        #endregion Refresh Methods

        #region Chat Event Handlers

        private void ChatClient_OnMessageOccurred(object sender, ChatMessageEventModel e)
        {
            if (e.message != null)
            {
                if (e.message.ContainsSkill)
                {
                    MixerSkillChatMessageViewModel message = new MixerSkillChatMessageViewModel(e);
                    this.OnMessageOccurred(sender, message);
                    GlobalEvents.SkillUseOccurred(message);
                    if (message.Skill.Cost > 0)
                    {
                        if (message.Skill.CostType == MixerSkillCostTypeEnum.Sparks)
                        {
                            GlobalEvents.SparkUseOccurred(new Tuple<UserViewModel, uint>(message.User, message.Skill.Cost));
                        }
                        else if (message.Skill.CostType == MixerSkillCostTypeEnum.Embers)
                        {
                            GlobalEvents.EmberUseOccurred(new UserEmberUsageModel(message));
                        }
                    }
                }
                else
                {
                    this.OnMessageOccurred(sender, new MixerChatMessageViewModel(e));
                }
            }
        }

        private void BotChatClient_OnMessageOccurred(object sender, ChatMessageEventModel e)
        {
            if (!string.IsNullOrEmpty(e.target))
            {
                this.OnMessageOccurred(sender, new MixerChatMessageViewModel(e));
            }
        }

        private void ChatClient_OnDeleteMessageOccurred(object sender, ChatDeleteMessageEventModel e)
        {
            this.OnDeleteMessageOccurred(sender, new Tuple<Guid, UserViewModel>(e.id, new UserViewModel(e.moderator)));
        }

        private void ChatClient_OnPurgeMessageOccurred(object sender, ChatPurgeMessageEventModel e)
        {
            UserViewModel user = ChannelSession.Services.User.GetUserByID(e.user_id);
            if (user != null)
            {
                UserViewModel modUser = null;
                if (e.moderator != null)
                {
                    modUser = new UserViewModel(e.moderator);
                }
                this.OnUserPurgeOccurred(sender, new Tuple<UserViewModel, UserViewModel>(user, modUser));
            }
        }

        private void ChatClient_OnClearMessagesOccurred(object sender, ChatClearMessagesEventModel e)
        {
            this.OnClearMessagesOccurred(sender, new EventArgs());
        }

        private void ChatClient_OnPollStartOccurred(object sender, ChatPollEventModel e) { }

        private void ChatClient_OnPollEndOccurred(object sender, ChatPollEventModel e) { this.OnPollEndOccurred(sender, e); }

        private async void ChatClient_OnUserJoinOccurred(object sender, ChatUserEventModel chatUser)
        {
            await this.userJoinLeaveEventsSemaphore.WaitAndRelease(() =>
            {
                this.userJoinEvents[chatUser.id] = chatUser;
                return Task.FromResult(0);
            });
        }

        private async void ChatClient_OnUserLeaveOccurred(object sender, ChatUserEventModel chatUser)
        {
            await this.userJoinLeaveEventsSemaphore.WaitAndRelease(() =>
            {
                this.userLeaveEvents[chatUser.id] = chatUser;
                return Task.FromResult(0);
            });
        }

        private async void ChatClient_OnUserUpdateOccurred(object sender, ChatUserEventModel chatUser)
        {
            UserViewModel user = await ChannelSession.Services.User.AddOrUpdateUser(chatUser.GetUser());
            if (user != null)
            {
                try
                {
                    if (user.Data.ViewingMinutes == 0)
                    {
                        if (EventCommand.CanUserRunEvent(user, EnumHelper.GetEnumName(OtherEventTypeEnum.ChatUserFirstJoin)))
                        {
                            await EventCommand.FindAndRunEventCommand(EnumHelper.GetEnumName(OtherEventTypeEnum.ChatUserFirstJoin), user);
                        }
                    }
                }
                catch (Exception ex) { Logger.Log(ex); }

                this.OnUserUpdateOccurred(sender, user);
                if (chatUser.roles != null && chatUser.roles.Count() > 0 && chatUser.roles.Where(r => !string.IsNullOrEmpty(r)).Contains(EnumHelper.GetEnumName(MixerRoleEnum.Banned)))
                {
                    this.OnUserBanOccurred(sender, user);
                }
            }
        }

        private async void Client_OnSkillAttributionOccurred(object sender, ChatSkillAttributionEventModel skillAttribution)
        {
            MixerSkillChatMessageViewModel message = new MixerSkillChatMessageViewModel(skillAttribution);

            // Add artificial delay to ensure skill event data from Constellation was received.
            for (int i = 0; i < 8; i++)
            {
                await Task.Delay(250);
                if (ChannelSession.Constellation.SkillEventsTriggered.ContainsKey(skillAttribution.id))
                {
                    message.Skill.SetPayload(ChannelSession.Constellation.SkillEventsTriggered[skillAttribution.id]);
                    ChannelSession.Constellation.SkillEventsTriggered.Remove(skillAttribution.id);
                    break;
                }
            }

            this.OnMessageOccurred(sender, message);
            GlobalEvents.SkillUseOccurred(message);
            if (message.Skill.Cost > 0)
            {
                if (message.Skill.CostType == MixerSkillCostTypeEnum.Sparks)
                {
                    GlobalEvents.SparkUseOccurred(new Tuple<UserViewModel, uint>(message.User, message.Skill.Cost));
                }
                else if (message.Skill.CostType == MixerSkillCostTypeEnum.Embers)
                {
                    GlobalEvents.EmberUseOccurred(new UserEmberUsageModel(message));
                }
            }
        }

        private async void StreamerClient_OnDisconnectOccurred(object sender, WebSocketCloseStatus e)
        {
            ChannelSession.DisconnectionOccurred("Streamer Chat");

            await this.DisconnectUser();
            do
            {
                await Task.Delay(2500);
            }
            while (!await this.ConnectUser());

            ChannelSession.ReconnectionOccurred("Streamer Chat");
        }

        private async void BotClient_OnDisconnectOccurred(object sender, WebSocketCloseStatus e)
        {
            ChannelSession.DisconnectionOccurred("Bot Chat");

            await this.DisconnectBot();
            do
            {
                await Task.Delay(2500);
            }
            while (!await this.ConnectBot());

            ChannelSession.ReconnectionOccurred("Bot Chat");
        }

        #endregion Chat Event Handlers
    }
}
