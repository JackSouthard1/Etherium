using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine;

[Serializable]
public class SavedGame {
	public static SavedGame data;

	public int seed;
	public List<int> civilizedIslandIndexes = new List<int>();
	public List<SavedBuilding> buildings = new List<SavedBuilding>();
	public List<SavedPickup> pickups = new List<SavedPickup>();
	public List<SavedResourceTile> resourceTiles = new List<SavedResourceTile>();
	public Dictionary<int, List<SavedEnemy>> savedEnemyLists = new Dictionary<int, List<SavedEnemy>>();
	public List<SavedTilePos> revealedTiles = new List<SavedTilePos>();

	public SavedTilePos playerPosition;
	public int weaponIndex;
	public int augmentIndex;
	public float playerHealth;
	public List<float> inventory = new List<float>();

	//for quick searching
	static Dictionary<Vector2, SavedBuilding> buildingsByPos = new Dictionary<Vector2, SavedBuilding>();
	static Dictionary<Vector2, SavedResourceTile> resourceTilesByPos = new Dictionary<Vector2, SavedResourceTile> ();

	public static void Init(int _seed) {
		data = new SavedGame ();
		data.seed = _seed;

		GameManager.instance.SaveThisTurn ();
	}

	public static void AddCivilizedIsland(int newIslandIndex) {
		data.civilizedIslandIndexes.Add (newIslandIndex);

		if (data.savedEnemyLists.ContainsKey (newIslandIndex)) {
			data.savedEnemyLists.Remove (newIslandIndex);
		}

		GameManager.instance.SaveThisTurn ();
	}

	public static void UpdateBuildings() {
		List<Building> allBuildings = TerrainManager.instance.buildings;
		data.buildings = new List<SavedBuilding> ();
		buildingsByPos.Clear ();
		foreach (Building building in allBuildings) {
			SavedBuilding savedBuilding = new SavedBuilding (building);
			data.buildings.Add(savedBuilding);
			buildingsByPos.Add (TerrainManager.PosToV2 (building.transform.position), savedBuilding);
		}

		//builds happen between turns
		GameManager.instance.SaveGame ();
	}

	public static void UpdateBuildingSupply(Building building) {
		Vector2 key = TerrainManager.PosToV2 (building.transform.position);
		SavedBuilding oldSavedBuilding = buildingsByPos [key];
		SavedBuilding newSavedBuilding = new SavedBuilding (building);

		data.buildings.Remove (oldSavedBuilding);
		data.buildings.Add (newSavedBuilding);
		buildingsByPos.Remove (key);
		buildingsByPos.Add (key, newSavedBuilding);

		GameManager.instance.SaveThisTurn ();
	}

	public static void UpdatePickups() {
		List<Pickup> allPickups = TerrainManager.instance.pickups.Values.ToList ();
		data.pickups = new List<SavedPickup> ();
		foreach (Pickup pickup in allPickups) {
			data.pickups.Add(new SavedPickup(pickup));
		}

		GameManager.instance.SaveThisTurn ();
	}
		
	public static void AddResourceTile(TerrainManager.Tile tile) {
		SavedResourceTile newSavedTile = new SavedResourceTile (tile);
		data.resourceTiles.Add (newSavedTile);
		resourceTilesByPos.Add (newSavedTile.tilePos.ToVector2 (), newSavedTile);
	}

	public static void RemoveResourceTile(TerrainManager.Tile tile) {
		Vector2 key = TerrainManager.PosToV2 (tile.tile.transform.position);
		SavedResourceTile oldTile = resourceTilesByPos [key];

		data.resourceTiles.Remove (oldTile);
		resourceTilesByPos.Remove (key);
	}

	public static void UpdatePlayerInfo() {
		data.playerPosition = new SavedTilePos (TerrainManager.PosToV2 (Player.instance.body.transform.position));
		data.weaponIndex = Player.instance.body.weapon.infoIndex;
		data.augmentIndex = Player.instance.body.augment.ToIndex();
		data.inventory = Player.instance.inventory;
		data.playerHealth = Player.instance.body.health;

		GameManager.instance.SaveThisTurn ();
	}

	public static void UpdateEnemyList(int islandIndex, List<Body> enemies) {
		if (data.savedEnemyLists.ContainsKey (islandIndex)) {
			data.savedEnemyLists.Remove (islandIndex);
		}

		List<SavedEnemy> newSavedEnemies = new List<SavedEnemy> ();
		foreach (Body enemy in enemies) {
			newSavedEnemies.Add (new SavedEnemy (enemy));
		}

		data.savedEnemyLists.Add (islandIndex, newSavedEnemies);

		GameManager.instance.SaveThisTurn ();
	}

	public static void UpdateRevealTiles(List<Vector2> newTiles) {
		data.revealedTiles.Clear ();
		foreach (Vector2 tile in newTiles) {
			data.revealedTiles.Add (new SavedTilePos (tile));
		}

		GameManager.instance.SaveThisTurn ();
	}

	public static string Serialize() {
		var b = new BinaryFormatter();
		var m = new MemoryStream();

		b.Serialize(m,data);

		return Convert.ToBase64String (m.GetBuffer ());
	}

	public static void LoadSavedGame() {
		var savedData = PlayerPrefs.GetString("SavedGame");

		if (!string.IsNullOrEmpty (savedData)) {
			var b = new BinaryFormatter ();
			var m = new MemoryStream (Convert.FromBase64String (savedData));

			data = (SavedGame)b.Deserialize (m);

			foreach (SavedBuilding building in data.buildings) {
				buildingsByPos.Add (building.tilePos.ToVector2(), building);
			}
			foreach (SavedResourceTile resourceTile in data.resourceTiles) {
				resourceTilesByPos.Add (resourceTile.tilePos.ToVector2 (), resourceTile);
			}
		} else {
			data = new SavedGame ();
		}
	}

	[Serializable]
	public struct SavedPickup {
		public SavedTilePos tilePos;
		public int pickupType;
		public int index;
		public int stackHeight;

		public SavedPickup (Pickup pickup) {
			tilePos = new SavedTilePos(pickup.position);
			if (pickup is ResourcePickup) {
				ResourcePickup resourcePickup = pickup as ResourcePickup;
				pickupType = 0;
				index = resourcePickup.info.ToIndex ();
				stackHeight = pickup.gameObjects.Count;
			} else if (pickup is WeaponPickup) {
				WeaponPickup weaponPickup = pickup as WeaponPickup;
				pickupType = 1;
				index = weaponPickup.info.ToIndex ();
				stackHeight = 1;
			} else if (pickup is AugmentPickup) {
				AugmentPickup augmentPickup = pickup as AugmentPickup;
				pickupType = 2;
				index = augmentPickup.info.ToIndex ();
				stackHeight = 1;
			} else {
				Debug.LogError ("Unrecognized pickup type");
				pickupType = -1;
				index = -1;
				stackHeight = -1;
			}
		}

		public void Spawn() {
			GameObject tileAtPos = TerrainManager.instance.GetTileAtPosition (tilePos.ToVector2 ());
			Vector3 spawnPos = new Vector3 (tilePos.x, tileAtPos.transform.position.y, tilePos.y);
			Island island = tileAtPos.transform.parent.GetComponent<Island>();

			for (int i = 0; i < stackHeight; i++) {
				switch (pickupType) {
				case 0:
					TerrainManager.instance.SpawnResource (spawnPos, ResourceInfo.GetInfoFromIndex (index), island);
					break;
				case 1:
					TerrainManager.instance.SpawnWeapon (spawnPos, WeaponInfo.GetInfoFromIndex (index), island);
					break;
				case 2:
					TerrainManager.instance.SpawnAugment (spawnPos, AugmentInfo.GetInfoFromIndex (index), island);
					break;
				default:
					Debug.LogError ("Unrecognized pickup type");
					break;
				}
			}
		}
	}

	[Serializable]
	public struct SavedBuilding {
		public SavedTilePos tilePos;
		public int index;
		public int supply;

		public SavedBuilding (Building building) {
			tilePos = new SavedTilePos(TerrainManager.PosToV2 (building.transform.position));
			index = Crafting.instance.buildingInfos.IndexOf (building.info);
			supply = building.supply;
			//TODO: to prevent trolls we'll probably also need to store the turns before the next spawn but for now it's low priority
		}

		public void Spawn() {
			BuildingInfo info = Crafting.instance.buildingInfos [index];

			GameObject tileAtPos = TerrainManager.instance.GetTileAtPosition (tilePos.ToVector2 () - info.anchorOffset);
			Vector3 spawnPos = new Vector3 (tilePos.x, tileAtPos.transform.position.y, tilePos.y);
			Island island = tileAtPos.transform.parent.GetComponent<Island>();

			Building newBuilding = TerrainManager.instance.SpawnBuilding (spawnPos, info.prefab, info, island, true);
			newBuilding.supply = supply;
		}
	}

	[Serializable]
	public struct SavedResourceTile {
		public SavedTilePos tilePos;
		public int resourceIndex;

		public SavedResourceTile (TerrainManager.Tile tile) {
			tilePos = new SavedTilePos(TerrainManager.PosToV2(tile.tile.transform.position));
			resourceIndex = ResourceInfo.GetIndexFromType(tile.resourceType);
		}

		public void Spawn() {
			ResourceInfo info = ResourceInfo.GetInfoFromIndex(resourceIndex);
			TerrainManager.Tile tile = TerrainManager.instance.tiles [tilePos.ToVector2 ()];

			tile.resourceType = info.type;
			tile.tile.GetComponent<Renderer> ().material.color = info.colorLight;
		}
	}

	[Serializable]
	public struct SavedEnemy {
		public SavedTilePos tilePos;
		public int index;
		public float health;

		public SavedEnemy (Body enemyBody) {
			tilePos = new SavedTilePos(TerrainManager.PosToV2(enemyBody.gameObject.transform.position));
			index = enemyBody.id;
			health = enemyBody.health;
		}

		public Body Spawn() {
			GameObject newEnemy = (GameObject) GameObject.Instantiate (TerrainManager.instance.enemyPrefabs[index], new Vector3(tilePos.x, 0f, tilePos.y), Quaternion.identity);
			Body body = newEnemy.GetComponent<Body> ();
			body.id = index;
			body.health = health;

			return body;
		}
	}

	[Serializable]
	public struct SavedTilePos {
		public int x;
		public int y;

		public SavedTilePos(Vector2 pos) {
			x = Mathf.RoundToInt(pos.x);
			y = Mathf.RoundToInt(pos.y);
		}

		public Vector2 ToVector2() {
			return new Vector2 (x, y);
		}
	}
}