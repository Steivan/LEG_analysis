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
        public async Task<IActionResult> Index(List<string> SelectedTimeSeries, string SelectedView = "15-min", int? SelectedYear = null)
        {
            if (_pvRecords is null)
            {
                var dataImporter = new DataImporter();
                var (siteId, pvRecords, modelValidRecords, installedKwP, periodsPerHour) = await dataImporter.ImportE3DcData(1, meteoDataLag: 0);
                _pvRecords = pvRecords;
            }

            if (SelectedTimeSeries is null || !SelectedTimeSeries.Any())
            {
                SelectedTimeSeries = GetTimeSeriesOptions().Select(o => o.Value).ToList()!;
            }

            var minYear = _pvRecords.Min(r => r.Timestamp.Year);
            var maxYear = _pvRecords.Max(r => r.Timestamp.Year);
            var yearOptions = Enumerable.Range(minYear, maxYear - minYear + 1)
                                        .Select(y => new SelectListItem(y.ToString(), y.ToString()))
                                        .ToList();

            var currentYear = SelectedYear ?? minYear;
            var recordsForYear = _pvRecords.Where(r => r.Timestamp.Year == currentYear).ToList();

            var plotHtmls = CreateSubplots(recordsForYear, SelectedTimeSeries, SelectedView);

            var model = new VisualizationViewModel
            {
                PlotHtmls = plotHtmls,
                SelectedTimeSeries = SelectedTimeSeries,
                SelectedView = SelectedView,
                TimeSeriesOptions = GetTimeSeriesOptions(),
                ViewOptions = GetViewOptions(),
                YearOptions = yearOptions,
                SelectedYear = currentYear,
                MinYear = minYear,
                MaxYear = maxYear
            };

            return View(model);
        }

        private Dictionary<string, string> CreateSubplots(List<PvRecord> records, List<string> timeSeriesNames, string viewName)
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
                title.FontSize = 16;
                title.Bold = true;

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

                if (viewName != "Yearly" && records.Any())
                {
                    var year = records.First().Timestamp.Year;
                    var startDate = new DateTime(year, 1, 1);
                    var endDate = new DateTime(year, 12, 31);
                    plt.Axes.SetLimits(startDate.ToOADate(), endDate.ToOADate());

                    // Add vertical lines for each month
                    for (int month = 2; month <= 12; month++)
                    {
                        var monthStart = new DateTime(year, month, 1);
                        var line = plt.Add.VerticalLine(monthStart.ToOADate());
                        line.Color = Colors.Black.WithAlpha(0.1f);
                    }

                    // Create manual ticks for the 15th of each month
                    var monthlyTicks = new List<Tick>();
                    for (int month = 1; month <= 12; month++)
                    {
                        var tickPosition = new DateTime(year, month, 15).ToOADate();
                        var tickLabel = new DateTime(year, month, 1).ToString("MMM");
                        monthlyTicks.Add(new Tick(tickPosition, tickLabel));
                    }
                    plt.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(monthlyTicks.ToArray());
                    plt.Axes.Bottom.TickLabelStyle.Rotation = 0;
                    plt.Axes.Bottom.MajorTickStyle.Length = 0; // Hide tick marks
                }
                else
                {
                    plt.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.DateTimeAutomatic();
                }

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
                plt.GetImage(1, 1); // Render to a dummy image to calculate layout
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
                    return records.GroupBy(r => System.Globalization.ISOWeek.GetWeekOfYear(r.Timestamp))
                                  .Select(g => new DataPointViewModel { Timestamp = g.First().Timestamp, Value = aggregationFunc(g.Select(valueSelector)) })
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
        #endregion
    }
}