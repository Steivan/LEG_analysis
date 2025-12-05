using LEG.MeteoSwiss.Abstractions.Models;
using LEG.PV.Data.Processor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PV.Forecasting.App.Models;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static LEG.PV.Core.Models.PvDataClass;

namespace PV.Forecasting.App.Controllers
{
    public class VisualizationController : Controller
    {
        private static List<PvRecordLists>? _pvRecords;
        private static Dictionary<string, List<string>>? _pvRecordLabels;

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Index(
            List<string> SelectedTimeSeries,
            string SelectedPeriod = "All",
            DateTime? SelectedDate = null,
            string SelectedView = "Weekly",
            bool reset = false)
        {
            if (_pvRecords is null)
            {
                var dataImporter = new DataImporter();
                var (siteId, pvRecords, pvRecordLabels, modelValidRecords, installedKwP, periodsPerHour) = await dataImporter.ImportE3DcHistoryAndCalculated(2, displayPeriod: 2);
                _pvRecords = pvRecords;

                if (pvRecordLabels is not null)
                {
                    _pvRecordLabels = new Dictionary<string, List<string>>
                    {
                        { "Power", pvRecordLabels.PowerLabels },
                        { "Irradiance", pvRecordLabels.RadiationLabels },
                        { "Ambient Temperature", pvRecordLabels.TemperatureLabels },
                        { "Wind Velocity", pvRecordLabels.WindSpeedLabels }
                    };
                }
            }

            if (_pvRecords is null || !_pvRecords.Any())
            {
                ViewBag.ErrorMessage = "No data available to display.";
                return View(new VisualizationViewModel
                {
                    ViewOptions = GetFilteredViewOptions(SelectedPeriod),
                    PeriodOptions = GetPeriodOptions(),
                    SelectedPeriod = SelectedPeriod,
                    SelectedView = SelectedView,
                    SelectedDate = SelectedDate ?? DateTime.Today,
                });
            }

            // 1. Build parameterGroups (already in your code)
            var parameterGroups = MeteoParameterInfo.ParameterToUnit
                .GroupBy(kvp => kvp.Value)
                .ToDictionary(g => g.Key, g => g.Select(kvp => kvp.Key).ToList());
            
            parameterGroups = new Dictionary<string, List<string>> {
                { "Power", new List<string> { "MeasuredPower", "ComputedPower" } }
            }.Concat(parameterGroups).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            var groupVariables = parameterGroups; // Already maps group → variables

            // Build locations per group
            var groupLocations = new Dictionary<string, List<string>>();
            foreach (var group in parameterGroups.Keys)
            {
                if (group == "Power")
                    groupLocations[group] = new List<string> { "PV Site" };
                else
                    groupLocations[group] = DataImporter.selectedStationsIdList; // or filter as needed
            }




            // 2. Set up default checked groups, variables, and locations (step 2)
            var defaultCheckedGroups = new HashSet<string> { "Power", "Radiation", "Temperature", "Wind" };
            var defaultCheckedVariables = new Dictionary<string, HashSet<string>>
{
            { "Temperature", new HashSet<string> { "Temperature" } }, // DewPoint unchecked
            { "Radiation", new HashSet<string> { "GlobalRadiation", "DiffuseRadiation" } }, // others unchecked
            { "Wind", new HashSet<string> { "WindSpeed" } }, // others unchecked
            // Add more as needed
};
            // For locations, you can default to all checked or a subset
            var defaultCheckedLocations = new Dictionary<string, HashSet<string>>();
            // ...build this as needed...

            // 3. Build the ViewModel
            var model = new VisualizationViewModel
            {
                // ...existing assignments...
                GroupChecked = parameterGroups.Keys.ToDictionary(
                    g => g,
                    g => defaultCheckedGroups.Contains(g)
                ),
                GroupVariables = parameterGroups,
                //GroupLocations = /* build from your data */,                          TODO
                GroupLocations = groupLocations,
                CheckedVariables = parameterGroups.Keys.ToDictionary(
                    g => g,
                    g => defaultCheckedVariables.ContainsKey(g) ? defaultCheckedVariables[g] : new HashSet<string>()
                ),
                //CheckedLocations = /* similar logic for locations */,         TODO
                CheckedLocations = groupLocations.Keys.ToDictionary(
                    g => g,
                    g => defaultCheckedLocations.ContainsKey(g) ? defaultCheckedLocations[g] : new HashSet<string>()
                ),
                // ...other assignments...
            };

            // Handle reset: restore default selection
            List<SelectListItem> viewOptions;

            if (reset)
            {
                SelectedPeriod = "All";
                SelectedDate = DateTime.Today;
                viewOptions = GetFilteredViewOptions(SelectedPeriod);
                SelectedView = "Weekly"; //viewOptions.FirstOrDefault()?.Value ?? "Weekly";
                SelectedTimeSeries = _pvRecordLabels?.SelectMany(g => g.Value).ToList() ?? new List<string>();
            }
            else if (SelectedTimeSeries is null || !SelectedTimeSeries.Any())
            {
                SelectedTimeSeries = _pvRecordLabels?.SelectMany(g => g.Value).ToList() ?? new List<string>();
                viewOptions = GetFilteredViewOptions(SelectedPeriod);
            }
            else
            {
                viewOptions = GetFilteredViewOptions(SelectedPeriod);
            }

            if (!viewOptions.Any(v => v.Value == SelectedView))
            {
                SelectedView = viewOptions.First().Value!;
            }

            var minDate = _pvRecords[0].Timestamp.Date;
            var maxDate = _pvRecords[^1].Timestamp.Date;
            var nowDate = DateTime.Today.Date;
            var currentDate = SelectedDate ?? nowDate;

            var (startDate, endDate) = GetDateRange(currentDate, SelectedPeriod, minDate, maxDate);
            var recordsForPeriod = _pvRecords.Where(r => r.Timestamp >= startDate && r.Timestamp < endDate).ToList();

            var plotHtmls = CreateSubplots(recordsForPeriod, SelectedTimeSeries, SelectedView, startDate, endDate);

            var labelsByGroup = _pvRecordLabels ?? new Dictionary<string, List<string>>();

            model = new VisualizationViewModel
            {
                PlotHtmls = plotHtmls,
                TimeSeriesLabelsByGroup = labelsByGroup,
                SelectedTimeSeries = SelectedTimeSeries,
                ViewOptions = viewOptions,
                PeriodOptions = GetPeriodOptions(),
                SelectedPeriod = SelectedPeriod,
                SelectedView = SelectedView,
                SelectedDate = SelectedDate ?? DateTime.Today,
                MinYear = minDate.Year,
                MaxYear = maxDate.Year,    // ... existing properties ...
                ParameterGroupsByUnit = parameterGroups,
                //SelectedParameters = SelectedParameters // You may need to add logic to set this list
                                                        // ... other properties ...
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

        private Dictionary<string, (string HtmlWithLegend, string HtmlWithoutLegend)> CreateSubplots(List<PvRecordLists> records, List<string> selectedTimeSeries, string viewName, DateTime startDate, DateTime endDate)
        {
            var plotHtmls = new Dictionary<string, (string HtmlWithLegend, string HtmlWithoutLegend)>();
            if (records is null || !records.Any() || _pvRecordLabels is null) return plotHtmls;

            var activePlotGroups = _pvRecordLabels
                .Where(g => g.Value.Any(ts => selectedTimeSeries.Contains(ts)))
                .ToDictionary(g => g.Key, g => g.Value);

            if (!activePlotGroups.Any()) return plotHtmls;

            var plots = new List<Plot>();
            var plotGroupNames = activePlotGroups.Keys.ToList();

            // Pass 1: Create and configure all plots
            for (int i = 0; i < activePlotGroups.Count; i++)
            {
                var groupName = plotGroupNames[i];
                var timeSeriesInGroup = activePlotGroups[groupName];
                var isLastPlot = i == activePlotGroups.Count - 1;

                var plt = new Plot();
                plots.Add(plt);

                var title = plt.Add.Text(groupName, 0.05, 0.95);
                title.Alignment = Alignment.UpperLeft;
                title.LabelFontSize = 16;
                title.LabelBold = true;

                if (isLastPlot)
                {
                    plt.XLabel("Date");
                }
                plt.YLabel(groupName);

                for (int j = 0; j < timeSeriesInGroup.Count; j++)
                {
                    var timeSeriesName = timeSeriesInGroup[j];
                    if (!selectedTimeSeries.Contains(timeSeriesName)) continue;

                    bool isSum = groupName is "Power" or "Irradiance";
                    Func<IEnumerable<double?>, double?> aggregationFunc = isSum ? Enumerable.Sum : Enumerable.Average;
                    var data = AggregateData(records, viewName, r => GetValueFromRecord(r, groupName, j), aggregationFunc);
                    var plotColor = GetColorForTimeSeries(timeSeriesName, groupName, j);

                    var dates = data.Select(d => d.Timestamp.ToOADate()).ToArray();
                    var values = data.Select(d => d.Value).ToArray();
                    var scatter = plt.Add.Scatter(dates, values);
                    scatter.Color = plotColor;
                    scatter.LegendText = timeSeriesName;
                }

                plt.Axes.SetLimits(startDate.ToOADate(), endDate.ToOADate());
                plt.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.DateTimeAutomatic();

                if (!isLastPlot)
                {
                    plt.Axes.Bottom.FrameLineStyle.IsVisible = false;
                }
            }

            // Pass 2: Synchronize axis limits and render
            const int imageWidth = 2000;
            const int imageHeight = 250;
            float maxLeftAxisWidth = 0;
            foreach (var plt in plots)
            {
                var image = plt.GetImage(imageWidth, imageHeight); // Render to a dummy image to calculate layout
                maxLeftAxisWidth = Math.Max(maxLeftAxisWidth, plt.LastRender.DataRect.Left);
            }

            // Apply the largest size to all plots and render
            for (int i = 0; i < plots.Count; i++)
            {
                var plt = plots[i];
                var groupName = plotGroupNames[i];
                plt.Axes.Left.MinimumSize = maxLeftAxisWidth;

                // Render with legend
                plt.Legend.IsVisible = true;
                var htmlWithLegend = plt.GetPngHtml(imageWidth, imageHeight);

                // Render without legend
                plt.Legend.IsVisible = false;
                var htmlWithoutLegend = plt.GetPngHtml(imageWidth, imageHeight);

                plotHtmls[groupName] = (htmlWithLegend, htmlWithoutLegend);
            }

            return plotHtmls;
        }

        private Color GetColorForTimeSeries(string timeSeriesName, string groupName, int seriesIndex)
        {
            // Power group has fixed, named colors
            if (groupName == "Power")
            {
                return timeSeriesName switch
                {
                    "MeasuredPower" => Colors.Red,
                    "ComputedPower" => Colors.Purple,
                    _ => Colors.Black
                };
            }

            // Other groups are colored by index (for different weather stations)
            var palette = new ScottPlot.Palettes.Category10();
            return palette.GetColor(seriesIndex);
        }

        #region Helper Methods
        private List<DataPointViewModel> AggregateData(List<PvRecordLists> records, string view, Func<PvRecordLists, double?> valueSelector, Func<IEnumerable<double?>, double?> aggregationFunc)
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

        private double? GetValueFromRecord(PvRecordLists record, string groupName, int index)
        {
            var list = groupName switch
            {
                "Power" => record.Power,
                "Irradiance" => record.Radiation,
                "Ambient Temperature" => record.Temperature,
                "Wind Velocity" => record.WindSpeed,
                _ => null
            };

            if (list is null || index < 0 || index >= list.Count)
            {
                return null;
            }
            return list[index];
        }

        private List<SelectListItem> GetViewOptions() =>
        [
            new("15-min", "15-min"),
            new("Hourly", "Hourly"),
            new("3-hourly", "3-hourly"),
            new("Daily", "Daily"),
            new("Weekly", "Weekly"),
            new("Monthly", "Monthly"),
            new("Yearly", "Year")
        ];

        private List<SelectListItem> GetFilteredViewOptions(string period)
        {
            var allOptions = GetViewOptions();
            return period switch
            {
                "Day" => allOptions.Where(o => o.Value == "15-min" || o.Value == "Hourly" || o.Value == "3-hourly").ToList(),
                "Week" => allOptions.Where(o => o.Value != "Weekly" && o.Value != "Monthly" && o.Value != "Yearly").ToList(),
                "Month" => allOptions.Where(o => o.Value != "15-min" && o.Value != "Monthly" && o.Value != "Yearly").ToList(),
                "Year" => allOptions.Where(o => o.Value != "15-min" && o.Value != "Hourly" && o.Value != "Yearly").ToList(),
                "All" => allOptions.Where(o => o.Value != "15-min" && o.Value != "Hourly" && o.Value != "3-hourly").ToList(),
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
