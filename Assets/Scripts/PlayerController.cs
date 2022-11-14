using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/**
 * Basic controller for handling player movement.
 */
public class PlayerController : MonoBehaviour {

    [SerializeField] private LayerMask ground;

    protected PlayerControls controls;
    protected Rigidbody2D body;
    protected Collider2D collider;

    private void Awake() {
        this.controls = new PlayerControls();
        this.body = GetComponent<Rigidbody2D>();
        this.collider = GetComponent<Collider2D>();
    }

    private void OnEnable() {
        this.controls.Enable();
    }

    private void OnDisable() {
        this.controls.Disable();
    }

    void Start() {
        this.controls.Move.Jump.performed += _ => Jump();
        this.controls.Move.ShortJump.performed += _ => ShortJump();
    }

    void Update() {
        this.MoveHorizontal();
    }

    /**
     * Move the player horizontally.
     */
    void MoveHorizontal() {
        float horizontal = controls.Move.Move.ReadValue<float>();

        float updatedXVel = body.velocity.x + horizontal * CharMovementData.HORI_ACCEL * Time.deltaTime;
        if (updatedXVel > CharMovementData.MAX_HORI_SPEED) {
            updatedXVel = Mathf.Max(body.velocity.x, CharMovementData.MAX_HORI_SPEED);
        } else if (updatedXVel < -CharMovementData.MAX_HORI_SPEED) {
            updatedXVel = Mathf.Min(body.velocity.x, -CharMovementData.MAX_HORI_SPEED);
        }

        body.velocity = new Vector2(updatedXVel, body.velocity.y);
    }

    /**
     * Perform a grounded or aerial jump if possible.
     */
    void Jump() {
        Debug.Log(IsGrounded());
        if (this.IsGrounded()) {
            body.velocity = new Vector2(body.velocity.x, CharMovementData.JUMP_VEL);
        }
    }

    /**
     * Perform a grounded or aerial jump if possible.
     */
    void ShortJump() {
        Debug.Log(IsGrounded());
        if (this.IsGrounded()) {
            body.velocity = new Vector2(body.velocity.x, CharMovementData.SHORT_JUMP_VEL);
        }
    }

    /**
     * Get the top left collision box of this character with a small offset overlap.
     */
    private Vector2 GetTopLeftBound() {
        Vector2 topLeft = transform.position;
        topLeft.x -= collider.bounds.extents.x + CharMovementData.COLLIDER_OFFSET;
        topLeft.y += collider.bounds.extents.y + CharMovementData.COLLIDER_OFFSET;
        return topLeft;
    }

    /**
     * Get the bottom right collision box of this character with a small offset overlap.
     */
    private Vector2 GetBotRightBound() {
        Vector2 botRight = transform.position;
        botRight.x += collider.bounds.extents.x + CharMovementData.COLLIDER_OFFSET;
        botRight.y -= collider.bounds.extents.y + CharMovementData.COLLIDER_OFFSET;
        return botRight;
    }

    /**
     * Determine whether the player is grounded.
     */
    private bool IsGrounded() {
        Vector2 topLeft = this.GetTopLeftBound();
        Vector2 botRight = this.GetBotRightBound();

        return Physics2D.OverlapArea(topLeft, botRight, ground);
    }
}
