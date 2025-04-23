using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalChatClient.Services
{
    public interface IDialogService
    {
        (string InputValue, bool IsAdmin) ShowInputDialog(string message);
        bool ShowConfirmationDialog(string message);
        void ShowNotification(string message, string caption = "");
        bool ShowConfirmationRequest(string message, string caption = "");
    }
}
