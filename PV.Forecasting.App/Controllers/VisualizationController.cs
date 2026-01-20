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
using static LEG.PV.Core.Models.Structures.PvConstants;
using static LEG.MeteoSwiss.Abstractions.Models.MeteoConstants;
using LEG.CoreLib.SampleData.SampleData;

namespace PV.Forecasting.App.Controllers
{
    public class VisualizationController : Controller
    {
        string[] siteIds = { SiteNamesList.SyntheticSite, SiteNamesList.Senn, SiteNamesList.SennV, SiteNamesList.Studenrain };

        // *****************************************************************************************************************

        const int siteIdIndex = 2;           // <<<<< ==== Choose site 0 -3 here

        const int displayPeriod = 2;        // 0: downloaded history, 1: meteo history till now, 2: including meteo forecast

        // ******************************************************************************************************************

        public static List<string> SelectedStationsIdList = new List<string>();

        const string ConsumptionGroup = "Consumption";
        const string ProductionGroup = "Production";
        const string ResidualsGroup = "Residuals";
        const string RadiationGroup = "Radiation";
        const string TemperatureGroup = "Temperature";
        const string WindspeedGroup = "Wind Speed";
        const string SnowDepthGroup = "Snow Depth";
        const string HumidityGroup = "Relative Humidity";

        const string PeriodDay = "Day";
        const string PeriodWeek = "Week";
        const string PeriodMonth = "Month";
        const string PeriodYear = "Year";
        const string PeriodAll = "All";

        const string PeriodDayName = "Day";
        const string PeriodWeekName = "Week";
        const string PeriodMonthName = "Month";
        const string PeriodYearName = "Year";
        const string PeriodAllName = "All";

        const string Interval15Min = "Data";
        const string IntervalHourly = "Hourly";
        const string Interval3Hourly = "3-hourly";
        const string IntervalDaily = "Daily";
        const string IntervalWeekly = "Weekly";
        const string IntervalMonthly = "Monthly";
        const string IntervalYearly = "Yearly";

        const string Interval15MinName = "Data";
        const string IntervalHourlyName = "Hourly";
        const string Interval3HourlyName = "3-hourly";
        const string IntervalDailyName = "Daily";
        const string IntervalWeeklyName = "Weekly";
        const string IntervalMonthlyName = "Monthly";
        const string IntervalYearlyName = "Yearly";

        private static List<PvRecordLists>? _pvRecords;
        private static Dictionary<string, List<string>>? _pvRecordLabels;

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Index(
            List<string> SelectedTimeSeries,
            List<string> SelectedGroups,
            string SelectedPeriod = PeriodAll,
            DateTime? SelectedDate = null,
            string SelectedView = IntervalWeeklyName,
            bool reset = false,
            string groupChanged = null)
        {
            if (_pvRecords is null)
            {
                var dataImporter = new DataImporter();
                var (pvRecords, pvRecordLabels, modelValidRecords, installedKwP, periodsPerHour) =
                    await dataImporter.ImportHistoryAndCalculated(siteIds[siteIdIndex], displayPeriod: displayPeriod);
                _pvRecords = pvRecords;

                if (pvRecordLabels is not null)
                {
                    _pvRecordLabels = new Dictionary<string, List<string>>
                    {
                        { ConsumptionGroup, pvRecordLabels.ConsumptionLabels ?? new List<string>() },
                        { ProductionGroup, pvRecordLabels.ProductionLabels ?? new List<string>() },
                        { ResidualsGroup, pvRecordLabels.ResidualsLabels ?? new List<string>() },
                        { RadiationGroup, pvRecordLabels.RadiationLabels ?? new List<string>() },
                        { TemperatureGroup, pvRecordLabels.TemperatureLabels ?? new List<string>() },
                        { WindspeedGroup, pvRecordLabels.WindSpeedLabels ?? new List<string>() },
                        { SnowDepthGroup, pvRecordLabels.SnowDepthLabels ?? new List<string>() },
                        { HumidityGroup, pvRecordLabels.RelativeHumidityLabels ?? new List<string>() }
                    };
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
            }

            // 1. Build parameterGroups (already in your code)
            var parameterGroups = new Dictionary<string, List<string>>
            {
                { ConsumptionGroup, new List<string> { MeasuredPower, ConsumedPower, WallBox, Battery, Grid, Residual } },
                { ProductionGroup, new List<string> { MeasuredPower, PowerGR, PowerGRTW, PowerGRTWSF } },
                { ResidualsGroup, new List<string> { Reference, UflGR, UflGRTW, UflGRTWSF } },
                { RadiationGroup, new List<string> { GlobalRadiation, DiffuseRadiation } },
                { TemperatureGroup, new List<string> { Temperature, DewPoint } },
                { WindspeedGroup, new List<string> { WindSpeed } },
                { SnowDepthGroup, new List<string> { SnowDepth } },
                { HumidityGroup, new List<string> { RelativeHumidity } }
            };

            // Build locations per group
            var groupLocations = new Dictionary<string, List<string>>();
            foreach (var group in parameterGroups.Keys)
            {
                if (group == ConsumptionGroup || group == ProductionGroup || group == ResidualsGroup)
                    groupLocations[group] = new List<string> { "PV Site" };
                else
                    groupLocations[group] = SelectedStationsIdList; // or filter as needed
            }

            // 2. Set up default checked groups, variables, and locations (step 2)
            var defaultCheckedGroups = new HashSet<string>
            { ConsumptionGroup, ProductionGroup, ResidualsGroup, RadiationGroup, TemperatureGroup, WindspeedGroup, SnowDepthGroup, HumidityGroup };

            var defaultCheckedVariables = new Dictionary<string, HashSet<string>>
            {
                { RadiationGroup, new HashSet<string> { GlobalRadiation, DiffuseRadiation } }, // others unchecked
                { TemperatureGroup, new HashSet<string> { Temperature, DewPoint } }, // others unchecked
                { WindspeedGroup, new HashSet<string> { WindSpeed } }, // others unchecked
                { SnowDepthGroup, new HashSet<string> { SnowDepth } }, // others unchecked
                { HumidityGroup, new HashSet<string> { RelativeHumidity } }, // others unchecked
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
                GroupLocations = groupLocations,
                CheckedVariables = parameterGroups.Keys.ToDictionary(
                    g => g,
                    g => defaultCheckedVariables.ContainsKey(g) ? defaultCheckedVariables[g] : new HashSet<string>()
                ),
                CheckedLocations = groupLocations.Keys.ToDictionary(
                    g => g,
                    g => defaultCheckedLocations.ContainsKey(g) ? defaultCheckedLocations[g] : new HashSet<string>()
                ),
            };

            // Handle reset: restore default selection
            List<SelectListItem> viewOptions;

            if (reset)
            {
                SelectedPeriod = PeriodAll;
                SelectedDate = DateTime.Today;
                viewOptions = GetFilteredViewOptions(SelectedPeriod);
                SelectedView = IntervalWeekly;
                SelectedTimeSeries = _pvRecordLabels?.SelectMany(g => g.Value).ToList() ?? new List<string>();
            }
            else if (SelectedTimeSeries is null || !SelectedTimeSeries.Any())
            {
                //SelectedTimeSeries = _pvRecordLabels?.SelectMany(g => g.Value).ToList() ?? new List<string>();
                SelectedTimeSeries = _pvRecordLabels?
                    .Where(g => g.Value != null)
                    .SelectMany(g => g.Value)
                    .ToList() ?? new List<string>();
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

            // Set initialCalendarDate = Max(firstTimeStamp, Min(now, lastTimeStamp))
            var initialCalendarDate = minDate > nowDate ? minDate : (nowDate > maxDate ? maxDate : nowDate);
            var currentDate = SelectedDate ?? initialCalendarDate;

            var (startDate, endDate) = GetDateRange(currentDate, SelectedPeriod, minDate, maxDate);
            var recordsForPeriod = _pvRecords.Where(r => r.Timestamp >= startDate && r.Timestamp < endDate).ToList();

            var selectedGroups = (SelectedGroups == null || !SelectedGroups.Any())
                ? defaultCheckedGroups
                : SelectedGroups.ToHashSet();

            // If SelectedTimeSeries is null/empty or the period was changed, reset to all items of checked groups

            if (SelectedTimeSeries == null || !SelectedTimeSeries.Any())
            {
                // Only reset if not provided
                SelectedTimeSeries = (_pvRecordLabels ?? new Dictionary<string, List<string>>())
                    .Where(g => selectedGroups.Contains(g.Key))
                    .SelectMany(g => g.Value)
                    .ToList();
            }
            else if (!string.IsNullOrEmpty(groupChanged))
            {
                // Only auto-select all series for the group that was just checked
                var allGroupLabels = _pvRecordLabels ?? new Dictionary<string, List<string>>();
                var selectedTimeSeriesSet = SelectedTimeSeries.ToHashSet();

                if (selectedGroups.Contains(groupChanged) && allGroupLabels.TryGetValue(groupChanged, out var labels) && labels != null)
                {
                    // If none of the group's series are selected, select all
                    if (!labels.Any(label => selectedTimeSeriesSet.Contains(label)))
                    {
                        foreach (var label in labels)
                            selectedTimeSeriesSet.Add(label);
                    }
                }
                SelectedTimeSeries = selectedTimeSeriesSet.ToList();
            }
            // else: do nothing, preserve user's manual series selection

            // Always filter SelectedTimeSeries to only include series from checked groups
            var validSeries = (_pvRecordLabels ?? new Dictionary<string, List<string>>())
                .Where(g => selectedGroups.Contains(g.Key))
                .SelectMany(g => g.Value)
                .ToHashSet();

            SelectedTimeSeries = SelectedTimeSeries.Where(ts => validSeries.Contains(ts)).ToList();

            var labelsByGroup = (_pvRecordLabels ?? new Dictionary<string, List<string>>())
                .Where(g => selectedGroups.Contains(g.Key))
                .ToDictionary(g => g.Key, g => g.Value);

            var plotHtmls = CreateSubplots(recordsForPeriod, SelectedTimeSeries, SelectedView, startDate, endDate, labelsByGroup.Keys.ToList());

            model = new VisualizationViewModel
            {
                PlotHtmls = plotHtmls,
                TimeSeriesLabelsByGroup = labelsByGroup,
                SelectedTimeSeries = SelectedTimeSeries,
                ViewOptions = viewOptions,
                PeriodOptions = GetPeriodOptions(),
                SelectedPeriod = SelectedPeriod,
                SelectedView = SelectedView,
                SelectedDate = SelectedDate ?? currentDate,
                MinYear = minDate.Year,
                MaxYear = maxDate.Year,    // ... existing properties ...
                ParameterGroupsByUnit = parameterGroups,
                //SelectedParameters = SelectedParameters // You may need to add logic to set this list
                // ... other properties ...
            };

            // Update GroupChecked based on user selection
            model.GroupChecked = parameterGroups.Keys.ToDictionary(
                g => g,
                g => (SelectedGroups == null || !SelectedGroups.Any())
                ? defaultCheckedGroups.Contains(g)
                : SelectedGroups.Contains(g)
                );

            return View(model);
        }

        private (DateTime, DateTime) GetDateRange(DateTime date, string period, DateTime minDate, DateTime maxDate)
        {
            return period switch
            {
                PeriodDay => (date.Date, date.Date.AddDays(1)),
                PeriodWeek => (GetMondayOfWeek(date), GetMondayOfWeek(date).AddDays(7)),
                PeriodMonth => (new DateTime(date.Year, date.Month, 1), new DateTime(date.Year, date.Month, 1).AddMonths(1)),
                PeriodYear => (new DateTime(date.Year, 1, 1), new DateTime(date.Year, 1, 1).AddYears(1)),
                PeriodAll => (minDate, maxDate.AddDays(1)),
                _ => (new DateTime(date.Year, 1, 1), new DateTime(date.Year, 1, 1).AddYears(1))
            };
        }

        private DateTime GetMondayOfWeek(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }

        private Dictionary<string, (string HtmlWithLegend, string HtmlWithoutLegend)> 
            CreateSubplots(List<PvRecordLists> records, 
            List<string> selectedTimeSeries, 
            string viewName, 
            DateTime startDate,
            DateTime endDate,
            List<string> selectedGroups)
        {
            var plotHtmls = new Dictionary<string, (string HtmlWithLegend, string HtmlWithoutLegend)>();
            if (records is null || !records.Any() || _pvRecordLabels is null) return plotHtmls;

            var activePlotGroups = _pvRecordLabels
                .Where(g => selectedGroups.Contains(g.Key) && g.Value.Any(ts => selectedTimeSeries.Contains(ts)))
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

                    bool isSum = groupName is ConsumptionGroup or ProductionGroup or RadiationGroup;
                    Func<IEnumerable<double?>, double?> aggregationFunc = isSum ? Enumerable.Sum : Enumerable.Average;
                    var data = AggregateData(records, viewName, r => GetValueFromRecord(r, groupName, timeSeriesName), aggregationFunc);
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
            if (groupName == ConsumptionGroup)
            {
                return timeSeriesName switch
                {
                    MeasuredPower => Colors.Red,
                    ConsumedPower => Colors.Green,
                    WallBox => Colors.Purple,
                    Battery => Colors.Blue,
                    Grid => Colors.Orange,
                    Residual => Colors.Magenta,
                    _ => Colors.Gray
                };
            }
            if (groupName == ProductionGroup)
            {
                return timeSeriesName switch
                {
                    MeasuredPower => Colors.Red,
                    PowerGR => Colors.Purple,
                    PowerGRTW => Colors.Blue,
                    PowerGRTWSF => Colors.Green,
                    _ => Colors.Gray
                };
            }
            if (groupName == ResidualsGroup)
            {
                return timeSeriesName switch
                {
                    Reference => Colors.Red,
                    UflGR => Colors.Purple,
                    UflGRTW => Colors.Blue,
                    UflGRTWSF => Colors.Green,
                    _ => Colors.Gray
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
                case IntervalHourly:
                    return records.GroupBy(r => new { r.Timestamp.Year, r.Timestamp.Month, r.Timestamp.Day, r.Timestamp.Hour })
                                  .Select(g => new DataPointViewModel { Timestamp = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day, g.Key.Hour, 0, 0), Value = aggregationFunc(g.Select(valueSelector)) })
                                  .OrderBy(d => d.Timestamp)
                                  .ToList();
                case Interval3Hourly:
                    return records.GroupBy(r => new { r.Timestamp.Date, HourBlock = r.Timestamp.Hour / 3 })
                                  .Select(g => new DataPointViewModel {
                                      Timestamp = g.Key.Date.AddHours(g.Key.HourBlock * 3),
                                      Value = aggregationFunc(g.Select(valueSelector))
                                  })
                                  .OrderBy(d => d.Timestamp)
                                  .ToList();
                case IntervalDaily:
                    return records.GroupBy(r => r.Timestamp.Date)
                                  .Select(g => new DataPointViewModel { Timestamp = g.Key, Value = aggregationFunc(g.Select(valueSelector)) })
                                  .OrderBy(d => d.Timestamp)
                                  .ToList();
                case IntervalWeekly:
                    return records.GroupBy(r => new { Year = System.Globalization.ISOWeek.GetYear(r.Timestamp), Week = System.Globalization.ISOWeek.GetWeekOfYear(r.Timestamp) })
                                  .Select(g => new DataPointViewModel { Timestamp = System.Globalization.ISOWeek.ToDateTime(g.Key.Year, g.Key.Week, DayOfWeek.Monday), Value = aggregationFunc(g.Select(valueSelector)) })
                                  .OrderBy(d => d.Timestamp)
                                  .ToList();
                case IntervalMonthly:
                    return records.GroupBy(r => new { r.Timestamp.Year, r.Timestamp.Month })
                                  .Select(g => new DataPointViewModel { Timestamp = new DateTime(g.Key.Year, g.Key.Month, 1), Value = aggregationFunc(g.Select(valueSelector)) })
                                  .OrderBy(d => d.Timestamp)
                                  .ToList();
                case IntervalYearly:
                    return records.GroupBy(r => r.Timestamp.Year)
                                 .Select(g => new DataPointViewModel { Timestamp = new DateTime(g.Key, 1, 1), Value = aggregationFunc(g.Select(valueSelector)) })
                                 .OrderBy(d => d.Timestamp)
                                  .ToList();
                case Interval15Min:
                default:
                    return records.Select(r => new DataPointViewModel { Timestamp = r.Timestamp, Value = valueSelector(r) }).ToList();
            }
        }

        private double? GetValueFromRecord(PvRecordLists record, string groupName, string label)
        {
            return groupName switch
            {
                ConsumptionGroup => record.Consumption.TryGetValue(label, out var value) ? value : null,
                ProductionGroup => record.Production.TryGetValue(label, out var value) ? value : null,
                ResidualsGroup => record.Residuals.TryGetValue(label, out var value) ? value : null,
                RadiationGroup => record.Radiation.TryGetValue(label, out var value) ? value : null,
                TemperatureGroup => record.Temperature.TryGetValue(label, out var value) ? value : null,
                WindspeedGroup => record.WindSpeed.TryGetValue(label, out var value) ? value : null,
                SnowDepthGroup => record.SnowDepth.TryGetValue(label, out var value) ? value : null,
                HumidityGroup => record.RelativeHumidity.TryGetValue(label, out var value) ? value : null,
                _ => null
            };
        }

        private List<SelectListItem> GetViewOptions() =>
        [
            new(Interval15Min, Interval15MinName),
            new(IntervalHourly, IntervalHourlyName),
            new(Interval3Hourly, Interval3HourlyName),
            new(IntervalDaily, IntervalDailyName),
            new(IntervalWeekly, IntervalWeeklyName),
            new(IntervalMonthly, IntervalMonthlyName),
            new(IntervalYearly, IntervalYearlyName)
        ];

        private List<SelectListItem> GetFilteredViewOptions(string period)
        {
            var allOptions = GetViewOptions();
            return period switch
            {
                PeriodDay => allOptions.Where(o => o.Value == Interval15MinName || o.Value == IntervalHourlyName || o.Value == Interval3HourlyName).ToList(),
                PeriodWeek => allOptions.Where(o => o.Value != IntervalWeeklyName && o.Value != IntervalMonthlyName && o.Value != IntervalYearlyName).ToList(),
                PeriodMonth => allOptions.Where(o => o.Value != Interval15MinName && o.Value != IntervalMonthlyName && o.Value != IntervalYearlyName).ToList(),
                PeriodYear => allOptions.Where(o => o.Value != Interval15MinName && o.Value != IntervalHourlyName && o.Value != IntervalYearlyName).ToList(),
                PeriodAll => allOptions.Where(o => o.Value != Interval15MinName && o.Value != IntervalHourlyName && o.Value != Interval3HourlyName).ToList(),
                _ => allOptions
            };
        }

        private List<SelectListItem> GetPeriodOptions() =>
        [
            new(PeriodDay, PeriodDayName),
            new(PeriodWeek, PeriodWeekName),
            new(PeriodMonth, PeriodMonthName),
            new(PeriodYear, PeriodYearName),
            new(PeriodAll, PeriodAllName)
        ];
        #endregion
    }
}
