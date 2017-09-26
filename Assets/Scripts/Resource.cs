using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Resource {
	public TerrainManager.ResourceInfo info;
	public Vector2 position;
	public float originalBaseHeight;
	public List<GameObject> resourceGO = new List<GameObject>();
	public Island island;

	private Crafting crafting;
	private TerrainManager tm;

	public void UpdatePosition () {
		position = new Vector2 (resourceGO[0].transform.position.x, resourceGO[0].transform.position.z);
		originalBaseHeight = resourceGO [0].transform.position.y;
	}

	public Resource (TerrainManager.ResourceInfo _info, GameObject _resourceGO, Island _island) {
		info = _info;
		island = _island;
		resourceGO.Add(_resourceGO);
		if (_resourceGO != null) {
			UpdatePosition ();
		}
	}

	public void TestForCrafting () {
		if (crafting == null) {
			crafting = GameObject.Find ("GameManager").GetComponent<Crafting> ();
		}
		if (tm == null) {
			tm = GameObject.Find ("Terrain").GetComponent<TerrainManager> ();
		}

		List<Crafting.Recipe> possibleRecipes = crafting.GetPossibleRecipes (new Crafting.Stack(info.type, resourceGO.Count));

		Crafting.Recipe confirmedRecipe;
		foreach (var recipe in possibleRecipes) {
			Debug.Log ("Check Possible Recipe");
			int correct = 0;
			for (int y = 0; y < recipe.resources.GetLength(1); y++) {
				for (int x = 0; x < recipe.resources.GetLength(0); x++) {
					Vector2 posToCheck = new Vector2 (position.x + x, position.y + y);
					if (tm.ResourceAtPosition(posToCheck)) {
						Resource resourceAtPos = tm.GetResourceAtPosition (posToCheck);
						if (resourceAtPos.info.type == recipe.resources [x, y].type && resourceAtPos.resourceGO.Count == recipe.resources [x, y].count) {
							correct++;
						}
					}
				}
			}
			Debug.Log ("Correct: " + correct);
			if (correct >= recipe.resources.Length) {
				confirmedRecipe = recipe;
				Debug.Log ("Build");
			}
 		}
	}
}
