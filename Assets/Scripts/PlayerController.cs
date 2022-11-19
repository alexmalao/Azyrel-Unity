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
    protected CharProps charProps;
    protected CharMovementData moveData;

    protected Rigidbody2D body;
    protected Collider2D collider;

    //////////////////////
    /// Setup Methods. ///
    //////////////////////

    private void Awake() {
        this.controls = new PlayerControls();
        this.charProps = CharProps.CreateInstance<CharProps>();
        this.moveData = CharMovementData.CreateInstance<CharMovementData>();

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
        this.controls.Move.Jump.started += _ => InstantJump();
        this.controls.Move.Jump.performed += _ => Jump();
        this.controls.Move.ShortJump.performed += _ => ShortJump();
        this.controls.Move.Dash.performed += _ => Dash();
    }

    void Update() {
        this.MoveHorizontal();
        this.MovementDataUpdate();
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

        if (this.moveData.onRightWall) {
            float relativeMagnitude = body.velocity.y;
            // TODO: SLOPE VECTOR AND WALL SNAPPING
            if (horizontal == -1) {
                this.moveData.onRightWall = false;
                return;
            }

            float wallSlideAccel = relativeMagnitude < 0 ?
                charProps.wallSlideAccel : charProps.wallUpwardsSlideAccel;

            if (relativeMagnitude > -charProps.wallSlideMaxSpeed) {
                relativeMagnitude = Mathf.Max(-charProps.wallSlideMaxSpeed,
                    relativeMagnitude - wallSlideAccel * Time.deltaTime);
            } else if (relativeMagnitude < -charProps.wallSlideMaxSpeed) {
                relativeMagnitude = Mathf.Min(-charProps.wallSlideMaxSpeed,
                    relativeMagnitude + wallSlideAccel * Time.deltaTime);
            }
            newVelocity = new Vector2(0.0f, relativeMagnitude);
        } else if (this.IsGrounded() || this.moveData.lastFrameGrounded) {
            newVelocity = this.MoveGrounded(horizontal, look);
        } else {
            newVelocity = this.MoveAirborne(horizontal);
        }

        // apply the new velocity
        body.velocity = newVelocity;
    }

    /**
     * Update whether to reset jumps.
     */
    void MovementDataUpdate() {
        float horizontal = controls.Move.Move.ReadValue<float>();
        bool isGrounded = this.IsGrounded();

        if (isGrounded) {
            body.gravityScale = 0.0f;
            this.moveData.UpdateDirection(body.velocity.x);
            this.moveData.ResetJumps();
            this.moveData.ResetWallRun();
            this.moveData.onRightWall = false;

        } else if (!moveData.suspendGravity && this.IsAirborne()) {
            body.gravityScale = 1.0f;
        }

        // only attach to the wall if there is a towards input
        if (this.IsTouchingRightWall() && horizontal == 1 && !this.moveData.wallVaulted) {
            this.moveData.onRightWall = true;
            body.gravityScale = 0.0f;
        } else if (!this.IsTouchingRightWall()) {
            this.moveData.onRightWall = false;
        }
        this.moveData.lastFrameGrounded = isGrounded;
    }

    ////////////////////////////////////////
    /// Methods for mapped button slots. ///
    ////////////////////////////////////////

    /**
     * Perform a grounded or aerial jump if possible.
     */
    void Jump() {
        GroundJump(this.charProps.jumpVel);
    }

    /**
     * Perform a grounded or aerial jump if possible.
     */
    void ShortJump() {
        GroundJump(this.charProps.shortJumpVel);
    }

    /**
     * Perform an aerial jump if possible.
     */
    void InstantJump() {
        if (this.moveData.onRightWall && !this.IsGrounded()) {
            body.velocity = charProps.wallJumpVector * charProps.wallJumpSpeed;
            StartCoroutine(this.WallVault());
            this.moveData.onRightWall = false;
        } else if (this.IsAirborne() && this.moveData.AttemptJump()) {
            float horizontal = controls.Move.Move.ReadValue<float>();

            // perform jump, apply minimum jump speed
            float newXVal = body.velocity.x;
            if (horizontal == 1) {
                newXVal = Mathf.Max(body.velocity.x, this.charProps.minAirJumpXSpeed);
            } else if (horizontal == -1) {
                newXVal = Mathf.Min(body.velocity.x, -this.charProps.minAirJumpXSpeed);
            }
            body.velocity = new Vector2(newXVal, charProps.airJumpVel);
        } else if (this.IsGrounded()) {
            this.moveData.edgeJump = true;
        }
    }

    /**
     *  Perform a grounded or aerial dash if possible.
     */
    void Dash() {
        float horizontal = controls.Move.Move.ReadValue<float>();
        bool right;
        if (horizontal == 0) {
            // no input direction default to facing direction
            right = this.moveData.facingRight;
        } else {
            right = horizontal > 0;
        }

        if (this.moveData.onRightWall && !this.IsGrounded()) {
            // dash off a wall
            body.velocity = new Vector2(-charProps.maxGroundSpeed, 0.0f);
            StartCoroutine(this.WallVault());
            StartCoroutine(SuspendDashGravity(charProps.dashFloatDur));
            this.moveData.onRightWall = false;
        }
        else if (this.IsGrounded()) {
            // ground dash
            float newVelocity;
            float magnitude = body.velocity.magnitude;

            // executing land, revoking landing magnitude
            if (Math.Abs(body.velocity.y) > Mathf.Abs(body.velocity.x) + 0.01f) {
                magnitude = 0;
            }

            Vector2 slopeVector = this.GetGroundSlopeVector();
            if (right) {
                newVelocity = Mathf.Max(magnitude, this.charProps.maxGroundSpeed);
            } else {
                newVelocity = Mathf.Min(-magnitude, -this.charProps.maxGroundSpeed);
            }
            body.velocity = new Vector2(newVelocity * slopeVector.x, newVelocity * slopeVector.y);
            if (Mathf.Abs(slopeVector.y) < 0.01f) {
                StartCoroutine(SuspendDashGravity(charProps.dashFloatDur));
            }
        } else if (this.IsAirborne() && !this.moveData.lastFrameGrounded) {
            // aerial dash
            float look = controls.Move.Look.ReadValue<float>();
            if (look == -1 && body.velocity.y <= 0) {

                this.body.gravityScale = 1.0f;
                float newYVal = Mathf.Min(body.velocity.y, this.charProps.airDownDashVel);
                body.velocity = new Vector2(body.velocity.x, newYVal);
                this.moveData.UpdateDirection(body.velocity.x);
            }

            if (look != -1 && this.moveData.AttemptJump()) {
                float newXVal;
                if (right) {
                    newXVal = Mathf.Max(body.velocity.x, this.charProps.maxGroundSpeed);
                } else {
                    newXVal = Mathf.Min(body.velocity.x, -this.charProps.maxGroundSpeed);
                }
                body.velocity = new Vector2(newXVal, 0.0f);
                StartCoroutine(SuspendDashGravity(charProps.dashFloatDur));
                this.moveData.UpdateDirection(body.velocity.x);
            }
        }
    }

    ////////////////////////
    /// Private Methods. ///
    ////////////////////////

    /**
     * Get the velocity vector for grounded movement.
     */
    private Vector2 MoveGrounded(float horizontal, float look) {

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
            slopeVector = this.GetGroundSlopeVector();
        }
        catch (InvalidOperationException) {
            return body.velocity;
        }

        float minGroundSpeed = charProps.minGroundSpeed;
        float maxGroundSpeed = charProps.maxGroundSpeed;
        float modifiedVelocity = relativeMagnitude + horizontal * charProps.groundAccel * Time.deltaTime;
        float speedPenalty = charProps.groundSpeedPenalty * Time.deltaTime;

        if (Mathf.Abs(slopeVector.y) > Math.Abs(slopeVector.x) - 0.001f && look == -1) {
            // sliding time
            horizontal = slopeVector.y < 0 ? 1.0f : -1.0f;
            modifiedVelocity = relativeMagnitude + horizontal * charProps.slideAccel * Time.deltaTime;
            minGroundSpeed = charProps.minSlideSpeed;
            maxGroundSpeed = charProps.maxSlidepeed;
        }

        if (horizontal == 1 && body.velocity.x > -0.001f) {
            // grounded movement will only trigger when already in the direction of input
            modifiedVelocity = Mathf.Max(modifiedVelocity, minGroundSpeed);
            if (modifiedVelocity > maxGroundSpeed) {
                modifiedVelocity = Mathf.Max(relativeMagnitude - speedPenalty, maxGroundSpeed);
            }
            return new Vector2(modifiedVelocity * slopeVector.x, modifiedVelocity * slopeVector.y);
        }
        else if (horizontal == -1 && body.velocity.x < 0.001f) {
            modifiedVelocity = Mathf.Min(modifiedVelocity, -minGroundSpeed);
            if (modifiedVelocity < -maxGroundSpeed) {
                modifiedVelocity = Mathf.Min(relativeMagnitude + speedPenalty, -maxGroundSpeed);
            }
            return new Vector2(modifiedVelocity * slopeVector.x, modifiedVelocity * slopeVector.y);
        }
        else {
            // stop the character
            if (Mathf.Abs(body.velocity.x) < charProps.stopSpeed &&
                Mathf.Abs(body.velocity.y) < charProps.stopSpeed) {
                return new Vector2(0.0f, 0.0f);
            }
            else {
                relativeMagnitude *= Mathf.Pow(charProps.traction, Time.deltaTime);
                return new Vector2(
                    relativeMagnitude * slopeVector.x,
                    relativeMagnitude * slopeVector.y);
            }
        }
    }

    /**
     * Get the velocity vector for airborne movement.
     */
    private Vector2 MoveAirborne(float horizontal) {
        float updatedXVel;

        // airborne movement does not apply any traction or slowdown
        float modifiedSpeed = body.velocity.x + horizontal * charProps.airXAccel * Time.deltaTime;
        if (modifiedSpeed > charProps.maxAirXSpeed) {
            updatedXVel = Mathf.Min(modifiedSpeed, body.velocity.x);
            updatedXVel = Mathf.Max(updatedXVel, charProps.maxAirXSpeed);
        }
        else if (modifiedSpeed < -charProps.maxAirXSpeed) {
            updatedXVel = Mathf.Max(modifiedSpeed, body.velocity.x);
            updatedXVel = Mathf.Min(updatedXVel, -charProps.maxAirXSpeed);
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
        if (isGrounded || this.moveData.edgeJump) {

            jumpVel = Mathf.Max(jumpVel, body.velocity.y);
            body.velocity = new Vector2(body.velocity.x, jumpVel);
            this.moveData.suspendGravity = false;
        }
        this.moveData.edgeJump = false;
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
        List<RaycastHit2D> raycasts = this.MakeDownRaycasts(charProps.raycastDown);
        return (raycasts[0] || raycasts[1]);
    }

    /**
     * Determine if the player is on the right wall.
     */
    private bool IsTouchingRightWall() {
        List<RaycastHit2D> raycasts = this.MakeHorizontalRaycasts(charProps.raycastHorizontal, Vector2.right);
        if (raycasts[0]) {
            Vector2 slopeVector = GetSlopeVectorFromHit(raycasts[0]);
            return Mathf.Abs(slopeVector.y) > slopeVector.x + 0.01f;
        } else if (raycasts[1]) {
            Vector2 slopeVector = GetSlopeVectorFromHit(raycasts[1]);
            return Mathf.Abs(slopeVector.y) > slopeVector.x + 0.01f;
        }
        return false;
    }

    /**
     * Return the slope angle.
     * 
     * Also has the side effect of snapping the transform position to the ground's raycast hit point.
     */
    private Vector2 GetGroundSlopeVector() {
        List<RaycastHit2D> raycasts = this.MakeDownRaycasts(charProps.raycastDownSlope);
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

    /**
     * Create horizontal raycasts for the top and bottom of this transform.
     */
    private List<RaycastHit2D> MakeHorizontalRaycasts(float raycastLength, Vector2 direction) {
        Vector2 colliderPosBot = this.GetColliderPos();
        colliderPosBot.y -= collider.bounds.extents.y;
        Vector2 colliderPosTop = this.GetColliderPos();
        colliderPosTop.y += collider.bounds.extents.y;
        return new List<RaycastHit2D> {
            Physics2D.Raycast(colliderPosBot, direction, raycastLength, ground),
            Physics2D.Raycast(colliderPosTop, direction, raycastLength, ground)
        };
    }

    /**
     * Return a normalized vector in the positive x direction.
     */
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
        return !this.IsGrounded() && !this.moveData.onRightWall;
    }

    /**
     * Suspends gravity for a set amount of time.
     */
    private IEnumerator SuspendDashGravity(float time) {
        body.gravityScale = 0.0f;
        moveData.suspendGravity = true;
        yield return new WaitForSeconds(time);
        moveData.suspendGravity = false;
    }
    
    /**
     * Vault off the wall, preventing immediate wall cling.
     */
    private IEnumerator WallVault() {
        this.moveData.wallVaulted = true;
        yield return new WaitForSeconds(charProps.wallVaultDur);
        this.moveData.wallVaulted = false;
    }
}
