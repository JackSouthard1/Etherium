using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RecipeUI : MonoBehaviour {
	Image[,] tiles = new Image[2,2];
	Image[,] resources = new Image[2,2];
	Text[,] numbers = new Text[2,2];
	Text recipeName;
	int curRecipe = 0;

	Color defaultTileColor = new Color (200f / 255f, 200f / 255f, 200f / 255f);

	void Start() {
		LoadUI ();
		LoadRecipe ();
	}

	void LoadUI() {
		tiles = GetImagesFromParent (transform.Find ("Tiles"));
		resources = GetImagesFromParent (transform.Find ("Resources"));
		numbers = GetTextsFromParent (transform.Find ("Numbers"));

		recipeName = transform.Find ("RecipeName").GetComponent<Text>();
	}

	Image[,] GetImagesFromParent(Transform parent) {
		Image[,] newImages = new Image[2,2];

		for (int i = 0; i < parent.childCount; i++) {
			string name = parent.GetChild (i).name;

			int x;
			int.TryParse (name.Substring (name.IndexOf('[') + 1, 1), out x);

			int y;
			int.TryParse (name.Substring (name.IndexOf(',') + 1, 1), out y);

			newImages [x, y] = parent.GetChild (i).GetComponent<Image> ();
		}

		return newImages;
	}

	Text[,] GetTextsFromParent(Transform parent) {
		Text[,] newTexts = new Text[2,2];

		for (int i = 0; i < parent.childCount; i++) {
			string name = parent.GetChild (i).name;

			int x;
			int.TryParse (name.Substring (name.IndexOf('[') + 1, 1), out x);

			int y;
			int.TryParse (name.Substring (name.IndexOf(',') + 1, 1), out y);

			newTexts [x, y] = parent.GetChild (i).GetComponent<Text> ();
		}

		return newTexts;
	}

	public void SwitchRecipe(int dir) {
		curRecipe = (curRecipe + dir) % Crafting.instance.recipes.Count;
		if (curRecipe < 0) {
			curRecipe += Crafting.instance.recipes.Count;
		}
		LoadRecipe ();
	}

	void LoadRecipe() {
		for (int x = 0; x < 2; x++) {
			for (int y = 0; y < 2; y++) {
				if (x < Crafting.instance.recipes [curRecipe].resources.GetLength(0) && y < Crafting.instance.recipes [curRecipe].resources.GetLength(1)) {
					Crafting.Stack curStack = Crafting.instance.recipes [curRecipe].resources [x, y];
					tiles [x, y].color = (curStack.tileType != ResourceInfo.ResourceType.None) ? ResourceInfo.GetInfoFromType (curStack.tileType).colorDark : defaultTileColor;
					resources [x, y].sprite = ResourceInfo.GetInfoFromType (curStack.resourceType).sprite;
					numbers [x, y].text = curStack.count.ToString ();
					resources [x, y].gameObject.SetActive (true);
				} else {
					tiles [x, y].color = defaultTileColor;
					resources [x, y].gameObject.SetActive (false);
					numbers [x, y].text = "";
				}
			}
		}

		recipeName.text = Crafting.instance.recipes [curRecipe].name;
	}
}
