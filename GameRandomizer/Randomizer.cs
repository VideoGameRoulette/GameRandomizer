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
        private TypeDifficulty randomizerDifficulty = TypeDifficulty.Easy;
        private string logPath;
        Pcg rand;

        public Randomizer(string? exePath, int difficulty, string filePath)
        {
            logPath = filePath;
            randomizerDifficulty = (TypeDifficulty)(object)difficulty;
            if (exePath != null)
            {
                locationDict = Locations<TypeEnum>.ParseLocations(Path.Combine(exePath, GetLocationFile(difficulty)));
                itemDict = Items<TypeEnum>.ParseItems(Path.Combine(exePath, GetItemFile(difficulty)));
                locationIDDict = Locations<TypeEnum>.ParseLocationID(Path.Combine(exePath, "logic/locations.json"));
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

        public TypeEnum GetCurrentPowers()
        {
            return currentPowers;
        }

        public void SetCurrentPowers(TypeEnum newPowers)
        {
            currentPowers = (TypeEnum)(object)(((int)(object)currentPowers) | ((int)(object)newPowers));
        }

        private string GetItemFile(int difficulty)
        {
            switch (difficulty)
            {
                case 1: return "logic/items_easy.json";
                case 2: return "logic/items_normal.json";
                default: return "logic/items_hard.json";
            }
        }

        private string GetLocationFile(int difficulty)
        {
            switch (difficulty)
            {
                case 1: return "logic/locations_easy.json";
                case 2: return "logic/locations_normal.json";
                default: return "logic/locations_hard.json";
            }
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

        public string GetLogPath()
        {
            return logPath;
        }

        public void GenerateItems(Randomizer<TypeEnum> randomizer, long seed)
        {
            if (seed == 0) seed = new Random().Next(0, int.MaxValue);
            Utils.LogToFile($"Generating Seed: {seed}", logPath);
            Utils.LogToFile($"Difficulty: {randomizerDifficulty.ToString()}", logPath);
            rand = new Pcg((int)seed);
            // Get Available Locations
            availableLocations = Locations<TypeEnum>.GetOpenLocations(currentPowers, locationDict, randomizedItems);
            Utils.LogToFile($"Available Locations: {String.Join(", ", availableLocations)}", logPath);
            Utils.LogToFile($"Locations Count: {availableLocations.Count}", logPath);
            // If No Locations Left Alert Me
            if (availableLocations.Count == 0)
            {
                // Double Check Used Locations Count to see if 125 if not re-roll later to-do
                Utils.LogToFile($"No Locations Left - Randomized Items: {randomizedItems.Count}", logPath);
                return;
            }
            // if location is available choose a random location
            string location = Locations<TypeEnum>.GetRandomLocation(rand, locationDict, availableLocations);
            Utils.LogToFile($"=== Location Randomized ===", logPath);
            Utils.LogToFile($"Name: {location}", logPath);
            // check items available
            availableItems = Items<TypeEnum>.GetItems(itemDict, usedItems, currentPowers);
            Utils.LogToFile($"Available Items: {String.Join(", ", availableItems)}", logPath);
            Utils.LogToFile($"Items Count: {availableItems.Count}", logPath);
            // If No Items Left Alert Me
            if (availableItems.Count == 0)
            {
                // Double Check Used Items Count to see if 125 if not re-roll later to-do
                Utils.LogToFile($"No Items Left - Randomized Items: {randomizedItems.Count}", logPath);
                return;
            }
            var item = Items<TypeEnum>.GetRandomItem(randomizer, rand, itemDict, availableItems, usedItems, currentPowers);
            if (item != null)
            {
                randomizedItems.Add(location, item);
            }
        }

    }

}
