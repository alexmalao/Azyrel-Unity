using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/**
 * Basic controller for handling player movement.
 */
public class PlayerController : MonoBehaviour {

    [SerializeField] private LayerMask ground;

    protected PlayerControls controls;
    protected CharMovementData charData;

    protected Rigidbody2D body;
    protected Collider2D collider;

    private void Awake() {
        this.controls = new PlayerControls();
        this.charData = new CharMovementData();
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

        float updatedXVel = body.velocity.x + horizontal * charData.hori_accel * Time.deltaTime;
        if (updatedXVel > charData.max_hori_speed) {
            updatedXVel = Mathf.Max(body.velocity.x, charData.max_hori_speed);
        } else if (updatedXVel < -charData.max_hori_speed) {
            updatedXVel = Mathf.Min(body.velocity.x, -charData.max_hori_speed);
        }

        body.velocity = new Vector2(updatedXVel, body.velocity.y);
    }

    /**
     * Perform a grounded or aerial jump if possible.
     */
    void Jump() {
        Debug.Log(IsGrounded());
        if (this.IsGrounded()) {
            body.velocity = new Vector2(body.velocity.x, charData.jump_vel);
        }
    }

    /**
     * Perform a grounded or aerial jump if possible.
     */
    void ShortJump() {
        Debug.Log(IsGrounded());
        if (this.IsGrounded()) {
            body.velocity = new Vector2(body.velocity.x, charData.short_jump_vel);
        }
    }

    /**
     * Get the top left collision box of this character with a small offset overlap.
     */
    private Vector2 GetTopLeftBound() {
        Vector2 topLeft = transform.position;
        topLeft.x -= collider.bounds.extents.x - charData.collider_offset;
        topLeft.y += collider.bounds.extents.y + charData.collider_offset;
        return topLeft;
    }

    /**
     * Get the bottom right collision box of this character with a small offset overlap.
     */
    private Vector2 GetBotRightBound() {
        Vector2 botRight = transform.position;
        botRight.x += collider.bounds.extents.x - charData.collider_offset;
        botRight.y -= collider.bounds.extents.y + charData.collider_offset;
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
