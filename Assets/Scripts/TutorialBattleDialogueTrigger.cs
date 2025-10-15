using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Extended BattleDialogueTrigger that uses TutorialDialogueHandler for tutorial-specific features
/// Use this for Battle_Tutorial scene instead of the regular BattleDialogueTrigger
/// </summary>
public class TutorialBattleDialogueTrigger : MonoBehaviour
{
    [Header("Tutorial Dialogue Files")]
    [SerializeField] private TextAsset introDialogueJSON;
    [SerializeField] private TextAsset midBattleDialogueJSON;
    [SerializeField] private TextAsset victoryDialogueJSON;

    [Header("Battle Triggers")]
    [SerializeField] private bool playIntroDialogue = true;
    [SerializeField] private bool playVictoryDialogue = true;
    [SerializeField] private int midBattleTriggerTurn = 3;

    private TutorialDialogueHandler introHandler;
    private TutorialDialogueHandler midBattleHandler;
    private TutorialDialogueHandler victoryHandler;
    
    private bool hasPlayedIntro = false;
    private bool hasPlayedMidBattle = false;
    private bool hasPlayedVictory = false;
    
    private CombatManager combatManager;

    private void Awake()
    {
        // Create tutorial handlers for each dialogue point
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
    }
    
    private IEnumerator PlayIntroWithDelay()
    {
        // Wait for 1 second before playing intro dialogue
        yield return new WaitForSeconds(1f);
        
        hasPlayedIntro = true;
        
        // TUTORIAL: Position dialogue at the top of the screen
        DialogueManager.Instance.SetDialoguePositionTop();
        
        // Pause combat during tutorial
        if (combatManager != null)
        {
            combatManager.SetCombatActive(false);
            
            // Subscribe to dialogue end event to resume combat
            DialogueManager.OnDialogueStateChanged += OnTutorialDialogueStateChanged;
        }
        
        DialogueManager.Instance.StartInkDialogue(introHandler);
    }
    
    private void OnTutorialDialogueStateChanged(bool isActive)
    {
        if (!isActive) // Dialogue ended
        {
            Debug.Log("[Tutorial] Dialogue ended, resuming combat");
            
            // Reset dialogue position to bottom (default)
            DialogueManager.Instance.SetDialoguePositionBottom();
            
            // Unsubscribe from event
            DialogueManager.OnDialogueStateChanged -= OnTutorialDialogueStateChanged;
            
            // Resume combat
            if (combatManager != null)
            {
                combatManager.SetCombatActive(true);
            }
        }
    }
    
    private void SetupDialogueHandlers()
    {
        // Create tutorial handlers for each dialogue point
        if (introDialogueJSON != null)
        {
            introHandler = gameObject.AddComponent<TutorialDialogueHandler>();
            introHandler.InkJSON = introDialogueJSON;
        }
        
        if (midBattleDialogueJSON != null)
        {
            midBattleHandler = gameObject.AddComponent<TutorialDialogueHandler>();
            midBattleHandler.InkJSON = midBattleDialogueJSON;
        }
        
        if (victoryDialogueJSON != null)
        {
            victoryHandler = gameObject.AddComponent<TutorialDialogueHandler>();
            victoryHandler.InkJSON = victoryDialogueJSON;
        }
    }
    
    public void CheckTurnBasedTriggers(int currentTurn)
    {
        // Check for mid-battle dialogue
        if (!hasPlayedMidBattle && midBattleHandler != null && currentTurn == midBattleTriggerTurn)
        {
            hasPlayedMidBattle = true;
            
            // TUTORIAL: Position dialogue at the top of the screen
            DialogueManager.Instance.SetDialoguePositionTop();
            
            // Pause combat during dialogue
            if (combatManager != null)
            {
                combatManager.SetCombatActive(false);
                DialogueManager.OnDialogueStateChanged += OnTutorialDialogueStateChanged;
            }
            
            DialogueManager.Instance.StartInkDialogue(midBattleHandler);
        }
    }
    
    private void HandleCombatEnd(bool playerWon)
    {
        if (playerWon && playVictoryDialogue && victoryHandler != null && !hasPlayedVictory)
        {
            hasPlayedVictory = true;
            
            // TUTORIAL: Position dialogue at the top of the screen
            DialogueManager.Instance.SetDialoguePositionTop();
            
            // Subscribe to dialogue end event to reset position
            DialogueManager.OnDialogueStateChanged += OnVictoryDialogueStateChanged;
            
            DialogueManager.Instance.StartInkDialogue(victoryHandler);
        }
    }
    
    private void OnVictoryDialogueStateChanged(bool isActive)
    {
        if (!isActive) // Dialogue ended
        {
            Debug.Log("[Tutorial] Victory dialogue ended");
            
            // Reset dialogue position to bottom (default)
            DialogueManager.Instance.SetDialoguePositionBottom();
            
            // Unsubscribe from event
            DialogueManager.OnDialogueStateChanged -= OnVictoryDialogueStateChanged;
        }
    }
    
    private void OnDestroy()
    {
        // Clean up event subscriptions
        if (combatManager != null)
        {
            combatManager.OnCombatEnd -= HandleCombatEnd;
        }
        
        DialogueManager.OnDialogueStateChanged -= OnTutorialDialogueStateChanged;
    }
}

