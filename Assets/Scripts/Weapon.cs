using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WeaponInfo : Crafting.Craftable{
	public float damage;
	public int range;
	public bool passesThroughEnemies;

	public GameObject pickupPrefab;
	public GameObject weaponPrefab;

	[Header("Animation")]
	public int shotsPerAttack = 1;
	public float rateOfFirePerAttack = 0.5f;
	public float initialDelay = 0.25f;

	public static WeaponInfo GetInfoFromIndex(int index) {
		if (index < Crafting.instance.weaponInfos.Count && index >= 0) {
			return Crafting.instance.weaponInfos [index];
		} else {
			return null;
		}
	}

	public int ToIndex() {
		return Crafting.instance.weaponInfos.IndexOf (this);
	}
}

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
