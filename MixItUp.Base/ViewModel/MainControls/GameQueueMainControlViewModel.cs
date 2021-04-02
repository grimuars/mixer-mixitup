﻿using MixItUp.Base.Model.Commands;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.User;
using MixItUp.Base.ViewModels;
using StreamingClient.Base.Util;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MixItUp.Base.ViewModel.MainControls
{
    public class QueueUser
    {
        public UserViewModel user { get; set; }

        public int QueuePosition { get; set; }

        public string Username { get { return this.user.DisplayName; } }

        public string PrimaryRole { get { return EnumHelper.GetEnumName(this.user.PrimaryRole); } }

        public QueueUser(UserViewModel user, int queuePosition)
        {
            this.user = user;
            this.QueuePosition = queuePosition;
        }
    }

    public class GameQueueMainControlViewModel : WindowControlViewModelBase
    {
        public bool IsEnabled { get { return ChannelSession.Services.GameQueueService.IsEnabled; } }

        public string EnableDisableButtonText { get { return (this.IsEnabled) ? "Disable" : "Enable"; } }

        public bool SubPriority
        {
            get { return ChannelSession.Settings.GameQueueSubPriority; }
            set
            {
                ChannelSession.Settings.GameQueueSubPriority = value;
                this.NotifyPropertyChanged();
            }
        }

        public ThreadSafeObservableCollection<QueueUser> QueueUsers { get; private set; } = new ThreadSafeObservableCollection<QueueUser>();

        public CommandModelBase GameQueueUserJoinedCommand
        {
            get { return this.gameQueueUserJoinedCommand; }
            set
            {
                this.gameQueueUserJoinedCommand = value;
                this.NotifyPropertyChanged();
            }
        }
        private CommandModelBase gameQueueUserJoinedCommand;
        public CommandModelBase GameQueueUserSelectedCommand
        {
            get { return this.gameQueueUserSelectedCommand; }
            set
            {
                this.gameQueueUserSelectedCommand = value;
                this.NotifyPropertyChanged();
            }
        }
        private CommandModelBase gameQueueUserSelectedCommand;

        public ICommand EnableDisableCommand { get; private set; }
        public ICommand MoveUpCommand { get; private set; }
        public ICommand MoveDownCommand { get; private set; }
        public ICommand DeleteCommand { get; private set; }
        public ICommand ClearQueueCommand { get; private set; }

        public GameQueueMainControlViewModel(UIViewModelBase windowViewModel)
            : base(windowViewModel)
        {
            GlobalEvents.OnGameQueueUpdated += GlobalEvents_OnGameQueueUpdated;

            this.GameQueueUserJoinedCommand = ChannelSession.Settings.GetCommand(ChannelSession.Settings.GameQueueUserJoinedCommandID);
            this.GameQueueUserSelectedCommand = ChannelSession.Settings.GetCommand(ChannelSession.Settings.GameQueueUserSelectedCommandID);

            this.EnableDisableCommand = this.CreateCommand(async (x) =>
            {
                if (this.IsEnabled)
                {
                    await ChannelSession.Services.GameQueueService.Disable();
                }
                else
                {
                    await ChannelSession.Services.GameQueueService.Enable();
                }
                this.NotifyPropertyChanges();
            });

            this.MoveUpCommand = this.CreateCommand(async (user) =>
            {
                await ChannelSession.Services.GameQueueService.MoveUp((UserViewModel)user);
                this.NotifyPropertyChanges();
            });

            this.MoveDownCommand = this.CreateCommand(async (user) =>
            {
                await ChannelSession.Services.GameQueueService.MoveDown((UserViewModel)user);
                this.NotifyPropertyChanges();
            });

            this.DeleteCommand = this.CreateCommand(async (user) =>
            {
                await ChannelSession.Services.GameQueueService.Leave((UserViewModel)user);
                this.NotifyPropertyChanges();
            });

            this.ClearQueueCommand = this.CreateCommand(async (x) =>
            {
                if (await DialogHelper.ShowConfirmation(Resources.ClearGameQueuePrompt))
                {
                    await ChannelSession.Services.GameQueueService.Clear();
                    this.NotifyPropertyChanges();
                }
            });
        }

        private void GlobalEvents_OnGameQueueUpdated(object sender, System.EventArgs e)
        {
            List<QueueUser> queue = new List<QueueUser>();
            int position = 1;
            foreach (UserViewModel user in ChannelSession.Services.GameQueueService.Queue)
            {
                queue.Add(new QueueUser(user, position));
                position++;
            }
            this.QueueUsers.ClearAndAddRange(queue);

            this.NotifyPropertyChanges();
        }

        private void NotifyPropertyChanges()
        {
            this.NotifyPropertyChanged("IsEnabled");
            this.NotifyPropertyChanged("EnableDisableButtonText");
            this.NotifyPropertyChanged("SubPriority");
            this.NotifyPropertyChanged("QueueUsers");
        }
    }
}
