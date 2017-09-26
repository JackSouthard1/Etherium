using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Weapon : MonoBehaviour {
	public float damage;
	protected TerrainManager tm;
	protected Body body;

	void Start () {
		tm = GameObject.Find ("Terrain").GetComponent<TerrainManager> ();
		body = GetComponentInParent<Body> ();
		ChildStart ();
	}

	public abstract void Attack (Vector2 direction, Vector2 anchor);

	public abstract void ChildStart ();
}
