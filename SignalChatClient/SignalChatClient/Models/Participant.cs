using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SignalChatClient.ViewModels;

namespace SignalChatClient.Models
{
    public class Participant : ViewModelBase
    {
        public string Name { get; set; }
        public ObservableCollection<ChatMessage> Chatter { get; set; }

        private bool _isLoggedIn = true;
        public bool IsLoggedIn
        {
            get { return _isLoggedIn; }
            set { _isLoggedIn = value; OnPropertyChanged(); }
        }

        private bool _hasSentNewMessage;
        public bool HasSentNewMessage
        {
            get { return _hasSentNewMessage; }
            set { _hasSentNewMessage = value; OnPropertyChanged(); }
        }

        // свойство для проверки, выбран ли участник для рассылки уведомлений
        private bool _isChosen;
        public bool IsChosen
        {
            get { return _isChosen; }
            set { _isChosen = value; OnPropertyChanged(); }
        }

        public Participant() { Chatter = new ObservableCollection<ChatMessage>(); }

        public void ToggleChosen()
        {
            IsChosen = !IsChosen;
        }
    }
}
