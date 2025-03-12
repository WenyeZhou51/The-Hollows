using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    // Add a serialized field for the font asset
    [SerializeField] private TMP_FontAsset permanentMarkerFont;
    
    private TextMeshPro textMesh;
    private float disappearTimer;
    private Color textColor;
    private Vector3 moveVector;
    // Increase the timer max to make the popup last longer
    private const float DISAPPEAR_TIMER_MAX = 1.2f;
    private const float MOVE_SPEED = 2f;
    private static int sortingOrder = 5000;

    public static DamagePopup Create(Vector3 position, float damageAmount, bool isPlayerDamage)
    {
        // Load the damage popup prefab from Resources folder
        GameObject damagePopupObject = new GameObject("DamagePopup");
        damagePopupObject.transform.position = position;

        DamagePopup damagePopup = damagePopupObject.AddComponent<DamagePopup>();
        
        // Find and assign the font asset
        damagePopup.permanentMarkerFont = Resources.Load<TMP_FontAsset>("Fonts/PermanentMarker-Regular SDF");
        
        damagePopup.Setup(damageAmount, isPlayerDamage);

        return damagePopup;
    }

    private void Awake()
    {
        textMesh = gameObject.AddComponent<TextMeshPro>();
    }

    public void Setup(float damageAmount, bool isPlayerDamage)
    {
        textMesh.fontSize = 4;
        textMesh.alignment = TextAlignmentOptions.Center;
        // Use red color for all physical damage (both player and enemy)
        textMesh.color = Color.red;
        textMesh.text = damageAmount.ToString();
        
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