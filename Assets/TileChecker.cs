using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileChecker : MonoBehaviour {

	// For debugging
	void Update () {
		if (Input.GetKeyDown (KeyCode.W)) {
			transform.position += Vector3.forward;
		}
		if (Input.GetKeyDown (KeyCode.S)) {
			transform.position += Vector3.back;
		}
		if (Input.GetKeyDown (KeyCode.D)) {
			transform.position += Vector3.right;
		}
		if (Input.GetKeyDown (KeyCode.A)) {
			transform.position += Vector3.left;
		}
		if (Input.GetKeyDown (KeyCode.C)) {
			bool isTile = TerrainManager.instance.isTileAtPosition (new Vector2 (transform.position.x, transform.position.z));
			print ("Checking: " + new Vector2 (transform.position.x, transform.position.z) + " Tile: " + isTile);
		}
	}
}
