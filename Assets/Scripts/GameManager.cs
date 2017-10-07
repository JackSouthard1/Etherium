using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {
	public static GameManager instance;

	public Body player;
	public List<Body> enemies = new List<Body>();
	private int enemiesMoving = 0;
	private TerrainManager tm;

	public bool transitioning = false;

	private Animator cutsceneBars;

	void Awake () {
		instance = this;
		tm = TerrainManager.instance;
		cutsceneBars = GameObject.Find ("CutsceneBars").GetComponent<Animator> ();
	}

	void Start () {
		player = Player.instance.gameObject.GetComponent<Body> ();

		GameObject[] enemiesGOArray = GameObject.FindGameObjectsWithTag ("Enemy");
		for (int i = 0; i < enemiesGOArray.Length; i++) {
			enemies.Add (enemiesGOArray [i].GetComponent<Body> ());
		}

		PlayerTurnStart ();
	}

	public void PlayerTurnEnd () {
		tm.TurnEnd ();

//		print ("Player Turn End");
		if (!transitioning) {
			EnemyTurnStart ();
		}
	}

	public void EnemyTurnDone () {
		enemiesMoving--;
		if (enemiesMoving <= 0) {
			PlayerTurnStart ();
//			print ("Enemy Turn End");
		}
	}

	private void PlayerTurnStart () {
		player.TurnStart ();
//		print ("Player Turn Start");
	}

	private void EnemyTurnStart () {
//		print ("Enemy Turn Start");
		if (enemies.Count > 0) {
			enemiesMoving = enemies.Count;
			for (int i = 0; i < enemies.Count; i++) {
				enemies [i].TurnStart ();
			}
		} else {
			enemiesMoving = 0;
			EnemyTurnDone ();
		}
	}

	public void EnemyDeath (Body enemy) {
		enemies.Remove (enemy);
	}

	public void CivilizeStart () {
		transitioning = true;
		cutsceneBars.SetBool ("Civilizing", true);
	}

	public void CivilizeEnd () {
		transitioning = false;
		cutsceneBars.SetBool ("Civilizing", false);
		PlayerTurnStart ();
	}
}
