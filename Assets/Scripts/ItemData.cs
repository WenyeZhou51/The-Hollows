using UnityEngine;

[System.Serializable]
public class ItemData
{
    public string name;
    public string description;
    public int amount;
    public bool requiresTarget;
    
    public ItemData(string name, string description, int amount, bool requiresTarget)
    {
        this.name = name;
        this.description = description;
        this.amount = amount;
        this.requiresTarget = requiresTarget;
    }
} 