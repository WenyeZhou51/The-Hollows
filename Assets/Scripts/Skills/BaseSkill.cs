using UnityEngine;

public abstract class BaseSkill : ScriptableObject
{
    public string Name;
    public string Description;
    public float SPCost;
    public bool RequiresTarget;
    
    // Method that will be implemented by specific skills
    public abstract void Use(CombatStats user, CombatStats target = null);
    
    // Convert to SkillData for backwards compatibility
    public SkillData ToSkillData()
    {
        return new SkillData(Name, Description, SPCost, RequiresTarget);
    }
} 