﻿using MixItUp.Base.Model.Overlay;
using MixItUp.Base.ViewModel.Controls.Overlay;
using MixItUp.WPF.Util;
using System.Threading.Tasks;

namespace MixItUp.WPF.Controls.Overlay
{
    /// <summary>
    /// Interaction logic for OverlayLeaderboardControl.xaml
    /// </summary>
    public partial class OverlayLeaderboardControl : OverlayItemControl
    {
        private OverlayLeaderboardItemViewModel viewModel;

        public OverlayLeaderboardControl()
        {
            InitializeComponent();

            this.viewModel = new OverlayLeaderboardItemViewModel();
        }

        public OverlayLeaderboardControl(OverlayLeaderboardListItemModel item)
        {
            InitializeComponent();

            this.viewModel = new OverlayLeaderboardItemViewModel(item);
        }

        public override OverlayItemViewModelBase GetViewModel() { return this.viewModel; }

        public override OverlayItemModelBase GetItem()
        {
            return this.viewModel.GetOverlayItem();
        }

        protected override async Task OnLoaded()
        {
            this.TextFontComboBox.ItemsSource = InstalledFonts.GetInstalledFonts();

            this.DataContext = this.viewModel;
            await this.viewModel.OnLoaded();
        }
    }
}
