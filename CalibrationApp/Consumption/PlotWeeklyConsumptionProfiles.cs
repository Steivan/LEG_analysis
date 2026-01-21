using LEG.OxyPlotHelper;
using OxyPlot;
using CalibrationApp.Helpers;
using static CalibrationApp.Consumption.WeekdaySeasonalAnalysis;
using static LEG.OxyPlotHelper.MultiPanelPlotContext;

namespace CalibrationApp.Consumption
{
    public class PlotWeeklyConsumptionProfiles
    {
        internal static double[] Plot13x4WeeklyProfiles(string siteId, List<WeekdayStats> timeSlotStats)
        {
            const int periods13x4 = 13;
            const int daysPerWeek = 7;

            // Prepare data
            var weekdaySupport = new double[daysPerWeek];
            var count = new int[periods13x4, daysPerWeek];
            var mean = new double[periods13x4, daysPerWeek];
            var hiRange = new double[periods13x4, daysPerWeek];
            var loRange = new double[periods13x4, daysPerWeek];

            var conversionFactor = 1.0 / 1000.0; // Wh to kWh
            foreach (var timeSlot in timeSlotStats)
            {
                var p13 = timeSlot.Period - 1;
                var i = (int)timeSlot.DayOfWeek;
                if (p13 == 0)
                {
                    weekdaySupport[i] = (double)i + 0.5;
                }
                count[p13, i] = timeSlot.SampleDays;
                mean[p13, i] = timeSlot.AverageDailyKWh * conversionFactor;
                var stdDev = timeSlot.StdDevKWh * conversionFactor;
                hiRange[p13, i] = mean[p13, i] + stdDev;
                loRange[p13, i] = mean[p13, i] - stdDev;
            }
            var (weekdayFactors, modelMeans) = Normalize2DArrays.GetModelArray(mean);
            Console.WriteLine($"Weekday factors: {string.Join(", ", weekdayFactors.Select(f => f.ToString("F3")))}");
            Console.WriteLine();

            // Define plot axes styles
            var maxPower = hiRange.Max2D();
            var powerMaxScale = maxPower * 1.1;
            var (majorTickSizer, minorTickSize, nDecimals) = GetAxisTickSizes(powerMaxScale);

            var panelXAxis = new AxisStyleRecord(
                true,
                3.0,                                    // MajorTickSize
                1.0,                                    // MinorTickSize
                0,
                LineStyle.Dash,                                    // MajorGridlineStyle
                LineStyle.Dot,                                     // MinorGridlineStyle
                LineStyle.Solid,                                    // AxislineStyle
                OxyColors.Black,                        // AxislineColor
                OxyColors.LightGray,                       // TextColor
                OxyColors.LightGray,                    // TicklineColor
                7,
                false,
                false,
                "Average Weekday Profiles per 4 Weeks Period" // Title
            );

            var panelYAxis1 = new AxisStyleRecord(
                false,
                majorTickSizer,                                 // MajorTickSize
                minorTickSize,                                  // MinorTickSize
                nDecimals,
                panelXAxis.MajorGridlineStyle,                  // MajorGridlineStyle
                panelXAxis.MinorGridlineStyle,                  // MinorGridlineStyle
                panelXAxis.AxislineStyle,                       // AxislineStyle
                panelXAxis.AxislineColor,                       // AxislineColor
                panelXAxis.TextColor,                           // TextColor
                panelXAxis.TicklineColor,                       // TicklineColor
                10,
                true,
                true,
                "Power [kWh]"                               // Title
            );

            // Initialize plot context
            var context = new MultiPanelPlotContext(
                nRows: 1,
                nCols: periods13x4,
                xMin: 0, xMax: daysPerWeek,
                yMins: [0],
                yMaxs: [powerMaxScale],
                overallTitle: $"Consumption Profiles for: {siteId}, Avg Daily Power = {maxPower:N3}kWh",
                panelXAxis: panelXAxis,
                panelYAxis: [panelYAxis1],
                legendPosition: -6         // "-" => outside, "6" => middle right 
                );

            // Plot profiles
            var period13x4Labels = new List<string>() { "P1", "P2", "P3", "P4", "P5", "P6", "P7", "P8", "P9", "P10", "P11", "P12", "P13" };
            for (var period = 0; period < periods13x4; period++)
            {
                // Plot InterQuartile range
                context.AddAreaToPanel(0, period,
                    weekdaySupport, Convert2DArray.GetRow(hiRange, period, daysPerWeek), Convert2DArray.GetRow(loRange, period, daysPerWeek),
                    OxyColors.LightSteelBlue, strokeColor: null, strokeThickness: 1, label: period == 0 ? "Range" : "");

                // Plot the daily means
                context.AddCurveToPanel(0, period, weekdaySupport, Convert2DArray.GetRow(modelMeans, period, daysPerWeek),
                    OxyColors.Red, lineWidth: 1, lineStyle: LineStyle.Solid, label: period == 0 ? "Model" : "", filterZeros: false);

                context.AddCurveToPanel(0, period, weekdaySupport, Convert2DArray.GetRow(mean, period, daysPerWeek),
                    OxyColors.Green, lineWidth: 2, lineStyle: LineStyle.Solid, label: period == 0 ? "Mean" : "", filterZeros: false);

                // Add period labels
                context.AddTextToPanel(0, period, daysPerWeek / 2, maxPower, $"{period13x4Labels[period]}", OxyColors.Black,
                    textAlignment: 2, fontSize: 10, drawBox: false);
            }

            // Show the plot in a window
            MultiPanelPlotContext.ShowPlot(context.PlotModel, 1200, 600); // or context.PlotModel.ShowPlot(...)


            // Optionally, save the plot as a PNG
            context.SavePlot("plot.png", width: 800, height: 600);

            return weekdayFactors;
        }

    }

}
