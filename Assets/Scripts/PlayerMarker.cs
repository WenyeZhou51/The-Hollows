using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerMarker : MonoBehaviour
{
    [SerializeField] private string markerId;
    
    // Scene-local ID provided by the user
    public string MarkerId => markerId;
    
    // Full ID that includes scene name for global uniqueness
    public string FullMarkerId => $"{SceneManager.GetActiveScene().name}:{markerId}";
    
    private void OnDrawGizmos()
    {
        // Visual representation in the editor
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, 0.5f);
        
        // Draw an arrow pointing up
        Gizmos.DrawRay(transform.position, Vector3.up * 1f);
        
        // Draw marker ID in the scene for easier identification
        #if UNITY_EDITOR
        string sceneName = gameObject.scene.name;
        string displayText = $"Marker: {markerId}\nScene: {sceneName}";
        
        UnityEditor.Handles.color = Color.white;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.2f, displayText);
        #endif
    }
} 