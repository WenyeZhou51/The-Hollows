using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class HealingPopup : MonoBehaviour
{
    // Add a serialized field for the font asset
    [SerializeField] private TMP_FontAsset permanentMarkerFont;
    
    private TextMeshPro textMesh;
    private float disappearTimer;
    private Color textColor;
    private Vector3 moveVector;
    // Match the timer with DamagePopup
    private const float DISAPPEAR_TIMER_MAX = 1.2f;
    private const float MOVE_SPEED = 2f;
    private static int sortingOrder = 5000;
    
    // Track popup stacking within a frame
    private static Dictionary<Transform, int> popupCountPerTarget = new Dictionary<Transform, int>();
    private static float POPUP_STACK_OFFSET = 0.5f; // Vertical offset between stacked popups
    private static float POPUP_RESET_DELAY = 1.0f; // Time before resetting the counter for a target
    
    // Time tracking to reset popup counts
    private static float lastFrameTime = 0f;

    // Create a health healing popup (green)
    public static HealingPopup CreateHealthPopup(Vector3 position, float healAmount, Transform targetTransform = null)
    {
        // Ensure heal amount is always a whole number
        int wholeHealAmount = Mathf.FloorToInt(healAmount);
        
        // Check if we need to reset popup counts (if more than POPUP_RESET_DELAY has passed since last popup)
        float currentTime = Time.time;
        if (currentTime > lastFrameTime + POPUP_RESET_DELAY)
        {
            popupCountPerTarget.Clear();
        }
        lastFrameTime = currentTime;
        
        // If we have a target transform, use it to track popup stacking
        if (targetTransform != null)
        {
            // Initialize or increment the popup count for this target
            if (!popupCountPerTarget.ContainsKey(targetTransform))
            {
                popupCountPerTarget[targetTransform] = 0;
            }
            int popupCount = popupCountPerTarget[targetTransform]++;
            
            // Apply vertical offset based on number of existing popups
            position += new Vector3(0, POPUP_STACK_OFFSET * popupCount, 0);
        }
        
        GameObject healingPopupObject = new GameObject("HealthHealingPopup");
        healingPopupObject.transform.position = position;

        HealingPopup healingPopup = healingPopupObject.AddComponent<HealingPopup>();
        healingPopup.Setup(wholeHealAmount, true);

        return healingPopup;
    }

    // Create a sanity healing popup (cyan)
    public static HealingPopup CreateSanityPopup(Vector3 position, float healAmount, Transform targetTransform = null)
    {
        // Ensure heal amount is always a whole number
        int wholeHealAmount = Mathf.FloorToInt(healAmount);
        
        // Check if we need to reset popup counts (if more than POPUP_RESET_DELAY has passed since last popup)
        float currentTime = Time.time;
        if (currentTime > lastFrameTime + POPUP_RESET_DELAY)
        {
            popupCountPerTarget.Clear();
        }
        lastFrameTime = currentTime;
        
        // If we have a target transform, use it to track popup stacking
        if (targetTransform != null)
        {
            // Initialize or increment the popup count for this target
            if (!popupCountPerTarget.ContainsKey(targetTransform))
            {
                popupCountPerTarget[targetTransform] = 0;
            }
            int popupCount = popupCountPerTarget[targetTransform]++;
            
            // Apply vertical offset based on number of existing popups
            position += new Vector3(0, POPUP_STACK_OFFSET * popupCount, 0);
        }
        
        GameObject healingPopupObject = new GameObject("SanityHealingPopup");
        healingPopupObject.transform.position = position;

        HealingPopup healingPopup = healingPopupObject.AddComponent<HealingPopup>();
        healingPopup.Setup(wholeHealAmount, false);

        return healingPopup;
    }

    private void Awake()
    {
        textMesh = gameObject.AddComponent<TextMeshPro>();
    }

    public void Setup(float healAmount, bool isSanityHealing)
    {
        // Ensure heal amount is always a whole number
        int wholeHealAmount = Mathf.FloorToInt(healAmount);
        
        textMesh.fontSize = 4;
        textMesh.alignment = TextAlignmentOptions.Center;
        
        // Use green for physical healing and blue for sanity healing
        textMesh.color = isSanityHealing ? Color.blue : Color.green;
        textMesh.text = wholeHealAmount.ToString();
        
        // Apply the Permanent Marker font if available
        if (permanentMarkerFont != null)
        {
            textMesh.font = permanentMarkerFont;
        }
        else
        {
            Debug.LogWarning("Permanent Marker font not found!");
        }
        
        // Ensure the text renders on top
        textMesh.sortingOrder = sortingOrder++;
        
        textColor = textMesh.color;
        disappearTimer = DISAPPEAR_TIMER_MAX;

        // Random movement direction
        moveVector = new Vector3(Random.Range(-1f, 1f), 1) * MOVE_SPEED;
        
        // Set a fixed scale instead of changing it over time
        transform.localScale = Vector3.one;
    }

    private void Update()
    {
        transform.position += moveVector * Time.deltaTime;
        moveVector -= moveVector * 8f * Time.deltaTime;

        // Continuously decrease opacity throughout the lifetime
        disappearTimer -= Time.deltaTime;
        float fadeRatio = disappearTimer / DISAPPEAR_TIMER_MAX;
        textColor.a = fadeRatio;
        textMesh.color = textColor;

        if (disappearTimer < 0)
        {
            Destroy(gameObject);
        }
    }
} 