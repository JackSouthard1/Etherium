using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMind : Mind {
	void Update () {
		if (myTurn && !gm.transitioning) {
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
			}
		}
	}
}
