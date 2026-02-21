using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

/// <summary>
/// Main player script for controlling player movement and jump in a networked environment.
/// Requires Rigidbody component and uses Unity Netcode for GameObjects.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class MainPlayerScript : NetworkBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Speed of player movement")]
    public float speed = 5f;
    
    [Tooltip("Speed of player rotation")]
    public float rotationSpeed = 10f;
    
    [Tooltip("Force applied when jumping")]
    public float jumpForce = 5f;
    
    [Header("Ground Check Settings")]
    [Tooltip("Transform position for ground check raycast")]
    public Transform groundCheck;
    
    [Tooltip("Radius of ground check sphere")]
    public float groundCheckRadius = 0.2f;
    
    [Tooltip("Layer mask for ground detection")]
    public LayerMask groundLayer;
    
    [Tooltip("Distance to check for ground when groundCheck is not assigned")]
    public float groundCheckDistance = 1.1f;
    
    // Private variables
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private Vector2 moveInput;
    private bool jumpInput;
    private bool isGrounded;
    
    /// <summary>
    /// Cache components on awake
    /// </summary>
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
    }
    
    /// <summary>
    /// Warn if ground layer is not configured
    /// </summary>
    private void Start()
    {
        if (groundLayer.value == 0)
        {
            Debug.LogWarning("Ground Layer is not set on " + gameObject.name + "! Player jump will not work correctly.", this);
        }
    }
    
    /// <summary>
    /// Called when the network object is spawned
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Disable physics for non-owner clients to prevent prediction issues
        if (!IsOwner)
        {
            rb.isKinematic = true;
        }
    }
    
    /// <summary>
    /// Handle physics-based movement in FixedUpdate
    /// </summary>
    private void FixedUpdate()
    {
        // Only allow control if this is the owner
        if (!IsOwner) return;
        
        // Check if player is on the ground
        CheckGround();
        
        // Handle movement
        HandleMovement();
        
        // Handle jump (reset jump input after processing)
        if (jumpInput)
        {
            HandleJump();
            jumpInput = false;
        }
    }
    
    /// <summary>
    /// Check if the player is grounded using sphere check at feet position
    /// </summary>
    private void CheckGround()
    {
        // Calculate the position at the player's feet
        Vector3 feetPosition;
        float checkRadius;
        
        if (groundCheck != null)
        {
            feetPosition = groundCheck.position;
            checkRadius = groundCheckRadius;
        }
        else if (capsuleCollider != null)
        {
            // Use capsule collider to determine feet position
            float feetOffset = capsuleCollider.center.y - capsuleCollider.height / 2f + capsuleCollider.radius;
            feetPosition = transform.position + Vector3.down * (-feetOffset + 0.05f);
            checkRadius = capsuleCollider.radius * 0.9f;
        }
        else
        {
            // Fallback: use raycast from player position
            feetPosition = transform.position + Vector3.down * 0.1f;
            checkRadius = groundCheckRadius;
        }
        
        // Check for ground using sphere check (more reliable than raycast for uneven terrain)
        isGrounded = Physics.CheckSphere(feetPosition, checkRadius, groundLayer, QueryTriggerInteraction.Ignore);
    }
    
    /// <summary>
    /// Handle player movement using Rigidbody.MovePosition and MoveRotation
    /// </summary>
    private void HandleMovement()
    {
        // Convert Vector2 input to Vector3 movement direction
        Vector3 movement = new Vector3(moveInput.x, 0f, moveInput.y);
        
        // Only process if there is meaningful movement input (using sqrMagnitude for robustness)
        if (movement.sqrMagnitude > 0.001f)
        {
            // Calculate new position
            Vector3 newPosition = rb.position + movement * speed * Time.fixedDeltaTime;
            
            // Move to new position using Rigidbody
            rb.MovePosition(newPosition);
            
            // Calculate target rotation based on movement direction (normalized for safety)
            Quaternion targetRotation = Quaternion.LookRotation(movement.normalized, Vector3.up);
            
            // Smoothly rotate towards movement direction
            Quaternion newRotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newRotation);
        }
    }
    
    /// <summary>
    /// Handle player jump using Rigidbody.AddForce
    /// </summary>
    private void HandleJump()
    {
        // Only jump if grounded to prevent air jumping
        if (isGrounded)
        {
            // Apply upward force for jump
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }
    
    /// <summary>
    /// Called by Player Input (Invoke Unity Events) when Move action is performed
    /// </summary>
    /// <param name="context">Input action callback context containing Vector2 input from WASD or Arrow keys</param>
    public void OnMove(InputAction.CallbackContext context)
    {
        // Only process input if this is the owner
        if (!IsOwner) return;
        
        moveInput = context.ReadValue<Vector2>();
    }
    
    /// <summary>
    /// Called by Player Input (Invoke Unity Events) when Jump action is performed
    /// </summary>
    /// <param name="context">Input action callback context for jump button (Spacebar)</param>
    public void OnJump(InputAction.CallbackContext context)
    {
        // Only process input if this is the owner
        if (!IsOwner) return;
        
        if (context.performed)
        {
            jumpInput = true;
        }
    }
    
    /// <summary>
    /// Draw gizmos for ground check visualization
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Determine feet position and check radius for gizmo
        Vector3 feetPosition;
        float checkRadius;
        
        if (groundCheck != null)
        {
            feetPosition = groundCheck.position;
            checkRadius = groundCheckRadius;
        }
        else if (capsuleCollider != null)
        {
            float feetOffset = capsuleCollider.center.y - capsuleCollider.height / 2f + capsuleCollider.radius;
            feetPosition = transform.position + Vector3.down * (-feetOffset + 0.05f);
            checkRadius = capsuleCollider.radius * 0.9f;
        }
        else
        {
            feetPosition = transform.position + Vector3.down * 0.1f;
            checkRadius = groundCheckRadius;
        }
        
        // Draw ground check sphere (green if grounded, red if not)
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(feetPosition, checkRadius);
    }
}
