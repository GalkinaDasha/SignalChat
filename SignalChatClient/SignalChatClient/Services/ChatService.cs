using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SignalChatClient.Enums;
using SignalChatClient.Models;
using SignalChatClient.Services;
using Microsoft.AspNet.SignalR.Client;

namespace SignalChatClient
{
    public class ChatService : IChatService
    {
        public event Action<string, string, MessageType> NewTextMessage;
        public event Action<string> ParticipantDisconnected;
        public event Action<User> ParticipantLoggedIn;
        public event Action<string> ParticipantLoggedOut;
        public event Action<string> ParticipantReconnected;
        public event Action ConnectionReconnecting;
        public event Action ConnectionReconnected;
        public event Action ConnectionClosed;

        public event Action<User> ParticipantAdded;
        public event Action<string> ParticipantRemoved;

        private IHubProxy hubProxy;
        private HubConnection connection;
        private string url = "http://localhost:8080/signalchat";

        public async Task ConnectAsync()
        {
            connection = new HubConnection(url);
            hubProxy = connection.CreateHubProxy("ChatHub");
            hubProxy.On<User>("ParticipantLogin", (u) => ParticipantLoggedIn?.Invoke(u));
            hubProxy.On<string>("ParticipantLogout", (n) => ParticipantLoggedOut?.Invoke(n));
            hubProxy.On<string>("ParticipantDisconnection", (n) => ParticipantDisconnected?.Invoke(n));
            hubProxy.On<string>("ParticipantReconnection", (n) => ParticipantReconnected?.Invoke(n));
            hubProxy.On<string, string>("BroadcastTextMessage", (n, m) => NewTextMessage?.Invoke(n, m, MessageType.Broadcast));
            hubProxy.On<string, string>("UnicastTextMessage", (n, m) => NewTextMessage?.Invoke(n, m, MessageType.Unicast));

            hubProxy.On<User>("ParticipantAdded", (u) => ParticipantAdded?.Invoke(u));
            hubProxy.On<string>("ParticipantRemoved", (name) => ParticipantRemoved?.Invoke(name));

            connection.Reconnecting += Reconnecting;
            connection.Reconnected += Reconnected;
            connection.Closed += Disconnected;

            ServicePointManager.DefaultConnectionLimit = 10;
            await connection.Start();
        }

        private void Disconnected()
        {
            ConnectionClosed?.Invoke();
        }

        private void Reconnected()
        {
            ConnectionReconnected?.Invoke();
        }

        private void Reconnecting()
        {
            ConnectionReconnecting?.Invoke();
        }

        public async Task<List<User>> LoginAsync(string name)
        {
            return await hubProxy.Invoke<List<User>>("Login", new object[] { name });
        }

        public async Task<List<User>> RegistrationAsync(string name)
        {
            return await hubProxy.Invoke<List<User>>("Registration", new object[] { name });
        }

        public async Task LogoutAsync()
        {
            await hubProxy.Invoke("Logout");
        }

        public async Task SendBroadcastMessageAsync(string msg)
        {
            await hubProxy.Invoke("BroadcastTextMessage", msg);
        }

        public async Task SendUnicastMessageAsync(string recepient, string msg)
        {
            await hubProxy.Invoke("UnicastTextMessage", new object[] { recepient, msg });
        }

        public async Task<int> AddUserAsync(string name, bool isadmin)
        {
            int PerenRez = await hubProxy.Invoke<int>("AddUser", name, isadmin);
            return PerenRez;
        }

        public async Task<int> RemoveUserAsync(string name)
        {
            int PerenRez = await hubProxy.Invoke<int>("RemoveUser", name);
            return PerenRez;
        }
    }
}
