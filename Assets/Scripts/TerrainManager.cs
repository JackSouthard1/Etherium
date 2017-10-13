using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TerrainManager : MonoBehaviour {
	public static TerrainManager instance;
	[Header("Set Spawns")]
	public List<SetSpawn> setSpawns;

	[Space(10)]
	[Header("Islands")]
	public GameObject islandPrefab;
	public int count;
	public int spacing;

	[HideInInspector]
	public Island[] islands;

	[Space(10)]
	[Header("Teirs")]
	public Teir[] teirs;

	[Space(10)]
	[Header("Resources")]
	public ResourceInfo[] resourceInfos;
	public GameObject resourcePrefab;
	public float stackHeight;

	[Header("Enemies")]
	public GameObject[] enemyPrefabs;

	[Space(10)]
	[Header("Buses")]
	public GameObject busPrefab;
	public int lifeTime;

	[HideInInspector]
	public List<Bus> buses = new List<Bus>();

	[HideInInspector]
	public Dictionary<Vector2, Tile> tiles = new Dictionary<Vector2, Tile> ();

	[HideInInspector]
	public Dictionary<Vector2, Pickup> pickups = new Dictionary<Vector2, Pickup> ();

	[HideInInspector]
	public Dictionary<Vector2, float> pads = new Dictionary<Vector2, float> ();

	private Crafting crafting;

	//TODO: I was gonna use a System.Action so we can all of these at once but eh
	List<Building> buildings = new List<Building>();

	public void TurnEnd () {
		for (int i = 0; i < buses.Count; i++) {
			buses [i].TurnEnd ();
		}
		for (int i = 0; i < islands.Length; i++) {
			islands [i].TestEnemyCount ();
		}
		for (int i = 0; i < buildings.Count; i++) {
			buildings [i].TurnEnd ();
		}
	}

	void Awake () {
		instance = this;
		crafting = GameObject.Find ("GameManager").GetComponent<Crafting> ();
		GenerateIslands ();
	}

	void GenerateIslands () {
		int countSqrt = (int)Mathf.Sqrt (count);
		islands = new Island[count];
		int index = 0;
		for (int z = 0; z < countSqrt; z++) {
			for (int x = 0; x < countSqrt; x++) {
				Vector3 pos = new Vector3 (x * spacing, 0f, z * spacing);

				GameObject island = (GameObject)Instantiate (islandPrefab, transform);
				island.transform.position = pos;
				island.name = "Island (" + x + "," + z + ")";

				Island islandScript = island.GetComponent<Island> ();
				islandScript.index = new Vector2(x, z);
				islands [index] = islandScript;
				islandScript.InitIsland ();
				

				index++;
			}
		}
	}

	public void SpawnBuilding (Vector3 position, GameObject prefab, Color mainColor, Color secondaryColor, Color alternateColor, Island island) {
		GameObject building = (GameObject)Instantiate (prefab, position, Quaternion.identity, transform);
		MeshRenderer[] mrs = building.transform.Find("Model").GetComponentsInChildren<MeshRenderer> ();
		for (int i = 0; i < mrs.Length; i++) {
			if (mrs [i].gameObject.name.Contains ("(P)")) {
				mrs [i].material.color = mainColor;
			} else if (mrs [i].gameObject.name.Contains ("(S)")) {
				mrs [i].material.color = secondaryColor;
			} else if (mrs [i].gameObject.name.Contains ("(A)")) {
				mrs [i].material.color = alternateColor;
			}
		}

		Building buildingScript = building.GetComponent<Building> ();
		buildingScript.Init (island);
		buildings.Add (buildingScript);

		Transform pad = building.transform.Find ("Pad");
		if(pad != null) {
			Vector2 pos2D = new Vector2 (Mathf.RoundToInt(pad.position.x), Mathf.RoundToInt(pad.position.z));
			pads.Add(pos2D, pad.position.y);
		}
	}

	public ResourcePickup SpawnResource (Vector3 position, ResourceInfo info, Island island, bool initialSpawn = false, float startingHeight = 0f) {
		Vector2 posV2 = new Vector2 (position.x, position.z);

		if (island == null || GetBuildingAtPosition(posV2) != null || WeaponPickup.IsAtPosition(posV2)) {
			return null;
		}

		if (ResourcePickup.IsAtPosition(posV2)) {
			ResourcePickup curResource = ResourcePickup.GetAtPosition(posV2);
			if (curResource.info.type == info.type) {
				GameObject resourceGO = Instantiate (resourcePrefab, island.transform);
				resourceGO.transform.position = position;
				resourceGO.gameObject.GetComponentInChildren<SpriteRenderer> ().sprite = info.sprite;
				resourceGO.gameObject.GetComponentInChildren<SpriteRenderer> ().color = info.color;

				curResource.gameObjects.Add (resourceGO);
				resourceGO.transform.Translate (Vector3.up * (stackHeight * (curResource.gameObjects.Count - 1) + startingHeight));

				if (!initialSpawn) {
					UpdateResources ();
				}
				return curResource;
			} else {
				return null;
			}
		} else {
			GameObject resourceGO = Instantiate (resourcePrefab, island.transform);
			resourceGO.transform.position = position;
			ResourcePickup resource = new ResourcePickup (info, resourceGO, island);

			resourceGO.GetComponentInChildren<SpriteRenderer> ().sprite = info.sprite;
			resourceGO.gameObject.GetComponentInChildren<SpriteRenderer> ().color = info.color;
			pickups.Add (posV2, resource);
			island.pickups.Add (resource);

			resourceGO.transform.Translate (Vector3.up * startingHeight);
			if (!initialSpawn) {
				UpdateResources ();
			}
			return resource;
		}
	}

	public WeaponPickup SpawnWeapon (Vector3 position, WeaponInfo info, Island island) {
		Vector2 posV2 = new Vector2 (position.x, position.z);

		if (island == null || GetBuildingAtPosition(posV2) != null || GetPickupAtPosition(posV2) != null) {
			return null;
		}

		if (!Crafting.instance.weaponInfos.Contains (info)) {
			Debug.LogError ("Weapon info not found in list");
			return null;
		}
		
		GameObject weaponGO = Instantiate (info.pickupPrefab, island.transform);
		weaponGO.transform.position = position;
		WeaponPickup weapon = new WeaponPickup (info, weaponGO, island);

		pickups.Add (posV2, weapon);
		island.pickups.Add (weapon);
		
		return weapon;
	}

	public void ConsumeResources (List<ResourcePickup> consumedResources, List<int> amountsToConsume = null) {
		List<Pickup> objectsToPickup = new List<Pickup> ();
		foreach (ResourcePickup resource in consumedResources) {
			objectsToPickup.Add (resource.ToPickup ());
		}

		PickupObjects (objectsToPickup, amountsToConsume);
	}

	public void PickupWeapon (WeaponPickup weapon) {
		List<Pickup> objectsToPickup = new List<Pickup> { weapon.ToPickup() };

		PickupObjects (objectsToPickup);
	}

	void PickupObjects (List<Pickup> pickupsToConsume, List<int> amountsToConsume = null) {
		List<Vector2> keysToRemove = new List<Vector2> ();

		for (int k = 0; k < pickupsToConsume.Count; k++) {
			int amountToConsume = (amountsToConsume != null) ? amountsToConsume [k] : pickupsToConsume [k].gameObjects.Count;
			bool consumesFullStack = amountToConsume == pickupsToConsume [k].gameObjects.Count;

			if (consumesFullStack) {
				pickupsToConsume [k].island.pickups.Remove (pickupsToConsume [k]);
			}

			List<GameObject> destroying = new List<GameObject> ();
			int max = (pickupsToConsume [k].gameObjects.Count - 1);
			int min = (pickupsToConsume [k].gameObjects.Count - amountToConsume);
			for (int i = max; i >= min; i--) {
				destroying.Add (pickupsToConsume[k].gameObjects [i]);
				pickupsToConsume [k].gameObjects.RemoveAt (i);
			}

			for (int i = 0; i < destroying.Count; i++) {
				Destroy (destroying [i]);
			}

			if(consumesFullStack)
				keysToRemove.Add (pickupsToConsume[k].position);
		}

		foreach (Vector2 pickupKey in pickups.Keys.ToList()) {
			if (keysToRemove.Contains (pickupKey)) {
				pickups.Remove (pickupKey);
			}
		}
	}

	void UpdateResources () {
		foreach (Vector2 key in pickups.Keys.ToList()) {
			if (ResourcePickup.IsAtPosition(key)) {
				ResourcePickup resourceToTest = pickups [key] as ResourcePickup;
				crafting.TestForCrafting (resourceToTest);
			}
		}
	}

	public GameObject GetTileAtPosition (Vector2 position) {
		if (tiles.ContainsKey (position)) {
			return tiles [position].tile;
		} else {
			return null;
		}
	}

	public Pickup GetPickupAtPosition (Vector2 position) {
		if (pickups.ContainsKey (position)) {
			return pickups [position];
		} else {
			return null;
		}
	}

	public bool EnemyInRange (Vector2 origin, Vector2 direction, int range) {
		List<Vector2> positionsToTest = new List<Vector2> ();
		for (int i = 1; i <= range; i++) {
			positionsToTest.Add (origin + (direction * i));
		}

		for (int i = 0; i < positionsToTest.Count; i++) {
			RaycastHit hit;
			if (Physics.Raycast (new Vector3 (positionsToTest[i].x, 2f, positionsToTest[i].y), Vector3.down, out hit, 2f)) {
				if (hit.collider.gameObject.tag == "Enemy") {
					return true;
				}
			}
		}
		
		return false;
	}

	public bool PlayerInRange (Vector2 origin, Vector2 direction, int range) {
		Vector3 playerPosition = GameObject.Find("Player").transform.position;

		List<Vector2> positionsToTest = new List<Vector2> ();
		for (int i = 1; i <= range; i++) {
			positionsToTest.Add (origin + (direction * i));
		}

		for (int i = 0; i < positionsToTest.Count; i++) {
			if (new Vector2 (playerPosition.x, playerPosition.z) == positionsToTest[i]) {
				return true;
			}
		}

		return false;
	}

	public bool UnstandableBuildingAtPosition (Vector2 position) {
		RaycastHit hit;
		if (Physics.Raycast (new Vector3 (position.x, 5f, position.y), Vector3.down, out hit, 5f)) {
			if (hit.collider.gameObject.layer == 8 && hit.collider.gameObject.tag == "Building" && !hit.collider.gameObject.GetComponent<Building>().standable) {
				return true;
			} else {
				return false;
			}
		} else {
			return false;
		}
	}

	public GameObject GetBuildingAtPosition (Vector2 position) {
		RaycastHit[] allHits = Physics.RaycastAll (new Vector3 (position.x, 5f, position.y), Vector3.down, 5f);

		if (allHits.Length != 0) {
			foreach (RaycastHit hit in allHits) {
				if (hit.collider.gameObject.tag == "Building") {
					return hit.collider.gameObject;
				}
			}

			return null;
		} else {
			return null;
		}
	}

	public float? PadAtPosition (Vector2 position) {
		if (pads.ContainsKey (position)) {
			return pads [position];
		} else {
			return null;
		}
	}

	public void CreateBus (Vector3 position) {
		GameObject bus = (GameObject)Instantiate (busPrefab, transform);
		bus.transform.position = position;
		buses.Add (new Bus (lifeTime, bus, GetComponent<TerrainManager>()));
	}

	protected void DestroyBus (Bus bus) {
		buses.Remove (bus);
	}

	[System.Serializable]
	public struct Layer {
		public Color color;
	}

	[System.Serializable]
	public struct ResourceInfo {
		public Sprite sprite;
		public Color color;
		public enum ResourceType
		{
			None,
			Yellow,
			Green,
			Purple,
			Blue
		};
		public ResourceType type;
	}

	public struct Tile {
		public GameObject tile;
		public ResourceInfo.ResourceType resourceType;
		public Island island;
		public Color originalColor;
		public float originalY;

		public Tile (GameObject _tile, ResourceInfo.ResourceType _resourceType, Island _island, Color _originalColor, float _originalY) {
			tile = _tile;
			resourceType = _resourceType;
			island = _island;
			originalColor = _originalColor;
			originalY = _originalY;
		}
	}

	public class Bus {
		int lifeTime;
		int timeLeft;
		GameObject busTile;
		TerrainManager tm;

		public Bus (int lifeTime, GameObject bus, TerrainManager tm) {
			this.lifeTime = lifeTime;
			busTile = bus;
			this.tm = tm;
			timeLeft = lifeTime;
		}

		public void TurnEnd () {
			
			float timeRatio = (float)timeLeft / (float)lifeTime;
			busTile.transform.localScale = new Vector3 (0.1f * timeRatio, 1f, 0.1f * timeRatio);


			if (timeLeft <= 0) {
				Destroy (busTile);
				tm.DestroyBus (this);
			}

			timeLeft -= 1;

		}

	}

	[System.Serializable]
	public struct Teir {
		public Layer[] layers;
		public int[] resourceIndexes;
		public int enemyCount;
		public int[] enemyIDs;
	}

	//TODO: make these static
	public ResourceInfo ResourceIndexToInfo(int index) {
		return resourceInfos[index];
	}

	//TODO: do we want to store the list as a dictionary here in the TerrainManager to possibly improve searching?
	public int ResourceTypeToIndex(ResourceInfo.ResourceType resourceType) {
		for (int i = 0; i < resourceInfos.Length; i++) {
			if (resourceInfos [i].type == resourceType)
				return i;
		}

		Debug.LogError ("Resource type " + resourceType.ToString () + " not found in list");
		return 0;
	}

	public ResourceInfo ResourceTypeToInfo(ResourceInfo.ResourceType resourceType) {
		for (int i = 0; i < resourceInfos.Length; i++) {
			if (resourceInfos [i].type == resourceType)
				return resourceInfos[i];
		}

		Debug.LogError ("Resource type " + resourceType.ToString () + " not found in list");
		return new ResourceInfo();
	}

	[System.Serializable]
	public struct SetSpawn {
		public int teir;
		public enum SpawnType {
			Enemy,
			Tile
		};
		public SpawnType type;
		public int spawnID;
	}

	public List<SetSpawn> GetSetSpawns (int teir) {
		List<SetSpawn> allSetSpawnsOfTeir = new List<SetSpawn> ();
		List<SetSpawn> setSpawnsToUse = new List<SetSpawn> ();
//		List<int> indexesToRemove = new List<int> ();

		for (int i = 0; i < setSpawns.Count; i++) {
			if (setSpawns[i].teir == teir) {
				allSetSpawnsOfTeir.Add (setSpawns[i]);
//				indexesToRemove.Add (i);
			}
		}

		if (allSetSpawnsOfTeir.Count > 0) {
			int islandsInTeir = Mathf.RoundToInt(teir + 1f);
			int setSpawnNumberToGet = Mathf.CeilToInt((float)allSetSpawnsOfTeir.Count / (float)islandsInTeir);
			if (allSetSpawnsOfTeir.Count < setSpawnNumberToGet) {
				setSpawnNumberToGet = allSetSpawnsOfTeir.Count;
			}

			for (int i = 0; i < setSpawnNumberToGet; i++) {
				setSpawnsToUse.Add (allSetSpawnsOfTeir [i]);
				setSpawns.Remove (allSetSpawnsOfTeir [i]);
			}
//			indexesToRemove.RemoveRange (setSpawnNumberToGet, allSetSpawnsOfTeir.Count - 1);
//
//			foreach (var index in indexesToRemove) {
//				setSpawns.RemoveAt(index);
//			}
		}
		return setSpawnsToUse;
	}
}