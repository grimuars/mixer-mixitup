﻿using MixItUp.Base.Services.External;
using MixItUp.Base.ViewModel.User;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MixItUp.Base.Model.Actions
{
    public enum DiscordActionTypeEnum
    {
        SendMessage,
        MuteSelf,
        DeafenSelf,
    }

    [DataContract]
    public class DiscordActionModel : ActionModelBase
    {
        private static SemaphoreSlim asyncSemaphore = new SemaphoreSlim(1);

        protected override SemaphoreSlim AsyncSemaphore { get { return DiscordActionModel.asyncSemaphore; } }

        public static DiscordActionModel CreateForChatMessage(DiscordChannel channel, string message, string filePath) { return new DiscordActionModel(DiscordActionTypeEnum.SendMessage) { ChannelID = channel.ID, MessageText = message, FilePath = filePath }; }

        public static DiscordActionModel CreateForMuteSelf(bool mute) { return new DiscordActionModel(DiscordActionTypeEnum.MuteSelf) { ShouldMuteDeafen = mute }; }

        public static DiscordActionModel CreateForDeafenSelf(bool deafen) { return new DiscordActionModel(DiscordActionTypeEnum.DeafenSelf) { ShouldMuteDeafen = deafen }; }

        [DataMember]
        public DiscordActionTypeEnum ActionType { get; set; }

        [DataMember]
        public string ChannelID { get; set; }

        [DataMember]
        public string MessageText { get; set; }
        [DataMember]
        public string FilePath { get; set; }

        [DataMember]
        public bool ShouldMuteDeafen { get; set; }

        private DiscordChannel channel;

        public DiscordActionModel(DiscordActionTypeEnum actionType)
            : base(ActionTypeEnum.Discord)
        {
            this.ActionType = actionType;
        }

        internal DiscordActionModel(MixItUp.Base.Actions.DiscordAction action)
            : base(ActionTypeEnum.Discord)
        {
            this.ActionType = (DiscordActionTypeEnum)(int)action.DiscordType;
            this.ChannelID = action.SendMessageChannelID;
            this.MessageText = action.SendMessageText;
            this.FilePath = action.FilePath;
            this.ShouldMuteDeafen = action.ShouldMuteDeafen;
        }

        protected override async Task PerformInternal(UserViewModel user, StreamingPlatformTypeEnum platform, IEnumerable<string> arguments, Dictionary<string, string> specialIdentifiers)
        {
            if (this.ActionType == DiscordActionTypeEnum.SendMessage)
            {
                if (this.channel == null)
                {
                    this.channel = await ChannelSession.Services.Discord.GetChannel(this.ChannelID);
                }

                if (this.channel != null)
                {
                    string message = await this.ReplaceStringWithSpecialModifiers(this.MessageText, user, platform, arguments, specialIdentifiers);
                    await ChannelSession.Services.Discord.CreateMessage(this.channel, message, this.FilePath);
                }
            }
            else if (this.ActionType == DiscordActionTypeEnum.MuteSelf)
            {
                await ChannelSession.Services.Discord.MuteServerMember(ChannelSession.Services.Discord.Server, ChannelSession.Services.Discord.User, this.ShouldMuteDeafen);
            }
            else if (this.ActionType == DiscordActionTypeEnum.DeafenSelf)
            {
                await ChannelSession.Services.Discord.DeafenServerMember(ChannelSession.Services.Discord.Server, ChannelSession.Services.Discord.User, this.ShouldMuteDeafen);
            }
        }
    }
}