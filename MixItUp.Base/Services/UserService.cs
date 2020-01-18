using Mixer.Base.Model.Chat;
using Mixer.Base.Model.MixPlay;
using Mixer.Base.Model.User;
using MixItUp.Base.Commands;
using MixItUp.Base.Model;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.User;
using StreamingClient.Base.Util;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitchNewAPI = Twitch.Base.Models.NewAPI;

namespace MixItUp.Base.Services
{
    public static class ChatUserEventModelExtensions
    {
        public static ChatUserModel ToChatUserModel(this ChatUserEventModel chatUser) { return new ChatUserModel() { userId = chatUser.id, userName = chatUser.username, userRoles = chatUser.roles }; }
    }

    public interface IUserService
    {
        UserViewModel GetUserByID(uint id);

        UserViewModel GetUserByID(string id);

        UserViewModel GetUserByUsername(string id);

        UserViewModel GetUserByMixPlayID(string id);

        IEnumerable<UserViewModel> GetUsersByID(IEnumerable<uint> ids);

        IEnumerable<UserViewModel> GetUsersByID(IEnumerable<string> ids);

        Task<UserViewModel> AddOrUpdateUser(ChatUserEventModel mixerChatUser);

        Task<UserViewModel> AddOrUpdateUser(ChatUserModel mixerChatUser);

        Task<UserViewModel> AddOrUpdateUser(MixPlayParticipantModel mixplayUser);

        Task<UserViewModel> AddOrUpdateUser(TwitchNewAPI.Users.UserModel twitchChatUser);

        Task<UserViewModel> RemoveUser(ChatUserEventModel chatUser);

        Task<UserViewModel> RemoveUser(ChatUserModel chatUser);

        Task<UserViewModel> RemoveUser(MixPlayParticipantModel mixplayUser);

        Task<UserViewModel> RemoveUser(string twitchLogin);

        void Clear();

        IEnumerable<UserViewModel> GetAllUsers();

        IEnumerable<UserViewModel> GetAllWorkableUsers();

        int Count();
    }

    public class UserService : IUserService
    {
        public static readonly HashSet<string> SpecialUserAccounts = new HashSet<string>() { "HypeBot", "boomtvmod", "StreamJar", "PretzelRocks", "ScottyBot", "Streamlabs", "StreamElements" };

        private LockedDictionary<string, UserViewModel> usersByMixerID = new LockedDictionary<string, UserViewModel>();
        private LockedDictionary<string, UserViewModel> usersByMixerUsername = new LockedDictionary<string, UserViewModel>();
        private LockedDictionary<string, UserViewModel> usersByMixPlayID = new LockedDictionary<string, UserViewModel>();
        private LockedDictionary<string, UserViewModel> usersByTwitchID = new LockedDictionary<string, UserViewModel>();
        private LockedDictionary<string, UserViewModel> usersByTwitchUsername = new LockedDictionary<string, UserViewModel>();

        public UserViewModel GetUserByID(uint id) { return this.GetUserByID(id.ToString()); }

        public UserViewModel GetUserByID(string id)
        {
            if (this.usersByMixerID.TryGetValue(id, out UserViewModel user))
            {
                return user;
            }
            return null;
        }

        public UserViewModel GetUserByUsername(string id)
        {
            if (this.usersByMixerUsername.TryGetValue(id, out UserViewModel user))
            {
                return user;
            }
            return null;
        }

        public UserViewModel GetUserByMixPlayID(string id)
        {
            if (this.usersByMixPlayID.TryGetValue(id, out UserViewModel user))
            {
                return user;
            }
            return null;
        }

        public IEnumerable<UserViewModel> GetUsersByID(IEnumerable<uint> ids) { return this.GetUsersByID(ids.Select(i => i.ToString())); }

        public IEnumerable<UserViewModel> GetUsersByID(IEnumerable<string> ids)
        {
            List<UserViewModel> results = new List<UserViewModel>();
            foreach (string id in ids)
            {
                if (this.usersByMixerID.TryGetValue(id, out UserViewModel user))
                {
                    results.Add(user);
                }
            }
            return results;
        }

        public async Task<UserViewModel> AddOrUpdateUser(ChatUserEventModel chatUser) { return await this.AddOrUpdateUser(chatUser.ToChatUserModel()); }

        public async Task<UserViewModel> AddOrUpdateUser(ChatUserModel chatUser)
        {
            UserViewModel user = new UserViewModel(chatUser);
            if (chatUser.userId.HasValue && chatUser.userId.GetValueOrDefault() > 0)
            {
                if (this.usersByMixerID.ContainsKey(chatUser.userId.GetValueOrDefault().ToString()))
                {
                    user = this.usersByMixerID[chatUser.userId.GetValueOrDefault().ToString()];
                }
                user.SetChatDetails(chatUser);
                await this.AddOrUpdateUser(user);
            }
            return user;
        }

        public async Task<UserViewModel> AddOrUpdateUser(MixPlayParticipantModel mixplayUser)
        {
            UserViewModel user = new UserViewModel(mixplayUser);
            if (mixplayUser.userID > 0 && !string.IsNullOrEmpty(mixplayUser.sessionID))
            {
                if (this.usersByMixerID.ContainsKey(mixplayUser.userID.ToString()))
                {
                    user = this.usersByMixerID[mixplayUser.userID.ToString()];
                }
                user.SetInteractiveDetails(mixplayUser);
                this.usersByMixPlayID[mixplayUser.sessionID] = user;
                await this.AddOrUpdateUser(user);
            }
            return user;
        }

        public async Task<UserViewModel> AddOrUpdateUser(TwitchNewAPI.Users.UserModel twitchChatUser)
        {
            if (!string.IsNullOrEmpty(twitchChatUser.id) && !string.IsNullOrEmpty(twitchChatUser.login))
            {
                UserViewModel user = new UserViewModel(twitchChatUser);
                await this.AddOrUpdateUser(user);
                return user;
            }
            return null;
        }

        private async Task AddOrUpdateUser(UserViewModel user)
        {
            if (!user.IsMixerAnonymous && user.MixerID > 0 && !string.IsNullOrEmpty(user.UserName))
            {
                if (user.Platform == StreamingPlatformTypeEnum.Mixer)
                {
                    this.usersByMixerID[user.MixerID.ToString()] = user;
                    this.usersByMixerUsername[user.UserName] = user;
                }
                else if (user.Platform == StreamingPlatformTypeEnum.Twitch)
                {
                    this.usersByTwitchID[user.MixerID.ToString()] = user;
                    this.usersByTwitchUsername[user.UserName] = user;
                }

                if (UserService.SpecialUserAccounts.Contains(user.UserName))
                {
                    user.IgnoreForQueries = true;
                }
                else
                {
                    user.IgnoreForQueries = false;
                    if (user.Data.ViewingMinutes == 0)
                    {
                        if (EventCommand.CanUserRunEvent(user, EnumHelper.GetEnumName(OtherEventTypeEnum.ChatUserFirstJoin)))
                        {
                            await EventCommand.FindAndRunEventCommand(EnumHelper.GetEnumName(OtherEventTypeEnum.ChatUserFirstJoin), user);
                        }
                    }

                    if (EventCommand.CanUserRunEvent(user, EnumHelper.GetEnumName(OtherEventTypeEnum.ChatUserJoined)))
                    {
                        user.Data.TotalStreamsWatched++;
                        await EventCommand.FindAndRunEventCommand(EnumHelper.GetEnumName(OtherEventTypeEnum.ChatUserJoined), user);
                    }
                }
            }
        }

        public async Task<UserViewModel> RemoveUser(ChatUserEventModel chatUser) { return await this.RemoveUser(chatUser.ToChatUserModel()); }

        public async Task<UserViewModel> RemoveUser(ChatUserModel chatUser)
        {
            if (this.usersByMixerID.TryGetValue(chatUser.userId.GetValueOrDefault().ToString(), out UserViewModel user))
            {
                user.RemoveChatDetails(chatUser);
                if (user.InteractiveIDs.Count == 0)
                {
                    await this.RemoveUser(user);
                }
                return user;
            }
            return null;
        }

        public async Task<UserViewModel> RemoveUser(MixPlayParticipantModel mixplayUser)
        {
            if (this.usersByMixPlayID.TryGetValue(mixplayUser.sessionID, out UserViewModel user))
            {
                this.usersByMixPlayID.Remove(mixplayUser.sessionID);
                user.RemoveInteractiveDetails(mixplayUser);
                if (user.InteractiveIDs.Count == 0 && !user.IsInChat)
                {
                    await this.RemoveUser(user);
                }
                return user;
            }
            return null;
        }

        public async Task<UserViewModel> RemoveUser(string twitchLogin)
        {
            if (this.usersByTwitchID.TryGetValue(twitchLogin, out UserViewModel user))
            {
                await this.RemoveUser(user);
                return user;
            }
            return null;
        }

        private async Task RemoveUser(UserViewModel user)
        {
            if (user.Platform == StreamingPlatformTypeEnum.Mixer)
            {
                this.usersByMixerID.Remove(user.MixerID.ToString());
                this.usersByMixerUsername.Remove(user.UserName);
            }
            else if (user.Platform == StreamingPlatformTypeEnum.Twitch)
            {
                this.usersByTwitchID.Remove(user.MixerID.ToString());
                this.usersByTwitchUsername.Remove(user.UserName);
            }

            if (EventCommand.CanUserRunEvent(user, EnumHelper.GetEnumName(OtherEventTypeEnum.ChatUserLeft)))
            {
                await EventCommand.FindAndRunEventCommand(EnumHelper.GetEnumName(OtherEventTypeEnum.ChatUserLeft), user);
            }
        }

        public void Clear()
        {
            this.usersByMixerID.Clear();
            this.usersByMixerUsername.Clear();
            this.usersByMixPlayID.Clear();
        }

        public IEnumerable<UserViewModel> GetAllUsers() { return this.usersByMixerID.Values; }

        public IEnumerable<UserViewModel> GetAllWorkableUsers()
        {
            IEnumerable<UserViewModel> results = this.GetAllUsers();
            return results.Where(u => !u.IgnoreForQueries);
        }

        public int Count() { return this.usersByMixerID.Count; }
    }
}
