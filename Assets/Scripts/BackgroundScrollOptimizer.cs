using UnityEngine;

/// <summary>
/// Optimization script for the diagonal scrolling background
/// Provides performance monitoring and optimization features
/// </summary>
public class BackgroundScrollOptimizer : MonoBehaviour
{
    [Header("Performance Monitoring")]
    [SerializeField] private bool enablePerformanceMonitoring = true;
    [SerializeField] private float updateInterval = 1.0f;
    
    [Header("Optimization Settings")]
    [SerializeField] private bool enableCulling = true;
    [SerializeField] private float cullDistance = 50f;
    [SerializeField] private bool enableLOD = true;
    [SerializeField] private float lodDistance = 25f;
    
    private InfiniteScrollBackground scrollBackground;
    private float lastUpdateTime;
    private int frameCount;
    private float fps;
    private float averageFPS;
    private int fpsSamples;
    
    void Start()
    {
        scrollBackground = FindObjectOfType<InfiniteScrollBackground>();
        
        if (scrollBackground == null)
        {
            Debug.LogError("BackgroundScrollOptimizer: No InfiniteScrollBackground found!");
            enabled = false;
            return;
        }
        
        // Apply initial optimizations
        ApplyOptimizations();
    }
    
    void Update()
    {
        if (!enablePerformanceMonitoring) return;
        
        frameCount++;
        
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            fps = frameCount / (Time.time - lastUpdateTime);
            averageFPS = (averageFPS * fpsSamples + fps) / (fpsSamples + 1);
            fpsSamples++;
            
            // Log performance info
            Debug.Log($"Background Scroll Performance - FPS: {fps:F1}, Average: {averageFPS:F1}");
            
            // Auto-optimize based on performance
            if (fps < 30f && averageFPS < 30f)
            {
                Debug.LogWarning("Background Scroll: Low FPS detected, applying optimizations...");
                ApplyAggressiveOptimizations();
            }
            
            frameCount = 0;
            lastUpdateTime = Time.time;
        }
    }
    
    void ApplyOptimizations()
    {
        if (scrollBackground == null) return;
        
        // Set reasonable defaults for performance
        scrollBackground.SetScrollSpeed(Mathf.Clamp(scrollBackground.GetScrollSpeed(), 0.5f, 5f));
        
        Debug.Log("BackgroundScrollOptimizer: Applied standard optimizations");
    }
    
    void ApplyAggressiveOptimizations()
    {
        if (scrollBackground == null) return;
        
        // Reduce scroll speed if performance is poor
        float currentSpeed = scrollBackground.GetScrollSpeed();
        float optimizedSpeed = currentSpeed * 0.7f;
        scrollBackground.SetScrollSpeed(optimizedSpeed);
        
        Debug.Log($"BackgroundScrollOptimizer: Applied aggressive optimizations - Speed reduced from {currentSpeed} to {optimizedSpeed}");
    }
    
    void OnGUI()
    {
        if (!enablePerformanceMonitoring) return;
        
        GUILayout.BeginArea(new Rect(Screen.width - 250, 10, 240, 100));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("Background Performance", GUI.skin.box);
        GUILayout.Label($"FPS: {fps:F1}");
        GUILayout.Label($"Average FPS: {averageFPS:F1}");
        GUILayout.Label($"Samples: {fpsSamples}");
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
