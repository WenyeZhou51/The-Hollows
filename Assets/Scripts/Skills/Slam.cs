using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Slam", menuName = "Skills/Slam")]
public class Slam : BaseSkill
{
    [SerializeField] private float damage = 10f;
    
    private void OnEnable()
    {
        Name = "Slam!";
        Description = "Deal 10 damage to all enemies";
        SPCost = 0f;
        RequiresTarget = false;
    }
    
    public override void Use(CombatStats user, CombatStats target = null)
    {
        // This skill affects all enemies, so we need to find them
        CombatManager combatManager = GameObject.FindObjectOfType<CombatManager>();
        if (combatManager != null)
        {
            // Get all enemies
            List<CombatStats> enemies = new List<CombatStats>(combatManager.enemies);
            
            foreach (CombatStats enemy in enemies)
            {
                if (enemy != null && !enemy.IsDead())
                {
                    enemy.TakeDamage(damage);
                    Debug.Log($"{Name} used: Hit {enemy.name} for {damage} damage");
                }
            }
            
            // Deduct sanity cost
            user.UseSanity(SPCost);
        }
        else
        {
            Debug.LogWarning("Could not find Combat Manager to use Slam! skill");
        }
    }
} 