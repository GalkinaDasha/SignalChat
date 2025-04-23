using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SignalChatClient.Commands;
using SignalChatClient.Enums;
using SignalChatClient.Models;
using SignalChatClient.Services;
using System.Windows.Input;
using System.Reactive.Linq;

namespace SignalChatClient.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private IChatService chatService;
        private IDialogService dialogService;
        private TaskFactory ctxTaskFactory;

        private string _userName;
        public string UserName
        {
            get { return _userName; }
            set
            {
                _userName = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<Participant> _participants = new ObservableCollection<Participant>();
        public ObservableCollection<Participant> Participants
        {
            get { return _participants; }
            set
            {
                _participants = value;
                OnPropertyChanged();
            }
        }

        private Participant _selectedParticipant;
        public Participant SelectedParticipant
        {
            get { return _selectedParticipant; }
            set
            {
                _selectedParticipant = value;
                if (_selectedParticipant != null)
                {
                    if (_selectedParticipant.HasSentNewMessage) _selectedParticipant.HasSentNewMessage = false;
                }
                OnPropertyChanged();
            }
        }

        private UserModes _userMode;
        public UserModes UserMode
        {
            get { return _userMode; }
            set
            {
                _userMode = value;
                OnPropertyChanged();
            }
        }

        private string _textMessage;
        public string TextMessage
        {
            get { return _textMessage; }
            set
            {
                _textMessage = value;
                OnPropertyChanged();
            }
        }

        private bool _isConnected;
        public bool IsConnected
        {
            get { return _isConnected; }
            set
            {
                _isConnected = value;
                OnPropertyChanged();
            }
        }

        private bool _isLoggedIn;
        public bool IsLoggedIn
        {
            get { return _isLoggedIn; }
            set
            {
                _isLoggedIn = value;
                OnPropertyChanged();
            }
        }

        #region Connect Command
        private ICommand _connectCommand;
        public ICommand ConnectCommand
        {
            get
            {
                return _connectCommand ?? (_connectCommand = new RelayCommandAsync(() => Connect()));
            }
        }

        private async Task<bool> Connect()
        {
            try
            {
                await chatService.ConnectAsync();
                IsConnected = true;
                return true;
            }
            catch (Exception) { return false; }
        }
        #endregion

        #region Login Command
        private ICommand _loginCommand;
        public ICommand LoginCommand
        {
            get
            {
                return _loginCommand ?? (_loginCommand =
                    new RelayCommandAsync(() => Login(), (o) => CanLogin()));
            }
        }

        private async Task<bool> Login()
        {
            try
            {
                List<User> users = new List<User>();
                users = await chatService.LoginAsync(_userName);
                if (users != null)
                {
                    users.ForEach(u => Participants.Add(new Participant { Name = u.Name }));
                    UserMode = UserModes.Chat;
                    IsLoggedIn = true;
                    return true;
                }
                else
                {
                    dialogService.ShowNotification("Пользователь или уже вошел или не зарегистрирован. Обратитесь к администратору.");
                    return false;
                }

            }
            catch (Exception) { return false; }
        }

        private bool CanLogin()
        {
            return !string.IsNullOrEmpty(UserName) && UserName.Length >= 2 && IsConnected;
        }
        #endregion

        #region Registration Command
        private bool CanRegistration()
        {
            return !string.IsNullOrEmpty(UserName) && UserName.Length >= 2 && IsConnected;
        }

        private ICommand _registrationCommand;
        public ICommand RegistrationCommand
        {
            get
            {
                return _registrationCommand ?? (_registrationCommand =
                    new RelayCommandAsync(() => Registration(), (o) => CanRegistration()));
            }
        }

        private async Task<bool> Registration()
        {
            try
            {
                List<User> users = new List<User>();
                users = await chatService.RegistrationAsync(_userName);
                if (users != null)
                {
                    users.ForEach(u => Participants.Add(new Participant { Name = u.Name }));
                    UserMode = UserModes.Chat;
                    IsLoggedIn = true;
                    return true;
                }
                else
                {
                    dialogService.ShowNotification("Ошибка регистрации. Обратитесь к администратору.");
                    return false;
                }

            }
            catch (Exception) { return false; }
        }
        #endregion

        #region Logout Command
        private ICommand _logoutCommand;
        public ICommand LogoutCommand
        {
            get
            {
                return _logoutCommand ?? (_logoutCommand =
                    new RelayCommandAsync(() => Logout(), (o) => CanLogout()));
            }
        }

        private async Task<bool> Logout()
        {
            try
            {
                await chatService.LogoutAsync();
                UserMode = UserModes.Login;
                return true;
            }
            catch (Exception) { return false; }
        }

        private bool CanLogout()
        {
            return IsConnected && IsLoggedIn;
        }
        #endregion

        #region Send Text Message Command
        private ICommand _sendTextMessageCommand;
        public ICommand SendTextMessageCommand
        {
            get
            {
                return _sendTextMessageCommand ?? (_sendTextMessageCommand =
                    new RelayCommandAsync(() => SendTextMessage(), (o) => CanSendTextMessage()));
            }
        }

        private async Task<bool> SendTextMessage()
        {
            try
            {
                var recepient = _selectedParticipant.Name;
                await chatService.SendUnicastMessageAsync(recepient, _textMessage);
                return true;
            }
            catch (Exception) { return false; }
            finally
            {
                ChatMessage msg = new ChatMessage
                {
                    Author = UserName,
                    Message = _textMessage,
                    Time = DateTime.Now,
                    IsOriginNative = true
                };
                SelectedParticipant.Chatter.Add(msg);
                TextMessage = string.Empty;
            }
        }

        private bool CanSendTextMessage()
        {
            return (!string.IsNullOrEmpty(TextMessage) && IsConnected &&
                _selectedParticipant != null && _selectedParticipant.IsLoggedIn);
        }
        #endregion

        #region Add User Command
        private ICommand _addUserCommand;
        public ICommand AddUserCommand
        {
            get
            {
                return _addUserCommand ?? (_addUserCommand =
                    new RelayCommandAsync(() => AddUser (), (o) => CanAddUser ()));
            }
        }

        private async Task<bool> AddUser()
        {
            try
            {
                if (string.IsNullOrEmpty(UserName) || !IsLoggedIn)
                {
                    dialogService.ShowNotification("Вы должны быть авторизованы для добавления пользователей.");
                    return false;
                }

                // получение кортежа
                var (inputValue, isAdmin) = dialogService.ShowInputDialog("Введите имя пользователя для добавления!:");

                if (!string.IsNullOrEmpty(inputValue))
                {
                    int rez = await chatService.AddUserAsync(inputValue, isAdmin); // метод для добавления пользователя на сервере

                    switch (rez)
                    {
                        case 1:
                            dialogService.ShowNotification($"Пользователь '{inputValue}' успешно добавлен.");
                            break;
                        case 0:
                            dialogService.ShowNotification($"Произошла ошибка при добавлении пользователя '{inputValue}'.");
                            break;
                        case 2:
                            dialogService.ShowNotification($"Недостаточно прав для добавления пользователя '{inputValue}'.");
                            break;
                        case 3:
                            dialogService.ShowNotification($"Пользователь '{inputValue}' уже существует.");
                            break;
                        default:
                            dialogService.ShowNotification($"Произошла ошибка при добавлении пользователя '{inputValue}'.");
                            break;
                    }

                    //Participants.Add(new Participant { Name = newUserName });
                    return true;
                }
            }
            catch (Exception ex)
            {
                dialogService.ShowNotification($"Ошибка при добавлении пользователя: {ex.Message}");
            }
            return false;
        }

        private bool CanAddUser()
        {
            return IsLoggedIn; // Можно добавить дополнительные проверки, если необходимо
        }
        #endregion

        #region RemoveUserCommand
        private ICommand _removeUserCommand;
        public ICommand RemoveUserCommand
        {
            get
            {
                return _removeUserCommand ?? (_removeUserCommand =
                    new RelayCommandAsync(() => RemoveUser(), (o) => CanRemoveUser()));
            }
        }

        private async Task<bool> RemoveUser()
        {
            try
            {
                if (SelectedParticipant == null || !IsLoggedIn)
                {
                    dialogService.ShowNotification("Вы должны выбрать пользователя для удаления.");
                    return false;
                }

                var confirm = dialogService.ShowConfirmationDialog($"Вы уверены, что хотите удалить пользователя {SelectedParticipant.Name}?");
                if (confirm)
                {
                    int rez = await chatService.RemoveUserAsync(SelectedParticipant.Name); // метод для удаления пользователя на сервере

                    switch (rez)
                    {
                        case 1:
                            dialogService.ShowNotification($"Пользователь '{SelectedParticipant.Name}' успешно удален.");
                            break;
                        case 0:
                            dialogService.ShowNotification($"Произошла ошибка при удалении пользователя '{SelectedParticipant.Name}'.");
                            break;
                        case 2:
                            dialogService.ShowNotification($"Недостаточно прав для удаления пользователя '{SelectedParticipant.Name}'.");
                            break;
                        default:
                            dialogService.ShowNotification($"Произошла ошибка при удалении пользователя '{SelectedParticipant.Name}'.");
                            break;
                    }

                    Participants.Remove(SelectedParticipant);
                    SelectedParticipant = null; // сбросить выбор

                    return true;
                }
            }
            catch (Exception ex)
            {
                dialogService.ShowNotification($"Ошибка при удалении пользователя: {ex.Message}");
            }
            return false;
        }

        private bool CanRemoveUser()
        {
            return SelectedParticipant != null && IsLoggedIn; // Можно добавить дополнительные проверки, если необходимо
        }
        #endregion

        #region Event Handlers
        private void NewTextMessage(string name, string msg, MessageType mt)
        {
            if (mt == MessageType.Unicast)
            {
                ChatMessage cm = new ChatMessage { Author = name, Message = msg, Time = DateTime.Now };
                var sender = _participants.Where((u) => string.Equals(u.Name, name)).FirstOrDefault();
                ctxTaskFactory.StartNew(() => sender.Chatter.Add(cm)).Wait();

                if (!(SelectedParticipant != null && sender.Name.Equals(SelectedParticipant.Name)))
                {
                    ctxTaskFactory.StartNew(() => sender.HasSentNewMessage = true).Wait();
                }
            }
        }

        private void ParticipantLogin(User u)
        {
            var ptp = Participants.FirstOrDefault(p => string.Equals(p.Name, u.Name));
            if (_isLoggedIn && ptp == null) // добавляем нового участника
            {
                ctxTaskFactory.StartNew(() => Participants.Add(new Participant
                {
                    Name = u.Name
                })).Wait();

                // уведомление для всех выбранных участников
                NotifyChosenParticipants(u.Name, "вошел в систему.");
            }
            else if (ptp != null) // юзер найден, обновляем его статус входа
            {
                ptp.IsLoggedIn = true;
                // уведомление для всех выбранных участников
                NotifyChosenParticipants(ptp.Name, "снова вошел в систему.");
            }
        }

        private ICommand _toggleUserChosenCommand;
        public ICommand ToggleUserChosenCommand
        {
            get
            {
                return _toggleUserChosenCommand ?? (_toggleUserChosenCommand = new RelayCommand<Participant>(ToggleParticipantChosen));
            }
        }

        public void ToggleParticipantChosen(Participant participant)
        {
            // переключаем состояние IsChosen, уведомляем об этом
            participant.ToggleChosen();

            if (participant.IsChosen)
            {
                dialogService.ShowNotification($"Пользователь '{participant.Name}' добавлен в список уведомлений.");
            }
            else
            {
                dialogService.ShowNotification($"Пользователь '{participant.Name}' удален из списка уведомлений.");
            }
        }

        // уведомление выбранных участников
        private void NotifyChosenParticipants(string userName, string action)
        {
            foreach (var participant in Participants)
            {
                if (participant.IsChosen)
                {
                    dialogService.ShowNotification($"Пользователь '{userName}' {action}");
                }
            }
        }

        private void ParticipantDisconnection(string name)
        {
            var person = Participants.Where((p) => string.Equals(p.Name, name)).FirstOrDefault();
            if (person != null) person.IsLoggedIn = false;

            // уведомление для всех выбранных участников
            NotifyChosenParticipants(name, "вышел из системы.");
        }

        private void ParticipantReconnection(string name)
        {
            var person = Participants.Where((p) => string.Equals(p.Name, name)).FirstOrDefault();
            if (person != null) person.IsLoggedIn = true;
        }

        private void Reconnecting()
        {
            IsConnected = false;
            IsLoggedIn = false;
        }

        private async void Reconnected()
        {
            if (!string.IsNullOrEmpty(_userName)) await chatService.LoginAsync(_userName);
            IsConnected = true;
            IsLoggedIn = true;
        }

        private async void Disconnected()
        {
            var connectionTask = chatService.ConnectAsync();
            await connectionTask.ContinueWith(t => {
                if (!t.IsFaulted)
                {
                    IsConnected = true;
                    chatService.LoginAsync(_userName).Wait();
                    IsLoggedIn = true;
                }
            });
        }
        #endregion

        public MainWindowViewModel(IChatService chatSvc, IDialogService diagSvc)
        {
            dialogService = diagSvc;
            chatService = chatSvc;

            chatSvc.NewTextMessage += NewTextMessage;
            chatSvc.ParticipantLoggedIn += ParticipantLogin;
            chatSvc.ParticipantLoggedOut += ParticipantDisconnection;
            chatSvc.ParticipantDisconnected += ParticipantDisconnection;
            chatSvc.ParticipantReconnected += ParticipantReconnection;
            chatSvc.ConnectionReconnecting += Reconnecting;
            chatSvc.ConnectionReconnected += Reconnected;
            chatSvc.ConnectionClosed += Disconnected;

            ctxTaskFactory = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());
        }

    }
}