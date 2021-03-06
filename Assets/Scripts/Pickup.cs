﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pickup {
	public Vector2 position;
	public float originalBaseHeight;
	public List<GameObject> gameObjects = new List<GameObject> ();
	public Island island;

	public void UpdatePosition () {
		position = TerrainManager.PosToV2(gameObjects[0].transform.position);
		originalBaseHeight = gameObjects [0].transform.position.y;
	}

	public Pickup ToPickup() {
		Pickup newPickup = new Pickup ();
		newPickup.gameObjects = gameObjects;
		newPickup.island = island;
		if (gameObjects [0] != null) {
			newPickup.UpdatePosition ();
		}

		return newPickup;
	}

	protected static bool IsPickupAtPosition (Vector2 position, System.Type type) {
		Pickup pickupAtPosition = TerrainManager.instance.GetPickupAtPosition (position);
		if (pickupAtPosition != null) {
			return (type == pickupAtPosition.GetType ());
		} else {
			return false;
		}
	}
}

[System.Serializable]
public struct ResourceInfo {
	public Sprite sprite;
	public Color colorLight;
	public Color colorDark;
	public bool hasTile;
	public Color tileColor;
	public Mesh mesh;
	public enum ResourceType
	{
		None,
		Y1,
		Y2,
		Y3,
		G1,
		G2,
		G3,
		P1,
		P2,
		P3
	};
	public ResourceType type;

	public static ResourceInfo GetInfoFromIndex(int index) {
		return TerrainManager.instance.resourceInfos[index];
	}

	public static ResourceInfo GetInfoFromType(ResourceType type) {
		int index = GetIndexFromType (type);
		if (index != -1) {
			return GetInfoFromIndex (index);
		} else {
			return new ResourceInfo ();
		}
	}

	public static int GetIndexFromType(ResourceType type) {
		for (int i = 0; i < TerrainManager.instance.resourceInfos.Count; i++) {
//			Debug.Log (TerrainManager.instance.resourceInfos [i].type + "     " + type);
			if (TerrainManager.instance.resourceInfos [i].type == type)
				return i;
		}

		Debug.LogError ("Resource type " + type.ToString () + " not found in list");
		return -1;
	}

	public int ToIndex() {
		return TerrainManager.instance.resourceInfos.IndexOf(this);
	}
}

public class ResourcePickup : Pickup {
	public ResourceInfo info;

	public ResourcePickup (ResourceInfo _info, GameObject _resourceGO, Island _island) {
		info = _info;
		island = _island;
		gameObjects.Add(_resourceGO);
		if (_resourceGO != null) {
			UpdatePosition ();
		}
	}

	public static bool IsAtPosition(Vector2 position) {
		return IsPickupAtPosition (position, typeof(ResourcePickup));
	}

	public static ResourcePickup GetAtPosition(Vector2 position) {
		return TerrainManager.instance.GetPickupAtPosition(position) as ResourcePickup;
	}

	public void AnimateMove(Vector3 endPos, bool forwards, int count) {
		for (int i = 0; i < count; i++) {
			GameObject newResource = TerrainManager.CreateResource (info);
			ResourceLerp lerper = newResource.AddComponent<ResourceLerp> ();
			if (forwards) {
				lerper.Init (gameObjects[0].transform.position, endPos, i);
			} else {
				gameObjects [gameObjects.Count - i - 1].GetComponentInChildren<Renderer> ().enabled = false;
				lerper.cb = RevealResource;
				lerper.Init (endPos, gameObjects[0].transform.position, i);
			}
		}
	}

	public void RevealResource() {
		foreach (GameObject go in gameObjects) {
			Renderer rend = go.GetComponentInChildren<Renderer> ();
			if (!rend.enabled) {
				rend.enabled = true;
				break;
			}
		}
	}
}

[System.Serializable]
public class WeaponInfo : Craftable {
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

public class WeaponPickup : Pickup {
	public WeaponInfo info;

	public WeaponPickup (WeaponInfo _info, GameObject _weaponGO, Island _island) {
		info = _info;
		island = _island;
		gameObjects.Add (_weaponGO);
		if (_weaponGO != null) {
			UpdatePosition ();
		}
	}

	public static bool IsAtPosition(Vector2 position) {
		return IsPickupAtPosition (position, typeof(WeaponPickup));
	}

	public static WeaponPickup GetAtPosition(Vector2 position) {
		return TerrainManager.instance.GetPickupAtPosition(position) as WeaponPickup;
	}
}

[System.Serializable]
public class AugmentInfo : Craftable {
	public enum Slot
	{
		None,
		Top,
		Back,
		Side
	}

	public Slot type;

	//TODO: is this really the best system for specifying what each augment does?
	public enum BoostType
	{
		Health,
		Inventory,
		Vision,
		CriticalChance
	}
	public BoostType boost;
	public int boostValue;

	public GameObject pickupPrefab;
	public GameObject augmentPrefab;

	public static AugmentInfo GetInfoFromIndex(int index) {
		if (index < Crafting.instance.augmentInfos.Count && index >= 0) {
			return Crafting.instance.augmentInfos [index];
		} else {
			return null;
		}
	}

	public int ToIndex() {
		return Crafting.instance.augmentInfos.IndexOf (this);
	}
}

public class AugmentPickup : Pickup {
	public AugmentInfo info;

	public AugmentPickup (AugmentInfo _info, GameObject _augmentGO, Island _island) {
		info = _info;
		island = _island;
		gameObjects.Add (_augmentGO);
		if (_augmentGO != null) {
			UpdatePosition ();
		}
	}

	public static bool IsAtPosition(Vector2 position) {
		return IsPickupAtPosition (position, typeof(AugmentPickup));
	}

	public static AugmentPickup GetAtPosition(Vector2 position) {
		return TerrainManager.instance.GetPickupAtPosition(position) as AugmentPickup;
	}
}