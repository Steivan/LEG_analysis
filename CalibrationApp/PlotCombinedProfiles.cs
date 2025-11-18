using LEG.CoreLib.Abstractions.SolarCalculations.Domain;
using LEG.OxyPlotHelper;
using OxyPlot;
using static LEG.OxyPlotHelper.MultiPanelPlotContext;

namespace CalibrationApp
{
    public class PlotCombinedProfiles
    {
        internal static async Task ProductionProfilePlot(
            List<SolarProductionAggregateResults> annualProductionList, 
            SolarProductionAggregateResults referenceModel,
            double[] referenceModelAdjustmentFactors,
            bool adjustReferenceModel,
            int startYear)
        {
            var ((dimCurves, dimMonth, dimHours), 
                (referenceMaxPower, overallPeakPower, referenceModelPowerPerMonth),
                (maximaRelativeYearMonthLists,
                referenceMaximaAbsoluteMonthList,
                productionMaximaAbsoluteMonthMeanList,
                productionMaximaAbsoluteMonthMinLis,
                productionMaximaAbsoluteMonthMaxList),

                (effectiveRelativeYearMonthLists,
                referenceEffectiveAbsoluteMonthList,
                productionEffectiveAbsoluteMonthMeanList,
                productionEffectiveAbsoluteMonthMinList,
                productionEffectiveAbsoluteMonthMaxList)
                ) = DecomposeRecords.ExtractProfileLists(annualProductionList, referenceModel, referenceModelAdjustmentFactors, adjustReferenceModel: adjustReferenceModel);

            // Define plot axes styles
            var peakPowerBound = referenceModel.PeakPowerPerRoof.Sum();
            var powerMaxScale = referenceMaxPower * 1.1;
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
                "Average Diurnal Profiles per Month" // Title
            );

            var panelYAxis0 = new AxisStyleRecord(
                false,
                0.2,                                // MajorTickSize
                0.05,                               // MinorTickSize
                1,
                panelXAxis.MajorGridlineStyle,                  // MajorGridlineStyle
                panelXAxis.MinorGridlineStyle,                  // MinorGridlineStyle
                panelXAxis.AxislineStyle,                       // AxislineStyle
                panelXAxis.AxislineColor,                       // AxislineColor
                panelXAxis.TextColor,                           // TextColor
                panelXAxis.TicklineColor,                       // TicklineColor
                8,
                true,
                true,
                "Relative Output"                // Title
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
                "Power [kW]"                               // Title
            );

            // Initialize plot context
            var context = new MultiPanelPlotContext(
                nRows: 2,
                nCols: 12,
                xMin: 0, xMax: 24,
                yMins: [0, 0],
                yMaxs: [1.1, powerMaxScale],
                overallTitle: $"Computed Profiles for: {referenceModel.SiteId}, max {referenceMaxPower:N1}kW/{peakPowerBound:N1}kWp, {referenceModelPowerPerMonth[0]:N0}kWh (effective {referenceModel.EvaluationYear})",
                panelXAxis: panelXAxis,
                panelYAxis: [panelYAxis0, panelYAxis1],
                legendPosition: -6         // "-" => outside, "6" => middle right 
                );

            // Plot profiles
            var monthLabels = new List<string>() { "", " Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul,", "Aug", "Sep", "Oct", "Nov", "Dec" };
            var hourSupport = Enumerable.Range(0, 24).Select(h => (h + 0.5)).ToArray();

            // Upper Panel
            for (var curveIndex = 1; curveIndex <= dimCurves; curveIndex++)
            {
                var annualIndex = curveIndex % dimCurves;   // 0 = reference, 1..N = annualProductionList => Plot reference as last curve
                var curveColor = annualIndex == 0 ? OxyColors.DarkBlue : ManualColor(annualIndex);
                for (var month = 1; month <= 12; month++)
                {
                    // Plot of relative outputs in upper panel
                    var theoreticalLabel = month == 1 ? "Theoretical" : "";
                    var effectiveLabel = month == 1 ? "Effective" : "";
                    var curveLabel = month != 1 ? "" : annualIndex == 0 ? "Model" : $"{startYear - 1 + annualIndex}";

                    var theoreticalRelativeProductionPerMonth = maximaRelativeYearMonthLists[annualIndex][month - 1];
                    var effectiveRelativeProductionPerMonth = effectiveRelativeYearMonthLists[annualIndex][month - 1];

                    context.AddCurveToPanel(0, month - 1, hourSupport, effectiveRelativeProductionPerMonth,
                        curveColor, lineWidth: 2, lineStyle: LineStyle.Solid, label: curveLabel, filterZeros: true);
                    context.AddCurveToPanel(0, month - 1, hourSupport, theoreticalRelativeProductionPerMonth,
                        curveColor, lineWidth: 1, lineStyle: LineStyle.Dash, label: "", filterZeros: true);

                    // Solid Line on top of upper panel (100% level)
                    context.AddCurveToPanel(0, month - 1, [0, 24], [1.0, 1.0],
                        OxyColors.Black, lineWidth: 1, lineStyle: LineStyle.Solid, label: "", filterZeros: false);
                }
            }

            // Lower Panel;
            for (var month = 1; month <= 12; month++)
            {
                // Plot of absolute outputs in upper panel
                var theoreticalLabel = month == 1 ? "Theoretical" : "";
                var effectiveLabel = month == 1 ? "Effective" : "";

                // Plot Ranges in lower panel
                context.AddAreaToPanel(1, month - 1,
                    hourSupport, productionMaximaAbsoluteMonthMaxList[month - 1], productionMaximaAbsoluteMonthMinLis[month - 1],
                    OxyColors.LightSteelBlue, strokeColor: null, strokeThickness: 1, label: "");
                context.AddAreaToPanel(1, month - 1,
                    hourSupport, productionEffectiveAbsoluteMonthMaxList[month - 1], productionEffectiveAbsoluteMonthMinList[month - 1],
                     OxyColors.LightSteelBlue, strokeColor: null, strokeThickness: 1, label: month == 1 ? "Range" : "");

                // Plot of aggregate absolute productions in lower panel
                context.AddCurveToPanel(1, month - 1, hourSupport, referenceMaximaAbsoluteMonthList[month - 1],
                    OxyColors.DarkBlue, lineWidth: 1, lineStyle: LineStyle.Dash, label: "", filterZeros: true);
                context.AddCurveToPanel(1, month - 1, hourSupport, referenceEffectiveAbsoluteMonthList[month - 1],
                    OxyColors.DarkBlue, lineWidth: 2, lineStyle: LineStyle.Solid, label: "", filterZeros: true);

                // Plot of aggregate absolute productions in lower panel
                context.AddCurveToPanel(1, month - 1, hourSupport, productionMaximaAbsoluteMonthMeanList[month - 1],
                    OxyColors.DarkRed, lineWidth: 1, lineStyle: LineStyle.Dash, label: theoreticalLabel, filterZeros: true);
                context.AddCurveToPanel(1, month - 1, hourSupport, productionEffectiveAbsoluteMonthMeanList[month - 1],
                    OxyColors.DarkRed, lineWidth: 2, lineStyle: LineStyle.Solid, label: effectiveLabel, filterZeros: true);

                // Add month labels and total production texts
                context.AddTextToPanel(0, month - 1, 12, 1.08, $"{monthLabels[month]}", OxyColors.Black,
                    textAlignment: 8, fontSize: 10, drawBox: false);
                context.AddTextToPanel(1, month - 1, 23, powerMaxScale * 0.98, $"{referenceModelPowerPerMonth[month]:N0} kWh", OxyColors.Black,
                    textAlignment: 9, fontSize: 10, drawBox: false);
            }

            //context.AddMarkerToPanel(1, 5, 12, 50, OxyColors.Green, MarkerType.Circle, 4);
            //context.AddTextToPanel(0, 0, 12, 80, "Peak", OxyColors.Black, textAlignment: 5, fontSize: 10, drawBox: false);

            // Show the plot in a window
            MultiPanelPlotContext.ShowPlot(context.PlotModel, 1200, 600); // or context.PlotModel.ShowPlot(...)

            // Optionally, save the plot as a PNG
            context.SavePlot("plot.png", width: 800, height: 600);
        }
    }

}
