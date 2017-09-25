using System.Collections;
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
	public int resourceCount;
	public int resourceTileCount;

	public List<Resource> resources = new List<Resource>();

	[Space(10)]
	[Header("Enemies")]
	public GameObject enemyPrefab;

	public List<Body> enemies = new List<Body>();

	private bool buildable = false;

	[Space(10)]
	[Header("Civilizing")]
	public Color civilizedColor;
	public float timeToCivilize;
	private float civStartTime;
	private Vector3 targetPos;
	private Vector3 startPos;
	private bool civilizing = false;

	public TerrainManager.Tile[] tiles;
	private TerrainManager tm;
	private GameManager gm;
	public Vector2 index;

	void Start () {
		tm = GameObject.Find ("Terrain").GetComponent<TerrainManager> ();
		gm = GameObject.Find ("GameManager").GetComponent<GameManager> ();

		teir = Mathf.RoundToInt(index.x + index.y);
		teirInfo = tm.teirs [teir];

		GenerateTiles ();
		SpawnResources ();
		SpawnEnemies ();

//		TestEnemyCount ();
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
		int index = 0;
		tiles = new TerrainManager.Tile[size * size];

		List<int> resourceTileSpawnIndexs = new List<int> ();

		for (int i = 0; i < resourceTileCount; i++) {
			int random = Random.Range (0, tiles.Length - 1);

			if (resourceTileSpawnIndexs.Contains(random)) {
				while (resourceTileSpawnIndexs.Contains(random)) {
					random = Random.Range (0, tiles.Length - 1);
				}
			}
			resourceTileSpawnIndexs.Add (random);

		}

		for (int z = 0; z < size; z++) {
			for (int x = 0; x < size; x++) {
				int layer = Random.Range (0, teirInfo.layers.Length);
				float y = layer * heightPerLayer;
				Vector3 position = new Vector3 (x, y, z);

				GameObject tile = (GameObject)Instantiate (tilePrefab, transform);
				tile.transform.localPosition = position;
				tile.name = "Tile (" + x + "," + z + ")";

				TerrainManager.ResourceInfo.ResourceType resourceInfoType;
				Color tileColor;
				if (resourceTileSpawnIndexs.Contains (index)) {
					int resourceIndex = teirInfo.resourceIndexes [Random.Range (0, teirInfo.resourceIndexes.Length)];
					TerrainManager.ResourceInfo resourceInfo = tm.resourceInfos [resourceIndex];
					resourceInfoType = resourceInfo.type;
					tileColor = resourceInfo.color;
				} else {
					resourceInfoType = TerrainManager.ResourceInfo.ResourceType.None;
					tileColor = teirInfo.layers [layer].color;
				}

				TerrainManager.Tile tileInfo = new TerrainManager.Tile(tile, resourceInfoType, GetComponent<Island>(), tileColor, y);
				tile.GetComponent<MeshRenderer> ().material.color = tileColor;

				tiles [index] = tileInfo;
				tm.tiles.Add (new Vector2 (x + transform.position.x, z + transform.position.z), tileInfo);

				index++;
			}
		}
	}

	void SpawnEnemies () {
		List<int> spawnIndexs = new List<int> ();

		for (int f = 0; f < teirInfo.enemyCount; f++) {
			int random = Random.Range (0, tiles.Length - 1);

			if (spawnIndexs.Contains(random)) {
				while (spawnIndexs.Contains(random)) {
					random = Random.Range (0, tiles.Length - 1);
				}
			}

			spawnIndexs.Add (random);

		}

		for (int i = 0; i < spawnIndexs.Count; i++) {
			GameObject enemy = (GameObject)Instantiate(enemyPrefab, transform);
			enemy.transform.position = tiles[spawnIndexs[i]].tile.transform.position;
			enemies.Add (enemy.GetComponent<Body>());
		}
	}

	void SpawnResources () {
		int[] spawnIndex = new int[resourceCount];
		for (int i = 0; i < resourceCount; i++) {
			spawnIndex [i] = Random.Range (0, tiles.Length - 1);
		}

		for (int i = 0; i < spawnIndex.Length; i++) {
			TerrainManager.ResourceInfo info = tm.resourceInfos [teirInfo.resourceIndexes[Random.Range(0, teirInfo.resourceIndexes.Length)]];
			tm.SpawnResource(tiles[spawnIndex[i]].tile.transform.position, info, GetComponent<Island>());
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
			GameObject tile = tiles [i].tile;
			tm.tiles.Remove (new Vector2(tile.transform.position.x, tile.transform.position.z));
		}

		foreach (Resource resouce in resources) {
			tm.resources.Remove (resouce.position);
		}
	}

	void Update () {
		if (civilizing) {
			float timeRatio = Mathf.Clamp01((Time.time - civStartTime) / timeToCivilize);

			transform.position = Vector3.Lerp (startPos, targetPos, timeRatio);
			for (int i = 0; i < tiles.Length; i++) {
				GameObject tileGO = tiles [i].tile;
				float newHeight = tiles [i].originalY * (1f - timeRatio);
				tileGO.transform.position = new Vector3 (tileGO.transform.position.x, newHeight, tileGO.transform.position.z);

				if (tiles [i].resourceType == TerrainManager.ResourceInfo.ResourceType.None) {
					Color newColor = Color.Lerp (tiles [i].originalColor, civilizedColor, timeRatio);
					tileGO.GetComponentInChildren<MeshRenderer> ().material.color = newColor;
				}
			}

			foreach (var resource in resources) {
				for (int i = 0; i < resource.resourceGO.Count; i++) {
					float newHeight = ((resource.originalBaseHeight) * (1f - timeRatio)) + (tm.stackHeight * i);
					Vector3 newPos = new Vector3 (resource.resourceGO [i].transform.position.x, newHeight, resource.resourceGO [i].transform.position.z);
					resource.resourceGO[i].transform.position = newPos;
				}
			}

			if (timeRatio == 1) {
				FinishCivilizing();
			}
		}
	}

	void FinishCivilizing () {
		if (buildable) {
			return;
		}

		transform.position = targetPos;
		foreach (Resource resouce in resources) {
			List<GameObject> resourceGOs = resouce.resourceGO;

			for (int k = 0; k < resourceGOs.Count; k++) {
				float height = tm.stackHeight * k;
				resourceGOs[k].transform.position = new Vector3 (Mathf.RoundToInt(resourceGOs[k].transform.position.x), height, Mathf.RoundToInt(resourceGOs[k].transform.position.z));
			}
		}
			
		for (int i = 0; i < tiles.Length; i++) {
			tiles [i].tile.transform.position = new Vector3 (Mathf.RoundToInt (tiles [i].tile.transform.position.x), 0f, Mathf.RoundToInt (tiles [i].tile.transform.position.z));
			tm.tiles.Add (new Vector2 (tiles [i].tile.transform.position.x, tiles [i].tile.transform.position.z), tiles [i]);
		}
			
		for (int i = 0; i < resources.Count; i++) {
			resources[i].UpdatePosition();
			tm.resources.Add (resources[i].position, resources [i]);
		}

		GameObject.FindGameObjectWithTag ("Player").transform.parent = null;

		buildable = true;
		civilizing = false;

		gm.CivilizeEnd();
	}
		
	public struct Layer {
		public Color color;
	}
}