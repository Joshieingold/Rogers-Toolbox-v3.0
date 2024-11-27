using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WindowsInput;  // This is the correct namespace
using Microsoft.Win32;
using ClosedXML.Excel;

namespace Rogers_Toolbox_v3._0
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private InputSimulator inputSimulator = new InputSimulator();  // Initialize InputSimulator
        private List<string> serialsList = new List<string>(); // Stores the serials
        private int remainingSerials; // Stores the count of remaining serials
        public MainWindow()
        {
            InitializeComponent();
        }
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            // Check if the sender is the Blitz button
            if (((Button)sender).Content.ToString() == "Blitz")
            {
                // Process the TextBox line by line
                string[] lines = TextBox.Text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                await Task.Delay(10000);  // Initial delay to simulate waiting

                foreach (string line in lines)
                {
                    // Simulate typing the line
                    await SimulateTyping(line);

                    // Simulate pressing Tab (move to next field, or simulate with other keys if needed)
                    SimulateTabKey();
                    serialsList.Remove(line);
                    UpdateSerialsDisplay();

                    // Wait a moment to simulate human-like behavior
                    await Task.Delay(100);  // Adjust delay as needed
                }
            }
            else if (((Button)sender).Content.ToString() == "Import")
            {
                OpenExcel();
            }
        }
        private async Task SimulateTyping(string text)
        {
            foreach (char c in text)
            {
                // Use InputSimulator to simulate key press
                inputSimulator.Keyboard.TextEntry(c);  // Simulates typing the character
                await Task.Delay(5);  // Adjust speed (lower is faster)
            }
        }
        private void SimulateTabKey() // Presses Tab
        {
            inputSimulator.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.RETURN);
        }
        private void UpdateSerialsDisplay()
        {
            {
                TextBox.Clear();
                TextBox.Text = string.Join(Environment.NewLine, serialsList);
                remainingSerials = serialsList.Count; // Update remaining se
                                                      // rial count
                InfoBox.Content = ($"{remainingSerials} Serials Loaded");
            }
        }
        private void OpenExcel()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Open an Excel file for use",
                Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                LoadSerials(openFileDialog.FileName);
            }
        }
        private void LoadSerials(string filePath)
        {
            try
            {
                using (var workbook = new XLWorkbook(filePath))
                {
                    var worksheet = workbook.Worksheet(1); // Load the first sheet
                    serialsList.Clear();

                    // Iterate through rows in the first column
                    foreach (var row in worksheet.RowsUsed())
                    {
                        var cellValue = row.Cell(1).GetValue<string>();
                        if (!string.IsNullOrWhiteSpace(cellValue))
                        {
                            serialsList.Add(cellValue);
                        }
                    }
                }

                // Update remaining serials and display
                remainingSerials = serialsList.Count;
                UpdateSerialsDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load serials: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CTRUpdate()
        {

        }
        public void CombineExcels()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Open an Excel files to combine",
                Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*"
            };
        }
    }
}
