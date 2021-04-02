﻿using MixItUp.Base.Services.External;
using MixItUp.Base.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MixItUp.Base.ViewModel.Services
{
    public class TiltifyServiceControlViewModel : ServiceControlViewModelBase
    {
        public ThreadSafeObservableCollection<TiltifyCampaign> Campaigns { get; set; } = new ThreadSafeObservableCollection<TiltifyCampaign>();

        public TiltifyCampaign SelectedCampaign
        {
            get { return this.selectedCampaign; }
            set
            {
                this.selectedCampaign = value;
                this.NotifyPropertyChanged();

                if (this.SelectedCampaign != null)
                {
                    ChannelSession.Settings.TiltifyCampaign = this.SelectedCampaign.ID;
                }
                else
                {
                    ChannelSession.Settings.TiltifyCampaign = 0;
                }
            }
        }
        private TiltifyCampaign selectedCampaign;

        public ICommand LogInCommand { get; set; }
        public ICommand LogOutCommand { get; set; }

        public TiltifyServiceControlViewModel()
            : base(Resources.Tiltify)
        {
            this.LogInCommand = this.CreateCommand(async (parameter) =>
            {
                Result result = await ChannelSession.Services.Tiltify.Connect();
                if (result.Success)
                {
                    this.IsConnected = true;
                    await this.RefreshCampaigns();
                }
                else
                {
                    await this.ShowConnectFailureMessage(result);
                }
            });

            this.LogOutCommand = this.CreateCommand(async (parameter) =>
            {
                await ChannelSession.Services.Tiltify.Disconnect();

                ChannelSession.Settings.TiltifyOAuthToken = null;
                ChannelSession.Settings.TiltifyCampaign = 0;

                this.IsConnected = false;
            });

            this.IsConnected = ChannelSession.Services.Tiltify.IsConnected;
        }

        protected override async Task OnLoadedInternal()
        {
            if (this.IsConnected)
            {
                await this.RefreshCampaigns();
                this.SelectedCampaign = this.Campaigns.FirstOrDefault(c => c.ID == ChannelSession.Settings.TiltifyCampaign);
            }
        }

        public async Task RefreshCampaigns()
        {
            TiltifyUser user = await ChannelSession.Services.Tiltify.GetUser();

            Dictionary<int, TiltifyCampaign> campaignDictionary = new Dictionary<int, TiltifyCampaign>();

            foreach (TiltifyCampaign campaign in await ChannelSession.Services.Tiltify.GetUserCampaigns(user))
            {
                campaignDictionary[campaign.ID] = campaign;
            }

            foreach (TiltifyTeam team in await ChannelSession.Services.Tiltify.GetUserTeams(user))
            {
                foreach (TiltifyCampaign campaign in await ChannelSession.Services.Tiltify.GetTeamCampaigns(team))
                {
                    campaignDictionary[campaign.ID] = campaign;
                }
            }

            this.Campaigns.ClearAndAddRange(campaignDictionary.Values.Where(c => c.Ends > DateTimeOffset.Now));
        }
    }
}
