﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Island : MonoBehaviour {
	[Header("Teir")]
	private TerrainManager.Teir teirInfo;
	public int teir;

	[Space(10)]
	[Header("Tiles")]
	public GameObject tilePrefab;
	public Layer[] layers;
	public float heightPerLayer = 0.7f;
	public int size;

	[Space(10)]
	[Header("Resouces")]
	public int resourceTileCount;
	public int resourcesPerTile;

	public List<Pickup> pickups = new List<Pickup>();

	[Space(10)]
	[Header("Enemies")]
	public GameObject enemyPrefab;

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
	public Vector2 index;

	void Awake () {
		tm = TerrainManager.instance;
	}

	public void InitIsland() {
		teir = Mathf.Clamp(Mathf.RoundToInt(index.x + index.y), 0, tm.teirs.Length - 1);
		teirInfo = tm.teirs [teir];

		setSpawns = tm.GetSetSpawns (teir);

		GenerateTiles ();
		SpawnEnemies ();
	}

	//has to be in start because GameManager instance hasn't been set yet
	void Start () {
		gm = GameManager.instance;
	}

	public void PlayerEnterIsland () {
		for (int i = 0; i < enemies.Count; i++) {
			enemies [i].GetComponentInChildren<Mind> ().active = true;
		}
	}

	public void PlayerExitIsland () {
		for (int i = 0; i < enemies.Count; i++) {
			enemies [i].GetComponentInChildren<Mind> ().active = false;
		}
	}

	public void EnemyDeath (Body enemy) {
		enemies.Remove (enemy);
	}

	public void TestEnemyCount () {
		if (!buildable) {
			if (enemies.Count <= 0) {
				StartCivilizing ();
			}
		}
	}

	void GenerateTiles () {
		tiles = new TerrainManager.Tile[size * size];

		// void tiles
		voidTileIndexes = new List<int> ();
		for (int i = 0; i < voidCount; i++) {
			voidTileIndexes.Add (GetAvaiableTileIndex(true));
		}

		int tileIndex = 0;
		for (int z = 0; z < size; z++) {
			for (int x = 0; x < size; x++) {
				if (voidTileIndexes.Contains (tileIndex)) {
					usedTilePositions.Add (new Vector2(x, z));
				}
				tileIndex++;
			}
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
		for (int z = 0; z < size; z++) {
			for (int x = 0; x < size; x++) {
				if (!voidTileIndexes.Contains (index)) {
					int layer = Random.Range (0, teirInfo.layers.Length);
					float y = layer * heightPerLayer;
					Vector3 position = new Vector3 (x, y, z);

					GameObject tile = (GameObject)Instantiate (tilePrefab, transform);
					tile.transform.localPosition = position;
					tile.name = "Tile (" + x + "," + z + ")";

					TerrainManager.ResourceInfo.ResourceType resourceInfoType;
					Color tileColor;
					if (resourceTileSpawnIndexs.Contains (index)) {
						int resourceIndex = tileSpawnIDs [resourceSpawnListIndex];
						resourceSpawnListIndex++;
						TerrainManager.ResourceInfo resourceInfo = tm.resourceInfos [resourceIndex];
						resourceInfoType = resourceInfo.type;
						tileColor = resourceInfo.color;

						for (int i = 0; i < resourcesPerTile; i++) {
							tm.SpawnResource(position: transform.TransformPoint(position), info: resourceInfo, island: GetComponent<Island>(), initialSpawn: true);
						}
					} else {
						resourceInfoType = TerrainManager.ResourceInfo.ResourceType.None;
						tileColor = teirInfo.layers [layer].color;
					}

					TerrainManager.Tile tileInfo = new TerrainManager.Tile (tile, resourceInfoType, GetComponent<Island> (), tileColor, y);
					tile.GetComponent<MeshRenderer> ().material.color = tileColor;

					tiles [index] = tileInfo;
					tm.tiles.Add (new Vector2 (x + transform.position.x, z + transform.position.z), tileInfo);
				}

				index++;
			}
		}
	}

	void SpawnEnemies () {
		List<int> spawnIDs = new List<int> ();
		foreach (var setSpawn in setSpawns) {
			if (setSpawn.type == TerrainManager.SetSpawn.SpawnType.Enemy) {
				spawnIDs.Add (setSpawn.spawnID);
			}
		}
		int enemiesLeft = teirInfo.enemyCount - spawnIDs.Count;
		for (int i = 0; i < enemiesLeft; i++) {
			spawnIDs.Add(teirInfo.enemyIDs[Random.Range(0, teirInfo.enemyIDs.Length)]);
		}

		List<int> spawnTileIndexs = new List<int> ();

		for (int f = 0; f < spawnIDs.Count; f++) {
			spawnTileIndexs.Add (GetAvaiableTileIndex (true));
		}

		for (int i = 0; i < spawnIDs.Count; i++) {
			GameObject enemyPrefab = tm.enemyPrefabs [spawnIDs [i]];
			GameObject enemy = (GameObject)Instantiate(enemyPrefab, transform);
			enemy.transform.position = tiles[spawnTileIndexs[i]].tile.transform.position;
			enemies.Add (enemy.GetComponent<Body>());
		}
	}

	void StartCivilizing () {
		gm.CivilizeStart();
		civStartTime = Time.time;
		civilizing = true;
		startPos = transform.position;
		targetPos = (transform.position / tm.spacing) * size;
		Transform player = GameObject.FindGameObjectWithTag ("Player").transform;
		player.parent = tm.GetTileAtPosition(new Vector2(Mathf.RoundToInt(player.position.x), Mathf.RoundToInt(player.position.z))).transform;
		player.Find ("Model").localPosition = Vector3.zero;

		for (int i = 0; i < tiles.Length; i++) {
			if (!voidTileIndexes.Contains (i)) {
				GameObject tile = tiles [i].tile;
				tm.tiles.Remove (new Vector2 (tile.transform.position.x, tile.transform.position.z));
			}
		}

		foreach (Pickup pickup in pickups) {
			tm.pickups.Remove (pickup.position);
		}
	}

	void Update () {
		if (civilizing) {
			float timeRatio = Mathf.Clamp01((Time.time - civStartTime) / timeToCivilize);

			transform.position = Vector3.Lerp (startPos, targetPos, timeRatio);
			for (int i = 0; i < tiles.Length; i++) {
				if (!voidTileIndexes.Contains (i)) {
					GameObject tileGO = tiles [i].tile;
					float newHeight = tiles [i].originalY * (1f - timeRatio);
					tileGO.transform.position = new Vector3 (tileGO.transform.position.x, newHeight, tileGO.transform.position.z);

					if (tiles [i].resourceType == TerrainManager.ResourceInfo.ResourceType.None) {
						Color newColor = Color.Lerp (tiles [i].originalColor, civilizedColor, timeRatio);
						tileGO.GetComponentInChildren<MeshRenderer> ().material.color = newColor;
					}
				}
			}

			foreach (Pickup pickup in pickups) {
				for (int i = 0; i < pickup.gameObjects.Count; i++) {
					float newHeight = ((pickup.originalBaseHeight) * (1f - timeRatio)) + (tm.stackHeight * i);
					Vector3 newPos = new Vector3 (pickup.gameObjects [i].transform.position.x, newHeight, pickup.gameObjects [i].transform.position.z);
					pickup.gameObjects[i].transform.position = newPos;
				}
			}

			if (timeRatio == 1) {
				FinishCivilizing();
			}
		}
	}

	private int GetAvaiableTileIndex (bool canBeNextToUsedTile) {
		int random = Random.Range (0, tiles.Length - 1);
		int playerTileIndex = 0; // hack?

		int x = random % size;
		int y = Mathf.FloorToInt (random / size);
		Vector2 pos = new Vector2 (x, y);

		if (canBeNextToUsedTile) {
			if (usedTiles.Contains (random) || random == playerTileIndex) {
				while (usedTiles.Contains (random) || random == playerTileIndex) {
					random = Random.Range (0, tiles.Length - 1);
					x = random % size;
					y = Mathf.FloorToInt (random / size);
					pos = new Vector2 (x, y);
				}
			}
		} else {
			if (usedTiles.Contains (random) || random == playerTileIndex || NextToUsedTile(pos, random)) {
				while (usedTiles.Contains (random) || random == playerTileIndex || NextToUsedTile(pos, random)) {
					random = Random.Range (0, tiles.Length - 1);
					x = random % size;
					y = Mathf.FloorToInt (random / size);
					pos = new Vector2 (x, y);
				}
			}
		}
		usedTiles.Add (random);
		usedTilePositions.Add (pos);

		return random;
	}

	private bool NextToUsedTile (Vector2 pos, int tileIndex) {
		if (voidTileIndexes.Contains (tileIndex)) {
			return true;
		} else {
			List<Vector2> allDirections = new List<Vector2> () { Vector2.right, Vector2.up, Vector2.left, Vector2.down };

			foreach (var direction in allDirections) {
				Vector2 testingPos = pos + direction;
				if (usedTilePositions.Contains (testingPos)) {
					return true;
				}
			}
			return false;
		}
	}

	void FinishCivilizing () {
		if (buildable) {
			return;
		}

		transform.position = targetPos;
		foreach (Pickup pickup in pickups) {
			List<GameObject> gameObjects = pickup.gameObjects;

			for (int k = 0; k < gameObjects.Count; k++) {
				float height = tm.stackHeight * k;
				gameObjects[k].transform.position = new Vector3 (Mathf.RoundToInt(gameObjects[k].transform.position.x), height, Mathf.RoundToInt(gameObjects[k].transform.position.z));
			}
		}
			
		for (int i = 0; i < tiles.Length; i++) {
			if (!voidTileIndexes.Contains (i)) {
				tiles [i].tile.transform.position = new Vector3 (Mathf.RoundToInt (tiles [i].tile.transform.position.x), 0f, Mathf.RoundToInt (tiles [i].tile.transform.position.z));
				tm.tiles.Add (new Vector2 (tiles [i].tile.transform.position.x, tiles [i].tile.transform.position.z), tiles [i]);
			}
		}
			
		for (int i = 0; i < pickups.Count; i++) {
			pickups[i].UpdatePosition();
			tm.pickups.Add (pickups[i].position, pickups [i]);
		}

		GameObject.FindGameObjectWithTag ("Player").transform.parent = null;

		buildable = true;
		civilizing = false;

		gm.CivilizeEnd();
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