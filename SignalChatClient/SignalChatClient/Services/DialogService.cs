using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Windows;
using SignalChatClient.Views;

namespace SignalChatClient.Services
{
    public class DialogService : IDialogService
    {
        public (string InputValue, bool IsAdmin) ShowInputDialog(string message)
        {
            var inputDialog = new InputDialog(message);
            bool isConfirmed = inputDialog.ShowDialog() == true; // если пользователь нажал "ОК"

            bool isAdminFlag = inputDialog.AdminFlag.IsChecked == true; // Проверяем, что CheckBox отмечен

            return (isConfirmed ? inputDialog.InputValue : null, isAdminFlag);
        }

        public bool ShowConfirmationDialog(string message)
        {
            // Используем MessageBox для отображения диалогового окна подтверждения
            var result = MessageBox.Show(message, "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes; // возвращаем true, если пользователь нажал "Да"
        }

        public bool ShowConfirmationRequest(string message, string caption = "")
        {
            var result = MessageBox.Show(message, caption, MessageBoxButton.OKCancel);
            return result.HasFlag(MessageBoxResult.OK);
        }

        public void ShowNotification(string message, string caption = "")
        {
            MessageBox.Show(message, caption); ;
        }
    }
}
