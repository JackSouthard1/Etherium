using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Island : MonoBehaviour {
	int islandIndex;

	[Header("Teir")]
	private TerrainManager.Teir teirInfo;
	public int teir;

	[Space(10)]
	[Header("Tiles")]
	public GameObject tilePrefab;
	public Layer[] layers;
	public float heightPerLayer = 0.7f;

	[Space(10)]
	[Header("Resouces")]
	public int resourceTileCount;
	public int resourcesPerTile;

	public List<Pickup> pickups = new List<Pickup>();

	private GameObject enemyPrefab;

	[HideInInspector]
	public List<Body> enemies = new List<Body>();

	public bool buildable = false;

	[Space(10)]
	[Header("Civilizing")]
	public Color civilizedColor;
	public float timeToCivilize;
	private float civStartTime;
	private Vector3 targetPos;
	private Vector3 startPos;
	private bool civilizing = false;

	[Space(10)]
	[Header("Voids")]
	public int voidCount;
	public List<int> voidTileIndexes;

	[HideInInspector]
	public List<Vector2> usedTilePositions = new List<Vector2>();

	[HideInInspector]
	public List<TerrainManager.SetSpawn> setSpawns;

	public List<int> usedTiles = new List<int>();
	public TerrainManager.Tile[] tiles;
	private TerrainManager tm;
	private GameManager gm;
	public int diffuculty;

	void Awake () {
		tm = TerrainManager.instance;
		gm = GameManager.instance;
	}

	public void InitIsland(List<Vector2> _tilePositions, Vector3 civilizedPosition, int index, int _teir) {
		islandIndex = index;
		targetPos = civilizedPosition;
//		teir = Mathf.Clamp(Mathf.FloorToInt(transform.position.magnitude / tm.teirDstIntervals), 0, tm.teirs.Length - 1);
		if (_teir > (tm.teirs.Length - 1)) {
			_teir = tm.teirs.Length - 1;
		}
		this.teir = _teir;
		teirInfo = tm.teirs [teir];

		setSpawns = tm.GetSetSpawns (teir);
		tilePositions = _tilePositions;
		GenerateTiles ();
		MakeInitialBorder ();
		bool alreadyCivilized = SavedGame.data.civilizedIslandIndexes.Contains (index);
		if (!alreadyCivilized) {
			SpawnEnemies ();
		} else {
			StartCivilizing ();
		}
	}
		
	public void PlayerEnterIsland () {
		for (int i = 0; i < enemies.Count; i++) {
			enemies [i].GetComponentInChildren<Mind> ().active = true;
		}

		tm.DestroyAllBuses ();
	}

	public void PlayerExitIsland () {
		for (int i = 0; i < enemies.Count; i++) {
			enemies [i].GetComponentInChildren<Mind> ().active = false;
		}
	}

	public void EnemyDeath (Body enemy) {
		enemies.Remove (enemy);
		SaveEnemies ();
	}

	public void TestEnemyCount () {
		if (!buildable) {
			if (enemies.Count <= 0) {
				StartCivilizing ();
			}
		}
	}

	List<Vector2> tilePositions;

	void GenerateTiles () {
		tiles = new TerrainManager.Tile[tilePositions.Count];

		// void tiles
		voidTileIndexes = new List<int> ();
		for (int i = 0; i < voidCount; i++) {
			voidTileIndexes.Add (GetAvaiableTileIndex (true));
		}
			
		// resource tiles
		List<int> tileSpawnIDs = new List<int> ();
		foreach (var setSpawn in setSpawns) {
			if (setSpawn.type == TerrainManager.SetSpawn.SpawnType.Tile) {
				tileSpawnIDs.Add (setSpawn.spawnID);
			}
		}
		int tilesLeft = resourceTileCount - tileSpawnIDs.Count;
		for (int i = 0; i < tilesLeft; i++) {
			tileSpawnIDs.Add(teirInfo.resourceIndexes[Random.Range(0, teirInfo.resourceIndexes.Length)]);
//			print ("Index: " + i + ", ID: " + tileSpawnIDs [i]);
		}

		List<int> resourceTileSpawnIndexs = new List<int> ();

		for (int i = 0; i < tileSpawnIDs.Count; i++) {
			resourceTileSpawnIndexs.Add (GetAvaiableTileIndex (false));
		}

		int index = 0;
		int resourceSpawnListIndex = 0;
		foreach (Vector2 tile in tilePositions) {
			if (!voidTileIndexes.Contains (index)) {
				int layer = Random.Range (0, teirInfo.layers.Length);
				float y = layer * heightPerLayer;
				Vector3 position = new Vector3 (tile.x, y, tile.y);
				GameObject tileGO = (GameObject)Instantiate (tilePrefab, transform);
				tileGO.transform.localPosition = position;
				tileGO.name = "Tile (" + tile.x + "," + tile.y + ")";

				ResourceInfo.ResourceType resourceInfoType;
				Color tileColor;

				ResourceInfo resourceInfo = new ResourceInfo ();
				if (resourceTileSpawnIndexs.Contains (index) && !GameManager.isLoadingFromSave) {
					int resourceIndex = tileSpawnIDs [resourceSpawnListIndex];
					resourceSpawnListIndex++;

					resourceInfo = ResourceInfo.GetInfoFromIndex(resourceIndex);



					if (resourceInfo.hasTile) {
						resourceInfoType = resourceInfo.type;
						tileColor = resourceInfo.tileColor;
					} else {
						resourceInfoType = ResourceInfo.ResourceType.None;
						tileColor = teirInfo.layers [layer].color;
					}
				} else {
					resourceInfoType = ResourceInfo.ResourceType.None;
					tileColor = teirInfo.layers [layer].color;
				}

				TerrainManager.Tile tileInfo = new TerrainManager.Tile (tileGO, resourceInfoType, GetComponent<Island> (), tileColor, y);
				tileGO.GetComponent<MeshRenderer> ().material.color = tileColor;
				tiles [index] = tileInfo;
				tm.tiles.Add (new Vector2 (tile.x + transform.position.x, tile.y + transform.position.z), tileInfo);

				if (resourceTileSpawnIndexs.Contains (index) && !GameManager.isLoadingFromSave) {
					for (int i = 0; i < resourcesPerTile; i++) {
						tm.SpawnResource(position: transform.TransformPoint(position), info: resourceInfo, island: GetComponent<Island>(), initialSpawn: true);
					}
				}
				//TODO: it's kinda dumb to have this if statement twice
				if (resourceTileSpawnIndexs.Contains (index) && tileInfo.resourceType != ResourceInfo.ResourceType.None && !GameManager.isLoadingFromSave) {
					SavedGame.AddResourceTile (tileInfo);
				}
			}

			index++;
		}
	}

	void SpawnEnemies () {
		if (!SavedGame.data.savedEnemyLists.ContainsKey (islandIndex)) {
			List<int> spawnIDs = new List<int> ();
			foreach (var setSpawn in setSpawns) {
				if (setSpawn.type == TerrainManager.SetSpawn.SpawnType.Enemy) {
					spawnIDs.Add (setSpawn.spawnID);
				}
			}
			int enemiesLeft = teirInfo.enemyCount - spawnIDs.Count;
			for (int i = 0; i < enemiesLeft; i++) {
				spawnIDs.Add (teirInfo.enemyIDs [Random.Range (0, teirInfo.enemyIDs.Length)]);
			}

			List<int> spawnTileIndexs = new List<int> ();

			for (int f = 0; f < spawnIDs.Count; f++) {
				spawnTileIndexs.Add (GetAvaiableTileIndex (true));
			}

			for (int i = 0; i < spawnIDs.Count; i++) {
				GameObject enemyPrefab = tm.enemyPrefabs [spawnIDs [i]];
				GameObject enemy = (GameObject)Instantiate (enemyPrefab, transform);
				enemy.transform.position = tiles [spawnTileIndexs [i]].tile.transform.position;

				Body enemyBody = enemy.GetComponent<Body> ();
				enemyBody.id = spawnIDs [i];
				enemies.Add (enemyBody);
			}
		} else {
			foreach (SavedGame.SavedEnemy enemy in SavedGame.data.savedEnemyLists[islandIndex]) {
				enemies.Add (enemy.Spawn ());
			}
		}
	}

	void StartCivilizing () {
		if (buildable) {
			return;
		}

		if (!GameManager.isLoadingFromSave) {
			gm.TransitionStart();
			civStartTime = Time.time;
			civilizing = true;
			startPos = transform.position;
			Transform player = GameObject.FindGameObjectWithTag ("Player").transform;

			if (tm.GetTileAtPosition (TerrainManager.PosToV2 (player.position)) != null) {
				player.parent = tm.GetTileAtPosition (TerrainManager.PosToV2 (player.position)).transform;
				player.Find ("Model").localPosition = Vector3.zero;
			}
		}

		for (int i = 0; i < tiles.Length; i++) {
			if (!voidTileIndexes.Contains (i)) {
				GameObject tile = tiles [i].tile;
				tm.tiles.Remove (TerrainManager.PosToV2(tile.transform.position));

				if (!GameManager.isLoadingFromSave) {
					if (tiles [i].resourceType != ResourceInfo.ResourceType.None) {
						SavedGame.RemoveResourceTile (tiles [i]);
					}
				}
			}
		}

		if (!GameManager.isLoadingFromSave) {
			foreach (Pickup pickup in pickups) {
				tm.pickups.Remove (pickup.position);
			}
			SavedGame.AddCivilizedIsland (islandIndex);
		} else {
			FinishCivilizing ();
		}
	}

	void Update () {
		if (borderData.isideBorders.Count > 0) {
			if (Input.GetKeyDown (KeyCode.R)) {
				StartRift ();
			}
			if (Input.GetKeyDown (KeyCode.T)) {
				AdvanceRift ();
			}
		}

		if (civilizing) {
			float timeRatio = Mathf.Clamp01((Time.time - civStartTime) / timeToCivilize);

			transform.position = Vector3.Lerp (startPos, targetPos, timeRatio);
			for (int i = 0; i < tiles.Length; i++) {
				if (!voidTileIndexes.Contains (i)) {
//					GameObject tileGO = tiles [i].tile;
//					float newHeight = tiles [i].originalY * (1f - timeRatio);
//					tileGO.transform.position = new Vector3 (tileGO.transform.position.x, newHeight, tileGO.transform.position.z);
//
//					if (tiles [i].resourceType == TerrainManager.ResourceInfo.ResourceType.None) {
//						Color newColor = Color.Lerp (tiles [i].originalColor, civilizedColor, timeRatio);
//						tileGO.GetComponentInChildren<MeshRenderer> ().material.color = newColor;
//					}
				}
			}

			foreach (Pickup pickup in pickups) {
				for (int i = 0; i < pickup.gameObjects.Count; i++) {
//					float newHeight = ((pickup.originalBaseHeight) * (1f - timeRatio)) + (tm.stackHeight * i);
//					Vector3 newPos = new Vector3 (pickup.gameObjects [i].transform.position.x, newHeight, pickup.gameObjects [i].transform.position.z);
//					pickup.gameObjects[i].transform.position = newPos;
				}
			}

			if (timeRatio == 1) {
				FinishCivilizing();
			}
		}
	}

	private int GetAvaiableTileIndex (bool canBeNextToUsedTile) {
		int attempts = 0;
		int random = Random.Range (0, tiles.Length - 1);
		Vector2 pos = tilePositions [random];

		if (canBeNextToUsedTile) {
			while (usedTiles.Contains (random)) {
				random = Random.Range (0, tiles.Length - 1); 
				pos = tilePositions [random];
				attempts++;
				if (attempts > 3) {
//					print (attempts + " Attempts Made");
					break;
				}
			}
		} else {
			while (usedTiles.Contains (random) || AreAdjacentTiles(pos)) {
				random = Random.Range (0, tiles.Length - 1); 
				pos = tilePositions [random];
				attempts++;
				if (attempts > 3) {
					print (attempts + " Attempts Made");
					break;
				}
			}
		}

		usedTiles.Add (random);
		usedTilePositions.Add (pos);

		return random;
	}

	private bool AreAdjacentTiles (Vector2 pos) {
		List<Vector2> allDirections = new List<Vector2> () { Vector2.right, Vector2.up, Vector2.left, Vector2.down };

		foreach (Vector2 direction in allDirections) {
			Vector2 posToCheck = pos + direction;
			if (usedTilePositions.Contains (posToCheck)) {
				return true;
			}
		}

		return false;
	}

	void FinishCivilizing () {
		if (buildable) {
			return;
		}

		transform.position = targetPos;
		foreach (Pickup pickup in pickups) {
			List<GameObject> gameObjects = pickup.gameObjects;

			for (int k = 0; k < gameObjects.Count; k++) {
				//				float height = tm.stackHeight * k;
				gameObjects[k].transform.position = new Vector3 (Mathf.RoundToInt(gameObjects[k].transform.position.x), gameObjects[k].transform.position.y, Mathf.RoundToInt(gameObjects[k].transform.position.z));
			}
		}
			
		for (int i = 0; i < tiles.Length; i++) {
			if (!voidTileIndexes.Contains (i)) {
				tiles [i].tile.transform.position = new Vector3 (Mathf.RoundToInt (tiles [i].tile.transform.position.x), tiles [i].tile.transform.position.y, Mathf.RoundToInt (tiles [i].tile.transform.position.z));
				tm.tiles.Add (TerrainManager.PosToV2(tiles[i].tile.transform.position), tiles [i]);

				if (!GameManager.isLoadingFromSave) {
					if (tiles [i].resourceType != ResourceInfo.ResourceType.None) {
						SavedGame.AddResourceTile (tiles [i]);
					}
				}
			}
		}
			
		for (int i = 0; i < pickups.Count; i++) {
			pickups[i].UpdatePosition();
			tm.pickups.Add (pickups[i].position, pickups [i]);
		}

		GameObject.FindGameObjectWithTag ("Player").transform.parent = null;

		buildable = true;
		civilizing = false;

		List<TileEdge> newEdges = new List<TileEdge> ();
		foreach (TileEdge edge in borderData.edges) {
			TileEdge newEdge = new TileEdge ();
			newEdge.position = edge.position + (new Vector2 (targetPos.x, targetPos.z) - new Vector2 (startPos.x, startPos.z));
			newEdges.Add (newEdge);
		}
		borderData.edges = newEdges;

		if (teir != 0) {
			UpdateBorder ();
		}

		if (!GameManager.isLoadingFromSave) {
			SavedGame.UpdatePickups ();
		}

		gm.TransitionEnd();
	}

	public void SaveEnemies() {
		SavedGame.UpdateEnemyList (islandIndex, enemies);
	}
		
	public struct Layer {
		public Color color;
	}

	// Borders
	public class BorderData {
		public List<TileEdge> edges = new List<TileEdge>();
		public List<InsideBorderData> isideBorders = new List<InsideBorderData>();
	}

	public struct InsideBorderData {
		public int startIndex;
		public int endIndex;

		public InsideBorderData (int _startIndex, int _endIndex) {
			startIndex = _startIndex;
			endIndex = _endIndex;
		}
	}

	public struct TileEdge {
		public Vector2 position;

		public TileEdge (Vector2 _position) {
			position = _position;
		}
	}

	public BorderData borderData = new BorderData ();
	public GameObject marker;

	public void MakeInitialBorder () {
		List<TileEdge> newEdgeData = GetBorderEdges ();

		borderData.edges = newEdgeData;
	}

	public void UpdateBorder () {
		List<InsideBorderData> newInsideBorderData = GetInsideBorderDatas ();

		borderData.isideBorders = newInsideBorderData;
	}

	public int riftLength;
	public int riftCurIndex;

	public void StartRift () {
		riftLength = 0;
		riftCurIndex = borderData.isideBorders [0].startIndex;

		GameObject riftMarkerGO = Instantiate (marker, transform);
		riftMarkerGO.transform.position = new Vector3 (borderData.edges[riftCurIndex].position.x, 2f, borderData.edges[riftCurIndex].position.y);
		riftMarkerGO.GetComponent<MeshRenderer> ().material.color = Color.red;
	}

	public void AdvanceRift () {
		riftLength++;
		riftCurIndex++;
		if (riftCurIndex >= borderData.edges.Count) {
			riftCurIndex = 0;
		}

		GameObject riftMarkerGO = Instantiate (marker, transform);
		riftMarkerGO.transform.position = new Vector3 (borderData.edges[riftCurIndex].position.x, 1f, borderData.edges[riftCurIndex].position.y);
		riftMarkerGO.GetComponent<MeshRenderer> ().material.color = Color.yellow;

		if (riftCurIndex == borderData.isideBorders [0].endIndex) {
			EndRift ();
		}
	}

	public void EndRift () {
		print ("Rift End");

		GameObject finalRiftMarkerGO = Instantiate (marker, transform);
		finalRiftMarkerGO.transform.position = new Vector3 (borderData.edges[riftCurIndex].position.x, 2f, borderData.edges[riftCurIndex].position.y);
		finalRiftMarkerGO.GetComponent<MeshRenderer> ().material.color = Color.red;
	}

	List<InsideBorderData> GetInsideBorderDatas () {
		List<InsideBorderData> insideBorderData = new List<InsideBorderData> ();

		Vector2 firstPos = borderData.edges [0].position;

		// temp
//		GameObject firstMarkerGO = Instantiate (marker, transform);
//		firstMarkerGO.transform.position = new Vector3 (firstPos.x, 1f, firstPos.y);
//		firstMarkerGO.GetComponent<MeshRenderer> ().material.color = Color.magenta;

		bool firstPosExposed = CornerExposed (firstPos);

		int transitionStart;
		if (firstPosExposed) {
			// repeat until unexposed corner is found
			int nextTile = 1;
			bool nextCornerExposed = CornerExposed (borderData.edges [nextTile].position);

			while (nextCornerExposed) {
				nextTile++;
				if (nextTile >= borderData.edges.Count) {
					print ("Could not find a viable transition point");
					return insideBorderData;
				}

				nextCornerExposed = CornerExposed (borderData.edges [nextTile].position);
			}

			transitionStart = nextTile - 1;
		} else {
			// repeat until exposed corner is found
			int nextTile = 1;
			bool nextCornerExposed = CornerExposed (borderData.edges [nextTile].position);

			while (!nextCornerExposed) {
				nextTile++;
				if (nextTile >= borderData.edges.Count) {
					print ("Could not find a viable transition point");
					return insideBorderData;
				}
				nextCornerExposed = CornerExposed (borderData.edges [nextTile].position);
			}

			transitionStart = nextTile;
		}

		int transitionEnd;

		GameObject transitionMarkerGO = Instantiate (marker, transform);
		transitionMarkerGO.transform.position = new Vector3 (borderData.edges[transitionStart].position.x, 1f, borderData.edges[transitionStart].position.y);
		transitionMarkerGO.GetComponent<MeshRenderer> ().material.color = Color.grey;

		int _nextTile = transitionStart + 1;
		bool secondPosExposed = CornerExposed (borderData.edges [_nextTile].position);
		bool _nextCornerExposed = secondPosExposed;

		while (_nextCornerExposed == secondPosExposed) {
			_nextTile++;
			if (_nextTile >= borderData.edges.Count) {
				print ("Could not find a viable transition point");
				return insideBorderData;
			}
			_nextCornerExposed = CornerExposed (borderData.edges [_nextTile].position);
		}

		transitionEnd = _nextTile;

		if (secondPosExposed) {
			transitionEnd -= 1;
		}

		GameObject _transitionMarkerGO = Instantiate (marker, transform);
		_transitionMarkerGO.transform.position = new Vector3 (borderData.edges[transitionEnd].position.x, 1f, borderData.edges[transitionEnd].position.y);
		_transitionMarkerGO.GetComponent<MeshRenderer> ().material.color = new Color(1f,0.5f,1f,1f);

		insideBorderData.Add (new InsideBorderData (transitionStart, transitionEnd));
		return insideBorderData;
	}

	bool CornerExposed (Vector2 pos) {
		bool TileTopLeft 		= tm.GetTileAtPosition (pos + new Vector2 (-0.5f, 0.5f));
		bool TileTopRight 		= tm.GetTileAtPosition (pos + new Vector2 (0.5f, 0.5f));
		bool TileBottomLeft 	= tm.GetTileAtPosition (pos + new Vector2 (-0.5f, -0.5f));
		bool TileBottomRight 	= tm.GetTileAtPosition (pos + new Vector2 (0.5f, -0.5f));

		if (!TileTopLeft || !TileTopRight || !TileBottomLeft || !TileBottomRight) {
			// must be on exposed edge
//			print ("Marker at: " + pos + " is exposed");
			return true;
		} else {
			// must be on an inside edge
//			print ("Marker at: " + pos + " is not exposed");
			return false;
		}
	}

	List<TileEdge> GetBorderEdges () {
		List<TileEdge> borderEdges = new List<TileEdge> ();

		Vector2 startingTilePosLocal = tilePositions [tilePositions.Count - 1];
		Vector2 startingTilePosWorld = startingTilePosLocal + new Vector2 (transform.position.x, transform.position.z);
		Vector2? startPos = GetExposedCorner (startingTilePosWorld);

		tm.GetTileAtPosition (startingTilePosWorld).GetComponent<MeshRenderer> ().material.color = Color.cyan; // temp

		// if tile is on inside of island, repeat until edge tile is found
		while (startPos == null) {
			startingTilePosLocal = tilePositions [Random.Range(0, tilePositions.Count - 1)];
			startingTilePosWorld = startingTilePosLocal + new Vector2 (transform.position.x, transform.position.z);
			startPos = GetExposedCorner (startingTilePosWorld);

			tm.GetTileAtPosition (startingTilePosWorld).GetComponent<MeshRenderer> ().material.color = Color.red; // temp
		}

		tm.GetTileAtPosition (startingTilePosWorld).GetComponent<MeshRenderer> ().material.color = Color.green; // temp

		GameObject firstMarkerGO = Instantiate (marker, transform);
		firstMarkerGO.transform.localPosition = new Vector3 (startPos.Value.x, 0f, startPos.Value.y);
		firstMarkerGO.GetComponent<MeshRenderer> ().material.color = Color.green; // temp
		tm.GetTileAtPosition (startingTilePosWorld).GetComponent<MeshRenderer> ().material.color = Color.blue; // temp

		borderEdges.Add(new TileEdge(new Vector2 (firstMarkerGO.transform.position.x, firstMarkerGO.transform.position.z)));

		// Get Rest of edges
		Vector2 anchorPos = new Vector2 (firstMarkerGO.transform.position.x, firstMarkerGO.transform.position.z);
		Vector2 firstPos = anchorPos;
		Vector2? lastPos = null;

		int attempts = 100;
		bool borderComplete = false;
		while (!borderComplete && attempts > 0) {
			attempts--;
			if (attempts == 0) {
				print ("Failed to find new pos at: " + anchorPos + " LastPos: " + lastPos + " because ran out of attempts");
			}
			TileEdge? nextEdge = GetNextEdge (firstPos, lastPos, anchorPos);
			if (nextEdge != null) {
				GameObject markerGO = Instantiate (marker, transform);
				markerGO.transform.position = new Vector3 (nextEdge.Value.position.x, 0f, nextEdge.Value.position.y);
				markerGO.GetComponent<MeshRenderer> ().material.color = Color.cyan;

				borderEdges.Add (new TileEdge (nextEdge.Value.position));

				lastPos = anchorPos;
				anchorPos = new Vector2(markerGO.transform.position.x, markerGO.transform.position.z);


			} else {
				borderComplete = true;
			}
		}

		return borderEdges;
	}

	TileEdge? GetNextEdge (Vector2 firstPos, Vector2? lastPos, Vector2 pos) {
		float newDir = 0f;
		Vector2 newPos = Vector2.zero;

		Vector2 rawTestLeft = new Vector2 (-0.5f, 0.5f);
		Vector2 rawTestRight = new Vector2 (0.5f, 0.5f);

		List<Vector2> allDirections = new List<Vector2> () { Vector2.right, Vector2.up, Vector2.left, Vector2.down };
		bool foundNewEdge = false;
		bool isFinalEdge = false;

		foreach (Vector2 direction in allDirections) {
			Vector2 posToCheck = pos + direction;
			float degrees = -Vector2.SignedAngle (Vector2.up, direction);
			Vector2 rotatedTestLeft = Quaternion.Euler (0, 0, degrees) * rawTestLeft;
			Vector2 rotatedTestRight = Quaternion.Euler (0, 0, degrees) * rawTestRight;

			if (direction == Vector2.right || direction == Vector2.left) {
				rotatedTestLeft = -rotatedTestLeft;
				rotatedTestRight = -rotatedTestRight;
			}
			
			Vector2 posTestLeft = pos + rotatedTestLeft;
//			posTestLeft = new Vector2 (Mathf.RoundToInt (posTestLeft.x), Mathf.RoundToInt (posTestLeft.y));
			Vector2 posTestRight = pos + rotatedTestRight;
//			posTestRight = new Vector2 (Mathf.RoundToInt (posTestRight.x), Mathf.RoundToInt (posTestRight.y));

			// left test
//				if (direction == Vector2.up) {
//					print ("Dir: " + direction + "Left Check: " + posTestLeft + "Right Check: " + posTestRight);
//				}
			bool leftTile = TerrainManager.instance.isTileAtPosition (posTestLeft);
			bool rightTile = TerrainManager.instance.isTileAtPosition (posTestRight);

			if ((leftTile && !rightTile) || (!leftTile && rightTile)) {
				if (posToCheck != lastPos) {
					if (posToCheck == firstPos) {
						isFinalEdge = true;
					} else {
						newDir = degrees;
						newPos = posToCheck;
						foundNewEdge = true;
					}
				}
			}

//			print ("Dir: " + direction + " Pos: " + pos + " First Pos: " + firstPos + " Vector Left: " + posTestLeft + " Left Tile: " + leftTile + " Vector Right: " + posTestRight + " Right Tile: " + rightTile);

			if (direction == Vector2.down) {
				if (!foundNewEdge && !isFinalEdge) {
					print ("Failed to find new edge at: " + pos);
					GameObject failMarkerGO = Instantiate (marker, transform);
					failMarkerGO.transform.position = new Vector3 (pos.x, 1f, pos.y);
					failMarkerGO.GetComponent<MeshRenderer> ().material.color = Color.red; // temp
				}
			}
		}

		if (foundNewEdge) {
			return new TileEdge (newPos); 
		} else {
//			print ("First Pos: " + firstPos + " Last Pos: " + lastPos + " Anchor Pos: " + pos);
			return null;
		}
	}

	Vector2? GetExposedCorner (Vector2 tilePos) {
		List<Vector2> allDirections = new List<Vector2> () { Vector2.right, Vector2.up, Vector2.left, Vector2.down };
		Vector2 rawVector = new Vector2 (0.5f, 0.5f);
		foreach (Vector2 direction in allDirections) {
			Vector2 posToCheck = tilePos + direction;

			if (tm.GetTileAtPosition (posToCheck) == null) {
				float degrees = -Vector2.SignedAngle (Vector2.up, direction);
				Vector2 rotatedVector = Quaternion.Euler(0,0,degrees) * rawVector;
				if (direction == Vector2.right || direction == Vector2.left) {
					rotatedVector = -rotatedVector;
				}
				return tilePos + rotatedVector - new Vector2 (transform.position.x, transform.position.z);
			}
		}

		return null;
	}
}