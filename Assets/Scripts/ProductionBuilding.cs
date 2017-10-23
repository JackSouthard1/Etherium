using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProductionBuilding : Building
{
	[Header("Production")]
	public ResourceInfo.ResourceType resourceType;
	public int turnWaitPerResource;

	[Header("Refinery")]
	public bool isRefinery;
	public ResourceInfo.ResourceType consumedType;
	public int resourcesConsumed;
	public float productionAnimationTime;

	TerrainManager tm;
	[HideInInspector]
	public int turnsUntilNextResource { get; private set; }

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
		if (state != BuildingState.Active)
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
			tm.SpawnResource (position: spawnPos, info: ResourceInfo.GetInfoFromType (resourceType), island: island);
			supply -= 1;

			SavedGame.UpdateBuildingSupply (this);
		}

		base.TurnEnd ();
	}

	bool ConsumeAdjacentResources {
		get {
			List<ResourcePickup> resourcesToConsume = new List<ResourcePickup> ();
			List<int> amountsToConsume = new List<int> ();
			int totalResourceCount = 0;

			foreach (Vector2 adjacentTile in adjacentTiles) {
				if (ResourcePickup.IsAtPosition(adjacentTile)) {
					ResourcePickup adjacentResource = ResourcePickup.GetAtPosition (adjacentTile);
					if (adjacentResource.info.type == consumedType) {
						resourcesToConsume.Add (adjacentResource);
						amountsToConsume.Add (Mathf.Min(adjacentResource.gameObjects.Count, resourcesConsumed - totalResourceCount));

						totalResourceCount += adjacentResource.gameObjects.Count;

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
		Vector2 curPosV2 = TerrainManager.PosToV2 (transform.position);
		List<Vector2> adjacentTiles = new List<Vector2> ();

		adjacentTiles.Add (curPosV2 + Vector2.right);
		adjacentTiles.Add (curPosV2 + Vector2.left);
		adjacentTiles.Add (curPosV2 + Vector2.up);
		adjacentTiles.Add (curPosV2 + Vector2.down);

		return adjacentTiles;
	}

	IEnumerator RefineryProductionAnimation() {
		SetAnimTrigger ("Producing");
		yield return new WaitForSeconds (productionAnimationTime);
		SetAnimTrigger ("Waiting");
	}
}