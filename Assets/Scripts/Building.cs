using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : MonoBehaviour {

	public enum BuildingState
	{
		Blueprint = 0,
		Active = 1,
		Inactive = 2
	}

	public bool standable = false;
	public float height = 0f;

	public Island island;

	public Transform pad;

	public int supply;

	public BuildingState state;

	public Animator anim;

	const float blueprintAlpha = 0.5f;
	Color padColor = new Color(0.25f, 0.95f, 0.95f, 1f);

	[HideInInspector]
	public List<Vector2> coveredTiles = new List<Vector2>();
	BuildingInfo info;

	//has to be in Start because subclasses use Awake
	void Start () {
		pad = transform.Find("Model").Find ("Pad");
	}

	public void Init(BuildingInfo info, Island island) {
		this.info = info;
		this.island = island;

		Crafting.EditorRecipe recipe = info.recipe;
		Vector2 anchorPos = TerrainManager.PosToV2 (transform.position) - info.anchorOffset;
		for (int y = 0; y < recipe.rows.Count; y++) {
			for (int x = 0; x < recipe.rows[y].columns.Count; x++) {
				coveredTiles.Add(anchorPos + new Vector2 (x, y));
			}
		}
	}

	public void CreateBlueprint() {
		state = BuildingState.Blueprint;
		MeshRenderer[] mrs = transform.Find("Model").GetComponentsInChildren<MeshRenderer> ();
		foreach (MeshRenderer rend in mrs) {
			rend.material.color = new Color (1f, 1f, 1f, blueprintAlpha);
			StandardShaderHelper.ChangeRenderMode (rend.material, StandardShaderHelper.BlendMode.Fade);
		}

		//temp
		gameObject.GetComponentInChildren<Animation>().Stop();
	}

	public void Build() {
		state = BuildingState.Active;
		MeshRenderer[] mrs = transform.Find("Model").GetComponentsInChildren<MeshRenderer> ();
		foreach (MeshRenderer rend in mrs) {
			if (rend.gameObject.name.Contains ("(P)")) {
				rend.material.color = info.mainColor;
			} else if (rend.gameObject.name.Contains ("(S)")) {
				rend.material.color = info.secondaryColor;
			} else if (rend.gameObject.name.Contains ("(A)")) {
				rend.material.color = info.alternateColor;
			} else if (rend.gameObject.name == "Pad") {
				rend.material.color = padColor;
			}
			StandardShaderHelper.ChangeRenderMode(rend.material, StandardShaderHelper.BlendMode.Opaque);
		}

		//temp
		gameObject.GetComponentInChildren<Animation>().Play();
	}

	public virtual void TurnEnd() {
		if (supply <= 0) {
			state = BuildingState.Inactive;
			SetAnimTrigger ("Deactivate");
		}
	}

	public void SetAnimTrigger(string triggerName) {
		if (anim != null)
			anim.SetTrigger (triggerName);
	}

	public bool IsPlayerInBuilding {
		get {
			Vector2 playerPos = TerrainManager.PosToV2 (GameObject.Find ("Player").transform.position);
			foreach (Vector2 pos in coveredTiles) {
				if (pos == playerPos)
					return true;
			}

			return false;
		}
	}
}
