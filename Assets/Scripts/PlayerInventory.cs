using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private List<ItemData> items = new List<ItemData>();
    
    // Flag to prevent saving during scene transitions
    private bool preventNextSave = false;
    
    public delegate void InventoryChangedHandler();
    public event InventoryChangedHandler OnInventoryChanged;
    
    public List<ItemData> Items => items;
    
    private void Start()
    {
        // Load inventory from PersistentGameManager if it exists
        LoadFromPersistentManager();
    }
    
    private void OnDisable()
    {
        // Check if save prevention is active
        if (preventNextSave)
        {
            Debug.Log("[INVENTORY DEBUG] Skipping save on disable due to preventNextSave flag");
            // Reset the flag for next time
            preventNextSave = false;
            return;
        }
        
        // Save inventory to PersistentGameManager when disabled
        SaveToPersistentManager();
    }
    
    private void LoadFromPersistentManager()
    {
        // Make sure PersistentGameManager exists
        if (PersistentGameManager.EnsureExists() != null)
        {
            Dictionary<string, int> persistentInventory = PersistentGameManager.Instance.GetPlayerInventory();
            
            // Only load if we have saved inventory data
            if (persistentInventory.Count > 0)
            {
                // Clear current inventory
                items.Clear();
                
                // Add each item from the persistent inventory
                foreach (var pair in persistentInventory)
                {
                    // Determine item type - special handling for known key items
                    ItemData.ItemType itemType = ItemData.ItemType.Consumable;
                    
                    // Check for known key items by name
                    if (pair.Key == "Cold Key" || 
                        (pair.Key.Contains("Key") && pair.Key.Contains("Cold")) || 
                        pair.Key.Contains("Medallion") || 
                        pair.Key.StartsWith("Medal"))
                    {
                        itemType = ItemData.ItemType.KeyItem;
                        Debug.Log($"Loaded {pair.Key} as a KeyItem type");
                    }
                    
                    ItemData item = new ItemData(pair.Key, "", pair.Value, false, itemType);
                    items.Add(item);
                }
                
                Debug.Log($"Loaded {items.Count} items from PersistentGameManager");
                
                // Notify listeners
                OnInventoryChanged?.Invoke();
            }
            else
            {
                Debug.Log("No inventory data found in PersistentGameManager");
            }
        }
    }
    
    private void SaveToPersistentManager()
    {
        // Make sure PersistentGameManager exists
        if (PersistentGameManager.EnsureExists() != null)
        {
            // Convert items to dictionary format
            Dictionary<string, int> inventoryDict = new Dictionary<string, int>();
            
            foreach (ItemData item in items)
            {
                inventoryDict[item.name] = item.amount;
            }
            
            // Update persistent manager
            PersistentGameManager.Instance.UpdatePlayerInventory(inventoryDict);
            
            Debug.Log($"Saved {items.Count} items to PersistentGameManager");
        }
    }
    
    /// <summary>
    /// Prevents the next save operation when OnDisable is called
    /// Used to prevent race conditions during scene transitions
    /// </summary>
    public void PreventNextSave()
    {
        preventNextSave = true;
        Debug.Log("[INVENTORY DEBUG] Next save operation will be skipped");
    }
    
    /// <summary>
    /// Force saves the current inventory to the PersistentGameManager
    /// Used during scene transitions to ensure data is properly saved
    /// </summary>
    public void ForceSaveToPersistentManager()
    {
        Debug.Log("[INVENTORY DEBUG] Forcing save to PersistentGameManager with " + items.Count + " items");
        SaveToPersistentManager();
    }
    
    public void AddItem(ItemData item)
    {
        // Check if the item already exists in inventory
        ItemData existingItem = items.Find(i => i.name == item.name);
        
        if (existingItem != null)
        {
            // Just increase the amount if it's the same item
            existingItem.amount += item.amount;
        }
        else
        {
            // Add new item
            items.Add(item);
        }
        
        // Update persistent manager
        if (PersistentGameManager.Instance != null)
        {
            PersistentGameManager.Instance.AddItemToInventory(item.name, item.amount);
        }
        
        // Notify listeners that inventory has changed
        OnInventoryChanged?.Invoke();
    }
    
    public void RemoveItem(ItemData item)
    {
        ItemData existingItem = items.Find(i => i.name == item.name);
        
        if (existingItem != null)
        {
            existingItem.amount -= item.amount;
            
            // Remove the item if amount is 0 or less
            if (existingItem.amount <= 0)
            {
                items.Remove(existingItem);
            }
            
            // Update persistent manager
            if (PersistentGameManager.Instance != null)
            {
                PersistentGameManager.Instance.RemoveItemFromInventory(item.name, item.amount);
            }
            
            // Notify listeners that inventory has changed
            OnInventoryChanged?.Invoke();
        }
    }
    
    public bool HasItem(string itemName)
    {
        return items.Exists(i => i.name == itemName && i.amount > 0);
    }
    
    public ItemData GetItem(string itemName)
    {
        return items.Find(i => i.name == itemName);
    }
    
    /// <summary>
    /// Clears all items from the inventory
    /// </summary>
    public void ClearInventory()
    {
        items.Clear();
        
        // Notify listeners that inventory has changed
        OnInventoryChanged?.Invoke();
    }
} 