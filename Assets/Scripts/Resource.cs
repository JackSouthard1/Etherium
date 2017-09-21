using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Resource {
	public enum ResourceType
	{
		Yellow,
		Green,
		Purple,
		Blue
	};
	public ResourceType type;
	public Vector2 position;
	public GameObject resourceGO;

	public Resource (ResourceType _type, GameObject _resourceGO) {
		type = _type;
		resourceGO = _resourceGO;
		if (_resourceGO != null) {
			position = new Vector2 (resourceGO.transform.position.x, resourceGO.transform.position.z);
		}
	}
}
