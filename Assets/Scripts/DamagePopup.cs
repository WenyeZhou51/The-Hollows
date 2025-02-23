using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    private TextMeshPro textMesh;
    private float disappearTimer;
    private Color textColor;
    private Vector3 moveVector;
    private const float DISAPPEAR_TIMER_MAX = 1f;
    private const float MOVE_SPEED = 2f;
    private static int sortingOrder = 5000;

    public static DamagePopup Create(Vector3 position, float damageAmount, bool isPlayerDamage)
    {
        GameObject damagePopupObject = new GameObject("DamagePopup");
        damagePopupObject.transform.position = position;

        DamagePopup damagePopup = damagePopupObject.AddComponent<DamagePopup>();
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
        textMesh.color = isPlayerDamage ? Color.red : Color.yellow;
        textMesh.text = damageAmount.ToString();
        
        // Ensure the text renders on top
        textMesh.sortingOrder = sortingOrder++;
        
        textColor = textMesh.color;
        disappearTimer = DISAPPEAR_TIMER_MAX;

        // Random movement direction
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