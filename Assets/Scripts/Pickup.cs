using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pickup {
	public Vector2 position;
	public float originalBaseHeight;
	public List<GameObject> gameObjects = new List<GameObject> ();
	public Island island;

	public void UpdatePosition () {
		position = new Vector2 (gameObjects[0].transform.position.x, gameObjects[0].transform.position.z);
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

public class ResourcePickup : Pickup {
	public TerrainManager.ResourceInfo info;

	public ResourcePickup (TerrainManager.ResourceInfo _info, GameObject _resourceGO, Island _island) {
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
		return IsPickupAtPosition (position, typeof(ResourcePickup));
	}

	public static WeaponPickup GetAtPosition(Vector2 position) {
		return TerrainManager.instance.GetPickupAtPosition(position) as WeaponPickup;
	}
}