﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {
	private Body player;
	private List<Body> enemies = new List<Body>();
	private int enemiesMoving = 0;
	private TerrainManager tm;

	void Start () {
		player = GameObject.Find ("Player").GetComponent<Body> ();
		tm = GameObject.Find ("Terrain").GetComponent<TerrainManager> ();

		GameObject[] enemiesGOArray = GameObject.FindGameObjectsWithTag ("Enemy");
		for (int i = 0; i < enemiesGOArray.Length; i++) {
			enemies.Add (enemiesGOArray [i].GetComponent<Body> ());
		}

		PlayerTurnStart ();
	}

	public void PlayerTurnEnd () {
		tm.TurnEnd ();
//		print ("Player Turn End");
		EnemyTurnStart ();
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
}
