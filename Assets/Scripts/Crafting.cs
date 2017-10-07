using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crafting : MonoBehaviour {
	[Header("Crafting")]
	public List<BuildingInfo> buildingInfos;

	[System.Serializable]
	public class EditorRecipe {
		public List<EditorRecipeRow> rows = new List<EditorRecipeRow>();
	}

	[System.Serializable]
	public class EditorRecipeRow {
		public List<Crafting.Stack> columns = new List<Crafting.Stack>();
	}

	private TerrainManager tm;

	[HideInInspector]
	public Dictionary<string, BuildingInfo> buildings = new Dictionary<string, BuildingInfo>();
	[HideInInspector]
	public List<Recipe> recipes = new List<Recipe> ();
	[HideInInspector]
	public List<Stack> anchors = new List<Stack>();

	void Start () {
		GenerateDictionary ();
		// *** buildings ***

		GenerateRecipes ();

		// *** weapons ***
	}

	public struct Recipe {
		public string name;
		public Stack[,] resources;

		public Recipe (string _name, Stack[,] _resources) {
			name = _name;
			resources = _resources;
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

			List<Stack> matchingAnchors = anchors.FindAll (DoesAnchorsContainStack);

			foreach (var anchor in matchingAnchors) {
				int index = matchingAnchors.IndexOf (anchor);
				possibleRecipes.Add (recipes[index]);
			}
		}

		return possibleRecipes;
	}

	private bool DoesAnchorsContainStack (Stack stack) {
		if (anchors.Contains(stack)) {
			return true;
		} else {
			return false;
		}

	}

	public void TestForCrafting (Resource _resource) {
		Resource anchorResource = _resource;

		if (tm == null) {
			tm = TerrainManager.instance;
		}

		TerrainManager.ResourceInfo.ResourceType tileType = tm.tiles[anchorResource.position].resourceType;
		List<Crafting.Recipe> possibleRecipes = GetPossibleRecipes (new Crafting.Stack(anchorResource.info.type, tileType, anchorResource.resourceGO.Count));

		bool hasRecipe = false;
		Crafting.Recipe confirmedRecipe = new Crafting.Recipe ();;
		List<Resource> affectedResources = new List<Resource> ();

		foreach (var recipe in possibleRecipes) {
			int correct = 0;
			for (int y = 0; y < recipe.resources.GetLength(1); y++) {
				for (int x = 0; x < recipe.resources.GetLength(0); x++) {
					Vector2 posToCheck = new Vector2 (anchorResource.position.x + x, anchorResource.position.y + y);
					if (tm.ResourceAtPosition(posToCheck)) {
						Resource resourceAtPos = tm.GetResourceAtPosition (posToCheck);
						if (resourceAtPos.info.type == recipe.resources [x, y].resourceType && resourceAtPos.resourceGO.Count == recipe.resources [x, y].count) {
							// check tile
							TerrainManager.ResourceInfo.ResourceType curTileType = tm.tiles[posToCheck].resourceType;
							if (curTileType == recipe.resources[x,y].tileType) {
								affectedResources.Add (resourceAtPos);
								correct++;
							}
						}
					}
				}
			}

			if (correct >= recipe.resources.Length) {
				hasRecipe = true;
				confirmedRecipe = recipe;
			}
		}

		if (hasRecipe) {
			// Craft
			Crafting.BuildingInfo buildingInfo = buildings[confirmedRecipe.name];
			Vector3 spawnPos = anchorResource.resourceGO[0].transform.position + new Vector3 (buildingInfo.anchorOffset.x, 0, buildingInfo.anchorOffset.y);
			tm.SpawnBuilding(spawnPos, buildingInfo.prefab, buildingInfo.mainColor, buildingInfo.secondaryColor, buildingInfo.alternateColor, anchorResource.island);

			tm.ConsumeResources (affectedResources);
		}
	}

	void GenerateDictionary () {
		foreach (BuildingInfo buildingInfo in buildingInfos) {
			buildings.Add (buildingInfo.name, buildingInfo);
		}
	}

	void GenerateRecipes () {
		foreach (BuildingInfo buildingInfo in buildingInfos) {
			EditorRecipe editorRecipe = buildingInfo.recipe;

			Stack[,] recipe = new Stack[editorRecipe.rows[0].columns.Count,editorRecipe.rows.Count];
			for (int y = 0; y < editorRecipe.rows.Count; y++) {
				for (int x = 0; x < editorRecipe.rows [y].columns.Count; x++) {
					recipe [x, y] = editorRecipe.rows [y].columns [x];
				}
			}

			recipes.Add (new Recipe (buildingInfo.name, recipe));
			anchors.Add (recipe [0, 0]);
		}
	}

	[System.Serializable]
	public struct BuildingInfo {
		public string name;
		public GameObject prefab;
		public Color mainColor;
		public Color secondaryColor;
		public Color alternateColor;

		public EditorRecipe recipe;
		public Vector2 anchorOffset;
	}
}
