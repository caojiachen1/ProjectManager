using System;
using System.Diagnostics;
using System.Windows.Controls;

namespace ProjectManager.Helpers
{
    /// <summary>
    /// Suppresses the known WPF DataGrid CellsPanelHorizontalOffset binding error
    /// This is a harmless warning that occurs when scrolling DataGrid horizontally
    /// </summary>
    public static class DataGridBindingErrorSuppressor
    {
        private static bool _isInitialized = false;

        /// <summary>
        /// Initializes the error suppressor for DataGrid binding errors
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized) return;

            // Only suppress this specific error in Debug builds
#if DEBUG
            PresentationTraceSources.DataBindingSource.Listeners.Clear();
            PresentationTraceSources.DataBindingSource.Listeners.Add(new DataGridBindingErrorListener());
#endif
            _isInitialized = true;
        }

        private class DataGridBindingErrorListener : TraceListener
        {
            public override void Write(string message)
            {
                // Suppress CellsPanelHorizontalOffset binding errors
                if (message.Contains("CellsPanelHorizontalOffset") && message.Contains("is not valid for target property"))
                    return;
                
                // Write other messages to the default listener
                Debug.Write(message);
            }

            public override void WriteLine(string message)
            {
                // Suppress CellsPanelHorizontalOffset binding errors
                if (message.Contains("CellsPanelHorizontalOffset") && message.Contains("is not valid for target property"))
                    return;
                
                // Write other messages to the default listener
                Debug.WriteLine(message);
            }
        }
    }
}