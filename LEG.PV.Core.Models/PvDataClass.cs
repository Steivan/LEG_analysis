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
                var (computedPowerRecord, derivatives) = ComputedPower(modelParams, installedPower, periodsPerHour);
                var referencePower = installedPower / periodsPerHour;
                var measuredPower = MeasuredPower ?? 0;

                var unexplainedFractionalLoss = new PvPowerRecord(0);
                if (SolarGeometry.HasIrradiance)
                {
                    // Calibration weight for snow and fog adjustments (implicitly applied to unexplainedFractionalLoss)
                    // f_Snow and f_Fog are relative to PowerGRTW and PowerGRTWS, respectively
                    // UFL is instead evaluated relative to installed power
                    var weight_S = computedPowerRecord.PowerGRTW / referencePower;
                    var weight_SF = computedPowerRecord.PowerGRTWS / referencePower;

                    derivatives = new PvModelParams(
                        etha: derivatives.Etha,
                        gamma: derivatives.Gamma,
                        u0: derivatives.U0,
                        u1: derivatives.U1,
                        lDegr: derivatives.LDegr,
                        lambdadaDSnow: derivatives.LambdaDSnow * weight_S,
                        lambdaAFog: derivatives.LambdaAFog * weight_SF,
                        bFog: derivatives.BFog * weight_SF,
                        lambdaKFog: derivatives.LambdaKFog * weight_SF
                        );
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
                    Derivatives = derivatives,
                    UnexplainedFractionLossRecord = unexplainedFractionalLoss
                };
            }
            public (PvPowerRecord power, PvModelParams derivatives) ComputedPower(                                                // P_computed [W]
                PvModelParams modelParams,
                double installedPower,
                int periodsPerHour)
            {
                var power = new PvPowerRecord(0);
                var derivatives = new PvModelParams(0, 0, 0, 0, 0, 0, 0, 0, 0);

                if (SolarGeometry.HasIrradiance)
                {
                    (power, derivatives) = PvPowerJacobian.PvJacobianFunc(
                        installedPower,
                        periodsPerHour,
                        SolarGeometry,
                        MeteoParameters,
                        Age,
                        modelParams
                        );
                }

                return (power, derivatives);
            }
        }
    }
}
