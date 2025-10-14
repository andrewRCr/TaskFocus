using System;
using System.Collections.Generic;

namespace TaskFocusUI.Library.Logging
{
    public class InMemoryLog
    {
        public event Action OnChange;

        public List<string> Log { get; set; } = new List<string>();

        public void LogItem(string item)
        {
            Log.Add(item);
            OnChange?.Invoke();
        }
    }
}
