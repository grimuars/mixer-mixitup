﻿using MixItUp.Base.ViewModel.User;
using StreamingClient.Base.Util;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MixItUp.Base.Actions
{
    [Obsolete]
    public enum ModerationActionTypeEnum
    {
        ChatTimeout,
        PurgeUser,
        [Obsolete]
        InteractiveTimeout,
        BanUser,
        ClearChat,
        AddModerationStrike,
        RemoveModerationStrike,
        UnbanUser,
        ModUser,
        UnmodUser,
        VIPUser,
        UnVIPUser
    }

    [Obsolete]
    [DataContract]
    public class ModerationAction : ActionBase
    {
        private static SemaphoreSlim asyncSemaphore = new SemaphoreSlim(1);

        protected override SemaphoreSlim AsyncSemaphore { get { return ModerationAction.asyncSemaphore; } }

        [DataMember]
        public ModerationActionTypeEnum ModerationType { get; set; }

        [DataMember]
        public string UserName { get; set; }
        [DataMember]
        public string TimeAmount { get; set; }

        [DataMember]
        public string ModerationReason { get; set; }

        public ModerationAction() : base(ActionTypeEnum.Moderation) { }

        public ModerationAction(ModerationActionTypeEnum moderationType, string username, string timeAmount, string moderationReason)
            : this()
        {
            this.ModerationType = moderationType;
            this.UserName = username;
            this.TimeAmount = timeAmount;
            this.ModerationReason = moderationReason;
        }

        protected override Task PerformInternal(UserViewModel user, IEnumerable<string> arguments)
        {
            return Task.FromResult(0);
        }
    }
}
