using Newtonsoft.Json;
using System.Text;

namespace GameRandomizer
{
    public static class Items<TypeEnum> where TypeEnum : Enum
    {
        public class Item
        {
            public string name { get; set; }
            public TypeEnum powers { get; set; }
            // public int id { get; set; }
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

            Utils.CheckOrCreateDirectory(logDirectory);

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

}
