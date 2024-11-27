using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WindowsInput;  // This is the correct namespace
using Microsoft.Win32;
using ClosedXML.Excel;
using System.Linq;
using System.Diagnostics.Contracts;


namespace Rogers_Toolbox_v3._0
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Global Variables
        private InputSimulator inputSimulator = new InputSimulator();  // Initialize InputSimulator
        private List<string> serialsList = new List<string>(); // Stores the serials
        private int remainingSerials; // Stores the count of remaining serials
        public MainWindow()
        {
            InitializeComponent();
        }
        // Processing what happens on button Click
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Split the text of the TextBox into lines based on newline characters
            var lines = TextBox.Text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            // Create the line number text (e.g., 1, 2, 3, ...)
            var lineNumbers = string.Join("\n", lines.Select((line, index) => $"{index + 1}:"));

            // Update the line number label with the new line numbers
            LineNumberLabel.Text = lineNumbers;


        }
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            // If Sender is Blitz Import
            if (((Button)sender).Content.ToString() == "Blitz")
            {
                // Process the TextBox line by line
                string[] lines = TextBox.Text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                await Task.Delay(10000);  // Allows user to focus on the screen they want to import to

                foreach (string line in lines)
                {
                    await SimulateTyping(line);
                    SimulateTabKey();
                    serialsList.Remove(line);
                    UpdateSerialsDisplay();

                    // Short Delay after finishing a serial
                    await Task.Delay(10);  // Adjust delay as needed
                }
            }
            // If sender is the open excel button 
            else if (((Button)sender).Content.ToString() == "Import")
            {
                OpenExcel();
            }
            else if (((Button)sender).Content.ToString() == "CTR")
            {
                CombineExcels();
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
        public void CombineExcels()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select Excel Files to Combine",
                Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var filePaths = openFileDialog.FileNames;
                try
                {
                    CombineExcelFiles(filePaths);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to combine files: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void CombineExcelFiles(string[] filePaths)
        {
            var combinedWorkbook = new XLWorkbook();
            var combinedWorksheet = combinedWorkbook.Worksheets.Add("Combined");

            int currentRow = 1;

            foreach (var filePath in filePaths)
            {
                using (var workbook = new XLWorkbook(filePath))
                {
                    var worksheet = workbook.Worksheet(1); // Use the first worksheet
                    var rows = worksheet.RowsUsed();

                    foreach (var row in rows)
                    {
                        for (int col = 1; col <= row.LastCellUsed().Address.ColumnNumber; col++)
                        {
                            combinedWorksheet.Cell(currentRow, col).Value = row.Cell(col).Value;
                        }
                        currentRow++;
                    }
                }
            }

            SaveCombinedExcelFile(combinedWorkbook);
        }
        private void SaveCombinedExcelFile(XLWorkbook combinedWorkbook)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = "Save Combined Excel File",
                Filter = "Excel files (*.xlsx)|*.xlsx",
                FileName = "CTR-CombinedFile.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                combinedWorkbook.SaveAs(saveFileDialog.FileName);
                CTRUpdate();
            }
        }
        public void CTRUpdate()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select Excel File for CTR Update",
                Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var filePath = openFileDialog.FileName;
                try
                {
                    ProcessCTRUpdate(filePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to process CTR update: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void ProcessCTRUpdate(string filePath)
        {
            using (var workbook = new XLWorkbook(filePath))
            {
                var sheet = workbook.Worksheet(1); // Process the first sheet
                var results = AnalyzeSheet(sheet);
                SaveCTRResults(results);
            }
        }
        private void UpdateTotals(Dictionary<string, int> totals, string itemCode, List<string> allowedDevices, Dictionary<string,string> deviceMapping)
        {
            if (deviceMapping.ContainsKey(itemCode)) 
            {
                string device = deviceMapping[itemCode];  // Get the device from the mapping

                // Check if the device is in the allowedDevices list
                if (allowedDevices.Contains(device))
                {
                    // If the device is already in the totals dictionary, increment it; otherwise, initialize to 1
                    if (totals.ContainsKey(device))
                    {
                        totals[device]++;
                    }
                    else
                    {
                        totals[device] = 1;
                    }
                }
            }
        }
        private List<string> AnalyzeSheet(IXLWorksheet sheet)
        {
            var results = new List<string>();

            var CTRList = new HashSet<string> { "8052", "8067", "8975", "8986", "8990", "8994", "8997" };
            var robitailleList = new HashSet<string> { "8017", "8037", "8038", "8041", "8047", "8080", "8093" };
            var combinedCTRS = new HashSet<string> { "8993", "8982" };
            var warehouseList = new HashSet<string> { "NB1", "NF1" };

            var deviceMapping = new Dictionary<string, string>
            {
                {"CGM4981COM", "XB8"},
                {"CGM4331COM", "XB7"}, {"TG4482A", "XB7"},
                {"IPTVARXI6HD", "XI6"}, {"IPTVTCXI6HD", "XI6"},
                {"SCXI11BEI", "XIONE" },
                {"XE2SGROG1", "PODS"},
                {"XS010XB", "ONTS"}, {"XS010XQ", "ONTS"}, {"XS020XONT", "ONTS"},
                {"SCHB1AEW", "CAM1"},
                {"SCHC2AEW", "CAM2"},
                {"SCHC3AE0", "CAM3"},
                {"SCXI11BEI-ENTOS", "ENTOS" },
                {"MR36HW", "MR36HW" },
                {"S5A134A", "S5A134A" },
                {"CM8200A", "CM8200A" },
                {"CODA5810", "CODA5810" }
            };

            List<string> robitailleDevices = new List<string>
             {
                 "XB8", "XB7", "XI6", "XIONE", "PODS", "ONTS","SCHB1AEW", "SCHC2AEW", "SCHC3AEW", "SCXI11BEI-ENTOS","MR36HW", "S5A134A", "CM8200A", "CODA5810"
             };
            List<string> contractorDevices = new List<string>
            {
                "XB8", "XB7", "XI6", "XIONE", "PODS", "ONTS", "SCHB1AEW", "SCHC2AEW", "SCHC3AEW", "SCXI11BEI-ENTOS", "CODA5810"
            };
                
    
            // Processing robitaille
            foreach (var RobCTR in robitailleList)
            {
                var contractorTotals = robitailleDevices.ToDictionary(device => device, device => 0);
                foreach (var row in sheet.RowsUsed().Skip(2))
                {
                    var contractorId = row.Cell(8).GetValue<string>(); // Column H
                    var itemCode = row.Cell(6).GetValue<string>();     // Column F
                    var inventoryType = row.Cell(9).GetValue<string>();// Column J
                    if (contractorTotals.ContainsKey(itemCode) && inventoryType == $"CTR.Subready.{RobCTR}")
                    {
                        UpdateTotals(contractorTotals, itemCode, robitailleDevices, deviceMapping);
                    }
                }
                results.Add(FormatTotals(contractorTotals, robitailleDevices));
            }
            // Processing normal CTRS
            foreach (var contractor in CTRList)
            {
                var contractorTotals = contractorDevices.ToDictionary(device => device, device => 0);
                foreach (var row in sheet.RowsUsed().Skip(2))
                {
                    var contractorId = row.Cell(8).GetValue<string>(); // Column H
                    var itemCode = row.Cell(6).GetValue<string>();     // Column F
                    var inventoryType = row.Cell(9).GetValue<string>();// Column J
                    if (contractorTotals.ContainsKey(itemCode) && inventoryType == $"CTR.Subready.{contractor}")
                    {
                        UpdateTotals(contractorTotals, itemCode, contractorDevices, deviceMapping);
                    }
                }
                results.Add(FormatTotals(contractorTotals, contractorDevices));
            }
            // Processing Combined CTRS

            // Placing this outside in the hope that it will combine the numbers oganically
            var combinedContractorTotals = contractorDevices.ToDictionary(device => device, device => 0);

            foreach (var combinedCTR in combinedCTRS)
            {
                
                foreach (var row in sheet.RowsUsed().Skip(2))
                {
                    var contractorId = row.Cell(8).GetValue<string>(); // Column H
                    var itemCode = row.Cell(6).GetValue<string>();     // Column F
                    var inventoryType = row.Cell(9).GetValue<string>();// Column J
                    if (combinedContractorTotals.ContainsKey(itemCode) && inventoryType == $"CTR.Subready.{combinedCTR}")
                    {
                        UpdateTotals(combinedContractorTotals, itemCode, contractorDevices, deviceMapping);
                    }
                }
                results.Add(FormatTotals(combinedContractorTotals, contractorDevices));
            }

            // Processing Warehouses.

            foreach (var warehouse in warehouseList)
            {
                var warehouseTotals = robitailleDevices.ToDictionary(device => device, device => 0);
                foreach (var row in sheet.RowsUsed().Skip(2))
                {
                    var contractorId = row.Cell(2).GetValue<string>(); // Column B
                    var itemCode = row.Cell(6).GetValue<string>();     // Column F
                    var inventoryType = row.Cell(9).GetValue<string>();// Column J
                    if (contractorId == warehouse)
                    {
                        UpdateTotals(warehouseTotals, itemCode, robitailleDevices, deviceMapping);
                    }
                }
                results.Add(FormatTotals(warehouseTotals, contractorDevices));
            }

            return results;
        }
        private void SaveCTRResults(List<string> results)
        {
            return;
        }
        public string FormatTotals(Dictionary<string, int> totals, List<string> deviceOrder)
        {
            return string.Join(Environment.NewLine, deviceOrder.Select(device => totals.ContainsKey(device) ? totals[device].ToString() : "0"));
        }
    }
        
}
    
   