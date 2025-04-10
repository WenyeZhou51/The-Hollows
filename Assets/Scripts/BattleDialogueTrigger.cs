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
        if (playerWon && playVictoryDialogue && victoryHandler != null && !hasPlayedVictory && !isTransitioningPhases)
        {
            hasPlayedVictory = true;
            isTransitioningPhases = true;
            
            // Begin phase transition sequence with fade to black
            StartCoroutine(Phase1VictorySequence());
        }
    }
    
    private IEnumerator Phase1VictorySequence()
    {
        // Set combat system inactive while we transition phases
        if (combatManager != null)
        {
            // Stop the combat temporarily
            combatManager.SetCombatActive(false);
        }
        
        // Fade to black
        if (ScreenFader.Instance != null)
        {
            yield return StartCoroutine(ScreenFader.Instance.FadeToBlack(fadeOutDuration));
        }
        else
        {
            Debug.LogError("ScreenFader not found for phase transition!");
            yield return new WaitForSeconds(fadeOutDuration);
        }
        
        // Play the victory dialogue while screen is black
        DialogueManager.Instance.StartInkDialogue(victoryHandler);
        
        // Subscribe to dialogue end event to handle phase transition
        DialogueManager.OnDialogueStateChanged += HandleVictoryDialogueEnd;
    }
    
    private void HandleVictoryDialogueEnd(bool isActive)
    {
        if (!isActive) // Dialogue ended
        {
            // Unsubscribe to prevent multiple calls
            DialogueManager.OnDialogueStateChanged -= HandleVictoryDialogueEnd;
            
            // Begin phase 2 transition after dialogue completes
            StartCoroutine(TransitionToPhase2());
        }
    }
    
    private IEnumerator TransitionToPhase2()
    {
        Debug.Log("Victory dialogue completed, transitioning to phase 2");
        
        if (shouldTransitionToPhase2)
        {
            // Destroy phase 1 obelisk
            if (phase1Obelisk != null)
            {
                Destroy(phase1Obelisk);
            }
            
            // Spawn phase 2 obelisk
            if (phase2ObeliskPrefab != null)
            {
                GameObject phase2Obelisk = Instantiate(phase2ObeliskPrefab, phase2SpawnPosition, Quaternion.identity);
                Debug.Log($"Spawned Phase 2 Obelisk: {phase2Obelisk.name}");
                
                // Add to combat manager's enemies list
                if (combatManager != null)
                {
                    CombatStats phase2Stats = phase2Obelisk.GetComponent<CombatStats>();
                    if (phase2Stats != null)
                    {
                        combatManager.AddEnemy(phase2Stats);
                    }
                }
            }
            
            // Give a moment for the new phase to initialize
            yield return new WaitForSeconds(0.5f);
            
            // Fade back in from black
            if (ScreenFader.Instance != null)
            {
                yield return StartCoroutine(ScreenFader.Instance.FadeFromBlack(fadeInDuration));
            }
            else
            {
                yield return new WaitForSeconds(fadeInDuration);
            }
            
            // Resume combat with phase 2
            if (combatManager != null)
            {
                combatManager.SetCombatActive(true);
                combatManager.ResetCombatEndStatus();
            }
            
            // Reset transition flag
            isTransitioningPhases = false;
        }
        else
        {
            // If not transitioning to phase 2, proceed with normal victory
            Debug.Log("Phase 2 transition disabled, proceeding with normal victory");
            
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