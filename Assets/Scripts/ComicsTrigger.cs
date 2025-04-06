using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Trigger component for activating comic display sequences when the player enters a specific area
/// </summary>
public class ComicsTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] private bool triggerOnEnter = true;
    [SerializeField] private bool playOnce = true;
    [SerializeField] private float triggerDelay = 0.5f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Comic Panels")]
    [SerializeField] private List<ComicPanelConfig> comicPanels = new List<ComicPanelConfig>();

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private Color gizmoColor = new Color(0, 1, 0, 0.3f);
    [SerializeField] private bool debugMode = true;

    private bool hasTriggered = false;
    private Coroutine delayCoroutine = null;

    private void Start()
    {
        if (debugMode)
        {
            Debug.Log($"[ComicsTrigger] {gameObject.name} initialized with {comicPanels.Count} panels");
            
            // Validate panel configurations
            int validPanels = 0;
            foreach (ComicPanelConfig config in comicPanels)
            {
                if (config.panelImage != null)
                {
                    validPanels++;
                }
                else
                {
                    Debug.LogWarning($"[ComicsTrigger] {gameObject.name} has a panel config with missing image reference");
                }
            }
            
            Debug.Log($"[ComicsTrigger] {gameObject.name} has {validPanels} valid panel configurations");
            
            // Check if player layer is set
            if (playerLayer.value == 0)
            {
                Debug.LogError($"[ComicsTrigger] {gameObject.name} has no player layer set! Trigger will not work correctly.");
            }
            else
            {
                Debug.Log($"[ComicsTrigger] {gameObject.name} is set to detect player on layer(s): {LayerMaskToString(playerLayer)}");
            }
        }
    }

    private void OnValidate()
    {
        // Ensure the trigger has a collider
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            Debug.LogWarning($"[ComicsTrigger] {gameObject.name} requires a Collider2D component. Adding BoxCollider2D.");
            BoxCollider2D boxCollider = gameObject.AddComponent<BoxCollider2D>();
            boxCollider.isTrigger = true;
            boxCollider.size = new Vector2(2f, 2f);
        }
        else if (!collider.isTrigger)
        {
            Debug.LogWarning($"[ComicsTrigger] {gameObject.name} collider should be set as a trigger. Setting isTrigger to true.");
            collider.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (debugMode)
        {
            Debug.Log($"[ComicsTrigger] {gameObject.name} detected collision with {collision.gameObject.name} on layer {LayerMask.LayerToName(collision.gameObject.layer)}");
        }
        
        if (!triggerOnEnter)
        {
            if (debugMode) Debug.Log($"[ComicsTrigger] {gameObject.name} has triggerOnEnter disabled, ignoring collision");
            return;
        }
        
        if (playOnce && hasTriggered)
        {
            if (debugMode) Debug.Log($"[ComicsTrigger] {gameObject.name} has already triggered once and playOnce is true, ignoring collision");
            return;
        }

        // Check if the collision is with the player
        if (((1 << collision.gameObject.layer) & playerLayer) != 0)
        {
            if (debugMode) Debug.Log($"[ComicsTrigger] {gameObject.name} detected player, preparing to trigger sequence");
            
            if (triggerDelay > 0)
            {
                if (delayCoroutine != null)
                {
                    if (debugMode) Debug.Log($"[ComicsTrigger] {gameObject.name} cancelling existing delayed trigger");
                    StopCoroutine(delayCoroutine);
                }
                
                if (debugMode) Debug.Log($"[ComicsTrigger] {gameObject.name} starting delayed trigger with {triggerDelay}s delay");
                delayCoroutine = StartCoroutine(DelayedTrigger());
            }
            else
            {
                if (debugMode) Debug.Log($"[ComicsTrigger] {gameObject.name} triggering sequence immediately");
                TriggerComicSequence();
            }
        }
        else if (debugMode)
        {
            Debug.Log($"[ComicsTrigger] {gameObject.name} collision was not with player (layer mismatch)");
        }
    }

    /// <summary>
    /// Manually trigger the comic sequence (can be called by other scripts)
    /// </summary>
    public void TriggerSequence()
    {
        if (debugMode) Debug.Log($"[ComicsTrigger] {gameObject.name} TriggerSequence called externally");
        
        if (playOnce && hasTriggered)
        {
            if (debugMode) Debug.Log($"[ComicsTrigger] {gameObject.name} has already triggered once and playOnce is true, ignoring external trigger");
            return;
        }
            
        TriggerComicSequence();
    }

    private IEnumerator DelayedTrigger()
    {
        if (debugMode) Debug.Log($"[ComicsTrigger] {gameObject.name} starting delayed trigger countdown: {triggerDelay}s");
        yield return new WaitForSeconds(triggerDelay);
        if (debugMode) Debug.Log($"[ComicsTrigger] {gameObject.name} delay completed, triggering sequence");
        TriggerComicSequence();
        delayCoroutine = null;
    }

    private void TriggerComicSequence()
    {
        if (debugMode) Debug.Log($"[ComicsTrigger] {gameObject.name} TriggerComicSequence called");
        
        // Find or create the comic display controller
        ComicsDisplayController controller = ComicsDisplayController.Instance;
        if (controller == null)
        {
            if (debugMode) Debug.Log($"[ComicsTrigger] {gameObject.name} no controller instance found, creating new one");
            GameObject controllerObj = new GameObject("ComicsDisplayController");
            controller = controllerObj.AddComponent<ComicsDisplayController>();
        }
        else if (debugMode)
        {
            Debug.Log($"[ComicsTrigger] {gameObject.name} found existing controller instance");
        }

        // Clear any existing panels and add our configured panels
        controller.ClearPanels();
        
        int addedPanels = 0;
        foreach (ComicPanelConfig panelConfig in comicPanels)
        {
            if (panelConfig.panelImage != null)
            {
                controller.AddComicPanel(panelConfig.panelImage.gameObject, panelConfig.transitionDirection);
                addedPanels++;
            }
            else if (debugMode)
            {
                Debug.LogWarning($"[ComicsTrigger] {gameObject.name} skipping null panel image reference");
            }
        }
        
        if (debugMode) Debug.Log($"[ComicsTrigger] {gameObject.name} added {addedPanels} panels to controller");
        
        // Start the sequence
        if (addedPanels > 0)
        {
            if (debugMode) Debug.Log($"[ComicsTrigger] {gameObject.name} starting comic sequence");
            controller.StartComicSequence();
            
            // Mark as triggered if playOnce is true
            hasTriggered = true;
            if (debugMode && playOnce) Debug.Log($"[ComicsTrigger] {gameObject.name} marked as triggered (one-time only)");
        }
        else
        {
            Debug.LogError($"[ComicsTrigger] {gameObject.name} failed to start sequence - no valid panels were added");
        }
    }

    public void ResetTrigger()
    {
        if (debugMode) Debug.Log($"[ComicsTrigger] {gameObject.name} reset trigger state");
        hasTriggered = false;
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos)
            return;
            
        Gizmos.color = gizmoColor;
        
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            // Draw a box for box colliders
            if (collider is BoxCollider2D boxCollider)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(boxCollider.offset, boxCollider.size);
            }
            // Draw a sphere for circle colliders
            else if (collider is CircleCollider2D circleCollider)
            {
                Gizmos.DrawSphere(transform.position + (Vector3)circleCollider.offset, circleCollider.radius);
            }
        }
        else
        {
            // If no collider, draw a default box
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero, new Vector3(2f, 2f, 0.1f));
        }
    }
    
    /// <summary>
    /// Converts a layer mask to readable string of layer names
    /// </summary>
    private string LayerMaskToString(LayerMask layerMask)
    {
        List<string> layers = new List<string>();
        for (int i = 0; i < 32; i++)
        {
            if ((layerMask & (1 << i)) != 0)
            {
                string layerName = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(layerName))
                {
                    layers.Add(layerName);
                }
                else
                {
                    layers.Add($"Layer {i}");
                }
            }
        }
        
        return string.Join(", ", layers);
    }
}

/// <summary>
/// Configuration for a single comic panel in the trigger
/// </summary>
[System.Serializable]
public class ComicPanelConfig
{
    public UnityEngine.UI.Image panelImage;
    public TransitionDirection transitionDirection = TransitionDirection.RIGHT;
} 