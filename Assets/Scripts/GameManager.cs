using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {
	public static GameManager instance;

	public List<Body> enemies = new List<Body>();
	private int enemiesMoving = 0;
	private TerrainManager tm;

	public bool transitioning = false;

	private Animator cutsceneBars;

	Body player;

	void Awake () {
		instance = this;
		tm = TerrainManager.instance;
		cutsceneBars = GameObject.Find ("CutsceneBars").GetComponent<Animator> ();
		cutsceneBars.gameObject.SetActive (false);
	}

	public void StartGame () {
		cutsceneBars.gameObject.SetActive (true);

		tm.SpawnPlayer ();
		tm.GenerateIslands ();

		Player.instance.Init ();
		GameObject.FindObjectOfType<MapReveal> ().Init ();
		GameObject.FindObjectOfType<CameraController> ().Init ();

		Mind[] minds = GameObject.FindObjectsOfType<Mind> ();
		foreach (Mind mind in minds) {
			mind.Init ();
		}

		player = GameObject.Find ("Player").GetComponent<Body> ();

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
