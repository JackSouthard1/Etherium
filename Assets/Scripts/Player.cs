using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {
	public Transform inventoryParent;

	Body body;
	TerrainManager tm;

	//TODO: what would be the best data structures to use here?
	//we could simplify these to lists, and then get the ResourceInfo from the TerrainManager instead
	Dictionary<TerrainManager.ResourceInfo, float> inventory = new Dictionary<TerrainManager.ResourceInfo, float>();
	Dictionary<TerrainManager.ResourceInfo, UIResource> inventoryUI = new Dictionary<TerrainManager.ResourceInfo, UIResource>();

	public GameObject uiResourcePrefab;

	void Start () {
		tm = GameObject.Find ("Terrain").GetComponent<TerrainManager> ();
		body = GetComponent<Body> ();

		InitInventory ();
	}

	void InitInventory () {
		foreach (TerrainManager.ResourceInfo resourceInfo in tm.resourceInfos) {
			inventory.Add(resourceInfo, 0f);

			GameObject newUIResourceObj = (GameObject)Instantiate (uiResourcePrefab, inventoryParent);
			UIResource newUIResource = newUIResourceObj.GetComponent<UIResource> ();
			newUIResource.Init (resourceInfo);

			inventoryUI.Add(resourceInfo, newUIResource);
		}

		UpdateInventoryUI ();
	}

	void Update () {
		if (Input.GetKeyDown (KeyCode.I)) {
			string str = "Inventory - ";
			foreach (KeyValuePair<TerrainManager.ResourceInfo, float> keyValuePair in inventory) {
				str += keyValuePair.Key.type.ToString () + ": " + keyValuePair.Value.ToString () + ", ";
			}
			print (str);
		}
	}

	public void CollectResource (Resource resource) {
		int stackCount = resource.resourceGO.Count;
		inventory [resource.info] += (float) stackCount;
			
		resource.island.resources.Remove (resource);
	
		tm.resources.Remove (resource.position);
		for (int i = 0; i < stackCount; i++) {
			Destroy (resource.resourceGO [i]);
		}

		UpdateInventoryUI ();
	}

	public void DropResource (TerrainManager.ResourceInfo resourceInfo) {
		if (inventory [resourceInfo] < 1f) {
			return;
		}

		Vector2 posV2 = new Vector2 (transform.position.x, transform.position.z);
		if (tm.GetTileAtPosition(posV2) == null) {
			return;
		}
			
		if (tm.GetResourceAtPosition (posV2) != null) {
			Resource curResource = tm.GetResourceAtPosition (posV2);
			if (curResource.info.type != resourceInfo.type) {
				return;
			}
		}

		tm.SpawnResource (transform.position, resourceInfo, body.location);
		inventory[resourceInfo] -= 1f;
		UpdateInventoryUI ();
	}

	void UpdateInventoryUI () {
		float leftAnchor = 0f;
		foreach (KeyValuePair<TerrainManager.ResourceInfo, UIResource> keyValuePair in inventoryUI) {
			keyValuePair.Value.amount = inventory[keyValuePair.Key];
			keyValuePair.Value.UpdateVisual();

			keyValuePair.Value.gameObject.GetComponent<RectTransform> ().offsetMin = new Vector2 (leftAnchor, 0f);
			leftAnchor += keyValuePair.Value.totalWidth;
		}
	}
}
