using LEG.MeteoSwiss.Abstractions.Models;
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
        // SF: lambdaA, B, lambdaK
        private const int Offset_GRTW = 0;
        private const int ParameterCount_GRTW = 5;
        private const int Offset_SF = ParameterCount_GRTW + 1;
        private const int ParameterCount_SF = 3;
        // Estimated variance of measurement noise (Adjust this based on data analysis)
        private const double BaselineCv = 0.005; // => 50W per 10kW standard deviation

        // Delegate matching the required Jacobian function signature
        public delegate (PvPowerRecord powerRecord, PvModelParams paramDerivatives) JacobianFunc(
            double installedPower, int periodsPerHour, 
            PvSolarGeometry geometryFactors,
            MeteoParameters meteoParameters,
            double age,
            PvModelParams modelParams);

        public record PvPriors
        {
            public PvModelParams PriorMeans { get; init; } = GetAllPriorsMeans();
            public PvModelParams PriorSigmas { get; init; } = GetAllPriorsSigmas();
        }

        private static PvModelParams ThetaToPvModelParams(Vector<double> theta_GRTW, Vector<double> theta_SF, PvModelParams defaultParams)
        {
            return new PvModelParams(
                etha: theta_GRTW[0],
                gamma: theta_GRTW[1],
                u0: theta_GRTW[2],
                u1: theta_GRTW[3],
                lDegr: theta_GRTW[4],
                lambdaDSnow: defaultParams.LambdaDSnow,
                lambdaAFog: theta_SF[0],
                bFog: theta_SF[1],
                lambdaKFog: theta_SF[2]
                );
        }

        private static (Vector<double> theta_GRTW, Vector<double> theta_SF) PvModelParamsToTheta(PvModelParams modelParams)
        {
            return ( 
                Vector<double>.Build.DenseOfArray(new double[]
                {
                    modelParams.Etha, modelParams.Gamma, modelParams.U0, modelParams.U1, modelParams.LDegr
                }),
                Vector<double>.Build.DenseOfArray(new double[]
                {
                    modelParams.LambdaAFog, modelParams.BFog, modelParams.LambdaKFog
                })
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
            var (theta_GRTW, theta_SF) = PvModelParamsToTheta(modelParams);

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
            Vector<double> sigma2_SF = Vector<double>.Build.DenseOfArray(new double[]
            {
                pvPriors.PriorSigmas.LambdaAFog * pvPriors.PriorSigmas.LambdaAFog,
                pvPriors.PriorSigmas.BFog * pvPriors.PriorSigmas.BFog,
                pvPriors.PriorSigmas.LambdaKFog * pvPriors.PriorSigmas.LambdaKFog
            });

            // 2. Calculate the scaled precision vector (1/sigma^2 * 1/SigmaDataSquared)
            var dataPrecision = 1.0 / Math.Pow(installedPower * BaselineCv, 2);
            Vector<double> diagonalValuesVector_GRTW = sigma2_GRTW.Map(x => 1.0 / x).Multiply(dataPrecision);
            Vector<double> diagonalValuesVector_SF = sigma2_SF.Map(x => 1.0 / x).Multiply(dataPrecision);

            // 3. Convert the Vector<double> to a double array to match the Build.Diagonal signature
            Matrix<double> lambdaPrior_GRTW = Matrix<double>.Build.Diagonal(diagonalValuesVector_GRTW.ToArray());
            Matrix<double> lambdaPrior_SF = Matrix<double>.Build.Diagonal(diagonalValuesVector_SF.ToArray());

            Vector<double> muPrior_GRTW = Vector<double>.Build.DenseOfArray(new double[]
                { pvPriors.PriorMeans.Etha, pvPriors.PriorMeans.Gamma, pvPriors.PriorMeans.U0, pvPriors.PriorMeans.U1, pvPriors.PriorMeans.LDegr });
            Vector<double> muPrior_SF = Vector<double>.Build.DenseOfArray(new double[]
                { pvPriors.PriorMeans.LambdaAFog, pvPriors.PriorMeans.BFog, pvPriors.PriorMeans.LambdaKFog });

            int nrRecords = pvRecords.Count;
            bool applyDataFilter = validRecords != null && validRecords.Count == nrRecords;
            var thetaCalibratedList = new List<PvModelParams>(); 
            int iterations = 0;
            for (int k = 0; k < maxIterations; k++)
            {
                // Unpack current parameters
                modelParams = ThetaToPvModelParams(theta_GRTW, theta_SF, modelParams);

                // 3. Build Jacobian (J) and Residual Vector (r = Y - P_eff)
                Matrix<double> J_GRTW = Matrix<double>.Build.Dense(nrRecords, ParameterCount_GRTW);
                Vector<double> Y_GRTW = Vector<double>.Build.Dense(nrRecords);
                Vector<double> Peff_Model_GRTW = Vector<double>.Build.Dense(nrRecords);

                Matrix<double> J_SF = Matrix<double>.Build.Dense(nrRecords, ParameterCount_SF);
                Vector<double> Y_SF = Vector<double>.Build.Dense(nrRecords);
                Vector<double> Peff_Model_SF = Vector<double>.Build.Dense(nrRecords);

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

                    if (pvRecord.Weight <= 0.0)
                    {
                        var DEBUG_1 = 0;
                    }
                    // Weighting (if applicable)
                    var weight_GRTW = pvRecord.HasMeasuredPower ? Math.Sqrt(pvRecord.Weight) : 0.0;
                    var weight_SF = pvRecord.HasMeasuredPower ? 1.0 : 0.0;

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

                    if (double.IsNaN(powerRecord.PowerGRTW))
                    {
                        var DEBUG_2 = 0;
                    }

                    Y_GRTW[i] = pvRecord.HasMeasuredPower ? pvRecord.MeasuredPower.Value * weight_GRTW : 0.0; 
                    Peff_Model_GRTW[i] = powerRecord.PowerGRTWSF * weight_GRTW;

                    Y_SF[i] = 0.0 * weight_SF;   // Target is zero unexplained loss 
                    Peff_Model_SF[i] = unexplainedFractionLossRecord.PowerGRTWSF * weight_SF;

                    // Jacobian Matrix J
                    J_GRTW[i, 0] = derivativesRecord.Etha * derivativeAdjustmentFactor_GRTW * weight_GRTW;
                    J_GRTW[i, 1] = derivativesRecord.Gamma * derivativeAdjustmentFactor_GRTW * weight_GRTW;
                    J_GRTW[i, 2] = derivativesRecord.U0 * derivativeAdjustmentFactor_GRTW * weight_GRTW;
                    J_GRTW[i, 3] = derivativesRecord.U1 * derivativeAdjustmentFactor_GRTW * weight_GRTW;
                    J_GRTW[i, 4] = derivativesRecord.LDegr * derivativeAdjustmentFactor_GRTW * weight_GRTW;

                    J_SF[i, 0] = derivativesRecord.LambdaAFog * weight_SF;
                    J_SF[i, 1] = derivativesRecord.BFog * weight_SF;
                    J_SF[i, 2] = derivativesRecord.LambdaKFog * weight_SF;

                    // For Debugging Purposes
                    DEBUG_maxF = Math.Max(DEBUG_maxF, derivativeAdjustmentFactor_GRTW);
                    DEBUG_minF = Math.Min(DEBUG_minF, derivativeAdjustmentFactor_GRTW);
                    DEBUG_maxW = Math.Max(DEBUG_maxW, weight_GRTW);
                    DEBUG_minW = Math.Min(DEBUG_minW, weight_GRTW);

                    DEBUG_maxU = Math.Max(DEBUG_maxU, Peff_Model_SF[i]);
                    DEBUG_minU = Math.Min(DEBUG_minU, Peff_Model_SF[i]);
                    DEBUG_maxA = Math.Max(DEBUG_maxA, J_SF[i, 0]);
                    DEBUG_minA = Math.Min(DEBUG_minA, J_SF[i, 0]);
                    DEBUG_maxB = Math.Max(DEBUG_maxB, J_SF[i, 1]);
                    DEBUG_minB = Math.Min(DEBUG_minB, J_SF[i, 1]);
                    DEBUG_maxK = Math.Max(DEBUG_maxK, J_SF[i, 2]);
                    DEBUG_minK = Math.Min(DEBUG_minK, J_SF[i, 2]);
                }

                Vector<double> residual_GRTW = Y_GRTW.Subtract(Peff_Model_GRTW);
                Vector<double> residual_SF = Y_SF.Subtract(Peff_Model_SF);              // Remark: Y_SF = 0

                // 4. Form the Penalized Normal Equation components: M * Delta_theta = b
                // M = J^T * J + Lambda_prior
                Matrix<double> JTJ_GRTW = J_GRTW.Transpose() * J_GRTW;
                Matrix<double> M_GRTW = JTJ_GRTW.Add(lambdaPrior_GRTW);

                Matrix<double> JTJ_SF = J_SF.Transpose() * J_SF;
                Matrix<double> M_SF = JTJ_SF.Add(lambdaPrior_SF);

                // b = J^T * r - Lambda_prior * (theta_k - mu_prior)
                Vector<double> JT_r_GRTW = J_GRTW.Transpose() * residual_GRTW;
                Vector<double> prior_penalty_GRTW = lambdaPrior_GRTW * (theta_GRTW.Subtract(muPrior_GRTW));
                Vector<double> b_GRTW = JT_r_GRTW.Subtract(prior_penalty_GRTW);

                Vector<double> JT_r_SF = J_SF.Transpose() * residual_SF;
                Vector<double> prior_penalty_SF = lambdaPrior_SF * (theta_SF.Subtract(muPrior_SF));
                Vector<double> b_SF = JT_r_SF.Subtract(prior_penalty_SF);

                // 5. Solve for Delta_theta
                Vector<double> deltaTheta_GRTW = M_GRTW.Solve(b_GRTW);
                Vector<double> deltaTheta_SF = M_SF.Solve(b_SF);

                // 6. Update Parameters
                theta_GRTW = theta_GRTW.Add(deltaTheta_GRTW);
                theta_SF = theta_SF.Add(deltaTheta_SF);

                // 7. Enforce Hard Physical Constraints (Clamping/Projection)
                ClampParameters_GRTW(ref theta_GRTW);
                ClampParameters_SF(ref theta_SF);

                // Store calibrated parameters for this iteration
                thetaCalibratedList.Add(ThetaToPvModelParams(theta_GRTW, theta_SF, modelParams));

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
        private static void ClampParameters_SF(ref Vector<double> theta)
        {
            for (int i = 0; i < theta.Count; i++)
            {
                var paramIndex = Offset_SF + i;
                theta[i] = Math.Min(GetPriorMax(paramIndex), Math.Max(GetPriorMin(paramIndex), theta[i]));
            }
        }
    }
}
