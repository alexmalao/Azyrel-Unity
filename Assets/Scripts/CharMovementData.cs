using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/**
 * Dataclass for storing basic physics values. Include in a player controller
 * for basic 
 */
public class CharMovementData {
    
    public static float HORI_ACCEL = 50.0f;
    public static float MAX_HORI_SPEED = 20.0f;
    public static float JUMP_VEL = 20.0f;
    public static float SHORT_JUMP_VEL = 15.0f;

    public static float COLLIDER_OFFSET = 0.5f;
}
