using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Building : MonoBehaviour {

	public enum BuildingState
	{
		Blueprint = 0,
		Active = 1,
		Waiting = 2,
		Inactive = 3,
		Destroyed = 4
	}

	public bool standable = false;
	public float height = 0f;

	[HideInInspector]
	public Island island;

	[HideInInspector]
	public BuildingState state;
	public bool isPhysical { get { return state != BuildingState.Blueprint && state != BuildingState.Destroyed; } }

	[HideInInspector]
	public Animator anim;

	const float blueprintAlpha = 0.5f;

	[HideInInspector]
	public List<Vector2> coveredTiles = new List<Vector2>();
	[HideInInspector]
	public BuildingInfo info { get; private set; }

	Color padColor = new Color(0.25f, 0.95f, 0.95f, 1f);

	public virtual void Init(BuildingInfo info, Island island) {
		anim = gameObject.GetComponentInChildren<Animator> ();

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
	}

	public virtual void Build() {
		state = BuildingState.Active;
		SetBuildingColors ();
	}

	void SetBuildingColors() {
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
	}

	public abstract void TurnEnd ();

	protected void Deactivate() {
		state = BuildingState.Inactive;

		if (anim != null) {
			anim.enabled = false;
		}

		MeshRenderer[] mrs = transform.Find("Model").GetComponentsInChildren<MeshRenderer> ();
		foreach (MeshRenderer rend in mrs) {
			if (rend.gameObject.name.Contains ("(P)")) {
				rend.material.color = new Color (info.mainColor.r / 2f, info.mainColor.g / 2f, info.mainColor.b / 2f);
			} else if (rend.gameObject.name.Contains ("(S)")) {
				rend.material.color = new Color (info.secondaryColor.r / 2f, info.secondaryColor.g / 2f, info.secondaryColor.b / 2f);
			} else if (rend.gameObject.name.Contains ("(A)")) {
				rend.material.color = new Color (info.alternateColor.r / 2f, info.alternateColor.g / 2f, info.alternateColor.b / 2f);
			} else if (rend.gameObject.name == "Pad") {
				rend.material.color = new Color (padColor.r / 2f, padColor.g / 2f, padColor.b / 2f);
			}
			StandardShaderHelper.ChangeRenderMode(rend.material, StandardShaderHelper.BlendMode.Opaque);
		}

		//TODO: this can lead to issues if the building is on multiple islands
		//we can't use the tile's original color though because it won't match with the layer that it's on until after it has been destroyed
		Color anchorColor = TerrainManager.instance.tiles [coveredTiles [0]].originalColor;
		foreach (Vector2 pos in coveredTiles) {
			TerrainManager.instance.ClearTileAtPosition (pos);
			TerrainManager.instance.GetTileAtPosition(pos).GetComponent<Renderer> ().material.color = anchorColor;
		}
	}

	protected void SetAnimBool(string boolName, bool isActive) {
		if (anim != null)
			anim.SetBool (boolName, isActive);
	}

	protected void SetAnimTrigger(string triggerName) {
		if (anim != null)
			anim.SetTrigger (triggerName);
	}

	protected void ResetAnimTrigger(string triggerName) {
		if (anim != null)
			anim.ResetTrigger (triggerName);
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
