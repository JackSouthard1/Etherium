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

	void Start () {
		turnsUntilNextResource = turnWaitPerResource;
		tm = GameObject.FindObjectOfType<TerrainManager> ();
	}

	public void TurnEnd() {
		turnsUntilNextResource -= 1;
		if (turnsUntilNextResource <= 0) {
			turnsUntilNextResource = turnWaitPerResource;
			tm.SpawnResource (transform.position, tm.ResourceTypeToInfo (resourceType), island, height);
		}
	}
}