using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Following tutorial https://www.youtube.com/watch?v=LqnPeqoJRFY
public class PlayerMovement : MonoBehaviour {

	public float movementSpeed = 6f;
	public float movementMultiplier = 10f;
	public float airMovementMultiplier = 0.4f;
	public float groundDrag = 6f;
	public float airDrag = 2f;
	public float jumpForce = 15f;
	public KeyCode jumpKey = KeyCode.Space;	

	private float playerHeight = 2f;
	private float horizontalMovement;
	private float verticalMovement;
	private Vector3 moveDirection;
	private Rigidbody rb;
	private bool isGrounded;
	private bool isSprinting = false;

	private void Start() {
		rb = GetComponent<Rigidbody>();
		rb.freezeRotation = true;
	}

	private void Update() {
		if (transform.position.y < 32.0f) // Underwater
			isGrounded = true;
		else
			isGrounded = Physics.Raycast(transform.position, Vector3.down, playerHeight / 2.0f + 0.1f);
		GetInput();
		ControlDrag();

		if (Input.GetKeyDown(jumpKey) && isGrounded) {
			Jump();
		}
	}

	private void Jump() {
		rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
	}

	private void ControlDrag() {
		if (isGrounded) rb.drag = groundDrag;
		else rb.drag = airDrag;
	}

	private void GetInput() {
		if (GetComponent<PlayerHandler>().inventoryImage.enabled) return;
		horizontalMovement = Input.GetAxisRaw("Horizontal");
		verticalMovement = Input.GetAxisRaw("Vertical");
		isSprinting = Input.GetKey(KeyCode.LeftControl);

		moveDirection = transform.forward * verticalMovement + transform.right * horizontalMovement;
	}

	private void FixedUpdate() {
		MovePlayer();
	}

	private void MovePlayer() {
		float multipliers = movementMultiplier * (isGrounded ? 1.0f : airMovementMultiplier) * (isSprinting ? 1.5f : 1.0f);
		rb.AddForce(moveDirection.normalized * movementSpeed * multipliers, ForceMode.Acceleration);
	}
}
