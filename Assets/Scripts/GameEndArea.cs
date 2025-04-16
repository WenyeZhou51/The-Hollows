using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameEndArea : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float walkDuration = 15f;
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float fadeOutDuration = 3f;
    
    [Header("Optional")]
    [SerializeField] private bool showCreditsBeforeQuit = false;
    [SerializeField] private string creditsSceneName = "Credits";
    [SerializeField] private AudioClip endingMusic;

    private bool hasBeenTriggered = false;
    private PlayerController playerController;
    private Rigidbody2D playerRigidbody;
    private Animator playerAnimator;

    private void Start()
    {
        // Make sure we have a collider for the trigger
        if (GetComponent<Collider2D>() == null)
        {
            BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            Debug.Log("Added BoxCollider2D to GameEndArea");
        }
        else
        {
            // If there's already a collider, make sure it's set as a trigger
            Collider2D existingCollider = GetComponent<Collider2D>();
            existingCollider.isTrigger = true;
        }
        
        // Ensure ScreenFader exists
        if (ScreenFader.Instance == null)
        {
            ScreenFader.EnsureExists();
        }

        Debug.Log("GameEndArea initialized");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only trigger once
        if (hasBeenTriggered) return;
        
        // Check if the object entering the trigger is the player
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entered GameEndArea - starting end sequence");
            hasBeenTriggered = true;
            
            // Get references to player components
            playerController = other.GetComponent<PlayerController>();
            playerRigidbody = other.GetComponent<Rigidbody2D>();
            playerAnimator = other.GetComponent<Animator>();
            
            if (playerController != null)
            {
                // Disable player control
                playerController.SetCanMove(false);
                
                // Start the end sequence
                StartCoroutine(EndGameSequence(other.gameObject));
            }
            else
            {
                Debug.LogError("PlayerController not found on player!");
            }
        }
    }
    
    private IEnumerator EndGameSequence(GameObject player)
    {
        // Play ending music if assigned
        if (endingMusic != null)
        {
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            audioSource.clip = endingMusic;
            audioSource.loop = true;
            audioSource.Play();
            
            // Lower any other music that might be playing
            AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
            foreach (AudioSource source in allAudioSources)
            {
                if (source != audioSource)
                {
                    StartCoroutine(FadeAudioSource(source, 1.5f, 0.2f));
                }
            }
        }
        
        // Get the main camera but don't move it during the sequence
        Camera mainCamera = Camera.main;
        
        // Disable any camera follow scripts that might be attached
        if (mainCamera != null)
        {
            // Try to find and disable camera follow scripts
            MonoBehaviour[] cameraScripts = mainCamera.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in cameraScripts)
            {
                // Look for common camera follow script names
                if (script.GetType().Name.Contains("CameraFollow") || 
                    script.GetType().Name.Contains("CameraController") ||
                    script.GetType().Name.Contains("Follow"))
                {
                    script.enabled = false;
                    Debug.Log($"Disabled camera script: {script.GetType().Name}");
                }
            }
            
            // Also check for camera scripts on the parent
            if (mainCamera.transform.parent != null)
            {
                MonoBehaviour[] parentScripts = mainCamera.transform.parent.GetComponents<MonoBehaviour>();
                foreach (MonoBehaviour script in parentScripts)
                {
                    if (script.GetType().Name.Contains("CameraFollow") || 
                        script.GetType().Name.Contains("CameraController") ||
                        script.GetType().Name.Contains("Follow"))
                    {
                        script.enabled = false;
                        Debug.Log($"Disabled parent camera script: {script.GetType().Name}");
                    }
                }
            }
            
            Debug.Log("Camera will remain stationary during end sequence");
        }
        
        // Make player walk to the right
        if (playerAnimator != null)
        {
            playerAnimator.SetBool("IsMoving", true);
            playerAnimator.SetFloat("Horizontal", 1);
            playerAnimator.SetFloat("Vertical", 0);
            playerAnimator.speed = 1;
            Debug.Log("Set player animation to walk right");
        }
        
        // Make sure any sprite renderer is facing right
        SpriteRenderer playerSprite = player.GetComponent<SpriteRenderer>();
        if (playerSprite != null)
        {
            playerSprite.flipX = false;
        }
        
        // Walk right for the specified duration
        float elapsedTime = 0;
        while (elapsedTime < walkDuration)
        {
            // Move the player to the right
            if (playerRigidbody != null)
            {
                playerRigidbody.velocity = new Vector2(walkSpeed, 0);
            }
            else
            {
                // Fallback if no rigidbody
                player.transform.Translate(Vector3.right * walkSpeed * Time.deltaTime);
            }
            
            // Camera stays in place - no camera update code here
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Stop the player
        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector2.zero;
        }
        
        // Stop the walking animation
        if (playerAnimator != null)
        {
            playerAnimator.SetBool("IsMoving", false);
        }
        
        Debug.Log("Player walk complete - starting fade out");
        
        // Fade to black
        if (ScreenFader.Instance != null)
        {
            yield return ScreenFader.Instance.FadeOut(fadeOutDuration);
        }
        else
        {
            // Simple fade if no ScreenFader
            float fadeTime = 0;
            Color fadeColor = Color.black;
            fadeColor.a = 0;
            
            // Create a texture to draw the fade
            Texture2D fadeTexture = new Texture2D(1, 1);
            fadeTexture.SetPixel(0, 0, Color.black);
            fadeTexture.Apply();
            
            while (fadeTime < fadeOutDuration)
            {
                fadeTime += Time.deltaTime;
                fadeColor.a = Mathf.Clamp01(fadeTime / fadeOutDuration);
                yield return null;
            }
            
            // Hold black screen for a moment
            yield return new WaitForSeconds(1.0f);
        }
        
        Debug.Log("Fade complete - finishing game");
        
        // Hold the black screen for a moment
        yield return new WaitForSeconds(2.0f);
        
        // Either load credits or quit
        if (showCreditsBeforeQuit && !string.IsNullOrEmpty(creditsSceneName))
        {
            Debug.Log("Loading credits scene: " + creditsSceneName);
            SceneManager.LoadScene(creditsSceneName);
        }
        else
        {
            Debug.Log("Quitting application");
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }
    
    private IEnumerator FadeAudioSource(AudioSource audioSource, float duration, float targetVolume)
    {
        float startVolume = audioSource.volume;
        float time = 0;
        
        while (time < duration)
        {
            time += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, time / duration);
            yield return null;
        }
        
        audioSource.volume = targetVolume;
    }
    
    // Draw the trigger area in the editor
    private void OnDrawGizmos()
    {
        // Visualize the trigger area with a semi-transparent color
        Gizmos.color = new Color(0.8f, 0.2f, 0.2f, 0.3f);
        
        // Use the collider bounds if available, otherwise use a default size
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            // Draw the actual collider shape
            if (collider is BoxCollider2D)
            {
                BoxCollider2D boxCollider = collider as BoxCollider2D;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(boxCollider.offset, boxCollider.size);
            }
            else
            {
                // For other collider types, just show the bounds
                Gizmos.DrawCube(collider.bounds.center, collider.bounds.size);
            }
        }
        else
        {
            // No collider yet, draw a default box
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero, new Vector3(3f, 2f, 0.1f));
        }
        
        // Draw an arrow pointing to the right to indicate walk direction
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
        Vector3 center = transform.position;
        Vector3 right = center + Vector3.right * 3f;
        Gizmos.DrawLine(center, right);
        Gizmos.DrawLine(right, right + new Vector3(-0.5f, 0.5f, 0));
        Gizmos.DrawLine(right, right + new Vector3(-0.5f, -0.5f, 0));
    }
} 