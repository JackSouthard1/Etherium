﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Body : MonoBehaviour {
	[HideInInspector]
	public int id;

	[Header("Drops")]
	public ResourceInfo.ResourceType dropType;
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
	[HideInInspector]
	public Animator anim;

	void Awake () {
		maxHealth = health;

		weapon = GetComponentInChildren<Weapon> ();
		model = transform.Find ("Model");
	
		mind = GetComponentInChildren<Mind> ();
		if (mind.GetType () == typeof(PlayerMind)) {
			player = true;
			playerScript = Player.instance;
		}

		if (player) {
			anim = transform.Find ("Model").GetComponent<Animator> ();
		}
	}

	void Start () {
		tm = TerrainManager.instance;
		gm = GameManager.instance;
		mr = GameObject.FindObjectOfType<MapReveal> ();

		if (healthBar != null)
			healthBar.UpdateBar (health, maxHealth);

		if (augment.ToIndex() == -1) {
			augment = AugmentInfo.GetInfoFromIndex (0);
		}
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

	public void MoveToPos (Vector3 targetPos, Quaternion targetRot, bool instant = false) {
		if (!instant) {
			inAction = true;
			StartCoroutine (MoveToPosition (targetPos, moveTime));
			StartCoroutine (RotateToDir (targetRot, moveTime));
		} else {
			model.rotation = targetRot;
		}
		Vector3 oldPos = transform.position;
		transform.position = targetPos;
		model.position = (!instant) ? oldPos : targetPos;

		if (anim != null && TerrainManager.PosToV2(targetPos) != TerrainManager.PosToV2(oldPos) && !instant) {
			anim.SetTrigger ("Move");
		}
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
		if (player) {
			if (!mind.initializing) {
				playerScript.Eat ();
			}
		}

		if (mind.initializing) {
			mind.initializing = false;
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
		if (player && (health > 0f)) {
			GameManager.instance.SaveThisTurn ();
		}
	}

	public void Heal(float extraHealth) {
		ChangeHealth (extraHealth);
		if (player) {
			GameManager.instance.SaveGame ();
		}
	}

	public void ResetHealth() {
		health = maxHealth;

		if (healthBar != null) {
			healthBar.UpdateBar (health, maxHealth);
		}
	}

	void ChangeHealth (float amount) {
		health += amount;

		if (location != null) {
			location.SaveEnemies ();
		}

		if (amount < 0f) {
			if (health <= 0) {
				if (player) {
					StartCoroutine (playerScript.Respawn ());
				} else {
					for (int i = 0; i < dropCount; i++) {
						tm.SpawnResource (transform.position, ResourceInfo.GetInfoFromType (dropType), location);
					}
					location.EnemyDeath (GetComponent<Body> ());
					gm.EnemyDeath (GetComponent<Body> ());
					Destroy (gameObject);
				}
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
