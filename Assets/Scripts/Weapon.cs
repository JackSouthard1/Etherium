﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Weapon : MonoBehaviour {
	public float damage;
	protected TerrainManager tm;

	void Start () {
		tm = GameObject.Find ("Terrain").GetComponent<TerrainManager> ();
	}

	public abstract void Attack (Vector2 direction, Vector2 anchor);
}