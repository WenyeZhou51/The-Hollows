using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    private static ItemManager _instance;
    public static ItemManager Instance => _instance;
    
    // We're moving away from BaseItem implementations to hardcoded CombatUI implementations
    private Dictionary<string, ItemData> itemTemplates = new Dictionary<string, ItemData>();
    
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
        // Fruit Juice
        ItemData fruitJuice = new ItemData(
            "Fruit Juice", 
            "Heals 30 HP to all party members", 
            1, 
            true, 
            ItemData.ItemType.Consumable);
        itemTemplates.Add(fruitJuice.name, fruitJuice);
        
        // Super Espress-O
        ItemData superEspresso = new ItemData(
            "Super Espress-O", 
            "Restores 50 SP and increases ally's action generation by 50% for 3 turns", 
            1, 
            true, 
            ItemData.ItemType.Consumable);
        itemTemplates.Add(superEspresso.name, superEspresso);
        
        // Shiny Bead
        ItemData shinyBead = new ItemData(
            "Shiny Bead", 
            "Deals 20 damage to target enemy", 
            1, 
            true, 
            ItemData.ItemType.Consumable);
        itemTemplates.Add(shinyBead.name, shinyBead);
        
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