using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/**
 * Basic controller for handling player movement.
 */
public class PlayerController : MonoBehaviour {
    
    private PlayerControls controls;
    private Rigidbody2D body;

    private void Awake() {
        controls = new PlayerControls();
        body = GetComponent<Rigidbody2D>();
    }

    private void OnEnable() {
        controls.Enable();
    }

    private void OnDisable() {
        controls.Disable();
    }

    void Start() {}

    void Update() {
        MoveHorizontal();
    }

    /**
     * Controls the horizontal movement of the character body.
     */
    void MoveHorizontal() {
        float horizontal = controls.Default.Move.ReadValue<float>();

        float updatedXVel = body.velocity.x + horizontal * CharMovementData.HORI_ACCEL * Time.deltaTime;
        if (updatedXVel > CharMovementData.MAX_HORI_SPEED) {
            updatedXVel = Mathf.Max(body.velocity.x, CharMovementData.MAX_HORI_SPEED);
        } else if (updatedXVel < -CharMovementData.MAX_HORI_SPEED) {
            updatedXVel = Mathf.Min(body.velocity.x, -CharMovementData.MAX_HORI_SPEED);
        }

        body.velocity = new Vector2(updatedXVel, body.velocity.y);
    }
}
