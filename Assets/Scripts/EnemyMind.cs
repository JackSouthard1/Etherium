using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMind : Mind {
	private Transform target;

	protected override void MindStart () {
		target = GameObject.Find ("Player").transform;
	}

	Vector2 CalculateAction (List<Vector2> avaiableDirections) {
		Vector2 rawDiff = TerrainManager.PosToV2(target.position) - TerrainManager.PosToV2(transform.position);

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
		List<Vector2> availableDirections = GetAvailableDirections();
		if (availableDirections.Count == 0) {
			base.Idle ();
		} else {
			base.RelayAction (CalculateAction (availableDirections));
		}
	}

	protected override void EmptyAction () {
		base.Idle ();
	}

	protected override bool IsAvailableDirection (Vector2 direction) {
		Vector2 position = TerrainManager.PosToV2 (transform.position);

		return !tm.UnstandableBuildingAtPosition (position) && !tm.EnemyInRange(position, direction, 1) && tm.GetTileAtPosition(position + direction);
	}
}
