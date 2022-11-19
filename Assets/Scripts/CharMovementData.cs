using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/**
 * Data class for storing active movement data in real time.
 */
public class CharMovementData : ScriptableObject {
    public static int JUMPS = 1;

    ///////////////////////////
    /// Movement Attributes ///
    ///////////////////////////
    public int curJumps = CharMovementData.JUMPS;
    public bool hasWallRun = true;
    public bool facingRight = true;
    public bool onRightWall = false;
    public bool wallVaulted = false;

    ////////////////////
    /// Logic Values ///
    ////////////////////
    public bool lastFrameGrounded = true;
    // boolean for keeping track of jumps started slightly before going airborne
    public bool edgeJump = false;
    // determines whether gravity should be suspended
    public bool suspendGravity = false;

    /**
     * Reset the total amount of jumps. Call upon landing.
     */
    public void ResetJumps() {
        this.curJumps = CharMovementData.JUMPS;
    }

    /**
     * Reset the wall run. Call upon performing any jump or landing.
     */
    public void ResetWallRun() {
        this.hasWallRun = true;
    }

    /**
     * Determine whether a wall run is possible, then disable the wall run.
     */
    public bool AttemptWallRun() {
        if (this.hasWallRun) {
            this.hasWallRun = false;
            return true;
        }
        return false;
    }

    /**
     * Update the character's direction.
     */
    public void UpdateDirection(float xVelocity) {
        if (xVelocity > 0.001) {
            this.facingRight = true;
        }
        if (xVelocity < -0.001) {
            this.facingRight = false;
        }
    }

    /**
     * Determine whether a jump is possible, then decrement the available jumps.
     */
    public bool AttemptJump() {
        if (this.curJumps > 0) {
            this.curJumps -= 1;
            return true;
        }
        return false;
    }
}