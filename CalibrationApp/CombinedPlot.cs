using LEG.CoreLib.Abstractions.SolarCalculations.Domain;
using LEG.OxyPlotHelper;
using OxyPlot;
using OxyPlot.Series;
using static LEG.OxyPlotHelper.MultiPanelPlotContext;

namespace CalibrationApp
{
    public class CombinedPlot
    {
        internal static async Task ProductionProfilePlot(
            List<SolarProductionAggregateResults> annualProductionList, 
            SolarProductionAggregateResults referenceProduction,
            int startYear)
        {
            var countYears = annualProductionList.Count;
            var dimCurves = 1 + countYears;
            const int dimMonth = 13;
            const int dimHours = 24;

            var peakPower = referenceProduction.PeakPowerPerRoof[0];
            foreach (var annualProduction in annualProductionList)
            {
                peakPower = Math.Max(peakPower, annualProduction.PeakPowerPerRoof[0]);
            }
            if (peakPower <= 0.0) peakPower = 1.0;

            var normalizeFactors = new double[dimCurves];
            normalizeFactors[0] = referenceProduction.PeakPowerPerRoof[0] / peakPower;
            for (var annualIndex = 1; annualIndex < dimCurves; annualIndex++)
            {
                normalizeFactors[annualIndex] = annualProductionList[annualIndex - 1].PeakPowerPerRoof[0] / peakPower;
            }

            var theoreticalRelativeProductionList = new List<List<double[]>>();
            var effectiveRelativeProductionList = new List<List<double[]>>();

            var theoreticalAbsoluteMonthReferenceList = new List<double[]>();
            var effectiveAbsoluteMonthReferenceList = new List<double[]>();

            var theoreticalAbsoluteMonthMeanList = new List<double[]>();
            var theoreticalAbsoluteMonthMinList = new List<double[]>();
            var theoreticalAbsoluteMonthMaxList = new List<double[]>();

            var effectiveAbsoluteMonthMeanList = new List<double[]>();
            var effectiveAbsoluteMonthMinList = new List<double[]>();
            var effectiveAbsoluteMonthMaxList = new List<double[]>();

            var theoreticalAbsoluteMeanArray = new double[dimMonth, dimHours];
            var theoreticalAbsoluteMinArray = new double[dimMonth, dimHours];
            var theoreticalAbsoluteMaxArray = new double[dimMonth, dimHours];

            var effectiveAbsoluteMeanArray = new double[dimMonth, dimHours];
            var effectiveAbsoluteMinArray = new double[dimMonth, dimHours];
            var effectiveAbsoluteMaxArray = new double[dimMonth, dimHours];

            var maxPower = 0.0;
            var countPerMonth = new double[dimMonth];
            for (var annualIndex = 0; annualIndex < dimCurves; annualIndex++)
            {
                var productionRecord = annualIndex == 0 ? referenceProduction : annualProductionList[annualIndex - 1];

                var peakPowerPerRoof = productionRecord.PeakPowerPerRoof;
                var theoreticalAggregation = productionRecord.TheoreticalAggregation;
                var effectiveAggregation = productionRecord.EffectiveAggregation;

                var theoreticalRelativeYearMean = new List<double[]>();

                var effectiveRelativeYearMean = new List<double[]>();

                for (var month = 1; month < dimMonth; month++)
                {
                    var absoluteMonth = 0.0;
                    for (var hour = 0; hour < dimHours; hour++)
                    {
                        absoluteMonth += theoreticalAggregation[1, month, 1 + hour]; // kW
                    }
                    if (annualIndex > 0 && absoluteMonth > 0)
                    {
                        countPerMonth[month] += 1.0;
                    }
                    var theoreticalRelativeMonth = new double[dimHours];
                    var effectiveRelativeMonth = new double[dimHours];

                    var theoreticalAbsoluteMean = new double[dimHours];

                    var effectiveAbsoluteMean = new double[dimHours];

                    for (var hour = 0; hour < dimHours; hour++)
                    {
                        var hourIndex = 1 + hour;
                        theoreticalRelativeMonth[hour] = theoreticalAggregation[1, month, hourIndex] * normalizeFactors[annualIndex];
                        effectiveRelativeMonth[hour] = effectiveAggregation[1, month, hourIndex] * normalizeFactors[annualIndex];

                        if (annualIndex == 0)
                        {
                            theoreticalAbsoluteMean[hour] = theoreticalAggregation[1, month, hourIndex] * peakPowerPerRoof[0]; // kW
                            effectiveAbsoluteMean[hour] = effectiveAggregation[1, month, hourIndex] * peakPowerPerRoof[0]; // kW
                        }
                        else
                        {
                            if (absoluteMonth > 0)
                            {
                                var theoreticalValue = theoreticalAggregation[1, month, hourIndex] * peakPowerPerRoof[0]; // kW
                                var effectiveValue = effectiveAggregation[1, month, hourIndex] * peakPowerPerRoof[0]; // kW

                                if (countPerMonth[month] == 1)  // Initialize with first value
                                {
                                    theoreticalAbsoluteMinArray[month, hour] = theoreticalValue;
                                    effectiveAbsoluteMinArray[month, hour] = effectiveValue;
                                }
                                
                                theoreticalAbsoluteMeanArray[month, hour] += theoreticalValue;
                                theoreticalAbsoluteMinArray[month, hour] = Math.Min(theoreticalAbsoluteMinArray[month, hour], theoreticalValue);
                                theoreticalAbsoluteMaxArray[month, hour] = Math.Max(theoreticalAbsoluteMaxArray[month, hour], theoreticalValue);

                                effectiveAbsoluteMeanArray[month, hour] += effectiveValue;
                                effectiveAbsoluteMinArray[month, hour] = Math.Min(effectiveAbsoluteMinArray[month, hour], effectiveValue);
                                effectiveAbsoluteMaxArray[month, hour] = Math.Max(effectiveAbsoluteMaxArray[month, hour], effectiveValue);
                            }
                        }
                    }
                    theoreticalRelativeYearMean.Add(theoreticalRelativeMonth);
                    effectiveRelativeYearMean.Add(effectiveRelativeMonth);

                    if (annualIndex == 0)
                    {
                        theoreticalAbsoluteMonthReferenceList.Add(theoreticalAbsoluteMean);
                        effectiveAbsoluteMonthReferenceList.Add(effectiveAbsoluteMean);
                        maxPower = Math.Max(maxPower, theoreticalAbsoluteMean.Max());
                    }
                }
                theoreticalRelativeProductionList.Add(theoreticalRelativeYearMean);
                effectiveRelativeProductionList.Add(effectiveRelativeYearMean);
            }
            for (var month = 1; month < dimMonth; month++)
            {
                var theoreticalMean = new double[dimHours];
                var theoreticalMin = new double[dimHours];
                var theoreticalMax = new double[dimHours];

                var effectiveMean = new double[dimHours];
                var effectiveMin = new double[dimHours];
                var effectiveMax = new double[dimHours];
                for (var hour = 0; hour < dimHours; hour++)
                {
                    theoreticalMean[hour] = theoreticalAbsoluteMeanArray[month, hour] / countPerMonth[month];
                    theoreticalMin[hour] = theoreticalAbsoluteMinArray[month, hour];
                    theoreticalMax[hour] = theoreticalAbsoluteMaxArray[month, hour];

                    effectiveMean[hour] = effectiveAbsoluteMeanArray[month, hour] / countPerMonth[month];
                    effectiveMin[hour] = effectiveAbsoluteMinArray[month, hour];
                    effectiveMax[hour] = effectiveAbsoluteMaxArray[month, hour];
                }
                theoreticalAbsoluteMonthMeanList.Add(theoreticalMean);
                theoreticalAbsoluteMonthMinList.Add(theoreticalMin);
                theoreticalAbsoluteMonthMaxList.Add(theoreticalMax);

                effectiveAbsoluteMonthMeanList.Add(effectiveMean);
                effectiveAbsoluteMonthMinList.Add(effectiveMin);
                effectiveAbsoluteMonthMaxList.Add(effectiveMax);
            }

            // Define plot axes styles
            var peakPowerBound = referenceProduction.PeakPowerPerRoof.Sum();
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
                overallTitle: $"Computed Profiles for: {referenceProduction.SiteId}, max {maxPower:N1}kW/{peakPowerBound:N1}kWp, {referenceProduction.EffectiveYear[0]:N0}kWh (effective {referenceProduction.EvaluationYear})",
                panelXAxis: panelXAxis,
                panelYAxis: [panelYAxis0, panelYAxis1],
                legendPosition: -6         // "-" => outside, "6" => middle right 
                );

            // Plot profiles
            var monthLabels = new List<string>() { "", " Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul,", "Aug", "Sep", "Oct", "Nov", "Dec" };
            var hourSupportReference = Enumerable.Range(0, 24).Select(h => (h + 1.5)).ToArray();
            hourSupportReference[^1] = 24.0;
            var hourSupportE3DC = Enumerable.Range(0, 24).Select(h => (h + 0.5)).ToArray();

            // Upper Panel
            for (var curveIndex = 1; curveIndex <= dimCurves; curveIndex++)
            {
                var annualIndex = curveIndex % dimCurves;   // 0 = reference, 1..N = annualProductionList => Plot reference as last curve
                var hourSupport = annualIndex == 0 ? hourSupportReference : hourSupportE3DC;
                var curveColor = annualIndex == 0 ? OxyColors.DarkBlue : ManualColor(annualIndex);
                for (var month = 1; month <= 12; month++)
                {
                    // Plot of relative outputs in upper panel
                    var theoreticalLabel = month == 1 ? "Theoretical" : "";
                    var effectiveLabel = month == 1 ? "Effective" : "";
                    var curveLabel = month != 1 ? "" : annualIndex == 0 ? "Model" : $"{startYear - 1 + annualIndex}";

                    var theoreticalRelativeProductionPerMonth = theoreticalRelativeProductionList[annualIndex][month - 1];
                    var effectiveRelativeProductionPerMonth = effectiveRelativeProductionList[annualIndex][month - 1];

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
                    hourSupportE3DC, theoreticalAbsoluteMonthMaxList[month - 1], theoreticalAbsoluteMonthMinList[month - 1],
                    OxyColors.LightSteelBlue, strokeColor: null, strokeThickness: 1, label: "");
                context.AddAreaToPanel(1, month - 1,
                    hourSupportE3DC, effectiveAbsoluteMonthMaxList[month - 1], effectiveAbsoluteMonthMinList[month - 1],
                     OxyColors.LightSteelBlue, strokeColor: null, strokeThickness: 1, label: month == 1 ? "Range" : "");

                // Plot of aggregate absolute productions in lower panel
                context.AddCurveToPanel(1, month - 1, hourSupportReference, theoreticalAbsoluteMonthReferenceList[month - 1],
                    OxyColors.DarkBlue, lineWidth: 1, lineStyle: LineStyle.Dash, label: "", filterZeros: true);
                context.AddCurveToPanel(1, month - 1, hourSupportReference, effectiveAbsoluteMonthReferenceList[month - 1],
                    OxyColors.DarkBlue, lineWidth: 2, lineStyle: LineStyle.Solid, label: "", filterZeros: true);

                // Plot of aggregate absolute productions in lower panel
                context.AddCurveToPanel(1, month - 1, hourSupportE3DC, theoreticalAbsoluteMonthMeanList[month - 1],
                    OxyColors.DarkRed, lineWidth: 1, lineStyle: LineStyle.Dash, label: theoreticalLabel, filterZeros: true);
                context.AddCurveToPanel(1, month - 1, hourSupportE3DC, effectiveAbsoluteMonthMeanList[month - 1],
                    OxyColors.DarkRed, lineWidth: 2, lineStyle: LineStyle.Solid, label: effectiveLabel, filterZeros: true);

                // Add month labels and total production texts
                context.AddTextToPanel(0, month - 1, 12, 1.08, $"{monthLabels[month]}", OxyColors.Black,
                    textAlignment: 8, fontSize: 10, drawBox: false);
                context.AddTextToPanel(1, month - 1, 23, powerMaxScale * 0.98, $"{referenceProduction.EffectiveMonth[0][month]:N0} kWh", OxyColors.Black,
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
