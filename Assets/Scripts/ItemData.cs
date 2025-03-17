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
} 