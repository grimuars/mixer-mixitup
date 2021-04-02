﻿using MixItUp.Base.Model.Currency;
using MixItUp.Base.Model.User;
using MixItUp.Base.ViewModel.User;
using Newtonsoft.Json;
using StreamingClient.Base.Util;
using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace MixItUp.Base.ViewModel.Requirement
{
    [Obsolete]
    public enum CurrencyRequirementTypeEnum
    {
        NoCurrencyCost,
        MinimumOnly,
        MinimumAndMaximum,
        RequiredAmount
    }

    [Obsolete]
    [DataContract]
    public class CurrencyRequirementViewModel : IEquatable<CurrencyRequirementViewModel>
    {
        [DataMember]
        public Guid CurrencyID { get; set; }

        [DataMember]
        public int RequiredAmount { get; set; }
        [DataMember]
        public int MaximumAmount { get; set; }
        [DataMember]
        public CurrencyRequirementTypeEnum RequirementType { get; set; }

        [DataMember]
        public string RankName { get; set; }
        [DataMember]
        public bool MustEqual { get; set; }

        public CurrencyRequirementViewModel()
        {
            this.RequirementType = CurrencyRequirementTypeEnum.RequiredAmount;
        }

        public CurrencyRequirementViewModel(CurrencyModel currency, int amount)
            : this(currency, CurrencyRequirementTypeEnum.RequiredAmount, amount)
        { }

        public CurrencyRequirementViewModel(CurrencyModel currency, int minimumAmount, int maximumAmount)
            : this(currency, CurrencyRequirementTypeEnum.MinimumAndMaximum, minimumAmount)
        {
            this.MaximumAmount = maximumAmount;
        }

        public CurrencyRequirementViewModel(CurrencyModel currency, CurrencyRequirementTypeEnum requirementType, int amount)
        {
            this.CurrencyID = currency.ID;
            this.RequiredAmount = amount;
            this.RequirementType = requirementType;
        }

        public CurrencyRequirementViewModel(CurrencyModel currency, RankModel rank, bool mustEqual = false)
        {
            this.CurrencyID = currency.ID;
            this.RankName = rank.Name;
            this.MustEqual = mustEqual;
        }

        [JsonIgnore]
        public RankModel RequiredRank
        {
            get
            {
                CurrencyModel currency = this.GetCurrency();
                if (currency != null)
                {
                    RankModel rank = currency.Ranks.FirstOrDefault(r => r.Name.Equals(this.RankName));
                    if (rank != null)
                    {
                        return rank;
                    }
                }
                return CurrencyModel.NoRank;
            }
        }

        public CurrencyModel GetCurrency()
        {
            if (ChannelSession.Settings.Currency.ContainsKey(this.CurrencyID))
            {
                return ChannelSession.Settings.Currency[this.CurrencyID];
            }
            return null;
        }

        public bool TrySubtractAmount(UserDataModel userData, bool requireAmount = false) { return this.TrySubtractAmount(userData, this.RequiredAmount, requireAmount); }

        public bool TrySubtractAmount(UserDataModel userData, int amount, bool requireAmount = false)
        {
            if (this.DoesMeetCurrencyRequirement(amount))
            {
                CurrencyModel currency = this.GetCurrency();
                if (currency == null)
                {
                    return false;
                }

                if (requireAmount && !currency.HasAmount(userData, amount))
                {
                    return false;
                }

                currency.SubtractAmount(userData, amount);
                return true;
            }
            return false;
        }

        public bool DoesMeetCurrencyRequirement(UserDataModel userData)
        {
            if (userData.IsCurrencyRankExempt)
            {
                return true;
            }

            CurrencyModel currency = this.GetCurrency();
            if (currency == null)
            {
                return false;
            }

            if (currency.IsRank && !string.IsNullOrEmpty(this.RankName))
            {
                RankModel rank = this.RequiredRank;
                if (rank == CurrencyModel.NoRank)
                {
                    return false;
                }
            }

            return this.DoesMeetCurrencyRequirement(currency.GetAmount(userData));
        }

        public bool DoesMeetCurrencyRequirement(int amount)
        {
            if (amount < this.RequiredAmount)
            {
                return false;
            }

            if (this.MaximumAmount > 0 && amount > this.MaximumAmount)
            {
                return false;
            }

            return true;
        }

        public bool DoesMeetRankRequirement(UserDataModel userData)
        {
            if (userData.IsCurrencyRankExempt)
            {
                return true;
            }

            CurrencyModel currency = this.GetCurrency();
            if (currency == null)
            {
                return false;
            }

            RankModel rank = this.RequiredRank;
            if (rank == null)
            {
                return false;
            }

            if (!currency.HasAmount(userData, rank.Amount))
            {
                return false;
            }

            if (this.MustEqual && currency.GetRank(userData) != rank)
            {
                return false;
            }

            return true;
        }

        public async Task SendCurrencyNotMetWhisper(UserViewModel user)
        {
            if (ChannelSession.Services.Chat != null && ChannelSession.Settings.Currency.ContainsKey(this.CurrencyID))
            {
                if (this.RequirementType == CurrencyRequirementTypeEnum.MinimumAndMaximum)
                {
                    await ChannelSession.Services.Chat.SendMessage(string.Format("You do not have the required {0}-{1} {2} to do this",
                        this.RequiredAmount, this.MaximumAmount, ChannelSession.Settings.Currency[this.CurrencyID].Name));
                }
                else
                {
                    await ChannelSession.Services.Chat.SendMessage(string.Format("You do not have the required {0} {1} to do this",
                        this.RequiredAmount, ChannelSession.Settings.Currency[this.CurrencyID].Name));
                }
            }
        }

        public async Task SendCurrencyNotMetWhisper(UserViewModel user, int amount)
        {
            if (ChannelSession.Services.Chat != null && ChannelSession.Settings.Currency.ContainsKey(this.CurrencyID))
            {
                if (this.RequirementType == CurrencyRequirementTypeEnum.MinimumAndMaximum)
                {
                    await ChannelSession.Services.Chat.SendMessage(string.Format("You do not have the required {0}-{1} {2} to do this",
                        this.RequiredAmount, this.MaximumAmount, ChannelSession.Settings.Currency[this.CurrencyID].Name));
                }
                else
                {
                    await ChannelSession.Services.Chat.SendMessage(string.Format("You do not have the required {0} {1} to do this",
                        this.RequiredAmount, ChannelSession.Settings.Currency[this.CurrencyID].Name));
                }
            }
        }

        public async Task SendRankNotMetWhisper(UserViewModel user)
        {
            if (ChannelSession.Services.Chat != null && ChannelSession.Settings.Currency.ContainsKey(this.CurrencyID))
            {
                await ChannelSession.Services.Chat.SendMessage(string.Format("You do not have the required rank of {0} ({1} {2}) to do this",
                    this.RequiredRank.Name, this.RequiredRank.Amount, ChannelSession.Settings.Currency[this.CurrencyID].Name));
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is CurrencyRequirementViewModel)
            {
                return this.Equals((CurrencyRequirementViewModel)obj);
            }
            return false;
        }

        public bool Equals(CurrencyRequirementViewModel other) { return this.CurrencyID.Equals(other.CurrencyID); }

        public override int GetHashCode() { return this.CurrencyID.GetHashCode(); }
    }
}
