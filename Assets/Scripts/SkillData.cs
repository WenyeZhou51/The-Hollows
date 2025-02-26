using UnityEngine;

[System.Serializable]
public class SkillData
{
    public string name;
    public string description;
    public float sanityCost;
    public bool requiresTarget;
    
    public SkillData(string name, string description, float sanityCost, bool requiresTarget)
    {
        this.name = name;
        this.description = description;
        this.sanityCost = sanityCost;
        this.requiresTarget = requiresTarget;
    }
} 