﻿using MixItUp.Base.Model.Actions;
using MixItUp.Base.Model.Commands;
using MixItUp.Base.Model.Currency;
using MixItUp.Base.Model.User;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MixItUp.Base.ViewModel.Currency
{
    public class InventoryWindowViewModel : UIViewModelBase
    {
        public InventoryModel Inventory
        {
            get { return this.inventory; }
            set
            {
                this.inventory = value;
                this.NotifyPropertyChanged();
            }
        }
        private InventoryModel inventory;

        public string Name
        {
            get { return this.name; }
            set
            {
                this.name = value;
                this.NotifyPropertyChanged();
            }
        }
        private string name;
        public int DefaultItemMaxAmount
        {
            get { return this.defaultItemMaxAmount; }
            set
            {
                this.defaultItemMaxAmount = value;
                this.NotifyPropertyChanged();
            }
        }
        private int defaultItemMaxAmount;

        public ThreadSafeObservableCollection<InventoryItemModel> Items { get; private set; } = new ThreadSafeObservableCollection<InventoryItemModel>();

        public InventoryItemModel SelectedItem
        {
            get { return this.selectedItem; }
            set
            {
                this.selectedItem = value;
                this.NotifyPropertyChanged();
                this.NotifyPropertyChanged("SaveItemButtonText");

                if (this.SelectedItem == null)
                {
                    this.ItemName = null;
                    this.ItemMaxAmount = 0;
                    this.ItemBuyAmount = 0;
                    this.ItemSellAmount = 0;
                }
                else
                {
                    this.ItemName = this.SelectedItem.Name;
                    this.ItemMaxAmount = (this.SelectedItem.HasMaxAmount) ? this.SelectedItem.MaxAmount : 0;
                    this.ItemBuyAmount = (this.SelectedItem.HasBuyAmount) ? this.SelectedItem.BuyAmount : 0;
                    this.ItemSellAmount = (this.SelectedItem.HasSellAmount) ? this.SelectedItem.SellAmount : 0;
                }
            }
        }
        private InventoryItemModel selectedItem;
        public string ItemName
        {
            get { return this.itemName; }
            set
            {
                this.itemName = value;
                this.NotifyPropertyChanged();
            }
        }
        private string itemName;
        public int ItemMaxAmount
        {
            get { return this.itemMaxAmount; }
            set
            {
                this.itemMaxAmount = value;
                this.NotifyPropertyChanged();
            }
        }
        private int itemMaxAmount;
        public int ItemBuyAmount
        {
            get { return this.itemBuyAmount; }
            set
            {
                this.itemBuyAmount = value;
                this.NotifyPropertyChanged();
            }
        }
        private int itemBuyAmount;
        public int ItemSellAmount
        {
            get { return this.itemSellAmount; }
            set
            {
                this.itemSellAmount = value;
                this.NotifyPropertyChanged();
            }
        }
        private int itemSellAmount;
        public string SaveItemButtonText { get { return (this.SelectedItem != null) ? MixItUp.Base.Resources.Update : MixItUp.Base.Resources.AddItem; } }
        public ICommand SaveItemCommand { get; private set; }
        public ICommand DeleteItemCommand { get; private set; }

        public ICommand ManualResetCommand { get; private set; }

        public bool ShopEnabled
        {
            get { return this.shopEnabled; }
            set
            {
                this.shopEnabled = value;
                this.NotifyPropertyChanged();
            }
        }
        private bool shopEnabled;
        public string ShopCommandText
        {
            get { return this.shopCommandText; }
            set
            {
                this.shopCommandText = value;
                this.NotifyPropertyChanged();
            }
        }
        private string shopCommandText = "!shop";
        public CommandModelBase ShopBuyCommand
        {
            get { return this.shopBuyCommand; }
            set
            {
                this.shopBuyCommand = value;
                this.NotifyPropertyChanged();
            }
        }
        private CommandModelBase shopBuyCommand;
        public CommandModelBase ShopSellCommand
        {
            get { return this.shopSellCommand; }
            set
            {
                this.shopSellCommand = value;
                this.NotifyPropertyChanged();
            }
        }
        private CommandModelBase shopSellCommand;

        public bool TradeEnabled
        {
            get { return this.tradeEnabled; }
            set
            {
                this.tradeEnabled = value;
                this.NotifyPropertyChanged();
            }
        }
        private bool tradeEnabled;
        public string TradeCommandText
        {
            get { return this.tradeCommandText; }
            set
            {
                this.tradeCommandText = value;
                this.NotifyPropertyChanged();
            }
        }
        private string tradeCommandText = "!trade";
        public CommandModelBase TradeCommand
        {
            get { return this.tradeCommand; }
            set
            {
                this.tradeCommand = value;
                this.NotifyPropertyChanged();
            }
        }
        private CommandModelBase tradeCommand;

        public IEnumerable<CurrencyModel> Currencies { get { return ChannelSession.Settings.Currency.Values.ToList(); } }
        public CurrencyModel SelectedShopCurrency
        {
            get { return this.selectedShopCurrency; }
            set
            {
                this.selectedShopCurrency = value;
                this.NotifyPropertyChanged();
            }
        }
        private CurrencyModel selectedShopCurrency;

        public ICommand HelpCommand { get; private set; }

        private Dictionary<UserDataModel, int> userImportData = new Dictionary<UserDataModel, int>();

        public InventoryWindowViewModel(InventoryModel inventory)
            : this()
        {
            this.Inventory = inventory;

            this.Name = this.Inventory.Name;
            this.DefaultItemMaxAmount = this.Inventory.DefaultMaxAmount;

            this.Items.AddRange(this.Inventory.Items.Values);

            this.ShopEnabled = this.Inventory.ShopEnabled;
            this.ShopCommandText = this.Inventory.ShopCommand;
            this.SelectedShopCurrency = this.Currencies.FirstOrDefault(c => c.ID.Equals(this.Inventory.ShopCurrencyID));
            this.ShopBuyCommand = this.Inventory.ItemsBoughtCommand;
            this.ShopSellCommand = this.Inventory.ItemsSoldCommand;

            this.TradeEnabled = this.Inventory.TradeEnabled;
            this.TradeCommandText = this.Inventory.TradeCommand;
            this.TradeCommand = this.Inventory.ItemsTradedCommand;
        }

        public InventoryWindowViewModel()
        {
            this.DefaultItemMaxAmount = 99;

            CustomCommandModel buyCommand = new CustomCommandModel(MixItUp.Base.Resources.InventoryItemsBoughtCommandName);
            buyCommand.Actions.Add(new ChatActionModel("You bought $itemtotal $itemname for $itemcost $currencyname", sendAsStreamer: false));
            this.ShopBuyCommand = buyCommand;
            CustomCommandModel sellCommand = new CustomCommandModel(MixItUp.Base.Resources.InventoryItemsSoldCommandName);
            sellCommand.Actions.Add(new ChatActionModel("You sold $itemtotal $itemname for $itemcost $currencyname", sendAsStreamer: false));
            this.ShopSellCommand = sellCommand;

            CustomCommandModel tradeCommand = new CustomCommandModel(MixItUp.Base.Resources.InventoryItemsTradedCommandName);
            tradeCommand.Actions.Add(new ChatActionModel("@$username traded $itemtotal $itemname to @$targetusername for $targetitemtotal $targetitemname", sendAsStreamer: false));
            this.TradeCommand = tradeCommand;

            this.SaveItemCommand = this.CreateCommand(async (parameter) =>
            {
                if (string.IsNullOrEmpty(this.ItemName))
                {
                    await DialogHelper.ShowMessage("You must specify a name for the item");
                    return;
                }

                if (this.ItemMaxAmount < 0)
                {
                    await DialogHelper.ShowMessage("The item max amount must be either blank or a number greater than 0");
                    return;
                }

                if (this.ItemBuyAmount < 0)
                {
                    await DialogHelper.ShowMessage("The item buy amount must be either blank or a number greater than 0");
                    return;
                }

                if (this.ItemSellAmount < 0)
                {
                    await DialogHelper.ShowMessage("The item sell amount must be either blank or a number greater than 0");
                    return;
                }

                if (this.SelectedItem == null)
                {
                    InventoryItemModel existingItem = this.Items.FirstOrDefault(i => i.Name.Equals(this.ItemName, StringComparison.CurrentCultureIgnoreCase));
                    if (existingItem != null)
                    {
                        await DialogHelper.ShowMessage("An item with the same name already exists");
                        return;
                    }

                    this.Items.Add(new InventoryItemModel(this.ItemName, maxAmount: this.ItemMaxAmount, buyAmount: this.ItemBuyAmount, sellAmount: this.ItemSellAmount));
                }
                else
                {
                    this.SelectedItem.Name = this.ItemName;
                    this.SelectedItem.MaxAmount = this.ItemMaxAmount;
                    this.SelectedItem.BuyAmount = this.ItemBuyAmount;
                    this.SelectedItem.SellAmount = this.ItemSellAmount;

                    this.Items.Remove(this.SelectedItem);
                    this.Items.Add(this.SelectedItem);
                }

                this.SelectedItem = null;
            });

            this.ManualResetCommand = this.CreateCommand(async (parameter) =>
            {
                if (this.Inventory != null)
                {
                    if (await DialogHelper.ShowConfirmation(Resources.ResetAllItemPrompt))
                    {
                        await this.Inventory.Reset();
                    }
                }
            });

            this.HelpCommand = this.CreateCommand((parameter) =>
            {
                ProcessHelper.LaunchLink("https://github.com/SaviorXTanren/mixer-mixitup/wiki/Currency,-Rank,-&-Inventory");
                return Task.FromResult(0);
            });
        }

        public async Task<bool> Validate()
        {
            if (string.IsNullOrEmpty(this.Name))
            {
                await DialogHelper.ShowMessage("An inventory name must be specified");
                return false;
            }

            InventoryModel dupeInventory = ChannelSession.Settings.Inventory.Values.FirstOrDefault(c => c.Name.Equals(this.Name));
            if (dupeInventory != null && (this.inventory == null || !this.inventory.ID.Equals(dupeInventory.ID)))
            {
                await DialogHelper.ShowMessage("There already exists an inventory with this name");
                return false;
            }

            CurrencyModel dupeCurrency = ChannelSession.Settings.Currency.Values.FirstOrDefault(c => c.Name.Equals(this.Name));
            if (dupeCurrency != null)
            {
                await DialogHelper.ShowMessage("There already exists a currency or rank system with this name");
                return false;
            }

            if (this.DefaultItemMaxAmount <= 0)
            {
                await DialogHelper.ShowMessage("The default max amount must be greater than 0");
                return false;
            }

            if (this.Items.Count() == 0)
            {
                await DialogHelper.ShowMessage("At least 1 item must be added");
                return false;
            }

            if (this.ShopEnabled)
            {
                if (string.IsNullOrEmpty(this.ShopCommandText))
                {
                    await DialogHelper.ShowMessage("A command name must be specified for the shop");
                    return false;
                }

                if (this.SelectedShopCurrency == null)
                {
                    await DialogHelper.ShowMessage("A currency must be specified for the shop");
                    return false;
                }
            }

            if (this.TradeEnabled)
            {
                if (string.IsNullOrEmpty(this.TradeCommandText))
                {
                    await DialogHelper.ShowMessage("A command name must be specified for trading");
                    return false;
                }
            }

            return true;
        }

        public async Task Save()
        {
            if (this.inventory == null)
            {
                this.inventory = new InventoryModel();
                ChannelSession.Settings.Inventory[this.inventory.ID] = this.inventory;
            }

            this.inventory.Name = this.Name;
            this.inventory.DefaultMaxAmount = this.DefaultItemMaxAmount;
            this.inventory.SpecialIdentifier = SpecialIdentifierStringBuilder.ConvertToSpecialIdentifier(this.inventory.Name);
            this.inventory.Items = new Dictionary<Guid, InventoryItemModel>(this.Items.ToDictionary(i => i.ID, i => i));

            this.inventory.ShopEnabled = this.ShopEnabled;
            this.inventory.ShopCommand = this.ShopCommandText;
            if (this.SelectedShopCurrency != null)
            {
                this.inventory.ShopCurrencyID = this.SelectedShopCurrency.ID;
            }
            this.inventory.ItemsBoughtCommand = this.ShopBuyCommand;
            this.inventory.ItemsSoldCommand = this.ShopSellCommand;

            this.inventory.TradeEnabled = this.TradeEnabled;
            this.inventory.TradeCommand = this.TradeCommandText;
            this.inventory.ItemsTradedCommand = this.TradeCommand;

            await ChannelSession.SaveSettings();
        }
    }
}
