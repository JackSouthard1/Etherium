using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {
	public List<Resource> inventory = new List<Resource>();
	Body body;
	TerrainManager tm;

	void Start () {
		tm = GameObject.Find ("Terrain").GetComponent<TerrainManager> ();
		body = GetComponent<Body> ();
	}

	void Update () {
		if (Input.GetKeyDown (KeyCode.I)) {
			string str = "Inventory: ";
			for (int i = 0; i < inventory.Count; i++) {
				str += inventory [i].info.type + ", ";
			}
			print (str);
		}

		if (Input.GetKeyDown (KeyCode.E)) {
			if (inventory.Count > 0) {
				DropResource (inventory.Count - 1);
			}
		}
	}

	public void CollectResource (Resource resource) {
		int stackCount = resource.resourceGO.Count;
		for (int i = 0; i < stackCount; i++) {
			Resource newResource = new Resource (resource.info, null, null);
			inventory.Add (newResource);
		}
			
		resource.island.resources.Remove (resource);
	
		tm.resources.Remove (resource.position);
		for (int i = 0; i < stackCount; i++) {
			Destroy (resource.resourceGO [i]);
		}
	}

	public void DropResource (int index) {
		Vector2 posV2 = new Vector2 (transform.position.x, transform.position.z);
		if (tm.GetTileAtPosition(posV2) == null) {
			return;
		}

		TerrainManager.ResourceInfo resourceInfoToSpawn = inventory [index].info;

		if (tm.GetResourceAtPosition (posV2) != null) {
			Resource curResource = tm.GetResourceAtPosition (posV2);
			if (curResource.info.type == resourceInfoToSpawn.type) {
				tm.SpawnResource (transform.position, resourceInfoToSpawn, body.location);
				inventory.RemoveAt (index);
			}
		} else {
//			print((tm.SpawnResource (transform.position, resourceInfoToSpawn, tm.transform, body.location)).island);
			tm.SpawnResource (transform.position, resourceInfoToSpawn, body.location);
			inventory.RemoveAt (index);
		}
	}
}
