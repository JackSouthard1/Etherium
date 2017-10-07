using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Resource {
	public TerrainManager.ResourceInfo info;
	public Vector2 position;
	public float originalBaseHeight;
	public List<GameObject> resourceGO = new List<GameObject>();
	public Island island;

	private Crafting crafting;

	public void UpdatePosition () {
		position = new Vector2 (resourceGO[0].transform.position.x, resourceGO[0].transform.position.z);
		originalBaseHeight = resourceGO [0].transform.position.y;
	}

	public Resource (TerrainManager.ResourceInfo _info, GameObject _resourceGO, Island _island) {
		info = _info;
		island = _island;
		resourceGO.Add(_resourceGO);
		if (_resourceGO != null) {
			UpdatePosition ();
		}
	}
}
