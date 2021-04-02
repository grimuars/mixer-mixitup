﻿using MixItUp.Base.Model;
using MixItUp.Base.Model.Commands;
using MixItUp.Base.Model.Currency;
using MixItUp.Base.Model.User;
using MixItUp.Base.Services.Twitch;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.Chat;
using MixItUp.Base.ViewModel.User;
using StreamingClient.Base.Util;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MixItUp.Base.Services
{
    public enum EventTypeEnum
    {
        None = 0,

        // Platform-agnostic = 1

        //ChannelStreamStart = 1,
        //ChannelStreamStop = 2,
        //ChannelHosted = 3,

        //ChannelFollowed = 10,
        //ChannelUnfollowed = 11,

        //ChannelSubscribed = 20,
        //ChannelResubscribed = 21,
        //ChannelSubscriptionGifted = 22,

        ChatUserFirstJoin = 50,
        ChatUserPurge = 51,
        ChatUserBan = 52,
        ChatMessageReceived = 53,
        ChatUserJoined = 54,
        ChatUserLeft = 55,
        ChatMessageDeleted = 56,
        ChatUserTimeout = 57,
        ChatWhisperReceived = 58,

        // Mixer = 100

        [Obsolete]
        MixerChannelStreamStart = 100,
        [Obsolete]
        MixerChannelStreamStop = 101,
        [Obsolete]
        MixerChannelHosted = 102,

        [Obsolete]
        MixerChannelFollowed = 110,
        [Obsolete]
        MixerChannelUnfollowed = 111,

        [Obsolete]
        MixerChannelSubscribed = 120,
        [Obsolete]
        MixerChannelResubscribed = 121,
        [Obsolete]
        MixerChannelSubscriptionGifted = 122,

        //MixerChatUserFirstJoin = 150,
        //MixerChatUserPurge = 151,
        //MixerChatUserBan = 152,
        //MixerChatMessageReceived = 153,
        //MixerChatUserJoined = 154,
        //MixerChatUserLeft = 155,
        //MixerChatMessageDeleted = 156,
        //MixerChatUserTimeout = 156,

        [Obsolete]
        MixerChannelSparksUsed = 170,
        [Obsolete]
        MixerChannelEmbersUsed = 171,
        [Obsolete]
        MixerChannelSkillUsed = 172,
        [Obsolete]
        MixerChannelMilestoneReached = 173,
        [Obsolete]
        MixerChannelFanProgressionLevelUp = 174,

        // Twitch = 200

        TwitchChannelStreamStart = 200,
        TwitchChannelStreamStop = 201,
        TwitchChannelHosted = 202,
        TwitchChannelRaided = 203,

        TwitchChannelFollowed = 210,
        TwitchChannelUnfollowed = 211,

        TwitchChannelSubscribed = 220,
        TwitchChannelResubscribed = 221,
        TwitchChannelSubscriptionGifted = 222,
        TwitchChannelMassSubscriptionsGifted = 223,

        //TwitchChatUserFirstJoin = 250,
        //TwitchChatUserPurge = 251,
        //TwitchChatUserBan = 252,
        //TwitchChatMessageReceived = 253,
        //TwitchChatUserJoined = 254,
        //TwitchChatUserLeft = 255,
        //TwitchChatMessageDeleted = 256,

        TwitchChannelBitsCheered = 270,
        TwitchChannelPointsRedeemed = 271,

        // 300

        // External Services = 1000

        StreamlabsDonation = 1000,
        TiltifyDonation = 1020,
        ExtraLifeDonation = 1030,
        TipeeeStreamDonation = 1040,
        TreatStreamDonation = 1050,
        PatreonSubscribed = 1060,
        StreamJarDonation = 1070,
        JustGivingDonation = 1080,
        StreamlootsCardRedeemed = 1090,
        StreamlootsPackPurchased = 1091,
        StreamlootsPackGifted = 1092,
        StreamElementsDonation = 1100,
    }

    public class EventTrigger
    {
        public EventTypeEnum Type { get; set; }
        public StreamingPlatformTypeEnum Platform { get; set; } = StreamingPlatformTypeEnum.None;
        public UserViewModel User { get; set; }
        public List<string> Arguments { get; set; } = new List<string>();
        public Dictionary<string, string> SpecialIdentifiers { get; set; } = new Dictionary<string, string>();
        public UserViewModel TargetUser { get; set; }

        public EventTrigger(EventTypeEnum type)
        {
            this.Type = type;
        }

        public EventTrigger(EventTypeEnum type, UserViewModel user)
            : this(type)
        {
            this.User = user;
            if (this.User != null)
            {
                this.Platform = this.User.Platform;
            }
            else
            {
                this.Platform = StreamingPlatformTypeEnum.All;
            }
        }

        public EventTrigger(EventTypeEnum type, UserViewModel user, Dictionary<string, string> specialIdentifiers)
            : this(type, user)
        {
            this.SpecialIdentifiers = specialIdentifiers;
        }
    }

    public interface IEventService
    {
        ITwitchEventService TwitchEventService { get; }

        Task Initialize(ITwitchEventService twitchEventService);

        EventCommandModel GetEventCommand(EventTypeEnum type);

        bool CanPerformEvent(EventTrigger trigger);

        Task PerformEvent(EventTrigger trigger);
    }

    public class EventService : IEventService
    {
        private static HashSet<EventTypeEnum> singleUseTracking = new HashSet<EventTypeEnum>()
        {
            EventTypeEnum.ChatUserFirstJoin, EventTypeEnum.ChatUserJoined, EventTypeEnum.ChatUserLeft,

            EventTypeEnum.TwitchChannelStreamStart, EventTypeEnum.TwitchChannelStreamStop, EventTypeEnum.TwitchChannelFollowed, EventTypeEnum.TwitchChannelUnfollowed, EventTypeEnum.TwitchChannelHosted, EventTypeEnum.TwitchChannelRaided, EventTypeEnum.TwitchChannelSubscribed, EventTypeEnum.TwitchChannelResubscribed,
        };

        private LockedDictionary<EventTypeEnum, HashSet<Guid>> userEventTracking = new LockedDictionary<EventTypeEnum, HashSet<Guid>>();

        public EventService()
        {
            foreach (EventTypeEnum type in singleUseTracking)
            {
                this.userEventTracking[type] = new HashSet<Guid>();
            }
        }

        public static async Task ProcessDonationEvent(EventTypeEnum type, UserDonationModel donation, Dictionary<string, string> additionalSpecialIdentifiers = null)
        {
            EventTrigger trigger = new EventTrigger(type, donation.User);
            trigger.User.Data.TotalAmountDonated += donation.Amount;

            ChannelSession.Settings.LatestSpecialIdentifiersData[SpecialIdentifierStringBuilder.LatestDonationUserData] = trigger.User.ID;
            ChannelSession.Settings.LatestSpecialIdentifiersData[SpecialIdentifierStringBuilder.LatestDonationAmountData] = donation.AmountText;

            trigger.SpecialIdentifiers = donation.GetSpecialIdentifiers();
            if (additionalSpecialIdentifiers != null)
            {
                foreach (var kvp in additionalSpecialIdentifiers)
                {
                    trigger.SpecialIdentifiers[kvp.Key] = kvp.Value;
                }
            }

            await ChannelSession.Services.Events.PerformEvent(trigger);

            foreach (StreamPassModel streamPass in ChannelSession.Settings.StreamPass.Values)
            {
                if (trigger.User.HasPermissionsTo(streamPass.Permission))
                {
                    streamPass.AddAmount(donation.User.Data, (int)Math.Ceiling(streamPass.DonationBonus * donation.Amount));
                }
            }

            await ChannelSession.Services.Alerts.AddAlert(new AlertChatMessageViewModel(StreamingPlatformTypeEnum.All, trigger.User, string.Format("{0} Donated {1}", trigger.User.DisplayName, donation.AmountText), ChannelSession.Settings.AlertDonationColor));

            try
            {
                GlobalEvents.DonationOccurred(donation);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        public ITwitchEventService TwitchEventService { get; private set; }

        public Task Initialize(ITwitchEventService twitchEventService)
        {
            this.TwitchEventService = twitchEventService;
            return Task.FromResult(0);
        }

        public EventCommandModel GetEventCommand(EventTypeEnum type)
        {
            foreach (EventCommandModel command in ChannelSession.EventCommands)
            {
                if (command.EventType == type)
                {
                    return command;
                }
            }
            return null;
        }

        public bool CanPerformEvent(EventTrigger trigger)
        {
            UserViewModel user = (trigger.User != null) ? trigger.User : ChannelSession.GetCurrentUser();
            if (EventService.singleUseTracking.Contains(trigger.Type) && this.userEventTracking.ContainsKey(trigger.Type))
            {
                return !this.userEventTracking[trigger.Type].Contains(user.ID);
            }
            return true;
        }

        public async Task PerformEvent(EventTrigger trigger)
        {
            if (this.CanPerformEvent(trigger))
            {
                UserViewModel user = trigger.User;
                if (user == null)
                {
                    user = ChannelSession.GetCurrentUser();
                }

                if (this.userEventTracking.ContainsKey(trigger.Type))
                {
                    lock (this.userEventTracking)
                    {
                        this.userEventTracking[trigger.Type].Add(user.ID);
                    }
                }

                EventCommandModel command = this.GetEventCommand(trigger.Type);
                if (command != null)
                {
                    Logger.Log(LogLevel.Debug, $"Performing event trigger: {trigger.Type}");

                    await command.Perform(new CommandParametersModel(user, platform: trigger.Platform, arguments: trigger.Arguments, specialIdentifiers: trigger.SpecialIdentifiers) { TargetUser = trigger.TargetUser });
                }
            }
        }
    }
}
