using System.Collections.Generic;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    private static SkillManager _instance;
    public static SkillManager Instance => _instance;
    
    private Dictionary<string, BaseSkill> skillTemplates = new Dictionary<string, BaseSkill>();
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Load all skill scriptable objects
        LoadSkills();
    }
    
    private void LoadSkills()
    {
        // Find all skill scriptable objects in the Resources folder
        BaseSkill[] skills = Resources.LoadAll<BaseSkill>("Skills");
        
        foreach (BaseSkill skill in skills)
        {
            if (!skillTemplates.ContainsKey(skill.Name))
            {
                skillTemplates.Add(skill.Name, skill);
                Debug.Log($"Loaded skill: {skill.Name}");
            }
            else
            {
                Debug.LogWarning($"Duplicate skill found: {skill.Name}");
            }
        }
    }
    
    public BaseSkill GetSkill(string skillName)
    {
        if (skillTemplates.TryGetValue(skillName, out BaseSkill skill))
        {
            return skill;
        }
        
        Debug.LogWarning($"Skill not found: {skillName}");
        return null;
    }
    
    // Helper method to convert BaseSkill to SkillData for backwards compatibility
    public SkillData GetSkillData(string skillName)
    {
        BaseSkill skill = GetSkill(skillName);
        if (skill != null)
        {
            return skill.ToSkillData();
        }
        
        Debug.LogWarning($"SkillData not created: Skill {skillName} not found");
        return null;
    }
    
    // Get skills for a specific character
    public List<SkillData> GetSkillsForCharacter(string characterName)
    {
        List<SkillData> characterSkills = new List<SkillData>();
        
        switch (characterName)
        {
            case "The Magician":
                characterSkills.Add(GetSkillData("Before Your Eyes"));
                characterSkills.Add(GetSkillData("Fiend Fire"));
                characterSkills.Add(GetSkillData("Disappearing Trick"));
                characterSkills.Add(GetSkillData("Take a Break!"));
                break;
                
            case "The Fighter":
                characterSkills.Add(GetSkillData("Slam!"));
                characterSkills.Add(GetSkillData("Human Shield!"));
                break;
                
            case "The Bard":
                characterSkills.Add(GetSkillData("Healing Words"));
                break;
                
            case "The Ranger":
                characterSkills.Add(GetSkillData("Piercing Shot"));
                break;
                
            default:
                Debug.LogWarning($"No skills defined for character: {characterName}");
                break;
        }
        
        return characterSkills;
    }
    
    // Helper method for skill use
    public void UseSkill(string skillName, CombatStats user, CombatStats target = null)
    {
        BaseSkill skill = GetSkill(skillName);
        if (skill != null)
        {
            skill.Use(user, target);
        }
        else
        {
            Debug.LogWarning($"Cannot use skill: {skillName} not found");
        }
    }
} 