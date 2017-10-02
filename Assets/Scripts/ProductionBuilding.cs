using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProductionBuilding : Building
{
	[Header("Production")]
	public TerrainManager.ResourceInfo.ResourceType resourceType;
	public int turnWaitPerResource;

	TerrainManager tm;
	int turnsUntilNextResource;

	//has to be awake because base class has Start
	void Awake () {
		turnsUntilNextResource = turnWaitPerResource;
		tm = GameObject.FindObjectOfType<TerrainManager> ();
	}

	public override void TurnEnd() {
		if (!isActive)
			return;
		
		turnsUntilNextResource -= 1;
		if (turnsUntilNextResource <= 0) {
			turnsUntilNextResource = turnWaitPerResource;

			Vector3 spawnPos = (pad != null) ? pad.position : transform.position;
			float spawnHeight = (pad != null) ? pad.position.y : height;
			tm.SpawnResource (spawnPos, tm.ResourceTypeToInfo (resourceType), island, spawnHeight);

			supply -= 1;
		}

		base.TurnEnd ();
	}
}