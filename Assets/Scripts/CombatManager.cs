using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CombatManager : MonoBehaviour
{
    public List<CombatStats> players;
    public List<CombatStats> enemies;
    public CombatUI combatUI;
    public float actionBarFillRate = 20f; // Points per second

    private CombatStats activeCharacter;
    private bool isCombatActive = true;
    private bool isWaitingForPlayerInput = false;

    private void Start()
    {
        // Initialize lists if they're null
        if (players == null) players = new List<CombatStats>();
        if (enemies == null) enemies = new List<CombatStats>();
        
        // Remove any null entries
        players.RemoveAll(p => p == null);
        enemies.RemoveAll(e => e == null);
        
        // Show the menu but disable interaction
        if (combatUI != null)
        {
            // Always show the menu
            combatUI.actionMenu.SetActive(true);
            // Just disable interaction
            combatUI.GetComponent<MenuSelector>().SetMenuItemsEnabled(false);
        }
    }

    private void Update()
    {
        if (!isCombatActive || isWaitingForPlayerInput) return;

        // Update action bars with null check
        foreach (var character in players.Concat(enemies).Where(c => c != null))
        {
            if (character.IsDead()) continue;

            character.currentAction += actionBarFillRate * Time.deltaTime;

            if (character.currentAction >= character.maxAction)
            {
                character.currentAction = character.maxAction; // Cap it
                StartTurn(character);
                break; // Process one turn at a time
            }
        }

        // Update UI
        if (combatUI != null)
        {
            combatUI.UpdateUI();
        }

        // Check win/lose conditions
        CheckBattleEnd();
    }

    private void StartTurn(CombatStats character)
    {
        activeCharacter = character;
        
        if (character.isEnemy)
        {
            ExecuteEnemyTurn(character);
        }
        else
        {
            isWaitingForPlayerInput = true;
            character.HighlightCharacter(true);
            combatUI.ShowActionMenu(character);
        }
    }

    private void ExecuteEnemyTurn(CombatStats enemy)
    {
        // Find player with lowest HP
        var target = players
            .Where(p => !p.IsDead())
            .OrderBy(p => p.currentHealth)
            .FirstOrDefault();

        if (target != null)
        {
            target.TakeDamage(30f);
        }

        enemy.currentAction = 0;
    }

    public List<CombatStats> GetLivingEnemies()
    {
        return enemies.Where(e => e != null && !e.IsDead()).ToList();
    }

    public void ExecutePlayerAction(string action, CombatStats target = null)
    {
        if (activeCharacter == null || activeCharacter.isEnemy) return;

        switch (action.ToLower())
        {
            case "attack":
                if (target != null)
                {
                    target.TakeDamage(10f);
                    if (target.IsDead())
                    {
                        // Remove the enemy from the list and destroy it
                        enemies.Remove(target);
                        Destroy(target.gameObject);
                    }
                }
                break;

            case "heal":
                if (activeCharacter.currentSanity >= 10f)
                {
                    activeCharacter.HealHealth(10f);
                    activeCharacter.UseSanity(10f);
                }
                break;
        }

        EndPlayerTurn();
    }

    private void EndPlayerTurn()
    {
        if (activeCharacter != null)
        {
            activeCharacter.HighlightCharacter(false);
            activeCharacter.currentAction = 0;
        }
        
        // Disable menu items but keep menu visible
        combatUI.GetComponent<MenuSelector>().SetMenuItemsEnabled(false);
        isWaitingForPlayerInput = false;
        activeCharacter = null;
    }

    private void CheckBattleEnd()
    {
        if (players.All(p => p.IsDead()))
        {
            isCombatActive = false;
            Debug.Log("Game Over - Players Defeated");
        }
        else if (enemies.All(e => e.IsDead()))
        {
            isCombatActive = false;
            Debug.Log("Victory - All Enemies Defeated");
        }
    }
} 