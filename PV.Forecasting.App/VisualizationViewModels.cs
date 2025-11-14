//using LEG.PV.Data.Processor;
//using PV.Forecasting.App.Models;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
//using System.Web.Mvc;
//using static LEG.PV.Data.Processor.DataRecords;
using LEG.PV.Data.Processor;
using static LEG.PV.Core.Models.PvJacobian;
using static LEG.PV.Core.Models.PvPriorConfig;
using static LEG.PV.Data.Processor.DataRecords;

namespace PV.Forecasting.App.Controllers
{
    public class VisualizationController : Controller
    {
        private static List<PvRecord> _pvRecords;

        public async Task<ActionResult> Index(string selectedTimeSeries = "MeasuredPower", string selectedView = "Daily")
        {
            if (_pvRecords == null)
            {
                var dataImporter = new DataImporter();
                var (records, _, _) = await dataImporter.ImportE3DcData(1); // Default to "Senn"
                _pvRecords = records;
            }

            var plt = CreatePlot(selectedTimeSeries, selectedView);

            var model = new VisualizationViewModel
            {
                // Generate the script and div needed to render the plot
                PlotScript = plt.GetJs(),
                PlotDiv = $"<div id='{plt.GetId()}' style='width: 100%; height: 500px;'></div>",
                SelectedTimeSeries = selectedTimeSeries,
                SelectedView = selectedView,
                TimeSeriesOptions = GetTimeSeriesOptions(),
                ViewOptions = GetViewOptions()
            };

            return View(model);
        }

        private Plot CreatePlot(string timeSeriesName, string viewName)
        {
            var plt = new Plot();
            plt.Title($"{timeSeriesName} - {viewName} View");
            plt.XLabel("Date");
            plt.YLabel(timeSeriesName);

            // Default to the first year of data for simplicity
            var records = _pvRecords.Where(r => r.Timestamp.Year == _pvRecords.Min(t => t.Timestamp.Year)).ToList();

            var (aggregationFunc, isBar) = GetAggregationAndSeriesType(timeSeriesName, viewName);
            var data = AggregateData(records, viewName, r => GetPropertyValue(r, timeSeriesName), aggregationFunc);

            if (isBar)
            {
                var positions = data.Select((_, i) => (double)i).ToArray();
                var values = data.Select(d => d.Value).ToArray();
                plt.Add.Bars(positions, values);

                // Use numeric ticks and format them as dates
                var ticks = positions.Select((pos, i) => new Tick(pos, data[i].Timestamp.ToString("yyyy-MM-dd"))).ToArray();
                plt.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(ticks);
                plt.Axes.Bottom.MajorTickStyle.Rotation = 45;
            }
            else
            {
                var dates = data.Select(d => d.Timestamp.ToOADate()).ToArray();
                var values = data.Select(d => d.Value).ToArray();
                plt.Add.Scatter(dates, values);
                plt.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.DateTimeAutomatic();
            }

            plt.Axes.Bottom.MajorTickStyle.Length = 10;
            plt.Axes.Bottom.MinorTickStyle.Length = 5;

            return plt;
        }

        // The AggregateData, GetAggregationAndSeriesType, GetPropertyValue, etc. methods remain the same...
        #region Helper Methods
        private List<DataPointViewModel> AggregateData(List<PvRecord> records, string view, Func<PvRecord, double> valueSelector, Func<IEnumerable<double>, double> aggregationFunc)
        {
            switch (view)
            {
                case "Weekly":
                    return records.GroupBy(r => new { r.Timestamp.Year, Week = GetIso8601WeekOfYear(r.Timestamp), r.Timestamp.Hour })
                                  .Select(g => new DataPointViewModel { Timestamp = g.First().Timestamp, Value = aggregationFunc(g.Select(valueSelector)) })
                                  .OrderBy(d => d.Timestamp)
                                  .ToList();
                case "Monthly":
                    return records.GroupBy(r => r.Timestamp.Date)
                                  .Select(g => new DataPointViewModel { Timestamp = g.Key, Value = aggregationFunc(g.Select(valueSelector)) })
                                  .OrderBy(d => d.Timestamp)
                                  .ToList();
                case "Annual":
                    return records.GroupBy(r => new { r.Timestamp.Year, r.Timestamp.Month })
                                 .Select(g => new DataPointViewModel { Timestamp = new DateTime(g.Key.Year, g.Key.Month, 1), Value = aggregationFunc(g.Select(valueSelector)) })
                                 .OrderBy(d => d.Timestamp)
                                 .ToList();
                case "Overall":
                    return records.GroupBy(r => r.Timestamp.Year)
                                 .Select(g => new DataPointViewModel { Timestamp = new DateTime(g.Key, 1, 1), Value = aggregationFunc(g.Select(valueSelector)) })
                                 .OrderBy(d => d.Timestamp)
                                 .ToList();
                case "Daily":
                default:
                    return records.Select(r => new DataPointViewModel { Timestamp = r.Timestamp, Value = valueSelector(r) }).ToList();
            }
        }

        private (Func<IEnumerable<double>, double> AggregationFunc, bool IsBar) GetAggregationAndSeriesType(string timeSeriesName, string viewName)
        {
            bool isSum = timeSeriesName == "MeasuredPower" || timeSeriesName == "Irradiation";
            Func<IEnumerable<double>, double> aggregationFunc = isSum ? (Func<IEnumerable<double>, double>)Enumerable.Sum : Enumerable.Average;

            bool isBar = (viewName == "Monthly" || viewName == "Annual" || viewName == "Overall");

            return (aggregationFunc, isBar);
        }

        private double GetPropertyValue(PvRecord record, string propertyName)
        {
            var prop = typeof(PvRecord).GetProperty(propertyName);
            return Convert.ToDouble(prop.GetValue(record));
        }

        private List<SelectListItem> GetTimeSeriesOptions() => new List<SelectListItem>
        {
            new SelectListItem { Text = "Measured Power", Value = "MeasuredPower" },
            new SelectListItem { Text = "Irradiation", Value = "Irradiation" },
            new SelectListItem { Text = "Ambient Temperature", Value = "AmbientTemp" },
            new SelectListItem { Text = "Wind Velocity", Value = "WindVelocity" }
        };

        private List<SelectListItem> GetViewOptions() => new List<SelectListItem>
        {
            new SelectListItem { Text = "Daily", Value = "Daily" },
            new SelectListItem { Text = "Weekly", Value = "Weekly" },
            new SelectListItem { Text = "Monthly", Value = "Monthly" },
            new SelectListItem { Text = "Annual", Value = "Annual" },
            new SelectListItem { Text = "Overall", Value = "Overall" }
        };

        private static int GetIso8601WeekOfYear(DateTime time)
        {
            DayOfWeek day = System.Globalization.CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                time = time.AddDays(3);
            }
            return System.Globalization.CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }
        #endregion
    }

    public class DataPointViewModel
    {
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
    }
}