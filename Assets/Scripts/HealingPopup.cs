using UnityEngine;
using TMPro;

public class HealingPopup : MonoBehaviour
{
    private TextMeshPro textMesh;
    private float disappearTimer;
    private Color textColor;
    private Vector3 moveVector;
    private const float DISAPPEAR_TIMER_MAX = 1f;
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

    public void Setup(float healAmount, bool isHealthHealing)
    {
        textMesh.fontSize = 4;
        textMesh.alignment = TextAlignmentOptions.Center;
        
        // Health healing is green, sanity healing is cyan
        textMesh.color = isHealthHealing ? Color.green : new Color(0f, 1f, 1f); // Cyan for sanity
        
        // Add a "+" prefix to clearly indicate healing
        textMesh.text = "+" + healAmount.ToString();
        
        // Ensure the text renders on top
        textMesh.sortingOrder = sortingOrder++;
        
        textColor = textMesh.color;
        disappearTimer = DISAPPEAR_TIMER_MAX;

        // Random movement direction but always moving upward
        moveVector = new Vector3(Random.Range(-1f, 1f), 1) * MOVE_SPEED;
    }

    private void Update()
    {
        transform.position += moveVector * Time.deltaTime;
        moveVector -= moveVector * 8f * Time.deltaTime;

        if (disappearTimer > DISAPPEAR_TIMER_MAX * 0.5f)
        {
            // First half of the popup lifetime: scale up
            float increaseScaleAmount = 1f;
            transform.localScale += Vector3.one * increaseScaleAmount * Time.deltaTime;
        }
        else
        {
            // Second half of the popup lifetime: scale down
            float decreaseScaleAmount = 1f;
            transform.localScale -= Vector3.one * decreaseScaleAmount * Time.deltaTime;
        }

        disappearTimer -= Time.deltaTime;
        if (disappearTimer < 0)
        {
            // Start disappearing
            float disappearSpeed = 3f;
            textColor.a -= disappearSpeed * Time.deltaTime;
            textMesh.color = textColor;

            if (textColor.a < 0)
            {
                Destroy(gameObject);
            }
        }
    }
} 