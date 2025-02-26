using UnityEngine;

public class SkillButtonData : MonoBehaviour
{
    public SkillData skill;
    
    private void Awake()
    {
        Debug.Log($"[SkillButton Lifecycle] SkillButtonData Awake - GameObject: {gameObject.name}");
    }
    
    private void OnEnable()
    {
        Debug.Log($"[SkillButton Lifecycle] SkillButtonData OnEnable - GameObject: {gameObject.name}, Skill: {(skill != null ? skill.name : "not set yet")}");
    }
    
    private void OnDisable()
    {
        Debug.Log($"[SkillButton Lifecycle] SkillButtonData OnDisable - GameObject: {gameObject.name}, Skill: {(skill != null ? skill.name : "not set")}");
    }
    
    private void OnDestroy()
    {
        Debug.Log($"[SkillButton Lifecycle] SkillButtonData OnDestroy - GameObject: {gameObject.name}, Skill: {(skill != null ? skill.name : "not set")}");
    }
} 