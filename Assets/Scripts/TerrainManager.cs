using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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
				island.GetComponent<Island>().index = new Vector2(x, z);
				islands [index] = island.GetComponent<Island>();

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

	public Resource SpawnResource (Vector3 position, ResourceInfo info, Island island, bool initialSpawn = false, float startingHeight = 0f) {
		Vector2 posV2 = new Vector2 (position.x, position.z);

		if (island == null) {
			return null;
		}

		if (resources.ContainsKey (posV2)) {
			Resource curResource = resources [posV2];
			if (curResource.info.type == info.type) {
				GameObject resourceGO = Instantiate (resourcePrefab, island.transform);
				resourceGO.transform.position = position;
				resourceGO.gameObject.GetComponentInChildren<SpriteRenderer> ().sprite = info.sprite;
				resourceGO.gameObject.GetComponentInChildren<SpriteRenderer> ().color = info.color;

				curResource.resourceGO.Add (resourceGO);
				resourceGO.transform.Translate (Vector3.up * (stackHeight * (curResource.resourceGO.Count - 1) + startingHeight));

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
			Resource resource = new Resource (info, resourceGO, island);

			resourceGO.GetComponentInChildren<SpriteRenderer> ().sprite = info.sprite;
			resourceGO.gameObject.GetComponentInChildren<SpriteRenderer> ().color = info.color;
			resources.Add (posV2, resource);
			island.resources.Add (resource);

			resourceGO.transform.Translate (Vector3.up * startingHeight);
			if (!initialSpawn) {
				UpdateResources ();
			}
			return resource;
		}
	}

	public void ConsumeResources (List<Resource> consumedResources, List<int> amountsToConsume = null) {
		List<Vector2> keysToRemove = new List<Vector2> ();

		for (int k = 0; k < consumedResources.Count; k++) {
			int amountToConsume = (amountsToConsume != null) ? amountsToConsume [k] : consumedResources [k].resourceGO.Count;
			bool consumesFullStack = amountToConsume == consumedResources [k].resourceGO.Count;

			if (consumesFullStack)
				consumedResources [k].island.resources.Remove (consumedResources [k]);

			List<GameObject> destroying = new List<GameObject> ();
			int max = (consumedResources [k].resourceGO.Count - 1);
			int min = (consumedResources [k].resourceGO.Count - amountToConsume);
			for (int i = max; i >= min; i--) {
				destroying.Add (consumedResources[k].resourceGO [i]);
				consumedResources [k].resourceGO.RemoveAt (i);
			}

			for (int i = 0; i < destroying.Count; i++) {
				Destroy (destroying [i]);
			}

			if(consumesFullStack)
				keysToRemove.Add (consumedResources[k].position);
		}

		foreach (Vector2 resourceKey in resources.Keys.ToList()) {
			if (keysToRemove.Contains (resourceKey)) {
				resources.Remove (resourceKey);
			}
		}
	}

	void UpdateResources () {
		foreach (Vector2 key in resources.Keys.ToList()) {
			if (resources.ContainsKey (key)) {
				Resource resourceToTest = resources [key];
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
}