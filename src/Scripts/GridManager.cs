using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public enum SlopeDetectionMode {
	Euclidean, Vertices, Triangles, Face
}

[AddComponentMenu("HexGridEngine Manager/Grid Manager")]
public class GridManager : MonoBehaviour {
	private static List<GridManager> instances = null;
	private int actualInstanceIndex = -1;

	public GameObject Hex;

	public GridManagerCore[] grids;

	public bool debug;

	void Awake () {
		if (instances == null)
			instances = new List<GridManager>();
		instances.Add (this);
		actualInstanceIndex = instances.IndexOf (this);
	}

	void Start () {
		foreach (GridManagerCore core in grids) 
			core.Init (this);
	}

	public static GridManager[] GetInstances {
		get {
			return instances.ToArray ();
		}
	}
	
	public static GridManager GetInstance (int index) {
		return instances[index];
	}

	public static GridManagerCore GetCoreInstance (int index) {
		return GridManagerCore.GetInstance (index);
	}

	public GridManagerCore[] GetCores {
		get {
			return grids;
		}
	}

	public int GetActualInstanceIndex {
		get {
			return this.actualInstanceIndex;
		}
	}
}

[System.Serializable()]
public class GridManagerCore {
	private static List<GridManagerCore> instances = null;
	private int actualInstanceIndex = -1;

	private GridManager manager = null;

	public string name = "Grid";

	private GameObject Hex = null;

	public MapInfo mapInfo;
	public float XOffset = 0;
	public float ZOffset = 0;
	private GameObject Ground;
	private bool terrain;
	
	public GameObject Line;
	private List<GameObject> path;
	
	public Tile selectedTile = null;
	
	[System.NonSerialized()]
	public TileBehaviour originTileTB = null;
	[System.NonSerialized()]
	public TileBehaviour destTileTB = null;
	
	public float maxSlopeInDegrees = 45f;
	public SlopeDetectionMode slopeDetectMode = SlopeDetectionMode.Triangles;
	
	public float maxHeight = 2f;
	
	private GameObject[][] TBMatrix;
	
	public float yOffset = 0.5f;
	
	private bool useLayer = true;
	//public int layer = 8;
	
	public LayerMask layer;
	
	private float hexWidth;
	private float hexHeight;
	private float groundWidth;
	private float groundHeight;
	
	public void Init (GridManager manager) {
		this.manager = manager;
		Hex = GetManager.Hex;
		Awake ();
		Start ();
	}
	
	void Awake() {
		if (instances == null)
			instances = new List<GridManagerCore>();
		instances.Add(this);
		actualInstanceIndex = instances.IndexOf (this);
		mapInfo.SetMap ();
		Ground = mapInfo.GetActiveMap;
		terrain = mapInfo.useTerrain;
	}

	void Start () {
		SetSize();
		CreateGrid();
	}

	public GridManager GetManager {
		get {
			return this.manager;
		}
	}

	public static GridManagerCore[] GetInstances {
		get {
			return instances.ToArray ();
		}
	}
	
	public static GridManagerCore GetInstance (int index) {
		return instances[index];
	}
	
	public MapInfo GetMapInfos {
		get {
			return mapInfo;
		}
	}
	
	public float GetYOffset {
		get {
			return yOffset;
		}
	}
	
	public bool UseLayer {
		get {
			return useLayer;
		}
	}
	
	public int GetLayer {
		get {
			return layer.value;
		}
	}
	
	public LayerMask GetLayerMask {
		get {
			return layer;
		}
	}
	
	void SetSize() {
		hexWidth = Hex.GetComponent<Renderer>().bounds.size.x;
		hexHeight = Hex.GetComponent<Renderer>().bounds.size.z;
		if (!terrain) {
			groundWidth = Ground.GetComponent<Renderer>().bounds.size.x;
			groundHeight = Ground.GetComponent<Renderer>().bounds.size.z;
		} else {
			groundWidth = Ground.GetComponent<Terrain>().terrainData.size.x;
			groundHeight = Ground.GetComponent<Terrain>().terrainData.size.z;
		}
		groundWidth -= XOffset;
		groundHeight -= ZOffset;
	}
	
	Vector3 CalcGridSize() {
		float sideLenght = hexHeight / 2;
		int nrOfSides = (int)(groundHeight / sideLenght);
		
		int gridHeightInHexes = (int)(nrOfSides * 2 / 3);
		
		if (gridHeightInHexes % 2 == 0 && (nrOfSides * 0.5f) * sideLenght > groundHeight)
			gridHeightInHexes--;
		
		return new Vector2((int)(groundWidth / hexWidth), gridHeightInHexes);
	}
	
	Vector3 CalcInitPos() {
		float offset = 0;
		if (!terrain)
			return new Vector3(-groundWidth / 2 + hexWidth / 2 + Ground.GetComponent<Renderer>().bounds.center.x, 0, groundHeight / 2 - hexHeight / 2 + Ground.GetComponent<Renderer>().bounds.center.z - offset);
		else
			return new Vector3(hexWidth / 2 + Ground.transform.position.x, 0, groundHeight - hexHeight / 2 + Ground.transform.position.z - offset);
	}
	
	public Vector3 CalcWorldCoord(Vector2 gridPos) {
		Vector3 initPos = CalcInitPos();
		float offset = 0;
		
		if (gridPos.y % 2 != 0)
			offset = hexWidth / 2;
		
		float x = initPos.x + offset + gridPos.x * hexWidth;
		
		float z = initPos.z - gridPos.y * hexHeight * 0.75f;
		
		return new Vector3(x, yOffset, z);
	}
	
	void CreateGrid() {
		Vector2 gridSize = CalcGridSize();
		GameObject hexGridGO = new GameObject("HexGrid");

		hexGridGO.transform.parent = GetManager.transform;

		Dictionary<Point, Tile> board = new Dictionary<Point, Tile>();
		
		TBMatrix = new GameObject[(int)gridSize.x][];
		for (int i = 0; i < TBMatrix.Length; i++)
			TBMatrix[i] = new GameObject[(int)gridSize.y];
		
		for (int y = 0; y < gridSize.y; y++) {
			float sizeX = gridSize.x;
			
			if (y % 2 != 0 && (gridSize.x + 0.5) * hexWidth > groundWidth)
				sizeX--;
			for (int x = 0; x < sizeX; x++) {
				GameObject hex = (GameObject)GameObject.Instantiate(Hex);
				Vector2 gridPos = new Vector2((float)x, (float)y);
				
				hex.transform.position = CalcWorldCoord(gridPos);
				hex.transform.parent = hexGridGO.transform;
				var tb = hex.GetComponent<TileBehaviour>();
				TBMatrix[x][y] = hex;
				tb.tile = new Tile(x - (int)((float)y / 2), y, true, hex);
				
				
				tb.SetInstance (this.actualInstanceIndex);
				
				board.Add(tb.tile.Location, tb.tile);
			}
		}
		
		bool equalLineLengths = (gridSize.x + 0.5) * hexWidth <= groundWidth;
		
		foreach(Tile tile in board.Values)
			tile.FindNeighbours(board, gridSize, equalLineLengths);
	}
	
	public float GetMaxHeight (bool isTerrain) {
		if (isTerrain)
			return this.GetMapInfos.GetTerrainSize.y;
		else
			return this.GetMapInfos.GetActiveMap.transform.TransformPoint (this.GetMapInfos.GetActiveMap.GetComponent<MeshFilter>().mesh.bounds.size).y;
	}
	
	
	double CalcDistance(Tile tile) {
		Tile destTile = destTileTB.tile;
		
		float deltaX = Mathf.Abs(destTile.X - tile.X);
		float deltaY = Mathf.Abs(destTile.Y - tile.Y);
		
		int z1 = -(tile.X + tile.Y);
		int z2 = -(destTile.X + destTile.Y);
		
		float deltaZ = Mathf.Abs(z2 - z1);
		
		return Mathf.Max(deltaX, deltaY, deltaZ);
	}
	
	private void DrawPath(IEnumerable<Tile> path) {
		if (this.path == null)
			this.path = new List<GameObject>();
		
		this.path.ForEach(GameObject.Destroy);
		this.path.Clear();
		
		GameObject lines = GameObject.Find("Lines");
		if (lines == null)
			lines = new GameObject("Lines");

		foreach (Tile tile in path) {
			var line = (GameObject)GameObject.Instantiate(Line);
			Vector2 gridPos = new Vector2(tile.X + tile.Y / 2, tile.Y);
			line.transform.localScale = tile.tileBehaviour.transform.localScale / 2;
			
			//line.transform.position = CalcWorldCoord(gridPos);
			
			Mesh tileMesh = TBMatrix[(int)gridPos.x][(int)gridPos.y].GetComponent<MeshFilter>().mesh;
			Vector3 center = tileMesh.bounds.center;
			line.transform.position = TBMatrix[(int)gridPos.x][(int)gridPos.y].transform.TransformPoint(center);
			this.path.Add(line);
			line.transform.parent = lines.transform;
		}
	}
	
	public void GenerateAndShowPath() {
		if (originTileTB == null || destTileTB == null) {
			DrawPath(new List<Tile>());
			return;
		}
		
		Func<Tile, Tile, double> distance = (node1, node2) => 1;
		
		var path = PathFinder.FindPath(originTileTB.tile, destTileTB.tile, distance, CalcDistance);
		DrawPath(path);
	}
}

[System.Serializable()]
public class MapInfo {
	public GameObject meshMap;
	public GameObject terrainObj;
	public bool useTerrain;
	public bool useTerrainRaycast;

	private Terrain terrain;

	private GameObject activeMap;

	private TerrainData terrainData;
	private Vector3 terrainSize;
	private int heightmapWidth;
	private int heightmapHeight;
	private float[,] heightmapData;

	public void SetMap () {
		if (meshMap == null && terrainObj != null) {
			activeMap = terrainObj;
			useTerrain = true;
			GetTerrainInfos ();
		} else if (meshMap != null && terrainObj == null) {
			activeMap = meshMap;
			useTerrain = false;
		} else if (meshMap != null && terrainObj != null) {
			if (useTerrain) {
				activeMap = terrainObj;
				GetTerrainInfos ();
			} else
				activeMap = meshMap;
		} else
			activeMap = null;
	}

	void GetTerrainInfos() 
	{
		terrain = terrainObj.GetComponent<Terrain>();
		terrainData = terrain.terrainData;
		
		terrainSize = terrain.terrainData.size;
		
		heightmapWidth = terrain.terrainData.heightmapWidth;
		heightmapHeight = terrain.terrainData.heightmapHeight;
		
		heightmapData = terrainData.GetHeights(0, 0, heightmapWidth, heightmapHeight);
	}
	public GameObject GetActiveMap {
		get {
			return activeMap;
		}
	}
	public TerrainData GetTerrainData {
		get {
			return terrainData;
		}
	}
	public Vector3 GetTerrainSize {
		get {
			return terrainSize;
		}
	}
	public int GetHeightmapWidth {
		get {
			return heightmapWidth;
		}
	}
	public int GetHeightmapHeight {
		get {
			return heightmapHeight;
		}
	}
	public float[,] GetHightmapData {
		get {
			return heightmapData;
		}
	}
	public bool UseTerrainRaycast {
		get {
			return useTerrainRaycast;
		}
	}
	public bool UseTerrain {
		get {
			return useTerrain;
		}
	}
}