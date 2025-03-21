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

// Convert to ScriptableObject so it can be created as an asset
[CreateAssetMenu(fileName = "New Loot Table", menuName = "Game/Loot Table")]
public class LootTable : ScriptableObject
{
    [Header("Loot Settings")]
    [SerializeField] private List<LootItem> lootItems = new List<LootItem>();
    [Tooltip("Whether to use the default items if no items are defined")]
    [SerializeField] private bool initializeDefaultLoot = true;
    [Tooltip("The chance that this container will drop any loot (0-1)")]
    [Range(0, 1)]
    [SerializeField] private float dropRate = 1.0f;
    
    private float totalWeight = 0f;
    
    // Initialize when the asset is created or reset
    private void OnEnable()
    {
        InitializeIfNeeded();
        CalculateTotalWeight();
    }
    
    public void InitializeIfNeeded()
    {
        if (initializeDefaultLoot && lootItems.Count == 0)
        {
            // Add default loot items
            lootItems.Add(new LootItem { itemName = "Fruit Juice", description = "Heals 30 HP", weight = 0.8f, requiresTarget = true });
            lootItems.Add(new LootItem { itemName = "Shiny Bead", description = "Deals 20 damage to target enemy", weight = 0.1f, requiresTarget = true });
            lootItems.Add(new LootItem { itemName = "Super Espress-O", description = "Restores 50 SP and increases ally speed by 50%", weight = 0.1f, requiresTarget = true });
        }
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
        // Check if we should drop anything based on drop rate
        if (Random.value > dropRate || lootItems.Count == 0)
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
    
    // Calculate weights after Unity Inspector changes
    private void OnValidate()
    {
        CalculateTotalWeight();
    }
} 