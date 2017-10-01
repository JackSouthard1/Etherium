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
	Island island;

	void Start () {
		turnsUntilNextResource = turnWaitPerResource;
		tm = GameObject.FindObjectOfType<TerrainManager> ();

		//HACK
		island = tm.GetTileAtPosition(transform.position).transform.parent.GetComponent<Island>();
	}

	public void TurnEnd() {
		turnsUntilNextResource -= 1;
		if (turnsUntilNextResource <= 0) {
			turnsUntilNextResource = turnWaitPerResource;
			tm.SpawnResource (transform.position, tm.ResourceTypeToInfo (resourceType), island, height);
		}
	}
}