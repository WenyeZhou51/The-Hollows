using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private List<ItemData> items = new List<ItemData>();
    
    public delegate void InventoryChangedHandler();
    public event InventoryChangedHandler OnInventoryChanged;
    
    public List<ItemData> Items => items;
    
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