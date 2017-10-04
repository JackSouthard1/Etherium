using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Body : MonoBehaviour {
	[Header("Drops")]
	public TerrainManager.ResourceInfo.ResourceType dropType;
	public int dropCount;

	[HideInInspector]
	public TerrainManager tm;
	private GameManager gm;
	private Transform model;
	private Mind mind;
	private Player playerScript;
	private Weapon weapon;
	private MapReveal mr;
	[HideInInspector]
	public bool player = false;
	[HideInInspector]
	public bool inAction = false;

	[Space(10)]
	public float health;
	private float maxHealth;
	public bool canHeal { get { return health < maxHealth; } }

	[HideInInspector]
	public Island location = null;

	private float moveTime = 0.2f;
	[HideInInspector]
	public HealthBar healthBar;

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
		mr = GetComponent<MapReveal> ();
		maxHealth = health;
		if (healthBar != null)
			healthBar.UpdateBar (health, maxHealth);

		StartActionAttempt (Vector2.zero);
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

	public void StartActionAttempt (Vector2 direction) {
		Vector2 newTile = new Vector2(transform.position.x + direction.x, transform.position.z + direction.y);
		Quaternion targetRot = Quaternion.Euler(new Vector3 (0f, Mathf.Atan2 (direction.x, direction.y) * Mathf.Rad2Deg, 0f));

		// test to see if body is standing on tile
		bool canAttack = false;
		bool wantsToAttack = false;
	
		if (player) {
			if (EnemyAtPosition (newTile)) {
				wantsToAttack = true;
			}
		} else {
			if (PlayerAtPosition (newTile)) {
				wantsToAttack = true;
			}
		}

		if (tm.GetTileAtPosition (new Vector2 (transform.position.x, transform.position.z))) {
			canAttack = true;
		}
//		print ("Wants to atk: " + wantsToAttack + " , Can Atk: " + canAttack);

		if (wantsToAttack && canAttack) {
			AttackInDir (targetRot, direction);
			return;
		} else if (wantsToAttack && !canAttack) {
			Idle ();
			return;
		}


		if (!tm.GetTileAtPosition(newTile)) {
			if (player) {
				Vector3 targetPos = new Vector3 (transform.position.x + direction.x, 0f, transform.position.z + direction.y);

				MoveToPos (targetPos, targetRot);

				tm.CreateBus (targetPos);

				// update location
				if (location != null) {
					location.PlayerExitIsland ();
				}
				location = null;
			} else {
				Idle ();
			}
		} else {
			if (UnstandableBuildingAtPosition (newTile)) {
				Idle ();
			} else {
				float newTileHeight = tm.GetTileAtPosition (newTile).transform.position.y;

				if (GetBuildingAtPosition (newTile)) {
					GameObject building = GetBuildingAtPosition (newTile);
					newTileHeight += building.GetComponent<Building> ().height;
				}
				Vector3 targetPos = new Vector3 (transform.position.x + direction.x, newTileHeight, transform.position.z + direction.y);

				MoveToPos (targetPos, targetRot);

				// update location
				location = tm.tiles [newTile].island;
				if (player) {
					location.PlayerEnterIsland ();
				}
			}
		}

		if (player && direction != Vector2.zero)
			playerScript.HandlePlayerMove ();
	}

	void Idle () {
		MoveToPos(transform.position, transform.rotation);
	}

	void MoveToPos (Vector3 targetPos, Quaternion targetRot) {
		inAction = true;
		StartCoroutine (MoveToPosition (targetPos, moveTime));
		StartCoroutine (RotateToDir (targetRot, moveTime));
		Vector3 oldPos = transform.position;
		transform.position = targetPos;
		model.position = oldPos;
	}

	void AttackInDir (Quaternion targetRot, Vector2 direction) {
		inAction = true;
		StartCoroutine (RotateToDir (targetRot, moveTime));
		weapon.Attack (direction, new Vector2 (transform.position.x, transform.position.z));
	}

	public void CompleteAction () {
		Vector2 newTile = new Vector2 (transform.position.x, transform.position.z);
		if (player) {
			if (tm.GetResourceAtPosition (newTile) != null) {
				playerScript.CollectResource (tm.GetResourceAtPosition (newTile));
			}
		}
		inAction = false;
		TurnEnd ();
	}

	public void TakeDamage(float damage) {
		ChangeHealth (-damage);
	}

	public void Heal(float extraHealth) {
		ChangeHealth (extraHealth);
	}

	void ChangeHealth (float amount) {
		health += amount;
		if (healthBar != null)
			healthBar.UpdateBar (health, maxHealth);

		if (amount < 0f) {
			if (health <= 0) {
				if (player) {
					print ("Player Dead");
				} else {
					for (int i = 0; i < dropCount; i++) {
						tm.SpawnResource (transform.position, tm.ResourceTypeToInfo (dropType), location);
					}
					location.EnemyDeath (GetComponent<Body> ());
					gm.EnemyDeath (GetComponent<Body> ());
				}
				Destroy (gameObject);
			}
		} else {
			if (health > maxHealth) {
				health = maxHealth;
			}
		}
	}

	public bool EnemyAtPosition (Vector2 position) {
		RaycastHit hit;
		if (Physics.Raycast (new Vector3 (position.x, 2f, position.y), Vector3.down, out hit, 2f)) {
			if (hit.collider.gameObject.tag == "Enemy") {
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

	public bool UnstandableBuildingAtPosition (Vector2 position) {
		RaycastHit hit;
		if (Physics.Raycast (new Vector3 (position.x, 5f, position.y), Vector3.down, out hit, 5f)) {
			if (hit.collider.gameObject.layer == 8 && hit.collider.gameObject.tag == "Building" && !hit.collider.gameObject.GetComponent<Building>().standable) {
				return true;
			} else {
				return false;
			}
		} else {
			return false;
		}
	}

	public GameObject GetBuildingAtPosition (Vector2 position) {
		RaycastHit hit;
		if (Physics.Raycast (new Vector3 (position.x, 5f, position.y), Vector3.down, out hit, 5f)) {
			if (hit.collider.gameObject.tag == "Building") {
				return hit.collider.gameObject;
			} else {
				return null;
			}
		} else {
			return null;
		}
	}

	IEnumerator MoveToPosition (Vector3 targetPos, float time)
	{
		float elapsedTime = 0;
		Vector3 startingPos = model.transform.position;

		while (elapsedTime < time)
		{
			model.position = Vector3.Lerp(startingPos, targetPos, (elapsedTime / time));
			elapsedTime += Time.deltaTime;
			yield return null;
		}
		model.position = targetPos;

		CompleteAction ();
	}

	IEnumerator RotateToDir (Quaternion targetRot, float time) {
		float elapsedTime = 0;
		Quaternion startingRot = model.transform.rotation;

		while (elapsedTime < time)
		{
			model.rotation = Quaternion.Lerp(startingRot, targetRot, (elapsedTime / time));
			elapsedTime += Time.deltaTime;
			yield return null;
		}
		model.rotation = targetRot;
	}

//	[System.Serializable]
//	public struct DropInfo {
//		int count;
//		TerrainManager.ResourceInfo.ResourceType type;
//	}
}
