using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class LineManager : MonoBehaviour {

	//public GameObject algorithm;
	public Color polygonColor;
	public Color curveColor;
	public float lineWide = 1.0f;
	public float curveWide = 1.0f;

	private GameObject kreive;
	private GameObject lauzte;

	//private List<GameObject> polygonList;
	private List<GameObject> curvePoint_List;

	void Awake () {
		Init();
	}

	public void Init() {
		kreive = new GameObject ();
		kreive.transform.parent = gameObject.transform;
		kreive.name = "kreive";
		AddLineRenderer (kreive, curveWide, curveColor);

		lauzte = new GameObject ();
		lauzte.name = "lauzte";
		lauzte.transform.parent = gameObject.transform;
		AddLineRenderer (lauzte, lineWide, polygonColor);
		lauzte.SetActive(false);
	}

	public void ReAddCurve (List<Vector3> pointList) {
		SetPolygonPoints (kreive, pointList);
	}

	// Local privates Methods:
	private void AddLineRenderer (GameObject polygon, float widness, Color color) {
		LineRenderer lineRenderer = polygon.AddComponent<LineRenderer> ();
		//lineRenderer = polygon.GetComponent<LineRenderer> ();
		lineRenderer.material = new Material (Shader.Find ("Particles/Additive"));
		//lineRenderer.widthMultiplier = 0.04f;
		// A simple 2 color gradient with a fixed alpha of 1.0f.
		float alpha = 1.0f;
		Gradient gradient = new Gradient ();
		gradient.SetKeys (
			new GradientColorKey[] { new GradientColorKey (color, 0.0f), new GradientColorKey (color, 1.0f) },
			new GradientAlphaKey[] { new GradientAlphaKey (alpha, 0.0f), new GradientAlphaKey (alpha, 1.0f) }
		);
		lineRenderer.colorGradient = gradient;

		lineRenderer.widthMultiplier = widness;
		lineRenderer.SetPosition (1, new Vector3 (0, 0, 0));
	}

	private void SetPolygonPoints (GameObject polygon, List<Vector3> pointList) {
		LineRenderer lineRenderer = polygon.GetComponent<LineRenderer> ();
		lineRenderer.positionCount = pointList.Count;
		for (int i = 0; i < pointList.Count; i++) {
			lineRenderer.SetPosition (i, pointList[i]);
		}
	}

	public void UpdateData (List<Vector3[]> splineChain, List<Vector3> curvePoints) {
		List<Vector3> pointList = splineChain.SelectMany(x => x).ToList();
		SetPolygonPoints (lauzte, pointList);
		SetPolygonPoints (kreive, curvePoints);
	}

	
	public void UpdateData (Vector3[] controlPoints, List<Vector3> curvePoints) {
		List<Vector3> pointList = controlPoints.ToList();
		SetPolygonPoints (lauzte, pointList);
		SetPolygonPoints (kreive, curvePoints);
	}

	public void UpdateData_forSpline (List<Vector3> pointList) {
		if (lauzte.name != "splineLauzte") lauzte.name = "splineLauzte";
		SetPolygonPoints (lauzte, pointList);
	}

}