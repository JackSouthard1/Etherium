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
//			Vector2[] directions = new Vector2[4] {Vector2.right, Vector2.up, Vector2.left, Vector2.down};
			List<Vector2> availableDirections = new List<Vector2>() {Vector2.right, Vector2.up, Vector2.left, Vector2.down};
			for (int i = 0; i < availableDirections.Count; i++) {
				Vector2 position = new Vector2 (transform.position.x, transform.position.z) + availableDirections[i];
				if (body.EnemyAtPosition (position)) {
					availableDirections.Remove (availableDirections[i]);
				}
			}

			if (availableDirections.Count == 0) {
				base.Idle ();
				return;
			}

			base.RelayAction (CalculateAction(availableDirections));
		} else {
			base.Idle ();
		}
	}

	Vector2 CalculateAction (List<Vector2> avaiableDirections) {
		Vector2 rawDiff = new Vector2(target.position.x, target.position.z) - new Vector2(transform.position.x, transform.position.z);

		Vector2 bestDirection = Vector2.zero;
		float smallestDistance = Mathf.Infinity;
		for (int i = 0; i < avaiableDirections.Count; i++) {
			Vector2 directionalDiff = rawDiff - avaiableDirections[i];
			float distance = directionalDiff.magnitude;

			if (distance < smallestDistance) {
				smallestDistance = distance;
				bestDirection = avaiableDirections[i];
			}
		}

		return bestDirection;
				


//		bool movingOnX;
//		if (Mathf.Abs (rawDiff.x) > Mathf.Abs (rawDiff.y)) {
//			movingOnX = true;
//		} else {
//			movingOnX = false;
//		}
//
//		if (movingOnX) {
//			rawDiff = new Vector2 (rawDiff.x / Mathf.Abs (rawDiff.x), 0f);
//		} else {
//			rawDiff = new Vector2 (0f, rawDiff.y / Mathf.Abs (rawDiff.y));
//		}
//
//		return rawDiff;
	}
}
