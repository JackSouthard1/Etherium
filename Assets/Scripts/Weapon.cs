using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Weapon : MonoBehaviour {
	public int infoIndex;
	protected TerrainManager tm;
	protected Body body;

	public WeaponInfo info {
		get { return WeaponInfo.GetInfoFromIndex (infoIndex); }
		set { infoIndex = value.ToIndex (); }
	}

	void Start () {
		tm = TerrainManager.instance;
		body = GetComponentInParent<Body> ();
		ChildStart ();
	}

	public abstract void Attack (Vector2 direction, Vector2 anchor);

	public virtual void UpdateWeapon () {
		if (transform.childCount > 0) {
			Destroy(transform.GetChild(0).gameObject);
		}

		if (infoIndex != 0) {
			Instantiate (info.weaponPrefab, transform);
		}
	}

	public abstract void ChildStart ();
}
