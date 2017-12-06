using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MapReveal : MonoBehaviour {

	private Transform player;
	private Transform revealArea;
	public Vector3 offset;
	public int baseDetectionRange = 30;

	float detectionRange { get { return baseDetectionRange + Player.instance.body.GetTotalBoostValue (AugmentInfo.BoostType.Vision); } }

	public GameObject revealTile;
	public float scale;

	public static Dictionary<Vector2, FieldData> revealTiles = new Dictionary<Vector2, FieldData>();

	void Awake () {
		revealArea = GameObject.Find("RevealArea").transform;
	}

	public void Init() {
		player = GameObject.Find("Player").transform;
	}
	
	public void PlayerPositionChanged () {
		List<Vector2> cords = GetFieldCoordinatesInRange ();
		
		for (int i = 0; i < cords.Count; i++) {

			FieldData field;
			revealTiles.TryGetValue (cords [i], out field);

			if (field.GO == null) {
				GameObject newField = (GameObject)Instantiate (revealTile, Vector3.zero, Quaternion.identity, revealArea);
				newField.transform.position = new Vector3 (cords [i].x + offset.x, offset.y, cords [i].y + offset.z);
				newField.transform.localScale = new Vector3 (scale, 1, scale);
				revealTiles.Add (cords [i], new FieldData(cords[i], newField));
			}
		}
	}

	private List<Vector2> GetFieldCoordinatesInRange () {
		List<Vector2> coords = new List<Vector2> ();

		Vector3 playerPos = player.transform.position;
		Vector2 centerCoord = TerrainManager.PosToV2(playerPos + offset);
		Vector2 bottomLeft = new Vector2 (centerCoord.x - Mathf.RoundToInt(detectionRange / 2f), centerCoord.y - Mathf.RoundToInt(detectionRange / 2f));

		for (int y = 0; y < detectionRange; y++) {
			for (int x = 0; x < detectionRange; x++) {
				Vector2 cord = new Vector2 (x + bottomLeft.x, y + bottomLeft.y);
				float distanceFromCenter = Vector2.Distance(cord, centerCoord);

				if (distanceFromCenter <= (detectionRange / 2)) {
					coords.Add (cord);
				}
			}
		}

		return coords;
	}

	public void LoadSavedRevealArea () {
		foreach (SavedGame.SavedTilePos tilePos in SavedGame.data.revealedTiles) {
			Vector2 coord = tilePos.ToVector2 ();
			GameObject newField = (GameObject)Instantiate (revealTile, Vector3.zero, Quaternion.identity, revealArea);
			newField.transform.position = new Vector3 (coord.x + offset.x, offset.y, coord.y + offset.z);
			newField.transform.localScale = new Vector3 (scale, 1, scale);
			revealTiles.Add (coord, new FieldData(coord, newField));
		}
	}

	public struct FieldData {
		public Vector2 cord;
		public GameObject GO;

		public FieldData (Vector2 cord, GameObject GO) {
			this.cord = cord;
			this.GO = GO;
		}
	}
}
