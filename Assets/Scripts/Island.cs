using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Island : MonoBehaviour {
	[Header("Tiles")]
	public GameObject tilePrefab;
	public Layer[] layers;
	public float heightPerLayer = 0.7f;
	public int size;

	[Space(10)]
	[Header("Resouces")]
	public int resourceCount;

	public List<Resource> resources = new List<Resource>();

	[Space(10)]
	[Header("Enemies")]
	public GameObject enemyPrefab;
	public int enemyCount;

	public List<Body> enemies = new List<Body>();

	private bool buildable = false;

	[Space(10)]
	[Header("Civilizing")]
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

		GenerateTiles ();
		SpawnResources ();
		SpawnEnemies ();
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
		for (int z = 0; z < size; z++) {
			for (int x = 0; x < size; x++) {
				int layer = Random.Range (0, tm.layers.Length);
				float y = layer * heightPerLayer;
				Vector3 position = new Vector3 (x, y, z);

				GameObject tile = (GameObject)Instantiate (tilePrefab, transform);
				tile.transform.localPosition = position;
				tile.name = "Tile (" + x + "," + z + ")";
				tile.GetComponent<MeshRenderer> ().material.color = tm.layers [layer].color;

				TerrainManager.Tile tileInfo = new TerrainManager.Tile(tile, GetComponent<Island>(), tm.layers [layer].color, y);
				tiles [index] = tileInfo;
				tm.tiles.Add (new Vector2 (x + transform.position.x, z + transform.position.z), tileInfo);

				index++;
			}
		}
	}

	void SpawnEnemies () {
		int[] spawnIndex = new int[enemyCount];
		for (int i = 0; i < enemyCount; i++) {
			spawnIndex [i] = Random.Range (0, tiles.Length - 1);
		}

		for (int i = 0; i < spawnIndex.Length; i++) {
			GameObject enemy = (GameObject)Instantiate(enemyPrefab, transform);
			enemy.transform.position = tiles[spawnIndex[i]].tile.transform.position;
			enemies.Add (enemy.GetComponent<Body>());
		}
	}

	void SpawnResources () {
		int[] spawnIndex = new int[resourceCount];
		for (int i = 0; i < resourceCount; i++) {
			spawnIndex [i] = Random.Range (0, tiles.Length - 1);
		}

		for (int i = 0; i < spawnIndex.Length; i++) {
			TerrainManager.ResourceInfo info = tm.resourceInfos [Random.Range (0, tm.resourceInfos.Length)];
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
		player.parent = tm.GetTileAtPosition(new Vector2((int)player.position.x, (int)player.position.z)).transform;
		player.Find ("Model").localPosition = Vector3.zero;

		for (int i = 0; i < tiles.Length; i++) {
			GameObject tile = tiles [i].tile;
			tm.tiles.Remove (new Vector2(tile.transform.position.x, tile.transform.position.z));
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

				Color newColor = Color.Lerp (tiles [i].originalColor, tm.layers [0].color, timeRatio);
				tileGO.GetComponentInChildren<MeshRenderer> ().material.color = newColor;
			}

			foreach (var resource in resources) {
				for (int i = 0; i < resource.resourceGO.Count; i++) {
					float newHeight = (resource.originalBaseHeight + (tm.stackHeight * i)) * (1f - timeRatio);
					print ((resource.originalBaseHeight + (tm.stackHeight * i)));
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
				resourceGOs[k].transform.position = new Vector3 (resourceGOs[k].transform.position.x, height, resourceGOs[k].transform.position.z);
			}
			tm.resources.Remove (resouce.position);
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