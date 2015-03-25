using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public enum SlopeDetectionMode {
	Euclidean, Vertices, Triangles, Face
}

public class GridManager : MonoBehaviour {
	private static List<GridManager> instances = null;
	private int actualInstanceIndex = -1;

	public GameObject QuadTile;

	public GridManagerCore[] cores;

	public bool debug = false;

	void Awake () {
		if (instances == null)
			instances = new List<GridManager>();
		instances.Add (this);
		actualInstanceIndex = instances.IndexOf (this);
	}

	// Use this for initialization
	void Start () {
		foreach (GridManagerCore core in cores)
			core.Init (this);
	}
	
	// Update is called once per frame
	void Update () {

	}

	public GridManager GetActualInstance {
		get {
			return GridManager.instances[this.actualInstanceIndex];
		}
	}
	public static GridManager[] GetInstances {
		get {
			return GridManager.instances.ToArray ();
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

	private GameObject tile;

	public string name = "Grid";

	public MapInfo infos;

	public float offsetY = 0.25f;

	public LayerMask mask;

	public float maxSlope = 90f;
	public SlopeDetectionMode slopeDetectionMode;
	public bool mediumSlope = true;

	private Vector3 tileDim;
	private Vector3 mapDim;

	private GameObject[][] TileMatrix;

	private GameObject selectedTile = null;

	public void Init (GridManager manager) {
		this.manager = manager;
		tile = this.manager.QuadTile;

		if (instances == null)
			instances = new List<GridManagerCore>();
		instances.Add (this);
		actualInstanceIndex = instances.IndexOf (this);

		infos.SetMap ();

		SetSize ();
		CreateGrid ();
	}

	private void SetSize () {
		tileDim = tile.GetComponent<Renderer>().bounds.size;
		if (infos.UseTerrain)
			mapDim = infos.GetActiveMap.GetComponent<Terrain>().terrainData.size;
		else
			mapDim = infos.GetActiveMap.GetComponent<Renderer>().bounds.size;
	}

	private Sides GetSides () {
		return new Sides ((int)(mapDim.x / tileDim.x), (int)(mapDim.z / tileDim.z));
	}

	private Vector2 CalcGridSize () {
		int nrOfSidesX = GetSides ().X;
		int nrOfSidesZ = GetSides ().Z;
		return new Vector2 (nrOfSidesX, nrOfSidesZ);
	}

	private Vector3 GetInitPos () {
		Vector2 xzOffsets = new Vector2 ((mapDim.x - GetSides ().X * tileDim.x) / 2, (mapDim.z - GetSides ().Z * tileDim.z) / 2);
		if (infos.UseTerrain)
			return new Vector3 (tileDim.x / 2 + infos.GetActiveMap.transform.position.x + xzOffsets.x, infos.GetActiveMap.transform.position.y, mapDim.z - tileDim.z / 2 + infos.GetActiveMap.transform.position.z - xzOffsets.y);
		else
			return new Vector3 (-mapDim.x / 2 + tileDim.x / 2 + infos.GetActiveMap.GetComponent<Renderer>().bounds.center.x + xzOffsets.x, infos.GetActiveMap.GetComponent<Renderer>().bounds.center.y, mapDim.z/ 2 - tileDim.z / 2 + infos.GetActiveMap.GetComponent<Renderer>().bounds.center.z - xzOffsets.y);
	}

	public Vector3 GetWorldCoord (Vector2 tilePos) {
		Vector3 initPos = GetInitPos ();
		float x = initPos.x + tilePos.x * tileDim.x;
		float z = initPos.z - tilePos.y * tileDim.z;
		return new Vector3(x, offsetY, z);
	}

	private void CreateGrid () {
		Vector2 gridSize = CalcGridSize ();
		GameObject quadGridGO = new GameObject("QuadGrid");
		quadGridGO.transform.parent = GetManager.transform;
		TileMatrix = new GameObject[(int)gridSize.x][];
		for (int i = 0; i < TileMatrix.Length; i++)
			TileMatrix[i] = new GameObject[(int)gridSize.y];

		for (int x = 0; x < (int)gridSize.x; x++) {
			for (int z = 0; z < (int)gridSize.y; z++) {
				GameObject tile = (GameObject)GameObject.Instantiate (this.tile);
				Vector2 tilePos = new Vector2(x, z);
				tile.transform.position = GetWorldCoord (tilePos);
				tile.transform.parent = quadGridGO.transform.parent;
				TileManager tm = tile.GetComponent<TileManager> ();
				tm.ManagerIndex = actualInstanceIndex;
				tm.Core = this;
				tm.Manager = manager;
				TileMatrix[x][z] = tile;
			}
		}
	}

	public GameObject SelectedTile {
		get {
			return selectedTile;
		}
		set {
			selectedTile = value;
		}
	}

	public GridManager GetManager {
		get {
			return this.manager;
		}
	}

	public float GetMaxHeight {
		get {
			if (infos.UseTerrain)
				return infos.GetActiveMap.transform.TransformPoint (infos.GetActiveMap.GetComponent<Terrain>().terrainData.size).y;
			else
				return infos.GetActiveMap.transform.TransformPoint (infos.GetActiveMap.GetComponent<Renderer>().bounds.size).y;
		}
	}

	public int GetActualInstanceIndex {
		get {
			return this.actualInstanceIndex;
		}
	}

	public static GridManagerCore[] GetInstances {
		get {
			return GridManagerCore.instances.ToArray ();
		}
	}

	public static GridManagerCore GetInstance (int index) {
		return GridManagerCore.instances[index];
	}

	public GridManagerCore GetActualInstance {
		get {
			return GridManagerCore.instances[this.actualInstanceIndex];
		}
	}

	public static GridManager[] GetGridManagerInstances {
		get {
			return GridManager.GetInstances;
		}
	}
	public LayerMask GetLayerMask {
		get {
			return this.mask;
		}
	}
}

[System.Serializable()]
public class MapInfo {
	public GameObject MapMesh;
	public GameObject MapTerrain;
	public bool useTerrain;
	private GameObject activeMap = null;

	public void SetMap () {
		if (MapMesh == null && MapTerrain != null) {
			activeMap = MapTerrain;
			useTerrain = true;
		} else if (MapMesh != null && MapTerrain == null) {
			activeMap = MapMesh;
			useTerrain = false;
		} else if (MapMesh != null && MapTerrain != null) {
			if (useTerrain) {
				activeMap = MapTerrain;
			} else
				activeMap = MapMesh;
		} else
			activeMap = null;
	}

	public GameObject GetActiveMap {
		get {
			return activeMap;
		}
	}

	public bool UseTerrain {
		get {
			return useTerrain;
		}
	}
}

public class Sides {
	private int x, z;
	public Sides (int x, int z) {
		this.x = x;
		this.z = z;
	}
	public int X { get { return this.x; } }
	public int Z { get { return this.z; } }
}