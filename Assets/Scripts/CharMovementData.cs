using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/**
 * Dataclass for storing basic physics values. Include in a player controller
 * for basic physic and collision vaues.
 */
public class CharMovementData : ScriptableObject {
    
    public float hori_accel = 50.0f;
    public float air_hori_accel = 25.0f;
    public float max_hori_speed = 20.0f;

    public float jump_vel = 21.5f;
    public float short_jump_vel = 14.5f;
    public float air_jump_vel = 20.0f;
    public float boface_modifier = 0.75f;

    public float collider_offset = 0.05f;
}
