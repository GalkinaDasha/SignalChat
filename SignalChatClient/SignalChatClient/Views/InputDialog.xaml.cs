using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SignalChatClient.Views
{
    public partial class InputDialog : Window
    {
        public string InputValue { get; private set; }

        public InputDialog(string message)
        {
            InitializeComponent();
            this.Title = message;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            InputValue = InputTextBox.Text;
            this.DialogResult = true; // устанавливаем результат диалога как "ОК"
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false; // устанавливаем результат диалога как "Отмена"
            this.Close();
        }
    }
}
