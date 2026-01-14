namespace CalibrationApp.Helpers
{
    internal class SolverNNLS
    {
        /// <summary>
        /// Solves A*x = b with constraint x >= 0 using an iterative active-set method
        /// </summary>
        internal static double[] SolveNonNegative(double[,] A, double[] b, int maxIterations = 1000, double tolerance = 1e-10)
        {
            int n = b.Length;
            double[] x = new double[n];

            // Initial guess: unconstrained least squares solution or zero
            for (int i = 0; i < n; i++)
            {
                x[i] = Math.Max(0, b[i] / A[i, i]);
            }

            // Projected gradient descent with line search
            for (int iter = 0; iter < maxIterations; iter++)
            {
                // Compute residual: r = b - A*x
                double[] residuals = new double[n];
                for (int i = 0; i < n; i++)
                {
                    double sum = 0;
                    for (int j = 0; j < n; j++)
                    {
                        sum += A[i, j] * x[j];
                    }
                    residuals[i] = b[i] - sum;
                }

                // Check convergence
                double residualNorm = Math.Sqrt(residuals.Sum(r => r * r));
                if (residualNorm < tolerance)
                {
                    break;
                }

                // Update coefficients
                for (int i = 0; i < n; i++)
                {
                    double numerator = b[i];
                    for (int j = 0; j < n; j++)
                    {
                        if (i != j)
                        {
                            numerator -= A[i, j] * x[j];
                        }
                    }
                    x[i] = Math.Max(0, numerator / A[i, i]);
                }
            }

            return x;
        }
    }
}
