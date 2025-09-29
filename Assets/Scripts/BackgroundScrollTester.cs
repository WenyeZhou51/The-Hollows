using UnityEngine;

/// <summary>
/// Test script for the diagonal scrolling background
/// Provides runtime controls and debugging information
/// </summary>
public class BackgroundScrollTester : MonoBehaviour
{
    [Header("Test Controls")]
    [SerializeField] private KeyCode toggleScrollingKey = KeyCode.Space;
    [SerializeField] private KeyCode speedUpKey = KeyCode.Plus;
    [SerializeField] private KeyCode speedDownKey = KeyCode.Minus;
    [SerializeField] private KeyCode changeDirectionKey = KeyCode.D;
    
    [Header("Debug Info")]
    [SerializeField] private bool showDebugInfo = true;
    
    private InfiniteScrollBackground scrollBackground;
    private float originalSpeed;
    private Vector2 originalDirection;
    private bool isScrollingEnabled = true;
    
    void Start()
    {
        // Find the scrolling background component
        scrollBackground = FindObjectOfType<InfiniteScrollBackground>();
        
        if (scrollBackground != null)
        {
            originalSpeed = scrollBackground.GetScrollSpeed();
            originalDirection = scrollBackground.GetScrollDirection();
            Debug.Log($"BackgroundScrollTester: Found scrolling background with speed {originalSpeed} and direction {originalDirection}");
        }
        else
        {
            Debug.LogError("BackgroundScrollTester: No InfiniteScrollBackground found in scene!");
        }
    }
    
    void Update()
    {
        if (scrollBackground == null) return;
        
        // Toggle scrolling
        if (Input.GetKeyDown(toggleScrollingKey))
        {
            isScrollingEnabled = !isScrollingEnabled;
            scrollBackground.SetScrollingEnabled(isScrollingEnabled);
            Debug.Log($"BackgroundScrollTester: Scrolling {(isScrollingEnabled ? "enabled" : "disabled")}");
        }
        
        // Speed controls
        if (Input.GetKeyDown(speedUpKey))
        {
            float newSpeed = scrollBackground.GetScrollSpeed() + 0.5f;
            scrollBackground.SetScrollSpeed(newSpeed);
            Debug.Log($"BackgroundScrollTester: Speed increased to {newSpeed}");
        }
        
        if (Input.GetKeyDown(speedDownKey))
        {
            float newSpeed = Mathf.Max(0.1f, scrollBackground.GetScrollSpeed() - 0.5f);
            scrollBackground.SetScrollSpeed(newSpeed);
            Debug.Log($"BackgroundScrollTester: Speed decreased to {newSpeed}");
        }
        
        // Change direction
        if (Input.GetKeyDown(changeDirectionKey))
        {
            Vector2 currentDir = scrollBackground.GetScrollDirection();
            Vector2 newDir = new Vector2(-currentDir.x, currentDir.y); // Flip X direction
            scrollBackground.SetScrollDirection(newDir);
            Debug.Log($"BackgroundScrollTester: Direction changed to {newDir}");
        }
    }
    
    void OnGUI()
    {
        if (!showDebugInfo || scrollBackground == null) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("Background Scroll Tester", GUI.skin.box);
        GUILayout.Space(5);
        
        GUILayout.Label($"Scrolling: {(scrollBackground.IsScrollingEnabled() ? "ON" : "OFF")}");
        GUILayout.Label($"Speed: {scrollBackground.GetScrollSpeed():F1}");
        GUILayout.Label($"Direction: {scrollBackground.GetScrollDirection()}");
        
        GUILayout.Space(10);
        GUILayout.Label("Controls:");
        GUILayout.Label($"{toggleScrollingKey} - Toggle scrolling");
        GUILayout.Label($"{speedUpKey}/{speedDownKey} - Speed up/down");
        GUILayout.Label($"{changeDirectionKey} - Change direction");
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
