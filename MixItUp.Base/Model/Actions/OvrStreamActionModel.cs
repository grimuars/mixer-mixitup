﻿using MixItUp.Base.ViewModel.User;
using StreamingClient.Base.Util;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MixItUp.Base.Model.Actions
{
    public enum OvrStreamActionTypeEnum
    {
        [Name("Hide Title")]
        HideTitle,
        [Name("Play Title")]
        PlayTitle,
        [Name("Update Variables")]
        UpdateVariables,
        [Name("Enable Title")]
        EnableTitle,
        [Name("Disable Title")]
        DisableTitle,
    }

    [DataContract]
    public class OvrStreamActionModel : ActionModelBase
    {
        private static SemaphoreSlim asyncSemaphore = new SemaphoreSlim(1);

        protected override SemaphoreSlim AsyncSemaphore { get { return OvrStreamActionModel.asyncSemaphore; } }

        [DataMember]
        public OvrStreamActionTypeEnum ActionType { get; set; }

        [DataMember]
        public string TitleName { get; set; }

        [DataMember]
        public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();

        public OvrStreamActionModel(OvrStreamActionTypeEnum actionType, string titleName = null, Dictionary<string, string> variables = null)
            : base(ActionTypeEnum.OvrStream)
        {
            this.ActionType = actionType;
            this.TitleName = titleName;
            this.Variables = variables;
        }

        internal OvrStreamActionModel(MixItUp.Base.Actions.OvrStreamAction action)
            : base(ActionTypeEnum.OvrStream)
        {
            this.ActionType = (OvrStreamActionTypeEnum)(int)action.OvrStreamActionType;
            this.TitleName = action.TitleName;
            this.Variables = action.Variables;
        }

        protected override async Task PerformInternal(UserViewModel user, StreamingPlatformTypeEnum platform, IEnumerable<string> arguments, Dictionary<string, string> specialIdentifiers)
        {
            if (ChannelSession.Services.OvrStream.IsConnected)
            {
                if (this.ActionType == OvrStreamActionTypeEnum.UpdateVariables ||
                    this.ActionType == OvrStreamActionTypeEnum.PlayTitle)
                {
                    Dictionary<string, string> processedVariables = new Dictionary<string, string>();
                    foreach (var kvp in this.Variables)
                    {
                        processedVariables[kvp.Key] = await this.ReplaceStringWithSpecialModifiers(kvp.Value, user, platform, arguments, specialIdentifiers);

                        // Since OvrStream doesn't support URI based images, we need to trigger a download and get the path to those files
                        if (processedVariables[kvp.Key].StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                        {
                            string path = await ChannelSession.Services.OvrStream.DownloadImage(processedVariables[kvp.Key]);
                            if (path != null)
                            {
                                processedVariables[kvp.Key] = path;
                            }
                        }
                    }

                    switch (this.ActionType)
                    {
                        case OvrStreamActionTypeEnum.UpdateVariables:
                            await ChannelSession.Services.OvrStream.UpdateVariables(this.TitleName, processedVariables);
                            break;
                        case OvrStreamActionTypeEnum.PlayTitle:
                            await ChannelSession.Services.OvrStream.PlayTitle(this.TitleName, processedVariables);
                            break;
                    }
                }
                else if (this.ActionType == OvrStreamActionTypeEnum.HideTitle)
                {
                    await ChannelSession.Services.OvrStream.HideTitle(this.TitleName);
                }
                else if (this.ActionType == OvrStreamActionTypeEnum.EnableTitle)
                {
                    await ChannelSession.Services.OvrStream.EnableTitle(this.TitleName);
                }
                else if (this.ActionType == OvrStreamActionTypeEnum.DisableTitle)
                {
                    await ChannelSession.Services.OvrStream.DisableTitle(this.TitleName);
                }
            }
        }
    }
}
