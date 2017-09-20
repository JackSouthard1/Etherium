using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMind : Mind {
	void Update () {
		if (myTurn) {
			if (Input.GetKeyDown (KeyCode.UpArrow)) {
				base.RelayMove (Vector2.up);
			}

			if (Input.GetKeyDown (KeyCode.DownArrow)) {
				base.RelayMove (Vector2.down);
			}

			if (Input.GetKeyDown (KeyCode.RightArrow)) {
				base.RelayMove (Vector2.right);
			}

			if (Input.GetKeyDown (KeyCode.LeftArrow)) {
				base.RelayMove (Vector2.left);
			}
		}
	}
}
