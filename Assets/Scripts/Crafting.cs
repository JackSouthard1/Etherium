using System.Collections;
using System.Collections.Generic;
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

	private TerrainManager tm;

	Dictionary<string, Craftable> craftableInfos = new Dictionary<string, Craftable>();

	List<Recipe> recipes = new List<Recipe> ();
	List<Stack> anchors = new List<Stack>();

	void Awake () {
		instance = this;
	}

	void Start () {
		GenerateRecipes ();
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
		
	public void TestForCrafting (ResourcePickup _resource) {
		ResourcePickup anchorResource = _resource;

		if (tm == null) {
			tm = TerrainManager.instance;
		}

		TerrainManager.ResourceInfo.ResourceType tileType = tm.tiles[anchorResource.position].resourceType;
		List<Crafting.Recipe> possibleRecipes = GetPossibleRecipes (new Crafting.Stack(anchorResource.info.type, tileType, anchorResource.gameObjects.Count));

		bool hasRecipe = false;
		Crafting.Recipe confirmedRecipe = new Crafting.Recipe ();;
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
			// Craft
			Craftable craftableInfo = craftableInfos[confirmedRecipe.name];
			Vector3 spawnPos = anchorResource.gameObjects[0].transform.position + new Vector3(craftableInfo.anchorOffset.x, 0, craftableInfo.anchorOffset.y);
			tm.ConsumeResources (affectedResources);

			switch(confirmedRecipe.type) {
				case Recipe.RecipeType.Building:
					BuildingInfo buildingInfo = craftableInfo as BuildingInfo;
					tm.SpawnBuilding (spawnPos, buildingInfo.prefab, buildingInfo.mainColor, buildingInfo.secondaryColor, buildingInfo.alternateColor, anchorResource.island);
					break;
				case Recipe.RecipeType.Weapon:
					WeaponInfo weaponInfo = craftableInfo as WeaponInfo;
					tm.SpawnWeapon(spawnPos, weaponInfo, anchorResource.island);
					break;
				case Recipe.RecipeType.Augment:
					AugmentInfo augmentInfo = craftableInfo as AugmentInfo;
					tm.SpawnAugment (spawnPos, augmentInfo, anchorResource.island);
					break;
			}
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

	public class Craftable {
		public string name;
		public EditorRecipe recipe;
		public Vector2 anchorOffset;
	}

	[System.Serializable]
	public class BuildingInfo : Craftable {
		public GameObject prefab;
		public Color mainColor;
		public Color secondaryColor;
		public Color alternateColor;
	}
}
