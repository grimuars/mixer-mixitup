using MixItUp.Base.Commands;
using MixItUp.Base.Services;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.User;
using System;

namespace MixItUp.Base.Model.Settings
{
    public class StreamerDatabaseV2Model
    {
        public Guid ID { get; set; }

        public DatabaseDictionary<uint, UserDataViewModel> UserData { get; set; } = new DatabaseDictionary<uint, UserDataViewModel>();

        public LockedDictionary<Guid, UserCurrencyViewModel> Currencies { get; set; } = new LockedDictionary<Guid, UserCurrencyViewModel>();
        public LockedDictionary<Guid, UserInventoryViewModel> Inventories { get; set; } = new LockedDictionary<Guid, UserInventoryViewModel>();

        public LockedList<ChatCommand> ChatCommands { get; set; } = new LockedList<ChatCommand>();
        public LockedList<EventCommand> EventCommands { get; set; } = new LockedList<EventCommand>();
        public LockedList<MixPlayCommand> MixPlayCommands { get; set; } = new LockedList<MixPlayCommand>();
        public LockedList<TimerCommand> TimerCommands { get; set; } = new LockedList<TimerCommand>();
        public LockedList<ActionGroupCommand> ActionGroupCommands { get; set; } = new LockedList<ActionGroupCommand>();
        public LockedList<GameCommandBase> GameCommands { get; set; } = new LockedList<GameCommandBase>();

        public LockedList<UserQuoteViewModel> UserQuotes { get; set; } = new LockedList<UserQuoteViewModel>();

        public string FileName { get { return this.ID + SettingsV2Service.DatabaseFileExtension; } }
        public string BackupFileName { get { return this.FileName + SettingsV2Service.LocalBackupFileExtension; } }
    }
}
