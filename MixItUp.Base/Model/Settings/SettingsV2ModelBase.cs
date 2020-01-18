using MixItUp.Base.Services;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.Window.Dashboard;
using Newtonsoft.Json;
using StreamingClient.Base.Model.OAuth;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace MixItUp.Base.Model.Settings
{
    [DataContract]
    public abstract class SettingsV2ModelBase
    {
        public const int LatestVersion = 1;
        [DataMember]
        public int Version { get; set; } = SettingsV2ModelBase.LatestVersion;

        [DataMember]
        public Guid ID { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public bool IsStreamer { get; set; }

        [DataMember]
        public string SettingsBackupLocation { get; set; }
        [DataMember]
        public SettingsBackupRateEnum SettingsBackupRate { get; set; } = SettingsBackupRateEnum.None;
        [DataMember]
        public DateTimeOffset SettingsLastBackup { get; set; } = DateTimeOffset.MinValue;

        [DataMember]
        public bool OptOutTracking { get; set; }
        [DataMember]
        public string TelemetryUserId { get; set; }

        [DataMember]
        public bool DiagnosticLogging { get; set; }

        // Channel Authentication

        [DataMember]
        public OAuthTokenModel MixerUserOAuthToken { get; set; }
        [DataMember]
        public OAuthTokenModel TwitchUserOAuthToken { get; set; }

        [DataMember]
        public string MixerChannelID { get; set; }
        [DataMember]
        public string TwitchChannelID { get; set; }

        // Dashboard

        [DataMember]
        public DashboardLayoutTypeEnum DashboardLayout { get; set; } = DashboardLayoutTypeEnum.One;
        [DataMember]
        public List<DashboardItemTypeEnum> DashboardItems { get; set; } = new List<DashboardItemTypeEnum>();
        [DataMember]
        public List<Guid> DashboardQuickCommands { get; set; } = new List<Guid>();

        // Settings Menu

        [DataMember]
        public int ChatFontSize { get; set; } = 13;
        [DataMember]
        public bool ChatShowUserJoinLeave { get; set; }
        [DataMember]
        public string ChatUserJoinLeaveColorScheme { get; set; } = ColorSchemes.DefaultColorScheme;
        [DataMember]
        public bool ChatShowEventAlerts { get; set; }
        [DataMember]
        public string ChatEventAlertsColorScheme { get; set; } = ColorSchemes.DefaultColorScheme;
        [DataMember]
        public bool ChatShowMixPlayAlerts { get; set; }
        [DataMember]
        public string ChatMixPlayAlertsColorScheme { get; set; } = ColorSchemes.DefaultColorScheme;
        [DataMember]
        public bool SaveChatEventLogs { get; set; }
        [DataMember]
        public bool WhisperAllAlerts { get; set; }
        [DataMember]
        public bool OnlyShowAlertsInDashboard { get; set; }
        [DataMember]
        public bool LatestChatAtTop { get; set; }
        [DataMember]
        public bool HideViewerAndChatterNumbers { get; set; }
        [DataMember]
        public bool HideChatUserList { get; set; }
        [DataMember]
        public bool HideDeletedMessages { get; set; }
        [DataMember]
        public bool TrackWhispererNumber { get; set; }
        [DataMember]
        public bool AllowCommandWhispering { get; set; }
        [DataMember]
        public bool IgnoreBotAccountCommands { get; set; }
        [DataMember]
        public bool CommandsOnlyInYourStream { get; set; }
        [DataMember]
        public bool DeleteChatCommandsWhenRun { get; set; }
        [DataMember]
        public bool ShowMixrElixrEmotes { get; set; }
        [DataMember]
        public bool ShowChatMessageTimestamps { get; set; }

        [DataMember]
        public string NotificationChatMessageSoundFilePath { get; set; }
        [DataMember]
        public int NotificationChatMessageSoundVolume { get; set; } = 100;
        [DataMember]
        public string NotificationChatTaggedSoundFilePath { get; set; }
        [DataMember]
        public int NotificationChatTaggedSoundVolume { get; set; } = 100;
        [DataMember]
        public string NotificationChatWhisperSoundFilePath { get; set; }
        [DataMember]
        public int NotificationChatWhisperSoundVolume { get; set; } = 100;
        [DataMember]
        public string NotificationServiceConnectSoundFilePath { get; set; }
        [DataMember]
        public int NotificationServiceConnectSoundVolume { get; set; } = 100;
        [DataMember]
        public string NotificationServiceDisconnectSoundFilePath { get; set; }
        [DataMember]
        public int NotificationServiceDisconnectSoundVolume { get; set; } = 100;

        [DataMember]
        public int MaxMessagesInChat { get; set; } = 100;
        [DataMember]
        public int MaxUsersShownInChat { get; set; } = 100;

        public SettingsV2ModelBase() { }

        [JsonIgnore]
        public string FileName { get { return this.ID + SettingsV2Service.SettingsFileExtension; } }
        [JsonIgnore]
        public string BackupFileName { get { return this.FileName + SettingsV2Service.LocalBackupFileExtension; } }

        public virtual void Initialize()
        {
            this.ID = Guid.NewGuid();

            this.DashboardItems = new List<DashboardItemTypeEnum>() { DashboardItemTypeEnum.None, DashboardItemTypeEnum.None, DashboardItemTypeEnum.None, DashboardItemTypeEnum.None };
            this.DashboardQuickCommands = new List<Guid>() { Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty, Guid.Empty };
        }

        public virtual Task CopyLatestValues()
        {
            if (ChannelSession.MixerUserConnection != null)
            {
                this.MixerUserOAuthToken = ChannelSession.MixerUserConnection.Connection.GetOAuthTokenCopy();
            }
            if (ChannelSession.TwitchUserConnection != null)
            {
                this.TwitchUserOAuthToken = ChannelSession.TwitchUserConnection.Connection.GetOAuthTokenCopy();
            }
            return Task.FromResult(0);
        }
    }
}
