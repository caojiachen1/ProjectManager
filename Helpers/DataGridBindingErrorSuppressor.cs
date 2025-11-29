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

            // Suppress this specific error in all builds (this is a known WPF issue)
            try
            {
                // Store existing listeners
                var existingListeners = PresentationTraceSources.DataBindingSource.Listeners;
                
                // Clear and add our custom listener first
                existingListeners.Clear();
                existingListeners.Add(new DataGridBindingErrorListener());
            }
            catch
            {
                // Ignore any errors in setting up the suppressor
            }
            
            _isInitialized = true;
        }

        private class DataGridBindingErrorListener : TraceListener
        {
            public override void Write(string message)
            {
                // Suppress CellsPanelHorizontalOffset binding errors - more comprehensive filtering
                if (IsCellsPanelHorizontalOffsetError(message))
                    return;
                
                // Write other messages to the default listener
                Debug.Write(message);
            }

            public override void WriteLine(string message)
            {
                // Suppress CellsPanelHorizontalOffset binding errors - more comprehensive filtering
                if (IsCellsPanelHorizontalOffsetError(message))
                    return;
                
                // Write other messages to the default listener
                Debug.WriteLine(message);
            }

            private bool IsCellsPanelHorizontalOffsetError(string message)
            {
                // Check for various forms of CellsPanelHorizontalOffset errors
                if (string.IsNullOrEmpty(message)) return false;
                
                // Check for the specific error patterns
                bool hasCellsPanel = message.Contains("CellsPanelHorizontalOffset");
                bool hasWidthBinding = message.Contains("Button.Width") || message.Contains("Width");
                bool hasNegativeValue = message.Contains("-") && (message.Contains("E-") || message.Contains("is not valid for target property"));
                
                return hasCellsPanel && hasWidthBinding && hasNegativeValue;
            }
        }
    }
}