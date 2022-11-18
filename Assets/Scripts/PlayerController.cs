using System;
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
        this.PhysicsUpdate();
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

        Vector2 newVelocity;

        if (this.IsGrounded() || this.activeMoveData.lastFrameGrounded) {
            newVelocity = this.MoveGrounded(horizontal);
        } else {
            newVelocity = this.MoveAirborne(horizontal);
        }

        // apply the new velocity
        body.velocity = newVelocity;
    }

    /**
     * Update whether to reset jumps.
     */
    void PhysicsUpdate() {
        bool isGrounded = this.IsGrounded();
        if (isGrounded) {
            body.gravityScale = 0.0f;
            this.activeMoveData.UpdateDirection(body.velocity.x);
            this.activeMoveData.ResetJumps();
        } else if (!activeMoveData.suspendGravity) {
            body.gravityScale = 1.0f;
        }
        Debug.Log(isGrounded);
        this.activeMoveData.lastFrameGrounded = isGrounded;
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
            float newVelocity;
            float magnitude = body.velocity.magnitude;

            // executing land, revoking landing magnitude
            if (Math.Abs(body.velocity.y) > Mathf.Abs(body.velocity.x) + 0.01f) {
                magnitude = 0;
            }

            Vector2 slopeVector = this.GetSlopeVector();
            if (right) {
                newVelocity = Mathf.Max(magnitude, this.charData.maxGroundSpeed);
            } else {
                newVelocity = Mathf.Min(-magnitude, -this.charData.maxGroundSpeed);
            }
            body.velocity = new Vector2(newVelocity * slopeVector.x, newVelocity * slopeVector.y);
        } else if (this.IsAirborne() && !this.activeMoveData.lastFrameGrounded) {
            // aerial dash
            float look = controls.Move.Look.ReadValue<float>();
            if (look == -1 && body.velocity.y <= 0) {

                this.body.gravityScale = 1.0f;
                float newYVal = Mathf.Min(body.velocity.y, this.charData.airDownDashVel);
                body.velocity = new Vector2(body.velocity.x, newYVal);
                this.activeMoveData.UpdateDirection(body.velocity.x);
            }

            if (look != -1 && this.activeMoveData.AttemptJump()) {
                float newXVal;
                if (right) {
                    newXVal = Mathf.Max(body.velocity.x, this.charData.maxGroundSpeed);
                } else {
                    newXVal = Mathf.Min(body.velocity.x, -this.charData.maxGroundSpeed);
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
     * Get the velocity vector for grounded movement.
     */
    private Vector2 MoveGrounded(float horizontal) {

        float updatedXVel = body.velocity.x;
        float updatedYVel = body.velocity.y;

        // executing jump or land, steepest grounded state is 45 degrees
        if (Math.Abs(body.velocity.y) > Mathf.Abs(body.velocity.x) + 0.01f) {
            return body.velocity;
        }

        float relativeMagnitude = body.velocity.magnitude;
        // get magnitude respective to x direction
        if (body.velocity.x < 0) {
            relativeMagnitude *= -1;
        }

        Vector2 slopeVector;
        try {
            slopeVector = this.GetSlopeVector();
        }
        catch (InvalidOperationException) {
            return body.velocity;
        }
        float modifiedVelocity = relativeMagnitude + horizontal * charData.groundAccel * Time.deltaTime;
        float speedPenalty = charData.groundSpeedPenalty * Time.deltaTime;

        // grounded movement will only trigger when already in the direction of input
        if (horizontal == 1 && body.velocity.x > -0.001f) {
            modifiedVelocity = Mathf.Max(modifiedVelocity, charData.minGroundSpeed);
            if (modifiedVelocity > charData.maxGroundSpeed) {
                modifiedVelocity = Mathf.Max(relativeMagnitude - speedPenalty, charData.maxGroundSpeed);
            }
            return new Vector2(modifiedVelocity * slopeVector.x, modifiedVelocity * slopeVector.y);
        }
        else if (horizontal == -1 && body.velocity.x < 0.001f) {
            modifiedVelocity = Mathf.Min(modifiedVelocity, -charData.minGroundSpeed);
            if (modifiedVelocity < -charData.maxGroundSpeed) {
                modifiedVelocity = Mathf.Min(relativeMagnitude + speedPenalty, -charData.maxGroundSpeed);
            }
            return new Vector2(modifiedVelocity * slopeVector.x, modifiedVelocity * slopeVector.y);
        }
        else {
            // stop the character
            if (Mathf.Abs(body.velocity.x) < charData.stopSpeed &&
                Mathf.Abs(body.velocity.y) < charData.stopSpeed) {
                return new Vector2(0.0f, 0.0f);
            }
            else {
                return new Vector2(
                    body.velocity.x * Mathf.Pow(charData.traction, Time.deltaTime),
                    body.velocity.y * Mathf.Pow(charData.traction, Time.deltaTime));
            }
        }
    }

    /**
     * Get the velocity vector for airborne movement.
     */
    private Vector2 MoveAirborne(float horizontal) {
        float updatedXVel;

        // airborne movement does not apply any traction or slowdown
        float modifiedSpeed = body.velocity.x + horizontal * charData.airXAccel * Time.deltaTime;
        if (modifiedSpeed > charData.maxAirXSpeed) {
            updatedXVel = Mathf.Min(modifiedSpeed, body.velocity.x);
            updatedXVel = Mathf.Max(updatedXVel, charData.maxAirXSpeed);
        }
        else if (modifiedSpeed < -charData.maxAirXSpeed) {
            updatedXVel = Mathf.Max(modifiedSpeed, body.velocity.x);
            updatedXVel = Mathf.Min(updatedXVel, -charData.maxAirXSpeed);
        }
        else {
            updatedXVel = modifiedSpeed;
        }

        return new Vector2(updatedXVel, body.velocity.y);
    }

    /**
     * Method for performing any grounded jump.
     */
    private void GroundJump(float jumpVel) {
        bool isGrounded = this.IsGrounded();
        if (isGrounded || (!isGrounded && this.activeMoveData.edgeJump)) {

            jumpVel = Mathf.Max(jumpVel, body.velocity.y);
            body.velocity = new Vector2(body.velocity.x, jumpVel);
        }
        this.activeMoveData.edgeJump = false;
    }

    /**
     * Calculate the center of the collider.
     */
    private Vector2 GetColliderPos() {
        Vector2 position = new Vector2(transform.position.x, transform.position.y);
        return position + transform.lossyScale * collider.offset;
    }

    /**
     * Determine whether the player is grounded.
     */
    private bool IsGrounded() {
        List<RaycastHit2D> raycasts = this.MakeDownRaycasts(charData.raycastDown);
        RaycastHit2D bestHit;
        if (raycasts[0]) {
            if (raycasts[0].normal.y >= raycasts[0].normal.x - 1) {
                return true;
            }
        } else if (raycasts[1]) {
            if (raycasts[1].normal.y >= raycasts[1].normal.x - 1) {
                return true;
            }
        }
        return false;
    }

    /**
     * Return the slope angle.
     * 
     * Also has the side effect of snapping the transform position to the ground's raycast hit point.
     */
    private Vector2 GetSlopeVector() {
        List<RaycastHit2D> raycasts = this.MakeDownRaycasts(charData.raycastDownSlope);
        // snap player to the ground if both raycasts are hit
        if (raycasts[0] && raycasts[1]) {
            RaycastHit2D higher = raycasts[0].point.y > raycasts[1].point.y ? raycasts[0] : raycasts[1];
            transform.position = new Vector2(transform.position.x,
                higher.point.y + transform.lossyScale.y / 2);
            return this.GetSlopeVectorFromHit(higher);
        } else if (raycasts[0]) {
            return this.GetSlopeVectorFromHit(raycasts[0]);
        } else if (raycasts[1]) {
            return this.GetSlopeVectorFromHit(raycasts[1]);
        }
        throw new InvalidOperationException("No raycasts are hit, position is airborne");
    }

    /**
     * Create down raycasts for the left and right of this transform.
     */
    private List<RaycastHit2D> MakeDownRaycasts(float raycastLength) {
        Vector2 colliderPosLeft = this.GetColliderPos();
        colliderPosLeft.x -= collider.bounds.extents.x;
        Vector2 colliderPosRight = this.GetColliderPos();
        colliderPosRight.x += collider.bounds.extents.x;
        return new List<RaycastHit2D> {
            Physics2D.Raycast(colliderPosLeft, Vector2.down, raycastLength, ground),
            Physics2D.Raycast(colliderPosRight, Vector2.down, raycastLength, ground)
        };
    }

    private Vector2 GetSlopeVectorFromHit(RaycastHit2D hit) {
        Debug.DrawRay(hit.point, hit.normal, Color.red);
        Vector2 slopeVector = Vector2.Perpendicular(hit.normal);
        if (slopeVector.x < 0) {
            slopeVector *= -1;
        }
        return slopeVector.normalized;
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
        activeMoveData.suspendGravity = true;
        yield return new WaitForSeconds(time);
        activeMoveData.suspendGravity = false;
    }
}
