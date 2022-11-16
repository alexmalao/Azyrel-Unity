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

        float accel = this.IsGrounded() ? charData.xAccel : charData.airXAccel;
        float maxSpeed = this.IsGrounded() ? charData.maxXSpeed : charData.maxAirXSpeed;
        float updatedXVel = body.velocity.x + horizontal * accel * Time.deltaTime;
        if (updatedXVel > maxSpeed) {
            updatedXVel = Mathf.Max(body.velocity.x, maxSpeed);
        } else if (updatedXVel < -maxSpeed) {
            updatedXVel = Mathf.Min(body.velocity.x, -maxSpeed);
        }

        body.velocity = new Vector2(updatedXVel, body.velocity.y);
    }

    /**
     * Update whether to reset jumps.
     */
    void GroundUpdate() {
        this.activeMoveData.UpdateDirection(body.velocity.x);
        if (this.IsGrounded()) {
            body.gravityScale = 1.0f;
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
        GroundJump(this.charData.jumpVel);
    }

    /**
     * Perform a grounded or aerial jump if possible.
     */
    void ShortJump() {
        GroundJump(this.charData.shortJumpVel);
    }

    /**
     * Perform an aerial jump if possible.
     */
    void AirJump() {
        if (!this.IsGrounded() && this.activeMoveData.AttemptJump()) {
            float horizontal = controls.Move.Move.ReadValue<float>();

            // perform jump, apply minimum jump speed
            float newXVal = 0.0f;
            if (horizontal == 1) {
                newXVal = Mathf.Max(body.velocity.x, this.charData.minAirXJumpSpeed);
            } else if (horizontal == -1) {
                newXVal = Mathf.Min(body.velocity.x, -this.charData.minAirXJumpSpeed);
            }
             body.velocity = new Vector2(newXVal, charData.airJumpVel);
        } else if (this.IsGrounded()) {
            this.activeMoveData.edgeJump = true;
        }
    }

    /**
     *  Perform a grounded or aerial dash if possible.
     */
    void Dash() {
        float horizontal = controls.Move.Move.ReadValue<float>();
        bool right = true;
        if (horizontal == 0) {
            // no input direction default to facing direction
            right = this.activeMoveData.facingRight;
        } else {
            right = horizontal > 0;
        }

        if (this.IsGrounded()) {
            // ground dash
            float newXVal = 0;
            if (right) {
                newXVal = Mathf.Max(body.velocity.x, this.charData.maxXSpeed);
            } else {
                newXVal = Mathf.Min(body.velocity.x, -this.charData.maxXSpeed);
            }
            body.velocity = new Vector2(newXVal, body.velocity.y);
        } else if (!this.IsGrounded()) {
            // aerial dash
            float look = controls.Move.Look.ReadValue<float>();
            if (look == -1 && body.velocity.y <= 0) {

                this.body.gravityScale = 1.0f;
                float newYVal = Mathf.Min(body.velocity.y, this.charData.airDownDashVel);
                body.velocity = new Vector2(body.velocity.x, newYVal);
            }

            if (look != -1 && this.activeMoveData.AttemptJump()) {
                float newXVal = 0;
                if (right) {
                    newXVal = Mathf.Max(body.velocity.x, this.charData.maxXSpeed);
                } else {
                    newXVal = Mathf.Min(body.velocity.x, -this.charData.maxXSpeed);
                }
                body.velocity = new Vector2(newXVal, 0.0f);
                StartCoroutine(SuspendGravity(charData.dashFloatDur));
            }
        }
    }

    ///////////////////////
    /// Utilty Methods. ///
    ///////////////////////

    /**
     * Method for performing any grounded jump.
     */
    private void GroundJump(float jumpVel) {
        if (this.IsGrounded() || (!this.IsGrounded() && this.activeMoveData.edgeJump)) {
            body.velocity = new Vector2(body.velocity.x, jumpVel);
        }
        this.activeMoveData.edgeJump = false;
    }

    /**
     * Get the top left collision box of this character with a small offset overlap.
     */
    private Vector2 GetGroundedTopLeft() {
        Vector2 topLeft = transform.position;
        topLeft.x -= collider.bounds.extents.x - charData.colliderOffset;
        topLeft.y += collider.bounds.extents.y + charData.colliderOffset;
        return topLeft;
    }

    /**
     * Get the bottom right collision box of this character with a small offset overlap.
     */
    private Vector2 GetGroundedBotRight() {
        Vector2 botRight = transform.position;
        botRight.x += collider.bounds.extents.x - charData.colliderOffset;
        botRight.y -= collider.bounds.extents.y + charData.colliderOffset;
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

    /**
     * Suspends gravity for a set amount of time.
     */
    private IEnumerator SuspendGravity(float time) {
        body.gravityScale = 0.0f;
        yield return new WaitForSeconds(time);
        body.gravityScale = 0.5f;
        yield return new WaitForSeconds(time / 4);
        body.gravityScale = 1.0f;
    }
}
