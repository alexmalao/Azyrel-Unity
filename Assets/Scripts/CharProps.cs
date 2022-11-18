using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/**
 * Dataclass for storing basic physics values. Include in a player controller
 * for basic physic and collision values.
 * 
 * All acceleration and speed values are stored per second.
 */
public class CharProps : ScriptableObject {
    
    public float groundAccel = 7.5f;
    public float minGroundSpeed = 5.0f;
    public float maxGroundSpeed = 15.0f;

    public float slideAccel = 20.0f;
    public float minSlideSpeed = 2.5f;
    public float maxSlidepeed = 25.0f;

    // speed penalty for being faster than maxGroundSpeed
    public float groundSpeedPenalty = 2.5f;
    // percent of velocity retained each second while grounded
    public float traction = 0.04f;
    // percent of distance position is snapped to ground each second
    public float groundSnapModifier = 0.01f;
    // speed at which character will default stop while grounded
    public float stopSpeed = 1.5f;

    public float jumpVel = 17.5f;
    public float shortJumpVel = 10.0f;

    public float airJumpVel = 17.5f;
    public float airXAccel = 20.0f;
    // minimum horizontal jump speed if a direction is held
    public float minAirJumpXSpeed = 7.5f;
    public float maxAirXSpeed = 7.5f;
    public float airDownDashVel = -15.0f;
    public float dashFloatDur = 0.25f;

    public float wallJumpSpeed = 17.5f;
    public float wallRunSpeed = 17.5f;
    public float wallSlideAccel = 15.0f;
    public float wallUpwardsSlideAccel = 50.0f;
    public float wallSlideMaxSpeed = 7.5f;

    public float raycastDown = 1.1f;
    public float raycastDownSlope = 2.1f;
    public float raycastHorizontal = 0.6f;
}