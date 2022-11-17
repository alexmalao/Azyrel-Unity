using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/**
 * Dataclass for storing basic physics values. Include in a player controller
 * for basic physic and collision values.
 * 
 * All acceleration and speed values are stored per second.
 */
public class CharMovementData {
    
    public float xAccel = 7.5f;
    public float airXAccel = 15.5f;
    public float minXSpeed = 5.0f;
    public float maxXSpeed = 15.0f;

    // speed penalty for being faster than maxXSpeed
    public float xSpeedPenalty = 2.5f;
    // percent of velocity retained each second while grounded
    public float traction = 0.04f;
    // speed at which character will default stop while grounded
    public float stopSpeed = 1.5f;

    public float jumpVel = 20f;
    public float shortJumpVel = 12.5f;

    public float airJumpVel = 17.5f;
    // minimum horizontal jump speed if a direction is held
    public float minAirJumpXSpeed = 7.5f;
    public float maxAirXSpeed = 7.5f;
    public float airDownDashVel = -25.0f;
    public float dashFloatDur = 0.25f;

    public float colliderOffset = 0.01f;
}
