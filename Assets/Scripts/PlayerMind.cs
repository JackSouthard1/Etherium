using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMind : Mind {
	bool isSwiping;

	Player player;

	const float minDistanceForMove = 5f;

	protected override void MindStart () {
		player = Player.instance;
		TouchManager.instance.OnTouchDown += StartSwipe;
		TouchManager.instance.OnTouchUp += CancelSwipe;
	}

	void Update () {
		if (myTurn && !gm.transitioning) {
			HandleKeyboardInput ();
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
	
	void StartSwipe() {
		if (Input.touchCount != 1 || !myTurn || gm.transitioning) {
			return;
		}

		StartCoroutine (WaitForSwipe ());
	}

	void CancelSwipe() {
		isSwiping = false;
	}

	IEnumerator WaitForSwipe() {
		isSwiping = true;
		Vector2 startingTouchPos = Input.GetTouch (0).position;
		while (isSwiping) {
			if (Crafting.instance.isBuilding || (TouchManager.touchCount != 1)) {
				isSwiping = false;
				break;
			}

			Vector2 currentTouchPos = Input.GetTouch(0).position;
			if (Vector2.Distance (startingTouchPos, currentTouchPos) > minDistanceForMove) {
				//TODO: probably could use a lot of refactoring to ensure that these values are exactly what we want
				Vector2 dir = currentTouchPos - startingTouchPos;
				float angle = Mathf.Atan2 (dir.y, dir.x);
				isSwiping = false;

				float roundedAndle = Mathf.RoundToInt ((angle - (Mathf.PI / 4f)) / (Mathf.PI / 2f)) * (Mathf.PI / 2f);
				base.RelayAction (new Vector2(Mathf.RoundToInt(Mathf.Cos(roundedAndle)), Mathf.RoundToInt(Mathf.Sin(roundedAndle))));
			}

			yield return null;
		}
	}

	protected override void EmptyAction () {
		return;
	}

	protected override bool IsAvailableDirection (Vector2 direction) {
		Vector2 position = TerrainManager.PosToV2(transform.position) + direction;

		return !tm.UnstandableBuildingAtPosition (position);
	}
}
