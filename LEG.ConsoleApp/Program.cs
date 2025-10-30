using LEG.E3Dc.Abstractions;
using LEG.E3Dc.Client;
using System;

namespace E3DcConsoleApp
{
    public class Program
    {
        static void Main(string[] args)
        {
            // All MeteoSwiss-related logic has been migrated to the new MeteoConsoleApp.
            // This application is now only responsible for E3DC aggregation.

            // No command line arguments are provided, run the default E3DC aggregation.
            if (args.Length == 0)
            {
                Console.WriteLine("No arguments provided, running default E3DC aggregation...");
                E3DcAggregator.RunE3DcAggregation();
                return;
            }

            // Handle any other command-line arguments if necessary for other tasks.
            Console.WriteLine("Command line arguments are not used for E3DC aggregation.");
        }
    }
}