using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/**
 * Basic controller for handling player movement.
 */
public class PlayerController : MonoBehaviour {
    
    private PlayerControls controls;
    private Rigidbody2D body;
    private CharMovementData moveData;

    private void Awake() {
        controls = new PlayerControls();
        body = GetComponent<Rigidbody2D>();
        moveData = CharMovementData.CreateInstance<CharMovementData>();
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

    void MoveHorizontal() {
        float horizontal = controls.Default.Move.ReadValue<float>();

        float deltaX = body.velocity.x + horizontal * moveData.horizontalSpeed * Time.deltaTime;
        Debug.Log(horizontal);
        Debug.Log(deltaX);

        // FIXME: for some reason this value occassionally doesnt not like getting modified
        body.velocity = new Vector2(deltaX, body.velocity.y);
    }
}
