using LEG.CoreLib.SolarCalculations.Utilities;
using LEG.CoreLib.SolarCalculations.Domain;
using LEG.CoreLib.HorizonProfiles;
using LEG.HorizonProfiles.Abstractions;
using LEG.CoreLib.Abstractions.SolarCalculations.Domain;
using static LEG.CoreLib.Abstractions.ReferenceData.SiteStatus;

namespace LEG.CoreLib.SolarCalculations.Calculations
{
    public static class SolarCalculate
    {
        public static (double[] azi, double[] elev, double[] elev2, double[] size, double[] peak) GetRoofArrays(
                List<PvRoof> roofRecords)
        // Extract roof data from List of PvRoof records and return as 1D arrays per attribute
        {
            var nrRoofs = roofRecords.Count;
            var azi = new double[nrRoofs];
            var elev = new double[nrRoofs];
            var elev2 = new double[nrRoofs];
            var size = new double[nrRoofs];
            var peak = new double[nrRoofs];
            for (var i = 0; i < nrRoofs; i++)
            {
                azi[i] = roofRecords[i].Azi;
                elev[i] = roofRecords[i].Elev;
                elev2[i] = roofRecords[i].Elev2;
                size[i] = roofRecords[i].Area;
                peak[i] = roofRecords[i].Peak;
            }

            return (azi, elev, elev2, size, peak);
        }

        public static async Task<SolarProductionDetails> ComputePvSiteDetailedProductionFromSiteData(
                IPvSiteModel pvSiteModel,
                IHorizonProfileClient horizonProfileClient,
                ISiteCoordinateProvider coordinateProvider,
                ISiteHorizonControlProvider horizonControlProvider,
                int evaluationYear = BasicParametersAndConstants.DefaultYear,
                int evaluationStartHour = 4,
                int evaluationEndHour = 22,
                int minutesPerPeriod = 10,
                bool print = false)
        {
            // Initialize horizon data: new approach for evaluating the time of sunrise and sunset 
            var horizonService = new HorizonInitializationService(horizonProfileClient);
            var (siteAzimuths, siteAngles) = await horizonService.InitializeHorizonAzimuthsAndAngles(
                siteName: pvSiteModel.PvSite.SystemName,
                coordinateProvider: coordinateProvider,
                horizonControlProvider: horizonControlProvider,
                siteLat: pvSiteModel.PvSite.Lat,
                siteLon: pvSiteModel.PvSite.Lon
            );

            // Input data extracted from pvSiteModel
            var siteId = pvSiteModel.PvSite.SystemName;
            var town = pvSiteModel.PvSite.Town;
            var lon = pvSiteModel.PvSite.Lon;
            var lat = pvSiteModel.PvSite.Lat;
            var utcShift = pvSiteModel.PvSite.UtcShift;
            var indicativeNrOfRoofs = pvSiteModel.PvSite.IndicativeNrOfRoofs;

            // Extract modified data
            var roofsList = pvSiteModel.Inverters
                .SelectMany(inverter => pvSiteModel.RoofsPerInverter[inverter.SystemName])
                .ToList();
            var nrRoofs = roofsList.Count;

            var (azi, elev, elev2, size, peak) = GetRoofArrays(roofsList);
            var (_, _, factorModel) = FourierHelpers.GetFourierMeteo(pvSiteModel.MeteoProfile);

            var countPerMonth = new int[13];
            var countPerHour = new int[25];

            var date0 = new DateTime(evaluationYear, 1, 1);
            var dimensionDays = (new DateTime(evaluationYear + 1, 1, 1) - date0).Days;

            // Support for evaluation
            var startHour = evaluationStartHour - 1;
            var startMinute = 60 - minutesPerPeriod / 2;
            var startSecond = 0;
            var stepsPerHour = 60 / minutesPerPeriod; // 6
            var stepsPerDay = 24 * stepsPerHour;
            var deltaInterval = TimeSpan.FromMinutes(minutesPerPeriod);

            var dimensionIntervals = 1 + (evaluationEndHour - evaluationStartHour) * 60 / minutesPerPeriod;   // 103;
            const int maxDaysPerYear = 366; // for leap years too

            // Annual support dimensions
            var dimensionAnnualSupport = maxDaysPerYear * stepsPerDay;
            var validRecordsCount = (DateTime.IsLeapYear(evaluationYear) ? 366 : 365) * stepsPerDay;
            var theoreticalIrradiationFactor = new double[nrRoofs, dimensionAnnualSupport];
            var effectiveIrradiationFactor = new double[nrRoofs, dimensionAnnualSupport];

            Console.Write($"Calculating gain profiles for site {siteId} / {evaluationYear}: ...");
            for (var dayIndex = 0; dayIndex < dimensionDays; dayIndex++)
            {
                var indexDate = date0.AddDays(dayIndex);
                var indexYear = indexDate.Year;
                var indexMonth = indexDate.Month;
                var indexDay = indexDate.Day;
                if (indexDay == 1 && print)
                {
                    Console.WriteLine();
                    Console.Write($"{indexMonth,4} / 12 ...");
                }

                var dateTime0 = new DateTime(indexYear, indexMonth, indexDay, startHour, startMinute, startSecond);
                var indexTimeLag = (indexDate - new DateTime(indexYear, 1, 1)).Days;

                countPerMonth[indexMonth] += 1;
                var factorSun = factorModel[dayIndex + 1];

                for (var intervalIndex = 0; intervalIndex < dimensionIntervals; intervalIndex++)
                {
                    var indexDateTime = dateTime0.Add(deltaInterval * intervalIndex);
                    var indexHour = indexDateTime.Hour;
                    var indexMinute = indexDateTime.Minute;
                    var indexSecond = indexDateTime.Second;

                    if (dayIndex == 0) countPerHour[indexHour] += 1;

                    var (sunAziDeg, sunElevDeg) = AstroGeometry.GetSolarAziElev(indexYear, indexMonth, indexDay, indexHour, indexMinute,
                        indexSecond, utcShift, lon, lat);

                    var horizonElevDeg = SunRiseSetFromProfile.HorizonElevation(sunAziDeg, siteAzimuths, siteAngles);
                    if (sunElevDeg < horizonElevDeg)
                    {
                        continue;
                    }

                    // sun is above horizon
                    var indexAnnualSupport = indexTimeLag * stepsPerDay + indexHour * stepsPerHour + indexMinute / minutesPerPeriod;
                    for (var roof = 1; roof <= nrRoofs; roof++)
                    {
                        var geometryFactor = AstroGeometry.GetCosAngleToSun(sunAziDeg, sunElevDeg, azi[roof - 1], elev[roof - 1], 0);
                        var combinedFactor = geometryFactor * factorSun;

                        theoreticalIrradiationFactor[roof - 1, indexAnnualSupport] = geometryFactor;
                        effectiveIrradiationFactor[roof - 1, indexAnnualSupport] = combinedFactor;
                    }
                }
            }

            return new SolarProductionDetails(
                SiteId: pvSiteModel.PvSite.SystemName,
                Town: town,
                EvaluationYear: evaluationYear,
                UtcShift: utcShift,
                DimensionRoofs: nrRoofs,
                ValidRecordsCount: validRecordsCount,
                PeakPowerPerRoof: peak,
                TimeStamps: [.. Enumerable.Range(0, dimensionAnnualSupport)
                    .Select(i => date0.AddMinutes(minutesPerPeriod * i))],
                TheoreticalIrradiationPerRoofAndInterval: theoreticalIrradiationFactor,
                EffectiveIrradiationPerRoofAndInterval: effectiveIrradiationFactor,
                CountPerMonth: countPerMonth,
                CountPerHour: countPerHour
            );
        }

        public static async Task<SolarProductionAggregate> ComputePvSiteAggregateProductionFromSiteData(
                IPvSiteModel pvSiteModel,
                IHorizonProfileClient horizonProfileClient,
                ISiteCoordinateProvider coordinateProvider,
                ISiteHorizonControlProvider horizonControlProvider,
                int evaluationYear = BasicParametersAndConstants.DefaultYear,
                int evaluationStartHour = 4,
                int evaluationEndHour = 22,
                int minutesPerPeriod = 10,
                bool print = false)
        {
            var (
                    site,
                    town,
                    _,
                    utcShift,
                    nrRoofs,
                    validRecordsCount,
                    peakPowerPerRoof,
                    timeStamps,
                    theoreticalIrradiationPerRoofAndInterval,
                    effectiveIrradiationPerRoofAndInterval,
                    countPerMonth,
                    countPerHour
                )
                = await ComputePvSiteDetailedProductionFromSiteData(
                    pvSiteModel,
                    horizonProfileClient,
                    coordinateProvider,
                    horizonControlProvider,
                    evaluationYear: evaluationYear,
                    evaluationStartHour: evaluationStartHour,
                    evaluationEndHour: evaluationEndHour,
                    minutesPerPeriod: minutesPerPeriod,
                    print: print
                    );

            var theoreticalAggregation = new double[nrRoofs + 1, 13, 25];
            var effectiveAggregation = new double[nrRoofs + 1, 13, 25];
            for (var i = 0; i < validRecordsCount; i++)
            {
                var iMonth = timeStamps[i].Month;
                var iHour = timeStamps[i].Hour;
                for (var iRoof = 1; iRoof <= nrRoofs; iRoof++)
                {
                    theoreticalAggregation[iRoof, iMonth, iHour] +=
                        theoreticalIrradiationPerRoofAndInterval[iRoof - 1, i];
                    effectiveAggregation[iRoof, iMonth, iHour] +=
                        effectiveIrradiationPerRoofAndInterval[iRoof - 1, i];
                }
            }

            Console.WriteLine(" done.");

            for (var indexMonth = 1; indexMonth < 13; indexMonth++)
            {
                for (var indexHour = 1; indexHour < 25; indexHour++)
                {
                    var factorMonthHour = countPerMonth[indexMonth] * countPerHour[indexHour];

                    if (factorMonthHour <= 0)
                    {
                        continue;
                    }

                    for (var roof = 1; roof <= nrRoofs; roof++)
                    {
                        theoreticalAggregation[roof, indexMonth, indexHour] /= factorMonthHour;
                        effectiveAggregation[roof, indexMonth, indexHour] /= factorMonthHour;
                    }
                }
            }

            for (var roof = 1; roof <= nrRoofs; roof++)
            {
                for (var indexMonth = 1; indexMonth < 13; indexMonth++)
                {
                    for (var indexHour = 1; indexHour < 25; indexHour++)
                    {
                        theoreticalAggregation[0, indexMonth, indexHour] +=
                            theoreticalAggregation[roof, indexMonth, indexHour] * peakPowerPerRoof[roof - 1];
                        effectiveAggregation[0, indexMonth, indexHour] +=
                            effectiveAggregation[roof, indexMonth, indexHour] * peakPowerPerRoof[roof - 1];
                    }
                }
            }

            var peakSum = peakPowerPerRoof.Select(v => v).Sum();
            for (var indexMonth = 1; indexMonth < 13; indexMonth++)
            {
                for (var indexHour = 1; indexHour < 25; indexHour++)
                {
                    theoreticalAggregation[0, indexMonth, indexHour] /= peakSum;
                    effectiveAggregation[0, indexMonth, indexHour] /= peakSum;
                }
            }

            return new SolarProductionAggregate(
                SiteId: site,
                Town: town,
                EvaluationYear: evaluationYear,
                UtcShift: utcShift,
                DimensionRoofs: nrRoofs,
                PeakPowerPerRoof: peakPowerPerRoof,
                TheoreticalAggregation: theoreticalAggregation,
                EffectiveAggregation: effectiveAggregation,
                CountPerMonth: countPerMonth
                );
        }

        public static async Task<SolarProductionAggregateResults> ComputePvSiteAggregateProductionPerRoof(this IPvSiteModel pvSiteModel,
            IHorizonProfileClient horizonProfileClient,
            ISiteCoordinateProvider coordinateProvider,
            ISiteHorizonControlProvider horizonControlProvider,
            int evaluationYear = BasicParametersAndConstants.DefaultYear,
            int evaluationStartHour = 4,
            int evaluationEndHour = 22,
            int minutesPerPeriod = 10,
            bool print = false)
        {
            // return new SolarProductionAggregateResults();
            var (
                    site,
                    town,
                    _,
                    utcShift,
                    nrRoofs,
                    peak,
                    theoreticalAggregation,
                    effectiveAggregation,
                    countPerMonth
                    )
                = await ComputePvSiteAggregateProductionFromSiteData(
                    pvSiteModel,
                    horizonProfileClient,
                    coordinateProvider,
                    horizonControlProvider,
                    evaluationYear: evaluationYear,
                    evaluationStartHour: evaluationStartHour,
                    evaluationEndHour: evaluationEndHour,
                    minutesPerPeriod: minutesPerPeriod,
                    print: print
                    );

            // Final aggregation to monthly and yearly values
            var theoreticalMonth = new List<double[]>();
            var effectiveMonth = new List<double[]>();
            var theoreticalYear = new List<double>();
            var effectiveYear = new List<double>();
            var peakSum = peak.Sum();
            for (var roof = 0; roof <= nrRoofs; roof++)
            {
                var theoreticalSum = Enumerable.Range(0, 13)
                    .Select(month => Enumerable.Range(1, 24)
                        .Sum(hour => theoreticalAggregation[roof, month, hour]) * countPerMonth[month])
                    .ToArray();
                var theoreticalSumYear = Enumerable.Range(1, 12).Sum(month => theoreticalSum[month]);

                var effectiveSum = Enumerable.Range(0, 13)
                    .Select(month => Enumerable.Range(1, 24)
                        .Sum(hour => effectiveAggregation[roof, month, hour]) * countPerMonth[month])
                    .ToArray();
                var effectiveSumYear = Enumerable.Range(1, 12).Sum(month => effectiveSum[month]);

                var peakRoof = roof == 0 ? peakSum : peak[roof - 1];
                theoreticalSum = [..theoreticalSum.Select(value => value * peakRoof)];
                theoreticalSumYear *= peakRoof;
                effectiveSum = [.. effectiveSum.Select(value => value * peakRoof)];
                effectiveSumYear *= peakRoof;

                theoreticalMonth.Add(theoreticalSum);
                theoreticalYear.Add(theoreticalSumYear);
                effectiveMonth.Add(effectiveSum);
                effectiveYear.Add(effectiveSumYear);
            }

            return new SolarProductionAggregateResults(
                SiteId: site,
                Town: town,
                UtcShift: utcShift,
                EvaluationYear: evaluationYear,
                DimensionRoofs: nrRoofs,
                PeakPowerPerRoof: peak,
                TheoreticalAggregation: theoreticalAggregation,
                EffectiveAggregation: effectiveAggregation,
                CountPerMonth: countPerMonth,
                TheoreticalMonth: theoreticalMonth,
                EffectiveMonth: effectiveMonth,
                TheoreticalYear: theoreticalYear,
                EffectiveYear: effectiveYear
                );
        }

        public static async Task<SolarProductionAggregateResults> ComputePvSiteAggregateProductionPerSite(this IPvSiteModel pvSiteModel,
            IHorizonProfileClient horizonProfileClient,
            ISiteCoordinateProvider coordinateProvider,
            ISiteHorizonControlProvider horizonControlProvider,
            int evaluationYear = BasicParametersAndConstants.DefaultYear,
            int evaluationStartHour = 4,
            int evaluationEndHour = 22,
            int minutesPerPeriod = 10,
            bool print = false)
        {
            // Source data
            const int nrOfRoofs = 1;
            const int dimensionRoofs = 1 + nrOfRoofs;
            const int dimensionYear = 13;
            const int dimensionDay = 25;

            // Target data
            var peakPowerRoofs = new double[nrOfRoofs];
            var theoreticalAggregationRoofs = new double[dimensionRoofs, dimensionYear, dimensionDay];
            var effectiveAggregationRoofs = new double[dimensionRoofs, dimensionYear, dimensionDay];
            var aggregateMonthTheoreticalRoofs = new double[dimensionYear];
            var aggregateMonthEffectiveRoofs = new double[dimensionYear];

            var theoreticalYearRoofsList = new List<double>();
            var effectiveYearRoofsList = new List<double>();

            // Fetch production data on a roof level
            var productionPerRoof = await ComputePvSiteAggregateProductionPerRoof(
                    pvSiteModel,
                    horizonProfileClient,
                    coordinateProvider,
                    horizonControlProvider,
                    evaluationYear: evaluationYear,
                    evaluationStartHour: evaluationStartHour,
                    evaluationEndHour: evaluationEndHour,
                    minutesPerPeriod: minutesPerPeriod,
                    print: print
                    );

            var peakPowerPerRoof = productionPerRoof.PeakPowerPerRoof;
            var theoreticalAggregation = productionPerRoof.TheoreticalAggregation;
            var effectiveAggregation = productionPerRoof.EffectiveAggregation;
            var theoreticalMonth = productionPerRoof.TheoreticalMonth;
            var effectiveMonth = productionPerRoof.EffectiveMonth;
            var theoreticalYear = productionPerRoof.TheoreticalYear;
            var effectiveYear = productionPerRoof.EffectiveYear;

            var countSourceRoofs = peakPowerPerRoof.Length;

            // Aggregate to site level: assign to a single notional "roof"
            var peakPowerSum = peakPowerPerRoof.Sum();
            peakPowerRoofs[0] = peakPowerSum > 0 ? peakPowerSum : 1.0;
            var normalizeFactors = new double[1 + countSourceRoofs];
            normalizeFactors[0] = 1.0;
            for (var roofIndex = 0; roofIndex < countSourceRoofs; roofIndex++)
            {
                normalizeFactors[1 + roofIndex] = peakPowerPerRoof[roofIndex] / peakPowerRoofs[0];
            }

            for (var month = 0; month < dimensionYear; month++)
            {
                for (var hour = 0; hour < dimensionDay; hour++)
                {
                    for (var sourceRoof = 1; sourceRoof <= countSourceRoofs; sourceRoof++)
                    {
                        theoreticalAggregationRoofs[1, month, hour] += theoreticalAggregation[sourceRoof, month, hour] * normalizeFactors[sourceRoof];
                        effectiveAggregationRoofs[1, month, hour] += effectiveAggregation[sourceRoof, month, hour] * normalizeFactors[sourceRoof];
                    }
                    theoreticalAggregationRoofs[0, month, hour] = theoreticalAggregationRoofs[1, month, hour];
                    effectiveAggregationRoofs[0, month, hour] = effectiveAggregationRoofs[1, month, hour];
                }
            }

            for (var month = 1; month < dimensionYear; month++)
            {
                for (var roof = 1; roof <= countSourceRoofs; roof++)
                {
                    aggregateMonthTheoreticalRoofs[month] += theoreticalMonth[roof][month];
                    aggregateMonthEffectiveRoofs[month] += effectiveMonth[roof][month];
                }
            }

            return new SolarProductionAggregateResults(
                SiteId: productionPerRoof.SiteId,
                Town: productionPerRoof.Town,
                EvaluationYear: productionPerRoof.EvaluationYear,
                UtcShift: productionPerRoof.UtcShift,
                DimensionRoofs: nrOfRoofs,
                PeakPowerPerRoof: peakPowerRoofs,
                TheoreticalAggregation: theoreticalAggregationRoofs,
                EffectiveAggregation: effectiveAggregationRoofs,
                CountPerMonth: productionPerRoof.CountPerMonth,
                TheoreticalMonth: [aggregateMonthTheoreticalRoofs, aggregateMonthTheoreticalRoofs],
                EffectiveMonth: [aggregateMonthEffectiveRoofs, aggregateMonthEffectiveRoofs],
                TheoreticalYear: [aggregateMonthTheoreticalRoofs.Sum(), aggregateMonthTheoreticalRoofs.Sum()],
                EffectiveYear: [aggregateMonthEffectiveRoofs.Sum(), aggregateMonthEffectiveRoofs.Sum()]
                );
        }
    }
}