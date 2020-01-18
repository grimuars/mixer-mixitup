using MixItUp.Base.Actions;
using MixItUp.Base.Commands;
using MixItUp.Base.Model.MixPlay;
using MixItUp.Base.Model.Overlay;
using MixItUp.Base.Model.Remote.Authentication;
using MixItUp.Base.Model.Serial;
using MixItUp.Base.Remote.Models;
using MixItUp.Base.Services;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.Requirement;
using MixItUp.Base.ViewModel.User;
using Newtonsoft.Json.Linq;
using StreamingClient.Base.Model.OAuth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace MixItUp.Base.Model.Settings
{
    [DataContract]
    public class StreamerSettingsV2Model : SettingsV2ModelBase
    {
        [DataMember]
        public bool FeatureMe { get; set; }

        [DataMember]
        public OAuthTokenModel MixerBotOAuthToken { get; set; }
        [DataMember]
        public OAuthTokenModel TwitchBotOAuthToken { get; set; }

        // External Services
        [DataMember]
        public OAuthTokenModel StreamlabsOAuthToken { get; set; }

        [DataMember]
        public OAuthTokenModel TwitterOAuthToken { get; set; }

        [DataMember]
        public OAuthTokenModel DiscordOAuthToken { get; set; }
        [DataMember]
        public string DiscordServer { get; set; }
        [DataMember]
        public string DiscordCustomClientID { get; set; }
        [DataMember]
        public string DiscordCustomClientSecret { get; set; }
        [DataMember]
        public string DiscordCustomBotToken { get; set; }

        [DataMember]
        public OAuthTokenModel TiltifyOAuthToken { get; set; }
        [DataMember]
        public int TiltifyCampaign { get; set; }

        [DataMember]
        public OAuthTokenModel TipeeeStreamOAuthToken { get; set; }

        [DataMember]
        public OAuthTokenModel TreatStreamOAuthToken { get; set; }

        [DataMember]
        public OAuthTokenModel StreamJarOAuthToken { get; set; }

        [DataMember]
        public OAuthTokenModel PatreonOAuthToken { get; set; }
        [DataMember]
        public string PatreonTierMixerSubscriberEquivalent { get; set; }

        [DataMember]
        public OAuthTokenModel IFTTTOAuthToken { get; set; }

        [DataMember]
        public OAuthTokenModel StreamlootsOAuthToken { get; set; }

        [DataMember]
        public OAuthTokenModel JustGivingOAuthToken { get; set; }
        [DataMember]
        public string JustGivingPageShortName { get; set; }

        [DataMember]
        public int ExtraLifeTeamID { get; set; }
        [DataMember]
        public int ExtraLifeParticipantID { get; set; }
        [DataMember]
        public bool ExtraLifeIncludeTeamDonations { get; set; }

        [DataMember]
        public string OvrStreamServerIP { get; set; }

        [DataMember]
        public string OBSStudioServerIP { get; set; }
        [DataMember]
        public string OBSStudioServerPassword { get; set; }

        [DataMember]
        public bool EnableStreamlabsOBSConnection { get; set; }

        [DataMember]
        public bool EnableXSplitConnection { get; set; }
        [DataMember]
        public bool EnableDeveloperAPI { get; set; }

        // Timers
        [DataMember]
        public int TimerCommandsInterval { get; set; } = 10;
        [DataMember]
        public int TimerCommandsMinimumMessages { get; set; } = 10;
        [DataMember]
        public bool DisableAllTimers { get; set; }

        // MixPlay
        [DataMember]
        public uint DefaultMixPlayGame { get; set; }
        [DataMember]
        public bool PreventUnknownMixPlayUsers { get; set; }
        [DataMember]
        public bool PreventSmallerMixPlayCooldowns { get; set; }
        [DataMember]
        public List<MixPlaySharedProjectModel> CustomMixPlayProjectIDs { get; set; } = new List<MixPlaySharedProjectModel>();
        [DataMember]
        public Dictionary<uint, JObject> CustomMixPlaySettings { get; set; } = new Dictionary<uint, JObject>();
        [DataMember]
        public Dictionary<uint, List<MixPlayUserGroupModel>> MixPlayUserGroups { get; set; } = new Dictionary<uint, List<MixPlayUserGroupModel>>();

        // Quotes
        [DataMember]
        public bool QuotesEnabled { get; set; }
        [DataMember]
        public string QuotesFormat { get; set; }

        // Game Queue
        [DataMember]
        public bool GameQueueSubPriority { get; set; }
        [DataMember]
        public RequirementViewModel GameQueueRequirements { get; set; } = new RequirementViewModel();
        [DataMember]
        public CustomCommand GameQueueUserJoinedCommand { get; set; }
        [DataMember]
        public CustomCommand GameQueueUserSelectedCommand { get; set; }

        // Giveaway
        [DataMember]
        public string GiveawayCommand { get; set; } = "giveaway";
        [DataMember]
        public int GiveawayTimer { get; set; } = 1;
        [DataMember]
        public int GiveawayMaximumEntries { get; set; } = 1;
        [DataMember]
        public RequirementViewModel GiveawayRequirements { get; set; } = new RequirementViewModel();
        [DataMember]
        public int GiveawayReminderInterval { get; set; } = 5;
        [DataMember]
        public bool GiveawayRequireClaim { get; set; } = true;
        [DataMember]
        public bool GiveawayAllowPastWinners { get; set; }
        [DataMember]
        public CustomCommand GiveawayStartedReminderCommand { get; set; }
        [DataMember]
        public CustomCommand GiveawayUserJoinedCommand { get; set; }
        [DataMember]
        public CustomCommand GiveawayWinnerSelectedCommand { get; set; }

        // Moderation
        [DataMember]
        public bool ModerationUseCommunityFilteredWords { get; set; }
        [DataMember]
        public int ModerationFilteredWordsTimeout1MinuteOffenseCount { get; set; }
        [DataMember]
        public int ModerationFilteredWordsTimeout5MinuteOffenseCount { get; set; }
        [DataMember]
        public MixerRoleEnum ModerationFilteredWordsExcempt { get; set; } = MixerRoleEnum.Mod;
        [DataMember]
        public bool ModerationFilteredWordsApplyStrikes { get; set; } = true;
        [DataMember]
        public int ModerationCapsBlockCount { get; set; }
        [DataMember]
        public bool ModerationCapsBlockIsPercentage { get; set; } = true;
        [DataMember]
        public int ModerationPunctuationBlockCount { get; set; }
        [DataMember]
        public bool ModerationPunctuationBlockIsPercentage { get; set; } = true;
        [DataMember]
        public MixerRoleEnum ModerationChatTextExcempt { get; set; } = MixerRoleEnum.Mod;
        [DataMember]
        public bool ModerationChatTextApplyStrikes { get; set; } = true;
        [DataMember]
        public bool ModerationBlockLinks { get; set; }
        [DataMember]
        public MixerRoleEnum ModerationBlockLinksExcempt { get; set; } = MixerRoleEnum.Mod;
        [DataMember]
        public bool ModerationBlockLinksApplyStrikes { get; set; } = true;
        [DataMember]
        public ModerationChatInteractiveParticipationEnum ModerationChatInteractiveParticipation { get; set; } = ModerationChatInteractiveParticipationEnum.None;
        [DataMember]
        public MixerRoleEnum ModerationChatInteractiveParticipationExcempt { get; set; } = MixerRoleEnum.Mod;
        [DataMember]
        public bool ModerationResetStrikesOnLaunch { get; set; }
        [DataMember]
        public CustomCommand ModerationStrike1Command { get; set; }
        [DataMember]
        public CustomCommand ModerationStrike2Command { get; set; }
        [DataMember]
        public CustomCommand ModerationStrike3Command { get; set; }
        [DataMember]
        public List<string> FilteredWords { get; set; } = new List<string>();
        [DataMember]
        public List<string> BannedWords { get; set; } = new List<string>();

        // Remote
        [DataMember]
        public RemoteConnectionAuthenticationTokenModel RemoteHostConnection { get; set; }
        [DataMember]
        public List<RemoteConnectionModel> RemoteClientConnections { get; set; } = new List<RemoteConnectionModel>();
        [DataMember]
        public List<RemoteProfileModel> RemoteProfiles { get; set; } = new List<RemoteProfileModel>();
        [DataMember]
        public Dictionary<Guid, RemoteProfileBoardsModel> RemoteProfileBoards { get; set; } = new Dictionary<Guid, RemoteProfileBoardsModel>();

        // Overlay
        [DataMember]
        public bool EnableOverlay { get; set; }
        [DataMember]
        public Dictionary<string, int> OverlayCustomNameAndPorts { get; set; } = new Dictionary<string, int>();
        [DataMember]
        public string OverlaySourceName { get; set; }
        [DataMember]
        public int OverlayWidgetRefreshTime { get; set; } = 5;
        [DataMember]
        public List<OverlayWidgetModel> OverlayWidgets { get; set; } = new List<OverlayWidgetModel>();

        // Misc
        [DataMember]
        public List<PreMadeChatCommandSettings> PreMadeChatCommandSettings { get; set; } = new List<PreMadeChatCommandSettings>();

        [DataMember]
        public List<SerialDeviceModel> SerialDevices { get; set; } = new List<SerialDeviceModel>();

        [DataMember]
        public Dictionary<string, CommandGroupSettings> CommandGroups { get; set; } = new Dictionary<string, CommandGroupSettings>();
        [DataMember]
        public Dictionary<string, HotKeyConfiguration> HotKeys { get; set; } = new Dictionary<string, HotKeyConfiguration>();

        [DataMember]
        public StreamingSoftwareTypeEnum DefaultStreamingSoftware { get; set; } = StreamingSoftwareTypeEnum.OBSStudio;
        [DataMember]
        public string DefaultAudioOutput { get; set; }

        [DataMember]
        public List<string> RecentStreamTitles { get; set; } = new List<string>();
        [DataMember]
        public Dictionary<string, object> LatestSpecialIdentifiersData { get; set; } = new Dictionary<string, object>();

        [DataMember]
        public Dictionary<string, int> CooldownGroups { get; set; } = new Dictionary<string, int>();

        public StreamerSettingsV2Model() { }

        public override void Initialize()
        {
            base.Initialize();

            this.IsStreamer = true;

            this.GameQueueUserJoinedCommand = CustomCommand.BasicChatCommand("Game Queue Used Joined", "You are #$queueposition in the queue to play.", isWhisper: true);
            this.GameQueueUserSelectedCommand = CustomCommand.BasicChatCommand("Game Queue Used Selected", "It's time to play @$username! Listen carefully for instructions on how to join...");

            this.GiveawayStartedReminderCommand = CustomCommand.BasicChatCommand("Giveaway Started/Reminder", "A giveaway has started for $giveawayitem! Type $giveawaycommand in chat in the next $giveawaytimelimit minute(s) to enter!");
            this.GiveawayUserJoinedCommand = CustomCommand.BasicChatCommand("Giveaway User Joined", "You have been entered into the giveaway, stay tuned to see who wins!", isWhisper: true);
            this.GiveawayWinnerSelectedCommand = CustomCommand.BasicChatCommand("Giveaway Winner Selected", "Congratulations @$username, you won $giveawayitem!");

            this.ModerationStrike1Command = CustomCommand.BasicChatCommand("Moderation Strike 1", "$moderationreason. You have received a moderation strike & currently have $usermoderationstrikes strike(s)", isWhisper: true);
            this.ModerationStrike2Command = CustomCommand.BasicChatCommand("Moderation Strike 2", "$moderationreason. You have received a moderation strike & currently have $usermoderationstrikes strike(s)", isWhisper: true);
            this.ModerationStrike3Command = CustomCommand.BasicChatCommand("Moderation Strike 3", "$moderationreason. You have received a moderation strike & currently have $usermoderationstrikes strike(s)", isWhisper: true);
        }

        public override Task CopyLatestValues()
        {
            if (ChannelSession.MixerBotConnection != null)
            {
                this.MixerBotOAuthToken = ChannelSession.MixerBotConnection.Connection.GetOAuthTokenCopy();
            }
            if (ChannelSession.TwitchBotConnection != null)
            {
                this.TwitchBotOAuthToken = ChannelSession.TwitchBotConnection.Connection.GetOAuthTokenCopy();
            }

            this.StreamlabsOAuthToken = ChannelSession.Services.Streamlabs.GetOAuthTokenCopy();
            this.StreamJarOAuthToken = ChannelSession.Services.StreamJar.GetOAuthTokenCopy();
            this.TipeeeStreamOAuthToken = ChannelSession.Services.TipeeeStream.GetOAuthTokenCopy();
            this.TreatStreamOAuthToken = ChannelSession.Services.TreatStream.GetOAuthTokenCopy();
            this.StreamlootsOAuthToken = ChannelSession.Services.Streamloots.GetOAuthTokenCopy();
            this.TiltifyOAuthToken = ChannelSession.Services.Tiltify.GetOAuthTokenCopy();
            this.PatreonOAuthToken = ChannelSession.Services.Patreon.GetOAuthTokenCopy();
            this.IFTTTOAuthToken = ChannelSession.Services.IFTTT.GetOAuthTokenCopy();
            this.JustGivingOAuthToken = ChannelSession.Services.JustGiving.GetOAuthTokenCopy();
            this.DiscordOAuthToken = ChannelSession.Services.Discord.GetOAuthTokenCopy();
            this.TwitterOAuthToken = ChannelSession.Services.Twitter.GetOAuthTokenCopy();

            // Clear out unused Cooldown Groups and Command Groups
            //var allUsedCooldownGroupNames =
            //    this.MixPlayCommands.Select(c => c.Requirements?.Cooldown?.GroupName)
            //    .Union(this.ChatCommands.Select(c => c.Requirements?.Cooldown?.GroupName))
            //    .Union(this.GameCommands.Select(c => c.Requirements?.Cooldown?.GroupName))
            //    .Distinct();
            //var allUnusedCooldownGroupNames = this.CooldownGroups.Where(c => !allUsedCooldownGroupNames.Contains(c.Key, StringComparer.InvariantCultureIgnoreCase));
            //foreach (var unused in allUnusedCooldownGroupNames)
            //{
            //    this.CooldownGroups.Remove(unused.Key);
            //}

            //var allUsedCommandGroupNames =
            //    this.ChatCommands.Select(c => c.GroupName)
            //    .Union(this.ActionGroupCommands.Select(a => a.GroupName))
            //    .Union(this.TimerCommands.Select(a => a.GroupName))
            //    .Distinct();
            //var allUnusedCommandGroupNames = this.CommandGroups.Where(c => !allUsedCommandGroupNames.Contains(c.Key, StringComparer.InvariantCultureIgnoreCase));
            //foreach (var unused in allUnusedCommandGroupNames)
            //{
            //    this.CommandGroups.Remove(unused.Key);
            //}
        }
    }
}
