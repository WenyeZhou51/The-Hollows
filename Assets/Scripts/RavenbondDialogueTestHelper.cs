using UnityEngine;
using System.Reflection;

/// <summary>
/// Debug helper to test the Ravenbond dialogue effect without playing the full game.
/// Attach this to any GameObject in the scene for testing.
/// </summary>
public class RavenbondDialogueTestHelper : MonoBehaviour
{
    [Tooltip("Reference to the Ravenbond dialogue ink file")]
    [SerializeField] private TextAsset ravenbondInkFile;
    
    [Tooltip("Key to press to test the Ravenbond failure effect")]
    [SerializeField] private KeyCode testKey = KeyCode.F12;
    
    [Tooltip("Key to test setting HP and sanity to low values")]
    [SerializeField] private KeyCode setLowStatsKey = KeyCode.F10;
    
    [Tooltip("Character ID to check")]
    [SerializeField] private string characterId = "The Magician";
    
    [Header("Test Penalty Values")]
    [Tooltip("Amount to reduce max HP by in test")]
    [SerializeField] private int testMaxHPPenalty = 10;
    [Tooltip("Amount to reduce max sanity by in test")]
    [SerializeField] private int testMaxSanityPenalty = 10;
    
    private void Update()
    {
        // Press the test key to trigger the Ravenbond effect
        if (Input.GetKeyDown(testKey))
        {
            TestRavenbondFailure();
        }
        
        // Press the info key to display current character stats
        if (Input.GetKeyDown(KeyCode.F11))
        {
            DisplayCharacterStats();
        }
        
        // Test setting HP and sanity to low values
        if (Input.GetKeyDown(setLowStatsKey))
        {
            SetLowStats();
        }
    }
    
    /// <summary>
    /// Test the Ravenbond failure effect directly
    /// </summary>
    private void TestRavenbondFailure()
    {
        Debug.Log("===== TESTING RAVENBOND FAILURE EFFECT =====");
        
        // Create a temporary InkDialogueHandler
        GameObject tempObj = new GameObject("TempInkHandler");
        InkDialogueHandler handler = tempObj.AddComponent<InkDialogueHandler>();
        
        // Set the penalty values using reflection
        SetPenaltyFields(handler, testMaxHPPenalty, testMaxSanityPenalty);
        
        // Assign the Ravenbond Ink file
        FieldInfo inkField = typeof(InkDialogueHandler).GetField("inkJSON", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (inkField != null)
        {
            inkField.SetValue(handler, ravenbondInkFile);
            
            // Initialize the story
            handler.InitializeStory();
            
            // Invoke the private method using reflection
            MethodInfo method = typeof(InkDialogueHandler).GetMethod("ApplyRavenbondPenalty", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (method != null)
            {
                method.Invoke(handler, null);
                Debug.Log($"Ravenbond penalty applied through test helper: -{testMaxHPPenalty} HP, -{testMaxSanityPenalty} Sanity");
            }
            else
            {
                Debug.LogError("Could not find ApplyRavenbondPenalty method");
            }
        }
        
        // Clean up the temporary object
        Destroy(tempObj);
    }
    
    /// <summary>
    /// Set the penalty field values using reflection
    /// </summary>
    private void SetPenaltyFields(InkDialogueHandler handler, int hpPenalty, int sanityPenalty)
    {
        FieldInfo hpPenaltyField = typeof(InkDialogueHandler).GetField("ravenbondMaxHPPenalty", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        FieldInfo sanityPenaltyField = typeof(InkDialogueHandler).GetField("ravenbondMaxSanityPenalty", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (hpPenaltyField != null)
        {
            hpPenaltyField.SetValue(handler, hpPenalty);
        }
        
        if (sanityPenaltyField != null)
        {
            sanityPenaltyField.SetValue(handler, sanityPenalty);
        }
    }
    
    /// <summary>
    /// Test setting HP and sanity to values that would trigger game over on next penalty
    /// </summary>
    private void SetLowStats()
    {
        // Make sure PersistentGameManager exists
        PersistentGameManager.EnsureExists();
        
        if (PersistentGameManager.Instance != null)
        {
            Debug.Log("Setting Magician's stats to low values...");
            
            // Set HP and sanity to values that should trigger game over on next penalty
            int targetValue = testMaxHPPenalty; // Set to exactly the penalty amount
            
            PersistentGameManager.Instance.SaveCharacterStats(
                characterId, 
                targetValue,   // current health
                targetValue,   // max health
                targetValue,   // current sanity
                targetValue    // max sanity
            );
            
            DisplayCharacterStats();
        }
    }
    
    /// <summary>
    /// Display the current character stats
    /// </summary>
    private void DisplayCharacterStats()
    {
        // Make sure PersistentGameManager exists
        PersistentGameManager.EnsureExists();
        
        if (PersistentGameManager.Instance != null)
        {
            // Get current max values
            int maxHealth = PersistentGameManager.Instance.GetCharacterMaxHealth(characterId);
            int maxSanity = PersistentGameManager.Instance.GetCharacterMaxMind(characterId);
            
            // Get current values
            int currentHealth = PersistentGameManager.Instance.GetCharacterHealth(characterId, maxHealth);
            int currentSanity = PersistentGameManager.Instance.GetCharacterMind(characterId, maxSanity);
            
            Debug.Log($"===== {characterId} STATS =====");
            Debug.Log($"Health: {currentHealth}/{maxHealth}");
            Debug.Log($"Sanity: {currentSanity}/{maxSanity}");
            Debug.Log($"Deaths: {PersistentGameManager.Instance.GetDeaths()}");
            Debug.Log($"Test penalty values: HP -{testMaxHPPenalty}, Sanity -{testMaxSanityPenalty}");
        }
    }
} 