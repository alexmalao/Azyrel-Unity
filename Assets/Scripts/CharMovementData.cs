using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/**
 * Dataclass for storing basic physics values. Include in a player controller
 * for basic 
 */
public class CharMovementData : ScriptableObject {
    
    [SerializeField] public float horizontalSpeed = 25.0f;
    [SerializeField] public float maxHorizontalSpeed = 500.0f;
}
