using LEG.MeteoSwiss.Abstractions.Models;

namespace LEG.PV.Core.Models
{
    public partial class PvDataClass
    {
        public record PvRecord
        {
            public PvRecord(
                DateTime timestamp,
                int index,
                PvSolarGeometry geometryFactors,
                MeteoParameters meteoParameters,
                double weight,
                double age,
                double? measuredPower)
            {
                Timestamp = timestamp;
                Index = index;
                SolarGeometry = geometryFactors;
                MeteoParameters = meteoParameters;
                Weight = weight;
                Age = age;
                MeasuredPower = measuredPower;
            }
            public DateTime Timestamp { get; init; }                                            // Timestamp [YYYY-MM-DD HH:MM:SS]
            public int Index { get; init; }                                                     // Index [unitless]
            public PvSolarGeometry SolarGeometry { get; set; }
            public MeteoParameters MeteoParameters { get; set; }
            public double Weight { get; set; }
            public double Age { get; set; }                                                     // Age [years]
            public double? MeasuredPower { get; init; }                                         // P_meas [W]
            public bool HasMeasuredPower => MeasuredPower.HasValue;
            public PvResidualRecord GetPvResidualsRecord(                                            // P_computed [W]
                PvModelParams modelParams,
                double installedPower,
                int periodsPerHour)
            {
                var computedPowerRecord = ComputedPower(modelParams, installedPower, periodsPerHour);
                var referencePower = installedPower / periodsPerHour;
                var measuredPower = MeasuredPower ?? 0;

                var unexplainedFractionalLoss = new PvPowerRecord(0);
                if (SolarGeometry.HasIrradiance)
                {
                    unexplainedFractionalLoss = new PvPowerRecord(
                        (computedPowerRecord.PowerG - measuredPower) / referencePower,
                        (computedPowerRecord.PowerGR - measuredPower) / referencePower,
                        (computedPowerRecord.PowerGRT - measuredPower) / referencePower,
                        (computedPowerRecord.PowerGRTW - measuredPower) / referencePower,
                        (computedPowerRecord.PowerGRTWS - measuredPower) / referencePower,
                        (computedPowerRecord.PowerGRTWSF - measuredPower) / referencePower
                        );
                }

                return new PvResidualRecord
                {
                    HasCalculated = SolarGeometry.HasIrradiance,
                    HasMeasured = HasMeasuredPower,
                    ComputedPower = computedPowerRecord,
                    UnexplainedFractionLossRecord = unexplainedFractionalLoss
                };
            }
            public PvPowerRecord ComputedPower(                                                // P_computed [W]
                PvModelParams modelParams,
                double installedPower,
                int periodsPerHour)
            {
                if (!SolarGeometry.HasIrradiance)
                {
                    return new PvPowerRecord(0);
                }
                return PvPowerJacobian.EffectiveCellPower(
                    installedPower,
                    periodsPerHour,
                    SolarGeometry,
                    MeteoParameters,
                    Age,
                    modelParams
                    );
            }
        }
    }
}
