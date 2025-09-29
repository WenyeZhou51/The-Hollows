using UnityEngine;
using System.Collections.Generic;

public class InfiniteScrollBackground : MonoBehaviour
{
    [Header("Scrolling Settings")]
    [SerializeField] private Vector2 scrollDirection = new Vector2(1f, 0.5f); // Diagonal direction
    [SerializeField] private float scrollSpeed = 2f;
    [SerializeField] private bool normalizeDirection = true; // Normalize to maintain consistent speed
    [SerializeField] private bool enableScrolling = true; // Toggle scrolling on/off
    [SerializeField] private bool debugRepositioning = false; // Debug repositioning events
    
    [Header("Grid Configuration")]
    [SerializeField] private int gridWidth = 3;  // Number of tiles horizontally
    [SerializeField] private int gridHeight = 3; // Number of tiles vertically
    [SerializeField] private float bufferMultiplier = 1.5f; // Extra buffer to ensure no gaps
    
    [Header("Background Elements")]
    [SerializeField] private GameObject backgroundPrefab; // Prefab to instantiate
    [SerializeField] private Transform[,] backgroundGrid; // 2D array of background tiles
    
    private Camera mainCamera;
    private float elementWidth;
    private float elementHeight;
    private Vector2 actualScrollDirection;
    private bool isInitialized = false;
    
    void Start()
    {
        InitializeBackground();
    }
    
    void InitializeBackground()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("InfiniteScrollBackground: No main camera found!");
            return;
        }
        
        if (backgroundPrefab == null)
        {
            Debug.LogError("InfiniteScrollBackground: No background prefab assigned!");
            return;
        }
        
        // Normalize direction if requested
        actualScrollDirection = normalizeDirection ? scrollDirection.normalized : scrollDirection;
        
        // Get element dimensions from prefab
        SpriteRenderer sr = backgroundPrefab.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            elementWidth = sr.bounds.size.x;
            elementHeight = sr.bounds.size.y;
            Debug.Log($"InfiniteScrollBackground: Element dimensions - Width: {elementWidth}, Height: {elementHeight}");
        }
        else
        {
            Debug.LogError("InfiniteScrollBackground: Background prefab has no SpriteRenderer!");
            return;
        }
        
        // Create the grid of background elements
        CreateBackgroundGrid();
        isInitialized = true;
        Debug.Log("InfiniteScrollBackground: Initialization complete!");
    }
    
    void CreateBackgroundGrid()
    {
        backgroundGrid = new Transform[gridWidth, gridHeight];
        
        // Calculate starting position (centered around camera)
        Vector3 cameraPos = mainCamera.transform.position;
        float startX = cameraPos.x - (elementWidth * (gridWidth - 1) / 2f);
        float startY = cameraPos.y - (elementHeight * (gridHeight - 1) / 2f);
        
        // Instantiate grid elements
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                GameObject tile = Instantiate(backgroundPrefab, transform);
                tile.name = $"BG_Tile_{x}_{y}";
                
                float posX = startX + (x * elementWidth);
                float posY = startY + (y * elementHeight);
                tile.transform.position = new Vector3(posX, posY, 0);
                
                backgroundGrid[x, y] = tile.transform;
            }
        }
    }
    
    void LateUpdate()
    {
        if (!isInitialized || mainCamera == null || backgroundGrid == null || !enableScrolling)
            return;
            
        // Move all tiles in the scroll direction
        ScrollTiles();
        
        // Check and reposition tiles that have moved off-screen
        RepositionTiles();
    }
    
    void ScrollTiles()
    {
        Vector3 movement = new Vector3(
            actualScrollDirection.x * scrollSpeed * Time.deltaTime,
            actualScrollDirection.y * scrollSpeed * Time.deltaTime,
            0
        );
        
        // Move all tiles
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (backgroundGrid[x, y] != null)
                {
                    backgroundGrid[x, y].position -= movement;
                }
            }
        }
    }
    
    void RepositionTiles()
    {
        Vector3 cameraPos = mainCamera.transform.position;
        float camHeight = 2f * mainCamera.orthographicSize;
        float camWidth = camHeight * mainCamera.aspect;
        
        // Define the visible bounds with some buffer
        float leftBound = cameraPos.x - (camWidth * bufferMultiplier);
        float rightBound = cameraPos.x + (camWidth * bufferMultiplier);
        float bottomBound = cameraPos.y - (camHeight * bufferMultiplier);
        float topBound = cameraPos.y + (camHeight * bufferMultiplier);
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Transform tile = backgroundGrid[x, y];
                if (tile == null) continue;
                
                Vector3 tilePos = tile.position;
                bool repositioned = false;
                
                // Check horizontal wrapping - use grid-based repositioning to prevent stacking
                if (actualScrollDirection.x > 0 && tilePos.x < leftBound)
                {
                    // Tile has scrolled too far left, jump it to the right by grid width
                    float oldX = tilePos.x;
                    tilePos.x += elementWidth * gridWidth;
                    repositioned = true;
                    
                    if (debugRepositioning)
                    {
                        Debug.Log($"Repositioned tile [{x},{y}] horizontally: {oldX} -> {tilePos.x} (jumped by {elementWidth * gridWidth})");
                    }
                }
                else if (actualScrollDirection.x < 0 && tilePos.x > rightBound)
                {
                    // Tile has scrolled too far right, jump it to the left by grid width
                    float oldX = tilePos.x;
                    tilePos.x -= elementWidth * gridWidth;
                    repositioned = true;
                    
                    if (debugRepositioning)
                    {
                        Debug.Log($"Repositioned tile [{x},{y}] horizontally: {oldX} -> {tilePos.x} (jumped by -{elementWidth * gridWidth})");
                    }
                }
                
                // Check vertical wrapping - use grid-based repositioning to prevent stacking
                if (actualScrollDirection.y > 0 && tilePos.y < bottomBound)
                {
                    // Tile has scrolled too far down, jump it up by grid height
                    float oldY = tilePos.y;
                    tilePos.y += elementHeight * gridHeight;
                    repositioned = true;
                    
                    if (debugRepositioning)
                    {
                        Debug.Log($"Repositioned tile [{x},{y}] vertically: {oldY} -> {tilePos.y} (jumped by {elementHeight * gridHeight})");
                    }
                }
                else if (actualScrollDirection.y < 0 && tilePos.y > topBound)
                {
                    // Tile has scrolled too far up, jump it down by grid height
                    float oldY = tilePos.y;
                    tilePos.y -= elementHeight * gridHeight;
                    repositioned = true;
                    
                    if (debugRepositioning)
                    {
                        Debug.Log($"Repositioned tile [{x},{y}] vertically: {oldY} -> {tilePos.y} (jumped by -{elementHeight * gridHeight})");
                    }
                }
                
                if (repositioned)
                {
                    tile.position = tilePos;
                }
            }
        }
    }
    
    // Helper methods for debugging and visualization (kept for potential future use)
    float GetLeftmostPosition()
    {
        float leftmost = float.MaxValue;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (backgroundGrid[x, y] != null)
                {
                    float posX = backgroundGrid[x, y].position.x;
                    if (posX < leftmost) leftmost = posX;
                }
            }
        }
        return leftmost;
    }
    
    float GetRightmostPosition()
    {
        float rightmost = float.MinValue;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (backgroundGrid[x, y] != null)
                {
                    float posX = backgroundGrid[x, y].position.x;
                    if (posX > rightmost) rightmost = posX;
                }
            }
        }
        return rightmost;
    }
    
    float GetTopmostPosition()
    {
        float topmost = float.MinValue;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (backgroundGrid[x, y] != null)
                {
                    float posY = backgroundGrid[x, y].position.y;
                    if (posY > topmost) topmost = posY;
                }
            }
        }
        return topmost;
    }
    
    float GetBottommostPosition()
    {
        float bottommost = float.MaxValue;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (backgroundGrid[x, y] != null)
                {
                    float posY = backgroundGrid[x, y].position.y;
                    if (posY < bottommost) bottommost = posY;
                }
            }
        }
        return bottommost;
    }
    
    // Helper method to change scroll direction at runtime
    public void SetScrollDirection(Vector2 newDirection)
    {
        scrollDirection = newDirection;
        actualScrollDirection = normalizeDirection ? scrollDirection.normalized : scrollDirection;
    }
    
    // Helper method to change scroll speed at runtime
    public void SetScrollSpeed(float newSpeed)
    {
        scrollSpeed = newSpeed;
    }
    
    // Helper method to toggle scrolling
    public void SetScrollingEnabled(bool enabled)
    {
        enableScrolling = enabled;
    }
    
    // Helper method to get current scroll direction
    public Vector2 GetScrollDirection()
    {
        return actualScrollDirection;
    }
    
    // Helper method to get current scroll speed
    public float GetScrollSpeed()
    {
        return scrollSpeed;
    }
    
    // Helper method to check if scrolling is enabled
    public bool IsScrollingEnabled()
    {
        return enableScrolling;
    }
    
    // Helper method to visualize the grid in Scene view
    void OnDrawGizmos()
    {
        if (backgroundGrid == null) return;
        
        Gizmos.color = Color.green;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (backgroundGrid[x, y] != null)
                {
                    Gizmos.DrawWireCube(backgroundGrid[x, y].position, 
                        new Vector3(elementWidth, elementHeight, 0));
                }
            }
        }
        
        // Draw scroll direction
        if (mainCamera != null)
        {
            Gizmos.color = Color.red;
            Vector3 camPos = mainCamera.transform.position;
            Gizmos.DrawRay(camPos, actualScrollDirection * 5f);
        }
    }
}
