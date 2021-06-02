using System;

namespace uhttpsharp.Events
{
    public class LogEventArgs : EventArgs
    {
        #region Properties
        public string Message { get; private set; }

        public bool IsError { get; private set; }
        #endregion

        #region Constructor
        public LogEventArgs(string message, bool isError = false)
        {
            this.Message = message;
            this.IsError = isError;
        }
        #endregion
    }
}
