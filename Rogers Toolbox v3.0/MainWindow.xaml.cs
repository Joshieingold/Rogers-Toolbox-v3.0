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
using System.Diagnostics;
using System.IO;
using DocumentFormat.OpenXml.Packaging;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Google.Cloud.Firestore;
using static Rogers_Toolbox_v3._0.MainWindow;



namespace Rogers_Toolbox_v3._0
{
    public partial class MainWindow : Window
    {

        // Global Variables

        string username = "No User Assigned"; // Holds Data for username, primarily for textbox and database.
        static string bartenderNotepad = "not set"; // For printing labels, need to set path in settings.
        int blitzImportSpeed = 0; // The speed at which the blitz import will put between pasting serials. Informed by settings.
        int flexiImportSpeed = 0; // The speed at which the flexi import will put between checking if loading is done. Informed by settings.
        int wmsImportSpeed = 0; // The speed at which the wms import will put between pasting serials. Informed by settings.
        bool reverseImport = false; // Informs whether of not the list imported from exel will be flipper or not.
        int typingSpeed = 0; // The speed at which every individual serial is typed at.
        string flexiProCheckPixel = "not,set"; // The pixel that FlexiPro Import will check in order to know to proceed or not.
        string wmsCheckPixel = "not,set"; // The pixel that WMS Import will check in order to know if the serial passed or failed.
        bool isBomWip = true; // If true, when flexipro import is finished the data will be sent to database.
        static List<string> allContractors = new List<string> { "8017", "8037", "8038", "8041", "8047", "8080", "8093", "8052", "8067", "8975", "8986", "8990", "8994", "8997", "8993 and 8982", "NB1", "NF1", "Cleaning Up" }; //  List of all contractors to be updated.
        private InputSimulator inputSimulator = new InputSimulator();  // Initializes InputSimulator
        private static List<string> serialsList = new List<string>(); // Stores the serials once imported.
        private static List<string> passedList = new List<string>(); // For WMS import storing the passed serials.
        private static List<string> failedList = new List<string>(); // For WMS import storing the failed serials.
        private static int ctrImportSpeed = 0; // The speed that the user will get to click input locations between CTRS.
        private int remainingSerials; // Stores the count of remaining serials.

        private string CtrString = null;
        private string RobString = null;
        private bool CombineCTR = true;



        // Base Functions

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();
            InfoBox.Content = ($"Welcome to Rogers Toolbox v3.1 {username}");
        }
        private void LoadSettings() // Applies the users settings to the global variables.
        {
            username = Properties.Settings.Default.Username;
            bartenderNotepad = Properties.Settings.Default.BartenderNotepadPath;
            blitzImportSpeed = Properties.Settings.Default.BlitzImportSpeed;
            flexiImportSpeed = Properties.Settings.Default.FlexiImportSpeed;
            wmsImportSpeed = Properties.Settings.Default.WmsImportSpeed;
            reverseImport = Properties.Settings.Default.ReverseImport;
            typingSpeed = Properties.Settings.Default.TypingSpeed;
            flexiProCheckPixel = Properties.Settings.Default.FlexiproCheckPixel;
            wmsCheckPixel = Properties.Settings.Default.WMSCheckPixel;
            ctrImportSpeed = Properties.Settings.Default.CTRUpdateSpeed;
            isBomWip = Properties.Settings.Default.IsBomWip;

            CtrString = Properties.Settings.Default.CTRString;
            RobString = Properties.Settings.Default.RobitailleString;
            CombineCTR = Properties.Settings.Default.CombinedCTRsBool;




        }
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e) // Counts the new lines in the textbox to allow for a serial count next to the serials.
        {
            // Split the text of the TextBox into lines based on newline characters
            var lines = TextBox.Text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            // Create the line number text (e.g., 1, 2, 3, ...)
            var lineNumbers = string.Join("\n", lines.Select((line, index) => $"{index + 1}:"));

            // Update the line number label with the new line numbers
            LineNumberLabel.Text = lineNumbers;


        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Blitz Import
            if (((System.Windows.Controls.Button)sender).Content.ToString() == "Blitz")
            {
                BlitzImport();
            }
            // Import Excel
            else if (((System.Windows.Controls.Button)sender).Content.ToString() == "Import")
            {
                OpenExcel();
            }
            // CTR Update
            else if (((System.Windows.Controls.Button)sender).Content.ToString() == "CTR")
            {
                CombineExcels();
            }
            // Flexipro Import
            else if (((System.Windows.Controls.Button)sender).Content.ToString() == "Flexi")
            {
                FlexiProImport();
            }
            // WMS Import
            else if (((System.Windows.Controls.Button)sender).Content.ToString() == "WMS")
            {
                WMSImport();
            }
            // Print Purolator
            else if (((System.Windows.Controls.Button)sender).Content.ToString() == "Purolator")
            {
                CreatePurolatorSheet();
            }
            // Print Barcodes
            else if (((System.Windows.Controls.Button)sender).Content.ToString() == "Barcode")
            {
                CreateBarcodes();
            }
            // Print Lotsheet
            else if (((System.Windows.Controls.Button)sender).Content.ToString() == "LotSheet")
            {
                CreateLotSheet();
            }
        } // Executes functions based on GUI buttons.
        private void SettingsButton_Click(object sender, RoutedEventArgs e) // Opens the settings menu.
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog(); // Opens the settings window as a modal dialog
        }
        private void DatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            DataShowcaseForm databaseForm = new DataShowcaseForm();
            databaseForm.ShowDialog();
        } // Opens the Database Pannel.
        private void CompareListButton_Click(object sender, RoutedEventArgs e)
        {
            CompareLists compareLists = new CompareLists();
            compareLists.ShowDialog();
        } // Opens the Compare List Pannel.
        private void InputButton_Click(object sender, RoutedEventArgs e)
        {
            ShowInputDialog();
        } // Opens the Format serials pannel.
        private async Task SimulateTyping(string text)
        {
            foreach (char c in text)
            {
                // Use InputSimulator to simulate key press
                inputSimulator.Keyboard.TextEntry(c);  // Simulates typing the character
                await Task.Delay(typingSpeed);  // Adjust speed (lower is faster)
            }
        } // Types whatever string it is presented with.
        private void SimulateTabKey()
        {
            inputSimulator.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.RETURN);
        } // presses tab.
        private void SimulateKey(string key)
        {
            inputSimulator.Keyboard.KeyPress(WindowsInput.Native.VirtualKeyCode.RETURN);
        } // More universal function that allows ease in programming which key is pressed.
        private void UpdateSerialsDisplay()
        {
            {
                TextBox.Clear();
                TextBox.Text = string.Join(Environment.NewLine, serialsList);
                remainingSerials = serialsList.Count; // Update remaining se

                InfoBox.Content = ($"{remainingSerials} Serials Loaded");
            }
        } // Makes the textbox in main pannel update with the serials remaining.
        private void UpdateMessage(string text)
        {
            InfoBox.Content = (text);
        } // Updates the display box for the user to see what process is in action.

        // For Pasting Serials

        private async void BlitzImport()
        {
            // Lets the user know the import will begin soon and initializes the stopwatch.
            UpdateMessage("Starting Blitz Import! Please click input location"); 
            Stopwatch stopwatch = new Stopwatch();
            
            
            string[] lines = TextBox.Text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries); // Gets the data from the textbox and splits it by new lines.
            await Task.Delay(6000);  // focus time for input location
            
            // Starts stopwatch, prints all serials, and updates display accordingly.
            stopwatch.Start();
            foreach (string line in lines)
            {
                UpdateMessage($"Working on serial {line}, {serialsList.Count()} Serials Remaining");
                await SimulateTyping(line);
                SimulateTabKey();
                serialsList.Remove(line);
                UpdateSerialsDisplay();
                await Task.Delay(blitzImportSpeed);  // Allows user to control the speed in settings.
            }
            stopwatch.Stop(); // TIME!
            
            TimeSpan ts = stopwatch.Elapsed;
            string elapsedTime = String.Format("{0:00}h : {1:00}m : {2:00}s : {3:00} ms",
            ts.Hours, ts.Minutes, ts.Seconds,
            ts.Milliseconds / 10);
            UpdateMessage($"Import Completed in {elapsedTime}");
        } // Prints the serials as quickly as possible.
        private async void FlexiProImport()
        {
            string currentDevice = DetermineDevice(serialsList[0]); // Declare outside the block
            int quantityOfLoad = serialsList.Count(); // Declare outside the block
            DateTime i = DateTime.Now;
            DateTime utcDateTime = i.ToUniversalTime();

            string[] lines = TextBox.Text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            UpdateMessage("Starting FlexiPro Import! Please click input location");
            await Task.Delay(6000);  // Allows user to focus on the screen they want to import to
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (string line in lines)
            {
                UpdateMessage($"Working on serial {line}, {serialsList.Count()} Serials Remaining");
                bool isPixelGood = CheckPixel("(250, 250, 250)", GetCurrentPixel(flexiProCheckPixel));
                while (isPixelGood == false)
                {
                    await Task.Delay(700);
                    isPixelGood = CheckPixel("(250, 250, 250)", GetCurrentPixel(flexiProCheckPixel));
                }
                if (isPixelGood == true)
                {

                    await SimulateTyping(line);
                    await Task.Delay(100);
                    SimulateTabKey();
                    serialsList.Remove(line);
                    UpdateSerialsDisplay();
                    

                    // Short Delay after finishing a serial
                    await Task.Delay(flexiImportSpeed);
                }
            }
            if (isBomWip == true)
            {
                FirestoreHandler firestoreHandler = new FirestoreHandler();
                await firestoreHandler.PushToDatabase(currentDevice, username, quantityOfLoad, utcDateTime); // Pass DateTime directly
                UpdateMessage("Sending data to database");
            }
            stopwatch.Stop();
            TimeSpan ts = stopwatch.Elapsed;
            string elapsedTime = String.Format("{0:00}h : {1:00}m : {2:00}s : {3:00} ms",
            ts.Hours, ts.Minutes, ts.Seconds,
            ts.Milliseconds / 10);
            UpdateMessage($"Import Completed in {elapsedTime}");

        } // Prints the serials Whenever it finds the loading bar has changes.
        private async void WMSImport()
        {
            passedList.Clear();
            failedList.Clear();
            string[] lines = TextBox.Text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            UpdateMessage("Starting WMS Import! Please click input location");
            await Task.Delay(6000);  // Allows user to focus on the screen they want to import to
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (string line in lines)
            {
                UpdateMessage($"Working on serial {line}, {serialsList.Count()} Serials Remaining");
                await SimulateTyping(line);
                await Task.Delay(200);// changed here to possibly address sorting bug
                SimulateTabKey();
                await Task.Delay(1000);

                bool isPixelGood = CheckPixel("(0, 0, 0)", GetCurrentPixel(wmsCheckPixel));
                if (isPixelGood == true)
                {
                    passedList.Add(line);
                }
                else
                {
                    failedList.Add(line);
                    WMSFailAutomation();
                }
                serialsList.Remove(line);
                UpdateSerialsDisplay();
                
                await Task.Delay(wmsImportSpeed);
            }
            stopwatch.Stop();
            TimeSpan ts = stopwatch.Elapsed;
            string elapsedTime = String.Format("{0:00}h : {1:00}m : {2:00}s : {3:00} ms",
            ts.Hours, ts.Minutes, ts.Seconds,
            ts.Milliseconds / 10);
            UpdateMessage($"Import Completed in {elapsedTime}");

            ResultsWindow resultsWindow = new ResultsWindow(passedList, failedList);
            resultsWindow.Show();


        } // Prints the serials as quickly as the display will allow splitting them into the passed and failed list.
        private void WMSFailAutomation()
        {
            var sim = new InputSimulator();
            sim.Keyboard.ModifiedKeyStroke(WindowsInput.Native.VirtualKeyCode.CONTROL, WindowsInput.Native.VirtualKeyCode.VK_X);
        } // Preforms specific keys in the case a serial fails.
        private bool CheckPixel(string colorWanted, string colorFound)
        {
            if (colorWanted == colorFound)
            {
                return true; // Returns True if they match
            }
            else
            {
                return false;
            }
        } // Checks between the color the programmer wants and the color found at the pixel on the screen stipulated. 
        private string GetCurrentPixel(string pixelSource)
        {
            string[] cords = pixelSource.Split(',');
            int xCord = Convert.ToInt32(cords[0]);
            int yCord = Convert.ToInt32(cords[1]);
            System.Drawing.Point ixelCords = new System.Drawing.Point(xCord, yCord);

            // Capture the screen
            Bitmap screenshot = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            using (Graphics graphics = Graphics.FromImage(screenshot))
            {
                graphics.CopyFromScreen(new System.Drawing.Point(0, 0), new System.Drawing.Point(0, 0), screenshot.Size);
            }

            // Get the color of the pixel at the specified coordinates
            Color pixelColor = screenshot.GetPixel(xCord, yCord);

            // Format the color as "(R, G, B)"
            string colorFound = $"({pixelColor.R}, {pixelColor.G}, {pixelColor.B})";

            return colorFound;
        } // Finds the color of a pixel on the screen.

        // For Importing Serials from Excel

        private void OpenExcel()
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Open an Excel file for use",
                Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                LoadSerials(openFileDialog.FileName);
            }
        } // Establishes a path to the target excel for importing serials.
        private void LoadSerials(string filePath)
        {
            try
            {
                using (var workbook = new XLWorkbook(filePath))
                {
                    var worksheet = workbook.Worksheet(1); // Load the first sheet
                    serialsList.Clear(); // clears old serial list

                    // Iterate through rows in the first column and get their values.
                    foreach (var row in worksheet.RowsUsed())
                    {
                        var cellValue = row.Cell(1).GetValue<string>();
                        if (!string.IsNullOrWhiteSpace(cellValue))
                        {
                            serialsList.Add(cellValue);
                        }
                    }
                }
                // reverses serials if the use wishes.
                if (reverseImport == true)
                {
                    ReverseSerials(serialsList);

                }
                // Update remaining serials and display
                remainingSerials = serialsList.Count;
                UpdateSerialsDisplay();
                LoadSettings();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to load serials: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        } // Gets all data form the first column of the loaded excel file/
        private void ReverseSerials(List<string> Serials)
        {
            Serials.Reverse();
            serialsList = Serials;
        } // Reverses the serial list if the option == true.

        // For CTR Update

        public void CombineExcels()
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
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
                    System.Windows.MessageBox.Show($"Failed to combine files: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        } // Opens file dialog asking for paths to multiple excel files.
        private void CombineExcelFiles(string[] filePaths)
        {
            // creates a new workbook
            var combinedWorkbook = new XLWorkbook();
            var combinedWorksheet = combinedWorkbook.Worksheets.Add("Combined");

            int currentRow = 1;

            foreach (var filePath in filePaths)
            {
                using (var workbook = new XLWorkbook(filePath))
                {
                    var worksheet = workbook.Worksheet(1); // Use the first worksheet
                    var rows = worksheet.RowsUsed();
                    // gets all data from all rows and columns of selected files and adds them to the combined sheet.
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
        } // Takes the paths to excel files and combines them into one excel file.
        private void SaveCombinedExcelFile(XLWorkbook combinedWorkbook)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
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
        } // Allows the user to save the combined file somewhere on their pc
        public void CTRUpdate()
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
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
                    System.Windows.MessageBox.Show($"Failed to process CTR update: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        } // Asks the user to open a combined file in order to continue the update.
        private void ProcessCTRUpdate(string filePath)
        {
            using (var workbook = new XLWorkbook(filePath))
            {
                var sheet = workbook.Worksheet(1); // Process the first sheet
                var results = AnalyzeSheet(sheet);
                _ = SaveCTRResults(results);
            }
        } // executes the main process by grabbing all neccesary data.
        public async Task CtrAutomation(string contractorData)
        {
            // Copy contractorData to clipboard
            System.Windows.Clipboard.SetText(contractorData);

            // Pause for 3 seconds to ensure clipboard operation completes
            await Task.Delay(3000);

            var sim = new InputSimulator();

            // Simulate Ctrl+V (Paste)
            sim.Keyboard.ModifiedKeyStroke(WindowsInput.Native.VirtualKeyCode.CONTROL, WindowsInput.Native.VirtualKeyCode.VK_V);

            // Pause for 3 seconds after pasting
            await Task.Delay(3000);

            // Simulate Ctrl+Alt+PageDown
            sim.Keyboard.ModifiedKeyStroke(
                new[] { WindowsInput.Native.VirtualKeyCode.CONTROL, WindowsInput.Native.VirtualKeyCode.MENU },
                WindowsInput.Native.VirtualKeyCode.NEXT);

            // Simulate Ctrl+Left
            sim.Keyboard.ModifiedKeyStroke(
                WindowsInput.Native.VirtualKeyCode.CONTROL,
                WindowsInput.Native.VirtualKeyCode.LEFT);

            // Pause for the specified import speed
            await Task.Delay(ctrImportSpeed); // MAYBE WE DONT NEED TWO????
        } //  Pastes the data for the ctr that is called.
        private void UpdateTotals(Dictionary<string, int> totals, string itemCode, List<string> allowedDevices, Dictionary<string, string> deviceMapping)
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
        } // Uses a dictionary to get data totals.

        private List<string> ConvertStringsToCTR(string SettingsList)
        {
            // Split the input string into an array of strings
            string[] items = SettingsList.Split(' ');

            // Create a List to maintain order and uniqueness
            List<string> orderedUniqueList = new List<string>();

            // Iterate over each item
            foreach (string item in items)
            {
                // Add the item to the list only if it is not already present
                if (!orderedUniqueList.Contains(item))
                {
                    orderedUniqueList.Add(item);
                }
            }

            // Return the unique and ordered list
            return orderedUniqueList;
        }
        private List<string> AnalyzeSheet(IXLWorksheet sheet) // loops through each contractor (many times) and stores the data for them given their list of devices.
        {
            var results = new List<string>();




            var CTRList = ConvertStringsToCTR(CtrString);
            var robitailleList = ConvertStringsToCTR(RobString);
            var combinedCTRS = new List<string> { "8993", "8982" };
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
                {"MR36HW", "MERAKI" },
                {"S5A134A", "CRADLEPOINT" },
                {"CM8200A", "SOMEDEVICE" },
                {"CODA5810", "CODA" }
            };

            List<string> robitailleDevices = new List<string>
             {
                 "XB8", "XB7", "XI6", "XIONE", "PODS", "ONTS","CAM1", "CAM2", "CAM3", "ENTOS","MERAKI", "CRADLEPOINT", "SOMEDEVICE", "CODA"
             };
            List<string> contractorDevices = new List<string>
            {
                "XB8", "XB7", "XI6", "XIONE", "PODS", "ONTS","CAM1", "CAM2", "CAM3", "ENTOS", "CODA"
            };

            allContractors = new List<string>();
            foreach (string robctr in robitailleList )
            {
                allContractors.Add(robctr);
            }
            foreach (string ctr in CTRList)
            {
                allContractors.Add(ctr);
            }
            if (CombineCTR) 
            {
                allContractors.Add("8993 & 8982");
            }
            foreach (string house in warehouseList)
            {
                allContractors.Add(house);
            }
            allContractors.Add("Cleaning Up");



            // Processing robitaille
            foreach (var RobCTR in robitailleList)
            {
                var robTotals = robitailleDevices.ToDictionary(device => device, device => 0);
                foreach (var row in sheet.RowsUsed().Skip(2))
                {
                    var contractorId = row.Cell(8).GetValue<string>(); // Column H
                    var itemCode = row.Cell(6).GetValue<string>();     // Column F
                    var inventoryType = row.Cell(10).GetValue<string>();// Column J
                    if (contractorId == RobCTR && inventoryType.StartsWith("CTR.Subready."))
                    {
                        UpdateTotals(robTotals, itemCode, robitailleDevices, deviceMapping);
                    }

                }
                results.Add(FormatTotals(robTotals, robitailleDevices));
            }

            // Processing normal CTRS

            foreach (var contractor in CTRList)
            {
                var contractorTotals = contractorDevices.ToDictionary(device => device, device => 0);
                foreach (var row in sheet.RowsUsed().Skip(2))
                {
                    var contractorId = row.Cell(8).GetValue<string>(); // Column H
                    var itemCode = row.Cell(6).GetValue<string>();     // Column F
                    var inventoryType = row.Cell(10).GetValue<string>();// Column J
                    if (contractorId == contractor && inventoryType.StartsWith("CTR.Subready."))
                    {
                        UpdateTotals(contractorTotals, itemCode, contractorDevices, deviceMapping);
                    }
                }
                results.Add(FormatTotals(contractorTotals, contractorDevices));
            }
            // Processing Combined CTRS

            // Placing this outside in the hope that it will combine the numbers oganically
            if (CombineCTR)
            {
                var combinedContractorTotals = contractorDevices.ToDictionary(device => device, device => 0);
                foreach (var row in sheet.RowsUsed().Skip(2))
                {
                    var contractorId = row.Cell(8).GetValue<string>().Trim(); // Column H
                    var itemCode = row.Cell(6).GetValue<string>();           // Column F
                    var inventoryType = row.Cell(10).GetValue<string>();      // Column J

                    if ((contractorId == "8993" || contractorId == "8982") && inventoryType.StartsWith("CTR.Subready."))
                    {
                        UpdateTotals(combinedContractorTotals, itemCode, contractorDevices, deviceMapping);
                    }
                }
                var formattedTotals = FormatTotals(combinedContractorTotals, contractorDevices);
                results.Add(formattedTotals);
            }
            


            // Processing Warehouses.

            foreach (var warehouse in warehouseList)
            {
                var warehouseTotals = robitailleDevices.ToDictionary(device => device, device => 0);
                foreach (var row in sheet.RowsUsed().Skip(2))
                {
                    var contractorId = row.Cell(2).GetValue<string>(); // Column B
                    var itemCode = row.Cell(6).GetValue<string>();     // Column F
                    var inventoryType = row.Cell(10).GetValue<string>();// Column J
                    if (contractorId == warehouse)
                    {
                        UpdateTotals(warehouseTotals, itemCode, robitailleDevices, deviceMapping);
                    }
                }
                results.Add(FormatTotals(warehouseTotals, robitailleDevices));
            }

            return results;
        }
        private async Task SaveCTRResults(List<string> results) // loops through all stored data and uses the ctr automation for it.
        {
            int count = 0;

            foreach (string data in results)
            {
                // Update the InfoBox before starting the automation
                InfoBox.Content = $"Updating {allContractors[count]}";

                // Add a delay for visual feedback
                await Task.Delay(ctrImportSpeed); // hopefully this is enough time between ctrs? MAYBE WE DONT NEED TWO????

                // Ensure we await the CtrAutomation method
                await CtrAutomation(data); // This will now wait for the CtrAutomation to complete before moving on

                count++;
            }
            InfoBox.Content = $"CTR Update Completed!";
        }
        public string FormatTotals(Dictionary<string, int> totals, List<string> deviceOrder)
        {
            return string.Join(Environment.NewLine, deviceOrder.Select(device => totals.ContainsKey(device) ? totals[device].ToString() : "0"));
        } // uses dictionary of devices to format them into a single string seperated by new lines.

        // For Printing

        public void CreatePurolatorSheet() // Prints purolator sheet based on device information gathered globally using a cmd script.
        {
            string[] lines = TextBox.Text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            string device = DetermineDevice(lines[0]);
            UpdateMessage($"Creating Purolator sheets for {device}");
            if (device == "IPTVARXI6HD" || device == "IPTVTCXI6HD" || device == "SCXI11BEI")
            {
                int formatBy = 10;
                string puroSheet = FormatSheet(formatBy);
                
                // Write Purolator sheet to notepad
                File.WriteAllText(bartenderNotepad, puroSheet + Environment.NewLine);

                // Create and execute batch file
                string cmdScript = @" @echo off
                                    set ""target_printer=55EXP_Purolator""
                                    powershell -Command ""Get-WmiObject -Query 'SELECT * FROM Win32_Printer WHERE ShareName=''%target_printer%'' ' | Invoke-WmiMethod -Name SetDefaultPrinter""
                                    ""C:\Seagull\BarTender 7.10\Standard\bartend.exe"" /f=C:\BTAutomation\XI6.btw /p /x
                                    ";
                ExecuteBatchScript(cmdScript);
            }
            else
            {
                int formatBy = 8;
                string puroSheet = FormatSheet(formatBy);

                // Write Purolator sheet to notepad
                File.WriteAllText(bartenderNotepad, puroSheet + Environment.NewLine);

                // Create and execute batch file
                string cmdScript = @"
                                    @echo off
                                    set ""target_printer=55EXP_Purolator""
                                    powershell -Command ""Get-WmiObject -Query 'SELECT * FROM Win32_Printer WHERE ShareName=''%target_printer%'' ' | Invoke-WmiMethod -Name SetDefaultPrinter""
                                    ""C:\Seagull\BarTender 7.10\Standard\bartend.exe"" /f=C:\BTAutomation\CODA.btw /p /x
                                    ";
                ExecuteBatchScript(cmdScript);
            }
        }
        public static string DetermineDevice(string serial)
        {
            if (serial.StartsWith("TM"))
                return "IPTVTCXI6HD";
            else if (serial.StartsWith("M"))
                return "IPTVARXI6HD";
            else if (serial.StartsWith("409"))
                return "CGM4981COM";
            else if (serial.StartsWith("XI1"))
                return "SCXI11BEI";
            else if (serial.StartsWith("336"))
                return "CGM4331COM";
            else if (serial.StartsWith("AS97"))
                return "XE2SGROG1";
            else
                return "TG4482A";
        } //  Determines the device model based on the serial number.
        public void ExecuteBatchScript(string scriptContent)
        {
            UpdateMessage("Executing the Batch File");
            string tempFilePath = "temp_cmd.bat";

            // Write script content to a temporary file
            File.WriteAllText(tempFilePath, scriptContent);

            // Execute the batch file
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {tempFilePath}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();

            // Clean up temporary file
            File.Delete(tempFilePath);
            UpdateMessage("Finished Printing!");

        } // executes a cmd script given to it.
        public string FormatSheet(int numSplit)
        {
            UpdateMessage("Formatting the Serials");
            // Split TextBox content into lines, removing empty entries
            string[] lines = TextBox.Text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            if (lines == null || lines.Length == 0)
            {
                return "No serials available.";
            }

            int totalStrings = lines.Length;
            StringBuilder formattedList = new StringBuilder();

            for (int i = 0; i < totalStrings; i += numSplit)
            {
                // Split into chunks and reverse each chunk
                List<string> chunk = lines.Skip(i).Take(numSplit).ToList();
                chunk.Reverse();

                // Example placeholder for device determination
                formattedList.AppendLine(DetermineDevice(chunk[0]));

                // Append the reversed chunk to the formatted list
                formattedList.AppendLine(string.Join(Environment.NewLine, chunk));
            }

            return formattedList.ToString();
        }
        public void CreateLotSheet()
        {
            UpdateMessage("Printing your lot sheets");
            string[] lines = TextBox.Text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries); // NEW
            // string serialString = String.Join(Environment.NewLine, serialsList);
            string serialString = String.Join(Environment.NewLine, lines); // Maybe this gets the serial list and puts it for the lot sheet?
            File.WriteAllText(bartenderNotepad, serialString + Environment.NewLine);

            // Create and execute batch file
            string cmdScript = @" @echo off
                                    set ""target_printer=55EXP_2""
                                    powershell -Command ""Get-WmiObject -Query 'SELECT * FROM Win32_Printer WHERE ShareName=''%target_printer%'' ' | Invoke-WmiMethod -Name SetDefaultPrinter""
                                    ""C:\Seagull\BarTender 7.10\Standard\bartend.exe"" /f=C:\BTAutomation\NewPrintertest.btw /p /x
                                    ";
            ExecuteBatchScript(cmdScript);

        } // prints all serials to a lot sheet.
        public void CreateBarcodes()
        {
            UpdateMessage("Creating your barcodes");
            string[] lines = TextBox.Text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries); // NEW
            // string serialString = String.Join(Environment.NewLine, serialsList);
            string serialString = String.Join(Environment.NewLine, lines); // Maybe this gets the serial list and puts it for the barcodes?
            File.WriteAllText(bartenderNotepad, serialString + Environment.NewLine);

            // Create and execute batch file
            string cmdScript = @" @echo off
                                    set ""target_printer=55EXP_Barcode""
                                    powershell -Command ""Get-WmiObject -Query 'SELECT * FROM Win32_Printer WHERE ShareName=''%target_printer%'' ' | Invoke-WmiMethod -Name SetDefaultPrinter""
                                    ""C:\Seagull\BarTender 7.10\Standard\bartend.exe"" /f=C:\BTAutomation\singlebar.btw /p /x
                                    ";
            ExecuteBatchScript(cmdScript);

        } // prints all serials to the barcode printer.

        // For Serial Formatter

        private void ShowInputDialog()
        {
            var inputWindow = new InputWindow();
            inputWindow.Owner = this; // Set the owner to the main window
            if (inputWindow.ShowDialog() == true)
            {
                string userInput = inputWindow.InputValue;
                string[] lines = TextBox.Text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                string outputText = String.Join(userInput, lines);
                System.Windows.Clipboard.SetText(outputText);
                InfoBox.Content = ($"Okay {username}, all serials copied with '{userInput}' between them!");
            }
            if (inputWindow.UpperCaseClick == true)
            {
                serialsList = MakeSerialsUppercase();
                UpdateSerialsDisplay();
                LoadSettings();
            }
            if (inputWindow.DuplicateFind == true)
            {
                serialsList = RemoveDuplicates();
                UpdateSerialsDisplay();
                LoadSettings();
            }
        } // Opens the format serials box.
        private List<string> MakeSerialsUppercase() 
        {
            string[] lines = TextBox.Text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> formattedList = new List<string>();

            foreach (string line in lines) 
            {
                string uppercaseString = line.ToUpper();
                formattedList.Add(uppercaseString);
            }
            
            return formattedList;
        }
        private List<string> RemoveDuplicates()
        {
            string[] lines = TextBox.Text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            HashSet<string> uniqueSerials = new HashSet<string>();
            List<string> duplicates = new List<string>();

            foreach (string line in lines)
            {
                if (!uniqueSerials.Add(line))
                {
                    duplicates.Add(line); // Track duplicates
                }
            }

            List<string> result = new List<string>();

            // Add duplicates section
            result.Add($"Found {duplicates.Count} duplicates:");
            result.AddRange(duplicates);
            result.Add("\n");

            // Add separator and unique serials section
            result.Add("Unique Serials:");
            result.AddRange(uniqueSerials);

            return result;
        }



        // For Database 
        public class FirestoreService
        {
            private static FirestoreDb _firestoreDb;
            private static readonly object _lock = new object();

            public static FirestoreDb GetFirestoreDb()
            {
                if (_firestoreDb == null)
                {
                    lock (_lock) // Ensure thread safety
                    {
                        if (_firestoreDb == null) // Double-check locking
                        {
                            try
                            {
                                // Set the path to your service account key
                                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Keys", "bomwipstore-firebase-adminsdk-jhqev-acb5705838.json");
                                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", filePath);

                                // Create FirestoreDb instance
                                string projectId = "bomwipstore"; // Replace with your Firestore Project ID
                                _firestoreDb = FirestoreDb.Create(projectId);
                            }
                            catch (Exception ex)
                            {
                                // Handle initialization error (e.g., log it)
                                Console.WriteLine($"Error initializing Firestore: {ex.Message}");
                                throw; // Optionally rethrow the exception
                            }
                        }
                    }
                }
                return _firestoreDb;
            }
        } // initialize firestore.

        public class FirestoreHandler
        {
            private FirestoreDb _db;

            public FirestoreHandler()
            {
                _db = FirestoreService.GetFirestoreDb();
            }

            public async Task PushToDatabase(string device, string name, int quantity, DateTime date) // pushes data to database
            {
                if (string.IsNullOrEmpty(device) || string.IsNullOrEmpty(name) || quantity <= 0)
                    throw new ArgumentException("Invalid data provided.");

                // Convert DateTime to UTC
                DateTime utcDateTime = date.ToUniversalTime();

                DocumentReference docRef = _db.Collection("bom-wip").Document();
                var data = new
                {
                    Device = device,
                    Name = name,
                    Quantity = quantity,
                    Date = Timestamp.FromDateTime(utcDateTime) // Convert DateTime to Firestore Timestamp
                };

                try
                {
                    await docRef.SetAsync(data);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error pushing data to Firestore: {ex.Message}");
                    throw; 
                }
            }
        } // formats how to push the data to firestore

    }
}


// TO DO:
// 6. Make the buttons have some highlight on mouse over.


// For version 3.2:
// 4. Make a way to easily edit data in the database to account for errors.
// 1. The print lots sheets should open a dialog box that will also make the outside papers for you if you select yes. 
// 10. Have a static link to an excel file in settings that will allow for comparing with ERP data, similar to the existing comparison tool.
// 10.1. Have a splitter that allows for you to split serials of a list into different lists that all have their own import options.
// 10.2. Have a function that allows for the serials to be split based on the devices determined.
// 10.3. Have a window dedicated to showing the results.
// 10.4. Have this window have 3 small import buttons below each list.
// 14. Maybe someday I can add an option to push to database based on your company. This is not so neccesary but yk it will be nice to implement in case.

// COMPLETED:
// x. Make printing things use the textbox not the serial list. 
// x. The actual FormatSheet function needs to be getting its data from the textbox.
// x. Printing should have progress updates based on the process being done, not just once it is finished. 
// x. Add function to make all serials in textbox capital.
// x. Option to capitalize your serials in the textbox.
// x. Make processing function that will update the display for the user.
// x. Make this option appear in the Format Serials window. 
// x. Option to remove duplicates in the textbox.
// x. Make the CTR's in the CTR sheet customizable in settings.
// x. The database UI still needs to scale to full screen view.
// x. Make the CTR Import speed actually control the speed at which imports happen.
// x. Optimize the speed of the CTR import a bit more.