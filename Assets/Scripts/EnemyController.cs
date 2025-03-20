using UnityEngine;
using Pathfinding;

public class EnemyController : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // The player to chase

    [Header("Pathfinding")]
    public float activationDistance = 7f; // Distance at which enemy starts chasing
    public float stoppingDistance = 1f; // Distance to stop chasing/attacking
    
    [Header("Enemy Properties")]
    public bool faceTarget = true;
    public float rotationSpeed = 10f;
    
    // References to A* components
    private AIPath aiPath;
    private AIDestinationSetter aiDestinationSetter;
    
    // State tracking
    private bool isChasing = false;
    private Vector3 originalScale; // Store the original scale
    private SpriteRenderer spriteRenderer; // Reference to sprite renderer
    
    private void Start()
    {
        // Store original scale
        originalScale = transform.localScale;
        
        // Get sprite renderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Get components
        aiPath = GetComponent<AIPath>();
        aiDestinationSetter = GetComponent<AIDestinationSetter>();
        
        // Check if components exist
        if (aiPath == null)
        {
            Debug.LogError("AIPath component missing from " + gameObject.name + ". Add it in the inspector.");
            return;
        }
        
        if (aiDestinationSetter == null)
        {
            Debug.LogError("AIDestinationSetter component missing from " + gameObject.name + ". Add it in the inspector.");
            return;
        }
        
        // Set up target if assigned
        if (target != null)
        {
            aiDestinationSetter.target = target;
        }
        else
        {
            // Find the player in the scene if target is not manually assigned
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                aiDestinationSetter.target = target;
                Debug.Log("Automatically assigned player as target.");
            }
            else
            {
                Debug.LogWarning("No target assigned to " + gameObject.name + " and no player found with 'Player' tag.");
            }
        }
        
        // Configure AI path properties
        aiPath.endReachedDistance = stoppingDistance;
        
        // Initially disable movement until player is in range
        aiPath.canMove = false;
        
        // Configure for 2D
        if (aiPath != null)
        {
            // Disable rotation by A* path system, we'll handle it manually for 2D
            aiPath.enableRotation = false;
            aiPath.orientation = OrientationMode.YAxisForward;
        }
    }
    
    private void Update()
    {
        if (target == null) return;
        
        // Calculate distance to target
        float distanceToTarget = Vector2.Distance(transform.position, target.position);
        
        // Handle activation based on distance
        if (distanceToTarget <= activationDistance)
        {
            if (!isChasing)
            {
                isChasing = true;
                aiPath.canMove = true;
                Debug.Log("Enemy has spotted player, beginning chase");
            }
            
            // Only stop at stopping distance if we're chasing
            if (distanceToTarget <= stoppingDistance)
            {
                aiPath.canMove = false;
                
                // Optional: Trigger attack or other behavior when in range
                // Attack();
            }
            else
            {
                // Make sure we're moving if we're chasing but not close enough to stop
                aiPath.canMove = true;
            }
        }
        else
        {
            if (isChasing)
            {
                isChasing = false;
                aiPath.canMove = false;
                Debug.Log("Enemy lost sight of player, stopping chase");
            }
        }
        
        // Handle sprite flipping for 2D games instead of rotation
        if (faceTarget && target != null && spriteRenderer != null)
        {
            // Determine which way to face based on target position
            Vector2 direction = (target.position - transform.position);
            
            // Use absolute scale value to maintain size while flipping
            float xScale = Mathf.Abs(originalScale.x);
            
            // Flip the sprite based on direction
            if (direction.x > 0)
            {
                // Target is to the right, face right
                transform.localScale = new Vector3(xScale, originalScale.y, originalScale.z);
            }
            else if (direction.x < 0)
            {
                // Target is to the left, face left
                transform.localScale = new Vector3(-xScale, originalScale.y, originalScale.z);
            }
            
            // No rotation needed for 2D sprite flipping
        }
    }
    
    // Add collision handling to prevent thinning and disappearing
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Stop the enemy from moving when it collides with the player
            if (aiPath != null)
            {
                aiPath.canMove = false;
            }
            
            // Make sure the scale is maintained properly
            float xScale = Mathf.Abs(originalScale.x);
            if (target != null)
            {
                // Maintain facing direction even during collision
                if (target.position.x > transform.position.x)
                {
                    transform.localScale = new Vector3(xScale, originalScale.y, originalScale.z);
                }
                else
                {
                    transform.localScale = new Vector3(-xScale, originalScale.y, originalScale.z);
                }
            }
            
            // Optional: Add any damage or gameplay logic here
            // playerHealth.TakeDamage(damageAmount);
        }
    }
    
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Resume chasing if the player is still in range
            if (isChasing && target != null)
            {
                float distanceToTarget = Vector2.Distance(transform.position, target.position);
                if (distanceToTarget > stoppingDistance && distanceToTarget <= activationDistance)
                {
                    aiPath.canMove = true;
                }
            }
        }
    }
    
    // Visualize detection ranges in the editor
    private void OnDrawGizmosSelected()
    {
        // Draw activation radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationDistance);
        
        // Draw stopping radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);
    }
} 