using Newtonsoft.Json;
using System.Text;

namespace GameRandomizer
{
    public class Randomizer<TypeEnum> where TypeEnum : Enum
    {
        private TypeEnum currentPowers = (TypeEnum)(object)0;
        private Dictionary<string, TypeEnum[]> locationDict;
        private Dictionary<string, int> locationIDDict;
        private Dictionary<string, TypeEnum> itemDict;
        private List<string> availableLocations;
        private List<string> availableItems;
        private List<string> usedItems;
        private Dictionary<string, string> randomizedItems;
        Pcg rand;

        public Randomizer(string? exePath)
        {
            if (exePath != null)
            {
                locationDict = GameLocations<TypeEnum>.ParseLocations(Path.Combine(exePath, "logic/locations.json"));
                itemDict = GameItems<TypeEnum>.ParseItems(Path.Combine(exePath, "logic/items.json"));
                locationIDDict = GameLocations<TypeEnum>.ParseLocationID(Path.Combine(exePath, "logic/locations.json"));
            }
            else
            {
                locationDict = new Dictionary<string, TypeEnum[]>();
                itemDict = new Dictionary<string, TypeEnum>();
                locationIDDict = new Dictionary<string, int>();
            }
            availableLocations = new List<string>();
            availableItems = new List<string>();
            usedItems = new List<string>();
            randomizedItems = new Dictionary<string, string>();
        }

        public Dictionary<string, string> GetRandomizedItems()
        {
            return randomizedItems;
        }

        public Dictionary<string, int> GetLocationIDDictionary()
        {
            return locationIDDict;
        }

        public Dictionary<string, TypeEnum[]> GetLocationDictionary()
        {
            return locationDict;
        }

        public Dictionary<string, TypeEnum> GetItemDictionary()
        {
            return itemDict;
        }

        public void GenerateItems(string filePath, long seed)
        {
            if (seed == 0) seed = new Random().Next(0, int.MaxValue);
            rand = new Pcg((int)seed);
            // Get Available Locations
            availableLocations = GameLocations<TypeEnum>.GetOpenLocations(currentPowers, locationDict, randomizedItems);
            Utilitys.LogToFile($"Available Locations: {String.Join(", ", availableLocations)}", filePath);
            Utilitys.LogToFile($"Locations Count: {availableLocations.Count}", filePath);
            // If No Locations Left Alert Me
            if (availableLocations.Count == 0)
            {
                // Double Check Used Locations Count to see if 125 if not re-roll later to-do
                Utilitys.LogToFile($"No Locations Left - Randomized Items: {randomizedItems.Count}", filePath);
                return;
            }
            // if location is available choose a random location
            string location = GameLocations<TypeEnum>.GetRandomLocation(rand, locationDict, availableLocations);
            // check items available
            availableItems = GameItems<TypeEnum>.GetItems(itemDict, usedItems, currentPowers);
            Utilitys.LogToFile($"Available Items: {String.Join(", ", availableItems)}", filePath);
            Utilitys.LogToFile($"Items Count: {availableItems.Count}", filePath);
            // If No Items Left Alert Me
            if (availableItems.Count == 0)
            {
                // Double Check Used Items Count to see if 125 if not re-roll later to-do
                Utilitys.LogToFile($"No Items Left - Randomized Items: {randomizedItems.Count}", filePath);
                return;
            }
            var ItemTuple = GameItems<TypeEnum>.GetRandomItem(rand, itemDict, availableItems, usedItems, currentPowers);
            if (ItemTuple?.Item1 != null)
            {
                randomizedItems.Add(location, ItemTuple.Item1);
                currentPowers = (TypeEnum)(object)(((int)(object)currentPowers) | ((int)(object)ItemTuple.Item2));
                Utilitys.LogToFile($"Item Randomized: {ItemTuple?.Item1}", filePath);
                Utilitys.LogToFile($"Name: {ItemTuple?.Item1}", filePath);
                Utilitys.LogToFile($"Powers: {ItemTuple.Item2.ToString()}", filePath);
                Utilitys.LogToFile($"Current Powers: {currentPowers.ToString()}", filePath);
            }
        }

    }

    public static class GameItems<TypeEnum> where TypeEnum : Enum
    {
        public class Item
        {
            public string name { get; set; }
            public TypeEnum powers { get; set; }
            public int id { get; set; }
        }

        public static Dictionary<string, TypeEnum> ParseItems(string filename)
        {
            List<Item> items = JsonConvert.DeserializeObject<List<Item>>(File.ReadAllText(filename));
            return items.ToDictionary(i => i.name, i => i.powers);
        }

        public static void OutputItemDict(Dictionary<string, TypeEnum> itemDict)
        {
            string logDirectory = "logs";
            string logFilePath = $"{logDirectory}/items.log";

            Utilitys.CheckOrCreateDirectory(logDirectory);

            using (StreamWriter itemWriter = new StreamWriter(logFilePath))
            {
                itemWriter.WriteLine("ItemDict:");
                foreach (var kvp in itemDict)
                {
                    string powersList = string.Join(" | ", Enum.GetValues(typeof(TypeEnum))
                        .Cast<TypeEnum>()
                        .Where(power => (Convert.ToInt32(kvp.Value) & Convert.ToInt32(power)) == Convert.ToInt32(power))
                        .Select(power => Enum.GetName(typeof(TypeEnum), power)));

                    itemWriter.WriteLine("\n=== New Item ===");
                    itemWriter.WriteLine($"Item Name: {kvp.Key}");
                    itemWriter.WriteLine($"Required Powers: {powersList}");
                    itemWriter.WriteLine("=== End Item ===");
                }
            }
        }

        public static Tuple<string, TypeEnum>? GetRandomItem(Pcg rand, Dictionary<string, TypeEnum> itemDict, List<string> items, List<string> usedItems, TypeEnum currentPowers)
        {
            // Filter out the items that have already been used
            var unusedItems = itemDict.Where(item => items.Contains(item.Key)).ToList();

            // If there are no unused items, exit the method
            if (unusedItems.Count == 0)
            {
                return null;
            }

            // Select a random item from the list of unused items
            var randomIndex = rand.Next(0, unusedItems.Count);
            var randomItem = unusedItems[randomIndex];

            // Add the name of the selected item to the list of used items
            usedItems.Add(randomItem.Key);

            // Use reflection to add the value of the selected item to the currentPowers TypeEnum
            var currentValue = Convert.ToInt64(currentPowers);
            var selectedItemValue = Convert.ToInt64(randomItem.Value);
            var newValue = currentValue | selectedItemValue;
            currentPowers = (TypeEnum)Enum.ToObject(typeof(TypeEnum), newValue);
            return Tuple.Create(randomItem.Key, currentPowers);
        }

        public static List<string> GetItems(Dictionary<string, TypeEnum> itemsDict, List<string> usedItems, TypeEnum currentPowers)
        {
            // Find the next required power
            TypeEnum nextRequiredPower = default(TypeEnum);
            foreach (TypeEnum value in Enum.GetValues(typeof(TypeEnum)))
            {
                if (value.Equals(default(TypeEnum))) continue;
                if (currentPowers.HasFlag(value)) continue;

                nextRequiredPower = value;
                break;
            }

            // Filter out the items that have already been used
            // var unusedItems = itemsDict.Where(item => !usedItems.Contains(item.Key)).ToList();

            // Filter the unused items to only include items with the next required power
            var unusedItems = itemsDict.Where(item => !usedItems.Contains(item.Key) && item.Value.HasFlag(nextRequiredPower)).ToList();

            // If there are no unused items with the next required power, exit the method
            if (unusedItems.Count == 0)
            {
                return new List<string>();
            }

            // Convert the list of unused items to a list of item names
            var itemNames = unusedItems.Select(item => item.Key).ToList();

            // Return the list of item names
            return itemNames;
        }

        public static void GetItemByPower(TypeEnum power, Dictionary<string, TypeEnum> itemsDict, List<string> usedItems)
        {
            // Filter out the items that have already been used and don't have the requested power
            var matchingItems = itemsDict.Where(item => !usedItems.Contains(item.Key) && item.Value.HasFlag(power)).ToList();

            // If there are no matching items, exit the method
            if (matchingItems.Count == 0)
            {
                return;
            }

            // Select a random matching item
            var randomIndex = new Random().Next(matchingItems.Count);
            var randomItem = matchingItems[randomIndex];

            // Add the name of the selected item to the list of used items
            usedItems.Add(randomItem.Key);

            // Do something with the selected item...
            Console.WriteLine($"Selected item: {randomItem.Key}");
        }
    }

    public static class GameLocations<TypeEnum> where TypeEnum : Enum
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

            Utilitys.CheckOrCreateDirectory(logDirectory);

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

    public static class Utilitys
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
