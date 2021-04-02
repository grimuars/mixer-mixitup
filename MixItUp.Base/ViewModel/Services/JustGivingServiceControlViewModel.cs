﻿using MixItUp.Base.Services.External;
using MixItUp.Base.Util;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MixItUp.Base.ViewModel.Services
{
    public class JustGivingServiceControlViewModel : ServiceControlViewModelBase
    {
        public ThreadSafeObservableCollection<JustGivingFundraiser> Fundraisers { get; set; } = new ThreadSafeObservableCollection<JustGivingFundraiser>();

        public JustGivingFundraiser SelectedFundraiser
        {
            get { return this.selectedFundraiser; }
            set
            {
                this.selectedFundraiser = value;
                this.NotifyPropertyChanged();

                if (this.SelectedFundraiser != null)
                {
                    ChannelSession.Settings.JustGivingPageShortName = this.SelectedFundraiser.pageShortName;
                }
                else
                {
                    ChannelSession.Settings.JustGivingPageShortName = null;
                }
                ChannelSession.Services.JustGiving.SetFundraiser(this.SelectedFundraiser);
            }
        }
        private JustGivingFundraiser selectedFundraiser;

        public ICommand LogInCommand { get; set; }
        public ICommand LogOutCommand { get; set; }

        public JustGivingServiceControlViewModel()
            : base(Resources.JustGiving)
        {
            this.LogInCommand = this.CreateCommand(async (parameter) =>
            {
                Result result = await ChannelSession.Services.JustGiving.Connect();
                if (result.Success)
                {
                    this.IsConnected = true;
                    await this.RefreshFundraisers();
                }
                else
                {
                    await this.ShowConnectFailureMessage(result);
                }
            });

            this.LogOutCommand = this.CreateCommand(async (parameter) =>
            {
                await ChannelSession.Services.JustGiving.Disconnect();

                ChannelSession.Settings.JustGivingOAuthToken = null;
                ChannelSession.Settings.JustGivingPageShortName = null;

                this.IsConnected = false;
            });

            this.IsConnected = ChannelSession.Services.JustGiving.IsConnected;
        }

        protected override async Task OnLoadedInternal()
        {
            if (this.IsConnected)
            {
                await this.RefreshFundraisers();
                this.SelectedFundraiser = this.Fundraisers.FirstOrDefault(f => f.pageShortName.Equals(ChannelSession.Settings.JustGivingPageShortName));
            }
        }

        public async Task RefreshFundraisers()
        {
            IEnumerable<JustGivingFundraiser> fundraisers = await ChannelSession.Services.JustGiving.GetCurrentFundraisers();
            if (fundraisers != null)
            {
                fundraisers = fundraisers.Where(f => f.IsActive);
            }
            this.Fundraisers.ClearAndAddRange(fundraisers);
        }
    }
}
