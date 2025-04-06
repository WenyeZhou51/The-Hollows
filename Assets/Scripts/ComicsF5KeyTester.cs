using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// A test script that monitors F5 key presses and triggers the comics display sequence manually.
/// Attach this to any GameObject in your test scene.
/// </summary>
public class ComicsF5KeyTester : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool logAllKeyPresses = false;
    
    [Header("Test Panel Settings")]
    [SerializeField] private List<Image> testPanels = new List<Image>();
    [SerializeField] private TransitionDirection defaultTransition = TransitionDirection.RIGHT;
    
    private float keyCheckTimer = 0f;
    
    private void Update()
    {
        // Monitor key presses for testing purposes
        keyCheckTimer += Time.unscaledDeltaTime;
        
        // Only log key detection every second to avoid spamming the console
        if (keyCheckTimer >= 1.0f)
        {
            if (debugMode) Debug.Log($"[F5Tester] Update running, time scale: {Time.timeScale}");
            keyCheckTimer = 0f;
        }
        
        // Check for F5 key press
        if (Input.GetKeyDown(KeyCode.F5))
        {
            Debug.Log("[F5Tester] F5 key press detected! Attempting to trigger comics sequence");
            TriggerComicsSequence();
        }
        // Log all key presses if enabled
        else if (logAllKeyPresses && Input.anyKeyDown)
        {
            foreach (KeyCode kcode in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(kcode))
                {
                    Debug.Log($"[F5Tester] Key pressed: {kcode}");
                    break;
                }
            }
        }
    }
    
    /// <summary>
    /// Trigger the comics sequence with test panels
    /// </summary>
    private void TriggerComicsSequence()
    {
        // Make sure we have a controller instance
        ComicsDisplayController controller = ComicsDisplayController.Instance;
        if (controller == null)
        {
            Debug.LogError("[F5Tester] No ComicsDisplayController found in scene! Creating one now...");
            GameObject controllerObj = new GameObject("ComicsDisplayController");
            controller = controllerObj.AddComponent<ComicsDisplayController>();
            DontDestroyOnLoad(controllerObj);
        }
        
        // Clear any existing panels
        controller.ClearPanels();
        
        // Add our test panels
        if (testPanels.Count > 0)
        {
            Debug.Log($"[F5Tester] Adding {testPanels.Count} test panels to controller");
            
            foreach (Image panel in testPanels)
            {
                if (panel != null)
                {
                    controller.AddComicPanel(panel.gameObject, defaultTransition);
                }
                else
                {
                    Debug.LogWarning("[F5Tester] Skipping null panel reference");
                }
            }
            
            // Start the sequence
            controller.StartComicSequence();
        }
        else
        {
            Debug.LogError("[F5Tester] No test panels configured! Please add panel references in the Inspector.");
        }
    }
    
    /// <summary>
    /// Manually trigger the comics sequence (can be called from UI buttons in test scenes)
    /// </summary>
    public void TriggerSequenceFromButton()
    {
        Debug.Log("[F5Tester] Trigger requested from UI button");
        TriggerComicsSequence();
    }
} 