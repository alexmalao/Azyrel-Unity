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
    protected ActiveMovementData activeMoveData;

    protected Rigidbody2D body;
    protected Collider2D collider;

    //////////////////////
    /// Setup Methods. ///
    //////////////////////

    private void Awake() {
        this.controls = new PlayerControls();
        this.charData = new CharMovementData();
        this.activeMoveData = new ActiveMovementData();

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
        this.controls.Move.AirJump.performed += _ => AirJump();
        this.controls.Move.Dash.performed += _ => Dash();
    }

    void Update() {
        this.MoveHorizontal();
        this.GroundUpdate();
    }

    //////////////////////////////////
    /// Methods for frame updates. ///
    //////////////////////////////////

    /**
     * Move the player horizontally.
     */
    void MoveHorizontal() {
        float horizontal = controls.Move.Move.ReadValue<float>();
        this.activeMoveData.UpdateDirection(horizontal);

        float accel = this.IsGrounded() ? charData.hori_accel : charData.air_hori_accel;
        float updatedXVel = body.velocity.x + horizontal * accel * Time.deltaTime;
        if (updatedXVel > charData.max_hori_speed) {
            updatedXVel = Mathf.Max(body.velocity.x, charData.max_hori_speed);
        } else if (updatedXVel < -charData.max_hori_speed) {
            updatedXVel = Mathf.Min(body.velocity.x, -charData.max_hori_speed);
        }

        body.velocity = new Vector2(updatedXVel, body.velocity.y);
    }

    /**
     * Update whether to reset jumps.
     */
    void GroundUpdate() {
        body.gravityScale = 1.0f;
        this.activeMoveData.UpdateDirection(body.velocity.x);
        if (this.IsGrounded()) {
            this.activeMoveData.ResetJumps();
        }
    }

    ////////////////////////////////////////
    /// Methods for mapped button slots. ///
    ////////////////////////////////////////

    /**
     * Perform a grounded or aerial jump if possible.
     */
    void Jump() {
        if (this.IsGrounded()) {
            body.velocity = new Vector2(body.velocity.x, charData.jump_vel);
        }
    }

    /**
     * Perform a grounded or aerial jump if possible.
     */
    void ShortJump() {
        if (this.IsGrounded()) {
            body.velocity = new Vector2(body.velocity.x, charData.short_jump_vel);
        }
    }

    /**
     * Perform an aerial jump if possible.
     */
    void AirJump() {
        if (!this.IsGrounded() && this.activeMoveData.AttemptJump()) {
            float horizontal = controls.Move.Move.ReadValue<float>();

            // perform jump, check for alternating momentum
            if (body.velocity.x * horizontal < 0) {
                float newXVal = Mathf.Abs(body.velocity.x) * horizontal * this.charData.boface_modifier;
                body.velocity = new Vector2(newXVal, charData.air_jump_vel);
            } else {
                body.velocity = new Vector2(body.velocity.x, charData.air_jump_vel);
            }
        }
    }

    /**
     *  Perform a grounded or aerial dash if possible.
     */
    void Dash() {
        if (this.IsGrounded()) {
            float horizontal = controls.Move.Move.ReadValue<float>();
            bool right = true;
            if (horizontal == 0) {
                // no input direction default to facing direction
                right = this.activeMoveData.facingRight;
            } else {
                // input direction override
                right = horizontal > 0;
            }

            float newXVal = 0;
            if (right) {
                newXVal = Mathf.Max(body.velocity.x, this.charData.max_hori_speed);
            } else {
                newXVal = Mathf.Min(body.velocity.x, -this.charData.max_hori_speed);
            }
            body.velocity = new Vector2(newXVal, body.velocity.y);
        }
    }

    ///////////////////////
    /// Utilty Methods. ///
    ///////////////////////

    /**
     * Get the top left collision box of this character with a small offset overlap.
     */
    private Vector2 GetGroundedTopLeft() {
        Vector2 topLeft = transform.position;
        topLeft.x -= collider.bounds.extents.x - charData.collider_offset;
        topLeft.y += collider.bounds.extents.y + charData.collider_offset;
        return topLeft;
    }

    /**
     * Get the bottom right collision box of this character with a small offset overlap.
     */
    private Vector2 GetGroundedBotRight() {
        Vector2 botRight = transform.position;
        botRight.x += collider.bounds.extents.x - charData.collider_offset;
        botRight.y -= collider.bounds.extents.y + charData.collider_offset;
        return botRight;
    }

    /**
     * Determine whether the player is grounded.
     */
    private bool IsGrounded() {
        Vector2 topLeft = this.GetGroundedTopLeft();
        Vector2 botRight = this.GetGroundedBotRight();

        return Physics2D.OverlapArea(topLeft, botRight, ground);
    }
}
