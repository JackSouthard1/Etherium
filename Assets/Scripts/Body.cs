using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Body : MonoBehaviour {
	private TerrainManager tm;
	private GameManager gm;
	private Mind mind;
	private Player playerScript;
	private Weapon weapon;
	[HideInInspector]
	public bool player = false;

	public float health;

	public Island location = null;

	void Awake () {
		tm = GameObject.Find ("Terrain").GetComponent<TerrainManager> ();
		gm = GameObject.Find ("GameManager").GetComponent<GameManager> ();
		weapon = GetComponentInChildren<Weapon> ();
	
		mind = GetComponentInChildren<Mind> ();
		if (mind.GetType () == typeof(PlayerMind)) {
			player = true;
			playerScript = GetComponent<Player> ();
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

		// test if location is occuplied
		RaycastHit hit;
		if (Physics.Raycast (new Vector3 (newTile.x, 5f, newTile.y), Vector3.down, out hit, 5f)) {
			if (hit.collider.gameObject.layer == 8) {
				if (weapon != null) {
					weapon.Attack (direction, new Vector2(transform.position.x, transform.position.z));
				}
				return;
			}
		}

		if (!tm.GetTileAtPosition(newTile)) {
			dir3 = new Vector3 (direction.x, -transform.position.y, direction.y);
			transform.Translate (dir3, Space.World);
			tm.CreateBus (transform.position);

			// update location
			if (player && location != null) {
				location.PlayerExitIsland ();
			}
			location = null;
		} else {
			float newTileHeight = tm.GetTileAtPosition(newTile).transform.position.y;
			float heightDiff = newTileHeight - transform.position.y;
			dir3 = new Vector3 (direction.x, heightDiff, direction.y);
			transform.Translate (dir3, Space.World);

			// update location
			location = tm.tiles [newTile].island;
			if (player) {
				location.PlayerEnterIsland ();
				if (tm.GetResourceAtPosition (newTile) != null) {
					playerScript.CollectResource (tm.GetResourceAtPosition (newTile));
				}
			}
		}

		transform.eulerAngles = new Vector3 (0f, Mathf.Atan2 (direction.x, direction.y) * Mathf.Rad2Deg, 0f);
	}

	public void TakeDamage (float damage) {
		health -= damage;
		if (health <= 0) {
			Destroy (gameObject);
			if (player) {
				print ("Player Dead");
			} else {
				location.EnemyDeath (GetComponent<Body> ());
				gm.EnemyDeath (GetComponent<Body> ());
			}
		}
	}
}
