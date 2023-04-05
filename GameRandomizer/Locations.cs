using Newtonsoft.Json;
using System.Text;

namespace GameRandomizer
{
    public static class Locations<TypeEnum> where TypeEnum : Enum
    {
        public class Location
        {
            public string name { get; set; }
            public TypeEnum[] requiredPowers { get; set; }
            public int id { get; set; }
        }

        public static Dictionary<string, TypeEnum[]> ParseLocations(string filename)
        {
            List<Location> locations = JsonConvert.DeserializeObject<List<Location>>(File.ReadAllText(filename));
            return locations.ToDictionary(location => location.name, location => location.requiredPowers);
        }

        public static Dictionary<string, int> ParseLocationID(string filename)
        {
            List<Location> locations = JsonConvert.DeserializeObject<List<Location>>(File.ReadAllText(filename));
            return locations.ToDictionary(location => location.name, location => location.id);
        }

        public static void OutputLocationDict(Dictionary<string, TypeEnum[]> locationDict)
        {
            string logDirectory = "logs";
            string logFilePath = $"{logDirectory}/locations.log";

            Utils.CheckOrCreateDirectory(logDirectory);

            using (StreamWriter locationWriter = new StreamWriter(logFilePath))
            {
                locationWriter.WriteLine("LocationDict:");
                foreach (KeyValuePair<string, TypeEnum[]> kvp in locationDict)
                {
                    StringBuilder powersBuilder = new StringBuilder();
                    foreach (TypeEnum powers in kvp.Value)
                    {
                        string powerString = powers.ToString();
                        powersBuilder.Append(powerString.Replace(", ", " | ") + ", ");
                    }
                    locationWriter.WriteLine("\n=== New Location ===");
                    locationWriter.WriteLine("Location Item: " + kvp.Key);
                    string[] splitString = powersBuilder.ToString().Split(new string[] { ", " }, StringSplitOptions.None);
                    foreach (string s in splitString)
                    {
                        if (string.IsNullOrEmpty(s)) continue;
                        locationWriter.WriteLine("Required Powers: " + s);
                    }
                    locationWriter.WriteLine("=== End Location ===");
                }
            }
        }

        public static List<string> GetOpenLocations(TypeEnum currentPowers, Dictionary<string, TypeEnum[]> locationDict, Dictionary<string, string> usedLocations)
        {
            List<string> availableLocations = new List<string>();

            foreach (var location in locationDict)
            {
                // Check if the powers at this location match the current powers
                if (location.Value.Any(power => currentPowers.HasFlag(power)))
                {
                    // Check if this location has already been used
                    if (!usedLocations.ContainsKey(location.Key))
                    {
                        availableLocations.Add(location.Key);
                    }
                }
            }

            return availableLocations;
        }

        public static string GetRandomLocation(Pcg rand, Dictionary<string, TypeEnum[]> locationDict, List<string> availableLocations)
        {
            // Filter out the locations that have already been used
            var unusedLocations = locationDict.Where(location => availableLocations.Contains(location.Key)).ToList();

            // Select a random item from the list of unused items
            var randomIndex = rand.Next(0, unusedLocations.Count);
            var randomLocation = unusedLocations[randomIndex];

            return randomLocation.Key;
        }
    }

}
