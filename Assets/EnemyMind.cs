using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMind : Mind {
	private Transform target;

	protected override void MindStart () {
		target = GameObject.Find ("Player").transform;
	}

	public override void TurnStart () {
		if (active) {
			if (target.position.x > transform.position.x) {
				base.RelayMove (Vector2.right);
			} else if (target.position.x < transform.position.x) {
				base.RelayMove (Vector2.left);
			} else if (target.position.z > transform.position.z) {
				base.RelayMove (Vector2.up);
			} else if (target.position.z < transform.position.z) {
				base.RelayMove (Vector2.down);
			} else {
				base.RelayMove (Vector2.zero);
			}
		} else {
			base.Idle ();
		}
	}
}
