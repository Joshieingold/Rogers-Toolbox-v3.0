using LiveCharts;
using LiveCharts.Wpf;
using System.Windows;
using System;
using System.Windows.Media;

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
        private static readonly int XB7fcActual = 500;
        private static readonly int XB7FCActual = 500;
        private static readonly int Xi6tActual = 2600;
        private static readonly int Xi6AActual = 900;
        private static readonly int XiOneActual = 1300;
        private static readonly int PodsActual = 630;
        public SeriesCollection CreateChart(int goal, int completed)
        {
            // getting the data for the chart.
            double overflow = 0;
            double required = goal;
            double actual = completed;

            // preparing the data for the chart.
            if (required - actual < 0)
            {
                overflow = Math.Abs(required - actual);
            }
            if (overflow > 0)
            {
                actual = completed - overflow;
            }
            required = goal - (actual + overflow);
            if (required < 0)
            {
                required = 0;
            }
            // Creating the chart.
            SeriesCollection = new SeriesCollection
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
                Fill = new SolidColorBrush(Color.FromRgb(0 ,102 ,204)),
                LabelPoint = chartPoint => $"{chartPoint.Y} ({chartPoint.Participation:P})",
                StrokeThickness = 1
            }
        };

            return SeriesCollection;
                } 
        private int RequiredTotal => XB8Required + XB7fcRequired + XB7FCRequired + Xi6tRequired + Xi6ARequired + XiOneRequired + PodsRequired;
        private int ActualTotal => XB8Actual + XB7fcActual + XB7FCActual + Xi6tActual + Xi6AActual + XiOneActual + PodsActual;

        public SeriesCollection SeriesCollection { get; set; }

        public StatsWindow()
        {
            InitializeComponent();

            double totalActual = ActualTotal;
            double totalRequired = RequiredTotal;

            TotalPieChart.Series = CreateChart(RequiredTotal, ActualTotal);
            XB8Chart.Series = CreateChart(XB8Required, XB8Actual);
            CGMChart.Series = CreateChart(XB7fcRequired, XB7fcActual);
            TGChart.Series = CreateChart(XB7FCRequired, XB7FCActual);
            XI6TChart.Series = CreateChart(Xi6tRequired, Xi6tActual);
            XI6AChart.Series = CreateChart(Xi6ARequired, Xi6AActual);
            XIONEChart.Series = CreateChart(XiOneRequired, XiOneActual);
            PODSChart.Series = CreateChart(PodsRequired, PodsActual);

            // Assign the SeriesCollection to the PieChart
          
        }
    }
}
