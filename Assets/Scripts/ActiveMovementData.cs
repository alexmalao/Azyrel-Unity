using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/**
 * Data class for storing active movement data in real time.
 */
public class ActiveMovementData {
    public static int JUMPS = 1;

    public int curJumps = ActiveMovementData.JUMPS;
    public bool lastFrameGrounded = true;
    public bool facingRight = true;
    // boolean for keeping track of jumps started slightly before going airborne
    public bool edgeJump = false;
    // determines whether gravity should be suspended
    public bool suspendGravity = false;

    /**
     * Reset the total amount of jumps. Call upon landing.
     */
    public void ResetJumps() {
        this.curJumps = ActiveMovementData.JUMPS;
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