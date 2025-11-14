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

        public async Task<IActionResult> Index(string selectedTimeSeries = "MeasuredPower", string selectedView = "Daily")
        {
            if (_pvRecords is null)
            {
                var dataImporter = new DataImporter();
                var (siteId, pvRecords, modelValidRecords, installedKwP, periodsPerHour) = await dataImporter.ImportE3DcData(1, meteoDataLag: 0);
                _pvRecords = pvRecords;
            }

            var plt = CreatePlot(selectedTimeSeries, selectedView);

            var model = new VisualizationViewModel
            {
                PlotHtml = plt.GetPngHtml(800, 600),
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

            if (_pvRecords is null || !_pvRecords.Any())
            {
                plt.Title("No data available to display.");
                return plt;
            }

            var minYear = _pvRecords.First().Timestamp.Year;
            var records = _pvRecords.Where(r => r.Timestamp.Year == minYear).ToList();

            var (aggregationFunc, isBar) = GetAggregationAndSeriesType(timeSeriesName, viewName);
            var data = AggregateData(records, viewName, r => GetPropertyValue(r, timeSeriesName), aggregationFunc);

            if (isBar)
            {
                var positions = data.Select((_, i) => (double)i).ToArray();
                var values = data.Select(d => d.Value).ToArray();
                plt.Add.Bars(positions, values);

                var ticks = positions.Select((pos, i) => new Tick(pos, data[i].Timestamp.ToString("yyyy-MM-dd"))).ToArray();
                plt.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(ticks);
                plt.Axes.Bottom.TickLabelStyle.Rotation = 45;
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

        #region Helper Methods
        private List<DataPointViewModel> AggregateData(List<PvRecord> records, string view, Func<PvRecord, double> valueSelector, Func<IEnumerable<double>, double> aggregationFunc)
        {
            switch (view)
            {
                case "Weekly":
                    return records.GroupBy(r => new { r.Timestamp.Year, Week = System.Globalization.ISOWeek.GetWeekOfYear(r.Timestamp), r.Timestamp.Hour })
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
            Func<IEnumerable<double>, double> aggregationFunc = isSum ? Enumerable.Sum : Enumerable.Average;
            bool isBar = viewName == "Monthly" || viewName == "Annual" || viewName == "Overall";
            return (aggregationFunc, isBar);
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
            new("Daily", "Daily"),
            new("Weekly", "Weekly"),
            new("Monthly", "Monthly"),
            new("Annual", "Annual"),
            new("Overall", "Overall")
        ];
        #endregion
    }
}