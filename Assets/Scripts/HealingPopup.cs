using UnityEngine;
using TMPro;

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

    // Create a health healing popup (green)
    public static HealingPopup CreateHealthPopup(Vector3 position, float healAmount)
    {
        GameObject healingPopupObject = new GameObject("HealthHealingPopup");
        healingPopupObject.transform.position = position;

        HealingPopup healingPopup = healingPopupObject.AddComponent<HealingPopup>();
        healingPopup.Setup(healAmount, true);

        return healingPopup;
    }

    // Create a sanity healing popup (cyan)
    public static HealingPopup CreateSanityPopup(Vector3 position, float healAmount)
    {
        GameObject healingPopupObject = new GameObject("SanityHealingPopup");
        healingPopupObject.transform.position = position;

        HealingPopup healingPopup = healingPopupObject.AddComponent<HealingPopup>();
        healingPopup.Setup(healAmount, false);

        return healingPopup;
    }

    private void Awake()
    {
        textMesh = gameObject.AddComponent<TextMeshPro>();
    }

    public void Setup(float healAmount, bool isSanityHealing)
    {
        textMesh.fontSize = 4;
        textMesh.alignment = TextAlignmentOptions.Center;
        
        // Use green for physical healing and blue for sanity healing
        textMesh.color = isSanityHealing ? Color.blue : Color.green;
        textMesh.text =  healAmount.ToString();
        
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