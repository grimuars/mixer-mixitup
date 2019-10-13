using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.Chat.Twitch;
using MixItUp.Base.ViewModel.User;
using StreamingClient.Base.Util;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Twitch.Base.Clients;
using Twitch.Base.Models.Clients.PubSub;
using Twitch.Base.Models.Clients.PubSub.Messages;

namespace MixItUp.Base.Services.Twitch
{
    public interface ITwitchPubSubService
    {
        event EventHandler<PubSubMessagePacketModel> OnMessageOccurred;
    }

    public class TwitchPubSubService : AsyncServiceBase, ITwitchPubSubService
    {
        private PubSubClient client;

        private CancellationTokenSource cancellationTokenSource;

        public event EventHandler<PubSubMessagePacketModel> OnMessageOccurred = delegate { };

        public TwitchPubSubService() { }

        public async Task<bool> Connect()
        {
            return await this.AttemptRunAsync(async () =>
            {
                try
                {
                    if (ChannelSession.TwitchUserConnection != null)
                    {
                        this.client = new PubSubClient(ChannelSession.TwitchUserConnection.Connection);
                        this.cancellationTokenSource = new CancellationTokenSource();

                        if (ChannelSession.Settings.DiagnosticLogging)
                        {
                            this.client.OnSentOccurred += PubSub_OnSentOccurred;
                        }
                        this.client.OnReconnectReceived += PubSub_OnReconnectReceived;
                        this.client.OnDisconnectOccurred += PubSub_OnDisconnectOccurred;
                        this.client.OnPongReceived += PubSub_OnPongReceived;
                        this.client.OnResponseReceived += PubSub_OnResponseReceived;
                        this.client.OnMessageReceived += PubSub_OnMessageReceived;
                        this.client.OnWhisperReceived += Client_OnWhisperReceived;
                        this.client.OnBitsV1Received += Client_OnBitsV1Received;
                        this.client.OnBitsV2Received += Client_OnBitsV2Received;
                        this.client.OnBitsBadgeReceived += Client_OnBitsBadgeReceived;
                        this.client.OnSubscribedReceived += Client_OnSubscribedReceived;
                        this.client.OnSubscriptionsGiftedReceived += Client_OnSubscriptionsGiftedReceived;
                        this.client.OnCommerceReceived += Client_OnCommerceReceived;

                        await this.client.Connect();

                        await Task.Delay(1000);

                        List<PubSubListenTopicModel> topics = new List<PubSubListenTopicModel>();
                        foreach (PubSubTopicsEnum topic in EnumHelper.GetEnumList<PubSubTopicsEnum>())
                        {
                            topics.Add(new PubSubListenTopicModel(topic, ChannelSession.TwitchNewAPIChannel.id));
                        }

                        await this.client.Listen(topics);

                        await Task.Delay(1000);

                        await this.client.Ping();

                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
                return false;
            });
        }

        public async Task Disconnect()
        {
            await this.RunAsync(async () =>
            {
                if (this.client != null)
                {
                    this.client.OnSentOccurred -= PubSub_OnSentOccurred;
                    this.client.OnReconnectReceived -= PubSub_OnReconnectReceived;
                    this.client.OnDisconnectOccurred -= PubSub_OnDisconnectOccurred;
                    this.client.OnResponseReceived -= PubSub_OnResponseReceived;
                    this.client.OnMessageReceived -= PubSub_OnMessageReceived;
                    this.client.OnPongReceived -= PubSub_OnPongReceived;

                    await this.client.Disconnect();
                }
            });

            this.client = null;
            if (this.cancellationTokenSource != null)
            {
                this.cancellationTokenSource.Cancel();
                this.cancellationTokenSource = null;
            }
        }

        private void Client_OnWhisperReceived(object sender, PubSubWhisperEventModel whisper)
        {
            ChannelSession.Services.Chat.AddMessage(new TwitchChatMessageViewModel(whisper));
        }

        private void Client_OnBitsV1Received(object sender, PubSubBitsEventV1Model bits)
        {

        }

        private void Client_OnBitsV2Received(object sender, PubSubBitsEventV2Model bits)
        {

        }

        private void Client_OnBitsBadgeReceived(object sender, PubSubBitBadgeEventModel bitBadge)
        {

        }

        private void Client_OnSubscribedReceived(object sender, PubSubSubscriptionsEventModel subscription)
        {

        }

        private void Client_OnSubscriptionsGiftedReceived(object sender, PubSubSubscriptionsGiftEventModel subscriptions)
        {

        }

        private void Client_OnCommerceReceived(object sender, PubSubCommerceEventModel commerce)
        {

        }

        private void PubSub_OnMessageReceived(object sender, PubSubMessagePacketModel packet)
        {
            Logger.Log(LogLevel.Debug, string.Format("Twitch Event Message Received: {0} - {1}", packet.type, packet.data));
        }

        private void PubSub_OnPongReceived(object sender, System.EventArgs e)
        {
            System.Console.WriteLine("PONG");
            AsyncRunner.RunBackgroundTask(this.cancellationTokenSource.Token, async (token) =>
            {
                await Task.Delay(180000);
                await this.client.Ping();
            });
        }

        private void PubSub_OnResponseReceived(object sender, PubSubResponsePacketModel packet)
        {
            Logger.Log(LogLevel.Debug, string.Format("Twitch Event Response Received: {0} - {1}", packet.data, packet.error));
            if (!string.IsNullOrEmpty(packet.error))
            {
                Logger.Log(string.Format("Twitch Event Error Received: {0}", packet.error));
            }
        }

        private void PubSub_OnSentOccurred(object sender, string packet)
        {
            Logger.Log(LogLevel.Debug, string.Format("Twitch Event Packet Sent: {0}", packet));
        }

        private async void PubSub_OnDisconnectOccurred(object sender, System.Net.WebSockets.WebSocketCloseStatus e)
        {
            ChannelSession.DisconnectionOccurred("Twitch Events");

            await this.Disconnect();
            do
            {
                await Task.Delay(2500);
            }
            while (!await this.Connect());

            ChannelSession.ReconnectionOccurred("Twitch Event");
        }

        private void PubSub_OnReconnectReceived(object sender, System.EventArgs e)
        {
            Logger.Log(LogLevel.Debug, "Twitch Event Reconnected");
        }
    }
}
