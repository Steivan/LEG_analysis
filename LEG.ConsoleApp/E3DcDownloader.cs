using System.Diagnostics;

namespace LEG.ConsoleApp
{
    public class E3dcDownloader
    {
        private static readonly string PythonScript = @"C:\code\LEG_analysis\LEG.ConsoleApp\e3dc_download.py";

        public static async Task<bool> DownloadAsync(
            string serial,
            string ipAddress,
            string username,
            string portalPassword,
            string rscpKey,
            DateTime start,
            DateTime end,
            string resolution,          // "15min", "hour" or "day"
            string outputCsvPath)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "python",           // or full path: @"C:\Python312\python.exe"
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            psi.ArgumentList.Add(PythonScript);
            psi.ArgumentList.Add("--serial"); psi.ArgumentList.Add(serial);
            psi.ArgumentList.Add("--address"); psi.ArgumentList.Add(ipAddress);
            psi.ArgumentList.Add("--username"); psi.ArgumentList.Add(username);
            psi.ArgumentList.Add("--password"); psi.ArgumentList.Add(portalPassword);
            psi.ArgumentList.Add("--rscp-key"); psi.ArgumentList.Add(rscpKey);  // ← zurück zu --rscp-key!
            psi.ArgumentList.Add("--start"); psi.ArgumentList.Add(start.ToString("yyyy-MM-dd"));
            psi.ArgumentList.Add("--end"); psi.ArgumentList.Add(end.ToString("yyyy-MM-dd"));
            psi.ArgumentList.Add("--resolution"); psi.ArgumentList.Add(resolution);
            psi.ArgumentList.Add("--output"); psi.ArgumentList.Add(outputCsvPath);

            using var process = Process.Start(psi);
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            Console.WriteLine(output);
            if (!string.IsNullOrWhiteSpace(error))
                Console.WriteLine("ERROR: " + error);

            return process.ExitCode == 0 && File.Exists(outputCsvPath);
        }
    }
}