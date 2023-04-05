using Newtonsoft.Json;
using System.Text;

namespace GameRandomizer
{
    public static class Utils
    {
        public static void CheckOrCreateDirectory(string directoryName)
        {
            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }
        }

        public static void LogToFile(string message, string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                writer.WriteLine(message);
            }
        }

        public static void WriteDictionaryToLogFile(Dictionary<string, string> dict, string logFilePath)
        {
            using (StreamWriter writer = new StreamWriter(logFilePath))
            {
                foreach (KeyValuePair<string, string> entry in dict)
                {
                    writer.WriteLine($"{entry.Key}: {entry.Value}");
                }
            }
        }

    }

}
