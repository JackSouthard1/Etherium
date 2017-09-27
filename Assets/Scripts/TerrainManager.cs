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
	[Header("Teirs")]
	public Teir[] teirs;

	[Space(10)]
	[Header("Resources")]
	public ResourceInfo[] resourceInfos;
	public GameObject resourcePrefab;
	public float stackHeight;

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
		for (int i = 0; i < islands.Length; i++) {
			islands [i].TestEnemyCount ();
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

	public Resource SpawnResource (Vector3 position, ResourceInfo info, Island island) {
		Vector2 posV2 = new Vector2 (position.x, position.z);
		if (resources.ContainsKey (posV2)) {
			Resource curResource = resources [posV2];
			if (curResource.info.type == info.type) {
				GameObject resourceGO = Instantiate (resourcePrefab, island.transform);
				resourceGO.transform.position = position;
				resourceGO.gameObject.GetComponentInChildren<SpriteRenderer> ().sprite = info.sprite;
				resourceGO.gameObject.GetComponentInChildren<SpriteRenderer> ().color = info.color;

				curResource.resourceGO.Add (resourceGO);
				resourceGO.transform.Translate (Vector3.up * stackHeight * (curResource.resourceGO.Count - 1));

				UpdateResources ();
				return curResource;
			} else {
				return null;
			}
		} else {
			GameObject resourceGO = Instantiate (resourcePrefab, island.transform);
			resourceGO.transform.position = position;
			Resource resource = new Resource (info, resourceGO, island);

			resourceGO.GetComponentInChildren<SpriteRenderer> ().sprite = info.sprite;
			resourceGO.gameObject.GetComponentInChildren<SpriteRenderer> ().color = info.color;
			resources.Add (posV2, resource);
			island.resources.Add (resource);

			UpdateResources ();
			return resource;
		}
	}

	void UpdateResources () {
		foreach (KeyValuePair<Vector2, Resource> resource in resources) {
			resource.Value.TestForCrafting ();
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

	public bool ResourceAtPosition (Vector2 position) {
		if (resources.ContainsKey (position)) {
			return true;
		} else {
			return false;
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
}