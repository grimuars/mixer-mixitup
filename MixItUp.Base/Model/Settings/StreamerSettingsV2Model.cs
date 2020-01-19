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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StreamingClient.Base.Model.OAuth;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MixItUp.Base.Model.Settings
{
    [DataContract]
    public class StreamerSettingsV2Model : BaseSettingsV2ModelBase
    {
        [JsonProperty]
        public bool FeatureMe { get; set; }

        // External Services
        [JsonProperty]
        public OAuthTokenModel StreamlabsOAuthToken { get; set; }

        [JsonProperty]
        public OAuthTokenModel TwitterOAuthToken { get; set; }

        [JsonProperty]
        public OAuthTokenModel DiscordOAuthToken { get; set; }
        [JsonProperty]
        public string DiscordServer { get; set; }
        [JsonProperty]
        public string DiscordCustomClientID { get; set; }
        [JsonProperty]
        public string DiscordCustomClientSecret { get; set; }
        [JsonProperty]
        public string DiscordCustomBotToken { get; set; }

        [JsonProperty]
        public OAuthTokenModel TiltifyOAuthToken { get; set; }
        [JsonProperty]
        public int TiltifyCampaign { get; set; }

        [JsonProperty]
        public OAuthTokenModel TipeeeStreamOAuthToken { get; set; }

        [JsonProperty]
        public OAuthTokenModel TreatStreamOAuthToken { get; set; }

        [JsonProperty]
        public OAuthTokenModel StreamJarOAuthToken { get; set; }

        [JsonProperty]
        public OAuthTokenModel PatreonOAuthToken { get; set; }
        [JsonProperty]
        public string PatreonTierMixerSubscriberEquivalent { get; set; }

        [JsonProperty]
        public OAuthTokenModel IFTTTOAuthToken { get; set; }

        [JsonProperty]
        public OAuthTokenModel StreamlootsOAuthToken { get; set; }

        [JsonProperty]
        public OAuthTokenModel JustGivingOAuthToken { get; set; }
        [JsonProperty]
        public string JustGivingPageShortName { get; set; }

        [JsonProperty]
        public int ExtraLifeTeamID { get; set; }
        [JsonProperty]
        public int ExtraLifeParticipantID { get; set; }
        [JsonProperty]
        public bool ExtraLifeIncludeTeamDonations { get; set; }

        [JsonProperty]
        public string OvrStreamServerIP { get; set; }

        [JsonProperty]
        public string OBSStudioServerIP { get; set; }
        [JsonProperty]
        public string OBSStudioServerPassword { get; set; }

        [JsonProperty]
        public bool EnableStreamlabsOBSConnection { get; set; }

        [JsonProperty]
        public bool EnableXSplitConnection { get; set; }
        [JsonProperty]
        public bool EnableDeveloperAPI { get; set; }

        // Timers
        [JsonProperty]
        public int TimerCommandsInterval { get; set; } = 10;
        [JsonProperty]
        public int TimerCommandsMinimumMessages { get; set; } = 10;
        [JsonProperty]
        public bool DisableAllTimers { get; set; }

        // MixPlay
        [JsonProperty]
        public uint DefaultMixPlayGame { get; set; }
        [JsonProperty]
        public bool PreventUnknownMixPlayUsers { get; set; }
        [JsonProperty]
        public bool PreventSmallerMixPlayCooldowns { get; set; }
        [JsonProperty]
        public List<MixPlaySharedProjectModel> CustomMixPlayProjectIDs { get; set; } = new List<MixPlaySharedProjectModel>();
        [JsonProperty]
        public Dictionary<uint, JObject> CustomMixPlaySettings { get; set; } = new Dictionary<uint, JObject>();
        [JsonProperty]
        public Dictionary<uint, List<MixPlayUserGroupModel>> MixPlayUserGroups { get; set; } = new Dictionary<uint, List<MixPlayUserGroupModel>>();

        // Quotes
        [JsonProperty]
        public bool QuotesEnabled { get; set; }
        [JsonProperty]
        public string QuotesFormat { get; set; }

        // Game Queue
        [JsonProperty]
        public bool GameQueueSubPriority { get; set; }
        [JsonProperty]
        public RequirementViewModel GameQueueRequirements { get; set; } = new RequirementViewModel();
        [JsonProperty]
        public CustomCommand GameQueueUserJoinedCommand { get; set; }
        [JsonProperty]
        public CustomCommand GameQueueUserSelectedCommand { get; set; }

        // Giveaway
        [JsonProperty]
        public string GiveawayCommand { get; set; } = "giveaway";
        [JsonProperty]
        public int GiveawayTimer { get; set; } = 1;
        [JsonProperty]
        public int GiveawayMaximumEntries { get; set; } = 1;
        [JsonProperty]
        public RequirementViewModel GiveawayRequirements { get; set; } = new RequirementViewModel();
        [JsonProperty]
        public int GiveawayReminderInterval { get; set; } = 5;
        [JsonProperty]
        public bool GiveawayRequireClaim { get; set; } = true;
        [JsonProperty]
        public bool GiveawayAllowPastWinners { get; set; }
        [JsonProperty]
        public CustomCommand GiveawayStartedReminderCommand { get; set; }
        [JsonProperty]
        public CustomCommand GiveawayUserJoinedCommand { get; set; }
        [JsonProperty]
        public CustomCommand GiveawayWinnerSelectedCommand { get; set; }

        // Moderation
        [JsonProperty]
        public bool ModerationUseCommunityFilteredWords { get; set; }
        [JsonProperty]
        public int ModerationFilteredWordsTimeout1MinuteOffenseCount { get; set; }
        [JsonProperty]
        public int ModerationFilteredWordsTimeout5MinuteOffenseCount { get; set; }
        [JsonProperty]
        public MixerRoleEnum ModerationFilteredWordsExcempt { get; set; } = MixerRoleEnum.Mod;
        [JsonProperty]
        public bool ModerationFilteredWordsApplyStrikes { get; set; } = true;
        [JsonProperty]
        public int ModerationCapsBlockCount { get; set; }
        [JsonProperty]
        public bool ModerationCapsBlockIsPercentage { get; set; } = true;
        [JsonProperty]
        public int ModerationPunctuationBlockCount { get; set; }
        [JsonProperty]
        public bool ModerationPunctuationBlockIsPercentage { get; set; } = true;
        [JsonProperty]
        public MixerRoleEnum ModerationChatTextExcempt { get; set; } = MixerRoleEnum.Mod;
        [JsonProperty]
        public bool ModerationChatTextApplyStrikes { get; set; } = true;
        [JsonProperty]
        public bool ModerationBlockLinks { get; set; }
        [JsonProperty]
        public MixerRoleEnum ModerationBlockLinksExcempt { get; set; } = MixerRoleEnum.Mod;
        [JsonProperty]
        public bool ModerationBlockLinksApplyStrikes { get; set; } = true;
        [JsonProperty]
        public ModerationChatInteractiveParticipationEnum ModerationChatInteractiveParticipation { get; set; } = ModerationChatInteractiveParticipationEnum.None;
        [JsonProperty]
        public MixerRoleEnum ModerationChatInteractiveParticipationExcempt { get; set; } = MixerRoleEnum.Mod;
        [JsonProperty]
        public bool ModerationResetStrikesOnLaunch { get; set; }
        [JsonProperty]
        public CustomCommand ModerationStrike1Command { get; set; }
        [JsonProperty]
        public CustomCommand ModerationStrike2Command { get; set; }
        [JsonProperty]
        public CustomCommand ModerationStrike3Command { get; set; }
        [JsonProperty]
        public List<string> FilteredWords { get; set; } = new List<string>();
        [JsonProperty]
        public List<string> BannedWords { get; set; } = new List<string>();

        // Remote
        [JsonProperty]
        public RemoteConnectionAuthenticationTokenModel RemoteHostConnection { get; set; }
        [JsonProperty]
        public List<RemoteConnectionModel> RemoteClientConnections { get; set; } = new List<RemoteConnectionModel>();
        [JsonProperty]
        public List<RemoteProfileModel> RemoteProfiles { get; set; } = new List<RemoteProfileModel>();
        [JsonProperty]
        public Dictionary<Guid, RemoteProfileBoardsModel> RemoteProfileBoards { get; set; } = new Dictionary<Guid, RemoteProfileBoardsModel>();

        // Overlay
        [JsonProperty]
        public bool EnableOverlay { get; set; }
        [JsonProperty]
        public Dictionary<string, int> OverlayCustomNameAndPorts { get; set; } = new Dictionary<string, int>();
        [JsonProperty]
        public string OverlaySourceName { get; set; }
        [JsonProperty]
        public int OverlayWidgetRefreshTime { get; set; } = 5;
        [JsonProperty]
        public List<OverlayWidgetModel> OverlayWidgets { get; set; } = new List<OverlayWidgetModel>();

        // Misc
        [JsonProperty]
        public List<SerialDeviceModel> SerialDevices { get; set; } = new List<SerialDeviceModel>();

        [JsonProperty]
        public Dictionary<string, CommandGroupSettings> CommandGroups { get; set; } = new Dictionary<string, CommandGroupSettings>();
        [JsonProperty]
        public Dictionary<string, HotKeyConfiguration> HotKeys { get; set; } = new Dictionary<string, HotKeyConfiguration>();

        [JsonProperty]
        public StreamingSoftwareTypeEnum DefaultStreamingSoftware { get; set; } = StreamingSoftwareTypeEnum.OBSStudio;
        [JsonProperty]
        public string DefaultAudioOutput { get; set; }

        [JsonProperty]
        public List<string> RecentStreamTitles { get; set; } = new List<string>();
        [DataMember]
        public Dictionary<string, object> LatestSpecialIdentifiersData { get; set; } = new Dictionary<string, object>();

        [JsonProperty]
        public Dictionary<string, int> CooldownGroups { get; set; } = new Dictionary<string, int>();

        public StreamerSettingsV2Model() { }

        public override void Initialize()
        {
            base.Initialize();

            this.GameQueueUserJoinedCommand = CustomCommand.BasicChatCommand("Game Queue Used Joined", "You are #$queueposition in the queue to play.", isWhisper: true);
            this.GameQueueUserSelectedCommand = CustomCommand.BasicChatCommand("Game Queue Used Selected", "It's time to play @$username! Listen carefully for instructions on how to join...");

            this.GiveawayStartedReminderCommand = CustomCommand.BasicChatCommand("Giveaway Started/Reminder", "A giveaway has started for $giveawayitem! Type $giveawaycommand in chat in the next $giveawaytimelimit minute(s) to enter!");
            this.GiveawayUserJoinedCommand = CustomCommand.BasicChatCommand("Giveaway User Joined", "You have been entered into the giveaway, stay tuned to see who wins!", isWhisper: true);
            this.GiveawayWinnerSelectedCommand = CustomCommand.BasicChatCommand("Giveaway Winner Selected", "Congratulations @$username, you won $giveawayitem!");

            this.ModerationStrike1Command = CustomCommand.BasicChatCommand("Moderation Strike 1", "$moderationreason. You have received a moderation strike & currently have $usermoderationstrikes strike(s)", isWhisper: true);
            this.ModerationStrike2Command = CustomCommand.BasicChatCommand("Moderation Strike 2", "$moderationreason. You have received a moderation strike & currently have $usermoderationstrikes strike(s)", isWhisper: true);
            this.ModerationStrike3Command = CustomCommand.BasicChatCommand("Moderation Strike 3", "$moderationreason. You have received a moderation strike & currently have $usermoderationstrikes strike(s)", isWhisper: true);
        }
    }
}
