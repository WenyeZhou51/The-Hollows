using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    private static ItemManager _instance;
    public static ItemManager Instance => _instance;
    
    // We're moving away from BaseItem implementations to hardcoded CombatUI implementations
    private Dictionary<string, ItemData> itemTemplates = new Dictionary<string, ItemData>();
    
    /// <summary>
    /// Ensures ItemManager exists, creating it if necessary
    /// </summary>
    public static ItemManager EnsureExists()
    {
        if (_instance == null)
        {
            GameObject go = new GameObject("ItemManager");
            _instance = go.AddComponent<ItemManager>();
            DontDestroyOnLoad(go);
            Debug.Log("ItemManager created programmatically via EnsureExists");
        }
        return _instance;
    }
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Initialize default items
        InitializeDefaultItems();
    }
    
    private void InitializeDefaultItems()
    {
        // Create hardcoded ItemData objects instead of using BaseItem
        
        // ===== CURRENT ITEMS (used in loot tables) =====
        
        // Stone candy (replaces Fruit Juice in loot tables)
        ItemData stoneCandy = new ItemData(
            "Stone candy", 
            "Heals 30 HP", 
            1, 
            true, 
            ItemData.ItemType.Consumable);
        itemTemplates.Add(stoneCandy.name, stoneCandy);
        
        // Throwing dice (replaces Shiny Bead in loot tables)
        ItemData throwingDice = new ItemData(
            "Throwing dice", 
            "Deals 20 damage to target enemy", 
            1, 
            true, 
            ItemData.ItemType.Consumable);
        itemTemplates.Add(throwingDice.name, throwingDice);
        
        // Skipping pebble (replaces Super Espress-O in loot tables)
        ItemData skippingPebble = new ItemData(
            "Skipping pebble", 
            "Restores 50 SP and increases ally speed by 50%", 
            1, 
            true, 
            ItemData.ItemType.Consumable);
        itemTemplates.Add(skippingPebble.name, skippingPebble);
        
        // ===== LEGACY ITEMS (kept for backward compatibility with old saves) =====
        
        // Fruit Juice (legacy name)
        ItemData fruitJuice = new ItemData(
            "Fruit Juice", 
            "Heals 30 HP to all party members", 
            1, 
            true, 
            ItemData.ItemType.Consumable);
        itemTemplates.Add(fruitJuice.name, fruitJuice);
        
        // Super Espress-O (legacy name)
        ItemData superEspresso = new ItemData(
            "Super Espress-O", 
            "Restores 50 SP and increases ally's action generation by 50% for 3 turns", 
            1, 
            true, 
            ItemData.ItemType.Consumable);
        itemTemplates.Add(superEspresso.name, superEspresso);
        
        // Shiny Bead (legacy name)
        ItemData shinyBead = new ItemData(
            "Shiny Bead", 
            "Deals 20 damage to target enemy", 
            1, 
            true, 
            ItemData.ItemType.Consumable);
        itemTemplates.Add(shinyBead.name, shinyBead);
        
        // ===== SHARED ITEMS (in both old and new) =====
        
        // Panacea
        ItemData panacea = new ItemData(
            "Panacea", 
            "Heal target party member for 100HP and 100SP, remove all negative status effects", 
            1, 
            true, 
            ItemData.ItemType.Consumable);
        itemTemplates.Add(panacea.name, panacea);
        
        // Tower Shield
        ItemData towerShield = new ItemData(
            "Tower Shield", 
            "Gives TOUGH to ally for 3 turns", 
            1, 
            true, 
            ItemData.ItemType.Consumable);
        itemTemplates.Add(towerShield.name, towerShield);
        
        // Pocket Sand
        ItemData pocketSand = new ItemData(
            "Pocket Sand", 
            "WEAKENS all target enemies", 
            1, 
            true, 
            ItemData.ItemType.Consumable);
        itemTemplates.Add(pocketSand.name, pocketSand);
        
        // Otherworldly Tome
        ItemData otherworldlyTome = new ItemData(
            "Otherworldly Tome", 
            "Gives STRENGTH to all party members for 3 turns", 
            1, 
            false, 
            ItemData.ItemType.Consumable);
        itemTemplates.Add(otherworldlyTome.name, otherworldlyTome);
        
        // Unstable Catalyst
        ItemData unstableCatalyst = new ItemData(
            "Unstable Catalyst", 
            "Deals 40 damage to all enemies", 
            1, 
            false, 
            ItemData.ItemType.Consumable);
        itemTemplates.Add(unstableCatalyst.name, unstableCatalyst);
        
        // Ramen
        ItemData ramen = new ItemData(
            "Ramen", 
            "Heals ally for 15 HP", 
            1, 
            true, 
            ItemData.ItemType.Consumable);
        itemTemplates.Add(ramen.name, ramen);
        
        // Cold Key (KeyItem - special quest item)
        ItemData coldKey = new ItemData(
            "Cold Key", 
            "A frigid key that seems to emanate cold. You won it from a mysterious figure in a game of Ravenbond.", 
            1, 
            false, 
            ItemData.ItemType.KeyItem);
        itemTemplates.Add(coldKey.name, coldKey);
        
        // Medallion Left (KeyItem)
        ItemData medallionLeft = new ItemData(
            "Medallion Left", 
            "The left half of an ancient medallion.", 
            1, 
            false, 
            ItemData.ItemType.KeyItem);
        itemTemplates.Add(medallionLeft.name, medallionLeft);
        
        // Medallion Right (KeyItem)
        ItemData medallionRight = new ItemData(
            "Medallion Right", 
            "The right half of an ancient medallion.", 
            1, 
            false, 
            ItemData.ItemType.KeyItem);
        itemTemplates.Add(medallionRight.name, medallionRight);
        
        // Log the initialization
        Debug.Log($"Initialized {itemTemplates.Count} hardcoded items in ItemManager");
    }
    
    // Get ItemData by name
    public ItemData GetItemData(string itemName, int amount = 1)
    {
        if (itemTemplates.TryGetValue(itemName, out ItemData item))
        {
            // Create a clone with the specified amount
            ItemData newItem = item.Clone();
            newItem.amount = amount;
            return newItem;
        }
        
        Debug.LogWarning($"ItemData not found: {itemName}");
        return null;
    }
} 