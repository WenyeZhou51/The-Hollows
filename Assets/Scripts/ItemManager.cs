using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    private static ItemManager _instance;
    public static ItemManager Instance => _instance;
    
    private Dictionary<string, BaseItem> itemTemplates = new Dictionary<string, BaseItem>();
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Load all item scriptable objects
        LoadItems();
    }
    
    private void LoadItems()
    {
        // Find all item scriptable objects in the Resources folder
        BaseItem[] items = Resources.LoadAll<BaseItem>("Items");
        
        foreach (BaseItem item in items)
        {
            if (!itemTemplates.ContainsKey(item.Name))
            {
                itemTemplates.Add(item.Name, item);
                Debug.Log($"Loaded item: {item.Name}");
            }
            else
            {
                Debug.LogWarning($"Duplicate item found: {item.Name}");
            }
        }
    }
    
    public BaseItem GetItem(string itemName)
    {
        if (itemTemplates.TryGetValue(itemName, out BaseItem item))
        {
            return item;
        }
        
        Debug.LogWarning($"Item not found: {itemName}");
        return null;
    }
    
    // Helper method to convert BaseItem to ItemData for backwards compatibility
    public ItemData GetItemData(string itemName, int amount = 1)
    {
        BaseItem item = GetItem(itemName);
        if (item != null)
        {
            return item.ToItemData(amount);
        }
        
        Debug.LogWarning($"ItemData not created: Item {itemName} not found");
        return null;
    }
    
    // Helper method for item use
    public void UseItem(string itemName, CombatStats user, CombatStats target = null)
    {
        BaseItem item = GetItem(itemName);
        if (item != null)
        {
            item.Use(user, target);
        }
        else
        {
            Debug.LogWarning($"Cannot use item: {itemName} not found");
        }
    }
} 