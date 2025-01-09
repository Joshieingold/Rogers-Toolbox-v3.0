using LiveCharts;
using LiveCharts.Wpf;
using System.Windows;
using System;
using System.Windows.Media;
using Google.Cloud.Firestore;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Rogers_Toolbox_v3._0
{
    public partial class StatsWindow : Window, INotifyPropertyChanged
    {
        public SeriesCollection SeriesCollection { get; set; }

        public StatsWindow()
        {
            InitializeComponent();
            DataContext = this;
            InitializeCharts();
            UpdateRequiredPerDay(DateTime.Now.Month); // Default to current month
        }

        private int _requiredPerDay;
        public int RequiredPerDay
        {
            get => _requiredPerDay;
            set
            {
                _requiredPerDay = value;
                OnPropertyChanged();
            }
        }

        private int _dailyAverage;
        public int DailyAverage
        {
            get => _dailyAverage;
            set
            {
                _dailyAverage = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void InitializeCharts()
        {
            TotalPieChart.Series = CreateChart(RequiredTotal, ActualTotal);
            XB8Chart.Series = CreateChart(DeviceGoals.XB8Required, DeviceGoals.XB8Actual);
            CGMChart.Series = CreateChart(DeviceGoals.XB7fcRequired, DeviceGoals.XB7fcActual);
            TGChart.Series = CreateChart(DeviceGoals.XB7FCRequired, DeviceGoals.XB7FCActual);
            XI6TChart.Series = CreateChart(DeviceGoals.Xi6tRequired, DeviceGoals.Xi6tActual);
            XI6AChart.Series = CreateChart(DeviceGoals.Xi6ARequired, DeviceGoals.Xi6AActual);
            XIONEChart.Series = CreateChart(DeviceGoals.XiOneRequired, DeviceGoals.XiOneActual);
            PODSChart.Series = CreateChart(DeviceGoals.PodsRequired, DeviceGoals.PodsActual);
        }

        private async void FetchDataByMonth_Click(object sender, RoutedEventArgs e)
        {
            LoadingIndicator.Visibility = Visibility.Visible; // Show loading indicator
            if (monthSelector.SelectedItem is ComboBoxItem selectedItem)
            {
                if (int.TryParse(selectedItem.Tag.ToString(), out int selectedMonth))
                {
                    await FetchDataAndUpdateVariablesAsync(selectedMonth);
                }
                else
                {
                    MessageBox.Show("Invalid month selected.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                MessageBox.Show("Please select a month.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            LoadingIndicator.Visibility = Visibility.Collapsed; // Hide loading indicator
        }

        private async Task FetchDataAndUpdateVariablesAsync(int selectedMonth)
        {
            try
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Keys", "bomwipstore-firebase-adminsdk-jhqev-acb5705838.json");
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", filePath);

                FirestoreDb firestoreDb = FirestoreDb.Create("bomwipstore");

                // Convert selectedMonth to string for document reference
                string monthString = selectedMonth.ToString(); // Convert month number to string

                // Fetch goals for the selected month
                DocumentReference goalsDoc = firestoreDb.Collection("MonthlyGoals").Document(monthString);
                DocumentSnapshot goalsSnapshot = await goalsDoc.GetSnapshotAsync();

                if (goalsSnapshot.Exists)
                {
                    Dictionary<string, object> goalsData = goalsSnapshot.ToDictionary();
                    DeviceGoals.XB8Required = Convert.ToInt32(goalsData["CGM4981COM"]);
                    DeviceGoals.XB7fcRequired = Convert.ToInt32(goalsData["CGM4331COM"]);
                    DeviceGoals.XB7FCRequired = Convert.ToInt32(goalsData["TG4482A"]);
                    DeviceGoals.Xi6tRequired = Convert.ToInt32(goalsData["IPTVTCXI6HD"]);
                    DeviceGoals.Xi6ARequired = Convert.ToInt32(goalsData["IPTVARXI6HD"]);
                    DeviceGoals.XiOneRequired = Convert.ToInt32(goalsData["SCXI11BEI"]);
                    DeviceGoals.PodsRequired = Convert.ToInt32(goalsData["XE2SGROG1"]);
                }
                else
                {
                    DeviceGoals.XB8Required = 1;
                    DeviceGoals.XB7fcRequired = 1;
                    DeviceGoals.XB7FCRequired = 1;
                    DeviceGoals.Xi6tRequired = 1;
                    DeviceGoals.Xi6ARequired = 1;
                    DeviceGoals.XiOneRequired = 1;
                    DeviceGoals.PodsRequired = 1;
                }

                // Get the current year
                int year = DateTime.Now.Year;
                if (selectedMonth == 12)
                {
                    year = 2024;
                }

                // Calculate the start and end dates for the month
                DateTime startDate = new DateTime(year, selectedMonth, 1);
                DateTime endDate = startDate.AddMonths(1).AddDays(-1); // Last day of the month

                // Convert to UTC
                DateTime startDateUtc = startDate.ToUniversalTime();
                DateTime endDateUtc = endDate.ToUniversalTime();

                // Create a query with date range filtering
                Query query = firestoreDb.Collection("bom-wip")
                    .WhereGreaterThanOrEqualTo("Date", Timestamp.FromDateTime(startDateUtc))
                    .WhereLessThanOrEqualTo("Date", Timestamp.FromDateTime(endDateUtc));

                QuerySnapshot snapshot = await query.GetSnapshotAsync();

                Dictionary<string, int> actuals = new Dictionary<string, int>
                {
                    { "CGM4981COM", 0 },
                    { "CGM4331COM", 0 },
                    { "TG4482A", 0 },
                    { "IPTVTCXI6HD", 0 },
                    { "IPTVARXI6HD", 0 },
                    { "SCXI11BEI", 0 },
                    { "XE2SGROG1", 0 }
                };

                foreach (var document in snapshot.Documents)
                {
                    var data = document.ToDictionary();
                    string device = data["Device"]?.ToString();
                    if (int.TryParse(data["Quantity"]?.ToString(), out int quantity) && actuals.ContainsKey(device))
                    {
                        actuals[device] += quantity;
                    }
                }

                DeviceGoals.XB8Actual = actuals["CGM4981COM"];
                DeviceGoals.XB7fcActual = actuals["CGM4331COM"];
                DeviceGoals.XB7FCActual = actuals["TG4482A"];
                DeviceGoals.Xi6tActual = actuals["IPTVTCXI6HD"];
                DeviceGoals.Xi6AActual = actuals["IPTVARXI6HD"];
                DeviceGoals.XiOneActual = actuals["SCXI11BEI"];
                DeviceGoals.PodsActual = actuals["XE2SGROG1"];

                // Update charts after fetching data
                InitializeCharts();
                UpdateRequiredPerDay(selectedMonth);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fetching data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateRequiredPerDay(int selectedMonth)
        {
            int year = DateTime.Now.Year;

            // If the selected month is December and the current month is January, use the previous year
            if (selectedMonth == 12)
            {
                year -= 1; // Set to the previous year
            }

            int totalWeekdays = GetWeekdaysInMonth(year, selectedMonth);
            int weekdaysSoFar;

            // Check if the selected month is the current month
            if (selectedMonth == DateTime.Now.Month && DateTime.Now.Year == DateTime.Now.Year)
            {
                weekdaysSoFar = GetWeekdaysSoFar(DateTime.Now.Year, selectedMonth, DateTime.Now.Day);
            }
            else
            {
                weekdaysSoFar = totalWeekdays; // For past months, consider all weekdays
            }

            int remainingDays = totalWeekdays - weekdaysSoFar;



            // Calculate Daily Average based on actual total and weekdays so far
            if (weekdaysSoFar > 0)
            {
                DailyAverage = ActualTotal / weekdaysSoFar; // Average over weekdays so far
            }
            else
            {
                DailyAverage = 0; // No weekdays so far
            }

            // Calculate Required Per Day
            if (remainingDays > 0)
            {
                RequiredPerDay = (RequiredTotal - ActualTotal) / remainingDays; // Required per day based on remaining days
            }
            else
            {
                RequiredPerDay = 0; // No remaining days
            }

            // Update UI labels if necessary
            RequiredPerDayLabel.Content = $"Daily Required: {RequiredPerDay}";
            DailyAverageLabel.Content = $"Average Daily Completed: {DailyAverage}";
        }

        public SeriesCollection CreateChart(int goal, int completed)
        {
            double overflow = Math.Max(0, completed - goal);
            double actual = Math.Max(0, completed - overflow);
            double required = Math.Max(0, goal - (actual + overflow));

            return new SeriesCollection
            {
                new PieSeries
                {
                    Title = "Completed",
                    Values = new ChartValues<double> { actual },
                    DataLabels = false,
                    LabelPoint = chartPoint => $"{chartPoint.Y} ({chartPoint.Participation:P})",
                    StrokeThickness = 1
                },
                new PieSeries
                {
                    Title = "Unfinished",
                    Values = new ChartValues<double> { required },
                    DataLabels = false,
                    LabelPoint = chartPoint => $"{chartPoint.Y} ({chartPoint.Participation:P})",
                    StrokeThickness = 1
                },
                new PieSeries 
                {
                    Title = "Overflow",
                    Values = new ChartValues<double> { overflow },
                    DataLabels = false,
                    Fill = new SolidColorBrush(Color.FromRgb(0, 102, 204)),
                    LabelPoint = chartPoint => $"{chartPoint.Y} ({chartPoint.Participation:P})",
                    StrokeThickness = 1
                }
            };
        }

        private int RequiredTotal => DeviceGoals.XB8Required + DeviceGoals.XB7fcRequired + DeviceGoals.XB7FCRequired + DeviceGoals.Xi6tRequired + DeviceGoals.Xi6ARequired + DeviceGoals.XiOneRequired + DeviceGoals.PodsRequired;
        private int ActualTotal => DeviceGoals.XB8Actual + DeviceGoals.XB7fcActual + DeviceGoals.XB7FCActual + DeviceGoals.Xi6tActual + DeviceGoals.Xi6AActual + DeviceGoals.XiOneActual + DeviceGoals.PodsActual;
        DateTime Today = DateTime.Now;

        private int DaysRemaining
        {
            get
            {
                int totalWeekdays = GetWeekdaysInMonth(Today.Year, Today.Month);
                int weekdaysSoFar = GetWeekdaysSoFar(Today.Year, Today.Month, Today.Day);
                int remaining = totalWeekdays - weekdaysSoFar;
                return remaining;
            }
        }

        public static class DeviceGoals
        {
            public static int XB8Required { get; set; }
            public static int XB7fcRequired { get; set; }
            public static int XB7FCRequired { get; set; }
            public static int Xi6tRequired { get; set; }
            public static int Xi6ARequired { get; set; }
            public static int XiOneRequired { get; set; }
            public static int PodsRequired { get; set; }

            public static int XB8Actual { get; set; } = 1;
            public static int XB7fcActual { get; set; } = 1;
            public static int XB7FCActual { get; set; } = 1;
            public static int Xi6tActual { get; set; } = 1;
            public static int Xi6AActual { get; set; } = 1;
            public static int XiOneActual { get; set; } = 1;
            public static int PodsActual { get; set; } = 1;
        }

        static int GetWeekdaysInMonth(int year, int month)
        {
            int weekdays = 0;
            int daysInMonth = DateTime.DaysInMonth(year, month);

            for (int day = 1; day <= daysInMonth; day++)
            {
                DateTime currentDate = new DateTime(year, month, day);
                if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    weekdays++;
                }
            }

            return weekdays;
        }

        static int GetWeekdaysSoFar(int year, int month, int currentDay)
        {
            int weekdays = 0;

            for (int day = 1; day <= currentDay; day++)
            {
                DateTime currentDate = new DateTime(year, month, day);
                if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    weekdays++;
                }
            }

            return weekdays;
        }
    }
}