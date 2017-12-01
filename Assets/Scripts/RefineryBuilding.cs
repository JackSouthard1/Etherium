using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RefineryBuilding : ProductionBuilding
{
	[Header("Refinery")]
	public ResourceInfo.ResourceType consumedType;
	public int resourcesConsumedPerCycle;

	List<Vector2> adjacentTiles;
	bool hasResources;

	public override void Build () {
		base.Build ();

		adjacentTiles = GetAdjacentTiles ();
		SetAnimBool ("Producing", false);
	}

	public override void TurnEnd () {
		if (!shouldRecognizeTurn) {
			return;
		}

		if (hasResources) {
			if (shouldProduce) {
				ProduceResources ();
			} else {
				CompleteTurn ();
			}
		} else if (turnsUntilNextResource == turnWaitPerResource) {
			hasResources = shouldConsume;
		}
	}

	public override void ProduceResources () {
		hasResources = false;
		SetAnimBool ("Producing", false);
		base.ProduceResources ();
	}

	bool shouldConsume {
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

			TerrainManager.instance.ConsumeResources (resourcesToConsume, amountsToConsume);
			SetAnimBool ("Producing", true);
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
}