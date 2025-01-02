using Newtonsoft.Json.Linq;
using System;
using System.Windows;

namespace Rogers_Toolbox_v3._0
{
    public partial class SettingsWindow : Window
    {
        // Event to notify settings were saved
        public event EventHandler SettingsSaved;

        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Save settings
            Properties.Settings.Default.Save();

            // Raise the SettingsSaved event
            SettingsSaved?.Invoke(this, EventArgs.Empty);

            // Close the settings window
            this.Close();
        }
    }
}
