using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    
    [SerializeField] private float smoothSpeed = 0.05f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);
    
    [Header("Map Boundaries")]
    [SerializeField] private bool useBoundaries = true;
    [SerializeField] private Vector2 mapSize = new Vector2(100, 100); // Default size, set this in inspector
    [SerializeField] private Vector2 mapCenter = Vector2.zero; // Default center, adjust in inspector
    [SerializeField] private bool visuallyEditBounds = false;
    
    private Vector3 velocity = Vector3.zero;
    private Camera cam;
    
    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("No Camera component found on this object!");
            enabled = false;
        }
    }
    
    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }
        
        // Calculate desired position
        Vector3 desiredPosition = target.position + offset;
        
        // Apply camera boundaries if enabled
        if (useBoundaries)
        {
            // Calculate half of the camera's viewport in world units
            float verticalExtent = cam.orthographicSize;
            float horizontalExtent = verticalExtent * cam.aspect;
            
            // Calculate allowed camera positions based on map boundaries
            float minX = mapCenter.x - mapSize.x / 2 + horizontalExtent;
            float maxX = mapCenter.x + mapSize.x / 2 - horizontalExtent;
            float minY = mapCenter.y - mapSize.y / 2 + verticalExtent;
            float maxY = mapCenter.y + mapSize.y / 2 - verticalExtent;
            
            // Clamp the desired position within boundaries
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
        }
        
        // Use SmoothDamp with a lower smoothTime for smoother camera movement
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            desiredPosition, 
            ref velocity, 
            smoothSpeed,
            Mathf.Infinity,
            Time.unscaledDeltaTime); // Use unscaledDeltaTime to avoid time-scale issues
    }
    
    // Draw the camera boundaries in the editor for easier setup
    private void OnDrawGizmosSelected()
    {
        if (!useBoundaries) return;
        
        // Draw map boundaries
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(mapCenter, mapSize);
        
        // If we have a camera, also draw the visible area
        if (cam != null)
        {
            // Calculate camera bounds
            float verticalExtent = cam.orthographicSize;
            float horizontalExtent = verticalExtent * cam.aspect;
            Vector3 cameraBoundsSize = new Vector3(horizontalExtent * 2, verticalExtent * 2, 0.1f);
            
            // Draw current camera visible area
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, cameraBoundsSize);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(CameraFollow))]
public class CameraFollowEditor : Editor
{
    private CameraFollow cameraFollow;
    private SerializedProperty useBoundariesProp;
    private SerializedProperty mapSizeProp;
    private SerializedProperty mapCenterProp;
    private SerializedProperty visuallyEditBoundsProp;
    
    private void OnEnable()
    {
        cameraFollow = (CameraFollow)target;
        useBoundariesProp = serializedObject.FindProperty("useBoundaries");
        mapSizeProp = serializedObject.FindProperty("mapSize");
        mapCenterProp = serializedObject.FindProperty("mapCenter");
        visuallyEditBoundsProp = serializedObject.FindProperty("visuallyEditBounds");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // Draw default inspector excluding the properties we'll handle manually
        DrawPropertiesExcluding(serializedObject, "useBoundaries", "mapSize", "mapCenter", "visuallyEditBounds");
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Map Boundaries", EditorStyles.boldLabel);
        
        EditorGUILayout.PropertyField(useBoundariesProp);
        
        if (useBoundariesProp.boolValue)
        {
            EditorGUILayout.PropertyField(mapSizeProp);
            EditorGUILayout.PropertyField(mapCenterProp);
            
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(visuallyEditBoundsProp, new GUIContent("Edit in Scene View"));
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Center Map on Current Position"))
            {
                Undo.RecordObject(target, "Center Map");
                mapCenterProp.vector2Value = new Vector2(cameraFollow.transform.position.x, cameraFollow.transform.position.y);
            }
            
            if (GUILayout.Button("Fit to Current View"))
            {
                Undo.RecordObject(target, "Fit to Current View");
                Camera cam = cameraFollow.GetComponent<Camera>();
                if (cam != null)
                {
                    float verticalExtent = cam.orthographicSize * 2;
                    float horizontalExtent = verticalExtent * cam.aspect;
                    mapSizeProp.vector2Value = new Vector2(horizontalExtent, verticalExtent);
                    mapCenterProp.vector2Value = new Vector2(cameraFollow.transform.position.x, cameraFollow.transform.position.y);
                }
            }
        }
        
        serializedObject.ApplyModifiedProperties();
    }
    
    private void OnSceneGUI()
    {
        if (!cameraFollow.enabled || !useBoundariesProp.boolValue || !visuallyEditBoundsProp.boolValue)
            return;
        
        EditorGUI.BeginChangeCheck();
        
        // Get current values
        Vector2 mapCenter = mapCenterProp.vector2Value;
        Vector2 mapSize = mapSizeProp.vector2Value;
        
        // Draw handles for the corners
        Vector3[] corners = new Vector3[4];
        corners[0] = new Vector3(mapCenter.x - mapSize.x/2, mapCenter.y - mapSize.y/2, 0); // Bottom-left
        corners[1] = new Vector3(mapCenter.x + mapSize.x/2, mapCenter.y - mapSize.y/2, 0); // Bottom-right
        corners[2] = new Vector3(mapCenter.x + mapSize.x/2, mapCenter.y + mapSize.y/2, 0); // Top-right
        corners[3] = new Vector3(mapCenter.x - mapSize.x/2, mapCenter.y + mapSize.y/2, 0); // Top-left
        
        // Draw handles and update corners
        for (int i = 0; i < 4; i++)
        {
            Handles.color = Color.red;
            corners[i] = Handles.FreeMoveHandle(corners[i], Quaternion.identity, 1.0f, Vector3.zero, Handles.SphereHandleCap);
        }
        
        // Draw handle for center
        Handles.color = Color.yellow;
        Vector3 newCenter = Handles.FreeMoveHandle(
            new Vector3(mapCenter.x, mapCenter.y, 0),
            Quaternion.identity,
            1.0f,
            Vector3.zero,
            Handles.SphereHandleCap
        );
        
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(target, "Modify Map Boundaries");
            
            // If center was moved
            if (newCenter.x != mapCenter.x || newCenter.y != mapCenter.y)
            {
                Vector2 delta = new Vector2(newCenter.x - mapCenter.x, newCenter.y - mapCenter.y);
                mapCenterProp.vector2Value = new Vector2(newCenter.x, newCenter.y);
                
                // Don't update corners as they'll be moved with the center
                serializedObject.ApplyModifiedProperties();
                return;
            }
            
            // Calculate new size based on corner positions
            float minX = Mathf.Min(corners[0].x, corners[3].x);
            float maxX = Mathf.Max(corners[1].x, corners[2].x);
            float minY = Mathf.Min(corners[0].y, corners[1].y);
            float maxY = Mathf.Max(corners[2].y, corners[3].y);
            
            // Update map size and center
            Vector2 newSize = new Vector2(maxX - minX, maxY - minY);
            Vector2 newMapCenter = new Vector2(minX + newSize.x/2, minY + newSize.y/2);
            
            mapSizeProp.vector2Value = newSize;
            mapCenterProp.vector2Value = newMapCenter;
            
            serializedObject.ApplyModifiedProperties();
        }
        
        // Label the size near the bottom-right corner
        Handles.BeginGUI();
        Vector3 screenPoint = HandleUtility.WorldToGUIPoint(corners[1]);
        GUI.Label(new Rect(screenPoint.x + 10, screenPoint.y, 100, 20), 
            $"Size: {mapSize.x:F1} x {mapSize.y:F1}");
        Handles.EndGUI();
    }
}
#endif 