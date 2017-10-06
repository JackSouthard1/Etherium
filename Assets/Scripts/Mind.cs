using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Mind : MonoBehaviour {
	public Body body;
	protected GameManager gm;
	private TerrainManager tm;
	public bool myTurn = false;
	public bool active = false;

	List<Vector2> allDirections {
		get {
			return new List<Vector2> () {
				Vector2.right,
				Vector2.up,
				Vector2.left,
				Vector2.down
			};
		}
	}

	//TODO: do we really want to generate this list every time
	public List<Vector2> GetAvailableDirections() {
		List<Vector2> availableDirections = new List<Vector2> (allDirections);
		for (int i = 0; i < allDirections.Count; i++) {
			if (!IsAvailableDirection(allDirections[i])) {
				availableDirections.Remove (allDirections[i]);
			}
		}

		return availableDirections;
	}

	bool isPlayer { get { return (this is PlayerMind); } }

	void Start () {
		body = GetComponentInParent<Body> ();
		gm = GameObject.Find ("GameManager").GetComponent<GameManager> ();
		tm = GameObject.FindObjectOfType<TerrainManager> ();

		StartActionAttempt (Vector2.zero);
		MindStart ();
	}

	protected abstract void MindStart ();

	protected abstract void EmptyAction ();

	protected abstract bool IsAvailableDirection (Vector2 direction);

	public virtual void TurnStart () {
		if (active) {
			myTurn = true;
			CalculateMove ();
		} else {
			myTurn = false;
			body.TurnEnd ();
		}
	}

	protected virtual void CalculateMove () {}

	protected void RelayAction (Vector2 direction) {
		if (!body.inAction) {
			List<Vector2> availableDirections = GetAvailableDirections ();

			if (availableDirections.Contains (direction)) {
				StartActionAttempt (direction);
			}
		}
	}

	protected void Idle () {
		myTurn = false;
		body.TurnEnd ();
	}

	void StartActionAttempt (Vector2 direction) {
		Vector2 newTile = new Vector2(transform.position.x + direction.x, transform.position.z + direction.y);
		Quaternion targetRot = Quaternion.Euler(new Vector3 (0f, Mathf.Atan2 (direction.x, direction.y) * Mathf.Rad2Deg, 0f));

		// test to see if body is standing on tile
		bool canAttack = false;
		bool wantsToAttack = false;

		if (isPlayer) {
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

		if (wantsToAttack && canAttack) {
			body.AttackInDir (targetRot, direction);
			return;
		} else if (wantsToAttack && !canAttack) {
			EmptyAction ();
			return;
		}

		Vector3 targetPos = new Vector3 (transform.position.x + direction.x, 0f, transform.position.z + direction.y);

		if (!tm.GetTileAtPosition(newTile)) {
			tm.CreateBus (targetPos);

			// update location
			if (body.location != null) {
				body.location.PlayerExitIsland (); 
				body.location = null;
			}
		} else {
			float newTileHeight = tm.GetTileAtPosition (newTile).transform.position.y;

			if (GetBuildingAtPosition (newTile)) {
				GameObject building = GetBuildingAtPosition (newTile);
				newTileHeight += building.GetComponent<Building> ().height;
			}

			targetPos += new Vector3 (0f, newTileHeight, 0f);

			// update location
			body.location = tm.tiles [newTile].island;
			if (isPlayer) {
				body.location.PlayerEnterIsland ();
			}
		}

		body.MoveToPos (targetPos, targetRot);
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
}
