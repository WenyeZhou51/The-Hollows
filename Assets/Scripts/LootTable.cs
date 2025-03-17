using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LootItem
{
    public string itemName;
    public string description;
    public float weight;
    public ItemData.ItemType itemType = ItemData.ItemType.Consumable;
    public bool requiresTarget = false;
    public Sprite icon;
}

public class LootTable : MonoBehaviour
{
    [SerializeField] private List<LootItem> lootItems = new List<LootItem>();
    [SerializeField] private bool initializeDefaultLoot = true;
    
    private float totalWeight = 0f;
    
    private void Awake()
    {
        if (initializeDefaultLoot && lootItems.Count == 0)
        {
            // Add default loot items
            lootItems.Add(new LootItem { itemName = "Fruit Juice", description = "Heals 30 HP", weight = 0.8f });
            lootItems.Add(new LootItem { itemName = "Warding Charm", description = "Protects against harm", weight = 0.1f });
            lootItems.Add(new LootItem { itemName = "Shortsword", description = "A simple weapon", weight = 0.1f, itemType = ItemData.ItemType.Equipment });
        }
        
        // Calculate total weight
        CalculateTotalWeight();
    }
    
    private void CalculateTotalWeight()
    {
        totalWeight = 0f;
        foreach (LootItem item in lootItems)
        {
            totalWeight += item.weight;
        }
    }
    
    public ItemData GetRandomLoot()
    {
        if (lootItems.Count == 0)
            return null;
            
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        
        foreach (LootItem lootItem in lootItems)
        {
            currentWeight += lootItem.weight;
            
            if (randomValue <= currentWeight)
            {
                return new ItemData(
                    lootItem.itemName, 
                    lootItem.description, 
                    1, // Amount
                    lootItem.requiresTarget,
                    lootItem.itemType,
                    lootItem.icon
                );
            }
        }
        
        // Fallback (should never happen)
        return new ItemData("Mysterious Item", "An unexpected find", 1, false);
    }
    
    // Add a loot item to the table
    public void AddLootItem(string name, string description, float weight, ItemData.ItemType type = ItemData.ItemType.Consumable, bool requiresTarget = false, Sprite icon = null)
    {
        LootItem newItem = new LootItem
        {
            itemName = name,
            description = description,
            weight = weight,
            itemType = type,
            requiresTarget = requiresTarget,
            icon = icon
        };
        
        lootItems.Add(newItem);
        CalculateTotalWeight();
    }
} 