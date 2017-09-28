using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMind : Mind {
	private Transform target;
	List<Vector2> allDirections = new List<Vector2>() {Vector2.right, Vector2.up, Vector2.left, Vector2.down};

	protected override void MindStart () {
		target = GameObject.Find ("Player").transform;
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
	}

	protected override void CalculateMove () {
		List<Vector2> availableDirections = new List<Vector2> (allDirections);
		for (int i = 0; i < allDirections.Count; i++) {
			Vector2 position = new Vector2 (transform.position.x, transform.position.z) + allDirections[i];
			if (body.EnemyAtPosition (position) || !body.tm.GetTileAtPosition(position)) {
				availableDirections.Remove (allDirections[i]);
			}
		}
		if (availableDirections.Count == 0) {
			base.Idle ();
		} else {
			base.RelayAction (CalculateAction (availableDirections));
		}
	}
}
