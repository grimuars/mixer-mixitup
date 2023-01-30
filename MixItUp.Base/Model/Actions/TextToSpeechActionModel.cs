﻿using MixItUp.Base.Model.Commands;
using MixItUp.Base.Services;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace MixItUp.Base.Model.Actions
{
    [DataContract]
    public class TextToSpeechActionModel : ActionModelBase
    {
        public static readonly IEnumerable<string> AvailableVoices = new List<string>()
        {
            "Afrikaans Male",
            "Albanian Male",
            "Arabic Female",
            "Arabic Male",
            "Armenian Male",
            "Australian Female",
            "Australian Male",
            "Bangla Bangladesh Female",
            "Bangla Bangladesh Male",
            "Bangla India Female",
            "Bangla India Male",
            "Bosnian Male",
            "Brazilian Portuguese Female",
            "Catalan Male",
            "Chinese (Hong Kong) Female",
            "Chinese (Hong Kong) Male",
            "Chinese Female",
            "Chinese Male",
            "Chinese Taiwan Female",
            "Chinese Taiwan Male",
            "Croatian Male",
            "Czech Female",
            "Danish Female",
            "Deutsch Female",
            "Deutsch Male",
            "Dutch Female",
            "Dutch Male",
            "Esperanto Male",
            "Estonian Male",
            "Fallback UK Female",
            "Filipino Female",
            "Finnish Female",
            "French Canadian Female",
            "French Canadian Male",
            "French Female",
            "French Male",
            "Greek Female",
            "Hindi Female",
            "Hindi Male",
            "Hungarian Female",
            "Icelandic Female",
            "Indonesian Female",
            "Indonesian Male",
            "Italian Female",
            "Italian Male",
            "Japanese Female",
            "Japanese Male",
            "Korean Female",
            "Korean Male",
            "Latin Male",
            "Latvian Male",
            "Macedonian Male",
            "Moldavian Female",
            "Montenegrin Male",
            "Nepali",
            "Norwegian Female",
            "Norwegian Male",
            "Polish Female",
            "Polish Male",
            "Portuguese Female",
            "Portuguese Male",
            "Romanian Female",
            "Russian Female",
            "Serbian Male",
            "Serbo-Croatian Male",
            "Sinhala",
            "Slovak Female",
            "Spanish Female",
            "Spanish Latin American Female",
            "Spanish Latin American Male",
            "Swahili Male",
            "Swedish Female",
            "Swedish Male",
            "Tamil Female",
            "Tamil Male",
            "Thai Female",
            "Thai Male",
            "Turkish Female",
            "Turkish Male",
            "UK English Female",
            "UK English Male",
            "Ukrainian Female",
            "US English Female",
            "US English Male",
            "Vietnamese Female",
            "Vietnamese Male",
            "Welsh Male",
        };

        [DataMember]
        public string Text { get; set; }

        [DataMember]
        public string Voice { get; set; }

        [DataMember]
        public int Volume { get; set; }
        [DataMember]
        public int Pitch { get; set; }
        [DataMember]
        public int Rate { get; set; }

        public TextToSpeechActionModel(string text, string voice, int volume, int pitch, int rate)
            : base(ActionTypeEnum.TextToSpeech)
        {
            this.Text = text;
            this.Voice = voice;
            this.Volume = volume;
            this.Pitch = pitch;
            this.Rate = rate;
        }

        [Obsolete]
        public TextToSpeechActionModel() { }

        protected override async Task PerformInternal(CommandParametersModel parameters)
        {
            OverlayEndpointService overlay = ServiceManager.Get<OverlayService>().GetOverlay(ServiceManager.Get<OverlayService>().DefaultOverlayName);
            if (overlay != null)
            {
                string message = await ReplaceStringWithSpecialModifiers(this.Text, parameters);
                await overlay.SendTextToSpeech(new OverlayTextToSpeech() { Text = message, Voice = this.Voice, Volume = this.Volume / 100.0, Pitch = this.Pitch / 100.0, Rate = this.Rate / 100.0 });
            }
        }
    }
}
