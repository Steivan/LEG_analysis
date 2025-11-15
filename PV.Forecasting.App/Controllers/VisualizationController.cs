using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PV.Forecasting.App.Models;
using LEG.PV.Data.Processor;
using static LEG.PV.Data.Processor.DataRecords;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PV.Forecasting.App.Controllers
{
    public class VisualizationController : Controller
    {
        private static List<PvRecord>? _pvRecords;

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Index(List<string> SelectedTimeSeries, string SelectedPeriod = "Year", DateTime? SelectedDate = null, string SelectedView = "15-min")
        {
            if (_pvRecords is null)
            {
                var dataImporter = new DataImporter();
                var (siteId, pvRecords, modelValidRecords, installedKwP, periodsPerHour) = await dataImporter.ImportE3DcData(1);
                _pvRecords = pvRecords;
            }

            if (SelectedTimeSeries is null || !SelectedTimeSeries.Any())
            {
                SelectedTimeSeries = GetTimeSeriesOptions().Select(o => o.Value).ToList()!;
            }

            var minDate = _pvRecords.Min(r => r.Timestamp.Date);
            var maxDate = _pvRecords.Max(r => r.Timestamp.Date);
            var currentDate = SelectedDate ?? minDate;

            var (startDate, endDate) = GetDateRange(currentDate, SelectedPeriod, minDate, maxDate);
            var recordsForPeriod = _pvRecords.Where(r => r.Timestamp >= startDate && r.Timestamp < endDate).ToList();

            var viewOptions = GetFilteredViewOptions(SelectedPeriod);
            if (!viewOptions.Any(v => v.Value == SelectedView))
            {
                SelectedView = viewOptions.First().Value!;
            }

            var plotHtmls = CreateSubplots(recordsForPeriod, SelectedTimeSeries, SelectedView, startDate, endDate);

            var model = new VisualizationViewModel
            {
                PlotHtmls = plotHtmls,
                SelectedTimeSeries = SelectedTimeSeries,
                SelectedView = SelectedView,
                TimeSeriesOptions = GetTimeSeriesOptions(),
                ViewOptions = viewOptions,
                SelectedPeriod = SelectedPeriod,
                PeriodOptions = GetPeriodOptions(),
                SelectedDate = currentDate,
                MinYear = minDate.Year,
                MaxYear = maxDate.Year
            };

            return View(model);
        }

        private (DateTime, DateTime) GetDateRange(DateTime date, string period, DateTime minDate, DateTime maxDate)
        {
            return period switch
            {
                "Day" => (date.Date, date.Date.AddDays(1)),
                "Week" => (GetStartOfWeek(date), GetStartOfWeek(date).AddDays(7)),
                "Month" => (new DateTime(date.Year, date.Month, 1), new DateTime(date.Year, date.Month, 1).AddMonths(1)),
                "Year" => (new DateTime(date.Year, 1, 1), new DateTime(date.Year, 1, 1).AddYears(1)),
                "All" => (minDate, maxDate.AddDays(1)),
                _ => (new DateTime(date.Year, 1, 1), new DateTime(date.Year, 1, 1).AddYears(1))
            };
        }

        private DateTime GetStartOfWeek(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }

        private Dictionary<string, string> CreateSubplots(List<PvRecord> records, List<string> timeSeriesNames, string viewName, DateTime startDate, DateTime endDate)
        {
            var plotHtmls = new Dictionary<string, string>();
            if (records is null || !records.Any()) return plotHtmls;

            var plots = new List<Plot>();
            var plotNames = new List<string>();

            // Pass 1: Create and configure all plots
            for (int i = 0; i < timeSeriesNames.Count; i++)
            {
                var timeSeriesName = timeSeriesNames[i];
                var isLastPlot = i == timeSeriesNames.Count - 1;

                var plt = new Plot();
                plots.Add(plt);
                plotNames.Add(timeSeriesName);

                var title = plt.Add.Text(timeSeriesName, 0.05, 0.95);
                title.Alignment = Alignment.UpperLeft;
                title.LabelFontSize = 16;
                title.LabelBold = true;

                if (isLastPlot)
                {
                    plt.XLabel("Date");
                }
                plt.YLabel(timeSeriesName);

                bool isSum = timeSeriesName == "MeasuredPower" || timeSeriesName == "Irradiation";
                Func<IEnumerable<double>, double> aggregationFunc = isSum ? Enumerable.Sum : Enumerable.Average;
                var data = AggregateData(records, viewName, r => GetPropertyValue(r, timeSeriesName), aggregationFunc);
                var plotColor = GetColorForTimeSeries(timeSeriesName);

                var dates = data.Select(d => d.Timestamp.ToOADate()).ToArray();
                var values = data.Select(d => d.Value).ToArray();
                var scatter = plt.Add.Scatter(dates, values);
                scatter.Color = plotColor;

                plt.Axes.SetLimits(startDate.ToOADate(), endDate.ToOADate());
                plt.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.DateTimeAutomatic();

                if (!isLastPlot)
                {
                    plt.Axes.Bottom.FrameLineStyle.IsVisible = false;
                }
            }

            // Pass 2: Synchronize axis limits and render
            // Measure the largest left axis panel by performing a dry-run render
            float maxLeftAxisWidth = 0;
            foreach (var plt in plots)
            {
                var image = plt.GetImage(800, 250); // Render to a dummy image to calculate layout
                maxLeftAxisWidth = Math.Max(maxLeftAxisWidth, plt.LastRender.DataRect.Left);
            }

            // Apply the largest size to all plots and render
            for (int i = 0; i < plots.Count; i++)
            {
                var plt = plots[i];
                var timeSeriesName = plotNames[i];
                plt.Axes.Left.MinimumSize = maxLeftAxisWidth;
                plotHtmls[timeSeriesName] = plt.GetPngHtml(800, 250);
            }

            return plotHtmls;
        }

        private Color GetColorForTimeSeries(string timeSeriesName)
        {
            return timeSeriesName switch
            {
                "MeasuredPower" => Colors.Red,
                "Irradiation" => Colors.Orange,
                "AmbientTemp" => Colors.Blue,
                "WindVelocity" => Colors.Green,
                _ => Colors.Black
            };
        }

        #region Helper Methods
        private List<DataPointViewModel> AggregateData(List<PvRecord> records, string view, Func<PvRecord, double> valueSelector, Func<IEnumerable<double>, double> aggregationFunc)
        {
            switch (view)
            {
                case "Hourly":
                    return records.GroupBy(r => new { r.Timestamp.Year, r.Timestamp.Month, r.Timestamp.Day, r.Timestamp.Hour })
                                  .Select(g => new DataPointViewModel { Timestamp = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day, g.Key.Hour, 0, 0), Value = aggregationFunc(g.Select(valueSelector)) })
                                  .OrderBy(d => d.Timestamp)
                                  .ToList();
                case "3-hourly":
                    return records.GroupBy(r => new { r.Timestamp.Date, HourBlock = r.Timestamp.Hour / 3 })
                                  .Select(g => new DataPointViewModel {
                                      Timestamp = g.Key.Date.AddHours(g.Key.HourBlock * 3),
                                      Value = aggregationFunc(g.Select(valueSelector))
                                  })
                                  .OrderBy(d => d.Timestamp)
                                  .ToList();
                case "Daily":
                    return records.GroupBy(r => r.Timestamp.Date)
                                  .Select(g => new DataPointViewModel { Timestamp = g.Key, Value = aggregationFunc(g.Select(valueSelector)) })
                                  .OrderBy(d => d.Timestamp)
                                  .ToList();
                case "Weekly":
                    return records.GroupBy(r => new { Year = System.Globalization.ISOWeek.GetYear(r.Timestamp), Week = System.Globalization.ISOWeek.GetWeekOfYear(r.Timestamp) })
                                  .Select(g => new DataPointViewModel { Timestamp = System.Globalization.ISOWeek.ToDateTime(g.Key.Year, g.Key.Week, DayOfWeek.Monday), Value = aggregationFunc(g.Select(valueSelector)) })
                                  .OrderBy(d => d.Timestamp)
                                  .ToList();
                case "Monthly":
                    return records.GroupBy(r => new { r.Timestamp.Year, r.Timestamp.Month })
                                  .Select(g => new DataPointViewModel { Timestamp = new DateTime(g.Key.Year, g.Key.Month, 1), Value = aggregationFunc(g.Select(valueSelector)) })
                                  .OrderBy(d => d.Timestamp)
                                  .ToList();
                case "Yearly":
                    return records.GroupBy(r => r.Timestamp.Year)
                                 .Select(g => new DataPointViewModel { Timestamp = new DateTime(g.Key, 1, 1), Value = aggregationFunc(g.Select(valueSelector)) })
                                 .OrderBy(d => d.Timestamp)
                                  .ToList();
                case "15-min":
                default:
                    return records.Select(r => new DataPointViewModel { Timestamp = r.Timestamp, Value = valueSelector(r) }).ToList();
            }
        }

        private double GetPropertyValue(PvRecord record, string propertyName)
        {
            var prop = typeof(PvRecord).GetProperty(propertyName);
            return Convert.ToDouble(prop!.GetValue(record));
        }

        private List<SelectListItem> GetTimeSeriesOptions() =>
        [
            new("Measured Power", "MeasuredPower"),
            new("Irradiation", "Irradiation"),
            new("Ambient Temperature", "AmbientTemp"),
            new("Wind Velocity", "WindVelocity")
        ];

        private List<SelectListItem> GetViewOptions() =>
        [
            new("15-min", "15-min"),
            new("Hourly", "Hourly"),
            new("3-hourly", "3-hourly"),
            new("Daily", "Daily"),
            new("Weekly", "Weekly"),
            new("Monthly", "Monthly"),
            new("Yearly", "Yearly")
        ];

        private List<SelectListItem> GetFilteredViewOptions(string period)
        {
            var allOptions = GetViewOptions();
            return period switch
            {
                "Day" => allOptions.Where(o => o.Value == "15-min" || o.Value == "Hourly" || o.Value == "3-hourly").ToList(),
                "Week" => allOptions.Where(o => o.Value != "Weekly" && o.Value != "Monthly" && o.Value != "Yearly").ToList(),
                "Month" => allOptions.Where(o => o.Value != "Monthly" && o.Value != "Yearly").ToList(),
                "Year" => allOptions.Where(o => o.Value != "Yearly").ToList(),
                "All" => allOptions,
                _ => allOptions
            };
        }

        private List<SelectListItem> GetPeriodOptions() =>
        [
            new("Day", "Day"),
            new("Week", "Week"),
            new("Month", "Month"),
            new("Year", "Year"),
            new("All", "All")
        ];
        #endregion
    }
}