using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {
	public static Player instance;

	public Transform inventoryParent;

	public int defaultInventorySize;
	int curInventorySize { get { return defaultInventorySize + body.GetTotalBoostValue (AugmentInfo.BoostType.Inventory);} }

	public GameObject idleIcon;
	public Animator itemsUI;
	public HealthBar playerHealthBar;

	Transform playerTransform;
	TerrainManager tm;

	[HideInInspector]
	public Body body;
	[HideInInspector]
	public List<float> inventory = new List<float>();
	List<UIResource> inventoryUI = new List<UIResource>();
	[HideInInspector]
	public List<int> edibleResources = new List<int>();

	public GameObject uiResourcePrefab;

	[HideInInspector]
	public TerrainManager.Tile spawnTile;

	const float healAmount = 1f;
	const float inventoryPadding = 15f;
	const float teleportTime = 3f;
	const float animationTime = 1f;

	bool isTeleporting;

	void Awake () {
		instance = this;
	}

	void Start () {
		tm = TerrainManager.instance;

		idleIcon.SetActive (false);

		//TODO: maybe refactor the resource type system so that the enum only represents color and there is an int for the level
		edibleResources.Add(ResourceInfo.GetIndexFromType(ResourceInfo.ResourceType.G1));
		edibleResources.Add(ResourceInfo.GetIndexFromType(ResourceInfo.ResourceType.G2));
		edibleResources.Add(ResourceInfo.GetIndexFromType(ResourceInfo.ResourceType.G3));
	}

	public void Init() {
		playerTransform = GameObject.Find ("Player").transform;
		body = playerTransform.GetComponent<Body> ();
		body.healthBar = playerHealthBar;

		InitInventory ();

		if (GameManager.isLoadingFromSave) {
			playerTransform.position = new Vector3 (SavedGame.data.playerPosition.x, 0, SavedGame.data.playerPosition.y);
			inventory = SavedGame.data.inventory;

			SwitchWeapons (SavedGame.data.weaponIndex);
			InitAugments (SavedGame.data.augmentIndexes);

			body.health = SavedGame.data.playerHealth;
		} else {
			playerTransform.position = new Vector3 (tm.mapCenter.x, 0f, tm.mapCenter.y);
			GameManager.instance.SaveThisTurn ();
		}

		UpdateInventoryUI ();
	}

	void InitInventory () {
		foreach (ResourceInfo resourceInfo in tm.resourceInfos) {
			inventory.Add(0f);

			GameObject newUIResourceObj = (GameObject)Instantiate (uiResourcePrefab, inventoryParent);
			UIResource newUIResource = newUIResourceObj.GetComponent<UIResource> ();
			newUIResource.Init (resourceInfo);

			inventoryUI.Add(newUIResource);
		}

		ResetInventory ();
	}

	void ResetInventory () {
		for (int i = 0; i < inventory.Count; i++) {
			inventory [i] = 0f;
		}

		//temp
		inventory[ResourceInfo.GetIndexFromType(ResourceInfo.ResourceType.G1)] = 3f;

		UpdateInventoryUI ();
	}

	public void PickUpPickup() {
		Vector2 playerPos = TerrainManager.PosToV2 (playerTransform.position);
		if (ResourcePickup.IsAtPosition (playerPos)) {
			CollectResource (ResourcePickup.GetAtPosition (playerPos));
		} else if (WeaponPickup.IsAtPosition (playerPos)) {
			PickupWeapon (WeaponPickup.GetAtPosition (playerPos));
		} else if (AugmentPickup.IsAtPosition (playerPos)) {
			PickupAugment (AugmentPickup.GetAtPosition (playerPos));
		}
	}

	public void CollectResource (ResourcePickup resource) {
		int stackCount = resource.gameObjects.Count;
		int countToPickUp = stackCount;

		float carriedResourceCount = GetCarriedResourceCount ();
		if (carriedResourceCount + stackCount > curInventorySize) {
			int remainingSpace = Mathf.FloorToInt(curInventorySize - carriedResourceCount);
			if (remainingSpace <= 0) {
				return;
			} else {
				countToPickUp = remainingSpace;
			}
		}
			
		if (countToPickUp >= stackCount) {
			resource.island.pickups.Remove (resource);
			tm.pickups.Remove (resource.position);
		}

		inventory [resource.info.ToIndex()] += (float) countToPickUp;
		List<ResourcePickup> affectedResources = new List<ResourcePickup> {resource};
		List<int> countsToPickUp = new List<int> { countToPickUp };
		tm.ConsumeResources (affectedResources, countsToPickUp);

		GameManager.instance.SaveThisTurn ();
		UpdateInventoryUI ();
	}

	float GetCarriedResourceCount() {
		float count = 0f;
		foreach (float resourceCount in inventory) {
			count += resourceCount;
		}

		return count;
	}

	public void DropResource (ResourceInfo resourceInfo) {
		int resourceIndex = resourceInfo.ToIndex();

		if (inventory [resourceIndex] < 1f) {
			return;
		}

		Vector2 posV2 = TerrainManager.PosToV2 (playerTransform.position);
		if (tm.PadAtPosition (posV2) != null || tm.GetBuildingAtPosition(posV2) != null) {
			return;
		}

		if (tm.SpawnResource (playerTransform.position, resourceInfo, body.location) == null) {
			return;
		}
		
		inventory[resourceIndex] -= 1f;
		GameManager.instance.SaveThisTurn ();
		UpdateInventoryUI ();
	}

	public void PickupWeapon (WeaponPickup newWeapon) {
		if (body.weapon.info.ToIndex () != 0) {
			return;
		}

		tm.PickupWeapon (newWeapon);
		SwitchWeapons (newWeapon.info.ToIndex ());
	}

	public void DropWeapon() {
		SwitchWeapons (0);
	}

	void SwitchWeapons (int weaponIndex) {
		WeaponInfo currentWeaponInfo = body.weapon.info;

		if (currentWeaponInfo.ToIndex() != 0) {
			Vector2 posV2 = TerrainManager.PosToV2 (playerTransform.position);
			if (tm.PadAtPosition (posV2) != null) {
				return;
			}

			if (tm.SpawnWeapon (playerTransform.position, currentWeaponInfo, body.location) == null) {
				return;
			}
		}

		body.weapon.info = WeaponInfo.GetInfoFromIndex(weaponIndex);
		body.weapon.UpdateWeapon ();

		if (!GameManager.isLoadingFromSave) {
			GameManager.instance.SaveThisTurn ();
		}
	}

	public void PickupAugment (AugmentPickup newAugment) {
		if (body.UpdateAugment (newAugment.info)) {
			tm.PickupAugment (newAugment);
		}
	}

	public void InitAugments (List<int> augmentIndexes) {
		for (int i = 0; i < augmentIndexes.Count; i++) {
			body.augmentSlots [i].UpdateAugment (AugmentInfo.GetInfoFromIndex (augmentIndexes [i]));
		}
	}

	public void DropAugment (int slotIndex) {
		if (!body.augmentSlots[slotIndex].isEmpty) {
			Vector2 posV2 = TerrainManager.PosToV2 (playerTransform.position);
			if (tm.PadAtPosition (posV2) != null) {
				return;
			}

			if (tm.SpawnAugment (playerTransform.position, body.augmentSlots[slotIndex].augment, body.location) == null) {
				return;
			}
		}

		body.augmentSlots [slotIndex].UpdateAugment (AugmentInfo.GetInfoFromIndex(0));

		if (!GameManager.isLoadingFromSave) {
			GameManager.instance.SaveThisTurn ();
		}
	}

	public void Heal() {
		float newResourceAmount;
		int resourceIndex = GetLowestFoodIndex(1f, out newResourceAmount);

		if (!body.canHeal)
			return;

		inventory [resourceIndex] = Mathf.Round (newResourceAmount * 1000f) / 1000f;
		body.Heal (healAmount);

		UpdateInventoryUI ();
	}

	public void Eat() {
		float newResourceAmount = 0f;
		int resourceIndex = GetLowestFoodIndex (0.1f, out newResourceAmount);

		if (resourceIndex == -1) {
			body.TakeDamage (0.5f);
		} else {
			//TODO: better way to fix float innacuracy?
			inventory [resourceIndex] = Mathf.Round (newResourceAmount * 1000f) / 1000f;
		}

		UpdateInventoryUI ();
	}

	int GetLowestFoodIndex (float baseAmountToConsume, out float newResourceAmount) {
		newResourceAmount = 0f;

		for (int i = 0; i < edibleResources.Count; i++) {
			float multiplier = (i != 0) ? (3f * i) : 1;
			newResourceAmount = inventory [edibleResources[i]] - (baseAmountToConsume  / multiplier);

			if (newResourceAmount >= 0f) {
				return edibleResources[i];
			}
		}

		newResourceAmount = 0f;
		return -1;
	}

	void UpdateInventoryUI () {
		float leftAnchor = 0f;
		for (int i = 0; i < inventory.Count; i++) {
			UIResource uiResource = inventoryUI [i];
			uiResource.amount = inventory[i];
			uiResource.UpdateVisual();

			uiResource.gameObject.GetComponent<RectTransform> ().offsetMin = new Vector2 (leftAnchor, 0f);

			if(uiResource.totalWidth > 0)
				leftAnchor += (uiResource.totalWidth + inventoryPadding);
		}
	}

	public void OpenItemsUI (bool open) {
		itemsUI.SetBool ("Open", open);
	}

	public IEnumerator ShowIdleUI() {
		idleIcon.SetActive (true);
		yield return new WaitForSeconds (0.5f);
		idleIcon.SetActive (false);
	}

	public IEnumerator Respawn() {
		if (isTeleporting) {
			yield break;
		}
			
		yield return StartCoroutine (Teleport(TerrainManager.PosToV2(spawnTile.tile.transform.position), "Respawning"));

		ResetInventory ();
		body.ResetHealth ();
	}

	public IEnumerator Teleport(Vector2 targetTile, string animationKey) {
		isTeleporting = true;
		GameManager.instance.TransitionStart ();
		body.anim.SetBool (animationKey, true);

		yield return new WaitForSeconds (animationTime);

		Vector3 startingPos = body.transform.position;
		if (body.location != null) {
			body.location.PlayerExitIsland ();
		}
		body.MoveToPos (TerrainManager.instance.GetTileAtPosition(targetTile).transform.position, Quaternion.identity, true);
		Vector3 endingPos = body.transform.position;

		float transitionTime = teleportTime - (2f * animationTime);
		StartCoroutine(CameraController.instance.SmoothMoveOverTime (startingPos, endingPos, transitionTime));

		yield return new WaitForSeconds (transitionTime);
		
		body.anim.SetBool (animationKey, false);

		yield return new WaitForSeconds (animationTime);

		GameManager.instance.TransitionEnd ();
		isTeleporting = false;
	}
}
