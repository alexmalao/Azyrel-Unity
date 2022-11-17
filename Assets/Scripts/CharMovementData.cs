using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/**
 * Dataclass for storing basic physics values. Include in a player controller
 * for basic physic and collision vaues.
 */
public class CharMovementData {
    
    public float xAccel = 30.0f;
    public float airXAccel = 20.0f;
    public float minXSpeed = 7.5f;
    public float maxXSpeed = 15.0f;

    public float jumpVel = 21.5f;
    public float shortJumpVel = 14.5f;

    public float airJumpVel = 20.0f;
    public float minAirXJumpSpeed = 7.5f;
    public float maxAirXSpeed = 7.5f;
    public float airDownDashVel = -25.0f;
    public float dashFloatDur = 0.20f;

    public float colliderOffset = 0.01f;
}
