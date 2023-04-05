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
        Pcg rand;

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

        public Randomizer(string? exePath, int difficulty)
        {
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
            Utils.LogToFile($"Generating Seed: {seed}", filePath);
            Utils.LogToFile($"Difficulty: {randomizerDifficulty.ToString()}", filePath);
            rand = new Pcg((int)seed);
            // Get Available Locations
            availableLocations = Locations<TypeEnum>.GetOpenLocations(currentPowers, locationDict, randomizedItems);
            Utils.LogToFile($"Available Locations: {String.Join(", ", availableLocations)}", filePath);
            Utils.LogToFile($"Locations Count: {availableLocations.Count}", filePath);
            // If No Locations Left Alert Me
            if (availableLocations.Count == 0)
            {
                // Double Check Used Locations Count to see if 125 if not re-roll later to-do
                Utils.LogToFile($"No Locations Left - Randomized Items: {randomizedItems.Count}", filePath);
                return;
            }
            // if location is available choose a random location
            string location = Locations<TypeEnum>.GetRandomLocation(rand, locationDict, availableLocations);
            // check items available
            availableItems = Items<TypeEnum>.GetItems(itemDict, usedItems, currentPowers);
            Utils.LogToFile($"Available Items: {String.Join(", ", availableItems)}", filePath);
            Utils.LogToFile($"Items Count: {availableItems.Count}", filePath);
            // If No Items Left Alert Me
            if (availableItems.Count == 0)
            {
                // Double Check Used Items Count to see if 125 if not re-roll later to-do
                Utils.LogToFile($"No Items Left - Randomized Items: {randomizedItems.Count}", filePath);
                return;
            }
            var ItemTuple = Items<TypeEnum>.GetRandomItem(rand, itemDict, availableItems, usedItems, currentPowers);
            if (ItemTuple?.Item1 != null)
            {
                randomizedItems.Add(location, ItemTuple.Item1);
                currentPowers = (TypeEnum)(object)(((int)(object)currentPowers) | ((int)(object)ItemTuple.Item2));
                Utils.LogToFile($"Item Randomized: {ItemTuple?.Item1}", filePath);
                Utils.LogToFile($"Name: {ItemTuple?.Item1}", filePath);
                Utils.LogToFile($"Powers: {ItemTuple.Item2.ToString()}", filePath);
                Utils.LogToFile($"Current Powers: {currentPowers.ToString()}", filePath);
            }
        }

    }

}
