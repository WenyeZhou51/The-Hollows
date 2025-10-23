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
    private bool _isWaitingForPlayerInput = false;
    public bool isWaitingForPlayerInput => _isWaitingForPlayerInput;
    
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

    // Flag to control action accumulation
    private bool shouldAccumulateAction = true;

    private void Awake()
    {
        Debug.LogError("[BUILD DEBUG] CombatManager.Awake() - Beginning initialization");
        
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

        // CRITICAL FIX: Ensure PersistentGameManager exists BEFORE initializing character stats
        Debug.LogError("[BUILD DEBUG] CombatManager.Awake() - Checking for PersistentGameManager.Instance before initialization");
        
        if (PersistentGameManager.Instance == null)
        {
            Debug.LogError("[BUILD DEBUG] PersistentGameManager.Instance is NULL - calling EnsureExists");
            PersistentGameManager.EnsureExists();
            
            if (PersistentGameManager.Instance == null)
            {
                Debug.LogError("[BUILD DEBUG] CRITICAL ERROR: PersistentGameManager.Instance is STILL NULL after EnsureExists!");
            }
            else
            {
                Debug.LogError("[BUILD DEBUG] PersistentGameManager.Instance successfully created with ID: " + PersistentGameManager.Instance.GetInstanceID());
            }
        }
        else
        {
            Debug.LogError("[BUILD DEBUG] PersistentGameManager.Instance already exists with ID: " + PersistentGameManager.Instance.GetInstanceID());
        }
        
        // Wait one frame to ensure PersistentGameManager is fully initialized
        StartCoroutine(DelayedCharacterStatsInitialization());

        // Find the battle dialogue trigger
        battleDialogueTrigger = FindObjectOfType<BattleDialogueTrigger>();
        
        Debug.LogError("[BUILD DEBUG] CombatManager.Awake() - Completed");
    }
    
    // CRITICAL FIX: Use coroutine to delay initialization by one frame
    // This ensures the PersistentGameManager is fully initialized
    private IEnumerator DelayedCharacterStatsInitialization()
    {
        Debug.LogError("[BUILD DEBUG] Starting DelayedCharacterStatsInitialization coroutine");
        
        // Wait for the end of the frame to ensure PersistentGameManager is fully initialized
        yield return new WaitForEndOfFrame();
        
        Debug.LogError("[BUILD DEBUG] After one frame delay - PersistentGameManager.Instance exists: " + (PersistentGameManager.Instance != null));
        
        // Initialize character stats now that we've waited a frame
        InitializeCharacterStats();
        
        Debug.LogError("[BUILD DEBUG] DelayedCharacterStatsInitialization complete");
        
        // Log player stats after initialization
        foreach (var player in players)
        {
            if (player != null)
            {
                Debug.LogError($"[BUILD DEBUG] PLAYER STATS AFTER DELAYED INIT: {player.characterName} - Health: {player.currentHealth}/{player.maxHealth}, Mind: {player.currentSanity}/{player.maxSanity}");
            }
        }
    }

    private void Start()
    {
        Debug.LogError($"[CRITICAL DEBUG] CombatManager Start in scene: {SceneManager.GetActiveScene().name}, Is Battle_Obelisk: {SceneManager.GetActiveScene().name == "Battle_Obelisk"}");
        
        // CRITICAL FIX: Ensure SceneTransitionManager exists
        SceneTransitionManager.EnsureExists();
        
        // Log if SceneTransitionManager was found
        if (SceneTransitionManager.Instance != null)
        {
            Debug.LogError("[COMBAT DEBUG] SceneTransitionManager found with ID: " + SceneTransitionManager.Instance.GetInstanceID());
            
            // VERY CRITICAL: Check PlayerPrefs to see if there's a return scene
            if (PlayerPrefs.HasKey("ReturnSceneName"))
            {
                string savedScene = PlayerPrefs.GetString("ReturnSceneName");
                Debug.LogError($"[CRITICAL DEBUG] Return scene found in PlayerPrefs: '{savedScene}'");
            }
            else
            {
                Debug.LogError("[CRITICAL DEBUG] NO return scene found in PlayerPrefs!");
            }
        }
        else
        {
            Debug.LogError("[COMBAT DEBUG] CRITICAL ERROR: SceneTransitionManager.Instance is null even after EnsureExists!");
        }
        
        // Initialize from SceneTransitionManager's inventory
        if (SceneTransitionManager.Instance != null)
        {
            playerInventoryItems = SceneTransitionManager.Instance.GetPlayerInventory();
            
            // CRITICAL FIX: Make sure SceneTransitionManager is listening to combat end events
            Debug.LogError("[COMBAT DEBUG] Registering SceneTransitionManager for combat end events");
            if (OnCombatEnd != null)
            {
                Debug.LogError("[CRITICAL DEBUG] WARNING: OnCombatEnd already has listeners before subscribing SceneTransitionManager!");
                Delegate[] delegates = OnCombatEnd.GetInvocationList();
                foreach (var del in delegates)
                {
                    Debug.LogError($"[CRITICAL DEBUG] Existing delegate: {del.Target.GetType().Name}.{del.Method.Name}");
                }
            }
            OnCombatEnd += SceneTransitionManager.Instance.EndCombat;
            Debug.LogError($"[CRITICAL DEBUG] Added SceneTransitionManager.EndCombat to event. Now has listeners: {(OnCombatEnd != null)}");
        }
        else
        {
            Debug.LogError("[COMBAT DEBUG] Cannot get inventory, SceneTransitionManager.Instance is null!");
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
            
            // Remove GridLayoutGroup if it exists and add VerticalLayoutGroup
            GridLayoutGroup existingGrid = skillMenu.GetComponentInChildren<GridLayoutGroup>();
            if (existingGrid != null)
            {
                DestroyImmediate(existingGrid);
            }
            
            VerticalLayoutGroup verticalLayout = skillMenu.GetComponentInChildren<VerticalLayoutGroup>();
            if (verticalLayout == null && menuButtonsContainer != null)
            {
                verticalLayout = menuButtonsContainer.gameObject.AddComponent<VerticalLayoutGroup>();
                // Get spacing from CombatUI configuration
                CombatUI combatUI = GetComponent<CombatUI>();
                float spacing = combatUI != null ? combatUI.GetSkillButtonSpacing() : 5f;
                verticalLayout.spacing = spacing;
                verticalLayout.childAlignment = TextAnchor.UpperCenter;
                verticalLayout.childControlWidth = true;
                verticalLayout.childControlHeight = false;
                verticalLayout.childForceExpandWidth = false;
                verticalLayout.childForceExpandHeight = false;
                
                // Don't add ContentSizeFitter here - let CombatUI handle it based on ScrollRect presence
                // Adding ContentSizeFitter unconditionally causes conflicts and infinite height expansion
                Debug.Log("[CombatManager] VerticalLayoutGroup configured, ContentSizeFitter will be managed by CombatUI");
            }
            
            skillMenu.SetActive(false);
        }

        if (itemMenu != null)
        {
            // Remove GridLayoutGroup if it exists and add VerticalLayoutGroup for item menu too
            var existingItemGrid = itemMenu.GetComponentInChildren<GridLayoutGroup>();
            if (existingItemGrid != null)
            {
                DestroyImmediate(existingItemGrid);
            }
            
            VerticalLayoutGroup itemVerticalLayout = itemMenu.GetComponentInChildren<VerticalLayoutGroup>();
            if (itemVerticalLayout == null)
            {
                RectTransform itemContainer = itemMenu.GetComponentInChildren<RectTransform>();
                if (itemContainer != null)
                {
                    itemVerticalLayout = itemContainer.gameObject.AddComponent<VerticalLayoutGroup>();
                    itemVerticalLayout.spacing = 5f;
                    itemVerticalLayout.childAlignment = TextAnchor.UpperCenter;
                    itemVerticalLayout.childControlWidth = true;
                    itemVerticalLayout.childControlHeight = false;
                    itemVerticalLayout.childForceExpandWidth = false;
                    itemVerticalLayout.childForceExpandHeight = false;
                    
                    // DO NOT add ContentSizeFitter - it causes the item menu to expand infinitely
                    // The item menu must respect its original fixed size from the editor
                    Debug.Log("[CombatManager] Item menu VerticalLayoutGroup configured without ContentSizeFitter");
                }
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
        Debug.LogError("[BUILD DEBUG] InitializeCharacterStats called - checking if PersistentGameManager exists");
        
        // Make sure PersistentGameManager exists - this is crucial for builds
        if (PersistentGameManager.Instance == null)
        {
            Debug.LogError("[BUILD DEBUG] PersistentGameManager.Instance is NULL - attempting to create via EnsureExists");
            PersistentGameManager.EnsureExists();
        }
        
        // CRITICAL: Second check after ensure exists
        if (PersistentGameManager.Instance == null)
        {
            Debug.LogError("[BUILD DEBUG] CRITICAL FAILURE: PersistentGameManager.Instance is STILL NULL after EnsureExists - stats will not be initialized!");
            return;
        }

        Debug.LogError("===== INITIALIZING CHARACTER STATS FROM PERSISTENT MANAGER =====");
        foreach (var playerStat in players)
        {
            if (playerStat == null) 
            {
                Debug.LogError("[BUILD DEBUG] Skipping NULL player in InitializeCharacterStats");
                continue;
            }

            Debug.LogError($"[BUILD DEBUG] Initializing stats for {playerStat.name} with characterName: {playerStat.characterName}");
            
            // Log default values from CombatStats before modification
            Debug.LogError($"[BUILD DEBUG] Default values for {playerStat.characterName ?? "unnamed character"} before initialization: Health {playerStat.currentHealth}/{playerStat.maxHealth}, Mind {playerStat.currentSanity}/{playerStat.maxSanity}");

            string characterId = playerStat.characterName;
            if (string.IsNullOrEmpty(characterId))
            {
                Debug.LogError($"[BUILD DEBUG] Character has no name, cannot retrieve persistent stats");
                continue;
            }
            
            try
            {
                // Get health values from PersistentGameManager - wrap in try/catch for robustness
                float currentHealth = PersistentGameManager.Instance.GetCharacterHealth(characterId, (int)playerStat.maxHealth);
                float maxHealth = PersistentGameManager.Instance.GetCharacterMaxHealth(characterId);
                
                // Get mind/sanity values from PersistentGameManager
                float currentMind = PersistentGameManager.Instance.GetCharacterMind(characterId, (int)playerStat.maxSanity);
                float maxMind = PersistentGameManager.Instance.GetCharacterMaxMind(characterId);

                Debug.LogError($"[BUILD DEBUG] Retrieved values from PersistentGameManager for {characterId}: Health {currentHealth}/{maxHealth}, Mind {currentMind}/{maxMind}");

                // Apply values to combat stats - force the values to be applied
                playerStat.maxHealth = maxHealth;
                playerStat.currentHealth = currentHealth;
                playerStat.maxSanity = maxMind;
                playerStat.currentSanity = currentMind;
                
                // Mark the stats as initialized to prevent them from being reset in Start()
                playerStat.MarkStatsInitialized();

                // Log the final values after setting them
                Debug.LogError($"[BUILD DEBUG] Final initialized stats for {characterId}: Health {playerStat.currentHealth}/{playerStat.maxHealth}, Mind {playerStat.currentSanity}/{playerStat.maxSanity}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BUILD DEBUG] EXCEPTION during stats initialization for {characterId}: {ex.Message}");
                Debug.LogError($"[BUILD DEBUG] Stack trace: {ex.StackTrace}");
            }
        }
        
        Debug.LogError("[BUILD DEBUG] Character initialization from PersistentGameManager complete");
    }

    private void Update()
    {
        // Skip combat processing if dialogue is active
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive())
            return;
            
        if (!isCombatActive || _isWaitingForPlayerInput) return;

        // Update action bars with null check
        if (shouldAccumulateAction)
        {
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
            
            // Enemy turn - show the text panel with the enemy-specific message
            if (combatUI != null)
            {
                // Get the current scene name to determine which enemy message to show
                string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                
                if (currentSceneName == "Battle_Obelisk")
                {
                    combatUI.ShowTextPanel("The obelisk focuses on you");
                }
                else if (currentSceneName == "Battle_Weaver 1")
                {
                    combatUI.ShowTextPanel("The weaver spins its threads");
                }
                else if (currentSceneName == "Battle_Aperture")
                {
                    combatUI.ShowTextPanel("The aperture adjusts its focus");
                }
                else
                {
                    // Generic fallback message
                    combatUI.ShowTextPanel($"{character.characterName} prepares to attack");
                }
            }
            
            // Enemy turn - use the enemy's behavior component if available
            StartCoroutine(ExecuteEnemyTurn(character));
        }
        else
        {
            // Player turn - increment turn counter
            playerTurnCount++;
            
            // Display different messages based on the scene and turn count
            string turnMessage;
            string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            
            if (currentSceneName == "Battle_Obelisk")
            {
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
            }
            else if (currentSceneName == "Battle_Weaver 1")
            {
                if (playerTurnCount == 1)
                {
                    turnMessage = "Threads cover the ground";
                }
                else
                {
                    turnMessage = "Threads cover your body";
                }
            }
            else if (currentSceneName == "Battle_Aperture")
            {
                if (playerTurnCount == 1)
                {
                    turnMessage = "You cast your gaze into a strange lens";
                }
                else
                {
                    turnMessage = "The strange lens gazes also";
                }
            }
            else
            {
                // Generic fallback message for unknown battle scenes
                turnMessage = "Your turn";
            }
            
            // Show the text panel with the appropriate message
            if (combatUI != null)
            {
                combatUI.ShowTextPanel(turnMessage);
            }
            
            // Player turn - show action menu
            _isWaitingForPlayerInput = true;
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
            
            // Apply the attackMultiplier from status effects
            float calculatedDamage = enemy.CalculateDamage(baseDamage);
            
            // Round down to whole number
            int finalDamage = Mathf.FloorToInt(calculatedDamage);
            
            Debug.Log($"[COMBAT] {enemy.name} basic attack with base damage: {baseDamage}, attackMultiplier: {enemy.attackMultiplier}, final damage: {finalDamage}");
            
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
        // Brief wait for visual feedback
        yield return new WaitForSeconds(0.4f);
            
        // Base damage
        float baseDamage = 10f;
        
        // Apply 20% variance (80-120% of base damage)
        float variance = UnityEngine.Random.Range(0.8f, 1.2f);
        float damageWithVariance = baseDamage * variance;
        
        // Apply the attackMultiplier for weakness/strength statuses
        float calculatedDamage = activeCharacter.CalculateDamage(damageWithVariance);
        
        // Round down to whole number
        int finalDamage = Mathf.FloorToInt(calculatedDamage);
        
        Debug.Log($"[COMBAT] {activeCharacter.name} attacks with base damage: {baseDamage}, variance: {variance}, attackMultiplier: {activeCharacter.attackMultiplier}, final damage: {finalDamage}");
        
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
        // Brief wait for visual feedback
        yield return new WaitForSeconds(0.4f);
            
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
        
        _isWaitingForPlayerInput = false;
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
                    combatUI.ShowTextPanel("Defeat!");
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
                    combatUI.ShowTextPanel("You are victorious");
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
        Debug.LogError("[COMBAT DEBUG] TriggerVictory called - isCombatEnded: " + isCombatEnded);
        Debug.LogError("[COMBAT DEBUG] OnCombatEnd event has listeners: " + (OnCombatEnd != null).ToString());
        Debug.LogError("[COMBAT DEBUG] SceneTransitionManager exists: " + (SceneTransitionManager.Instance != null).ToString());
        
        // Check if combat has already ended to prevent multiple calls
        if (isCombatEnded)
        {
            Debug.LogError("[COMBAT DEBUG] TriggerVictory called but combat has already ended - ignoring");
            return;
        }
        
        // CRITICAL FIX: Special handling for Obelisk battle to allow phase transition
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool isObeliskBattle = (currentSceneName == "Battle_Obelisk");
        
        if (isObeliskBattle)
        {
            Debug.LogError("[COMBAT DEBUG] Obelisk battle detected - allowing phase transition");
        }
        
        // Set flag immediately to prevent multiple victory triggers
        isCombatEnded = true;
        
        if (OnCombatEnd != null)
        {
            Debug.LogError("[COMBAT DEBUG] Calling OnCombatEnd event with victory=true");
            OnCombatEnd(true);
        }
        else
        {
            Debug.LogError("[COMBAT DEBUG] No listeners for OnCombatEnd event, calling ProceedWithVictory directly");
            // CRITICAL FIX: If no event listeners, call ProceedWithVictory directly
            ProceedWithVictory();
        }
    }
    
    private void TriggerDefeat()
    {
        // Increment the death counter in PersistentGameManager
        if (PersistentGameManager.Instance != null)
        {
            PersistentGameManager.Instance.IncrementDeaths();
        }

        // Save character stats (for any characters that might still be alive)
        Debug.LogError("[COMBAT DEBUG] Explicitly saving character stats before defeat transition");
        SaveCharacterStats();
        
        // Make sure ScreenFader exists
        ScreenFader.EnsureExists();
        
        // Make sure SceneTransitionManager exists to handle cleanup
        SceneTransitionManager.EnsureExists();
        
        // Perform explicit cleanup of all combat-related objects
        CleanupCombatObjects();
        
        // Start the transition to the start menu with fade effect
        StartCoroutine(TransitionToStartMenu());
    }
    
    /// <summary>
    /// Cleanup all combat-related objects before scene transition
    /// </summary>
    private void CleanupCombatObjects()
    {
        Debug.Log("CombatManager: Cleaning up combat objects before transition");
        
        // Immediately hide all UI elements
        if (combatUI != null)
        {
            if (combatUI.textPanel != null)
                combatUI.textPanel.SetActive(false);
                
            if (combatUI.actionDisplayLabel != null)
                combatUI.actionDisplayLabel.SetActive(false);
                
            if (skillMenu != null)
                skillMenu.SetActive(false);
                
            if (itemMenu != null)
                itemMenu.SetActive(false);
                
            if (combatUI.actionMenu != null)
                combatUI.actionMenu.SetActive(false);
        }
        
        // Handle any remaining coroutines
        StopAllCoroutines();
    }
    
    /// <summary>
    /// Transition to the start menu after player defeat
    /// </summary>
    private IEnumerator TransitionToStartMenu()
    {
        // Fade to black
        yield return StartCoroutine(ScreenFader.Instance.FadeToBlack());
        
        // Explicitly set combat ended flag for good measure
        isCombatActive = false;
        isCombatEnded = true;
        
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
        
        // Find and destroy any persistent combat-related objects that may have survived the transition
        var combatManagers = FindObjectsOfType<CombatManager>();
        foreach (var manager in combatManagers)
        {
            if (manager != this)
            {
                Destroy(manager.gameObject);
            }
        }
        
        // Find and destroy any combat UI components in the scene
        var combatUIs = FindObjectsOfType<CombatUI>();
        foreach (var ui in combatUIs)
        {
            Destroy(ui.gameObject);
        }
        
        // Find any canvases that might contain combat elements
        Canvas[] allCanvases = FindObjectsOfType<Canvas>(true);
        foreach (Canvas canvas in allCanvases)
        {
            string canvasName = canvas.gameObject.name.ToLower();
            if (canvasName.Contains("combat") || canvasName.Contains("battle") || 
                canvasName.Contains("enemy") || canvasName.Contains("action") || 
                canvasName.Contains("menu") || canvasName.Contains("skill") || 
                canvasName.Contains("item") || canvasName.Contains("stats") ||
                canvasName.Contains("panel"))
            {
                Destroy(canvas.gameObject);
            }
        }
        
        // Ensure SceneTransitionManager performs its cleanup
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.ResetCombatStatus();
        }
        
        // Destroy this object after cleaning up
        Destroy(gameObject);
        
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

        Debug.Log("[COMBAT DEBUG] Saving character stats for all non-dead player characters");
        
        foreach (var playerStat in players)
        {
            // Skip null or dead characters
            if (playerStat == null || playerStat.IsDead()) 
            {
                if (playerStat == null)
                {
                    Debug.LogWarning("[COMBAT DEBUG] Skipping null player character when saving stats");
                }
                else 
                {
                    Debug.LogWarning($"[COMBAT DEBUG] Skipping dead character {playerStat.characterName} when saving stats");
                }
                continue;
            }

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

            Debug.Log($"[COMBAT DEBUG] Saved combat stats for {characterId}: Health {playerStat.currentHealth}/{playerStat.maxHealth}, Mind {playerStat.currentSanity}/{playerStat.maxSanity}");
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
                if (item.type == ItemData.ItemType.KeyItem || 
                    item.IsKeyItem() || 
                    item.name == "Cold Key" || 
                    item.name.Contains("Medallion") || 
                    item.name.StartsWith("Medal"))
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
        Debug.LogError("[COMBAT DEBUG] ProceedWithVictory called - isCombatEnded: " + isCombatEnded);
        Debug.LogError("[COMBAT DEBUG] OnCombatEnd has listeners: " + (OnCombatEnd != null).ToString());
        Debug.LogError("[COMBAT DEBUG] SceneTransitionManager exists: " + (SceneTransitionManager.Instance != null).ToString());
        
        // Check if combat has already ended to prevent multiple calls
        if (isCombatEnded)
        {
            Debug.LogError("[COMBAT DEBUG] ProceedWithVictory called but combat has already ended - ignoring");
            return;
        }
        
        // Set flag immediately to prevent multiple victory triggers
        isCombatEnded = true;
        
        // Always save character stats before proceeding with victory
        Debug.LogError("[COMBAT DEBUG] Explicitly saving character stats before victory transition");
        SaveCharacterStats();
        
        if (OnCombatEnd != null)
        {
            Debug.LogError("[COMBAT DEBUG] Inside ProceedWithVictory - Calling OnCombatEnd event with victory=true");
            OnCombatEnd(true);
        }
        else if (SceneTransitionManager.Instance != null)
        {
            Debug.LogError("[COMBAT DEBUG] Inside ProceedWithVictory - Calling SceneTransitionManager.EndCombat directly");
            SceneTransitionManager.Instance.EndCombat(true);
        }
        else
        {
            Debug.LogError("[COMBAT DEBUG] WARNING: Cannot proceed with victory - SceneTransitionManager is null");
        }
    }

    private void InitializeCharacterStats(CombatStats stats, bool isPlayerCharacter)
    {
        if (stats == null) return;
        
        // Set stats based on character type
        if (isPlayerCharacter)
        {
            // Get the character's name for lookup in PersistentGameManager
            string characterName = stats.characterName;
            Debug.LogError($"[BUILD DEBUG] InitializeCharacterStats for individual character: {characterName}");
            
            // CRITICAL: Ensure PersistentGameManager exists
            if (PersistentGameManager.Instance == null)
            {
                Debug.LogError("[BUILD DEBUG] PersistentGameManager.Instance is NULL - creating via EnsureExists");
                PersistentGameManager.EnsureExists();
                
                if (PersistentGameManager.Instance == null)
                {
                    Debug.LogError("[BUILD DEBUG] CRITICAL ERROR: PersistentGameManager.Instance is STILL NULL after EnsureExists!");
                }
            }
            
            // Only initialize from PersistentGameManager if we have a valid instance and character name
            if (PersistentGameManager.Instance != null && !string.IsNullOrEmpty(characterName))
            {
                try
                {
                    // Get max health and mind from persistent storage
                    int maxHealth = PersistentGameManager.Instance.GetCharacterMaxHealth(characterName);
                    int maxMind = PersistentGameManager.Instance.GetCharacterMaxMind(characterName);
                    float actionSpeed = PersistentGameManager.Instance.GetCharacterActionSpeed(characterName, stats.actionSpeed);
                    
                    // Set max values
                    stats.maxHealth = maxHealth;
                    stats.maxSanity = maxMind;
                    stats.actionSpeed = actionSpeed;
                    stats.baseActionSpeed = actionSpeed; // Store original value for status effects
                    
                    // Get current health and mind from persistent storage
                    int currentHealth = PersistentGameManager.Instance.GetCharacterHealth(characterName, (int)maxHealth);
                    int currentMind = PersistentGameManager.Instance.GetCharacterMind(characterName, (int)maxMind);
                    
                    // Set current values
                    stats.currentHealth = currentHealth;
                    stats.currentSanity = currentMind;
                    
                    Debug.LogError($"[BUILD DEBUG] Initialized {characterName} with health: {currentHealth}/{maxHealth}, mind: {currentMind}/{maxMind}, action speed: {actionSpeed}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[BUILD DEBUG] EXCEPTION during individual character initialization for {characterName}: {ex.Message}");
                    Debug.LogError($"[BUILD DEBUG] Stack trace: {ex.StackTrace}");
                }
            }
            else
            {
                Debug.LogError($"[BUILD DEBUG] No PersistentGameManager found or invalid character name: {characterName}. Using default values.");
            }
            
            // Mark as initialized to prevent default values in Start()
            stats.MarkStatsInitialized();
        }
        else
        {
            // Enemy characters keep their prefab values
            Debug.Log($"[COMBAT MANAGER] Initializing ENEMY character: {stats.name} with default values");
        }
    }

    /// <summary>
    /// Pauses action accumulation for all characters
    /// </summary>
    public void PauseActionAccumulation()
    {
        shouldAccumulateAction = false;
        Debug.Log("Action accumulation paused");
    }
    
    /// <summary>
    /// Resumes action accumulation for all characters
    /// </summary>
    public void ResumeActionAccumulation()
    {
        shouldAccumulateAction = true;
        Debug.Log("Action accumulation resumed");
    }
    
    /// <summary>
    /// Returns whether action should accumulate
    /// </summary>
    public bool ShouldAccumulateAction()
    {
        return shouldAccumulateAction;
    }

    /// <summary>
    /// Updates action speeds for all players from the PersistentGameManager
    /// </summary>
    public void UpdatePlayerActionSpeeds()
    {
        if (PersistentGameManager.Instance == null)
        {
            Debug.LogError("Cannot update player action speeds: PersistentGameManager.Instance is null");
            return;
        }

        foreach (var player in players)
        {
            if (player != null && !player.isEnemy)
            {
                float newSpeed = PersistentGameManager.Instance.GetCharacterActionSpeed(player.characterName, player.actionSpeed);
                player.actionSpeed = newSpeed;
                player.baseActionSpeed = newSpeed; // Store for status effects
                Debug.Log($"Updated {player.characterName}'s action speed to {newSpeed}");
            }
        }
    }
} 