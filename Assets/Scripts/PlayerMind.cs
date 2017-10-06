using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMind : Mind {
	Vector2? startingTouchPos = null;
	bool finishedSwipe = false;

	Player player;

	const float minDistanceForMove = 5f;

	void Awake () {
		player = GameObject.FindObjectOfType<Player> ();
	}

	void Update () {
		if (myTurn && !gm.transitioning) {
			HandleKeyboardInput ();
			HandleTouchInput ();
		}
	}

	void HandleKeyboardInput() {
		if (Input.GetKeyDown (KeyCode.UpArrow)) {
			base.RelayAction (Vector2.up);
		}

		if (Input.GetKeyDown (KeyCode.DownArrow)) {
			base.RelayAction (Vector2.down);
		}

		if (Input.GetKeyDown (KeyCode.RightArrow)) {
			base.RelayAction (Vector2.right);
		}

		if (Input.GetKeyDown (KeyCode.LeftArrow)) {
			base.RelayAction (Vector2.left);
		}

		if (Input.GetKeyDown (KeyCode.Space)) {
			base.Idle();
			StartCoroutine (player.ShowIdleUI ());
		}
	}

	void HandleTouchInput() {
		if (Input.touchCount > 0) {
			Touch firstTouch = Input.GetTouch (0);
			
			if (firstTouch.phase == TouchPhase.Began) {
				startingTouchPos = firstTouch.position;
				finishedSwipe = false;
			} else if (firstTouch.phase == TouchPhase.Ended) {
				startingTouchPos = null;
			} else if (!finishedSwipe) {
				Vector2 currentTouchPos = firstTouch.position;
				if (Vector2.Distance (startingTouchPos.GetValueOrDefault(Vector2.zero), currentTouchPos) > minDistanceForMove) {
					//TODO: probably could use a lot of refactoring to ensure that these values are exactly what we want
					Vector2 dir = currentTouchPos - startingTouchPos.GetValueOrDefault(Vector2.zero);
					float angle = Mathf.Atan2 (dir.y, dir.x);
					finishedSwipe = true;

					float roundedAndle = Mathf.RoundToInt ((angle - (Mathf.PI / 4f)) / (Mathf.PI / 2f)) * (Mathf.PI / 2f);
					base.RelayAction (new Vector2(Mathf.RoundToInt(Mathf.Cos(roundedAndle)), Mathf.RoundToInt(Mathf.Sin(roundedAndle))));
				}
			}
		}
	}
}
