using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using System.Data.SQLite;

namespace SignalChatServer
{
    public class ChatHub : Hub<IClient>
    {
        private static ConcurrentDictionary<string, User> ChatClients = new ConcurrentDictionary<string, User>();
        private const string ConnectionString = "Data Source=chatusers.db;Version=3;";


        public ChatHub()
        {
            // инициализация базы данных
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                string createTableQuery = "CREATE TABLE IF NOT EXISTS Users (Name TEXT PRIMARY KEY, IsAdmin INTEGER)";
                using (var command = new SQLiteCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }

                // проверяем, есть ли уже пользователи в базе данных
                string checkUsersQuery = "SELECT COUNT(*) FROM Users";
                using (var command = new SQLiteCommand(checkUsersQuery, connection))
                {
                    long userCount = (long)command.ExecuteScalar();
                    if (userCount == 0)
                    {
                        // если пользователей нет, добавляем администратора
                        string insertAdminQuery = "INSERT INTO Users (Name, IsAdmin) VALUES (@name, @isAdmin)";
                        using (var insertCommand = new SQLiteCommand(insertAdminQuery, connection))
                        {
                            insertCommand.Parameters.AddWithValue("@name", "admin"); 
                            insertCommand.Parameters.AddWithValue("@isAdmin", 1); 
                            insertCommand.ExecuteNonQuery();
                        }
                        Console.WriteLine("Создан пользователь-администратор с именем 'admin'.");
                    }
                }
            }
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            var userName = ChatClients.SingleOrDefault((c) => c.Value.ID == Context.ConnectionId).Key;
            if (userName != null)
            {
                Clients.Others.ParticipantDisconnection(userName);
                Console.WriteLine($"<> {userName} disconnected");
            }
            return base.OnDisconnected(stopCalled);
        }

        public override Task OnReconnected()
        {
            var userName = ChatClients.SingleOrDefault((c) => c.Value.ID == Context.ConnectionId).Key;
            if (userName != null)
            {
                Clients.Others.ParticipantReconnection(userName);
                Console.WriteLine($"== {userName} reconnected");
            }
            return base.OnReconnected();
        }

        public List<User> Login(string name)
        {
            List<User> users = new List<User>(ChatClients.Values);
            bool userAdded = false; // для отслеживания добавления пользователя

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT IsAdmin FROM Users WHERE Name = @name LIMIT 1";
                command.Parameters.AddWithValue("@name", name);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (!ChatClients.ContainsKey(name))
                        {
                            Console.WriteLine($"++ {name} logged in.");
                            User newUser = new User
                            {
                                Name = name,
                                ID = Context.ConnectionId,
                                IsAdmin = reader.GetInt32(0) == 1
                            };

                            // пытаемся добавить пользователя в ChatClients
                            userAdded = ChatClients.TryAdd(name, newUser);
                            if (userAdded)
                            {
                                Clients.CallerState.UserName = name;
                                Clients.Others.ParticipantLogin(newUser);
                            }
                        }
                    }
                }
            }

            // возвращаем список пользователей только если новый пользователь был добавлен
            return userAdded ? users : null; // если пользователь не найден или не добавлен, возвращаем null
        }

        public List<User> Registration(string name)
        {
            List<User> users = new List<User>(ChatClients.Values);
            bool userAdded = false; // для отслеживания добавления пользователя

            using (var connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();

                // Проверяем, существует ли пользователь в базе данных
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT IsAdmin FROM Users WHERE Name = @name LIMIT 1";
                    command.Parameters.AddWithValue("@name", name);

                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {     
                            // юзера с таким ником нет, добавляем его в базу данных
                            using (var insertCommand = connection.CreateCommand())
                            {
                                insertCommand.CommandText = "INSERT INTO Users (Name, IsAdmin) VALUES (@name, 0)"; // без прав админа
                                insertCommand.Parameters.AddWithValue("@name", name);
                                insertCommand.ExecuteNonQuery();
                            }

                            Console.WriteLine($"++ {name} registered and logged in.");
                            User newUser = new User
                            {
                                Name = name,
                                ID = Context.ConnectionId,
                                IsAdmin = false // без прав админа
                            };

                            userAdded = ChatClients.TryAdd(name, newUser);
                            if (userAdded)
                            {
                                Clients.CallerState.UserName = name;
                                Clients.Others.ParticipantLogin(newUser);
                            }
                        }
                    }
                }
            }

            // возвращаем список пользователей только если новый пользователь был добавлен
            return userAdded ? users : null; // если пользователь уже существовал, возвращаем null
        }

        public void Logout()
        {
            var name = Clients.CallerState.UserName;
            if (!string.IsNullOrEmpty(name))
            {
                User client = new User();
                ChatClients.TryRemove(name, out client);
                Clients.Others.ParticipantLogout(name);
                Console.WriteLine($"-- {name} logged out.");
            }
        }

        // сообщение в беседу
        public void BroadcastTextMessage(string message)
        {
            var name = Clients.CallerState.UserName;
            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(message))
            {
                Clients.Others.BroadcastTextMessage(name, message);
            }
        }

        // посылаем сообщение пользователю
        public void UnicastTextMessage(string recepient, string message)
        {
            var sender = Clients.CallerState.UserName;
            if (!string.IsNullOrEmpty(sender) && recepient != sender &&
                !string.IsNullOrEmpty(message) && ChatClients.ContainsKey(recepient))
            {
                User client = new User();
                ChatClients.TryGetValue(recepient, out client);
                Clients.Client(client.ID).UnicastTextMessage(sender, message);
            }
        }

        // проверка на роль админа
        private bool IsUserAdmin(string userName)
        {
            return ChatClients.TryGetValue(userName, out User user) && user.IsAdmin;
        }

        // добавление пользователя (только админом)
        public int AddUser(string name, bool isAdmin)
        {
            var adminName = Clients.CallerState.UserName;
            int userAdded = 0; // для отслеживания добавления пользователя
            if (IsUserAdmin(adminName))
            {
                try
                {
                    using (var connection = new SQLiteConnection(ConnectionString))
                    {
                        connection.Open();
                        var command = connection.CreateCommand();
                        command.CommandText = "INSERT OR IGNORE INTO Users (Name, IsAdmin) VALUES (@name, @isAdmin)";
                        command.Parameters.AddWithValue("@name", name);
                        command.Parameters.AddWithValue("@isAdmin", isAdmin ? 1 : 0);
                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            Console.WriteLine($"++ {name} add as {(isAdmin ? "admin" : "user")}");
                            userAdded = 1;
                        }
                        else
                        {
                            Console.WriteLine($"-- User {name} already exists.");
                            userAdded = 3;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"-- Error adding user: {ex.Message}");
                    userAdded = 0;
                }
            }
            else
            {
                Console.WriteLine($"-- Access denied: {adminName} is not an admin.");
                userAdded = 2;
            }
            return userAdded;
        }

        // удаление пользователя (только админом)
        public int RemoveUser(string name)
        {
            var adminName = Clients.CallerState.UserName;
            int userRemove = 0; // для отслеживания удаления пользователя
            if (IsUserAdmin(adminName))
            {
                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    string deleteQuery = "DELETE FROM Users WHERE Name = @name";
                    using (var command = new SQLiteCommand(deleteQuery, connection))
                    {
                        command.Parameters.AddWithValue("@name", name);
                        command.ExecuteNonQuery();
                    }
                }
                ChatClients.TryRemove(name, out _);
                Clients.Others.ParticipantLogout(name);
                Console.WriteLine($"-- {name} has been removed from the system.");
                userRemove = 1;
            }
            else
            {
                Console.WriteLine($"-- Access denied: {adminName} is not an admin.");
                userRemove = 2;
            }
            return userRemove;
        }
    }
    
}
