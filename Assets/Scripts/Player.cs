using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {
	public List<Resource> inventory = new List<Resource>();
	TerrainManager tm;

	void Start () {
		tm = GameObject.Find ("Terrain").GetComponent<TerrainManager> ();
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
		Resource newResource = new Resource (resource.info, null);
		inventory.Add (newResource);
//		print ("Before: " + tm.resources.Count);
		tm.resources.Remove (resource.position);
//		print ("After: " + tm.resources.Count);
		Destroy (resource.resourceGO);
	}

	public void DropResource (int index) {
		Vector2 posV2 = new Vector2 (transform.position.x, transform.position.z);
		if (tm.GetResourceAtPosition (posV2) != null || tm.GetTileAtPosition(posV2) == null) {
			return;
		}

		tm.SpawnResource (transform.position, inventory [index].info, tm.transform);
		inventory.RemoveAt (index);
	}
}
