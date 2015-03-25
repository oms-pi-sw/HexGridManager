using UnityEngine;
using System.Collections;

[AddComponentMenu("HexGridEngine Manager/Tile Behaviour")]
public class TileBehaviour : MonoBehaviour {

	public Tile tile;

	public Material OpaqueMaterial;
	public Material DefaultMaterial;

	public Color orange = new Color(255f / 255f, 127f / 255f, 0, 127f / 255f);
	public Color mouseEnterColor = Color.white;
	public Color mouseOverColor = Color.gray;
	public Color originColor = Color.red;
	public Color destinationColor = Color.blue;

	private Vector3 heightmapPos;

	private int instanceIndex = -1;
	private GridManagerCore gridManager;

	public void SetInstance (int index) {
		instanceIndex = index;
	}

	void ChangeColor(Color color) {
		if (color.a == 1)
			color.a = 130f / 255f;
		GetComponent<Renderer>().material = OpaqueMaterial;
		GetComponent<Renderer>().material.color = color;
	}

	void OnMouseEnter() {
		gridManager.selectedTile = tile;

		if (tile.Passable && this != gridManager.destTileTB && this != gridManager.originTileTB) {
			this.GetComponent<Renderer>().material = DefaultMaterial;
			this.GetComponent<Renderer>().material.color = mouseEnterColor;
		}
	}

	void OnMouseOver() {
		if (Input.GetMouseButtonUp(1))
		{
			if (this == gridManager.destTileTB || this == gridManager.originTileTB)
				return;
			tile.Passable = !tile.Passable;
			if (!tile.Passable)
				ChangeColor(mouseOverColor);
			else
				ChangeColor(orange);
			
			gridManager.GenerateAndShowPath();
		}

		if (Input.GetMouseButtonUp(0))
		{
			tile.Passable = true;
			
			TileBehaviour originTileTB = gridManager.originTileTB;

			if (this == originTileTB || originTileTB == null)
				originTileChanged();
			else
				destTileChanged();
			
			gridManager.GenerateAndShowPath();
		}
	}

	void originTileChanged()
	{
		var originTileTB = gridManager.originTileTB;

		if (this == originTileTB)
		{
			gridManager.originTileTB = null;
			GetComponent<Renderer>().material = DefaultMaterial;
			return;
		}

		gridManager.originTileTB = this;
		ChangeColor(originColor);
	}
	
	void destTileChanged()
	{
		var destTile = gridManager.destTileTB;

		if (this == destTile)
		{
			gridManager.destTileTB = null;
			GetComponent<Renderer>().material.color = orange;
			return;
		}

		if (destTile != null)
			destTile.GetComponent<Renderer>().material = DefaultMaterial;
		gridManager.destTileTB = this;
		ChangeColor(destinationColor);
	}

	void Awake () {

	}

	// Use this for initialization
	void Start () {
		gridManager = GridManager.GetCoreInstance (this.instanceIndex);
		CheckPassability ();
		SetTileTB ();
		if (!tile.Passable)
			gameObject.GetComponent<Renderer>().material.color = new Color(0, 0, 0, 0);
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	void CheckPassability () {
		if (gridManager.slopeDetectMode == SlopeDetectionMode.Face) {
			if (!CheckGround (GetVerticesWorldPosition ()) || !CheckSlopeAndHeight (GetMeshCenterWorldPosition ()))
				this.tile.Passable = false;
		} else if (gridManager.slopeDetectMode == SlopeDetectionMode.Vertices) {
			if (!CheckGround (GetVerticesWorldPosition ()) || !CheckSlopeAndHeight (GetVerticesWorldPosition ()))
				this.tile.Passable = false;
		} else {
			if (!CheckGround (GetVerticesWorldPosition ()) || !CheckSlopeAndHeight (GetVerticesWorldPosition (), GetMeshTriagles ()))
				this.tile.Passable = false;
		}
	}

	bool CheckSlopeAndHeight (Vector3[] verts) {
		float slope = GetVerticesMaxSlope (verts);
		return (slope < (gridManager.maxSlopeInDegrees * Mathf.Deg2Rad));
	}
	
	bool CheckSlopeAndHeight (Vector3 vert) {
		float slope = GetPlaneSlope (vert);
		return (slope < (gridManager.maxSlopeInDegrees * Mathf.Deg2Rad));
	}
	
	bool CheckSlopeAndHeight (Vector3[] verts, int[] triangles) {
		float slope = 0;
		if (gridManager.slopeDetectMode == SlopeDetectionMode.Triangles)
			slope = GetTrianglesMaxSlope (verts, triangles);
		else
			slope = GetEuclideanMaxSlope (verts, triangles);
		return (slope < (gridManager.maxSlopeInDegrees * Mathf.Deg2Rad));
	}
	
	bool CheckGround (Vector3[] verts) {
		float offset = 1f;
		float height = gridManager.GetMaxHeight (gridManager.GetMapInfos.UseTerrain) + offset;
		RaycastHit hit;
		
		foreach (Vector3 v in verts) {
			if (!GetRaycast (new Vector3 (v.x, height, v.z), Vector3.down, out hit, Mathf.Infinity)) 
				return false;
		}
		return true;
	}

	float GetEuclideanMaxSlope (Vector3[] origins, int[] triangles) {
		float offset = 1f;
		float height = gridManager.GetMaxHeight (gridManager.GetMapInfos.UseTerrain) + offset;
		RaycastHit hit;
		
		float slope = -1;

		for (int i = 0; i < triangles.Length; i++) {
			Vector3 normal = Vector3.zero;
			Vector3 v1 = Vector3.zero, v2 = Vector3.zero, v3 = Vector3.zero;
			if (GetRaycast (new Vector3(origins[triangles[i]].x, height, origins[triangles[i++]].z) , Vector3.down, out hit, Mathf.Infinity))
				v1 = hit.point;
			if (GetRaycast (new Vector3(origins[triangles[i]].x, height, origins[triangles[i++]].z) , Vector3.down, out hit, Mathf.Infinity))
				v2 = hit.point;
			if (GetRaycast (new Vector3(origins[triangles[i]].x, height, origins[triangles[i]].z) , Vector3.down, out hit, Mathf.Infinity))
				v3 = hit.point;
			v1 = TransformCanonicalAxes (v1);
			v2 = TransformCanonicalAxes (v2);
			v3 = TransformCanonicalAxes (v3);
			normal = new Vector3 (detM2 (v2.y - v1.y, v2.z - v1.z, v3.y - v1.y, v3.z - v1.z), -detM2 (v2.x - v1.x, v2.z - v1.z, v3.x - v1.x, v3.z - v1.z), detM2 (v2.x - v1.x, v2.y - v1.y, v3.x - v1.x, v3.y - v1.y));
			normal.Normalize ();
			normal = TransformUnityAxes (normal);
			float s = Mathf.Acos (Mathf.Abs (Vector3.Dot (Vector3.up, normal)));
			if (s > slope)
				slope = s;
		}
		if (gridManager.GetManager.debug)
			print (slope * Mathf.Rad2Deg);
		return slope;
	}

	Vector3 TransformCanonicalAxes (Vector3 v) {
		return new Vector3 (v.x, v.z, -v.y);
	}
	Vector3 TransformUnityAxes (Vector3 v) {
		return new Vector3 (v.x, -v.z, v.y);
	}

	float detM2 (float a, float b, float c, float d) {
		return (a * d - b * c);
	}

	float GetTrianglesMaxSlope (Vector3[] origins, int[] triangles) {
		float offset = 1f;
		float height = gridManager.GetMaxHeight (gridManager.GetMapInfos.UseTerrain) + offset;
		RaycastHit hit;
		
		float slope = -1;
		
		for (int i = 0; i < triangles.Length; i++) {
			Vector3 normal = Vector3.zero;
			
			for (int j = 0; j < 3; j++) {
				if (GetRaycast (new Vector3(origins[triangles[i]].x, height, origins[triangles[i++]].z) , Vector3.down, out hit, Mathf.Infinity)) {
					normal += hit.normal;
				}
			}
			i--;
			normal = (normal / 3).normalized;
			float s;
			if ((s = Mathf.Acos (Mathf.Abs (Vector3.Dot (Vector3.up, normal)))) > slope)
				slope = s;
		}
		if (gridManager.GetManager.debug)
			print (slope * Mathf.Rad2Deg);
		return slope;
	}
	
	float GetVerticesMaxSlope (Vector3[] origins) {
		float offset = 1f;
		float height = gridManager.GetMaxHeight (gridManager.GetMapInfos.UseTerrain) + offset;
		RaycastHit hit;
		
		float slope = -1;
		
		for (int i = 0; i < origins.Length; i++) {
			origins[i].y = height;
			if (GetRaycast (origins[i], Vector3.down, out hit, Mathf.Infinity)) {
				Vector3 normal = hit.normal.normalized;
				float s;
				if ((s = Mathf.Acos (Mathf.Abs (Vector3.Dot (Vector3.up, normal)))) > slope)
					slope = s;
			}
		}
		if (gridManager.GetManager.debug)
			print (slope * Mathf.Rad2Deg);
		return slope;
	}
	
	float GetPlaneSlope (Vector3 center) {
		float offset = 1f;
		float height = gridManager.GetMaxHeight (gridManager.GetMapInfos.UseTerrain) + offset;
		RaycastHit hit;
		
		int k = 0;
		Vector3 normal = Vector3.zero;
		
		if (GetRaycast (new Vector3 (center.x, height, center.z), Vector3.down, out hit, Mathf.Infinity)) {
			normal = hit.normal;
			k++;
		}
		
		normal /= k;
		normal.Normalize ();
		float slope = Mathf.Acos (Mathf.Abs (Vector3.Dot (Vector3.up, normal)));
		if (gridManager.GetManager.debug)
			print (slope * Mathf.Rad2Deg);
		return slope;
	}
	
	bool GetRaycast (Vector3 origin, Vector3 direction, out RaycastHit hit, float distance) {
		int layerMask = gridManager.GetLayer;
		if (gridManager.UseLayer)
			return Physics.Raycast (origin, direction, out hit, distance, layerMask);
		else
			return Physics.Raycast (origin, direction, out hit, distance);
	}



	public Vector3[] GetVerticesWorldPosition () {
		Vector3[] vs = GetComponent<MeshFilter>().mesh.vertices;
		var vsWorld = new Vector3[vs.Length];
		int i = 0;
		foreach (Vector3 v in vs)
			vsWorld[i++] = transform.TransformPoint (v);
		return vsWorld;
	}

	public Vector3 GetMeshCenterWorldPosition () {
		return transform.TransformPoint (GetComponent<MeshFilter> ().mesh.bounds.center);
	}

	public int[] GetMeshTriagles () {
		return GetComponent<MeshFilter> ().mesh.triangles;
	}

	public void SetTileTB () {
		if (gridManager.GetMapInfos.useTerrain)
			SetMeshShapeTerrain ();
		else
			SetMeshShapeMesh ();
	}
	
	void SetMesh (Vector3[] newVerts) {
		Mesh mesh = GetComponent<MeshFilter>().mesh;
		mesh.vertices = newVerts;
		
		mesh.RecalculateBounds();
		mesh.RecalculateNormals();
		
		MeshCollider mCollider = GetComponent<MeshCollider>();
		Destroy (mCollider);
		if (tile.Passable)
			gameObject.AddComponent<MeshCollider>();
	}

	Vector3[] SetTileVertsRaycast (bool terrain) {
		Mesh mesh = GetComponent<MeshFilter>().mesh;
		Vector3[] vs = mesh.vertices;
		
		for (int i = 0; i < vs.Length; i++) {
			Vector3 calcVector;
			Vector3 worldVector = transform.TransformPoint (vs[i]);
			
			float offset = 1f;

			float maxHeight = offset;

			maxHeight += gridManager.GetMaxHeight (terrain);

			RaycastHit hit;
			
			calcVector = worldVector;

			int layerMask = gridManager.GetLayer;
			
			var startPoint = new Vector3(worldVector.x, maxHeight + gridManager.GetYOffset, worldVector.z);

			bool raycast;
			if (gridManager.UseLayer)
				raycast = Physics.Raycast (startPoint, Vector3.down, out hit, Mathf.Infinity, layerMask);
			else
				raycast = Physics.Raycast (startPoint, Vector3.down, out hit, Mathf.Infinity);

			if (raycast) {
				calcVector.y = hit.point.y + gridManager.GetYOffset;
			} else
				calcVector.y = gridManager.GetYOffset;

			if (gridManager.GetManager.debug)
				Debug.DrawRay (startPoint, Vector3.down, Color.red, Mathf.Infinity);
			
			vs[i] = transform.InverseTransformPoint (calcVector);
		}
		return vs;
	}

	//------------------------------ MESH ---------------------------------------------//

	void SetMeshShapeMesh () {
		SetMesh (SetTileVertsMesh ());
	}
	
	Vector3[] SetTileVertsMesh () {
		return SetTileVertsRaycast (false);
	}

	//------------------------------ TERRAIN ------------------------------------------//

	void SetMeshShapeTerrain () {
		if (gridManager.GetMapInfos.useTerrainRaycast)
			SetMesh (SetTileVertsTerrainRaycast ());
		else {
			GetHeightmapPosition ();
			SetMesh (SetTileVertsTerrain ());
		}
	}

	//---------- TERRAIN RAYCAST ------------//

	Vector3[] SetTileVertsTerrainRaycast () {
		return SetTileVertsRaycast (true);
	}

	//---------- TERRAIN HEIGHTMAP -----------//

	void GetHeightmapPosition() 
	{
		Vector3 pos = transform.position;
		// find the heightmap position of that hit
		heightmapPos.x = ( pos.x / gridManager.GetMapInfos.GetTerrainSize.x ) * ((float) gridManager.GetMapInfos.GetHeightmapWidth );
		heightmapPos.z = ( pos.z / gridManager.GetMapInfos.GetTerrainSize.z ) * ((float) gridManager.GetMapInfos.GetHeightmapHeight );
		
		// convert to integer
		heightmapPos.x = Mathf.Round( heightmapPos.x );
		heightmapPos.z = Mathf.Round( heightmapPos.z );
		
		// clamp to heightmap dimensions to avoid errors
		heightmapPos.x = Mathf.Clamp( heightmapPos.x, 0, gridManager.GetMapInfos.GetHeightmapWidth - 1 );
		heightmapPos.z = Mathf.Clamp( heightmapPos.z, 0, gridManager.GetMapInfos.GetHeightmapHeight - 1 );
	}

	Vector3 GetHeightmapPosition(Vector3 pos) 
	{
		Vector3 heightmapPosR = new Vector3 (0, 0, 0);
		// find the heightmap position of that hit
		heightmapPosR.x = ( pos.x / gridManager.GetMapInfos.GetTerrainSize.x ) * ((float) gridManager.GetMapInfos.GetHeightmapWidth );
		heightmapPosR.z = ( pos.z / gridManager.GetMapInfos.GetTerrainSize.z ) * ((float) gridManager.GetMapInfos.GetHeightmapHeight );
		
		// convert to integer
		heightmapPosR.x = Mathf.Round( heightmapPosR.x );
		heightmapPosR.z = Mathf.Round( heightmapPosR.z );
		
		// clamp to heightmap dimensions to avoid errors
		heightmapPosR.x = Mathf.Clamp( heightmapPosR.x, 0, gridManager.GetMapInfos.GetHeightmapWidth - 1 );
		heightmapPosR.z = Mathf.Clamp( heightmapPosR.z, 0, gridManager.GetMapInfos.GetHeightmapHeight - 1 );

		return heightmapPosR;
	}

	Vector3[] SetTileVertsTerrain () {
		Mesh mesh = GetComponent<MeshFilter>().mesh;
		Vector3[] vs = mesh.vertices;

		for (int i = 0; i < vs.Length; i++) {
			Vector3 calcVector;

			Vector3 worldVertex = transform.TransformPoint(vs[i]);
			Vector3 vertexPos = GetHeightmapPosition(worldVertex);

			calcVector.x = vs[i].x;

			float calcPosX = vertexPos.x;
			calcPosX = Mathf.Clamp(calcPosX, 0, gridManager.GetMapInfos.GetHeightmapWidth - 1);

			float calcPosZ = vertexPos.z;
			calcPosZ = Mathf.Clamp(calcPosZ, 0, gridManager.GetMapInfos.GetHeightmapHeight - 1);

			calcVector.z = gridManager.GetMapInfos.GetHightmapData[(int)calcPosZ, (int)calcPosX] * gridManager.GetMapInfos.GetTerrainSize.y;
			calcVector.z += gridManager.GetYOffset;

			calcVector.y = vs[i].y;

			vs[i] = calcVector;
		}
		return vs;
	}
}
