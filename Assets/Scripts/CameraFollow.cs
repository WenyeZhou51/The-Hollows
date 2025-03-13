using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    
    [SerializeField] private float smoothSpeed = 0.05f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);
    
    private Vector3 velocity = Vector3.zero;
    
    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }
        
        // Calculate desired position
        Vector3 desiredPosition = target.position + offset;
        
        // Use SmoothDamp with a lower smoothTime for smoother camera movement
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            desiredPosition, 
            ref velocity, 
            smoothSpeed,
            Mathf.Infinity,
            Time.unscaledDeltaTime); // Use unscaledDeltaTime to avoid time-scale issues
    }
} 