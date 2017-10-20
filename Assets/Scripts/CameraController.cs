using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {
	public float zoomSpeed;
	public float zoomFriction;
	public float minZoom;
	public float maxZoom;
	float zoomMomentum;

	Transform target;
	Vector3 offset;
	Camera cam;

	bool isZooming;

	void Start () {
		cam = gameObject.GetComponent<Camera> ();
		TouchManager.instance.OnTouchDown += StartZoom;
		TouchManager.instance.OnTouchUp += EndZoom;
	}

	public void Init () {
		target = GameObject.Find ("Player").transform.Find("Model");
		offset = transform.position;
	}
	
	void LateUpdate () {
		if (target != null) {
			transform.position = target.position + offset;
		}
	}

	void StartZoom () {
		if (TouchManager.touchCount != 2) {
			return;
		}

		StopCoroutine (Zoom ());
		StartCoroutine (Zoom ());
	}

	IEnumerator Zoom () {
		isZooming = true;

		while (isZooming) {
			Touch touchZero = Input.GetTouch (0);
			Touch touchOne = Input.GetTouch (1);

			Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
			Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

			float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
			float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

			float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

			zoomMomentum = deltaMagnitudeDiff * zoomSpeed;

			cam.orthographicSize += zoomMomentum;
			cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);

			zoomMomentum *= Mathf.Pow(zoomFriction, Time.deltaTime);

			yield return null;
		}

		while (zoomMomentum > 0.05f) {
			cam.orthographicSize += zoomMomentum;
			cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);

			zoomMomentum *= Mathf.Pow(zoomFriction, Time.deltaTime);

			yield return null;
		}
	}

	void EndZoom () {
		isZooming = false;
	}
}
