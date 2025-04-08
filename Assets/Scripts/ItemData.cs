using UnityEngine;

[System.Serializable]
public class ItemData
{
    public enum ItemType
    {
        Consumable,
        Equipment,
        KeyItem
    }
    
    public string name;
    public string description;
    public int amount;
    public bool requiresTarget;
    public ItemType type;
    public Sprite icon;
    
    public ItemData(string name, string description, int amount, bool requiresTarget, ItemType type = ItemType.Consumable, Sprite icon = null)
    {
        this.name = name;
        this.description = description;
        this.amount = amount;
        this.requiresTarget = requiresTarget;
        this.type = type;
        this.icon = icon;
    }
    
    // Check if this item is a key item
    public bool IsKeyItem()
    {
        return type == ItemType.KeyItem;
    }
    
    // Clone method to ensure proper copying of all properties including type
    public ItemData Clone()
    {
        return new ItemData(name, description, amount, requiresTarget, type, icon);
    }
} 