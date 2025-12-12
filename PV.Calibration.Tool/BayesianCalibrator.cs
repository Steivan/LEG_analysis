using LEG.PV.Core.Models;
using LEG.PV.Data.Processor;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using static LEG.PV.Core.Models.PvDataClass;
using static LEG.PV.Core.Models.PvPriorConfig;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        // Snow depth ratios and maximum snow depth for calibration purposes
        private const double minSnowDepth = 1.0;
        private const double maxSnowDepth = 100.0;
        private const int snowDepthSteps = 10;

        private static double[] GetSnowDepth(double minSnowDepth, double maxSnowDepth)
        {
            var logMin = Math.Log(minSnowDepth);
            var logMax = Math.Log(maxSnowDepth);
            var delta = (logMax - logMin) / (snowDepthSteps - 1);
            return Enumerable.Range(0, snowDepthSteps).Select(i => Math.Exp(logMin + i * delta)).ToArray();
        }

        private static void UpdateErrors_S(double[] support, double[] errors, double snowDepth, double baselinePower, double measuredPower, double weight)
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

        public record PvPriors
        {
            public PvModelParams PriorMeans { get; init; } = GetAllPriorsMeans();
            public PvModelParams PriorSigmas { get; init; } = GetAllPriorsSigmas();
        }

        private static PvModelParams ThetaToPvModelParams(
            Vector<double> theta_GRTW, 
            double thetaMin_S, double thetaMax_S, 
            Vector<double> theta_F, PvModelParams defaultParams)
        {
            return new PvModelParams(
                etha: theta_GRTW[0],
                gamma: theta_GRTW[1],
                u0: theta_GRTW[2],
                u1: theta_GRTW[3],
                lDegr: theta_GRTW[4],
                dSnow: (thetaMin_S + thetaMax_S) / 2.0,
                lambdaAFog: theta_F[0],
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
            var(mintheta_S, maxtheta_S) = (minSnowDepth, maxSnowDepth);
            int iterations = 0;
            for (int k = 0; k < maxIterations; k++)
            {
                // Unpack current parameters
                modelParams = ThetaToPvModelParams(theta_GRTW, mintheta_S, maxtheta_S, theta_F, modelParams);

                // 3. Build Jacobian (J) and Residual Vector (r = Y - P_eff)
                Matrix<double> J_GRTW = Matrix<double>.Build.Dense(nrRecords, ParameterCount_GRTW);
                Vector<double> Y_GRTW = Vector<double>.Build.Dense(nrRecords);
                Vector<double> Peff_Model_GRTW = Vector<double>.Build.Dense(nrRecords);

                var support_S = GetSnowDepth(mintheta_S, maxtheta_S);
                var errors_S = support_S.Select(s => 0.0).ToArray();

                Matrix<double> J_F = Matrix<double>.Build.Dense(nrRecords, ParameterCount_F);
                Vector<double> Y_F = Vector<double>.Build.Dense(nrRecords);
                Vector<double> Peff_Model_F = Vector<double>.Build.Dense(nrRecords);

                // For Debugging Purposes
                var DEBUG_maxF = double.MinValue;
                var DEBUG_minF = double.MaxValue;
                var DEBUG_maxW = double.MinValue;
                var DEBUG_minW = double.MaxValue;

                var DEBUG_maxU = double.MinValue;
                var DEBUG_minU = double.MaxValue;
                var DEBUG_maxA = double.MinValue;
                var DEBUG_minA = double.MaxValue;
                var DEBUG_maxB = double.MinValue;
                var DEBUG_minB = double.MaxValue;
                var DEBUG_maxK = double.MinValue;
                var DEBUG_minK = double.MaxValue;

                for (int i = 0; i < nrRecords; i++)
                { 
                    if (applyDataFilter && !validRecords![i])
                        continue;

                    var pvRecord = pvRecords[i];
                    // Call the user's provided Jacobian function => obtained via pvRecord.GetPvResidualsRecord(...)

                    // Weighting (if applicable)
                    var (weightR, weightS, weightF) = pvRecord.MeteoDataRecord.GetWeightsRSW(pvRecord.SolarGeometry.SinSunElevation);
                    var weight_GRTW = weightR *(pvRecord.HasMeasuredPower ? Math.Sqrt(pvRecord.Weight) : 0.0);
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
                    var derivativesRecord = recordValues.Derivatives;
                    var unexplainedFractionLossRecord = recordValues.UnexplainedFractionLossRecord;
                    var derivativeAdjustmentFactor_GRTW = powerRecord.PowerGRTW > 0 ? powerRecord.PowerGRTWSF / powerRecord.PowerGRTW : 1.0;

                    Y_GRTW[i] = pvRecord.HasMeasuredPower ? pvRecord.MeasuredPower.Value * weight_GRTW : 0.0; 
                    Peff_Model_GRTW[i] = powerRecord.PowerGRTWSF * weight_GRTW;

                    var snowDepth = pvRecord.MeteoDataRecord.SnowDepth.Value;
                    if (weight_S > 0.0 && snowDepth > 0.0)
                    {
                        UpdateErrors_S(support_S, errors_S, snowDepth, powerRecord.PowerGRTW, pvRecord.MeasuredPower.Value, weight_S);
                    }

                    Y_F[i] = 0.0 * weight_F;   // Target is zero unexplained loss 
                    Peff_Model_F[i] = unexplainedFractionLossRecord.PowerGRTWSF * weight_F;

                    // Jacobian Matrix J
                    J_GRTW[i, 0] = derivativesRecord.Etha * derivativeAdjustmentFactor_GRTW * weight_GRTW;
                    J_GRTW[i, 1] = derivativesRecord.Gamma * derivativeAdjustmentFactor_GRTW * weight_GRTW;
                    J_GRTW[i, 2] = derivativesRecord.U0 * derivativeAdjustmentFactor_GRTW * weight_GRTW;
                    J_GRTW[i, 3] = derivativesRecord.U1 * derivativeAdjustmentFactor_GRTW * weight_GRTW;
                    J_GRTW[i, 4] = derivativesRecord.LDegr * derivativeAdjustmentFactor_GRTW * weight_GRTW;

                    J_F[i, 0] = derivativesRecord.LambdaAFog * weight_F;
                    J_F[i, 1] = derivativesRecord.BFog * weight_F;
                    J_F[i, 2] = derivativesRecord.LambdaKFog * weight_F;

                    // For Debugging Purposes
                    DEBUG_maxF = Math.Max(DEBUG_maxF, derivativeAdjustmentFactor_GRTW);
                    DEBUG_minF = Math.Min(DEBUG_minF, derivativeAdjustmentFactor_GRTW);
                    DEBUG_maxW = Math.Max(DEBUG_maxW, weight_GRTW);
                    DEBUG_minW = Math.Min(DEBUG_minW, weight_GRTW);

                    DEBUG_maxU = Math.Max(DEBUG_maxU, Peff_Model_F[i]);
                    DEBUG_minU = Math.Min(DEBUG_minU, Peff_Model_F[i]);
                    DEBUG_maxA = Math.Max(DEBUG_maxA, J_F[i, 0]);
                    DEBUG_minA = Math.Min(DEBUG_minA, J_F[i, 0]);
                    DEBUG_maxB = Math.Max(DEBUG_maxB, J_F[i, 1]);
                    DEBUG_minB = Math.Min(DEBUG_minB, J_F[i, 1]);
                    DEBUG_maxK = Math.Max(DEBUG_maxK, J_F[i, 2]);
                    DEBUG_minK = Math.Min(DEBUG_minK, J_F[i, 2]);
                }

                Vector<double> residual_GRTW = Y_GRTW.Subtract(Peff_Model_GRTW);
                Vector<double> residual_F = Y_F.Subtract(Peff_Model_F);              // Remark: Y_F = 0

                // 4. Form the Penalized Normal Equation components: M * Delta_theta = b
                // M = J^T * J + Lambda_prior
                Matrix<double> JTJ_GRTW = J_GRTW.Transpose() * J_GRTW;
                Matrix<double> M_GRTW = JTJ_GRTW.Add(lambdaPrior_GRTW);

                Matrix<double> JTJ_F = J_F.Transpose() * J_F;
                Matrix<double> M_F = JTJ_F.Add(lambdaPrior_F);

                // b = J^T * r - Lambda_prior * (theta_k - mu_prior)
                Vector<double> JT_r_GRTW = J_GRTW.Transpose() * residual_GRTW;
                Vector<double> prior_penalty_GRTW = lambdaPrior_GRTW * (theta_GRTW.Subtract(muPrior_GRTW));
                Vector<double> b_GRTW = JT_r_GRTW.Subtract(prior_penalty_GRTW);

                Vector<double> JT_r_F = J_F.Transpose() * residual_F;
                Vector<double> prior_penalty_F = lambdaPrior_F * (theta_F.Subtract(muPrior_F));
                Vector<double> b_F = JT_r_F.Subtract(prior_penalty_F);

                // 5. Solve for Delta_theta
                Vector<double> deltaTheta_GRTW = M_GRTW.Solve(b_GRTW);

                (mintheta_S, maxtheta_S) = Updatetheta_S(support_S, errors_S);
                //mintheta_S = Math.Max(minSnowDepth, Math.Min(maxSnowDepth, mintheta_S));
                //maxtheta_S = Math.Max(minSnowDepth, Math.Min(maxSnowDepth, maxtheta_S));

                Vector<double> deltaTheta_F = M_F.Solve(b_F);

                // 6. Update Parameters if not NaN
                if (!double.IsNaN(deltaTheta_GRTW.Sum()))
                {
                    theta_GRTW = theta_GRTW.Add(deltaTheta_GRTW);
                }
                if (!double.IsNaN(deltaTheta_F.Sum()))
                {
                    theta_F = theta_F.Add(deltaTheta_F);
                }

                // 7. Enforce Hard Physical Constraints (Clamping/Projection)
                ClampParameters_GRTW(ref theta_GRTW);
                ClampParameters_F(ref theta_F);

                // Store calibrated parameters for this iteration
                thetaCalibratedList.Add(ThetaToPvModelParams(theta_GRTW, mintheta_S, maxtheta_S, theta_F, modelParams));

                // Check for convergence before update
                iterations++;
                if (deltaTheta_GRTW.L2Norm() < tolerance)
                {
                    System.Console.WriteLine($"Converged after {k + 1} iterations.");
                    break;
                }
            }

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
