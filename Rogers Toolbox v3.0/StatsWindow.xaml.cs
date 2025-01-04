using LiveCharts;
using LiveCharts.Wpf;
using System.Windows;
using System;

namespace Rogers_Toolbox_v3._0
{
    public partial class StatsWindow : Window
    {
        // Fields for required and actual data
        private static readonly int XB8Required = 5000;
        private static readonly int XB7fcRequired = 1000;
        private static readonly int XB7FCRequired = 500;
        private static readonly int Xi6tRequired = 500;
        private static readonly int Xi6ARequired = 300;
        private static readonly int XiOneRequired = 2200;
        private static readonly int PodsRequired = 600;

        private static readonly int XB8Actual = 3760;
        private static readonly int XB7fcActual = 960;
        private static readonly int XB7FCActual = 500;
        private static readonly int Xi6tActual = 2600;
        private static readonly int Xi6AActual = 600;
        private static readonly int XiOneActual = 1300;
        private static readonly int PodsActual = 630;

        private int RequiredTotal => XB8Required + XB7fcRequired + XB7FCRequired + Xi6tRequired + Xi6ARequired + XiOneRequired + PodsRequired;
        private int ActualTotal => XB8Actual + XB7fcActual + XB7FCActual + Xi6tActual + Xi6AActual + XiOneActual + PodsActual;

        public SeriesCollection SeriesCollection { get; set; }

        public StatsWindow()
        {
            InitializeComponent();

            double totalActual = ActualTotal;
            double totalRequired = RequiredTotal;
            double overflow = totalActual > totalRequired ? totalActual - totalRequired : 0;
            double completed = Math.Min(totalActual, totalRequired);
            double unfinished = totalRequired - completed;


            // Initialize SeriesCollection
            SeriesCollection = new SeriesCollection
    {
        new PieSeries
        {
            Title = "Completed",
            Values = new ChartValues<double> { completed },
            DataLabels = true,
            LabelPoint = chartPoint => $"{chartPoint.Y} ({chartPoint.Participation:P})"
        },
        new PieSeries
        {
            Title = "Unfinished",
            Values = new ChartValues<double> { unfinished },
            DataLabels = true,
            LabelPoint = chartPoint => $"{chartPoint.Y} ({chartPoint.Participation:P})"
        },
        new PieSeries
        {
            Title = "Overflow",
            Values = new ChartValues<double> { overflow },
            DataLabels = true,
            LabelPoint = chartPoint => $"{chartPoint.Y} ({chartPoint.Participation:P})"
        }
    };

            // Assign the SeriesCollection to the PieChart
            TotalPieChart.Series = SeriesCollection;
        }
    }
}
