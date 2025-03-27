using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StartMenuBackground : MonoBehaviour
{
    [Header("Animation Frames")]
    [Tooltip("Drag the sequence of sprites (1, 2, 3, 4) that will be shown when animating")]
    [SerializeField] private Sprite[] animationFrames;
    
    [Header("Animation Settings")]
    [Tooltip("Time between each frame transition")]
    [SerializeField] private float frameRate = 0.2f;
    
    private Image backgroundImage;
    private bool isAnimating = false;
    
    private void Awake()
    {
        // Get the Image component
        backgroundImage = GetComponent<Image>();
        
        // Set the image to fully opaque
        if (backgroundImage != null)
        {
            Color currentColor = backgroundImage.color;
            currentColor.a = 1f; // Set alpha to 1 (fully opaque)
            backgroundImage.color = currentColor;
        }
        
        // Set the initial frame if we have frames
        if (backgroundImage != null && animationFrames.Length > 0)
        {
            backgroundImage.sprite = animationFrames[0];
        }
    }
    
    /// <summary>
    /// Plays the background animation sequence and returns when complete
    /// </summary>
    /// <returns>Coroutine that can be awaited</returns>
    public IEnumerator PlayAnimation()
    {
        if (isAnimating || animationFrames.Length == 0 || backgroundImage == null)
        {
            yield break;
        }
        
        isAnimating = true;
        
        // Play through all animation frames
        for (int i = 0; i < animationFrames.Length; i++)
        {
            backgroundImage.sprite = animationFrames[i];
            yield return new WaitForSeconds(frameRate);
        }
        
        isAnimating = false;
    }
    
    /// <summary>
    /// Check if the animation is currently playing
    /// </summary>
    /// <returns>True if animating, false otherwise</returns>
    public bool IsAnimating()
    {
        return isAnimating;
    }
} 