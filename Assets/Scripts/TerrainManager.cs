using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TerrainManager : MonoBehaviour {
	public static TerrainManager instance;
	public GameObject playerPrefab;

	[Header("Set Spawns")]
	public List<SetSpawn> setSpawns;

	[Space(10)]
	[Header("Islands")]
	public GameObject islandPrefab;
	public int count;
	public float spacing;
	public int mapSize;

	[HideInInspector]
	public Island[] islands;

	[Space(10)]
	[Header("Teirs")]
	public float teirDstIntervals;
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
	}

	public void GenerateIslands () {
//		count = Mathf.RoundToInt ((mapSize * mapSize) / islandSize);
		islands = new Island[count];
		List<TileGroup> tileGroups = GetTileGroups ();

		int sqrtSize = Mathf.CeilToInt (Mathf.Sqrt (tileGroups.Count));
		float[] bottomOffsets = new float[sqrtSize];
		float[,] heightOffsets = new float[sqrtSize, sqrtSize];

		float additiveWidthOffset = 0f;
		for (int x = 0; x < sqrtSize; x++) {
			float maxWidth = tileGroups [x].GetDimensions ().x;
			for (int y = 0; y < sqrtSize; y++) {
				int groupIndex = (y * sqrtSize) + x;
				float offset = tileGroups [groupIndex].GetDimensions ().x;
//				print (tileGroups [groupIndex].minX + " | " + tileGroups [groupIndex].maxX + " = " + offset);

				if (offset > maxWidth) {
					maxWidth = offset;
				}
			}
			bottomOffsets [x] = additiveWidthOffset;

			additiveWidthOffset += maxWidth + spacing;
		}

		for (int x = 0; x < sqrtSize; x++) {
			float additiveHeightOffset = 0f;
			for (int y = 0; y < sqrtSize; y++) {
				int groupIndex = (y * sqrtSize) + x;

				float heightOffset = tileGroups [groupIndex].GetDimensions ().y;

				heightOffsets [x, y] = additiveHeightOffset;
					
				additiveHeightOffset += heightOffset + spacing;
			}
		}

		for (int i = 0; i < tileGroups.Count; i++) {
			List<Vector2> tilePositions = tileGroups[i].tilePositions;

			Vector3 pos = new Vector3 (tilePositions[0].x + bottomOffsets[i % sqrtSize], 0f, tilePositions[0].y + heightOffsets[i % sqrtSize, Mathf.FloorToInt(i / sqrtSize)]);
			Vector3 targetPos = new Vector3 (tileGroups[i].tilePositions[0].x, 0f, tileGroups[i].tilePositions[0].y);

			List<Vector2> centeredTilePositions = new List<Vector2> ();
			foreach (Vector2 position in tilePositions) {
				centeredTilePositions.Add (position - new Vector2(targetPos.x, targetPos.z));
			}

			GameObject island = (GameObject)Instantiate (islandPrefab, transform);
			island.transform.position = pos;
			island.name = "Island";

			Island islandScript = island.GetComponent<Island> ();
			islandScript.diffuculty = Mathf.RoundToInt (i % sqrtSize + Mathf.FloorToInt(i / sqrtSize));
			islands [i] = islandScript;

			islandScript.InitIsland (centeredTilePositions, targetPos);
		}
	}

	public void SpawnPlayer() {
		GameObject newPlayer = (GameObject) Instantiate (playerPrefab);
		newPlayer.name = "Player";
	}

	public Building SpawnBuilding (Vector3 position, GameObject prefab, BuildingInfo info, Island island) {
		GameObject building = (GameObject)Instantiate (prefab, position, Quaternion.identity, transform);

		Building buildingScript = building.GetComponent<Building> ();
		buildingScript.Init (info, island);
		buildingScript.CreateBlueprint ();

		return buildingScript;
	}

	public void BuildBuilding (Building building) {
		List<ResourcePickup> resourcesToConsume = new List<ResourcePickup> ();
		float baseHeight = GetTileAtPosition(building.coveredTiles[0]).transform.position.y;
		foreach (Vector2 pos in building.coveredTiles) {
			resourcesToConsume.Add(ResourcePickup.GetAtPosition(pos));
			GetTileAtPosition (pos).transform.position = new Vector3 (pos.x, baseHeight, pos.y);
		}
		ConsumeResources (resourcesToConsume);

		building.Build ();
		buildings.Add (building);

		Transform pad = building.transform.Find ("Pad");
		if(pad != null) {
			Vector2 pos2D = PosToV2(pad.position);
			pads.Add(pos2D, pad.position.y);
		}
	}

	public void BreakDownBuilding (Building building) {
		building.state = Building.BuildingState.Destroyed;

		Crafting.EditorRecipe recipe = building.info.recipe;
		Vector2 anchorPos = TerrainManager.PosToV2 (building.gameObject.transform.position) - building.info.anchorOffset;
		for (int y = 0; y < recipe.rows.Count; y++) {
			for (int x = 0; x < recipe.rows[y].columns.Count; x++) {
				TerrainManager.ResourceInfo resourceInfo = TerrainManager.instance.ResourceTypeToInfo (recipe.rows [y].columns [x].resourceType);

				for (int i = 0; i < recipe.rows [y].columns [x].count; i++) {
					TerrainManager.instance.SpawnResource (new Vector3 (anchorPos.x + x, 0, anchorPos.y + y), resourceInfo, building.island);
				}
			}
		}

		Destroy (building.gameObject);
	}

	public ResourcePickup SpawnResource (Vector3 position, ResourceInfo info, Island island, bool initialSpawn = false, float startingHeight = 0f) {
		Vector2 posV2 = PosToV2 (position);

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
		Vector2 posV2 = PosToV2(position);

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

	public AugmentPickup SpawnAugment (Vector3 position, AugmentInfo info, Island island) {
		Vector2 posV2 = PosToV2(position);

		if (island == null || GetBuildingAtPosition(posV2) != null || GetPickupAtPosition(posV2) != null) {
			return null;
		}

		if (!Crafting.instance.augmentInfos.Contains (info)) {
			Debug.LogError ("Augment info not found in list");
			return null;
		}

		GameObject augmentGO = Instantiate (info.pickupPrefab, island.transform);
		augmentGO.transform.position = position;
		AugmentPickup augment = new AugmentPickup (info, augmentGO, island);

		pickups.Add (posV2, augment);
		island.pickups.Add (augment);

		return augment;
	}

	public void ConsumeResources (List<ResourcePickup> consumedResources, List<int> amountsToConsume = null) {
		List<Pickup> objectsToPickup = new List<Pickup> ();
		foreach (ResourcePickup resource in consumedResources) {
			objectsToPickup.Add (resource.ToPickup ());
		}

		PickupObjects (objectsToPickup, amountsToConsume);
		crafting.TestForBlueprints ();
	}

	public void PickupWeapon (WeaponPickup weapon) {
		List<Pickup> objectsToPickup = new List<Pickup> { weapon.ToPickup() };

		PickupObjects (objectsToPickup);
	}

	public void PickupAugment (AugmentPickup augment) {
		List<Pickup> objectsToPickup = new List<Pickup> { augment.ToPickup() };

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
		crafting.TestForCrafting ();
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

	public void ClearTileAtPosition (Vector2 position) {
		tiles [position].ClearResourceType();
		GetTileAtPosition (position).GetComponent<Renderer> ().material.color = islands [0].civilizedColor;
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
			if (PosToV2(playerPosition) == positionsToTest[i]) {
				return true;
			}
		}

		return false;
	}

	public bool UnstandableBuildingAtPosition (Vector2 position) {
		RaycastHit hit;
		if (Physics.Raycast (new Vector3 (position.x, 5f, position.y), Vector3.down, out hit, 5f)) {
			if (hit.collider.gameObject.layer == 8 && hit.collider.gameObject.tag == "Building") {
				Building building = hit.collider.gameObject.GetComponent<Building>();
				if (!building.standable && building.isPhysical) {
					return true;
				}
			}
		}

		return false;
	}

	public GameObject GetBuildingAtPosition (Vector2 position) {
		RaycastHit[] allHits = Physics.RaycastAll (new Vector3 (position.x, 5f, position.y), Vector3.down, 5f);

		if (allHits.Length != 0) {
			foreach (RaycastHit hit in allHits) {
				if (hit.collider.gameObject.tag == "Building") {
					if (hit.collider.gameObject.GetComponent<Building> ().isPhysical) {
						return hit.collider.gameObject;
					}
				}
			}
		}

		return null;
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

	protected void RemoveBus (Bus bus) {
		buses.Remove (bus);
	}

	public void DestroyAllBuses() {
		for (int i = 0; i < buses.Count; i++) {
			buses [i].DestroyBus ();
		}
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

	public class Tile {
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

		public void ClearResourceType() {
			resourceType = ResourceInfo.ResourceType.None;
		}
	}

	public class Bus {
		int lifeTime;
		int turnsLeft;
		GameObject busTile;
		TerrainManager tm;
		Vector3 goalSize;

		bool isResizing;

		public Bus (int lifeTime, GameObject bus, TerrainManager tm) {
			this.lifeTime = lifeTime;
			busTile = bus;
			this.tm = tm;
			turnsLeft = (lifeTime + 1);
			goalSize = new Vector3(0.1f, 1f, 0.1f);
		}

		public void TurnEnd () {
			if (turnsLeft <= 0) {
				return;
			}

			turnsLeft -= 1;

			float timeRatio = (float)turnsLeft / (float)lifeTime;
			goalSize = new Vector3 (0.1f * timeRatio, 1f, 0.1f * timeRatio);

			if (!isResizing) {
				tm.StartCoroutine(ResizeBus ());
			}
		}

		public void DestroyBus () {
			turnsLeft = 0;
			goalSize = new Vector3 (0f, 1f, 0f);
			if (!isResizing) {
				tm.StartCoroutine(ResizeBus ());
			}
		}

		public IEnumerator ResizeBus () {
			isResizing = true;
			while (Vector3.Distance (busTile.transform.localScale, goalSize) > 0.01f) {
				busTile.transform.localScale = Vector3.Lerp (busTile.transform.localScale, goalSize, Time.deltaTime * 10f);
				yield return null;
			}

			busTile.transform.localScale = goalSize;

			if (turnsLeft <= 0) {
				Destroy (busTile);
				tm.RemoveBus (this);
			}

			isResizing = false;
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

	public static Vector2 PosToV2 (Vector3 position) {
		return new Vector2 (Mathf.Round(position.x), Mathf.Round(position.z));
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
		}
		return setSpawnsToUse;
	}

	List<TileGroup> tileGroups = new List<TileGroup> ();
	TileInfo[,] allTiles;

	private List<TileGroup> GetTileGroups () {

		// Make array of all tiles on map (assumign square map)
		allTiles = new TileInfo[mapSize, mapSize];

		for (int y = 0; y < mapSize; y++) {
			for (int x = 0; x < mapSize; x++) {
				allTiles [x, y] = new TileInfo (-1, new Vector2 (x, y));
			}
		}

		// Assign Group Anchors
		List<Vector2> groupAnchorPositions = new List<Vector2> ();
		int index = 0;
		int countSqr = Mathf.CeilToInt(Mathf.Sqrt((float)count));
		float _spacing = Mathf.RoundToInt (mapSize / countSqr);
		for (int y = 0; y < countSqr; y++) {
			for (int x = 0; x < countSqr; x++) {
				Vector2 position = new Vector2 (Mathf.RoundToInt(x * _spacing), Mathf.RoundToInt(y * _spacing));
				groupAnchorPositions.Add (position);

				index++; 
			}
		}

		// turn group anchors into tileGroups
		for (int i = 0; i < groupAnchorPositions.Count; i++) {
			List<Vector2> _unassignedList = new List<Vector2> ();
			_unassignedList.Add (groupAnchorPositions [i]);

			tileGroups.Add (new TileGroup (i, _unassignedList));
			allTiles [(int)groupAnchorPositions [i].x, (int)groupAnchorPositions [i].y].islandIndex = -1;
		}

		int placedTiles = 0;
			
		while (placedTiles < (mapSize * mapSize)) {
			for (int _i = 0; _i < tileGroups.Count; _i++) {
				if (tileGroups [_i].unassignedTilePositions.Count > 0) {
					int expansionIndex = Random.Range (0, tileGroups [_i].unassignedTilePositions.Count);
					Vector2 tilePosition = tileGroups [_i].unassignedTilePositions [expansionIndex];

					List<TileInfo> borderingTiles = GetBorderingTileGroups (tilePosition);

					tileGroups [_i].unassignedTilePositions.Remove (tilePosition);

					allTiles [(int)tilePosition.x, (int)tilePosition.y].islandIndex = _i;
					tileGroups [_i].tilePositions.Add (tilePosition);
					tileGroups [_i].UpdateMinMax (tilePosition);

					tileGroups [_i].tileCount++;
					placedTiles++;

					for (int r = 0; r < tileGroups.Count; r++) {
//					print ("R: " + r + ", I: " + i);
						if (r != _i) {
							while (tileGroups [r].unassignedTilePositions.Contains (tilePosition)) {
//								print ("Overlapping Tile: " + tileGroups [r].islandIndex);
								tileGroups [r].unassignedTilePositions.Remove (tilePosition);
							}
						}
					}

					string str = "Pos: " + tilePosition + ", Index: " + _i + ", Bordering Tiles: ";
					for (int i = 0; i < borderingTiles.Count; i++) {
						if (borderingTiles [i].islandIndex == -1) {
							if (!tileGroups [_i].unassignedTilePositions.Contains (borderingTiles [i].position)) {
								tileGroups [_i].unassignedTilePositions.Add (borderingTiles [i].position);
								str += "Index: " + borderingTiles [i].islandIndex + ", Pos: " + borderingTiles [i].position + " | ";
							}
						}
						
					}
					str += " Total Unassigned: " + tileGroups [_i].unassignedTilePositions.Count;
//					print (str);
				}
			}
		}
//		string groupString = "";
//
//		foreach (var tileGroup in tileGroups) {
//			groupString += " | Tile Group (" + tileGroup.islandIndex + "): ";
//			foreach (var tile in tileGroup.tilePositions) {
//				groupString += "(" + tile.x + ", " + tile.y + ")";
//			}
//				
//		}
//		print (groupString);
		return tileGroups;
	}

	private List<TileInfo> GetBorderingTileGroups (Vector2 index) {
		List<TileInfo> borderingTileGroups = new List<TileInfo> ();
		List<Vector2> allDirections = new List<Vector2> () { Vector2.right, Vector2.up, Vector2.left, Vector2.down };
		foreach (Vector2 direction in allDirections) {
			Vector2 posToCheck = index + direction;
			if (posToCheck.x >= 0 && posToCheck.x <= (mapSize - 1) && posToCheck.y >= 0 && posToCheck.y <= (mapSize - 1)) {
//				print (posToCheck);
				borderingTileGroups.Add (allTiles [(int)posToCheck.x, (int)posToCheck.y]);
			}
		}

		return borderingTileGroups;
	}

	public struct TileInfo {
		public int islandIndex;
		public Vector2 position;

		public TileInfo (int _islandIndex, Vector2 _tilePosition) {
			islandIndex = _islandIndex;
			position = _tilePosition;
		}
	}

	public class TileGroup {
		public int islandIndex;
		public int tileCount = 0;

		public float minX = Mathf.Infinity;
		public float maxX = -Mathf.Infinity;
		public float minY = Mathf.Infinity;
		public float maxY = -Mathf.Infinity;

		public List<Vector2> tilePositions = new List<Vector2>();
		public List<Vector2> unassignedTilePositions;

		public void UpdateMinMax (Vector2 newPos) {
			if (newPos.x > maxX) {
				maxX = newPos.x;
			}
			if (newPos.y > maxY) {
				maxY = newPos.y;
			}
			if (newPos.x < minX) {
				minX = newPos.x;
			}
			if (newPos.y < minY) {
				minY = newPos.y;
			}
		}

		public Vector2 GetDimensions () {
			float width = maxX - minX;
			float height = maxY - minY;
			return new Vector2 (width + 1f, height + 1f);
		}

		public TileGroup (int _islandIndex, List<Vector2> _unassignedTilePositions) {
			islandIndex = _islandIndex;
			unassignedTilePositions = _unassignedTilePositions;
		}
	}

}