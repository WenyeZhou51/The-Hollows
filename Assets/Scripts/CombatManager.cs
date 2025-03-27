using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;  // Add this for GridLayoutGroup
using System.Linq;
using System;
using System.Collections;

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
    
    // Event for when combat ends
    public delegate void CombatEndHandler(bool playerWon);
    public event CombatEndHandler OnCombatEnd;

    // Player inventory for combat
    private List<ItemData> playerInventoryItems;

    private void Start()
    {
        // Initialize from SceneTransitionManager's inventory
        if (SceneTransitionManager.Instance != null)
        {
            playerInventoryItems = SceneTransitionManager.Instance.GetPlayerInventory();
        }

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

            // Use each character's individual actionSpeed instead of the global actionBarFillRate
            character.currentAction += character.actionSpeed * Time.deltaTime;

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
        
        // Deactivate guard stance if active
        if (character.IsGuarding)
        {
            character.DeactivateGuard();
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
        
        // Display turn message
        if (combatUI != null && combatUI.turnText != null)
        {
            combatUI.turnText.text = $"It's {character.characterName}'s Turn";
        }
        
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
        // Display action message
        if (combatUI != null && combatUI.turnText != null)
        {
            combatUI.DisplayTurnAndActionMessage($"{enemy.characterName} attacks!");
        }
        
        // Find player with lowest HP
        var target = players
            .Where(p => !p.IsDead())
            .OrderBy(p => p.currentHealth)
            .FirstOrDefault();

        if (target != null)
        {
            target.TakeDamage(30f);
        }

        // Update speed boost duration at the end of turn
        enemy.UpdateSpeedBoostDuration();
        
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
                    // Display just "Attack" in the action display label
                    if (combatUI != null)
                    {
                        combatUI.DisplayActionLabel("Attack");
                    }
                    
                    // Start a coroutine to wait for the message to complete before dealing damage
                    StartCoroutine(ExecuteAttackAfterMessage(target));
                    actionExecuted = true;
                }
                break;

            case "heal":
                if (activeCharacter.currentSanity >= 10f)
                {
                    // Display just "Heal" in the action display label
                    if (combatUI != null)
                    {
                        combatUI.DisplayActionLabel("Heal");
                    }
                    
                    // Start a coroutine to wait for the message to complete before healing
                    StartCoroutine(ExecuteHealAfterMessage());
                    actionExecuted = true;
                }
                break;
        }

        // Only end the turn if an action was executed (and will be handled by the coroutines)
        if (!actionExecuted)
        {
            // If no action was executed, reset the menu state
            menuSelector.ResetMenuState();
            combatUI.ShowActionMenu(activeCharacter);
        }
    }
    
    private IEnumerator ExecuteAttackAfterMessage(CombatStats target)
    {
        // Wait a tiny amount just to ensure the action label coroutine has started
        yield return null;
        
        // Wait for the game to resume (after action display is done)
        while (Time.timeScale == 0)
            yield return null;
            
        target.TakeDamage(10f);
        if (target.IsDead())
        {
            // Remove the enemy from the list and destroy it
            enemies.Remove(target);
            Destroy(target.gameObject);
        }
        
        EndPlayerTurn();
    }
    
    private IEnumerator ExecuteHealAfterMessage()
    {
        // Wait a tiny amount just to ensure the action label coroutine has started
        yield return null;
        
        // Wait for the game to resume (after action display is done)
        while (Time.timeScale == 0)
            yield return null;
            
        activeCharacter.HealHealth(10f);
        activeCharacter.UseSanity(10f);
        
        EndPlayerTurn();
    }

    public void EndPlayerTurn()
    {
        if (activeCharacter != null)
        {
            // Update speed boost duration at the end of turn
            activeCharacter.UpdateSpeedBoostDuration();
            
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
        // Check if all enemies are defeated
        bool allEnemiesDefeated = enemies.Count == 0 || enemies.All(e => e == null || e.IsDead());
        
        // Check if all players are defeated
        bool allPlayersDefeated = players.Count == 0 || players.All(p => p == null || p.IsDead());
        
        if (allEnemiesDefeated || allPlayersDefeated)
        {
            // End combat
            isCombatActive = false;
            
            // Determine winner
            bool playerWon = allEnemiesDefeated;
            
            // Update SceneTransitionManager with current inventory before ending combat
            if (SceneTransitionManager.Instance != null)
            {
                Debug.Log($"Combat ending: Saving {playerInventoryItems.Count} items to SceneTransitionManager");
                SceneTransitionManager.Instance.SetPlayerInventory(playerInventoryItems);
            }
            
            // Trigger combat end event
            if (OnCombatEnd != null)
            {
                OnCombatEnd(playerWon);
            }
            else
            {
                // If no listeners are registered, use SceneTransitionManager
                if (SceneTransitionManager.Instance != null)
                {
                    SceneTransitionManager.Instance.EndCombat(playerWon);
                }
                else
                {
                    Debug.LogWarning("Combat ended but no listeners or SceneTransitionManager found!");
                }
            }
        }
    }

    private void UpdateUI()
    {
        // Combine players and enemies into a single array for UI update
        var allCharacters = players.Concat(enemies).ToArray();
        combatUI.UpdateCharacterUI(allCharacters, activeCharacter);
    }

    /// <summary>
    /// Sets up the player's inventory for combat
    /// </summary>
    /// <param name="inventoryItems">The items in the player's inventory</param>
    public void SetupPlayerInventory(List<ItemData> inventoryItems)
    {
        // Store a copy of the inventory items
        playerInventoryItems = new List<ItemData>();
        
        Debug.Log("=== INVENTORY DEBUG: CombatManager.SetupPlayerInventory ===");
        
        if (inventoryItems != null)
        {
            Debug.Log($"Received {inventoryItems.Count} items from SceneTransitionManager");
            
            foreach (ItemData item in inventoryItems)
            {
                playerInventoryItems.Add(item);
                Debug.Log($"Added to combat inventory: {item.name}, Amount: {item.amount}");
            }
            
            Debug.Log($"Combat Manager received {playerInventoryItems.Count} items from player inventory");
            
            // Update the item menu if it exists
            if (itemMenu != null && combatUI != null)
            {
                Debug.Log("Populating item menu with current inventory items");
                combatUI.PopulateItemMenu(playerInventoryItems);
            }
            else
            {
                Debug.LogWarning("Could not populate item menu: itemMenu or combatUI is null");
            }
        }
        else
        {
            Debug.LogWarning("Received null inventory from SceneTransitionManager");
        }
    }
    
    /// <summary>
    /// Gets the player's inventory items
    /// </summary>
    public List<ItemData> GetPlayerInventoryItems()
    {
        if (playerInventoryItems == null)
        {
            Debug.LogWarning("Combat inventory is null when requested");
            playerInventoryItems = new List<ItemData>();
        }
        
        Debug.Log($"GetPlayerInventoryItems returning {playerInventoryItems.Count} items");
        foreach (var item in playerInventoryItems)
        {
            Debug.Log($"Current combat inventory item: {item.name}, Amount: {item.amount}");
        }
        
        return playerInventoryItems;
    }
} 