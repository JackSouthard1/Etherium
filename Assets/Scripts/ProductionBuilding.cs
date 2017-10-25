using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProductionBuilding : Building
{
	public bool usesTurnedBasedAnimatinon;
	public bool autoCycles = false;

	[Header("Production")]
	public ResourceInfo.ResourceType resourceType;
	public int resourcesProducedPerCycle = 1;
	public int turnWaitPerResource;

	[Header("Refinery")]
	public bool isRefinery;
	public ResourceInfo.ResourceType consumedType;
	public int resourcesConsumedPerCycle;
	public float productionAnimationTime;

	TerrainManager tm;
	[HideInInspector]
	public int turnsUntilNextResource { get; private set; }

	List<Vector2> adjacentTiles;

	Vector3 spawnPos;

	public override void Init (BuildingInfo info, Island island) {
		turnsUntilNextResource = turnWaitPerResource;
		tm = TerrainManager.instance;

		base.Init (info, island);

		if (isRefinery) {
			adjacentTiles = GetAdjacentTiles ();
			if (!usesTurnedBasedAnimatinon) {
				SetAnimTrigger ("Waiting");
			}
		} else if (!usesTurnedBasedAnimatinon) {
			SetAnimTrigger ("Producing");
		}

		spawnPos = (pad != null) ? pad.position : transform.position;
	}

	public override void TurnEnd() {
		if (state == BuildingState.Waiting) {
			if (!ResourcePickup.IsAtPosition (TerrainManager.PosToV2 (spawnPos))) {
				state = BuildingState.Active;
				if (usesTurnedBasedAnimatinon) {
					SetAnimTrigger ("Reset");
				} else {
					SetAnimTrigger ("Producing");
				}
			}
		}

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

			tm.SpawnResource (position: spawnPos, info: ResourceInfo.GetInfoFromType (resourceType), island: island);
			supply -= 1;

			SavedGame.UpdateBuildingSupply (this);
		}

		if (usesTurnedBasedAnimatinon) {
			SetAnimTrigger ("TurnEnd");
		}

		base.TurnEnd ();
	}

	//TODO: some way of knowing that this building is always unstandable
	public void SpawnTop() {
		state = BuildingState.Waiting;
		if (usesTurnedBasedAnimatinon) {
			SetAnimTrigger ("Skip");
		}
		//start here - we are making the function that resets the building when resources are placed on top
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
						amountsToConsume.Add (Mathf.Min(adjacentResource.gameObjects.Count, resourcesConsumedPerCycle - totalResourceCount));

						totalResourceCount += adjacentResource.gameObjects.Count;

						if (totalResourceCount >= resourcesConsumedPerCycle)
							break;
					}
				}
			}

			if (totalResourceCount < resourcesConsumedPerCycle)
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