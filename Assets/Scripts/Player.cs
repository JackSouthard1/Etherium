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
				str += inventory [i].type + ", ";
			}
			print (str);
		}
	}

	public void CollectResource (Resource resource) {
		Resource newResource = new Resource (resource.type, null);
		inventory.Add (newResource);
		tm.resources.Remove (resource.position);
		Destroy (resource.resourceGO);
	}
}
