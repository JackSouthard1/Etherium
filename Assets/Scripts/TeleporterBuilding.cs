using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleporterBuilding : Building {
	public int id;
	public int targetId = -1;

	public static List<TeleporterBuilding> teleporters = new List<TeleporterBuilding>();

	int GetRandomTarget() {
		if(teleporters.Count <= 1) {
			return -1;
		}

		int newTarget;
		while (true) {
			newTarget = Random.Range (0, teleporters.Count);
			if (newTarget != id && teleporters[newTarget].island != island) {
				break;
			}
		}

		return newTarget;
	}

	public override void Init (BuildingInfo info, Island island) {
		id = teleporters.Count;
		teleporters.Add (this);
		base.Init (info, island);
	}

	public override void TurnEnd () {
		if (state != BuildingState.Active) {
			return;
		}

		if (targetId == -1) {
			targetId = GetRandomTarget ();
			if (targetId == -1) {
				return;
			}
		}

		if (IsPlayerInBuilding) {
			StartCoroutine(Player.instance.Teleport (teleporters[targetId].position, "Teleporting"));
			GameManager.instance.SaveThisTurn ();
		}
	}

	public Vector2 position {
		get {
			return TerrainManager.PosToV2 (gameObject.transform.position);
		}
	}
}