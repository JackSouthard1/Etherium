using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Mind : MonoBehaviour {
	public Body body;
	protected GameManager gm;
	protected TerrainManager tm;
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
		gm = GameManager.instance;
		tm = TerrainManager.instance;

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
		Vector2 posV2 = TerrainManager.PosToV2 (transform.position);
		if (!tm.GetTileAtPosition (posV2)) {
			tm.CreateBus (transform.position);
		}

		body.Idle ();
	}

	void StartActionAttempt (Vector2 direction) {
		Vector2 newTile = new Vector2(transform.position.x + direction.x, transform.position.z + direction.y);
		Vector2 posV2 = TerrainManager.PosToV2 (transform.position);
		Quaternion targetRot = Quaternion.Euler(new Vector3 (0f, Mathf.Atan2 (direction.x, direction.y) * Mathf.Rad2Deg, 0f));

		// test to see if body is standing on tile
		bool canAttack = (tm.GetTileAtPosition (TerrainManager.PosToV2(transform.position)) && body.attacksLeft > 0);
		bool wantsToAttack = (isPlayer) ? tm.EnemyInRange(posV2, direction, body.weapon.info.range) : tm.PlayerInRange(posV2, direction, body.weapon.info.range);

		if (wantsToAttack && canAttack) {
			body.AttackInDir (targetRot, direction);
			return;
		} else if (wantsToAttack) {
			bool canMove = (isPlayer) ? tm.EnemyInRange(posV2, direction, 1) : tm.PlayerInRange(posV2, direction, 1);
			if (canMove) {
				EmptyAction ();
				return;
			}
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

			GameObject building = tm.GetBuildingAtPosition (newTile);
			if (building != null) {
				Building buildingScript = building.GetComponent<Building> ();
				if (buildingScript.state != Building.BuildingState.Blueprint) {
					newTileHeight += buildingScript.height;
				}
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
}
