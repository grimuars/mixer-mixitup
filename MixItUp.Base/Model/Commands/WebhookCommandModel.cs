﻿using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MixItUp.Base.Model.Commands
{
    [DataContract]
    public class WebhookJSONParameter
    {
        [DataMember]
        public string JSONParameterName { get; set; }

        [DataMember]
        public string SpecialIdentifierName { get; set; }
    }

    [DataContract]
    public class WebhookCommandModel : CommandModelBase
    {
        [DataMember]
        public List<WebhookJSONParameter> JSONParameters { get; set; } = new List<WebhookJSONParameter>();

        public WebhookCommandModel(string name) : base(name, CommandTypeEnum.Webhook) { }

        protected WebhookCommandModel() : base() { }
    }
}
