using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crafting : MonoBehaviour {
	[Header("Crafting")]
	public List<BuildingInfo> buildingInfos;

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

		// basic farm
		Stack[,] basicFarm = new Stack[1,1];
		basicFarm [0, 0] = new Stack (TerrainManager.ResourceInfo.ResourceType.Yellow, TerrainManager.ResourceInfo.ResourceType.Green, 2);

		recipes.Add(new Recipe ("BasicFarm", basicFarm));
		anchors.Add (basicFarm [0, 0]);

		// generator
		Stack[,] generator = new Stack[2,2];
		generator [0, 0] = new Stack (TerrainManager.ResourceInfo.ResourceType.Yellow, TerrainManager.ResourceInfo.ResourceType.Blue, 2);
		generator [1, 0] = new Stack (TerrainManager.ResourceInfo.ResourceType.Purple, TerrainManager.ResourceInfo.ResourceType.None, 1);
		generator [0, 1] = new Stack (TerrainManager.ResourceInfo.ResourceType.Blue, TerrainManager.ResourceInfo.ResourceType.None, 3);
		generator [1, 1] = new Stack (TerrainManager.ResourceInfo.ResourceType.Green, TerrainManager.ResourceInfo.ResourceType.None, 1);

		recipes.Add(new Recipe ("Generator", generator));
		anchors.Add (generator [0, 0]);

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
			tm = GameObject.Find ("Terrain").GetComponent<TerrainManager> ();
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
			tm.SpawnBuilding(anchorResource.resourceGO[0].transform.position, buildingInfo.prefab, buildingInfo.mainColor, buildingInfo.secondaryColor);

			tm.ResourceConsumed (affectedResources);
		}
	}

	void GenerateDictionary () {
		foreach (var buildingInfo in buildingInfos) {
			buildings.Add (buildingInfo.name, new BuildingInfo(buildingInfo.name, buildingInfo.prefab, buildingInfo.mainColor, buildingInfo.secondaryColor));
		}
	}

	[System.Serializable]
	public struct BuildingInfo {
		public string name;
		public GameObject prefab;
		public Color mainColor;
		public Color secondaryColor;

		public BuildingInfo (string _name, GameObject _prefab, Color _mainColor, Color _secondaryColor) {
			name = _name;
			prefab = _prefab;
			mainColor = _mainColor;
			secondaryColor = _secondaryColor;
		}
	}
}
