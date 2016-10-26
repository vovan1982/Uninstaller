using System;

namespace Uninstaller.Utils
{
    public class MessageEventArgs : EventArgs
    {
        public string message { get; private set; }
        public MessageEventArgs(string message)
        {
            this.message = message;
        }

    }
}
