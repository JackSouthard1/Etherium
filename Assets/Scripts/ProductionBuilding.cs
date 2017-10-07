using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProductionBuilding : Building
{
	[Header("Production")]
	public TerrainManager.ResourceInfo.ResourceType resourceType;
	public int turnWaitPerResource;

	[Header("Refinery")]
	public bool isRefinery;
	public TerrainManager.ResourceInfo.ResourceType consumedType;
	public int resourcesConsumed;
	public float productionAnimationTime;

	TerrainManager tm;
	int turnsUntilNextResource;

	List<Vector2> adjacentTiles;

	//has to be awake because base class has Start
	void Awake () {
		turnsUntilNextResource = turnWaitPerResource;
		tm = TerrainManager.instance;

		if (isRefinery) {
			adjacentTiles = GetAdjacentTiles ();
			SetAnimTrigger ("Waiting");
		} else {
			SetAnimTrigger ("Producing");
		}
	}

	public override void TurnEnd() {
		if (!isActive)
			return;

		turnsUntilNextResource -= 1;
		if (turnsUntilNextResource <= 0) {
			turnsUntilNextResource = turnWaitPerResource;

			if (isRefinery) {
				if (!ConsumeAdjacentResources) {
					return;
				}

				if (anim != null) {
					StartCoroutine (RefineryProductionAnimation ());
				}
			}

			Vector3 spawnPos = (pad != null) ? pad.position : transform.position;
			float spawnHeight = (pad != null) ? pad.position.y : height;
			tm.SpawnResource (position: spawnPos, info: tm.ResourceTypeToInfo (resourceType), island: island, startingHeight: spawnHeight);

			supply -= 1;
		}

		base.TurnEnd ();
	}

	bool ConsumeAdjacentResources {
		get {
			List<Resource> resourcesToConsume = new List<Resource> ();
			List<int> amountsToConsume = new List<int> ();
			int totalResourceCount = 0;

			foreach (Vector2 adjacentTile in adjacentTiles) {
				Resource adjacentResource = tm.GetResourceAtPosition (adjacentTile);
				if (adjacentResource != null) {
					if (adjacentResource.info.type == consumedType) {
						resourcesToConsume.Add (adjacentResource);
						amountsToConsume.Add (Mathf.Min(adjacentResource.resourceGO.Count, resourcesConsumed - totalResourceCount));

						totalResourceCount += adjacentResource.resourceGO.Count;

						if (totalResourceCount >= resourcesConsumed)
							break;
					}
				}
			}

			if (totalResourceCount < resourcesConsumed)
				return false;

			tm.ConsumeResources (resourcesToConsume, amountsToConsume);
			return true;
		}
	}

	List<Vector2> GetAdjacentTiles() {
		Vector2 curPosV2 = new Vector2 (Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.z));
		List<Vector2> adjacentTiles = new List<Vector2> ();

		adjacentTiles.Add (curPosV2 + new Vector2(1f, 0f));
		adjacentTiles.Add (curPosV2 + new Vector2(-1f, 0f));
		adjacentTiles.Add (curPosV2 + new Vector2(0f, 1f));
		adjacentTiles.Add (curPosV2 + new Vector2(0f, -1f));

		return adjacentTiles;
	}

	IEnumerator RefineryProductionAnimation() {
		SetAnimTrigger ("Producing");
		yield return new WaitForSeconds (productionAnimationTime);
		SetAnimTrigger ("Waiting");
	}
}