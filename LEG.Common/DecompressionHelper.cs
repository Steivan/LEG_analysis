using System.IO;
using System.IO.Compression;
using System.Threading;

namespace LEG.Common
{
    public static class FileHelper
    {
        public static bool IsGzipFile(string filePath)
        {
            if (!File.Exists(filePath))
                return false;

            using var fileStream = File.OpenRead(filePath);
            if (fileStream.Length < 2)
                return false;

            var firstByte = fileStream.ReadByte();
            var secondByte = fileStream.ReadByte();
            return firstByte == 0x1F && secondByte == 0x8B;
        }

        public static void DecompressGzipToCsv(string gzipPath, string csvPath)
        {
            // Step 1: Read the entire compressed file into memory
            var compressedData = File.ReadAllBytes(gzipPath);

            // Step 2: Decompress in memory
            byte[] decompressedData;
            using (var compressedStream = new MemoryStream(compressedData))
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var decompressedStream = new MemoryStream())
            {
                gzipStream.CopyTo(decompressedStream);
                decompressedData = decompressedStream.ToArray();
            }

            //// Step 3: Optionally wait a short moment to ensure file system releases the handle
            //Thread.Sleep(50);

            // Step 4: Write the decompressed data to the output file (can be the same as input)
            File.WriteAllBytes(csvPath, decompressedData);
        }
    }
}
