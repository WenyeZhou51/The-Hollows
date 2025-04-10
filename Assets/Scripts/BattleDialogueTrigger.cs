using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleDialogueTrigger : MonoBehaviour
{
    [Header("Dialogue Files")]
    [SerializeField] private TextAsset introDialogueJSON;
    [SerializeField] private TextAsset midBattleDialogueJSON;
    [SerializeField] private TextAsset victoryDialogueJSON;

    [Header("Battle Triggers")]
    [SerializeField] private bool playIntroDialogue = true;
    [SerializeField] private bool playVictoryDialogue = true;
    [SerializeField] private int midBattleTriggerTurn = 3;
    
    [Header("Phase Transition")]
    [SerializeField] private GameObject phase1Obelisk;
    [SerializeField] private GameObject phase2ObeliskPrefab;
    [SerializeField] private Vector3 phase2SpawnPosition = Vector3.zero;
    [SerializeField] private float fadeOutDuration = 1.0f;
    [SerializeField] private float fadeInDuration = 1.0f;
    [SerializeField] private bool shouldTransitionToPhase2 = true;

    private InkDialogueHandler introHandler;
    private InkDialogueHandler midBattleHandler;
    private InkDialogueHandler victoryHandler;
    
    private bool hasPlayedIntro = false;
    private bool hasPlayedMidBattle = false;
    private bool hasPlayedVictory = false;
    private bool isTransitioningPhases = false;
    
    private CombatManager combatManager;

    private void Awake()
    {
        // Create handlers for each dialogue point
        SetupDialogueHandlers();
        
        // Find the combat manager
        combatManager = FindObjectOfType<CombatManager>();
        
        // Subscribe to combat events if combat manager exists
        if (combatManager != null)
        {
            combatManager.OnCombatEnd += HandleCombatEnd;
        }
    }
    
    private void Start()
    {
        // Check if DialogueManager exists, if not create one
        if (DialogueManager.Instance == null)
        {
            DialogueManager.CreateInstance();
        }
        
        // Play intro dialogue if enabled
        if (playIntroDialogue && introHandler != null && !hasPlayedIntro)
        {
            StartCoroutine(PlayIntroWithDelay());
        }
        
        // Make sure ScreenFader exists
        ScreenFader.EnsureExists();
    }
    
    private IEnumerator PlayIntroWithDelay()
    {
        // Wait for 1 second before playing intro dialogue
        yield return new WaitForSeconds(1f);
        
        hasPlayedIntro = true;
        DialogueManager.Instance.StartInkDialogue(introHandler);
    }
    
    private void SetupDialogueHandlers()
    {
        // Create handlers for each dialogue point
        if (introDialogueJSON != null)
        {
            introHandler = gameObject.AddComponent<InkDialogueHandler>();
            introHandler.InkJSON = introDialogueJSON;
        }
        
        if (midBattleDialogueJSON != null)
        {
            midBattleHandler = gameObject.AddComponent<InkDialogueHandler>();
            midBattleHandler.InkJSON = midBattleDialogueJSON;
        }
        
        if (victoryDialogueJSON != null)
        {
            victoryHandler = gameObject.AddComponent<InkDialogueHandler>();
            victoryHandler.InkJSON = victoryDialogueJSON;
        }
    }
    
    public void CheckTurnBasedTriggers(int currentTurn)
    {
        // Check for mid-battle dialogue
        if (!hasPlayedMidBattle && midBattleHandler != null && currentTurn == midBattleTriggerTurn)
        {
            hasPlayedMidBattle = true;
            DialogueManager.Instance.StartInkDialogue(midBattleHandler);
        }
    }
    
    private void HandleCombatEnd(bool playerWon)
    {
        Debug.Log($"[DEBUG OBELISK TRANSITION] HandleCombatEnd called with playerWon={playerWon}, hasPlayedVictory={hasPlayedVictory}, isTransitioningPhases={isTransitioningPhases}");
        
        if (playerWon && playVictoryDialogue && victoryHandler != null && !hasPlayedVictory && !isTransitioningPhases)
        {
            Debug.Log("[DEBUG OBELISK TRANSITION] Starting victory sequence!");
            hasPlayedVictory = true;
            isTransitioningPhases = true;
            
            // Begin phase transition sequence with fade to black
            StartCoroutine(Phase1VictorySequence());
        }
        else
        {
            Debug.Log($"[DEBUG OBELISK TRANSITION] Skipping victory sequence. Reasons: playerWon={playerWon}, playVictoryDialogue={playVictoryDialogue}, " +
                      $"victoryHandler={victoryHandler != null}, hasPlayedVictory={hasPlayedVictory}, isTransitioningPhases={isTransitioningPhases}");
        }
    }
    
    private IEnumerator Phase1VictorySequence()
    {
        Debug.Log("[DEBUG OBELISK TRANSITION] Phase1VictorySequence started");
        
        // Set combat system inactive while we transition phases
        if (combatManager != null)
        {
            // Stop the combat temporarily
            Debug.Log("[DEBUG OBELISK TRANSITION] Setting combat inactive for transition");
            combatManager.SetCombatActive(false);
        }
        else
        {
            Debug.LogError("[DEBUG OBELISK TRANSITION] Combat manager is null when trying to set combat inactive!");
        }
        
        // Fade to black
        if (ScreenFader.Instance != null)
        {
            Debug.Log($"[DEBUG OBELISK TRANSITION] Starting fade to black, duration: {fadeOutDuration}");
            yield return StartCoroutine(ScreenFader.Instance.FadeToBlack(fadeOutDuration));
            Debug.Log("[DEBUG OBELISK TRANSITION] Fade to black complete");
        }
        else
        {
            Debug.LogError("[DEBUG OBELISK TRANSITION] ScreenFader not found for phase transition!");
            yield return new WaitForSeconds(fadeOutDuration);
        }
        
        // Play the victory dialogue while screen is black
        Debug.Log("[DEBUG OBELISK TRANSITION] Starting victory dialogue");
        DialogueManager.Instance.StartInkDialogue(victoryHandler);
        
        // Subscribe to dialogue end event to handle phase transition
        Debug.Log("[DEBUG OBELISK TRANSITION] Subscribed to DialogueStateChanged event");
        DialogueManager.OnDialogueStateChanged += HandleVictoryDialogueEnd;
    }
    
    private void HandleVictoryDialogueEnd(bool isActive)
    {
        Debug.Log($"[DEBUG OBELISK TRANSITION] HandleVictoryDialogueEnd called with isActive={isActive}");
        
        if (!isActive) // Dialogue ended
        {
            // Unsubscribe to prevent multiple calls
            Debug.Log("[DEBUG OBELISK TRANSITION] Dialogue ended, unsubscribing from event");
            DialogueManager.OnDialogueStateChanged -= HandleVictoryDialogueEnd;
            
            // Begin phase 2 transition after dialogue completes
            Debug.Log("[DEBUG OBELISK TRANSITION] Starting TransitionToPhase2 coroutine");
            StartCoroutine(TransitionToPhase2());
        }
    }
    
    private IEnumerator TransitionToPhase2()
    {
        Debug.Log("[DEBUG OBELISK TRANSITION] TransitionToPhase2 started");
        
        if (shouldTransitionToPhase2)
        {
            // Log the state of the references before transition
            Debug.Log($"[DEBUG OBELISK TRANSITION] Phase 1 Obelisk: {(phase1Obelisk != null ? phase1Obelisk.name : "NULL")}");
            Debug.Log($"[DEBUG OBELISK TRANSITION] Phase 2 Obelisk Prefab: {(phase2ObeliskPrefab != null ? phase2ObeliskPrefab.name : "NULL")}");
            Debug.Log($"[DEBUG OBELISK TRANSITION] Combat Manager: {(combatManager != null ? "Valid" : "NULL")}");
            Debug.Log($"[DEBUG OBELISK TRANSITION] Spawn Position: {phase2SpawnPosition}");
            
            // Check phase 1 obelisk reference before using it
            if (phase1Obelisk == null)
            {
                Debug.LogError("[DEBUG OBELISK TRANSITION] Phase 1 Obelisk reference is missing! Attempting to find in scene...");
                
                // Try to find the obelisk in the scene by tag or name
                phase1Obelisk = GameObject.FindWithTag("Obelisk");
                
                if (phase1Obelisk == null)
                {
                    phase1Obelisk = GameObject.Find("Obelisk");
                }
                
                if (phase1Obelisk == null)
                {
                    // Try to find by CombatStats component with "Obelisk" in the name
                    CombatStats[] allCombatStats = FindObjectsOfType<CombatStats>();
                    foreach (var stats in allCombatStats)
                    {
                        if (stats.characterName != null && stats.characterName.Contains("Obelisk"))
                        {
                            phase1Obelisk = stats.gameObject;
                            Debug.Log($"[DEBUG OBELISK TRANSITION] Found Obelisk by CombatStats: {phase1Obelisk.name}");
                            break;
                        }
                    }
                }
                
                if (phase1Obelisk == null)
                {
                    Debug.LogError("[DEBUG OBELISK TRANSITION] Failed to find Phase 1 Obelisk in scene!");
                }
                else
                {
                    Debug.Log($"[DEBUG OBELISK TRANSITION] Found Phase 1 Obelisk: {phase1Obelisk.name}");
                }
            }
            
            // Check if phase 2 prefab is assigned
            if (phase2ObeliskPrefab == null)
            {
                Debug.LogError("[DEBUG OBELISK TRANSITION] Phase 2 Obelisk Prefab is null! Transition will fail.");
                // Try to proceed with transition anyway
            }
            
            // Get position before destroying phase 1
            Vector3 spawnPosition = phase2SpawnPosition;
            
            // Store the CombatStats component from Phase 1 to use for Phase 2
            CombatStats phase1Stats = null;
            float currentHealthPercent = 1.0f;
            float currentActionPercent = 0f;
            
            if (phase1Obelisk != null)
            {
                phase1Stats = phase1Obelisk.GetComponent<CombatStats>();
                if (phase1Stats != null)
                {
                    // Store health percentage to transfer to Phase 2
                    currentHealthPercent = phase1Stats.currentHealth / phase1Stats.maxHealth;
                    currentActionPercent = phase1Stats.currentAction / phase1Stats.maxAction;
                    Debug.Log($"[DEBUG OBELISK TRANSITION] Phase 1 Obelisk health: {phase1Stats.currentHealth}/{phase1Stats.maxHealth} ({currentHealthPercent*100}%)");
                    Debug.Log($"[DEBUG OBELISK TRANSITION] Phase 1 Obelisk action: {phase1Stats.currentAction}/{phase1Stats.maxAction} ({currentActionPercent*100}%)");
                }
            }
            
            // Find the "Enemies" GameObject to use as parent
            Transform enemiesParent = null;
            GameObject enemiesObj = GameObject.Find("Enemies");
            if (enemiesObj != null)
            {
                enemiesParent = enemiesObj.transform;
                Debug.Log($"[DEBUG OBELISK TRANSITION] Found Enemies parent GameObject: {enemiesObj.name}");
            }
            else
            {
                Debug.LogWarning("[DEBUG OBELISK TRANSITION] Could not find 'Enemies' GameObject - trying to find parent from Phase 1 Obelisk");
                
                // Fallback: Try to find the proper parent by going up the hierarchy from Phase 1 Obelisk
                if (phase1Obelisk != null)
                {
                    Transform current = phase1Obelisk.transform.parent;
                    while (current != null)
                    {
                        if (current.name == "Enemies")
                        {
                            enemiesParent = current;
                            Debug.Log($"[DEBUG OBELISK TRANSITION] Found Enemies parent by traversing hierarchy: {current.name}");
                            break;
                        }
                        current = current.parent;
                    }
                    
                    // If still not found, just use immediate parent as fallback
                    if (enemiesParent == null && phase1Obelisk.transform.parent != null)
                    {
                        enemiesParent = phase1Obelisk.transform.parent;
                        Debug.Log($"[DEBUG OBELISK TRANSITION] Using Phase 1 Obelisk's parent as fallback: {enemiesParent.name}");
                    }
                }
            }
            
            // If spawn position is at origin (0,0,0) and phase1Obelisk exists, use its position
            if (spawnPosition == Vector3.zero && phase1Obelisk != null)
            {
                spawnPosition = phase1Obelisk.transform.position;
                Debug.Log($"[DEBUG OBELISK TRANSITION] Using Phase 1 Obelisk position for spawning: {spawnPosition}");
            }
            
            // Destroy phase 1 obelisk
            if (phase1Obelisk != null)
            {
                Debug.Log($"[DEBUG OBELISK TRANSITION] Destroying Phase 1 Obelisk: {phase1Obelisk.name}");
                Destroy(phase1Obelisk);
            }
            
            // Wait a frame to ensure destruction completes
            yield return null;
            
            // Spawn phase 2 obelisk
            if (phase2ObeliskPrefab != null)
            {
                // CRITICAL FIX: Always spawn at exact position (0,0,0) regardless of phase1 position
                Vector3 exactSpawnPosition = Vector3.zero;
                Debug.Log($"[DEBUG OBELISK TRANSITION] Force-setting spawn position to {exactSpawnPosition} for full screen visibility");
                
                Debug.Log($"[DEBUG OBELISK TRANSITION] Instantiating Phase 2 Obelisk at position: {exactSpawnPosition}, Parent: {(enemiesParent != null ? enemiesParent.name : "NULL")}");
                GameObject phase2Obelisk = Instantiate(phase2ObeliskPrefab, exactSpawnPosition, Quaternion.identity, enemiesParent);
                
                // CRITICAL FIX: Force Start() method to be called on Phase 2 Obelisk's CombatStats component
                CombatStats phase2Stats = phase2Obelisk.GetComponent<CombatStats>();
                if (phase2Stats != null)
                {
                    // Temporarily disable the CombatStats component to force re-initialization
                    phase2Stats.enabled = false;
                    
                    // Set health proportional to Phase 1's health
                    if (phase1Stats != null)
                    {
                        // Scale health based on Phase 1's remaining health percentage
                        phase2Stats.currentHealth = phase2Stats.maxHealth * currentHealthPercent;
                        phase2Stats.currentAction = phase2Stats.maxAction * currentActionPercent;
                        Debug.Log($"[DEBUG OBELISK TRANSITION] Set Phase 2 Obelisk health to: {phase2Stats.currentHealth}/{phase2Stats.maxHealth} ({currentHealthPercent*100}%)");
                        Debug.Log($"[DEBUG OBELISK TRANSITION] Set Phase 2 Obelisk action to: {phase2Stats.currentAction}/{phase2Stats.maxAction} ({currentActionPercent*100}%)");
                    }
                    
                    // Re-enable the component to force Start() to run
                    phase2Stats.enabled = true;
                    
                    // Wait a frame for the component to initialize
                    yield return null;
                    
                    Debug.Log($"[DEBUG OBELISK TRANSITION] Phase 2 Obelisk CombatStats re-initialized: Health={phase2Stats.currentHealth}/{phase2Stats.maxHealth}, Active={phase2Stats.enabled}");
                }
                else
                {
                    Debug.LogError("[DEBUG OBELISK TRANSITION] Phase 2 Obelisk does not have a CombatStats component!");
                }
                
                Debug.Log($"[DEBUG OBELISK TRANSITION] Spawned Phase 2 Obelisk: {phase2Obelisk.name} at position {phase2Obelisk.transform.position}");
                
                // Add to combat manager's enemies list
                if (combatManager != null)
                {
                    Debug.Log("[DEBUG OBELISK TRANSITION] Attempting to add Phase 2 Obelisk to combat manager");
                    
                    if (phase2Stats != null)
                    {
                        // Make sure the enemies list exists
                        if (combatManager.enemies == null)
                        {
                            Debug.LogError("[DEBUG OBELISK TRANSITION] Combat manager enemies list is null! Creating new list.");
                            combatManager.enemies = new List<CombatStats>();
                        }
                        
                        // Clear any existing dead enemies first
                        int initialEnemyCount = combatManager.enemies.Count;
                        Debug.Log($"[DEBUG OBELISK TRANSITION] Initial enemy count: {initialEnemyCount}");
                        
                        List<CombatStats> toRemove = new List<CombatStats>();
                        foreach (var enemy in combatManager.enemies)
                        {
                            if (enemy == null || enemy.IsDead() || !enemy.gameObject.activeSelf)
                            {
                                toRemove.Add(enemy);
                            }
                        }
                        
                        foreach (var enemy in toRemove)
                        {
                            if (combatManager.enemies.Contains(enemy))
                            {
                                combatManager.enemies.Remove(enemy);
                                Debug.Log($"[DEBUG OBELISK TRANSITION] Removed dead/null enemy from combat manager's list");
                            }
                        }
                        
                        // Now add the phase 2 obelisk
                        combatManager.enemies.Add(phase2Stats);
                        Debug.Log($"[DEBUG OBELISK TRANSITION] Added Phase 2 Obelisk to combat manager: {phase2Stats.characterName} with {phase2Stats.currentHealth}/{phase2Stats.maxHealth} HP");
                        
                        // Log the updated enemy list
                        Debug.Log("[DEBUG OBELISK TRANSITION] Updated enemy list:");
                        foreach (var enemy in combatManager.enemies)
                        {
                            Debug.Log($"[DEBUG OBELISK TRANSITION] - {(enemy != null ? enemy.characterName : "NULL")}");
                        }
                    }
                    else
                    {
                        Debug.LogError("[DEBUG OBELISK TRANSITION] Phase 2 Obelisk prefab does not have a CombatStats component!");
                    }
                }
                else
                {
                    Debug.LogError("[DEBUG OBELISK TRANSITION] Combat Manager is null, cannot add Phase 2 Obelisk to enemies list!");
                }
            }
            else
            {
                Debug.LogError("[DEBUG OBELISK TRANSITION] Phase 2 Obelisk Prefab is missing! Make sure it's assigned in the inspector.");
            }
            
            // Give a moment for the new phase to initialize
            Debug.Log("[DEBUG OBELISK TRANSITION] Waiting 0.5 seconds for initialization");
            yield return new WaitForSeconds(0.5f);
            
            // Fade back in from black
            if (ScreenFader.Instance != null)
            {
                Debug.Log($"[DEBUG OBELISK TRANSITION] Starting fade from black, duration: {fadeInDuration}");
                yield return StartCoroutine(ScreenFader.Instance.FadeFromBlack(fadeInDuration));
                Debug.Log("[DEBUG OBELISK TRANSITION] Fade from black complete");
            }
            else
            {
                Debug.LogError("[DEBUG OBELISK TRANSITION] ScreenFader missing for fade from black");
                yield return new WaitForSeconds(fadeInDuration);
            }
            
            // Resume combat with phase 2
            if (combatManager != null)
            {
                Debug.Log("[DEBUG OBELISK TRANSITION] Re-enabling combat for phase 2");
                
                // Force update the enemy list to remove any null references
                Debug.Log("[DEBUG OBELISK TRANSITION] Cleaning up enemy list");
                combatManager.CleanupEnemyList();
                
                // Reset the combat flags to ensure battle continues
                Debug.Log("[DEBUG OBELISK TRANSITION] Resetting combat status");
                combatManager.SetCombatActive(true);
                combatManager.ResetCombatEndStatus();
                Debug.Log("[DEBUG OBELISK TRANSITION] Combat Manager state reset, combat should now continue with Phase 2");
            }
            else
            {
                Debug.LogError("[DEBUG OBELISK TRANSITION] Cannot resume combat: Combat Manager is null");
            }
            
            // Reset transition flag
            isTransitioningPhases = false;
            Debug.Log("[DEBUG OBELISK TRANSITION] Phase transition completed, isTransitioningPhases=false");
        }
        else
        {
            // If not transitioning to phase 2, proceed with normal victory
            Debug.Log("[DEBUG OBELISK TRANSITION] Phase 2 transition disabled, proceeding with normal victory");
            
            // Fade back in from black
            if (ScreenFader.Instance != null)
            {
                yield return StartCoroutine(ScreenFader.Instance.FadeFromBlack(fadeInDuration));
            }
            
            // Inform the combat manager we're done with the special sequence
            if (combatManager != null)
            {
                combatManager.ProceedWithVictory();
            }
        }
    }
    
    private void OnDestroy()
    {
        // Clean up event subscriptions
        if (combatManager != null)
        {
            combatManager.OnCombatEnd -= HandleCombatEnd;
        }
        
        DialogueManager.OnDialogueStateChanged -= HandleVictoryDialogueEnd;
    }
} 