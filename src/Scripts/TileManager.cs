using UnityEngine;
using System.Collections;

public class TileManager : MonoBehaviour {

	private int managerIndex = -1;
	private GridManagerCore core;
	private GridManager manager;

	public GameObject TempTower;

	public Material normalMaterial, mouseOverMaterial, selectedTileMaterial;

	private bool selected = false;

	private bool actived = true;

	// Use this for initialization
	void Start () {
		DetectSlope ();
		SetMesh ();
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public bool Active {
		get {
			return this.actived;
		}
	}

	public Vector3 GetMeshCenterInWorldPosition {
		get {
			return transform.TransformPoint (GetComponent<MeshFilter> ().mesh.bounds.center);
		}
	}

	public int ManagerIndex {
		get {
			return managerIndex;
		}
		set {
			if (managerIndex == -1)
				managerIndex = value;
		}
	}

	public GridManagerCore Core {
		get {
			return core;
		}
		set {
			if (core == null)
				core = value;
		}
	}

	public GridManager Manager {
		get {
			return manager;
		}
		set {
			if (manager == null)
				manager = value;
		}
	}

	private void DetectSlope () {
		switch (core.slopeDetectionMode) {
		case SlopeDetectionMode.Face:
			if (GetPlaneSlope () * Mathf.Rad2Deg > core.maxSlope)
				Deactivate ();
			break;
		case SlopeDetectionMode.Triangles:
			if (GetTrianglesMaxSlope () * Mathf.Rad2Deg > core.maxSlope)
				Deactivate ();
			break;
		case SlopeDetectionMode.Vertices:
			if (GetVerticesMaxSlope () * Mathf.Rad2Deg > core.maxSlope)
				Deactivate ();
			break;
		default:
			if (GetEuclideanMaxSlope () * Mathf.Rad2Deg > core.maxSlope)
				Deactivate ();
			break;
		}
	}

	private void Deactivate () {
		this.GetComponent<Renderer>().material.color = new Color (0, 0, 0, 0);
		this.actived = false;
	}

	private float GetPlaneSlope () {
		Mesh mesh = GetComponent<MeshFilter> ().mesh;
		Vector3 center = mesh.bounds.center;
		center = transform.TransformPoint (center);

		float height = core.GetMaxHeight + 1;

		center.y = height;

		Vector3 normal = Vector3.zero;

		RaycastHit hit;

		if (GetRaycastDown (center, out hit))
		    normal = hit.normal;

		normal.Normalize ();

		float slope = Mathf.Acos (Mathf.Abs (Vector3.Dot (Vector3.up, normal)));

		if (manager.debug)
			print (slope * Mathf.Rad2Deg);

		return slope;
	}

	private float GetVerticesMaxSlope () {
		Mesh mesh = GetComponent<MeshFilter> ().mesh;
		Vector3[] origins = mesh.vertices;

		float height = core.GetMaxHeight + 1;

		RaycastHit hit;

		float slope = 0;
		if (!core.mediumSlope)
			slope = -1;

		for (int i = 0; i < origins.Length; i++) {

			origins[i] = transform.TransformPoint (origins[i]);
			origins[i].y = height;

			if (GetRaycastDown (origins[i], out hit)) {
				Vector3 normal = hit.normal.normalized;
				float s = Mathf.Acos (Mathf.Abs (Vector3.Dot (Vector3.up, normal)));

				if (core.mediumSlope)
					slope += s;
				else
					if (s > slope)
						slope = s;
			}
		}
		if (core.mediumSlope)
			slope /= origins.Length;

		if (manager.debug)
			print (slope * Mathf.Rad2Deg);

		return slope;
	}

	private float GetTrianglesMaxSlope () {
		Mesh mesh = GetComponent<MeshFilter> ().mesh;
		int[] triangles = mesh.triangles;
		Vector3[] origins = mesh.vertices;

		float height = core.GetMaxHeight + 1;
		RaycastHit hit;

		float slope = 0;

		if (!core.mediumSlope)
			slope = -1;

		for (int i = 0; i < triangles.Length; i++) {
			Vector3 normal = Vector3.zero;

			for (int j = 0; j < 3; j++) {
				Vector3 tVect = transform.TransformPoint (origins[triangles[i++]]);
				if (GetRaycastDown (new Vector3(tVect.x, height, tVect.z), out hit))
					normal += hit.normal;
			}
			--i;
			normal = (normal / 3).normalized;
			float s = Mathf.Acos (Mathf.Abs (Vector3.Dot (Vector3.up, normal)));

			if (core.mediumSlope)
				slope += s;
			else
				if (s > slope)
					slope = s;
		}

		if (core.mediumSlope)
			slope /= (triangles.Length / 3);

		if (manager.debug)
			print (slope * Mathf.Rad2Deg);
		
		return slope;
	}

	private float GetEuclideanMaxSlope () {
		Mesh mesh = GetComponent<MeshFilter> ().mesh;
		int[] triangles = mesh.triangles;
		Vector3[] origins = mesh.vertices;

		float height = core.GetMaxHeight + 1;
		RaycastHit hit;
		
		float slope;
		if (core.mediumSlope)
			slope = 0;
		else
			slope = -1;
		
		for (int i = 0; i < triangles.Length; i++) {
			Vector3 normal = Vector3.zero;

			Vector3 vTemp = transform.TransformPoint (origins[triangles[i++]]);
			Vector3 v1 = new Vector3(vTemp.x, height, vTemp.z);

			vTemp = transform.TransformPoint (origins[triangles[i++]]);
			Vector3 v2 = new Vector3(vTemp.x, height, vTemp.z);

			vTemp = transform.TransformPoint (origins[triangles[i]]);
			Vector3 v3 = new Vector3(vTemp.x, height, vTemp.z);

			if (GetRaycastDown (v1, out hit))
				v1 = hit.point;
			if (GetRaycastDown (v2, out hit))
				v2 = hit.point;

			if (GetRaycastDown (v3, out hit))
				v3 = hit.point;

			v1 = TransformCanonicalAxes (v1);
			v2 = TransformCanonicalAxes (v2);
			v3 = TransformCanonicalAxes (v3);
			normal = new Vector3 (detM2 (v2.y - v1.y, v2.z - v1.z, v3.y - v1.y, v3.z - v1.z), -detM2 (v2.x - v1.x, v2.z - v1.z, v3.x - v1.x, v3.z - v1.z), detM2 (v2.x - v1.x, v2.y - v1.y, v3.x - v1.x, v3.y - v1.y));
			normal.Normalize ();
			normal = TransformUnityAxes (normal);
			float s = Mathf.Acos (Mathf.Abs (Vector3.Dot (Vector3.up, normal)));

			if (core.mediumSlope)
				slope += s;
			else 
				if (s > slope)
					slope = s;
		}

		if (core.mediumSlope)
			slope /= (triangles.Length / 3);

		if (manager.debug)
			print (slope * Mathf.Rad2Deg);
		return slope;
	}

	private Vector3 TransformCanonicalAxes (Vector3 v) {
		return new Vector3 (v.x, v.z, -v.y);
	}

	private Vector3 TransformUnityAxes (Vector3 v) {
		return new Vector3 (v.x, -v.z, v.y);
	}

	/* 
	 * 		MATRIX
	 * 		[a b]
	 * 		[c d]
	 */
	private float detM2 (float a, float b, float c, float d) {
		return (a * d - b * c);
	}

	void OnMouseOver () {		
		if (Input.GetMouseButtonUp(0)) {
			GetComponent<Renderer>().material = selectedTileMaterial;
			selected = true;
			core.SelectedTile = gameObject;
			GameObject tower = GameObject.Instantiate (TempTower) as GameObject;
			tower.transform.position = GetMeshCenterInWorldPosition + new Vector3 (0, tower.transform.TransformPoint (tower.GetComponent<MeshFilter> ().mesh.bounds.center).y - tower.transform.TransformPoint (tower.GetComponent<MeshFilter> ().mesh.bounds.min).y, 0);
		} else if (!selected) {
			GetComponent<Renderer>().material = mouseOverMaterial;
		}
	}

	void OnMouseEnter () {
		GetComponent<Renderer>().material = mouseOverMaterial;
		selected = false;
		core.SelectedTile = null;
	}

	void OnMouseExit () {
		GetComponent<Renderer>().material = normalMaterial;
		selected = false;
		core.SelectedTile = null;
	}

	private void SetMesh () {
		Mesh mesh = GetComponent<MeshFilter> ().mesh;
		var vertices = new Vector3[mesh.vertices.Length];
		for (int i = 0; i < mesh.vertices.Length; i++) {
			Vector3 wPoint = transform.TransformPoint (mesh.vertices[i]);
			float yHeight = Core.GetMaxHeight + 1;
			RaycastHit hit;
			Vector3 calcVert = Vector3.zero;
			if (GetRaycastDown (new Vector3 (wPoint.x, yHeight, wPoint.z), out hit)) {
				calcVert = hit.point;
			}
			calcVert.y += Core.offsetY;
			calcVert.x = wPoint.x;
			calcVert.z = wPoint.z;
			vertices[i] = transform.InverseTransformPoint (calcVert);
		}
		mesh.vertices = vertices;
		mesh.RecalculateBounds ();
		mesh.RecalculateNormals ();
		Destroy (GetComponent<MeshCollider> ());
		if (this.actived)
			gameObject.AddComponent<MeshCollider> ();
	}
	private bool GetRaycastDown (Vector3 point, out RaycastHit hit) {
		if (Manager.debug)
			Debug.DrawRay (point, Vector3.down, Color.red, Mathf.Infinity);
		return Physics.Raycast (point, Vector3.down, out hit, Mathf.Infinity, Core.GetLayerMask);
	}

}
