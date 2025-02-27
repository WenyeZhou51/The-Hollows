using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;  // Add this for GridLayoutGroup
using System.Linq;

public class CombatManager : MonoBehaviour
{
    [Header("Required References")]
    public GameObject skillMenu;  // Only required reference for skills
    public GameObject itemMenu;   // Only required reference for items
    public GameObject buttonPrefab; // Single button prefab for both menus

    // Auto-found references
    private CombatUI combatUI;
    private MenuSelector menuSelector;
    private GameObject characterStatsPanel;
    private RectTransform menuButtonsContainer;
    private GridLayoutGroup menuButtonsGrid;

    public List<CombatStats> players;
    public List<CombatStats> enemies;
    public float actionBarFillRate = 20f; // Points per second

    private CombatStats activeCharacter;
    public CombatStats ActiveCharacter => activeCharacter;
    private bool isCombatActive = true;
    private bool isWaitingForPlayerInput = false;

    private void Start()
    {
        // Find required components
        combatUI = GetComponent<CombatUI>();
        menuSelector = GetComponent<MenuSelector>();
        
        // Find UI elements by name instead of tag
        characterStatsPanel = GameObject.Find("CharacterStatsPanel");
        if (characterStatsPanel == null)
            Debug.LogWarning("Character Stats Panel not found! Make sure it's named 'CharacterStatsPanel'");

        // Setup menu containers
        if (skillMenu != null)
        {
            menuButtonsContainer = skillMenu.GetComponentInChildren<RectTransform>();
            menuButtonsGrid = skillMenu.GetComponentInChildren<GridLayoutGroup>();
            if (menuButtonsGrid == null)
            {
                menuButtonsGrid = menuButtonsContainer.gameObject.AddComponent<GridLayoutGroup>();
                menuButtonsGrid.cellSize = new Vector2(120, 40);
                menuButtonsGrid.spacing = new Vector2(10, 10);
                menuButtonsGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                menuButtonsGrid.constraintCount = 2;
            }
            skillMenu.SetActive(false);
        }

        if (itemMenu != null)
        {
            // Use same grid settings for item menu
            var itemGrid = itemMenu.GetComponentInChildren<GridLayoutGroup>();
            if (itemGrid == null)
            {
                itemGrid = itemMenu.GetComponentInChildren<RectTransform>().gameObject.AddComponent<GridLayoutGroup>();
                itemGrid.cellSize = menuButtonsGrid.cellSize;
                itemGrid.spacing = menuButtonsGrid.spacing;
                itemGrid.constraint = menuButtonsGrid.constraint;
                itemGrid.constraintCount = menuButtonsGrid.constraintCount;
            }
            itemMenu.SetActive(false);
        }

        // Initialize lists
        if (players == null) players = new List<CombatStats>();
        if (enemies == null) enemies = new List<CombatStats>();
        
        players.RemoveAll(p => p == null);
        enemies.RemoveAll(e => e == null);
        
        // Initial UI setup
        if (combatUI != null)
        {
            combatUI.actionMenu.SetActive(true);
            menuSelector.SetMenuItemsEnabled(false);
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
            UpdateUI();
        }

        // Check win/lose conditions
        CheckBattleEnd();
    }

    private void StartTurn(CombatStats character)
    {
        if (character.IsDead()) return;

        // Set the active character
        activeCharacter = character;
        
        // Clear any guarding status if this character was guarding someone
        if (character.ProtectedAlly != null)
        {
            Debug.Log($"[Human Shield] {character.name}'s turn has come - stopping guard on {character.ProtectedAlly.name}");
            character.StopGuarding();
        }
        
        // Set the active character property and reset others
        foreach (var c in players.Concat(enemies))
        {
            if (c != null)
            {
                // Set IsActiveCharacter property instead of using highlight
                c.IsActiveCharacter = (c == character);
            }
        }
        
        // Update UI
        UpdateUI();
        
        // Handle turn based on character type
        if (character.isEnemy)
        {
            // Enemy turn
            ExecuteEnemyTurn(character);
        }
        else
        {
            // Player turn - show action menu
            isWaitingForPlayerInput = true;
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

        bool actionExecuted = false;

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
                    actionExecuted = true;
                }
                break;

            case "heal":
                if (activeCharacter.currentSanity >= 10f)
                {
                    activeCharacter.HealHealth(10f);
                    activeCharacter.UseSanity(10f);
                    actionExecuted = true;
                }
                break;
        }

        // Only end the turn if an action was actually executed
        if (actionExecuted)
        {
            EndPlayerTurn();
        }
        else
        {
            // If no action was executed, reset the menu state
            menuSelector.ResetMenuState();
            combatUI.ShowActionMenu(activeCharacter);
        }
    }

    public void EndPlayerTurn()
    {
        if (activeCharacter != null)
        {
            // Reset the active character property instead of using highlight
            activeCharacter.IsActiveCharacter = false;
            activeCharacter.currentAction = 0;
        }
        
        // Properly disable the menu
        MenuSelector menuSelector = GetComponent<MenuSelector>();
        if (menuSelector != null)
        {
            menuSelector.DisableMenu();
        }
        
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

    private void UpdateUI()
    {
        // Combine players and enemies into a single array for UI update
        var allCharacters = players.Concat(enemies).ToArray();
        combatUI.UpdateCharacterUI(allCharacters, activeCharacter);
    }
} 