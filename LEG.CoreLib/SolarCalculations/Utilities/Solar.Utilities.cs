namespace LEG.CoreLib.SolarCalculations.Utilities
{
    public class SolarUtilities
    {
        private static int EvaluationYear = BasicParametersAndConstants.CurrentYear;
        public static DateOnly EvaluationYearStartDate { get; private set; } = new DateOnly(EvaluationYear, 1, 1);

        public SolarUtilities(int evaluationYear = -9999)
        {
            EvaluationYear = evaluationYear == -9999 ? BasicParametersAndConstants.CurrentYear : evaluationYear;
            EvaluationYearStartDate = new DateOnly(EvaluationYear, 1, 1);
        }

        public static double[] ConvertStringArrayToDoubleArray(string[] stringArray) =>
            Array.ConvertAll(stringArray, double.Parse);

        public static double[] RollArray(double[] array, int shift)
        {
            var length = array.Length;
            shift %= length;
            shift = shift < 0 ? shift + length : shift;
            return [..array.Skip(length - shift).Concat(array.Take(length - shift))];
        }

        public static string ArrayToString(double[,] array)
        {
            var rows = array.GetLength(0);
            var cols = array.GetLength(1);
            var sb = new System.Text.StringBuilder();

            for (var i = 0; i < rows; i++)
            {
                var row = Enumerable.Range(0, cols)
                    .Select(j => array[i, j].ToString("G4"))
                    .ToArray();
                sb.AppendLine(string.Join("\t", row));
            }
            return sb.ToString();
        }

        public static string PrintList(List<string> list) =>
            "{" + string.Join(", ", list) + "}";

        public static string PrintArray(double[] array, int decimals = -100)
        {
            if (decimals < -10)
            {
                return "[" + string.Join(", ", array) + "]";
            }
            var format = $"N{decimals}";
            return "[" + string.Join(", ", array.Select(x => x.ToString(format, System.Globalization.CultureInfo.InvariantCulture))) + "]";
        }

        public static string Print2DArray(double[,] array, int decimals = -100)
        {
            var rows = array.GetLength(0);
            var cols = array.GetLength(1);

            var lines = Enumerable.Range(0, rows)
                .Select(i => PrintArray([.. Enumerable.Range(0, cols).Select(j => array[i, j])], decimals));
            return "\n[\n " + string.Join(",\n ", lines) + "\n]";
        }

        public static double[] ApplyFunctionTo1DArray(double[] array, Func<double, double> func) =>
            [.. array.Select(func)];

        public static double[,] ApplyFunctionTo2DArray(double[,] array, Func<double, double> func)
        {
            var rows = array.GetLength(0);
            var cols = array.GetLength(1);
            var result = new double[rows, cols];
            for (var i = 0; i < rows; i++)
                for (var j = 0; j < cols; j++)
                    result[i, j] = func(array[i, j]);
            return result;
        }

        public static double SumSlice(double[,,] array, int start, int end) =>
            Enumerable.Range(start, end - start + 1)
                .SelectMany(i => Enumerable.Range(0, array.GetLength(1))
                    .SelectMany(j => Enumerable.Range(0, array.GetLength(2))
                        .Select(k => array[i, j, k])))
                .Sum();

        public static double[] GetSunProfile(List<string> sunProfile) =>
            [..sunProfile.Select(s =>
            {
                var n = s.IndexOf(':');
                return double.Parse(s[..n]) + double.Parse(s[(n + 1)..]) / 60;
            })];
    }
}