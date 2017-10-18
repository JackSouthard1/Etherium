using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : MonoBehaviour {

	public enum BuildingState
	{
		Blueprint = 0,
		Active = 1,
		Inactive = 2,
		Destroyed = 3
	}

	public bool standable = false;
	public float height = 0f;

	public Island island;

	public Transform pad;

	public int supply;

	public BuildingState state;
	public bool isPhysical { get { return state != BuildingState.Blueprint && state != BuildingState.Destroyed; } }

	public Animator anim;

	const float blueprintAlpha = 0.5f;
	Color padColor = new Color(0.25f, 0.95f, 0.95f, 1f);

	[HideInInspector]
	public List<Vector2> coveredTiles = new List<Vector2>();
	[HideInInspector]
	public BuildingInfo info { get; private set; }

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
			Deactivate ();
		}
	}

	void Deactivate() {
		state = BuildingState.Inactive;
		SetAnimTrigger ("Deactivate");

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

		foreach (Vector2 pos in coveredTiles) {
			TerrainManager.instance.ClearTileAtPosition (pos);
		}

		//temp
		gameObject.GetComponentInChildren<Animation> ().Stop ();
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
