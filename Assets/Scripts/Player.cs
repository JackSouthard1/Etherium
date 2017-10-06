using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {
	public Transform inventoryParent;
	public int movesPerResource;

	public GameObject idleIcon;

	Body body;
	TerrainManager tm;

	List<float> inventory = new List<float>();
	List<UIResource> inventoryUI = new List<UIResource>();

	public GameObject uiResourcePrefab;

	const float healAmount = 1f;
	const float inventoryPadding = 15f;

	void Start () {
		tm = GameObject.Find ("Terrain").GetComponent<TerrainManager> ();
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

	public void CollectResource (Resource resource) {
		int stackCount = resource.resourceGO.Count;
			
		resource.island.resources.Remove (resource);
	
		tm.resources.Remove (resource.position);
		for (int i = 0; i < stackCount; i++) {
			Destroy (resource.resourceGO [i]);
		}

		inventory [tm.ResourceTypeToIndex(resource.info.type)] += (float) stackCount;
		List<Resource> affectedResources = new List<Resource> {resource};
		tm.ConsumeResources (affectedResources);

		UpdateInventoryUI ();
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
