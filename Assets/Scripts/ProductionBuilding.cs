﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProductionBuilding : Building
{
	[HideInInspector]
	public Transform pad;

	public bool hasLimitedSupply = true;
	public int supply;

	public bool autoCycles = false;

	[Header("Production")]
	public ResourceInfo.ResourceType resourceType;
	public int resourcesProducedPerCycle = 1;
	public int turnWaitPerResource;
	public int maxPadCapacity;

	[Header("Animation")]
	public bool movesResources = false;
	public Transform productionCenter;

	protected int turnsUntilNextResource;

	Vector3 spawnPos;

	public override void Init (BuildingInfo info, Island island) {
		turnsUntilNextResource = turnWaitPerResource;

		base.Init (info, island);

		pad = transform.Find("Model").Find ("Pad");

		spawnPos = (pad != null) ? pad.position : transform.position;
	}

	public override void Build () {
		base.Build ();
		if (autoCycles) {
			SetAnimBool ("Producing", true);
		}
	}

	public override void TurnEnd() {
		if (!shouldRecognizeTurn) {
			return;
		}
			
		CompleteTurn ();

		if(shouldProduce) {
			ProduceResources ();
		}
	}

	protected void CompleteTurn() {
		turnsUntilNextResource -= 1;

		if (!autoCycles) {
			SetAnimTrigger ("TurnEnd");
		}
	}

	protected bool shouldProduce {
		get {
			return (turnsUntilNextResource <= 0);
		}
	}

	bool exceedsPadCapacity {
		get {
			if (maxPadCapacity == 0) {
				return false;
			}

			Vector2 posV2 = TerrainManager.PosToV2 (spawnPos);
			if (!ResourcePickup.IsAtPosition (posV2)) {
				return false;
			}
			return (ResourcePickup.GetAtPosition (posV2).gameObjects.Count >= maxPadCapacity);
		}
	}

	protected bool shouldRecognizeTurn {
		get {
			if (state == BuildingState.Active) {
				return true;
			} else {
				if (state == BuildingState.Waiting) {
					if (!ResourcePickup.IsAtPosition (TerrainManager.PosToV2 (spawnPos)) && !TerrainManager.instance.PlayerAtPos (TerrainManager.PosToV2 (spawnPos)) && !exceedsPadCapacity) {
						state = BuildingState.Active;

						ResetAnimTrigger ("TurnEnd");
						SetAnimTrigger ("Reset");
					}
				}

				return false;
			}
		}
	}

	public virtual void ProduceResources() {
		turnsUntilNextResource = turnWaitPerResource;

		for (int i = 0; i < resourcesProducedPerCycle; i++) {
			if (hasLimitedSupply && supply <= 0) {
				break;
			}

			if (exceedsPadCapacity) {
				break;
			}

			TerrainManager.instance.SpawnResource (position: spawnPos, info: ResourceInfo.GetInfoFromType (resourceType), island: island);
			if (hasLimitedSupply) {
				supply -= 1;
			}
		}

		if (movesResources) {
			ResourcePickup.GetAtPosition (TerrainManager.PosToV2 (spawnPos)).AnimateMove (productionCenter.position, false, resourcesProducedPerCycle);
		}

		SavedGame.UpdateBuildingSupply (this);

		if (!autoCycles) {
			state = BuildingState.Waiting;
		}

		if (hasLimitedSupply && supply <= 0) {
			Deactivate ();
		}
	}

	//TODO: some way of knowing that this building is always unstandable
	public void SpawnTop() {
		if (!autoCycles) {
			state = BuildingState.Waiting;
			standable = true;
			SetAnimTrigger ("Skip");
		}
	}
}