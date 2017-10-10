using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WeaponInfo {
	public float damage;
	public int range;
}

public abstract class Weapon : MonoBehaviour {
	public WeaponInfo info;
	protected TerrainManager tm;
	protected Body body;

	void Start () {
		tm = TerrainManager.instance;
		body = GetComponentInParent<Body> ();
		ChildStart ();
	}

	public abstract void Attack (Vector2 direction, Vector2 anchor);

	public abstract void ChildStart ();
}
