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
	[HideInInspector]
	public Weapon weapon;
	private MapReveal mr;
	[HideInInspector]
	public bool player = false;
	[HideInInspector]
	public bool inAction = false;

	public int actionsPerTurn = 1;
	public int attacksPerTurn = 1;
	int actionsLeft;
	[HideInInspector]
	public int attacksLeft;

	[Space(10)]
	public float health;
	private float maxHealth;
	public bool canHeal { get { return health < maxHealth && mind.myTurn; } }

	[HideInInspector]
	public Island location = null;

	private float moveTime = 0.2f;
	[HideInInspector]
	public HealthBar healthBar;

	public Transform augmentParent;
	[HideInInspector]
	public AugmentInfo augment;

	void Awake () {
		weapon = GetComponentInChildren<Weapon> ();
		model = transform.Find ("Model");
	
		mind = GetComponentInChildren<Mind> ();
		if (mind.GetType () == typeof(PlayerMind)) {
			player = true;
			playerScript = Player.instance;
		}
	}

	void Start () {
		tm = TerrainManager.instance;
		gm = GameManager.instance;
		mr = GameObject.FindObjectOfType<MapReveal> ();

		maxHealth = health;
		if (healthBar != null)
			healthBar.UpdateBar (health, maxHealth);

		augment = AugmentInfo.GetInfoFromIndex (0);
	}

	public void TurnStart () {
		actionsLeft = actionsPerTurn;
		attacksLeft = attacksPerTurn;
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

	public void MoveToPos (Vector3 targetPos, Quaternion targetRot) {
		inAction = true;
		StartCoroutine (MoveToPosition (targetPos, moveTime));
		StartCoroutine (RotateToDir (targetRot, moveTime));
		Vector3 oldPos = transform.position;
		transform.position = targetPos;
		model.position = oldPos;
	}

	public void AttackInDir (Quaternion targetRot, Vector2 direction) {
		inAction = true;
		StartCoroutine (RotateToDir (targetRot, moveTime));
		weapon.Attack (direction, TerrainManager.PosToV2(transform.position));
		attacksLeft--;
	}

	public void Idle() {
		StartCoroutine (IdleTurn ());
	}

	public void CompleteAction () {
		Vector2 newTile = TerrainManager.PosToV2(transform.position);
		if (player) {
			if (ResourcePickup.IsAtPosition (newTile)) {
				playerScript.CollectResource (ResourcePickup.GetAtPosition (newTile));
			} else if (WeaponPickup.IsAtPosition (newTile)) {
				playerScript.PickupWeapon (WeaponPickup.GetAtPosition (newTile));
			} else if (AugmentPickup.IsAtPosition (newTile)) {
				playerScript.PickupAugment (AugmentPickup.GetAtPosition (newTile));
			}

			playerScript.Eat ();
		}

		actionsLeft--;
		inAction = false;

		if (actionsLeft > 0) {
			mind.TurnStart ();
		} else {
			TurnEnd ();
		}
	}

	public void TakeDamage(float damage) {
		ChangeHealth (-damage);
	}

	public void Heal(float extraHealth) {
		ChangeHealth (extraHealth);
	}

	void ChangeHealth (float amount) {
		health += amount;

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

		if (healthBar != null)
			healthBar.UpdateBar (health, maxHealth);
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

	IEnumerator IdleTurn() {
		inAction = true;
		actionsLeft = 0;
		attacksLeft = 0;
		
		yield return new WaitForSeconds (0.5f);

		CompleteAction ();
	}

	public void UpdateAugment(AugmentInfo newAugment) {
		if(augmentParent.childCount > 0) {
			Destroy(augmentParent.GetChild(0).gameObject);
		}
		
		augment = newAugment;

		if(newAugment.augmentPrefab != null) {
			Instantiate (newAugment.augmentPrefab, augmentParent);
		}
	}

//	[System.Serializable]
//	public struct DropInfo {
//		int count;
//		TerrainManager.ResourceInfo.ResourceType type;
//	}
}
