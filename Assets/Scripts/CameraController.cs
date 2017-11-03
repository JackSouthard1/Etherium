using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {
	public static CameraController instance;

	public float zoomSpeed;
	public float zoomFriction;
	public float minZoom;
	public float maxZoom;
	float zoomMomentum;

	Transform target;
	Vector3 offset;
	Camera cam;

	bool isZooming;
	bool smoothMoving;

	void Awake () {
		instance = this;
	}

	void Start () {
		cam = gameObject.GetComponent<Camera> ();
		TouchManager.instance.OnTouchDown += StartZoom;
		TouchManager.instance.OnTouchUp += EndZoom;
	}

	public void Init () {
		target = GameObject.Find ("Player").transform.Find("Model");
		offset = transform.position;
	}

	void Update () {
		if (Input.GetKey (KeyCode.Equals)) {
			zoomMomentum = -0.25f;
		}

		if (Input.GetKey (KeyCode.Minus)) {
			zoomMomentum = 0.25f;
		}
	}

	void LateUpdate () {
		if (target != null && !smoothMoving) {
			transform.position = target.position + offset;
		}

		if (Mathf.Abs(zoomMomentum) > 0.05f) {
			cam.orthographicSize += zoomMomentum;
			cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);

			zoomMomentum *= Mathf.Pow(zoomFriction, Time.deltaTime);
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

			yield return null;
		}
	}

	void EndZoom () {
		isZooming = false;
	}

	public IEnumerator SmoothMoveOverTime(Vector3 startingPlayerPos, Vector3 endingPlayerPos, float time) {
		smoothMoving = true;

		float timeLeft = time;
		Vector3 startingPos = startingPlayerPos + offset;
		Vector3 endingPos = endingPlayerPos + offset;

		while (timeLeft > 0f) {
			timeLeft -= Time.deltaTime;
			transform.position = Vector3.Lerp (startingPos, endingPos, 1f - (timeLeft / time));
			yield return null;
		}

		smoothMoving = false;
	}
}
