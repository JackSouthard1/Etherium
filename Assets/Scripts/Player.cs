using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {
	public static Player instance;

	public Transform inventoryParent;
	public int movesPerResource;

	public int defaultInventorySize;
	int curInventorySize { get { return defaultInventorySize + body.augment.extraInventorySpace;} }

	public GameObject idleIcon;
	public HealthBar playerHealthBar;

	Transform playerTransform;
	TerrainManager tm;

	[HideInInspector]
	public Body body;
	[HideInInspector]
	public List<float> inventory = new List<float>();
	List<UIResource> inventoryUI = new List<UIResource>();

	public GameObject uiResourcePrefab;

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
			SwitchAugments (SavedGame.data.augmentIndex);

			body.health = SavedGame.data.playerHealth;
		} else {
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
		inventory[ResourceInfo.GetIndexFromType(ResourceInfo.ResourceType.Green)] = 3f;

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
		tm.PickupWeapon (newWeapon);

		SwitchWeapons (newWeapon.info.ToIndex ());
	}

	public void SwitchWeapons (int weaponIndex) {
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
		tm.PickupAugment (newAugment);

		SwitchAugments (newAugment.info.ToIndex());
	}

	public void SwitchAugments (int augmentIndex) {
		AugmentInfo currentAugmentInfo = body.augment;

		if (currentAugmentInfo.ToIndex() > 0) {
			Vector2 posV2 = TerrainManager.PosToV2 (playerTransform.position);
			if (tm.PadAtPosition (posV2) != null) {
				return;
			}

			if (tm.SpawnAugment (playerTransform.position, currentAugmentInfo, body.location) == null) {
				return;
			}
		}

		body.UpdateAugment (AugmentInfo.GetInfoFromIndex(augmentIndex));

		if (!GameManager.isLoadingFromSave) {
			GameManager.instance.SaveThisTurn ();
		}
	}

	public void Heal(float resourcesConsumed) {
		int resourceIndex = ResourceInfo.GetIndexFromType (ResourceInfo.ResourceType.Green);

		if (!body.canHeal)
			return;

		float newAmount = inventory [resourceIndex] - resourcesConsumed;
		if (newAmount < 0f)
			return;

		inventory [resourceIndex] = Mathf.Round (newAmount * 10f) / 10f;
		body.Heal (healAmount);

		UpdateInventoryUI ();
	}

	public void Eat() {
		float resourcesPerMove = 1f / ((float) movesPerResource);
		int resourceIndex = ResourceInfo.GetIndexFromType (ResourceInfo.ResourceType.Green);

		float newAmount = inventory [resourceIndex] - resourcesPerMove;
		if (newAmount < 0f) {
			newAmount = 0f;
			body.TakeDamage (0.5f);
		}
		//TODO: better way to fix float innacuracy?
		inventory [resourceIndex] = Mathf.Round(newAmount * 10f) / 10f;

		UpdateInventoryUI ();
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

	public IEnumerator ShowIdleUI() {
		idleIcon.SetActive (true);
		yield return new WaitForSeconds (0.5f);
		idleIcon.SetActive (false);
	}

	public IEnumerator Respawn() {
		if (isTeleporting) {
			yield break;
		}

		yield return StartCoroutine (Teleport(Vector2.zero, "Respawning"));

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
