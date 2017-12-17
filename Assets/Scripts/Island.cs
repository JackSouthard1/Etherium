using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Island : MonoBehaviour {
	int islandIndex;

	[Header("Teir")]
	private TerrainManager.Teir teirInfo;
	public int teir;

	[Space(10)]
	[Header("Tiles")]
	public GameObject tilePrefab;
	public Layer[] layers;
	public float heightPerLayer = 0.7f;

	[Space(10)]
	[Header("Resouces")]
	public int resourceTileCount;
	public int resourcesPerTile;

	public List<Pickup> pickups = new List<Pickup>();

	private GameObject enemyPrefab;

	[HideInInspector]
	public List<Body> enemies = new List<Body>();

	public bool buildable = false;

	[Space(10)]
	[Header("Civilizing")]
	public Color civilizedColor;
	public float timeToCivilize;
	private float civStartTime;
	private Vector3 targetPos;
	private Vector3 startPos;
	private bool civilizing = false;

	[Space(10)]
	[Header("Voids")]
	public int voidCount;
	public List<int> voidTileIndexes;

	[HideInInspector]
	public List<Vector2> usedTilePositions = new List<Vector2>();

	[HideInInspector]
	public List<TerrainManager.SetSpawn> setSpawns;

	public List<int> usedTiles = new List<int>();
	public TerrainManager.Tile[] tiles;
	private TerrainManager tm;
	private GameManager gm;
	public int diffuculty;

	void Awake () {
		tm = TerrainManager.instance;
		gm = GameManager.instance;
	}

	public void InitIsland(List<Vector2> _tilePositions, Vector3 civilizedPosition, int index, int _teir) {
		islandIndex = index;
		targetPos = civilizedPosition;
//		teir = Mathf.Clamp(Mathf.FloorToInt(transform.position.magnitude / tm.teirDstIntervals), 0, tm.teirs.Length - 1);
		if (_teir > (tm.teirs.Length - 1)) {
			_teir = tm.teirs.Length - 1;
		}
		this.teir = _teir;
		teirInfo = tm.teirs [teir];

		setSpawns = tm.GetSetSpawns (teir);
		tilePositions = _tilePositions;
		GenerateTiles ();
		bool alreadyCivilized = SavedGame.data.civilizedIslandIndexes.Contains (index);
		if (!alreadyCivilized) {
			SpawnEnemies ();
		} else {
			StartCivilizing ();
		}
	}
		
	public void PlayerEnterIsland () {
		for (int i = 0; i < enemies.Count; i++) {
			enemies [i].GetComponentInChildren<Mind> ().active = true;
		}

		tm.DestroyAllBuses ();
	}

	public void PlayerExitIsland () {
		for (int i = 0; i < enemies.Count; i++) {
			enemies [i].GetComponentInChildren<Mind> ().active = false;
		}
	}

	public void EnemyDeath (Body enemy) {
		enemies.Remove (enemy);
		SaveEnemies ();
	}

	public void TestEnemyCount () {
		if (!buildable) {
			if (enemies.Count <= 0) {
				StartCivilizing ();
			}
		}
	}

	List<Vector2> tilePositions;

	void GenerateTiles () {
		tiles = new TerrainManager.Tile[tilePositions.Count];

		// void tiles
		voidTileIndexes = new List<int> ();
		for (int i = 0; i < voidCount; i++) {
			voidTileIndexes.Add (GetAvaiableTileIndex (true));
		}
			
		// resource tiles
		List<int> tileSpawnIDs = new List<int> ();
		foreach (var setSpawn in setSpawns) {
			if (setSpawn.type == TerrainManager.SetSpawn.SpawnType.Tile) {
				tileSpawnIDs.Add (setSpawn.spawnID);
			}
		}
		int tilesLeft = resourceTileCount - tileSpawnIDs.Count;
		for (int i = 0; i < tilesLeft; i++) {
			tileSpawnIDs.Add(teirInfo.resourceIndexes[Random.Range(0, teirInfo.resourceIndexes.Length)]);
//			print ("Index: " + i + ", ID: " + tileSpawnIDs [i]);
		}

		List<int> resourceTileSpawnIndexs = new List<int> ();

		for (int i = 0; i < tileSpawnIDs.Count; i++) {
			resourceTileSpawnIndexs.Add (GetAvaiableTileIndex (false));
		}

		int index = 0;
		int resourceSpawnListIndex = 0;
		foreach (Vector2 tile in tilePositions) {
			if (!voidTileIndexes.Contains (index)) {
				int layer = Random.Range (0, teirInfo.layers.Length);
				float y = layer * heightPerLayer;
				Vector3 position = new Vector3 (tile.x, y, tile.y);
				GameObject tileGO = (GameObject)Instantiate (tilePrefab, transform);
				tileGO.transform.localPosition = position;
				tileGO.name = "Tile (" + tile.x + "," + tile.y + ")";

				ResourceInfo.ResourceType resourceInfoType;
				Color tileColor;

				if (resourceTileSpawnIndexs.Contains (index) && !GameManager.isLoadingFromSave) {
					int resourceIndex = tileSpawnIDs [resourceSpawnListIndex];
					resourceSpawnListIndex++;

					ResourceInfo resourceInfo = ResourceInfo.GetInfoFromIndex(resourceIndex);

					for (int i = 0; i < resourcesPerTile; i++) {
						tm.SpawnResource(position: transform.TransformPoint(position), info: resourceInfo, island: GetComponent<Island>(), initialSpawn: true);
					}

					if (resourceInfo.hasTile) {
						resourceInfoType = resourceInfo.type;
						tileColor = resourceInfo.tileColor;
					} else {
						resourceInfoType = ResourceInfo.ResourceType.None;
						tileColor = teirInfo.layers [layer].color;
					}
				} else {
					resourceInfoType = ResourceInfo.ResourceType.None;
					tileColor = teirInfo.layers [layer].color;
				}

				TerrainManager.Tile tileInfo = new TerrainManager.Tile (tileGO, resourceInfoType, GetComponent<Island> (), tileColor, y);
				tileGO.GetComponent<MeshRenderer> ().material.color = tileColor;
				tiles [index] = tileInfo;
				tm.tiles.Add (new Vector2 (tile.x + transform.position.x, tile.y + transform.position.z), tileInfo);

				//TODO: it's kinda dumb to have this if statement twice
				if (resourceTileSpawnIndexs.Contains (index) && tileInfo.resourceType != ResourceInfo.ResourceType.None && !GameManager.isLoadingFromSave) {
					SavedGame.AddResourceTile (tileInfo);
				}
			}

			index++;
		}
	}

	void SpawnEnemies () {
		if (!SavedGame.data.savedEnemyLists.ContainsKey (islandIndex)) {
			List<int> spawnIDs = new List<int> ();
			foreach (var setSpawn in setSpawns) {
				if (setSpawn.type == TerrainManager.SetSpawn.SpawnType.Enemy) {
					spawnIDs.Add (setSpawn.spawnID);
				}
			}
			int enemiesLeft = teirInfo.enemyCount - spawnIDs.Count;
			for (int i = 0; i < enemiesLeft; i++) {
				spawnIDs.Add (teirInfo.enemyIDs [Random.Range (0, teirInfo.enemyIDs.Length)]);
			}

			List<int> spawnTileIndexs = new List<int> ();

			for (int f = 0; f < spawnIDs.Count; f++) {
				spawnTileIndexs.Add (GetAvaiableTileIndex (true));
			}

			for (int i = 0; i < spawnIDs.Count; i++) {
				GameObject enemyPrefab = tm.enemyPrefabs [spawnIDs [i]];
				GameObject enemy = (GameObject)Instantiate (enemyPrefab, transform);
				enemy.transform.position = tiles [spawnTileIndexs [i]].tile.transform.position;

				Body enemyBody = enemy.GetComponent<Body> ();
				enemyBody.id = spawnIDs [i];
				enemies.Add (enemyBody);
			}
		} else {
			foreach (SavedGame.SavedEnemy enemy in SavedGame.data.savedEnemyLists[islandIndex]) {
				enemies.Add (enemy.Spawn ());
			}
		}
	}

	void StartCivilizing () {
		if (buildable) {
			return;
		}

		if (!GameManager.isLoadingFromSave) {
			gm.TransitionStart();
			civStartTime = Time.time;
			civilizing = true;
			startPos = transform.position;
			Transform player = GameObject.FindGameObjectWithTag ("Player").transform;
			player.parent = tm.GetTileAtPosition (TerrainManager.PosToV2 (player.position)).transform;
			player.Find ("Model").localPosition = Vector3.zero;
		}

		for (int i = 0; i < tiles.Length; i++) {
			if (!voidTileIndexes.Contains (i)) {
				GameObject tile = tiles [i].tile;
				tm.tiles.Remove (TerrainManager.PosToV2(tile.transform.position));

				if (!GameManager.isLoadingFromSave) {
					if (tiles [i].resourceType != ResourceInfo.ResourceType.None) {
						SavedGame.RemoveResourceTile (tiles [i]);
					}
				}
			}
		}

		if (!GameManager.isLoadingFromSave) {
			foreach (Pickup pickup in pickups) {
				tm.pickups.Remove (pickup.position);
			}
			SavedGame.AddCivilizedIsland (islandIndex);
		} else {
			FinishCivilizing ();
		}
	}

	void Update () {
		if (civilizing) {
			float timeRatio = Mathf.Clamp01((Time.time - civStartTime) / timeToCivilize);

			transform.position = Vector3.Lerp (startPos, targetPos, timeRatio);
			for (int i = 0; i < tiles.Length; i++) {
				if (!voidTileIndexes.Contains (i)) {
//					GameObject tileGO = tiles [i].tile;
//					float newHeight = tiles [i].originalY * (1f - timeRatio);
//					tileGO.transform.position = new Vector3 (tileGO.transform.position.x, newHeight, tileGO.transform.position.z);
//
//					if (tiles [i].resourceType == TerrainManager.ResourceInfo.ResourceType.None) {
//						Color newColor = Color.Lerp (tiles [i].originalColor, civilizedColor, timeRatio);
//						tileGO.GetComponentInChildren<MeshRenderer> ().material.color = newColor;
//					}
				}
			}

			foreach (Pickup pickup in pickups) {
				for (int i = 0; i < pickup.gameObjects.Count; i++) {
//					float newHeight = ((pickup.originalBaseHeight) * (1f - timeRatio)) + (tm.stackHeight * i);
//					Vector3 newPos = new Vector3 (pickup.gameObjects [i].transform.position.x, newHeight, pickup.gameObjects [i].transform.position.z);
//					pickup.gameObjects[i].transform.position = newPos;
				}
			}

			if (timeRatio == 1) {
				FinishCivilizing();
			}
		}
	}

	private int GetAvaiableTileIndex (bool canBeNextToUsedTile) {
		int attempts = 0;
		int random = Random.Range (0, tiles.Length - 1);
		Vector2 pos = tilePositions [random];

		if (canBeNextToUsedTile) {
			while (usedTiles.Contains (random)) {
				random = Random.Range (0, tiles.Length - 1); 
				pos = tilePositions [random];
				attempts++;
				if (attempts > 3) {
					print (attempts + " Attempts Made");
					break;
				}
			}
		} else {
			while (usedTiles.Contains (random) || AreAdjacentTiles(pos)) {
				random = Random.Range (0, tiles.Length - 1); 
				pos = tilePositions [random];
				attempts++;
				if (attempts > 3) {
					print (attempts + " Attempts Made");
					break;
				}
			}
		}

		usedTiles.Add (random);
		usedTilePositions.Add (pos);

		return random;
	}

	private bool AreAdjacentTiles (Vector2 pos) {
		List<Vector2> allDirections = new List<Vector2> () { Vector2.right, Vector2.up, Vector2.left, Vector2.down };

		foreach (Vector2 direction in allDirections) {
			Vector2 posToCheck = pos + direction;
			if (usedTilePositions.Contains (posToCheck)) {
				return true;
			}
		}

		return false;
	}

	void FinishCivilizing () {
		if (buildable) {
			return;
		}

		transform.position = targetPos;
		foreach (Pickup pickup in pickups) {
			List<GameObject> gameObjects = pickup.gameObjects;

			for (int k = 0; k < gameObjects.Count; k++) {
				//				float height = tm.stackHeight * k;
				gameObjects[k].transform.position = new Vector3 (Mathf.RoundToInt(gameObjects[k].transform.position.x), gameObjects[k].transform.position.y, Mathf.RoundToInt(gameObjects[k].transform.position.z));
			}
		}
			
		for (int i = 0; i < tiles.Length; i++) {
			if (!voidTileIndexes.Contains (i)) {
				tiles [i].tile.transform.position = new Vector3 (Mathf.RoundToInt (tiles [i].tile.transform.position.x), tiles [i].tile.transform.position.y, Mathf.RoundToInt (tiles [i].tile.transform.position.z));
				tm.tiles.Add (TerrainManager.PosToV2(tiles[i].tile.transform.position), tiles [i]);

				if (!GameManager.isLoadingFromSave) {
					if (tiles [i].resourceType != ResourceInfo.ResourceType.None) {
						SavedGame.AddResourceTile (tiles [i]);
					}
				}
			}
		}
			
		for (int i = 0; i < pickups.Count; i++) {
			pickups[i].UpdatePosition();
			tm.pickups.Add (pickups[i].position, pickups [i]);
		}

		GameObject.FindGameObjectWithTag ("Player").transform.parent = null;

		buildable = true;
		civilizing = false;

		if (!GameManager.isLoadingFromSave) {
			SavedGame.UpdatePickups ();
		}

		gm.TransitionEnd();
	}

	public void SaveEnemies() {
		SavedGame.UpdateEnemyList (islandIndex, enemies);
	}
		
	public struct Layer {
		public Color color;
	}

//	public struct VoidInfo {
//		public Vector2 position;
//
//		public VoidInfo (Vector2 _position) {
//
//		}
//	}
}