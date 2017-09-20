using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Body : MonoBehaviour {
	private TerrainManager tm;
	private GameManager gm;
	private Mind mind;
	private bool player = false;

	public Island location = null;

	void Awake () {
		tm = GameObject.Find ("Terrain").GetComponent<TerrainManager> ();
		gm = GameObject.Find ("GameManager").GetComponent<GameManager> ();
	
		mind = GetComponentInChildren<Mind> ();
		if (mind.GetType () == typeof(PlayerMind)) {
			player = true;
		}
	}

	public void TurnStart () {
		mind.TurnStart ();
	}

	public void TurnEnd () {
		if (player) {
			gm.PlayerTurnEnd ();
		} else {
			gm.EnemyTurnDone ();
		}
	}

	public void Move (Vector2 direction) {
		Vector3 dir3;
		Vector2 newTile = new Vector2(transform.position.x + direction.x, transform.position.z + direction.y);

		if (!tm.GetTileAtPosition(newTile)) {
			dir3 = new Vector3 (direction.x, 0f, direction.y);
			transform.Translate (dir3);
			tm.CreateBus (transform.position);

			if (player && location != null) {
				location.PlayerExitIsland ();
			}
			location = null;
		} else {
			float newTileHeight = tm.GetTileAtPosition(newTile).transform.position.y;
			float heightDiff = newTileHeight - transform.position.y;
			dir3 = new Vector3 (direction.x, heightDiff, direction.y);
			transform.Translate (dir3);

			location = tm.tiles [newTile].island;
			if (player) {
				location.PlayerEnterIsland ();
			}
		}
	}
}
