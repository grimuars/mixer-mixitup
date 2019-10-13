using StreamingClient.Base.Model.OAuth;
using StreamingClient.Base.Util;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Twitch.Base;
using Twitch.Base.Models.NewAPI.Games;
using Twitch.Base.Models.V5.Channel;
using NewAPI = Twitch.Base.Models.NewAPI;
using V5API = Twitch.Base.Models.V5;

namespace MixItUp.Base.Services.Twitch
{
    public interface ITwitchConnectionService
    {

    }

    public class TwitchConnectionService : AsyncServiceBase, ITwitchConnectionService
    {
        public const string ClientID = "50ipfqzuqbv61wujxcm80zyzqwoqp1";

        public static readonly List<OAuthClientScopeEnum> StreamerScopes = new List<OAuthClientScopeEnum>()
        {
            OAuthClientScopeEnum.channel_commercial,
            OAuthClientScopeEnum.channel_editor,
            OAuthClientScopeEnum.channel_read,
            OAuthClientScopeEnum.channel_subscriptions,

            OAuthClientScopeEnum.user_read,

            OAuthClientScopeEnum.bits__read,
            OAuthClientScopeEnum.channel__moderate,
            OAuthClientScopeEnum.chat__edit,
            OAuthClientScopeEnum.chat__read,
            OAuthClientScopeEnum.user__edit,
            OAuthClientScopeEnum.whispers__read,
            OAuthClientScopeEnum.whispers__edit,
        };

        public static readonly List<OAuthClientScopeEnum> ModeratorScopes = new List<OAuthClientScopeEnum>()
        {
            OAuthClientScopeEnum.channel_editor,
            OAuthClientScopeEnum.channel_read,

            OAuthClientScopeEnum.user_read,

            OAuthClientScopeEnum.bits__read,
            OAuthClientScopeEnum.channel__moderate,
            OAuthClientScopeEnum.chat__edit,
            OAuthClientScopeEnum.chat__read,
            OAuthClientScopeEnum.user__edit,
            OAuthClientScopeEnum.whispers__read,
            OAuthClientScopeEnum.whispers__edit,
        };

        public static readonly List<OAuthClientScopeEnum> BotScopes = new List<OAuthClientScopeEnum>()
        {
            OAuthClientScopeEnum.channel_editor,
            OAuthClientScopeEnum.channel_read,

            OAuthClientScopeEnum.user_read,

            OAuthClientScopeEnum.bits__read,
            OAuthClientScopeEnum.channel__moderate,
            OAuthClientScopeEnum.chat__edit,
            OAuthClientScopeEnum.chat__read,
            OAuthClientScopeEnum.user__edit,
            OAuthClientScopeEnum.whispers__read,
            OAuthClientScopeEnum.whispers__edit,
        };

        public TwitchConnection Connection { get; private set; }

        public TwitchConnectionService() { }

        public async Task<bool> ConnectAsStreamer() { return await this.Connect(TwitchConnectionService.StreamerScopes); }

        public async Task<bool> ConnectAsModerator() { return await this.Connect(TwitchConnectionService.ModeratorScopes); }

        public async Task<bool> ConnectAsBot() { return await this.Connect(TwitchConnectionService.BotScopes); }

        public async Task<bool> Connect(OAuthTokenModel token)
        {
            try
            {
                this.Connection = await TwitchConnection.ConnectViaOAuthToken(token);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return (this.Connection != null);
        }

        public async Task<NewAPI.Users.UserModel> GetNewAPICurrentUser() { return await this.RunAsync(this.Connection.NewAPI.Users.GetCurrentUser()); }

        public async Task<NewAPI.Users.UserModel> GetNewAPIUserByID(string userID) { return await this.RunAsync(this.Connection.NewAPI.Users.GetUserByID(userID)); }

        public async Task<NewAPI.Users.UserModel> GetNewAPIUserByLogin(string login) { return await this.RunAsync(this.Connection.NewAPI.Users.GetUserByLogin(login)); }

        public async Task<V5API.Users.UserModel> GetV5APIUserByID(string userID) { return await this.RunAsync(this.Connection.V5API.Users.GetUserByID(userID)); }

        public async Task<V5API.Users.UserModel> GetV5APIUserByLogin(string login) { return await this.RunAsync(this.Connection.V5API.Users.GetUserByLogin(login)); }

        public async Task<V5API.Channel.ChannelModel> GetV5APIChannel(string channelID) { return await this.RunAsync(this.Connection.V5API.Channels.GetChannelByID(channelID)); }

        public async Task<V5API.Channel.ChannelModel> GetV5APIChannel(V5API.Users.UserModel user) { return await this.RunAsync(this.Connection.V5API.Channels.GetChannel(user)); }

        public async Task<V5API.Channel.ChannelModel> GetV5APIChannel(V5API.Channel.ChannelModel channel) { return await this.RunAsync(this.Connection.V5API.Channels.GetChannel(channel)); }

        public async Task UpdateChannel(V5API.Channel.ChannelModel channel, string status = null, GameModel game = null)
        {
            ChannelUpdateModel update = new ChannelUpdateModel()
            {
                status = (!string.IsNullOrEmpty(status)) ? status : null,
                game = (game != null) ? game.name : null
            };
            await this.RunAsync(this.Connection.V5API.Channels.UpdateChannel(channel, update));
        }

        public async Task<NewAPI.Games.GameModel> GetNewAPIGameByID(string id) { return await this.RunAsync(this.Connection.NewAPI.Games.GetGameByID(id)); }

        public async Task<IEnumerable<NewAPI.Games.GameModel>> GetNewAPIGamesByName(string name) { return await this.RunAsync(this.Connection.NewAPI.Games.GetGamesByName(name)); }

        private async Task<bool> Connect(IEnumerable<OAuthClientScopeEnum> scopes)
        {
            try
            {
                this.Connection = await TwitchConnection.ConnectViaLocalhostOAuthBrowser(TwitchConnectionService.ClientID, ChannelSession.SecretManager.GetSecret("TwitchSecret"), scopes,
                    forceApprovalPrompt: true, successResponse: OAuthServiceBase.LoginRedirectPageHTML);
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            return (this.Connection != null);
        }
    }
}
