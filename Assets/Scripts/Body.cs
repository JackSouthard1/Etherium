using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Body : MonoBehaviour {
	private TerrainManager tm;
	private GameManager gm;
	private Transform model;
	private Mind mind;
	private Player playerScript;
	private Weapon weapon;
	private MapReveal mr;
	[HideInInspector]
	public bool player = false;

	public float health;

	public Island location = null;

	private float moveTime = 0.2f;

	void Awake () {
		tm = GameObject.Find ("Terrain").GetComponent<TerrainManager> ();
		gm = GameObject.Find ("GameManager").GetComponent<GameManager> ();
		weapon = GetComponentInChildren<Weapon> ();
		model = transform.Find ("Model");
	
		mind = GetComponentInChildren<Mind> ();
		if (mind.GetType () == typeof(PlayerMind)) {
			player = true;
			playerScript = GetComponent<Player> ();
		}
	}

	void Start () {
		if (player) {
			mr = GetComponent<MapReveal> ();
			StartAction (Vector2.zero);
		}
	}

	public void TurnStart () {
		mind.TurnStart ();
	}

	public void TurnEnd () {
		if (player) {
			gm.PlayerTurnEnd ();
			mr.PlayerPositionChanged ();
		} else {
			gm.EnemyTurnDone ();
		}
	}

	public void StartAction (Vector2 direction) {
		Vector2 newTile = new Vector2(transform.position.x + direction.x, transform.position.z + direction.y);
		Quaternion targetRot = Quaternion.Euler(new Vector3 (0f, Mathf.Atan2 (direction.x, direction.y) * Mathf.Rad2Deg, 0f));


		// test if location is occuplied
		if (player) {
			if (EnemyAtPosition (newTile)) {
				if (weapon != null) {
					weapon.Attack (direction, new Vector2 (transform.position.x, transform.position.z));
				}
				MoveToPos (transform.position, targetRot); // TODO Replace with weapon effect
				return;
			}
		} else {
			if (PlayerAtPosition (newTile)) {
				if (weapon != null) {
					weapon.Attack (direction, new Vector2 (transform.position.x, transform.position.z));
					transform.eulerAngles = new Vector3 (0f, Mathf.Atan2 (direction.x, direction.y) * Mathf.Rad2Deg, 0f);
				}
				MoveToPos (transform.position, targetRot); // TODO Replace with weapon effect
				return;
			}
		}

		if (!tm.GetTileAtPosition(newTile)) {
			Vector3 targetPos = new Vector3 (transform.position.x + direction.x, 0f, transform.position.z + direction.y);

			MoveToPos (targetPos, targetRot);

			tm.CreateBus (targetPos);

			// update location
			if (player && location != null) {
				location.PlayerExitIsland ();
			}
			location = null;
		} else {
			float newTileHeight = tm.GetTileAtPosition(newTile).transform.position.y;
			Vector3 targetPos = new Vector3 (transform.position.x + direction.x, newTileHeight, transform.position.z + direction.y);

			MoveToPos (targetPos, targetRot);

			// update location
			location = tm.tiles [newTile].island;
			if (player) {
				location.PlayerEnterIsland ();
			}
		}
	}

	void MoveToPos (Vector3 targetPos, Quaternion targetRot) {
		StartCoroutine (MoveToPosition (targetPos, targetRot, moveTime));
		Vector3 oldPos = transform.position;
		transform.position = targetPos;
		model.position = oldPos;
	}

	void CompleteAction () {
		Vector2 newTile = new Vector2 (transform.position.x, transform.position.z);
		if (player) {
			if (tm.GetResourceAtPosition (newTile) != null) {
				playerScript.CollectResource (tm.GetResourceAtPosition (newTile));
			}
		}

		TurnEnd ();
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

	public bool EnemyAtPosition (Vector2 position) {
		RaycastHit hit;
		if (Physics.Raycast (new Vector3 (position.x, 5f, position.y), Vector3.down, out hit, 5f)) {
			if (hit.collider.gameObject.layer == 8 && hit.collider.gameObject.tag == "Enemy") {
				return true;
			} else {
				return false;
			}
		} else {
			return false;
		}
	}

	public bool PlayerAtPosition (Vector2 position) {
		Vector3 playerPosition = GameObject.Find("Player").transform.position;
		if (new Vector2 (playerPosition.x, playerPosition.z) == position) {
			return true;
		} else {
			return false;
		}
	}

	IEnumerator MoveToPosition (Vector3 targetPos, Quaternion targetRot, float time)
	{
		float elapsedTime = 0;
		Vector3 startingPos = model.transform.position;
		Quaternion startingRot = model.transform.rotation;

		while (elapsedTime < time)
		{
			model.position = Vector3.Lerp(startingPos, targetPos, (elapsedTime / time));
			model.rotation = Quaternion.Lerp(startingRot, targetRot, (elapsedTime / time));
			elapsedTime += Time.deltaTime;
			yield return null;
		}
		model.position = targetPos;
		model.rotation = targetRot;
		CompleteAction ();
	}
}
