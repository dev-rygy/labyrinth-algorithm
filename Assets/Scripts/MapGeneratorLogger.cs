using System;
using System.IO;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{

    public class MapGeneratorLogger
    {
        private static readonly string LogFilePath = System.IO.Path.Combine(Application.persistentDataPath, "game_log.txt");

        private static void InitializeLogFile()
        {
            if (File.Exists(LogFilePath))
            {
                File.Delete(LogFilePath);
            }

            File.Create(LogFilePath).Dispose();
        }

        public static void Log(string system, string message)
        {
            Write("LOG", system, message);
        }

        public static void Warning(string system, string message)
        {
            Write("WARNING", system, message);
        }

        public static void Error(string system, string message)
        {
            Write("ERROR", system, message);
        }

        private static void Write(string level, string system, string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            string line =
                $"[{timestamp}] [{level}] [{system}] {message}";

            File.AppendAllText(LogFilePath, line + Environment.NewLine);
        }

        public static string GetLogFilePath()
        {
            return LogFilePath;
        }

    }
}
