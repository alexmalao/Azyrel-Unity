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
        float look = controls.Move.Look.ReadValue<float>();

        float updatedXVel = body.velocity.x;
        float updatedYVel = body.velocity.y;

        if (this.IsGrounded()) {
            // grounded movement will only trigger when already in the direction of input
            float modifiedSpeed = body.velocity.x + horizontal * charData.xAccel * Time.deltaTime;

            if (horizontal == 1 && body.velocity.x > -0.001f) {
                updatedXVel = Mathf.Max(modifiedSpeed, charData.minXSpeed);
                if (updatedXVel > charData.maxXSpeed) {
                    updatedXVel = Mathf.Max(body.velocity.x, charData.maxXSpeed);
                }
            } else if (horizontal == -1 && body.velocity.x < 0.001f) {
                updatedXVel = Mathf.Min(modifiedSpeed, -charData.minXSpeed);
                if (updatedXVel < -charData.maxXSpeed) {
                    updatedXVel = Mathf.Min(body.velocity.x, -charData.maxXSpeed);
                }
            } else {
                // stop the character
                if (Mathf.Abs(updatedXVel) < charData.stopSpeed &&
                    Mathf.Abs(updatedYVel) < charData.stopSpeed) {
                    updatedXVel = 0.0f;
                    updatedYVel = 0.0f;
                } else {
                    updatedXVel *= Mathf.Pow(charData.traction, Time.deltaTime);
                    updatedYVel *= Mathf.Pow(charData.traction, Time.deltaTime);
                }
            }
        } else if (this.IsAirborne()) {
            float modifiedSpeed = body.velocity.x + horizontal * charData.airXAccel * Time.deltaTime;
            if (modifiedSpeed > charData.maxAirXSpeed) {
                updatedXVel = Mathf.Min(modifiedSpeed, body.velocity.x);
                updatedXVel = Mathf.Max(updatedXVel, charData.maxAirXSpeed);
            } else if (modifiedSpeed < -charData.maxAirXSpeed) {
                updatedXVel = Mathf.Max(modifiedSpeed, body.velocity.x);
                updatedXVel = Mathf.Min(updatedXVel, -charData.maxAirXSpeed);
            } else {
                updatedXVel = modifiedSpeed;
            }
        }
        
        // apply the new velocity
        body.velocity = new Vector2(updatedXVel, updatedYVel);
    }

    /**
     * Update whether to reset jumps.
     */
    void GroundUpdate() {
        if (this.IsGrounded()) {
            body.gravityScale = 1.0f;
            this.activeMoveData.UpdateDirection(body.velocity.x);
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
        if (this.IsAirborne() && this.activeMoveData.AttemptJump()) {
            float horizontal = controls.Move.Move.ReadValue<float>();

            // perform jump, apply minimum jump speed
            float newXVal = body.velocity.x;
            if (horizontal == 1) {
                newXVal = Mathf.Max(body.velocity.x, this.charData.minAirJumpXSpeed);
            } else if (horizontal == -1) {
                newXVal = Mathf.Min(body.velocity.x, -this.charData.minAirJumpXSpeed);
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
        } else if (this.IsAirborne()) {
            // aerial dash
            float look = controls.Move.Look.ReadValue<float>();
            if (look == -1 && body.velocity.y <= 0) {

                this.body.gravityScale = 1.0f;
                float newYVal = Mathf.Min(body.velocity.y, this.charData.airDownDashVel);
                body.velocity = new Vector2(body.velocity.x, newYVal);
                this.activeMoveData.UpdateDirection(body.velocity.x);
            }

            if (look != -1 && this.activeMoveData.AttemptJump()) {
                float newXVal = 0;
                if (right) {
                    newXVal = Mathf.Max(body.velocity.x, this.charData.maxXSpeed);
                } else {
                    newXVal = Mathf.Min(body.velocity.x, -this.charData.maxXSpeed);
                }
                body.velocity = new Vector2(newXVal, 0.0f);
                StartCoroutine(SuspendDashGravity(charData.dashFloatDur));
                this.activeMoveData.UpdateDirection(body.velocity.x);
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
        Debug.Log("performed grounded jump");
        if (this.IsGrounded() || (this.IsAirborne() && this.activeMoveData.edgeJump)) {

            jumpVel = Mathf.Max(jumpVel, body.velocity.y);
            body.velocity = new Vector2(body.velocity.x, jumpVel);
            Debug.Log(body.velocity.y);
        }
        this.activeMoveData.edgeJump = false;
    }

    /**
     * Get the top left collision box of this character with a small offset overlap.
     */
    private Vector2 GetGroundedTopLeft() {
        Vector2 topLeft = transform.position;
        topLeft += transform.lossyScale * collider.offset;
        topLeft.x -= collider.bounds.extents.x - charData.colliderOffset;
        topLeft.y += collider.bounds.extents.y + charData.colliderOffset;
        return topLeft;
    }

    /**
     * Get the bottom right collision box of this character with a small offset overlap.
     */
    private Vector2 GetGroundedBotRight() {
        Vector2 botRight = transform.position;
        botRight += transform.lossyScale * collider.offset;
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
     * Determine whether the player is airborne.
     */
    private bool IsAirborne() {
        // TODO: ensure there is no ceiling or wallhang
        return !this.IsGrounded();
    }

    /**
     * Suspends gravity for a set amount of time.
     */
    private IEnumerator SuspendDashGravity(float time) {
        body.gravityScale = 0.0f;
        yield return new WaitForSeconds(time);
        body.gravityScale = 1.0f;
    }
}
