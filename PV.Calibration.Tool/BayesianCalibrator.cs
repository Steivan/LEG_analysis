using LEG.PV.Data.Processor;
using MathNet.Numerics.LinearAlgebra;

using static LEG.PV.Core.Models.PvPriorConfig;
using static LEG.PV.Core.Models.DataRecords;
using LEG.MeteoSwiss.Abstractions.Models;

namespace PV.Calibration.Tool
{
    public class BayesianCalibrator
    {
        // Define the number of parameters being calibrated (etha, gamma, u0, u1, lDegr)
        private const int ParameterCount = 5;
        // Estimated variance of measurement noise (Adjust this based on data analysis)
        private const double SigmaDataSquared = 50.0 * 50.0; // e.g., 50W standard deviation

        // Delegate matching the required Jacobian function signature
        // NOTE: The geometryFactor (GPOA/Gref) is implicitly included in the inputs.
        public delegate (double Peff, double d_etha, double d_gamma, double d_u0, double d_u1, double d_lDegr) JacobianFunc(
            double installedPower, int periodsPerHour, 
            double directGeometryFactor, double diffuseGeometryFactor, double sinSunElevation,
            MeteoParameters meteoParameters,
            double age,
            PvModelParams modelParams);

        public record PvPriors
        {
            public double EthaSysMean { get; init; } = GetPriorMean(0);
            public double EthaSysStdDev { get; init; } = GetPriorSigma(0);
            public double GammaMean { get; init; } = GetPriorMean(1);
            public double GammaStdDev { get; init; } = GetPriorSigma(1);
            public double U0Mean { get; init; } = GetPriorMean(2);
            public double U0StdDev { get; init; } = GetPriorSigma(2);
            public double U1Mean { get; init; } = GetPriorMean(3);
            public double U1StdDev { get; init; } = GetPriorSigma(3);
            public double LDegrMean { get; init; } = GetPriorMean(4);
            public double LDegrStdDev { get; init; } = GetPriorSigma(4);
        }

        // --- Core Calibration Method ---
        public static (List<PvModelParams> thetaCalibrated, int iterations, double meanSquaredError) Calibrate(
            List<PvRecord> pvRecords,
            PvPriors pvPriors,
            JacobianFunc jacobianFunc,
            List<bool>? validRecords = null,
            double installedPower = 10.0,
            int periodsPerHour = 6,
            double tolerance = 1e-6,
            int maxIterations = 50)
        {
            // 1. Setup Initial Parameter Vector (theta_0)
            Vector<double> theta = Vector<double>.Build.DenseOfArray(new double[]
            {
                pvPriors.EthaSysMean, pvPriors.GammaMean, pvPriors.U0Mean, pvPriors.U1Mean, pvPriors.LDegrMean
            });

            // 2. Setup Prior Precision Matrix (Lambda_prior = Sigma_prior^-1)
            // Assuming diagonal covariance (independent priors)

            // 1. Vector of Variances (sigma^2 for each parameter)
            Vector<double> sigma2 = Vector<double>.Build.DenseOfArray(new double[]
            {
                pvPriors.EthaSysStdDev * pvPriors.EthaSysStdDev,
                pvPriors.GammaStdDev * pvPriors.GammaStdDev,
                pvPriors.U0StdDev * pvPriors.U0StdDev,
                pvPriors.U1StdDev * pvPriors.U1StdDev,
                pvPriors.LDegrStdDev * pvPriors.LDegrStdDev
            });

             // 2. Calculate the scaled precision vector (1/sigma^2 * 1/SigmaDataSquared)
            Vector<double> diagonalValuesVector = sigma2.Map(x => 1.0 / x).Multiply(1.0 / SigmaDataSquared);

            // 3. Convert the Vector<double> to a double array to match the Build.Diagonal signature
            Matrix<double> lambdaPrior = Matrix<double>.Build.Diagonal(diagonalValuesVector.ToArray());

            Vector<double> muPrior = Vector<double>.Build.DenseOfArray(new double[]
                { pvPriors.EthaSysMean, pvPriors.GammaMean, pvPriors.U0Mean, pvPriors.U1Mean, pvPriors.LDegrMean });

            int nrRecords = pvRecords.Count;
            bool applyDataFilter = validRecords != null && validRecords.Count == nrRecords;
            var thetaCalibratedList = new List<PvModelParams>(); 
            int iterations = 0;
            for (int k = 0; k < maxIterations; k++)
            {
                // Unpack current parameters
                var modelParams = new PvModelParams(etha: theta[0], gamma: theta[1], u0: theta[2], u1: theta[3], lDegr: theta[4]);

                // 3. Build Jacobian (J) and Residual Vector (r = Y - P_eff)
                Matrix<double> J = Matrix<double>.Build.Dense(nrRecords, ParameterCount);
                Vector<double> Y = Vector<double>.Build.Dense(nrRecords);
                Vector<double> Peff_model = Vector<double>.Build.Dense(nrRecords);

                for (int i = 0; i < nrRecords; i++)
                { 
                    if (applyDataFilter && !validRecords![i])
                        continue;
                
                    var meteoParameters = new MeteoParameters
                    (
                        Time: pvRecords[i].Timestamp,
                        Interval: TimeSpan.FromHours(1.0 / periodsPerHour),
                        SunshineDuration: pvRecords[i].SunshineDuration,
                        DirectRadiation: null,
                        DirectNormalIrradiance: null,
                        GlobalRadiation: pvRecords[i].GlobalHorizontalRadiation,
                        DiffuseRadiation: pvRecords[i].DiffuseHorizontalRadiation,
                        Temperature: pvRecords[i].AmbientTemp,
                        WindSpeed: pvRecords[i].WindSpeed,
                        WindDirection: null,
                        SnowDepth: pvRecords[i].SnowDepth,
                        RelativeHumidity: null,
                        DewPoint: null,
                        DirectRadiationVariance: null
                    );
                    var pvRecord = pvRecords[i];
                    // Call the user's provided Jacobian function
                    var (peff, d_etha, d_gamma, d_u0, d_u1, d_lDegr) = jacobianFunc(
                        installedPower,
                        periodsPerHour,
                        pvRecord.DirectGeometryFactor, 
                        pvRecord.DiffuseGeometryFactor, 
                        pvRecord.SinSunElevation,
                        meteoParameters,
                        pvRecord.Age,
                        modelParams);

                    // Weighting (if applicable)
                    var weight = pvRecord.HasMeasuredPower ? pvRecord.Weight : 0.0;
                    //weight = 1.0;

                    // Residual Vector r
                    Y[i] = pvRecord.HasMeasuredPower ? pvRecord.MeasuredPower.Value * weight : 0.0;      // TODO: Apply weighting
                    Peff_model[i] = peff * weight;

                    // Jacobian Matrix J
                    J[i, 0] = d_etha * weight;
                    J[i, 1] = d_gamma * weight;
                    J[i, 2] = d_u0 * weight;
                    J[i, 3] = d_u1 * weight;
                    J[i, 4] = d_lDegr * weight;
                }

                Vector<double> residual = Y.Subtract(Peff_model);

                // 4. Form the Penalized Normal Equation components: M * Delta_theta = b
                // M = J^T * J + Lambda_prior
                Matrix<double> JTJ = J.Transpose() * J;
                Matrix<double> M = JTJ.Add(lambdaPrior);

                // b = J^T * r - Lambda_prior * (theta_k - mu_prior)
                Vector<double> JT_r = J.Transpose() * residual;
                Vector<double> prior_penalty = lambdaPrior * (theta.Subtract(muPrior));
                Vector<double> b = JT_r.Subtract(prior_penalty);

                // 5. Solve for Delta_theta
                Vector<double> deltaTheta = M.Solve(b);

                // 6. Update Parameters
                theta = theta.Add(deltaTheta);

                // 7. Enforce Hard Physical Constraints (Clamping/Projection)
                ClampParameters(ref theta);

                // Store calibrated parameters for this iteration
                thetaCalibratedList.Add(
                    new PvModelParams(
                        etha: theta[0],
                        gamma: theta[1],
                        u0: theta[2],
                        u1: theta[3],
                        lDegr: theta[4]
                        )
                    );

                // Check for convergence before update
                iterations++;
                if (deltaTheta.L2Norm() < tolerance)
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
        private static void ClampParameters(ref Vector<double> theta)
        {
            for (int i = 0; i < theta.Count; i++)
            {
                theta[i] = Math.Min(GetPriorMax(i), Math.Max(GetPriorMin(i), theta[i]));
            }
        }
    }
}
