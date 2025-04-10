using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class DamagePopup : MonoBehaviour
{
    // Add a serialized field for the font asset
    [SerializeField] private TMP_FontAsset permanentMarkerFont;
    
    private TextMeshPro textMesh;
    private float disappearTimer;
    private Color textColor;
    private Vector3 moveVector;
    // Increase the timer max to make the popup last longer
    private const float DISAPPEAR_TIMER_MAX = 2f;
    private const float MOVE_SPEED = 2f;
    private static int sortingOrder = 5000;
    
    // Track popup stacking within a frame
    private static Dictionary<Transform, int> popupCountPerTarget = new Dictionary<Transform, int>();
    private static float POPUP_STACK_OFFSET = 0.5f; // Vertical offset between stacked popups
    private static float POPUP_RESET_DELAY = 1.0f; // Time before resetting the counter for a target
    
    // Time tracking to reset popup counts
    private static float lastFrameTime = 0f;

    public static DamagePopup Create(Vector3 position, float damageAmount, bool isPlayerDamage, Transform targetTransform = null)
    {
        // Ensure damage is always a whole number
        int wholeDamage = Mathf.FloorToInt(damageAmount);
        
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
        
        // Load the damage popup prefab from Resources folder
        GameObject damagePopupObject = new GameObject("DamagePopup");
        damagePopupObject.transform.position = position;

        DamagePopup damagePopup = damagePopupObject.AddComponent<DamagePopup>();
        
        // Find and assign the font asset
        damagePopup.permanentMarkerFont = Resources.Load<TMP_FontAsset>("Fonts/PermanentMarker-Regular SDF");
        
        damagePopup.Setup(wholeDamage, isPlayerDamage);

        return damagePopup;
    }

    private void Awake()
    {
        textMesh = gameObject.AddComponent<TextMeshPro>();
    }

    public void Setup(float damageAmount, bool isPlayerDamage)
    {
        // Ensure damage is always a whole number
        int wholeDamage = Mathf.FloorToInt(damageAmount);
        
        // Increase font size for bigger numbers
        textMesh.fontSize = 5;
        textMesh.alignment = TextAlignmentOptions.Center;
        // Use red color for all physical damage (both player and enemy)
        textMesh.color = Color.red;
        textMesh.text = wholeDamage.ToString();
        
        // Make text bold
        textMesh.fontStyle = FontStyles.Bold;
        
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
        
        // Make the scale slightly larger
        transform.localScale = Vector3.one * 1.2f;
    }

    private void Update()
    {
        transform.position += moveVector * Time.deltaTime;
        moveVector -= moveVector * 8f * Time.deltaTime;

        // Smoothly decrease opacity throughout the lifetime
        disappearTimer -= Time.deltaTime;
        float fadeRatio = disappearTimer / DISAPPEAR_TIMER_MAX;
        
        // Use a smoother fade curve (start fading faster toward the end)
        textColor.a = fadeRatio * fadeRatio;
        textMesh.color = textColor;

        if (disappearTimer < 0)
        {
            Destroy(gameObject);
        }
    }
} 