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

	Body body;
	TerrainManager tm;

	List<float> inventory = new List<float>();
	List<UIResource> inventoryUI = new List<UIResource>();

	public GameObject uiResourcePrefab;

	const float healAmount = 1f;
	const float inventoryPadding = 15f;

	void Awake () {
		instance = this;
	}

	void Start () {
		tm = TerrainManager.instance;
		body = GetComponent<Body> ();

		InitInventory ();

		idleIcon.SetActive (false);
	}

	void InitInventory () {
		foreach (TerrainManager.ResourceInfo resourceInfo in tm.resourceInfos) {
			inventory.Add(0f);

			GameObject newUIResourceObj = (GameObject)Instantiate (uiResourcePrefab, inventoryParent);
			UIResource newUIResource = newUIResourceObj.GetComponent<UIResource> ();
			newUIResource.Init (resourceInfo);

			inventoryUI.Add(newUIResource);
		}

		//temp
		inventory[tm.ResourceTypeToIndex(TerrainManager.ResourceInfo.ResourceType.Green)] = 3.1f;

		UpdateInventoryUI ();
	}

	void Update () {
		if (Input.GetKeyDown (KeyCode.I)) {
			string str = "Inventory - ";
			for (int i = 0; i < tm.resourceInfos.Length; i++) {
				str += tm.ResourceIndexToInfo(i).type.ToString() + ": " + inventory[i].ToString() + ", ";
			}
			print (str);
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

		inventory [tm.ResourceTypeToIndex(resource.info.type)] += (float) countToPickUp;
		List<ResourcePickup> affectedResources = new List<ResourcePickup> {resource};
		List<int> countsToPickUp = new List<int> { countToPickUp };
		tm.ConsumeResources (affectedResources, countsToPickUp);

		UpdateInventoryUI ();
	}

	float GetCarriedResourceCount() {
		float count = 0f;
		foreach (float resourceCount in inventory) {
			count += resourceCount;
		}

		return count;
	}

	public void DropResource (TerrainManager.ResourceInfo resourceInfo) {
		int resourceIndex = tm.ResourceTypeToIndex (resourceInfo.type);

		if (inventory [resourceIndex] < 1f) {
			return;
		}

		Vector2 posV2 = new Vector2 (Mathf.RoundToInt (transform.position.x), Mathf.RoundToInt (transform.position.z));
		if (tm.PadAtPosition (posV2) != null) {
			return;
		}

		if (tm.SpawnResource (transform.position, resourceInfo, body.location) == null) {
			return;
		}
		
		inventory[resourceIndex] -= 1f;
		UpdateInventoryUI ();
	}

	public void PickupWeapon (WeaponPickup newWeapon) {
		tm.PickupWeapon (newWeapon);

		SwitchWeapons (newWeapon.info.ToIndex ());
	}

	public void SwitchWeapons (int weaponIndex) {
		WeaponInfo currentWeaponInfo = body.weapon.info;

		if (currentWeaponInfo.ToIndex() != 0) {
			Vector2 posV2 = new Vector2 (Mathf.RoundToInt (transform.position.x), Mathf.RoundToInt (transform.position.z));
			if (tm.PadAtPosition (posV2) != null) {
				return;
			}

			if (tm.SpawnWeapon (transform.position, currentWeaponInfo, body.location) == null) {
				return;
			}
		}

		body.weapon.info = WeaponInfo.GetInfoFromIndex(weaponIndex);
		body.weapon.UpdateWeapon ();
	}

	public void PickupAugment (AugmentPickup newAugment) {
		tm.PickupAugment (newAugment);

		SwitchAugments (newAugment.info.ToIndex());
	}

	public void SwitchAugments (int augmentIndex) {
		AugmentInfo currentAugmentInfo = body.augment;

		if (currentAugmentInfo.ToIndex() != 0) {
			Vector2 posV2 = new Vector2 (Mathf.RoundToInt (transform.position.x), Mathf.RoundToInt (transform.position.z));
			if (tm.PadAtPosition (posV2) != null) {
				return;
			}

			if (tm.SpawnAugment (transform.position, currentAugmentInfo, body.location) == null) {
				return;
			}
		}

		body.UpdateAugment (AugmentInfo.GetInfoFromIndex(augmentIndex));
	}

	public void Heal(float resourcesConsumed) {
		int resourceIndex = tm.ResourceTypeToIndex (TerrainManager.ResourceInfo.ResourceType.Green);

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
		int resourceIndex = tm.ResourceTypeToIndex (TerrainManager.ResourceInfo.ResourceType.Green);

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
}
