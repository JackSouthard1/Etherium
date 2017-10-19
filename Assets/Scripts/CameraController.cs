using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {
	Transform target;
	Vector3 offset;

	public void Init () {
		target = GameObject.Find ("Player").transform.Find("Model");
		offset = transform.position;
	}
	
	void LateUpdate () {
		if (target != null) {
			transform.position = target.position + offset;
		}
	}
}
