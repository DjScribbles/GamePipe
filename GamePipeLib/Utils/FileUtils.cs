/**
* Copyright (c) 2016 Joseph Shaw
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/
using System;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
namespace GamePipeLib.Utils
{

    //'Peer to peer negotiation https://msdn.microsoft.com/en-us/library/bb906998.aspx
    //'Copy faster with fast copy! http://www.codeproject.com/Tips/777322/A-Faster-File-Copy

    static public class FileUtils
    {
        //This routine taken from an msdn example: http://msdn.microsoft.com/en-us/library/bb762914%28v=vs.110%29.aspx, the overwrite option was added as a feature
        public static bool CanMoveAcfFile()
        {
            return System.Diagnostics.Process.GetProcessesByName("SteamService").Any() == false;
        }

        public static string GetReadableFileSize(long diskSize)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = diskSize;
            int order = 0;
            while (len >= 1024 && order + 1 < sizes.Length)
            {
                order++;
                len = len / 1024;
            }

            // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
            // show a single decimal place, and no space.
            string result = String.Format("{0:0.#}{1}", len, sizes[order]);
            return result;
        }

        public static string ComputeFileMD5(string filePath)
        {
            if (File.Exists(filePath) == false)
            {
                return filePath;
                //Return the file path if the file doesn't exist, just to ensure it doesn't match a real file.
            }

            byte[] hashBytes = null;
            using (System.Security.Cryptography.MD5CryptoServiceProvider crypto = new System.Security.Cryptography.MD5CryptoServiceProvider())
            {
                using (System.IO.FileStream stream = new System.IO.FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    hashBytes = crypto.ComputeHash(stream);
                }
            }
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var word in hashBytes)
            {
                stringBuilder.Append(word.ToString("X2"));
            }

            return stringBuilder.ToString();
        }


        const int DEFAULT_BUFFER_SIZE = 16384;
        public static CrcStream OpenReadStream(string filePath, int bufferSize = DEFAULT_BUFFER_SIZE)
        {
            return new CrcStream(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None, bufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan));
        }

        public static CrcStream OpenWriteStream(string filePath, int bufferSize = DEFAULT_BUFFER_SIZE)
        {
            return new CrcStream(new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, bufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan));
        }

        public static CrcStream OpenCompressedReadStream(string filePath, int bufferSize = DEFAULT_BUFFER_SIZE)
        {
            return new CrcStream(new DeflateStream(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None, bufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan), CompressionMode.Decompress));
        }

        public static CrcStream OpenCompressedWriteStream(string filePath, int bufferSize = DEFAULT_BUFFER_SIZE, CompressionLevel compression = CompressionLevel.Optimal)
        {
            return new CrcStream(new DeflateStream(new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, bufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan), compression));
        }
    }
}
