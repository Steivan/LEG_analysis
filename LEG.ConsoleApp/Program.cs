//Login "Felix.senn@ggaweb.ch", "Verena1955"
//stationIds =  "481826002490", "702003001860" 

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace E3DC_DataDownloader
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // ==================  YOUR SETTINGS HERE  ==================
            string serialNumber = "S10-481826002490";  // ← add "S10-" prefix (confirm exact from portal!)            string portalEmail = "Felix.senn@ggaweb.chh";       // ← portal login email
            string portalEmail = "Felix.senn@ggaweb.ch"; // ← portal password (the one you use on s10.e3dc.com)
            string portalPassword = "Verena1955"; // ← portal password (the one you use on s10.e3dc.com)

            string startDate = "2025-11-01";   // YYYY-MM-DD
            string endDate = "2025-11-16";   // YYYY-MM-DD (inclusive)

            string targetFolder = @"C:\E3DC_Data";  // will be created if it doesn't exist
            // =========================================================

            //string pythonScript = "e3dc_download.py";
            // This points exactly to your project root – works forever, no copy needed
            string pythonScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\e3dc_download.py");
            // Build the argument string (password stays in process, never logged)

            string arguments = $"--serial {serialNumber} " +
                               $"--user \"{portalEmail}\" " +
                               $"--password \"{portalPassword}\"";
 
            //string arguments = $"--serial {serialNumber} " +
            //                   $"--start {startDate} " +
            //                   $"--end {endDate} " +
            //                   $"--target \"{targetFolder}\" " +
            //                   $"--user \"{portalEmail}\" " +
            //                   $"--password \"{portalPassword}\"";

            Console.WriteLine($"Starting download {startDate} → {endDate} for {serialNumber} …");
            Console.WriteLine($"Target folder: {targetFolder}");
            Console.WriteLine();

            var result = await RunPythonScript(pythonScript, arguments);

            if (result.Success)
            {
                Console.WriteLine("SUCCESS!");
                Console.WriteLine(result.Output);
            }
            else
            {
                Console.WriteLine("DOWNLOAD FAILED");
                Console.WriteLine("Error output:");
                Console.WriteLine(result.Error);
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static async Task<(bool Success, string Output, string Error)> RunPythonScript(string script, string arguments)
        {
            // <<<=== THIS IS THE ONLY CHANGE – 3 LINES ADDED ===>>>
            string scriptDirectory = Path.GetDirectoryName(Path.GetFullPath(script));
            // <<<==============================================>>>

            var startInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"{script}\" {arguments}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,

                // <<<=== THESE TWO LINES FIX THE MODULE SEARCH PATH ===>>>
                WorkingDirectory = scriptDirectory,   // ← crucial!
                                                      // Optional: also force PYTHONPATH (belt-and-suspenders)
                                                      // Environment = { {50} { "PYTHONPATH", scriptDirectory } }
                                                      // <<<====================================================>>>
            };

            using var process = new Process { StartInfo = startInfo };

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, e) => { if (e.Data != null) { Console.WriteLine(e.Data); outputBuilder.AppendLine(e.Data); } };
            process.ErrorDataReceived += (sender, e) => { if (e.Data != null) { Console.WriteLine("ERR> " + e.Data); errorBuilder.AppendLine(e.Data); } };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            return (process.ExitCode == 0, outputBuilder.ToString(), errorBuilder.ToString());
        }
    }
}

//namespace E3DcConsoleApp
//{
//    public class Program
//    {
//        static void Main(string[] args)
//        {
//            // All MeteoSwiss-related logic has been migrated to the new MeteoConsoleApp.
//            // This application is now only responsible for E3DC aggregation.

//            // No command line arguments are provided, run the default E3DC aggregation.
//            if (args.Length == 0)
//            {
//                Console.WriteLine("No arguments provided, running default E3DC aggregation...");
//                E3DcAggregator.RunE3DcAggregation();
//                return;
//            }

//            // Handle any other command-line arguments if necessary for other tasks.
//            Console.WriteLine("Command line arguments are not used for E3DC aggregation.");
//        }
//    }
//}
