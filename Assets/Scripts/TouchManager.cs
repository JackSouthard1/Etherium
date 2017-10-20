using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchManager : MonoBehaviour {
	public static TouchManager instance;
	public System.Action OnTouchDown;
	public System.Action OnTouchUp;
	public static int touchCount;

	bool hasTouchedDown;

	void Awake() {
		instance = this;
	}

	void Update() {
		int prevTouchCount = touchCount;

		if (Input.touchCount > 0) {
			touchCount = Input.touchCount;
		} else {
			touchCount = (Input.GetMouseButton (0)) ? 1 : 0;
		}

		if (Input.GetMouseButtonDown (0) || (touchCount > prevTouchCount)) {
			if (touchCount != 1 || (touchCount == 1 && (Input.mousePosition.y > Screen.height * 0.2f) && (Input.mousePosition.y < Screen.height * 0.8f))) {
				if (OnTouchDown != null) {
					OnTouchDown ();
				}

				hasTouchedDown = true;
			}
		}

		if (((hasTouchedDown && Input.GetMouseButtonUp (0)) || (touchCount < prevTouchCount))) {
			if (OnTouchUp != null) {
				OnTouchUp ();
			}

			if(touchCount > 0) {
				hasTouchedDown = false;
			}
		}
	}
}
