using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Following tutorial https://www.youtube.com/watch?v=LqnPeqoJRFY
public class PlayerLook : MonoBehaviour {
	
	public float sensitivityX;
	public float sensitivityY;

	private Camera cam;
	private float mouseX;
	private float mouseY;
	private float multiplier = 0.01f;
	private float xRotation;
	private float yRotation;

	private void Start() {
		cam = GetComponentInChildren<Camera>();
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	private void Update() {
		GetInput();
		cam.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
		transform.rotation = Quaternion.Euler(0, yRotation, 0);
	}

	private void GetInput() {
		if (GetComponent<PlayerHandler>().inventoryImage.enabled) return;
		mouseX = Input.GetAxisRaw("Mouse X");
		mouseY = Input.GetAxisRaw("Mouse Y");
		yRotation += mouseX * sensitivityX * multiplier;
		xRotation -= mouseY * sensitivityY * multiplier;
		xRotation = Mathf.Clamp(xRotation, -90f, 90f);
	}
}
