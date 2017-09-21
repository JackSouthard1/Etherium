using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Resource {
	public TerrainManager.ResourceInfo info;
	public Vector2 position;
	public GameObject resourceGO;

	public Resource (TerrainManager.ResourceInfo _info, GameObject _resourceGO) {
		info = _info;
		resourceGO = _resourceGO;
		if (_resourceGO != null) {
			position = new Vector2 (resourceGO.transform.position.x, resourceGO.transform.position.z);
		}
	}
}
