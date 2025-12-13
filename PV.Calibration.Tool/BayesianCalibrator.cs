using LEG.PV.Core.Models;
using LEG.PV.Data.Processor;
using MathNet.Numerics.LinearAlgebra;
using static LEG.PV.Core.Models.PvDataClass;
using static LEG.PV.Core.Models.PvPriorConfig;

namespace PV.Calibration.Tool
{
    public class BayesianCalibrator
    {
        // Define the number of parameters being calibrated
        // - GRTW: etha, gamma, u0, u1, lDegr
        // -    S: dSnow
        // -    F: lambdaA, B, lambdaK
        private const int ParameterCount_GRTW = 5;
        private const int ParameterCount_S = 1;
        private const int ParameterCount_F = 3;
        private const int Offset_GRTW = 0;
        private const int Offset_S = Offset_GRTW + ParameterCount_GRTW;
        private const int Offset_F = Offset_S + ParameterCount_S;
        // Estimated variance of measurement noise (Adjust this based on data analysis)
        private const double BaselineCv = 0.005; // => 50W per 10kW standard deviation
        // Snow depth range for calibration purposes
        private const int snowDepthSteps = 10;
        // AFog range for calibration purposes
        private const int aFogSteps = 10;

        private static double[] GetSupport(double minValue, double maxValue, int steps = 10, bool linear = false)
        {
            if (linear)
            {
                var delta = (maxValue - minValue) / (steps - 1);
                return Enumerable.Range(0, steps).Select(i => minValue + i * delta).ToArray();
            }
            else
            {
                var logMin = Math.Log(minValue);
                var logMax = Math.Log(maxValue);
                var delta = (logMax - logMin) / (steps - 1);
                return Enumerable.Range(0, steps).Select(i => Math.Exp(logMin + i * delta)).ToArray();
            }
        }

        // Processing of "filtered" snow records for DSnow calibration
        private static void UpdateErrors_S(double[] support, double[] errors, 
            double snowDepth, 
            double baselinePower, double measuredPower, double weight)
        {
            for (int index = 0; index < support.Length; index++)
            {
                var pS = baselinePower * PvPowerJacobian.GetSnowFactor(snowDepth, support[index]);
                var delta = pS - measuredPower;
                errors[index] += delta * delta * weight;
            }
        }
        private static (double thetaMin_S, double thetaMax_S) Updatetheta_S(double[] support, double[] errors)
        {
            var minError = errors.Min();
            var minIndex = Array.IndexOf(errors, minError);
            var vertex = Math.Log(support[minIndex]);
            var halfWidth = Math.Log(support[1]) - Math.Log(support[0]); // Define range around the vertex

            return (Math.Exp(vertex - halfWidth), Math.Exp(vertex + halfWidth));
        }

        // Processing of "filtered" fog records for LambdaAFog calibration
        private static void UpdateErrors_F(double[] support, double[] errors, 
            double dpd, 
            double bFog, double kFog, 
            double baselinePower, double measuredPower, double weight)
        {
            for (int index = 0; index < support.Length; index++)
            {
                var aFog = 1.0 / (1.0 + Math.Exp(-support[index]));
                var pS = baselinePower * PvPowerJacobian.GetFogFactor(dpd, aFog, bFog, kFog);
                var delta = pS - measuredPower;
                errors[index] += delta * delta * weight;
            }
        }
        private static (double thetaMin_F, double thetaMax_F) Updatetheta_F(double[] support, double[] errors)
        {
            var minError = errors.Min();
            var minIndex = Array.IndexOf(errors, minError);
            var vertex = support[minIndex];
            var halfWidth = support[1] - support[0]; // Define range around the vertex

            return (vertex - halfWidth, vertex + halfWidth);
        }

        public record PvPriors
        {
            public PvModelParams PriorMeans { get; init; } = GetAllPriorsMeans();
            public PvModelParams PriorSigmas { get; init; } = GetAllPriorsSigmas();
        }

        private static PvModelParams ThetaToPvModelParams(
            Vector<double> theta_GRTW, 
            double thetaMin_S, double thetaMax_S,
            double thetaMin_F, double thetaMax_F,
            Vector<double> theta_F, 
            bool aFogFromMinMax = false)
        {
            return new PvModelParams(
                etha: theta_GRTW[0],
                gamma: theta_GRTW[1],
                u0: theta_GRTW[2],
                u1: theta_GRTW[3],
                lDegr: theta_GRTW[4],
                dSnow: Math.Sqrt(thetaMin_S * thetaMax_S),
                lambdaAFog: aFogFromMinMax ? (thetaMin_F + thetaMax_F) / 2.0 : theta_F[0],
                bFog: theta_F[1],
                lambdaKFog: theta_F[2]
                );
        }

        private static (Vector<double> theta_GRTW, Vector<double> theta_F) PvModelParamsToTheta(PvModelParams modelParams)
        {
            return 
                (
                Vector<double>.Build.DenseOfArray(new double[]
                {
                    modelParams.Etha, modelParams.Gamma, modelParams.U0, modelParams.U1, modelParams.LDegr
                }
                ),
                Vector<double>.Build.DenseOfArray(new double[]
                {
                    modelParams.LambdaAFog, modelParams.BFog, modelParams.LambdaKFog
                }
                )
                );
        }

        // --- Core Calibration Method ---
        public static (List<PvModelParams> thetaCalibrated, int iterations, double meanSquaredError) Calibrate(
            List<PvRecord> pvRecords,
            PvPriors pvPriors,
            List<bool>? validRecords,
            double installedPower,
            int periodsPerHour = 6,
            double tolerance = 1e-6,
            int maxIterations = 50)
        {
            // 1. Setup Initial Parameter Vector (theta_0)
            var modelParams = pvPriors.PriorMeans;
            var (theta_GRTW, theta_F) = PvModelParamsToTheta(modelParams);

            // 2. Setup Prior Precision Matrix (Lambda_prior = Sigma_prior^-1)
            // Assuming diagonal covariance (independent priors)

            // 1. Vector of Variances (sigma^2 for each parameter)
            Vector<double> sigma2_GRTW = Vector<double>.Build.DenseOfArray(new double[]
            {
                pvPriors.PriorSigmas.Etha * pvPriors.PriorSigmas.Etha,
                pvPriors.PriorSigmas.Gamma * pvPriors.PriorSigmas.Gamma,
                pvPriors.PriorSigmas.U0 * pvPriors.PriorSigmas.U0,
                pvPriors.PriorSigmas.U1 * pvPriors.PriorSigmas.U1,
                pvPriors.PriorSigmas.LDegr * pvPriors.PriorSigmas.LDegr
            });
            Vector<double> sigma2_F = Vector<double>.Build.DenseOfArray(new double[]
            {
                pvPriors.PriorSigmas.LambdaAFog * pvPriors.PriorSigmas.LambdaAFog,
                pvPriors.PriorSigmas.BFog * pvPriors.PriorSigmas.BFog,
                pvPriors.PriorSigmas.LambdaKFog * pvPriors.PriorSigmas.LambdaKFog
            });

            // 2. Calculate the scaled precision vector (1/sigma^2 * 1/SigmaDataSquared)
            var dataPrecision = 1.0 / Math.Pow(installedPower / periodsPerHour * BaselineCv, 2);
            Vector<double> diagonalValuesVector_GRTW = sigma2_GRTW.Map(x => 1.0 / x).Multiply(dataPrecision);
            Vector<double> diagonalValuesVector_F = sigma2_F.Map(x => 1.0 / x).Multiply(dataPrecision);

            // 3. Convert the Vector<double> to a double array to match the Build.Diagonal signature
            Matrix<double> lambdaPrior_GRTW = Matrix<double>.Build.Diagonal(diagonalValuesVector_GRTW.ToArray());
            Matrix<double> lambdaPrior_F = Matrix<double>.Build.Diagonal(diagonalValuesVector_F.ToArray());

            Vector<double> muPrior_GRTW = Vector<double>.Build.DenseOfArray(new double[]
                { pvPriors.PriorMeans.Etha, pvPriors.PriorMeans.Gamma, pvPriors.PriorMeans.U0, pvPriors.PriorMeans.U1, pvPriors.PriorMeans.LDegr });
            Vector<double> muPrior_F = Vector<double>.Build.DenseOfArray(new double[]
                { pvPriors.PriorMeans.LambdaAFog, pvPriors.PriorMeans.BFog, pvPriors.PriorMeans.LambdaKFog });

            int nrRecords = pvRecords.Count;
            bool applyDataFilter = validRecords != null && validRecords.Count == nrRecords;
            var thetaCalibratedList = new List<PvModelParams>(); 

            var (_, _, mintheta_S, maxtheta_S) = GetPriorsDSnow();
            var (_, _, mintheta_F, maxtheta_F) = GetPriorsLambdaAFog();

            int iterations = 0;
            for (int k = 0; k < maxIterations; k++)
            {
                // Step 1: GRTW Parameters
                // =======================

                // Unpack current parameters
                modelParams = ThetaToPvModelParams(theta_GRTW, mintheta_S, maxtheta_S, mintheta_F, maxtheta_F, theta_F, aFogFromMinMax: false);

                // 3. Build Jacobian (J) and Residual Vector (r = Y - P_eff)
                Matrix<double> J_GRTW = Matrix<double>.Build.Dense(nrRecords, ParameterCount_GRTW);
                Vector<double> Y_GRTW = Vector<double>.Build.Dense(nrRecords);
                Vector<double> Peff_Model_GRTW = Vector<double>.Build.Dense(nrRecords);

                for (int i = 0; i < nrRecords; i++)
                {
                    if (applyDataFilter && !validRecords![i])
                        continue;

                    var pvRecord = pvRecords[i];

                    // Weighting (if applicable)
                    var (weightR, weightS, weightF) = pvRecord.MeteoDataRecord.GetWeightsRSW(pvRecord.SolarGeometry.SinSunElevation);
                    var weight_GRTW = weightR * (pvRecord.HasMeasuredPower ? Math.Sqrt(pvRecord.Weight) : 0.0);

                    // Power, Derivatives and Residual Vector r
                    var recordValues = pvRecord.GetPvResidualsRecord(modelParams, installedPower, periodsPerHour);
                    var calculated = recordValues.HasCalculated;
                    var measured = recordValues.HasMeasured;

                    if (!calculated || !measured)
                        continue;

                    var powerRecord = recordValues.ComputedPower;
                    var derivativesRecord = recordValues.Derivatives;
                    var unexplainedFractionLossRecord = recordValues.UnexplainedFractionLossRecord;
                    var derivativeAdjustmentFactor_GRTW = powerRecord.PowerGRTW > 0 ? powerRecord.PowerGRTWSF / powerRecord.PowerGRTW : 1.0;

                    Y_GRTW[i] = pvRecord.HasMeasuredPower ? pvRecord.MeasuredPower.Value * weight_GRTW : 0.0;
                    Peff_Model_GRTW[i] = powerRecord.PowerGRTWSF * weight_GRTW;

                    // Jacobian Matrix J
                    J_GRTW[i, 0] = derivativesRecord.Etha * derivativeAdjustmentFactor_GRTW * weight_GRTW;
                    J_GRTW[i, 1] = derivativesRecord.Gamma * derivativeAdjustmentFactor_GRTW * weight_GRTW;
                    J_GRTW[i, 2] = derivativesRecord.U0 * derivativeAdjustmentFactor_GRTW * weight_GRTW;
                    J_GRTW[i, 3] = derivativesRecord.U1 * derivativeAdjustmentFactor_GRTW * weight_GRTW;
                    J_GRTW[i, 4] = derivativesRecord.LDegr * derivativeAdjustmentFactor_GRTW * weight_GRTW;
                }

                Vector<double> residual_GRTW = Y_GRTW.Subtract(Peff_Model_GRTW);

                // 4. Form the Penalized Normal Equation components: M * Delta_theta = b
                // M = J^T * J + Lambda_prior
                Matrix<double> JTJ_GRTW = J_GRTW.Transpose() * J_GRTW;
                Matrix<double> M_GRTW = JTJ_GRTW.Add(lambdaPrior_GRTW);

                // b = J^T * r - Lambda_prior * (theta_k - mu_prior)
                Vector<double> JT_r_GRTW = J_GRTW.Transpose() * residual_GRTW;
                Vector<double> prior_penalty_GRTW = lambdaPrior_GRTW * (theta_GRTW.Subtract(muPrior_GRTW));
                Vector<double> b_GRTW = JT_r_GRTW.Subtract(prior_penalty_GRTW);

                // 5. Solve for Delta_theta
                Vector<double> deltaTheta_GRTW = M_GRTW.Solve(b_GRTW);

                // 6. Update Parameters if not NaN
                if (!double.IsNaN(deltaTheta_GRTW.Sum()))
                {
                    theta_GRTW = theta_GRTW.Add(deltaTheta_GRTW);
                }

                // 7. Enforce Hard Physical Constraints (Clamping/Projection)
                ClampParameters_GRTW(ref theta_GRTW);

                // Step 2: DSnow and AFog Parameters
                // =================================

                // Unpack current parameters
                modelParams = ThetaToPvModelParams(theta_GRTW, mintheta_S, maxtheta_S, mintheta_F, maxtheta_F, theta_F, aFogFromMinMax: true);

                var support_S = GetSupport(mintheta_S, maxtheta_S, steps: snowDepthSteps, linear: false);   // DSnow is confined to positive values
                var errors_S = support_S.Select(s => 0.0).ToArray();

                var support_F = GetSupport(mintheta_F, maxtheta_F, steps: aFogSteps, linear: true);         // AFog is confined to (0,1) => LambdaAFog is on a linear scale
                var errors_F = support_F.Select(s => 0.0).ToArray();

                for (int i = 0; i < nrRecords; i++)
                {
                    if (applyDataFilter && !validRecords![i])
                        continue;

                    var pvRecord = pvRecords[i];

                    // Weighting (if applicable)
                    var (weightR, weightS, weightF) = pvRecord.MeteoDataRecord.GetWeightsRSW(pvRecord.SolarGeometry.SinSunElevation);
                    var weight_SF = pvRecord.HasMeasuredPower ? 1.0 : 0.0;
                    var weight_S = weightS * weight_SF;
                    var weight_F = weightF * weight_SF;

                    // Power, Derivatives and Residual Vector r
                    var recordValues = pvRecord.GetPvResidualsRecord(modelParams, installedPower, periodsPerHour);
                    var calculated = recordValues.HasCalculated;
                    var measured = recordValues.HasMeasured;

                    if (!calculated || !measured)
                        continue;

                    var powerRecord = recordValues.ComputedPower;
                    //var derivativesRecord = recordValues.Derivatives;
                    //var unexplainedFractionLossRecord = recordValues.UnexplainedFractionLossRecord;
                    var measuredPower = pvRecord.MeasuredPower.Value;

                    var snowDepth = pvRecord.MeteoDataRecord.SnowDepth.Value;
                    if (weight_S > 0.0 && snowDepth > 0.0)
                    {
                        UpdateErrors_S(support_S, errors_S, snowDepth, powerRecord.PowerGRTW, measuredPower, weight_S);
                    }
                    var dpd = pvRecord.MeteoDataRecord.Temperature - pvRecord.MeteoDataRecord.DewPoint;
                    if (weight_S > 0.0 && snowDepth > 0.0)
                    {
                        UpdateErrors_F(support_F, errors_F, dpd?? 5.0, modelParams.BFog, modelParams.KFog, powerRecord.PowerGRTW, measuredPower, weight_F);
                    }
                }

                (mintheta_S, maxtheta_S) = Updatetheta_S(support_S, errors_S);
                (mintheta_F, maxtheta_F) = Updatetheta_F(support_F, errors_F);

                // Step 3: Fog Parameters
                // ======================

                // Unpack current parameters
                modelParams = ThetaToPvModelParams(theta_GRTW, mintheta_S, maxtheta_S, mintheta_F, maxtheta_F, theta_F, aFogFromMinMax: true);
                (_, theta_F) = PvModelParamsToTheta(modelParams);

                Matrix<double> J_F = Matrix<double>.Build.Dense(nrRecords, ParameterCount_F);
                Vector<double> Y_F = Vector<double>.Build.Dense(nrRecords);
                Vector<double> Peff_Model_F = Vector<double>.Build.Dense(nrRecords);

                for (int i = 0; i < nrRecords; i++)
                {
                    if (applyDataFilter && !validRecords![i])
                        continue;

                    var pvRecord = pvRecords[i];

                    // Weighting (if applicable)
                    var (weightR, weightS, weightF) = pvRecord.MeteoDataRecord.GetWeightsRSW(pvRecord.SolarGeometry.SinSunElevation);
                    var weight_SF = pvRecord.HasMeasuredPower ? 1.0 : 0.0;
                    var weight_F = weightF * weight_SF;

                    if (weightF > 0.2)
                    {
                    }

                    // Power, Derivatives and Residual Vector r
                    var recordValues = pvRecord.GetPvResidualsRecord(modelParams, installedPower, periodsPerHour);
                    var calculated = recordValues.HasCalculated;
                    var measured = recordValues.HasMeasured;

                    if (!calculated || !measured)
                        continue;

                    var powerRecord = recordValues.ComputedPower;
                    var derivativesRecord = recordValues.Derivatives;
                    var unexplainedFractionLossRecord = recordValues.UnexplainedFractionLossRecord;

                    Y_F[i] = 0.0 * weight_F;   // Target is zero unexplained loss 
                    Peff_Model_F[i] = unexplainedFractionLossRecord.PowerGRTWSF * weight_F;

                    J_F[i, 0] = derivativesRecord.LambdaAFog * weight_F;
                    J_F[i, 1] = derivativesRecord.BFog * weight_F;
                    J_F[i, 2] = derivativesRecord.LambdaKFog * weight_F;
                }

                Vector<double> residual_F = Y_F.Subtract(Peff_Model_F);              // Remark: Y_F = 0

                Matrix<double> JTJ_F = J_F.Transpose() * J_F;
                Matrix<double> M_F = JTJ_F.Add(lambdaPrior_F);

                Vector<double> JT_r_F = J_F.Transpose() * residual_F;
                Vector<double> prior_penalty_F = lambdaPrior_F * (theta_F.Subtract(muPrior_F));
                Vector<double> b_F = JT_r_F.Subtract(prior_penalty_F);

                // 5. Solve for Delta_theta
                Vector<double> deltaTheta_F = M_F.Solve(b_F);

                // 6. Update Parameters if not NaN
                if (!double.IsNaN(deltaTheta_F.Sum()))
                {
                    theta_F = theta_F.Add(deltaTheta_F);
                }
                mintheta_F = Math.Min(mintheta_F, mintheta_F - deltaTheta_F[0]);
                maxtheta_F = Math.Max(maxtheta_F, maxtheta_F + deltaTheta_F[0]);

                // 7. Enforce Hard Physical Constraints (Clamping/Projection)
                ClampParameters_F(ref theta_F);

                // Store calibrated parameters for this iteration
                thetaCalibratedList.Add(ThetaToPvModelParams(theta_GRTW, mintheta_S, maxtheta_S, mintheta_F, maxtheta_F, theta_F, aFogFromMinMax: false));

                // Check for convergence before update
                iterations++;
                if (deltaTheta_GRTW.L2Norm() + (maxtheta_S - mintheta_S) + deltaTheta_F.L2Norm() < tolerance)
                {
                    System.Console.WriteLine($"Converged after {k + 1} iterations.");
                    break;
                }
            }

            // Final calibrated parameters
            var meanSquaredError = PvErrorStatistics.ComputeMeanError(
            pvRecords,
            validRecords,
            installedPower,
            periodsPerHour,
            thetaCalibratedList[^1]
            );

            return (thetaCalibratedList, iterations, meanSquaredError);
        }

        // --- Helper Method for Clamping ---
        private static void ClampParameters_GRTW(ref Vector<double> theta)
        {
            for (int i = 0; i < theta.Count; i++)
            {
                theta[i] = Math.Min(GetPriorMax(i), Math.Max(GetPriorMin(Offset_GRTW + i), theta[i]));
            }
        }
        private static void ClampParameters_F(ref Vector<double> theta)
        {
            for (int i = 0; i < theta.Count; i++)
            {
                var paramIndex = Offset_F + i;
                theta[i] = Math.Min(GetPriorMax(paramIndex), Math.Max(GetPriorMin(paramIndex), theta[i]));
            }
        }
    }
}
