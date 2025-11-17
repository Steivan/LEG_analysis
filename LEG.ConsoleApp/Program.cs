//Login "Felix.senn@ggaweb", "Verena1955"
//stationIds =  "481826002490", "702003001860" 

using System;
using System.Diagnostics;
using System.IO;

namespace E3dcCsvCaller
{
    class Program
    {
        static void Main()
        {
            const string serial1 = "481826002490";
            const string serial2 = "702003001860";

            // === CONFIG IN C# ===
            string pythonExe = @"C:\Python312\python.exe";
            string scriptPath = @"C:\code\LEG_analysis\LEG.ConsoleApp\e3dc_download.py";

            string username = "Felix.senn@ggaweb";
            string password = "Verena1955";

            string serial = serial1;
            string start = "2025-11-01";
            string end = "2025-11-17";
            string output = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "E3DC_Month.csv");
            // =====================

            string args = $"\"{scriptPath}\" " +
                          $"--serial {serial} " +
                          $"--start {start} " +
                          $"--end {end} " +
                          $"--output \"{output}\" " +
                          $"--username \"{username}\" " +
                          $"--password \"{password}\"";

            Console.WriteLine("Starting E3DC download...");
            Console.WriteLine($"Command: {pythonExe} {args}");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();

            string outputText = process.StandardOutput.ReadToEnd();
            string errorText = process.StandardError.ReadToEnd();

            process.WaitForExit();

            Console.WriteLine("\n=== OUTPUT ===");
            Console.WriteLine(outputText);

            if (process.ExitCode != 0)
            {
                Console.WriteLine("\n=== ERROR ===");
                Console.WriteLine(errorText);
            }
            else
            {
                Console.WriteLine($"\nCSV saved to: {output}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
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
