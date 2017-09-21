using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {
	public Transform target;
	Vector3 offset;

	void Start () {
		offset = transform.position;
	}
	
	void LateUpdate () {
		if (target != null) {
			transform.position = target.position + offset;
		}
	}
}
