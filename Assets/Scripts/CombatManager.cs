using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;  // Add this for GridLayoutGroup
using System.Linq;
using System;
using System.Collections;
using UnityEngine.SceneManagement;

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
    
    // Flag to track if the item menu is currently active
    public bool isItemMenuActive = false;
    
    // Add a turn counter to track player turns
    private int playerTurnCount = 0;
    
    // Event for when combat ends
    public delegate void CombatEndHandler(bool playerWon);
    public event CombatEndHandler OnCombatEnd;

    // Player inventory for combat
    private List<ItemData> playerInventoryItems;

    // Add field for dialogue system integration
    private BattleDialogueTrigger battleDialogueTrigger;

    // Flag to track if we're in a phase transition
    private bool isInPhaseTransition = false;
    private bool isCombatEnded = false;

    private void Awake()
    {
        // Initialize lists
        if (players == null) players = new List<CombatStats>();
        if (enemies == null) enemies = new List<CombatStats>();
        
        players.RemoveAll(p => p == null);
        enemies.RemoveAll(e => e == null);
        
        // Log enemy information for debugging
        Debug.Log($"===== COMBAT INITIALIZATION: FOUND {enemies.Count} ENEMIES =====");
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != null)
            {
                Debug.Log($"Enemy {i}: {enemies[i].characterName} - Health: {enemies[i].currentHealth}/{enemies[i].maxHealth} - IsDead: {enemies[i].IsDead()} - Has Behavior: {enemies[i].enemyBehavior != null}");
            }
            else
            {
                Debug.Log($"Enemy {i}: NULL REFERENCE");
            }
        }

        // Initialize character stats from PersistentGameManager BEFORE Start() is called
        InitializeCharacterStats();

        // Find the battle dialogue trigger
        battleDialogueTrigger = FindObjectOfType<BattleDialogueTrigger>();
    }

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
        
        // Initial UI setup
        if (combatUI != null)
        {
            combatUI.actionMenu.SetActive(true);
            menuSelector.SetMenuItemsEnabled(false);
        }
    }

    // Initialize character health and mind values from PersistentGameManager
    private void InitializeCharacterStats()
    {
        // Make sure PersistentGameManager exists
        if (PersistentGameManager.Instance == null)
        {
            Debug.LogWarning("PersistentGameManager not found when initializing combat stats");
            return;
        }

        Debug.Log("===== INITIALIZING CHARACTER STATS FROM PERSISTENT MANAGER =====");
        foreach (var playerStat in players)
        {
            if (playerStat == null) continue;

            // Log default values from CombatStats before modification
            Debug.Log($"Default values for {playerStat.characterName ?? "unnamed character"} before initialization: Health {playerStat.currentHealth}/{playerStat.maxHealth}, Mind {playerStat.currentSanity}/{playerStat.maxSanity}");

            string characterId = playerStat.characterName;
            if (string.IsNullOrEmpty(characterId))
            {
                Debug.LogWarning($"Character has no name, cannot retrieve persistent stats");
                continue;
            }

            // Get health values from PersistentGameManager
            float currentHealth = PersistentGameManager.Instance.GetCharacterHealth(characterId, (int)playerStat.maxHealth);
            float maxHealth = PersistentGameManager.Instance.GetCharacterMaxHealth(characterId);
            
            // Get mind/sanity values from PersistentGameManager
            float currentMind = PersistentGameManager.Instance.GetCharacterMind(characterId, (int)playerStat.maxSanity);
            float maxMind = PersistentGameManager.Instance.GetCharacterMaxMind(characterId);

            Debug.Log($"Retrieved values from PersistentGameManager for {characterId}: Health {currentHealth}/{maxHealth}, Mind {currentMind}/{maxMind}");

            // Apply values to combat stats - force the values to be applied
            playerStat.maxHealth = maxHealth;
            playerStat.currentHealth = currentHealth;
            playerStat.maxSanity = maxMind;
            playerStat.currentSanity = currentMind;

            // Log the final values after setting them
            Debug.Log($"Final initialized stats for {characterId}: Health {playerStat.currentHealth}/{playerStat.maxHealth}, Mind {playerStat.currentSanity}/{playerStat.maxSanity}");
        }
    }

    private void Update()
    {
        // Skip combat processing if dialogue is active
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive())
            return;
            
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
        
        // Handle turn based on character type
        if (character.isEnemy)
        {
            // Reset action points BEFORE starting the enemy turn to prevent multiple turns
            character.currentAction = 0;
            
            // Enemy turn - show the text panel with the obelisk message
            if (combatUI != null)
            {
                combatUI.ShowTextPanel("The obelisk focuses on you", 0.5f);
            }
            
            // Enemy turn - use the enemy's behavior component if available
            StartCoroutine(ExecuteEnemyTurn(character));
        }
        else
        {
            // Player turn - increment turn counter
            playerTurnCount++;
            
            // Display different messages based on the turn count
            string turnMessage;
            if (playerTurnCount == 1)
            {
                turnMessage = "You've reached the end.";
            }
            else if (playerTurnCount == 2)
            {
                turnMessage = "The Obelisk towers over you";
            }
            else
            {
                turnMessage = "The Obelisk looms";
            }
            
            // Show the text panel with the appropriate message
            if (combatUI != null)
            {
                combatUI.ShowTextPanel(turnMessage, 0.5f);
            }
            
            // Player turn - show action menu
            isWaitingForPlayerInput = true;
            combatUI.ShowActionMenu(character);
        }
    }

    private IEnumerator ExecuteEnemyTurn(CombatStats enemy)
    {
        // Check if the enemy has a behavior script attached
        if (enemy.enemyBehavior != null)
        {
            // Use the enemy's behavior script
            yield return enemy.enemyBehavior.ExecuteTurn(enemy, players, combatUI);
        }
        else
        {
            // Fallback to basic attack if no behavior script is attached
            yield return ExecuteBasicEnemyAttack(enemy);
        }
        
        // The action points have already been reset before starting the turn
    }
    
    private IEnumerator ExecuteBasicEnemyAttack(CombatStats enemy)
    {
        // Display generic attack message
        if (combatUI != null && combatUI.turnText != null)
        {
            combatUI.DisplayTurnAndActionMessage($"{enemy.characterName} attacks!");
        }
        
        // Display "Basic Attack" in action display label
        if (combatUI != null)
        {
            combatUI.DisplayActionLabel("Basic Attack");
        }
        
        // Wait for action display to complete
        yield return new WaitForSeconds(0.1f); // Small delay
        while (Time.timeScale == 0)
            yield return null;
        
        // Find player with lowest HP
        var target = players
            .Where(p => !p.IsDead())
            .OrderBy(p => p.currentHealth)
            .FirstOrDefault();

        if (target != null)
        {
            // Base damage
            float baseDamage = 30f;
            
            // Round down to whole number
            int finalDamage = Mathf.FloorToInt(baseDamage);
            
            target.TakeDamage(finalDamage);
        }
        
        // Update speed boost duration at the end of turn
        enemy.UpdateSpeedBoostDuration();
    }

    public List<CombatStats> GetLivingEnemies()
    {
        return enemies.Where(e => e != null && !e.IsDead() && e.gameObject.activeSelf).ToList();
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
                    // Hide the text panel when player selects an action
                    if (combatUI != null)
                    {
                        combatUI.HideTextPanel();
                    }
                    
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
                    // Hide the text panel when player selects an action
                    if (combatUI != null)
                    {
                        combatUI.HideTextPanel();
                    }
                    
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
            
        // Base damage
        float baseDamage = 10f;
        
        // Apply 20% variance (80-120% of base damage)
        float variance = UnityEngine.Random.Range(0.8f, 1.2f);
        float damageWithVariance = baseDamage * variance;
        
        // Round down to whole number
        int finalDamage = Mathf.FloorToInt(damageWithVariance);
        
        // Deal the damage
        target.TakeDamage(finalDamage);
        
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
            
        // Base heal amount
        float baseHealAmount = 10f;
        float baseSanityCost = 10f;
        
        // Round down to whole numbers
        int finalHealAmount = Mathf.FloorToInt(baseHealAmount);
        int finalSanityCost = Mathf.FloorToInt(baseSanityCost);
        
        activeCharacter.HealHealth(finalHealAmount);
        activeCharacter.UseSanity(finalSanityCost);
        
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

        // Increment turn counter after a player's turn
        playerTurnCount++;
        
        // Check for any dialogue triggers on this turn
        if (battleDialogueTrigger != null)
        {
            battleDialogueTrigger.CheckTurnBasedTriggers(playerTurnCount);
        }
    }

    private void CheckBattleEnd()
    {
        // Skip battle end check if we're in a phase transition
        if (isInPhaseTransition) return;
    
        // Log enemy status before checking end conditions
        Debug.Log("===== CHECKING BATTLE END CONDITIONS =====");
        foreach (var enemy in enemies)
        {
            if (enemy == null)
            {
                Debug.Log("Enemy is NULL");
            }
            else
            {
                Debug.Log($"Enemy {enemy.characterName} - Health: {enemy.currentHealth}/{enemy.maxHealth} - IsDead: {enemy.IsDead()} - Active: {enemy.gameObject.activeSelf}");
            }
        }
        
        // Check if all players are dead (lose condition)
        bool allPlayersDead = players.All(p => p == null || p.IsDead());
        
        // Check if all enemies are dead (win condition)
        // Consider enemies that are inactive (faded out) as dead
        bool allEnemiesDead = enemies.All(e => e == null || e.IsDead() || !e.gameObject.activeSelf);
        
        Debug.Log($"Battle end check: All players dead? {allPlayersDead} - All enemies dead? {allEnemiesDead}");
        
        if (allPlayersDead || allEnemiesDead)
        {
            // Stop the combat
            isCombatActive = false;
            
            // Save character stats before ending combat
            SaveCharacterStats();
            
            // Determine winner
            bool playerWon = allEnemiesDead;
            
            // Update SceneTransitionManager with current inventory before ending combat
            if (SceneTransitionManager.Instance != null)
            {
                // Get the original inventory from SceneTransitionManager to retrieve key items
                List<ItemData> originalInventory = SceneTransitionManager.Instance.GetPlayerInventory();
                List<ItemData> keyItems = new List<ItemData>();
                
                // Extract key items from the original inventory
                foreach (var item in originalInventory)
                {
                    if (item.IsKeyItem())
                    {
                        Debug.Log($"Preserving key item after combat: {item.name}");
                        keyItems.Add(item.Clone());
                    }
                }
                
                // Get inventory items from each player character's items
                List<ItemData> combinedInventory = new List<ItemData>();
                
                // Only get items from the first character since all characters share the same inventory
                if (players.Count > 0 && players[0] != null)
                {
                    foreach (var item in players[0].items)
                    {
                        combinedInventory.Add(item);
                    }
                }
                
                // Add back the key items to ensure they're preserved
                foreach (var keyItem in keyItems)
                {
                    // Check if the item already exists to avoid duplicates
                    bool exists = false;
                    foreach (var item in combinedInventory)
                    {
                        if (item.name == keyItem.name)
                        {
                            exists = true;
                            break;
                        }
                    }
                    
                    if (!exists)
                    {
                        Debug.Log($"Adding key item back to inventory after combat: {keyItem.name}");
                        combinedInventory.Add(keyItem);
                    }
                }
                
                SceneTransitionManager.Instance.SetPlayerInventory(combinedInventory);
            }
            
            // Trigger the appropriate combat end event
            if (allPlayersDead)
            {
                Debug.Log("Combat ended: All players are defeated!");
                // Display defeat message
                if (combatUI != null)
                {
                    combatUI.ShowTextPanel("Defeat!", 2f);
                }
                
                // Trigger the combat end event after a delay
                Invoke("TriggerDefeat", 2f);
                isCombatEnded = true;
            }
            else
            {
                Debug.Log("Combat ended: All enemies are defeated!");
                // Display victory message
                if (combatUI != null)
                {
                    combatUI.ShowTextPanel("You are victorious", 1f);
                }
                
                // Set flag for phase transition and combat end notification
                isInPhaseTransition = true;
                
                // Trigger the combat end event after a delay
                Invoke("TriggerVictory", 1f);
            }
        }
    }

    private void TriggerVictory()
    {
        if (OnCombatEnd != null && !isCombatEnded)
        {
            OnCombatEnd(true);
        }
    }
    
    private void TriggerDefeat()
    {
        // Increment the death counter in PersistentGameManager
        if (PersistentGameManager.Instance != null)
        {
            PersistentGameManager.Instance.IncrementDeaths();
        }

        // Instead of calling OnCombatEnd, load the start menu directly
        Debug.Log("Player was defeated! Returning to start menu.");
        
        // Make sure ScreenFader exists
        ScreenFader.EnsureExists();
        
        // Start the transition to the start menu with fade effect
        StartCoroutine(TransitionToStartMenu());
    }
    
    /// <summary>
    /// Transition to the start menu after player defeat
    /// </summary>
    private IEnumerator TransitionToStartMenu()
    {
        // Fade to black
        yield return StartCoroutine(ScreenFader.Instance.FadeToBlack());
        
        // Register for scene loaded event BEFORE loading scene
        SceneManager.sceneLoaded += OnStartMenuSceneLoaded;
        
        // Load the start menu scene
        SceneManager.LoadScene("Start_Menu");
    }
    
    /// <summary>
    /// Called when the start menu scene has loaded
    /// </summary>
    private void OnStartMenuSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("Start Menu loaded, fading in from black");
        
        // Make sure the ScreenFader still exists
        if (ScreenFader.Instance != null)
        {
            // Fade from black once the scene is loaded
            StartCoroutine(ScreenFader.Instance.FadeFromBlack());
        }
        else
        {
            Debug.LogError("ScreenFader not found after loading Start_Menu scene!");
        }
        
        // Unregister the event to prevent memory leaks
        SceneManager.sceneLoaded -= OnStartMenuSceneLoaded;
    }

    // Save character stats to PersistentGameManager
    private void SaveCharacterStats()
    {
        // Make sure PersistentGameManager exists
        if (PersistentGameManager.Instance == null)
        {
            Debug.LogWarning("PersistentGameManager not found when saving combat stats");
            return;
        }

        foreach (var playerStat in players)
        {
            if (playerStat == null || playerStat.IsDead()) continue;

            string characterId = playerStat.characterName;
            if (string.IsNullOrEmpty(characterId))
            {
                Debug.LogWarning($"Character has no name, cannot save persistent stats");
                continue;
            }

            // Save character stats using SaveCharacterStats method
            PersistentGameManager.Instance.SaveCharacterStats(
                characterId,
                (int)playerStat.currentHealth,
                (int)playerStat.maxHealth,
                (int)playerStat.currentSanity,
                (int)playerStat.maxSanity
            );

            Debug.Log($"Saved combat stats for {characterId}: Health {playerStat.currentHealth}/{playerStat.maxHealth}, Mind {playerStat.currentSanity}/{playerStat.maxSanity}");
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
        // Initialize or clear inventory items list
        if (playerInventoryItems == null)
        {
            playerInventoryItems = new List<ItemData>();
        }
        else
        {
            playerInventoryItems.Clear();
        }
        
        Debug.Log("=== INVENTORY DEBUG: CombatManager.SetupPlayerInventory ===");
        
        if (inventoryItems != null && inventoryItems.Count > 0)
        {
            Debug.Log($"Received {inventoryItems.Count} items from SceneTransitionManager");
            int keyItemsSkipped = 0;
            
            foreach (ItemData item in inventoryItems)
            {
                // Double-check for key items for extra safety
                if (item.type == ItemData.ItemType.KeyItem || item.IsKeyItem() || item.name == "Cold Key")
                {
                    Debug.Log($"REJECTING key item in combat: {item.name} (Type: {item.type}) - Key items should not be available in combat");
                    keyItemsSkipped++;
                    continue;
                }
                
                // Always clone items to avoid reference issues
                ItemData clonedItem = item.Clone();
                playerInventoryItems.Add(clonedItem);
                Debug.Log($"Added to combat inventory: {clonedItem.name}, Amount: {clonedItem.amount}, Type: {clonedItem.type}");
            }
            
            Debug.Log($"Combat Manager received {playerInventoryItems.Count} items for combat inventory (filtered out {keyItemsSkipped} KeyItems)");
            
            // Perform one final verification that no key items made it through
            List<ItemData> illegalItems = playerInventoryItems.Where(item => item.IsKeyItem() || item.type == ItemData.ItemType.KeyItem).ToList();
            if (illegalItems.Count > 0)
            {
                Debug.LogError($"CRITICAL ERROR: {illegalItems.Count} key items somehow passed through filtering! Removing them now.");
                foreach (var item in illegalItems)
                {
                    Debug.LogError($"Removing illegal key item: {item.name}");
                    playerInventoryItems.Remove(item);
                }
            }
            
            // Update the item menu if it exists
            if (itemMenu != null && combatUI != null)
            {
                Debug.Log("Populating item menu with current inventory items (excluding KeyItems)");
                combatUI.PopulateItemMenu(playerInventoryItems);
            }
            else
            {
                Debug.LogWarning("Could not populate item menu: itemMenu or combatUI is null");
            }
        }
        else
        {
            Debug.LogWarning("Received empty or null inventory from SceneTransitionManager");
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

    // Method to enable/disable combat
    public void SetCombatActive(bool active)
    {
        isCombatActive = active;
    }
    
    // Method to add new enemies during combat (for phase transitions)
    public void AddEnemy(CombatStats enemy)
    {
        if (enemy != null)
        {
            enemies.Add(enemy);
            Debug.Log($"Added new enemy to combat: {enemy.characterName} with {enemy.currentHealth}/{enemy.maxHealth} HP");
        }
    }
    
    // Method to clean up the enemy list by removing dead or null enemies
    public void CleanupEnemyList()
    {
        if (enemies == null) return;
        
        // Create a temporary list to avoid modification during iteration
        List<CombatStats> toRemove = new List<CombatStats>();
        
        // Find all dead or null enemies
        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.IsDead() || !enemy.gameObject.activeSelf)
            {
                toRemove.Add(enemy);
            }
        }
        
        // Remove them from the main list
        foreach (var enemy in toRemove)
        {
            enemies.Remove(enemy);
            Debug.Log($"Removed dead/null enemy from combat manager's list during cleanup");
        }
        
        Debug.Log($"Enemy list cleanup complete. Remaining enemies: {enemies.Count}");
    }
    
    // Method to reset combat end status for phase 2
    public void ResetCombatEndStatus()
    {
        isCombatEnded = false;
        isInPhaseTransition = false;
        isCombatActive = true;
        Debug.Log("Combat status reset for phase 2");
    }
    
    // Method to proceed with victory after dialogue sequence
    public void ProceedWithVictory()
    {
        if (OnCombatEnd != null && !isCombatEnded)
        {
            isCombatEnded = true;
            OnCombatEnd(true);
        }
        else if (SceneTransitionManager.Instance != null && !isCombatEnded)
        {
            isCombatEnded = true;
            SceneTransitionManager.Instance.EndCombat(true);
        }
    }
} 