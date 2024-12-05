using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Google.Cloud.Firestore;

namespace Rogers_Toolbox_v3._0
{
    public partial class DataShowcaseForm : Window
    {
        private FirestoreDb firestoreDb;

        public DataShowcaseForm()
        {
            InitializeComponent();
            InitializeFirestore();
        }

        private void InitializeFirestore()
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Keys", "bomwipstore-firebase-adminsdk-jhqev-acb5705838.json");
            string pathToKey = filePath; // Update with your key's path
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", pathToKey);
            firestoreDb = FirestoreDb.Create("bomwipstore"); // Replace with your project ID
        }

        private async void FetchDataAsync()
        {
            try
            {
                Console.WriteLine("Fetching data from the bom-wip collection within the selected date range.");

                // Get the selected start and end dates
                DateTime? startDate = startDatePicker.SelectedDate;
                DateTime? endDate = endDatePicker.SelectedDate;

                // Check if both dates are selected
                if (startDate == null || endDate == null)
                {
                    MessageBox.Show("Please select both start and end dates.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Ensure the end date is after the start date
                if (endDate < startDate)
                {
                    MessageBox.Show("End date must be after start date.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Convert to UTC
                DateTime startDateUtc = startDate.Value.ToUniversalTime();
                DateTime endDateUtc = endDate.Value.ToUniversalTime();

                // Create a query with date range filtering
                Query query = firestoreDb.Collection("bom-wip")
                    .WhereGreaterThanOrEqualTo("Date", Timestamp.FromDateTime(startDateUtc))
                    .WhereLessThanOrEqualTo("Date", Timestamp.FromDateTime(endDateUtc));

                QuerySnapshot snapshot = await query.GetSnapshotAsync();
                Console.WriteLine($"Documents fetched: {snapshot.Documents.Count}");

                if (snapshot.Documents.Count == 0)
                {
                    Console.WriteLine("No documents found.");
                }
                TimeZoneInfo astTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Atlantic Standard Time");
                List<DataRecord> records = snapshot.Documents.Select(document =>
                {
                    var data = document.ToDictionary();
                    DateTime dateValue;

                    // Safely try to get the Date field as a Firestore Timestamp
                    if (data.TryGetValue("Date", out object dateObj) && dateObj is Timestamp timestamp)
                    {
                        // Convert Firestore Timestamp to DateTime
                        dateValue = timestamp.ToDateTime();

                        // Convert the DateTime to UTC-4 (AST)
                        dateValue = TimeZoneInfo.ConvertTimeFromUtc(dateValue, astTimeZone);
                    }
                    else
                    {
                        // Handle the case where the Date is missing or not a Timestamp
                        dateValue = DateTime.MinValue; // or set to a default value
                        Console.WriteLine("Date field is missing or not a Timestamp.");
                    }

                    return new DataRecord
                    {
                        Device = data["Device"]?.ToString(),
                        Name = data["Name"]?.ToString(),
                        Quantity = Convert.ToInt32(data["Quantity"]),
                        Date = dateValue
                    };
                }).ToList();

                dataGrid.ItemsSource = records;

                UpdateSummaries(records);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fetching data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateSummaries(List<DataRecord> records)
        {
            // Calculate device totals
            var deviceTotals = records
                .GroupBy(r => r.Device)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.Quantity));

            // Calculate user totals
            var userTotals = records
                .GroupBy(r => r.Name)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.Quantity));

            // Update UI
            deviceSumLabel.Text = string.Join(Environment.NewLine,
                deviceTotals.Select(d => $"{d.Key}: {d.Value}"));

            personTotalLabel.Text = string.Join(Environment.NewLine,
                userTotals.Select(u => $"{u.Key}: {u.Value}"));
        }

        private void fetchDataButton_Click(object sender, RoutedEventArgs e)
        {
            // Call FetchDataAsync without date parameters
            FetchDataAsync();
        }

        public class DataRecord
        {
            public string Device { get; set; }     // Name of the device
            public string Name { get; set; }       // Name of the user
            public int Quantity { get; set; }      // Quantity completed
            public DateTime Date { get; set; }     // Date of completion
        }
    }
}