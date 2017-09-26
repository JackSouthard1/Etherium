using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crafting : MonoBehaviour {
	[Header("Crafting")]
	public List<Recipe> recipes = new List<Recipe> ();
	public List<Stack> anchors = new List<Stack>();

	void Start () {
		// *** buildings ***
		// basic farm
		Stack[,] basicFarm = new Stack[2,2];
		basicFarm [0, 0] = new Stack (TerrainManager.ResourceInfo.ResourceType.Green, 2);
		basicFarm [1, 0] = new Stack (TerrainManager.ResourceInfo.ResourceType.Yellow, 1);
		basicFarm [0, 1] = new Stack (TerrainManager.ResourceInfo.ResourceType.Purple, 1);
		basicFarm [1, 1] = new Stack (TerrainManager.ResourceInfo.ResourceType.Purple, 2);

		recipes.Add(new Recipe (basicFarm));
		anchors.Add (basicFarm [0, 0]);

		// *** weapons ***
	}

	public struct Recipe {
		public Stack[,] resources;

		public Recipe (Stack[,] _resources) {
			resources = _resources;
		}
	}

	public struct Stack {
		public TerrainManager.ResourceInfo.ResourceType type;
		public int count;

		public Stack (TerrainManager.ResourceInfo.ResourceType _type, int _count) {
			type = _type;
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
}
