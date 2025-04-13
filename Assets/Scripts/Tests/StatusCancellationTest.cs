using UnityEngine;

public class StatusCancellationTest : MonoBehaviour
{
    public CombatStats testCharacter;
    private StatusManager statusManager;

    private void Start()
    {
        statusManager = StatusManager.Instance;
        if (statusManager == null)
        {
            Debug.LogError("[Test] StatusManager instance not found!");
            return;
        }

        if (testCharacter == null)
        {
            Debug.LogError("[Test] Test character not assigned!");
            return;
        }

        // Run tests after a short delay to ensure everything is initialized
        Invoke("RunTests", 1.0f);
    }

    private void RunTests()
    {
        Debug.Log("[Test] Beginning status cancellation tests...");

        // Test 1: Apply STRENGTH, then WEAKNESS (should cancel)
        Debug.Log("[Test 1] Applying STRENGTH, then WEAKNESS (should cancel)");
        statusManager.ApplyStatus(testCharacter, StatusType.Strength);
        
        // Check that STRENGTH was applied
        bool strengthApplied = statusManager.HasStatus(testCharacter, StatusType.Strength);
        Debug.Log($"[Test 1] STRENGTH applied: {strengthApplied}");
        
        // Now apply WEAKNESS, which should cancel STRENGTH
        statusManager.ApplyStatus(testCharacter, StatusType.Weakness);
        
        // Check that both are now canceled
        bool strengthRemains = statusManager.HasStatus(testCharacter, StatusType.Strength);
        bool weaknessApplied = statusManager.HasStatus(testCharacter, StatusType.Weakness);
        Debug.Log($"[Test 1] After applying WEAKNESS - STRENGTH remains: {strengthRemains}, WEAKNESS applied: {weaknessApplied}");
        
        // Clear all statuses to start fresh
        statusManager.ClearAllStatuses(testCharacter);

        // Test 2: Apply WEAKNESS, then STRENGTH (should cancel)
        Debug.Log("[Test 2] Applying WEAKNESS, then STRENGTH (should cancel)");
        statusManager.ApplyStatus(testCharacter, StatusType.Weakness);
        
        // Check that WEAKNESS was applied
        bool weaknessApplied2 = statusManager.HasStatus(testCharacter, StatusType.Weakness);
        Debug.Log($"[Test 2] WEAKNESS applied: {weaknessApplied2}");
        
        // Now apply STRENGTH, which should cancel WEAKNESS
        statusManager.ApplyStatus(testCharacter, StatusType.Strength);
        
        // Check that both are now canceled
        bool weaknessRemains = statusManager.HasStatus(testCharacter, StatusType.Weakness);
        bool strengthApplied2 = statusManager.HasStatus(testCharacter, StatusType.Strength);
        Debug.Log($"[Test 2] After applying STRENGTH - WEAKNESS remains: {weaknessRemains}, STRENGTH applied: {strengthApplied2}");
        
        // Clear all statuses to start fresh
        statusManager.ClearAllStatuses(testCharacter);

        // Test 3: Apply multiple statuses and check cancellation
        Debug.Log("[Test 3] Testing multiple statuses and cancellations");
        
        // Apply TOUGH and AGILE
        statusManager.ApplyStatus(testCharacter, StatusType.Tough);
        statusManager.ApplyStatus(testCharacter, StatusType.Agile);
        
        // Verify they were applied
        bool toughApplied = statusManager.HasStatus(testCharacter, StatusType.Tough);
        bool agileApplied = statusManager.HasStatus(testCharacter, StatusType.Agile);
        Debug.Log($"[Test 3] TOUGH applied: {toughApplied}, AGILE applied: {agileApplied}");
        
        // Now apply VULNERABLE, which should cancel TOUGH but leave AGILE
        statusManager.ApplyStatus(testCharacter, StatusType.Vulnerable);
        
        // Check results
        bool toughRemains = statusManager.HasStatus(testCharacter, StatusType.Tough);
        bool vulnerableApplied = statusManager.HasStatus(testCharacter, StatusType.Vulnerable);
        bool agileStillApplied = statusManager.HasStatus(testCharacter, StatusType.Agile);
        Debug.Log($"[Test 3] After applying VULNERABLE - TOUGH remains: {toughRemains}, VULNERABLE applied: {vulnerableApplied}, AGILE remains: {agileStillApplied}");
        
        // Finally, apply SLOWED which should cancel AGILE
        statusManager.ApplyStatus(testCharacter, StatusType.Slowed);
        
        // Check final results
        bool agileRemains = statusManager.HasStatus(testCharacter, StatusType.Agile);
        bool slowedApplied = statusManager.HasStatus(testCharacter, StatusType.Slowed);
        bool vulnerableStillApplied = statusManager.HasStatus(testCharacter, StatusType.Vulnerable);
        Debug.Log($"[Test 3] After applying SLOWED - AGILE remains: {agileRemains}, SLOWED applied: {slowedApplied}, VULNERABLE remains: {vulnerableStillApplied}");
        
        // Clear all statuses when done
        statusManager.ClearAllStatuses(testCharacter);
        
        Debug.Log("[Test] Status cancellation tests completed!");
    }
} 