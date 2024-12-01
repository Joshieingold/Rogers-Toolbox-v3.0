using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Rogers_Toolbox_v3._0
{
    /// <summary>
    /// Interaction logic for InputWindow.xaml
    /// </summary>
    public partial class InputWindow : Window
    {
        public InputWindow()
        {
            InitializeComponent();
        }
        public string InputValue { get; private set; }
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            InputValue = InputTextBox.Text; // Get the value from the TextBox
            DialogResult = true; // Indicate success
            Close(); // Close the window
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false; // Indicate cancellation
            Close(); // Close the window
        }
    }
}
