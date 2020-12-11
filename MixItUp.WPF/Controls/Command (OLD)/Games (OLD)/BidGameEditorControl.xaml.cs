﻿using MixItUp.Base.Commands;
using MixItUp.Base.Model.Currency;
using MixItUp.Base.ViewModel.Games;
using MixItUp.Base.ViewModel.Requirement;
using System.Threading.Tasks;

namespace MixItUp.WPF.Controls.Games
{
    /// <summary>
    /// Interaction logic for BidGameEditorControl.xaml
    /// </summary>
    public partial class BidGameEditorControl : GameEditorControlBase
    {
        private BidGameCommandEditorWindowViewModel viewModel;
        private BidGameCommand existingCommand;

        public BidGameEditorControl(CurrencyModel currency)
        {
            InitializeComponent();

            this.viewModel = new BidGameCommandEditorWindowViewModel(currency);
        }

        public BidGameEditorControl(BidGameCommand command)
        {
            InitializeComponent();

            this.existingCommand = command;
            this.viewModel = new BidGameCommandEditorWindowViewModel(this.existingCommand);
        }

        public override async Task<bool> Validate()
        {
            if (!await this.CommandDetailsControl.Validate())
            {
                return false;
            }
            return await this.viewModel.Validate();
        }

        public override void SaveGameCommand()
        {
            this.viewModel.SaveGameCommand(this.CommandDetailsControl.GameName, this.CommandDetailsControl.ChatTriggers, this.CommandDetailsControl.GetRequirements());
        }

        protected override async Task OnLoaded()
        {
            this.CommandDetailsControl.SetAsMinimumOnly();

            this.DataContext = this.viewModel;

            await this.viewModel.OnLoaded();

            if (this.existingCommand != null)
            {
                this.CommandDetailsControl.SetDefaultValues(this.existingCommand);
            }
            else
            {
                this.CommandDetailsControl.SetDefaultValues("Bid", "bid", CurrencyRequirementTypeEnum.MinimumOnly, 10);
            }

            await base.OnLoaded();
        }
    }
}