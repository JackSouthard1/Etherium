using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainManager : MonoBehaviour {
	[Header("Islands")]
	public GameObject islandPrefab;
	public int count;
	public int spacing;

	[HideInInspector]
	public Island[] islands;

	[Space(10)]
	[Header("Resources")]
	public ResourceInfo[] resourceInfos;

	[Space(10)]
	[Header("Layers")]
	public Layer[] layers;

	[Space(10)]
	[Header("Buses")]
	public GameObject busPrefab;
	public int lifeTime;

	[HideInInspector]
	public List<Bus> buses = new List<Bus>();

	[HideInInspector]
	public Dictionary<Vector2, Tile> tiles = new Dictionary<Vector2, Tile> ();

	[HideInInspector]
	public Dictionary<Vector2, Resource> resources = new Dictionary<Vector2, Resource> ();

	public void TurnEnd () {
		for (int i = 0; i < buses.Count; i++) {
			buses [i].TurnEnd ();
		}
	}

	void Awake () {
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
				island.GetComponent<Island>().index = new Vector2(x, z);
				islands [index] = island.GetComponent<Island>();

				index++;
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

	public Resource GetResourceAtPosition (Vector2 position) {
		if (resources.ContainsKey (position)) {
			return resources [position];
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
		public Resource.ResourceType type;
	}

	public struct Tile {
		public GameObject tile;
		public Island island;

		public Tile (GameObject _tile, Island _island) {
			tile = _tile;
			island = _island;
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
}