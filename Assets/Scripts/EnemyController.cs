using UnityEngine;
using Pathfinding;
using System.Collections;

public class EnemyController : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // The player to chase

    [Header("Pathfinding")]
    public float sightDistance = 5f; // Distance at which enemy can see player
    public float stoppingDistance = 1f; // Distance to stop chasing/attacking
    public LayerMask obstacleLayer; // Layers that block line of sight
    
    [Header("Enemy Properties")]
    public bool faceTarget = true;
    public float rotationSpeed = 10f;
    
    [Header("Wandering Behavior")]
    public float wanderRadius = 3f; // How far the enemy wanders from its starting position
    public float wanderPauseDuration = 2f; // Time to pause between wander movements
    
    [Header("Corner Handling")]
    public float cornerCutDistance = 0.5f; // How much to cut corners
    public float pathResetTime = 1f; // How often to recalculate the path
    
    // References to A* components
    private AIPath aiPath;
    private AIDestinationSetter aiDestinationSetter;
    private Seeker seeker;
    private Animator animator;
    
    // State tracking
    private bool hasLineOfSight = false;
    private float lostSightTimer = 0f;
    private float timeToLoseSight = 2f; // Time before forgetting player after losing sight
    private Vector3 originalScale; // Store the original scale
    private SpriteRenderer spriteRenderer; // Reference to sprite renderer
    private Vector3 startingPosition; // Original position for wandering reference
    
    // Add collision cooldown timer
    private float collisionCooldownTimer = 0f;
    private const float COLLISION_COOLDOWN = 1.0f; // 1 second cooldown after scene load
    
    // Path recalculation
    private float pathUpdateTimer = 0f;
    private bool stuckDetected = false;
    private Vector2 lastPosition;
    private float stuckCheckTimer = 0f;
    private float stuckCheckInterval = 0.5f;
    private float stuckThreshold = 0.1f;
    
    // Wandering state
    private enum EnemyState { Chasing, Wandering, Paused }
    private EnemyState currentState = EnemyState.Paused;
    private float wanderTimer = 0f;
    private Vector3 wanderTarget;
    private GameObject wanderTargetObject;
    
    // Track our manual pause state
    private bool isPausedByDialogue = false;
    private EnemyState stateBeforePause;
    
    private void Start()
    {
        // Store original scale and position
        originalScale = transform.localScale;
        startingPosition = transform.position;
        lastPosition = transform.position;
        
        // Start with a collision cooldown to prevent immediate combat after scene load
        collisionCooldownTimer = COLLISION_COOLDOWN;
        
        // Get sprite renderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Get components
        aiPath = GetComponent<AIPath>();
        aiDestinationSetter = GetComponent<AIDestinationSetter>();
        seeker = GetComponent<Seeker>();
        animator = GetComponent<Animator>();
        
        // Make sure this enemy has an identifier
        EnsureEnemyIdentifier();
        
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
        
        if (seeker == null)
        {
            Debug.LogError("Seeker component missing from " + gameObject.name + ". Add it in the inspector.");
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
        
        // Configure AI path properties for better corner handling
        aiPath.endReachedDistance = stoppingDistance;
        aiPath.pickNextWaypointDist = cornerCutDistance;
        aiPath.slowdownDistance = stoppingDistance * 0.5f;
        aiPath.slowWhenNotFacingTarget = false; // Prevent slowing on corners
        
        // Reduce the agent radius if it's too large
        float currentRadius = aiPath.radius;
        if (currentRadius > 0.3f)
        {
            aiPath.radius = 0.3f;
        }
        
        // Initially disable movement until player is in range
        aiPath.canMove = false;
        
        // Configure for 2D
        if (aiPath != null)
        {
            // Disable rotation by A* path system, we'll handle it manually for 2D
            aiPath.enableRotation = false;
            aiPath.orientation = OrientationMode.YAxisForward;
        }
        
        // Create a wander target object
        wanderTargetObject = new GameObject(gameObject.name + " Wander Target");
        
        // Set initial state to wandering
        ChangeState(EnemyState.Paused);
        // Start wandering immediately
        SetNewWanderTarget();
    }
    
    /// <summary>
    /// Ensures the enemy has an EnemyIdentifier component
    /// </summary>
    private void EnsureEnemyIdentifier()
    {
        // Check if this enemy already has an identifier
        if (GetComponent<EnemyIdentifier>() == null)
        {
            // Add the EnemyIdentifier component if it doesn't exist
            gameObject.AddComponent<EnemyIdentifier>();
            Debug.Log($"Added EnemyIdentifier to {gameObject.name}");
        }
    }
    
    private void OnEnable()
    {
        // Subscribe to dialogue events
        DialogueManager.OnDialogueStateChanged += OnDialogueStateChanged;
    }
    
    private void OnDisable()
    {
        // Unsubscribe from dialogue events
        DialogueManager.OnDialogueStateChanged -= OnDialogueStateChanged;
    }
    
    private void OnDialogueStateChanged(bool isDialogueActive)
    {
        if (isDialogueActive)
        {
            // Dialogue started - pause the enemy
            isPausedByDialogue = true;
            
            // Remember our current state to restore it later
            stateBeforePause = currentState;
            
            // Force AI path to stop moving
            if (aiPath != null)
            {
                aiPath.canMove = false;
            }
            
            // Set current state to paused
            ChangeState(EnemyState.Paused);
        }
        else
        {
            // Dialogue ended - unpause the enemy
            isPausedByDialogue = false;
            
            // Only restore AI path movement if we're not otherwise paused
            if (aiPath != null)
            {
                aiPath.canMove = (stateBeforePause == EnemyState.Chasing || stateBeforePause == EnemyState.Wandering);
            }
            
            // Restore previous state
            ChangeState(stateBeforePause);
        }
    }
    
    private void Update()
    {
        if (target == null) return;
        
        // Skip all update logic if paused by dialogue
        if (isPausedByDialogue)
        {
            return;
        }
        
        // Update collision cooldown timer if active
        if (collisionCooldownTimer > 0)
        {
            collisionCooldownTimer -= Time.deltaTime;
        }
        
        // Check line of sight to player
        CheckLineOfSight();
        
        // Handle state behavior
        switch (currentState)
        {
            case EnemyState.Chasing:
                HandleChasing();
                break;
            case EnemyState.Wandering:
                HandleWandering();
                break;
            case EnemyState.Paused:
                HandlePaused();
                break;
        }
        
        // Update animation based on movement
        UpdateAnimation();
        
        // Handle sprite flipping for 2D games
        UpdateFacing();
        
        // Periodically update path to prevent getting stuck
        pathUpdateTimer += Time.deltaTime;
        if (aiPath.canMove && pathUpdateTimer >= pathResetTime)
        {
            pathUpdateTimer = 0f;
            seeker.StartPath(transform.position, aiDestinationSetter.target.position, OnPathComplete);
        }
        
        // Check if the enemy is stuck
        CheckIfStuck();
    }
    
    private void CheckIfStuck()
    {
        // Only check if we're supposed to be moving
        if (!aiPath.canMove) return;
        
        stuckCheckTimer += Time.deltaTime;
        
        if (stuckCheckTimer >= stuckCheckInterval)
        {
            stuckCheckTimer = 0f;
            
            // Check if we've moved significantly
            float distanceMoved = Vector2.Distance(lastPosition, (Vector2)transform.position);
            
            if (distanceMoved < stuckThreshold && aiPath.velocity.magnitude < 0.1f)
            {
                if (!stuckDetected)
                {
                    stuckDetected = true;
                    Debug.Log("Enemy stuck detected - attempting to unstick");
                    StartCoroutine(UnstickEnemy());
                }
            }
            else
            {
                stuckDetected = false;
            }
            
            // Update last position for next check
            lastPosition = transform.position;
        }
    }
    
    private IEnumerator UnstickEnemy()
    {
        // Temporarily disable path following
        bool originalCanMove = aiPath.canMove;
        aiPath.canMove = false;
        
        // Try to move in a random direction to escape the stuck position
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        
        if (rb != null)
        {
            rb.velocity = randomDir * aiPath.maxSpeed;
            
            // Wait a short moment
            yield return new WaitForSeconds(0.2f);
            
            // Stop and reset
            rb.velocity = Vector2.zero;
        }
        else
        {
            // No rigidbody, try to move directly
            transform.position += new Vector3(randomDir.x, randomDir.y, 0) * 0.3f;
            
            // Wait a short moment
            yield return new WaitForSeconds(0.1f);
        }
        
        // Recalculate path
        if (seeker != null && aiDestinationSetter.target != null)
        {
            seeker.StartPath(transform.position, aiDestinationSetter.target.position, OnPathComplete);
        }
        
        // Re-enable movement if it was enabled before
        aiPath.canMove = originalCanMove;
        stuckDetected = false;
    }
    
    private void CheckLineOfSight()
    {
        if (target == null) return;
        
        float distanceToTarget = Vector2.Distance(transform.position, target.position);
        
        // Only check line of sight if within sight distance
        if (distanceToTarget <= sightDistance)
        {
            // Cast a ray to check if there are obstacles between enemy and player
            Vector2 directionToTarget = (target.position - transform.position).normalized;
            RaycastHit2D hit = Physics2D.Raycast(
                transform.position, 
                directionToTarget, 
                distanceToTarget,  // Only cast as far as the player, not the full sight distance
                obstacleLayer
            );
            
            // Debug ray to visualize the sight line
            Debug.DrawRay(transform.position, directionToTarget * distanceToTarget, 
                Color.red, 0.1f, false);
            
            // We have line of sight only if the ray didn't hit anything (no obstacles in the way)
            if (hit.collider == null)
            {
                hasLineOfSight = true;
                lostSightTimer = 0f;
                
                // If not already chasing, start chasing
                if (currentState != EnemyState.Chasing)
                {
                    ChangeState(EnemyState.Chasing);
                }
            }
            else
            {
                // Debug what was hit
                Debug.DrawLine(transform.position, hit.point, Color.yellow, 0.1f, false);
                
                // Line of sight is blocked by an obstacle
                hasLineOfSight = false;
            }
        }
        else
        {
            // Player is too far away
            hasLineOfSight = false;
        }
        
        // If line of sight is lost, increment timer
        if (!hasLineOfSight && currentState == EnemyState.Chasing)
        {
            lostSightTimer += Time.deltaTime;
            
            // If player has been out of sight long enough, start wandering
            if (lostSightTimer >= timeToLoseSight)
            {
                ChangeState(EnemyState.Paused);
                SetNewWanderTarget();
            }
        }
    }
    
    private void HandleChasing()
    {
        if (target == null) return;
        
        // Make sure target is set for pathfinding
        aiDestinationSetter.target = target;
        
        float distanceToTarget = Vector2.Distance(transform.position, target.position);
        
        // Stop at stopping distance
        if (distanceToTarget <= stoppingDistance)
        {
            aiPath.canMove = false;
            // Optional: Attack behavior when in range
        }
        else
        {
            aiPath.canMove = true;
        }
    }
    
    private void HandleWandering()
    {
        // Check if we've reached the wander target or timed out
        wanderTimer += Time.deltaTime;
        
        if (wanderTimer >= wanderPauseDuration || 
            Vector2.Distance(transform.position, wanderTarget) <= stoppingDistance)
        {
            // Pause before setting new target
            ChangeState(EnemyState.Paused);
            wanderTimer = 0f;
        }
    }
    
    private void HandlePaused()
    {
        // When paused, ensure we're not moving
        aiPath.canMove = false;
        
        // Increment pause timer
        wanderTimer += Time.deltaTime;
        
        // After pause duration, go back to wandering with a new target
        if (wanderTimer >= wanderPauseDuration)
        {
            wanderTimer = 0f;
            SetNewWanderTarget();
            ChangeState(EnemyState.Wandering);
        }
    }
    
    private void SetNewWanderTarget()
    {
        // Generate random point within wander radius of starting position
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        float randomDistance = Random.Range(1f, wanderRadius);
        
        wanderTarget = startingPosition + new Vector3(randomDirection.x, randomDirection.y, 0) * randomDistance;
        
        // Update wander target object position
        wanderTargetObject.transform.position = wanderTarget;
        
        // Set the wander target as the destination for A* pathfinding
        aiDestinationSetter.target = wanderTargetObject.transform;
        
        // Allow movement again
        aiPath.canMove = true;
        
        // Force immediate path recalculation
        if (seeker != null)
        {
            seeker.StartPath(transform.position, wanderTarget, OnPathComplete);
        }
    }
    
    private void ChangeState(EnemyState newState)
    {
        // Don't allow state changes if paused by dialogue, unless we're being paused
        if (isPausedByDialogue && newState != EnemyState.Paused)
        {
            return;
        }
        
        // Exit previous state
        switch (currentState)
        {
            case EnemyState.Chasing:
                // Nothing special to do when exiting chase state
                break;
            case EnemyState.Wandering:
            case EnemyState.Paused:
                // Nothing special to do when exiting these states
                break;
        }
        
        // Enter new state
        currentState = newState;
        
        switch (newState)
        {
            case EnemyState.Chasing:
                Debug.Log("Enemy has spotted player, beginning chase");
                if (animator != null) animator.SetTrigger("Chase");
                break;
            case EnemyState.Wandering:
                Debug.Log("Enemy is wandering");
                if (animator != null) animator.SetTrigger("Wander");
                break;
            case EnemyState.Paused:
                aiPath.canMove = false;
                Debug.Log("Enemy is paused");
                if (animator != null) animator.SetTrigger("Pause");
                break;
        }
        
        // Reset timer when changing states
        wanderTimer = 0f;
    }
    
    private void UpdateFacing()
    {
        // Determine which way to face
        Vector2 direction;
        
        if (currentState == EnemyState.Chasing && target != null)
        {
            // Face toward target when chasing
            direction = target.position - transform.position;
        }
        else if (currentState == EnemyState.Wandering && aiPath.velocity.magnitude > 0.1f)
        {
            // Face in movement direction when wandering
            direction = aiPath.velocity;
        }
        else
        {
            // Keep current direction when paused
            return;
        }
        
        // Only update if we have a valid direction and sprite renderer
        if (direction != Vector2.zero && spriteRenderer != null && faceTarget)
        {
            // Use absolute scale value to maintain size while flipping
            float xScale = Mathf.Abs(originalScale.x);
            
            // Flip the sprite based on direction
            if (direction.x > 0)
            {
                // Face right
                transform.localScale = new Vector3(xScale, originalScale.y, originalScale.z);
            }
            else if (direction.x < 0)
            {
                // Face left
                transform.localScale = new Vector3(-xScale, originalScale.y, originalScale.z);
            }
        }
    }
    
    private void UpdateAnimation()
    {
        if (animator == null) return;
        
        // Set animation parameters based on state and movement
        if (aiPath.velocity.magnitude > 0.1f)
        {
            // Play walking/running animation
            animator.SetBool("IsMoving", true);
        }
        else
        {
            // Play idle animation
            animator.SetBool("IsMoving", false);
        }
        
        // Optionally set additional parameters based on state
        animator.SetInteger("State", (int)currentState);
    }
    
    // Add collision handling to prevent thinning and disappearing
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Make sure we have an EnemyIdentifier component
            EnemyIdentifier identifier = GetComponent<EnemyIdentifier>();
            if (identifier == null)
            {
                Debug.LogError("Enemy missing EnemyIdentifier component!");
                return;
            }
            
            // Check if this enemy has already been defeated
            if (PersistentGameManager.Instance != null && 
                PersistentGameManager.Instance.IsEnemyDefeated(identifier.GetEnemyId()))
            {
                Debug.Log("Enemy has already been defeated, ignoring collision");
                return;
            }
            
            // Check if we're still in the cooldown period after scene load
            if (collisionCooldownTimer > 0)
            {
                Debug.Log($"Ignoring collision with player during cooldown period ({collisionCooldownTimer:F1}s remaining)");
                return;
            }
            
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
            
            // Start combat when the enemy collides with the player
            // Use the EnsureExists method to get or create the SceneTransitionManager
            SceneTransitionManager.EnsureExists().StartCombat(gameObject, collision.gameObject);
        }
        else if (currentState == EnemyState.Wandering)
        {
            // If we hit something while wandering, set a new wander target
            SetNewWanderTarget();
        }
    }
    
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Resume movement if we were in chase mode
            if (currentState == EnemyState.Chasing && hasLineOfSight)
            {
                aiPath.canMove = true;
            }
        }
    }
    
    // Visualize detection ranges in the editor
    private void OnDrawGizmosSelected()
    {
        // Draw sight radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightDistance);
        
        // Draw stopping radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stoppingDistance);
        
        // Draw wander radius if in play mode
        if (Application.isPlaying)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(startingPosition, wanderRadius);
            
            // Draw current wander target if wandering
            if (currentState == EnemyState.Wandering)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(wanderTarget, 0.2f);
                Gizmos.DrawLine(transform.position, wanderTarget);
            }
        }
    }
    
    // Clean up the wander target object when destroyed
    private void OnDestroy()
    {
        if (wanderTargetObject != null)
        {
            Destroy(wanderTargetObject);
        }
    }
    
    // Add a callback method for path completion
    private void OnPathComplete(Path p)
    {
        // Path completed - you can add logic here if needed
        if (p.error)
        {
            Debug.LogWarning($"Path error: {p.errorLog}");
        }
    }
} 