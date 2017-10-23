using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Crafting : MonoBehaviour {
	public static Crafting instance;

	[Header("Crafting")]
	public List<BuildingInfo> buildingInfos;
	public List<WeaponInfo> weaponInfos;
	public List<AugmentInfo> augmentInfos;

	[System.Serializable]
	public class EditorRecipe {
		public List<EditorRecipeRow> rows = new List<EditorRecipeRow>();
	}

	[System.Serializable]
	public class EditorRecipeRow {
		public List<Crafting.Stack> columns = new List<Crafting.Stack>();
	}

	[Header("Building")]
	public GameObject buildBar;
	public float buildTime = 1.5f;
	[HideInInspector]
	public bool isBuilding = false;
	Transform barUI;

	Dictionary<string, Craftable> craftableInfos = new Dictionary<string, Craftable>();
	Dictionary<ResourcePickup, Building> blueprints = new Dictionary<ResourcePickup, Building>();

	List<Recipe> recipes = new List<Recipe> ();
	List<Stack> anchors = new List<Stack>();

	Camera gameCam;
	TerrainManager tm;

	void Awake () {
		instance = this;
		tm = TerrainManager.instance;
		gameCam = Camera.main;
		barUI = buildBar.transform.Find ("Canvas").Find ("Bar");
		GenerateRecipes ();
		buildBar.SetActive (false);

		TouchManager.instance.OnTouchDown += CheckForBuildStart;
		TouchManager.instance.OnTouchUp += CancelBuild;
	}

	public struct Recipe {
		public enum RecipeType
		{
			Building = 0,
			Weapon = 1,
			Augment = 2
		}

		public string name;
		public Stack[,] resources;
		public RecipeType type;

		public Recipe (string _name, Stack[,] _resources, RecipeType _type) {
			name = _name;
			resources = _resources;
			type = _type;
		}
	}

	[System.Serializable]
	public struct Stack {
		public TerrainManager.ResourceInfo.ResourceType resourceType;
		public TerrainManager.ResourceInfo.ResourceType tileType;
		public int count;

		public Stack (TerrainManager.ResourceInfo.ResourceType _resourceType, TerrainManager.ResourceInfo.ResourceType _tileType, int _count) {
			resourceType = _resourceType;
			tileType = _tileType;
			count = _count;
		}
	}

	public List<Recipe> GetPossibleRecipes (Stack stack) {
		List<Recipe> possibleRecipes = new List<Recipe> ();

		if (anchors.Contains (stack)) {
			for (int i = 0; i < anchors.Count; i++) {
				if (anchors[i].Equals(stack)) {
					possibleRecipes.Add (recipes [i]);
				}
			}
		}

		return possibleRecipes;
	}
		
	public void TestForCrafting () {
		foreach (Vector2 key in tm.pickups.Keys.ToList()) {
			if (ResourcePickup.IsAtPosition(key)) {
				ResourcePickup resourceToTest = tm.pickups [key] as ResourcePickup;
				Vector3 spawnPos;
				Craftable craftableInfo = GetCraftableForResource (resourceToTest, out spawnPos);

				if (craftableInfo is BuildingInfo) {
					BuildingInfo buildingInfo = craftableInfo as BuildingInfo;

					if (!blueprints.ContainsKey (resourceToTest)) {
						Building newBuilding = tm.SpawnBuilding (spawnPos, buildingInfo.prefab, buildingInfo, resourceToTest.island);
						blueprints.Add (resourceToTest, newBuilding);
					}
				} else if (craftableInfo is WeaponInfo) {
					WeaponInfo weaponInfo = craftableInfo as WeaponInfo;
					tm.SpawnWeapon (spawnPos, weaponInfo, resourceToTest.island);
				} else if (craftableInfo is AugmentInfo) {
					AugmentInfo augmentInfo = craftableInfo as AugmentInfo;
					tm.SpawnAugment (spawnPos, augmentInfo, resourceToTest.island);
				}
			}
		}
	}

	public void TestForBlueprints () {
		List<ResourcePickup> blueprintKeysToRemove = blueprints.Keys.ToList ();

		foreach (Vector2 key in tm.pickups.Keys.ToList()) {
			if (ResourcePickup.IsAtPosition(key)) {
				ResourcePickup resourceToTest = tm.pickups [key] as ResourcePickup;

				Vector3 spawnPos;
				Craftable craftableInfo = GetCraftableForResource (resourceToTest, out spawnPos);

				if (craftableInfo is BuildingInfo) {
					blueprintKeysToRemove.Remove (resourceToTest);
				}
			}
		}

		foreach (ResourcePickup key in blueprintKeysToRemove) {
			Destroy(blueprints[key].gameObject);
			blueprints.Remove (key);
		}
	}

	Craftable GetCraftableForResource(ResourcePickup anchorResource, out Vector3 spawnPos) {
		TerrainManager.ResourceInfo.ResourceType tileType = tm.tiles[anchorResource.position].resourceType;
		List<Crafting.Recipe> possibleRecipes = GetPossibleRecipes (new Stack (anchorResource.info.type, tileType, anchorResource.gameObjects.Count));

		bool hasRecipe = false;
		Crafting.Recipe confirmedRecipe = new Crafting.Recipe ();
		List<ResourcePickup> affectedResources = new List<ResourcePickup> ();

		foreach (var recipe in possibleRecipes) {
			int correct = 0;
			for (int y = 0; y < recipe.resources.GetLength(1); y++) {
				for (int x = 0; x < recipe.resources.GetLength(0); x++) {
					Vector2 posToCheck = new Vector2 (anchorResource.position.x + x, anchorResource.position.y + y);
					if (ResourcePickup.IsAtPosition(posToCheck) && tm.PadAtPosition(posToCheck) == null) {
						ResourcePickup resourceAtPos = ResourcePickup.GetAtPosition(posToCheck);
						if (resourceAtPos.island.buildable) {
							if (resourceAtPos.info.type == recipe.resources [x, y].resourceType && resourceAtPos.gameObjects.Count == recipe.resources [x, y].count) {
								// check tile
								TerrainManager.ResourceInfo.ResourceType curTileType = tm.tiles [posToCheck].resourceType;
								if (curTileType == recipe.resources [x, y].tileType) {
									affectedResources.Add (resourceAtPos);
									correct++;
								}
							}
						}
					}
				}
			}

			if (correct >= recipe.resources.Length) {
				hasRecipe = true;
				confirmedRecipe = recipe;
				break;
			}
		}

		if (hasRecipe) {
			Craftable craftableInfo = craftableInfos [confirmedRecipe.name];
			spawnPos = anchorResource.gameObjects[0].transform.position + new Vector3(craftableInfo.anchorOffset.x, 0, craftableInfo.anchorOffset.y);

			if (confirmedRecipe.type != Recipe.RecipeType.Building) {
				tm.ConsumeResources (affectedResources);
			}

			return craftableInfo;
			
		} else {
			spawnPos = Vector3.zero;
			return null;
		}
	}

	void GenerateRecipes () {
		foreach (BuildingInfo buildingInfo in buildingInfos) {
			AddRecipe (buildingInfo.name, buildingInfo.recipe, Recipe.RecipeType.Building);
			craftableInfos.Add (buildingInfo.name, buildingInfo);
		}
		foreach (WeaponInfo weaponInfo in weaponInfos) {
			if (weaponInfo.ToIndex () == 0)
				continue;
			
			AddRecipe (weaponInfo.name, weaponInfo.recipe, Recipe.RecipeType.Weapon);
			craftableInfos.Add (weaponInfo.name, weaponInfo);
		}
		foreach (AugmentInfo augmentInfo in augmentInfos) {
			if (augmentInfo.ToIndex () == 0)
				continue;
			
			AddRecipe (augmentInfo.name, augmentInfo.recipe, Recipe.RecipeType.Augment);
			craftableInfos.Add (augmentInfo.name, augmentInfo);
		}
	}

	void AddRecipe (string name, EditorRecipe editorRecipe, Recipe.RecipeType type) {
		Stack[,] recipe = new Stack[editorRecipe.rows[0].columns.Count,editorRecipe.rows.Count];
		for (int y = 0; y < editorRecipe.rows.Count; y++) {
			for (int x = 0; x < editorRecipe.rows [y].columns.Count; x++) {
				recipe [x, y] = editorRecipe.rows [y].columns [x];
			}
		}

		recipes.Add (new Recipe (name, recipe, type));
		anchors.Add (recipe [0, 0]);
	}

	void CheckForBuildStart () {
		if (TouchManager.touchCount != 1) {
			return;
		}

		Vector2 touchPos;
		if (Input.touchCount == 1) {
			touchPos = Input.GetTouch (0).position;
		} else {
			touchPos = Input.mousePosition;
		}

		RaycastHit hitInfo;
		Physics.Raycast (gameCam.ScreenPointToRay (touchPos), out hitInfo, 20f);
		if (hitInfo.collider != null) {
			Building building = hitInfo.collider.gameObject.GetComponent<Building> ();
			if (building != null) {
				if (building.state != Building.BuildingState.Active && !building.IsPlayerInBuilding) {
					StartCoroutine (BuildBuilding (building));
				}
			}
		}
	}

	IEnumerator BuildBuilding(Building building) {
		isBuilding = true;
		buildBar.transform.position = building.gameObject.transform.position - new Vector3 (building.info.anchorOffset.x, 0, building.info.anchorOffset.y);
		buildBar.gameObject.SetActive (true);
		barUI.transform.localScale = new Vector3 (0, 1, 1);

		float endTime = Time.time + buildTime;

		while (isBuilding && (Time.time < endTime)) {
			float timeLeft = endTime - Time.time;
			barUI.transform.localScale = new Vector3 (1 - (timeLeft / buildTime), 1, 1);
			yield return null;
		}

		if (isBuilding) {
			if (building.state == Building.BuildingState.Blueprint) {
				blueprints.Remove (ResourcePickup.GetAtPosition (building.coveredTiles [0]));
				tm.BuildBuilding (building);
			} else if (building.state == Building.BuildingState.Inactive) {
				tm.BreakDownBuilding (building);
			}
		}

		isBuilding = false;
		buildBar.gameObject.SetActive (false);
		SavedGame.UpdateBuildings ();
	}

	void CancelBuild() {
		isBuilding = false;
	}
}

public class Craftable {
	public string name;
	public Crafting.EditorRecipe recipe;
	public Vector2 anchorOffset;
}

[System.Serializable]
public class BuildingInfo : Craftable {
	public GameObject prefab;
	public Color mainColor;
	public Color secondaryColor;
	public Color alternateColor;
}