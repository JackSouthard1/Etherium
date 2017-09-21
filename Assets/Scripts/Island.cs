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
	public GameObject resourcePrefab;
	public int resourceCount;

	public List<Resource> resources = new List<Resource>();

	[Space(10)]
	[Header("Enemies")]
	public GameObject enemyPrefab;
	public int enemyCount;

	public List<Body> enemies = new List<Body>();

	private bool buildable = false;


	public TerrainManager.Tile[] tiles;
	private TerrainManager tm;
	public Vector2 index;

	void Start () {
		tm = GameObject.Find ("Terrain").GetComponent<TerrainManager> ();
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
		if (enemies.Count <= 0) {
			Civilize ();
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

				TerrainManager.Tile tileInfo = new TerrainManager.Tile(tile, GetComponent<Island>());
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
			GameObject resourceGO = Instantiate(resourcePrefab, transform);
			TerrainManager.ResourceInfo info = tm.resourceInfos [Random.Range (0, tm.resourceInfos.Length)];
			Resource resource = new Resource (info.type, resourceGO);

			resource.resourceGO.GetComponentInChildren<SpriteRenderer> ().sprite = info.sprite;
			resource.resourceGO.transform.position = tiles[spawnIndex[i]].tile.transform.position;
			resources.Add (resource);
			tm.resources.Add (new Vector2 (tiles [spawnIndex [i]].tile.transform.position.x, tiles [spawnIndex [i]].tile.transform.position.z), resource);
		}
	}

	public void Civilize () {
		if (buildable) {
			return;
		}
		for (int i = 0; i < tiles.Length; i++) {
			GameObject tile = tiles [i].tile;
			tile.transform.position = new Vector3 (tile.transform.position.x, 0, tile.transform.position.z);
			tile.GetComponent<MeshRenderer> ().material.color = tm.layers [0].color;
			tm.tiles.Remove (new Vector2(tile.transform.position.x, tile.transform.position.z));
		}

		transform.position = (transform.position / tm.spacing) * size;

		for (int i = 0; i < tiles.Length; i++) {
			tm.tiles.Add (new Vector2 (tiles [i].tile.transform.position.x, tiles [i].tile.transform.position.z), tiles [i]);
		}

		buildable = true;

	}

	public struct Layer {
		public Color color;
	}
}