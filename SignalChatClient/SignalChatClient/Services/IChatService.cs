using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SignalChatClient.Enums;
using SignalChatClient.Models;

namespace SignalChatClient.Services
{
    public interface IChatService
    {
        event Action<User> ParticipantLoggedIn;
        event Action<string> ParticipantLoggedOut;
        event Action<string> ParticipantDisconnected;
        event Action<string> ParticipantReconnected;
        event Action ConnectionReconnecting;
        event Action ConnectionReconnected;
        event Action ConnectionClosed;
        event Action<string, string, MessageType> NewTextMessage;
        event Action<string> ParticipantTyping;

        Task ConnectAsync();
        Task<List<User>> LoginAsync(string name);
        Task LogoutAsync();

        Task SendBroadcastMessageAsync(string msg);
        Task SendUnicastMessageAsync(string recepient, string msg);
        Task TypingAsync(string recepient);
    }
}
