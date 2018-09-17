using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BezierBern : MonoBehaviour {

	public GameObject pointPrefab; //The prefab
	public float lineDivideScale;
	List<GameObject> basePoints;
	public Vector3[] initVexters;
	List<List<Vector3[]>> algIterations; // Iteration of DeCasteljau -> Iteration of CurveLevel-1 (last is Spline) -> PointList
	public List<Vector3> curvePoints;


	private bool inSpline;
	private int degree;
	private LineManager lineManager;
	private int iterationCounter;

	bool rational = false;
	float weight;
	public float[] weights;
	// Use this for initialization

	void Start () {
		if (this.gameObject.transform.parent == null) {
			inSpline = false;
			basePoints = Create_FirstPointList(); // Game Object List of POINTS
			initVexters = FromGO_toVectorList(basePoints).ToArray();
			weights = new float[degree+1];
			for (int w = 0; w < degree+1; w++) weights[w] = 1.0f; 
			//Initialization();
			curvePoints = DeCastelIter(6);
			lineManager = GetComponent<LineManager> ();
			lineManager.UpdateData (initVexters, curvePoints);
		}
	}

	// Update is called once per frame
	void FixedUpdate () {
	//	if (rational) initVexters = Derationalize(initVexters);
		
		if (!inSpline) initVexters = FromGO_toVectorList(basePoints).ToArray();
	//	Use_DeCastaljau (false);
		
		//lineManager.UpdateData (new List<Vector3[]>{initVexters}, curvePoints);
		curvePoints = DeCastelIter(50);
 		lineManager.UpdateData (initVexters, curvePoints);
	}

	private void Initialization(){
		iterationCounter = 0;
		List<Vector3[]> firstIter = new List<Vector3[]>{initVexters};
		algIterations = new List<List<Vector3[]>> { firstIter } ;  // List of List of VectorSplineChain
		curvePoints = new List<Vector3>();


		lineManager = GetComponent<LineManager> ();
		lineManager.UpdateData (algIterations[0], curvePoints);

		if (lineDivideScale == 0.0f) { lineDivideScale = 0.5f; } // veikia tik su 0.5

		for (int i = 0; i < 5; i++) Use_DeCastaljau (true);
	}

	public void Init_fromSpline (Vector3[] initVexters) {
		Set_initVexters(initVexters);
		inSpline = true;
		degree = initVexters.Length - 1;
		Initialization();
	}

	public void Init_fromSpline (Vector3[] initVexters, float weight) {
		this.weight = weight;
		this.rational = true;
		Init_fromSpline (initVexters);
	}

	public void Set_initVexters(Vector3[] initVexters){
		this.initVexters = initVexters;
	}

	public void Set_initVexters(Vector3[] initVexters, float weight){
		this.weight = weight;
		this.Set_initVexters(initVexters);
	}

	private List<Vector3> FromGO_toVectorList(List<GameObject> GoList) {
		List<Vector3> vecList = new List<Vector3>();
		foreach (GameObject GO in GoList) {
			vecList.Add(GO.transform.position);
		}
		return vecList;
	}

	private List<GameObject> Create_FirstPointList () {
		//GameObject[] initPoints = GameObject.FindGameObjectsWithTag ("Point").OrderBy (go => go.name).ToArray ();
		List<GameObject> pointList = new List<GameObject> ();
		Transform parentTrans = this.gameObject.transform;
		foreach (Transform child in parentTrans) {
			if ( child.tag == "Point") {
				GameObject point = child.gameObject;
				pointList.Add (point);
			}
		}
		this.degree = pointList.Count - 1;
		return pointList;
	}



	private int Factorial(int i){
    	if (i <= 1) return 1;
    	return i * Factorial(i - 1);
	}
	private float Pow(float value, float power) {
		return Mathf.Pow(value, power);
	}

	private float Bernstein(int iter, float t) {
		if (iter == 0) return Pow(1.0f-t, degree);
		if (degree == iter) return Pow(t,iter);
		int sk = degree-iter;
		return Factorial(degree) / (Factorial(iter) * sk) * Pow(1.0f-t, sk) * Pow(t, iter);
		//return Pow(t, iter)*Pow((1-t), sk);
	}

	private List<Vector3> DeCastelIter (int steps) {
		float step = 1.0f/steps;
		float road = step;
		List<Vector3> curve = new List<Vector3>();

		for (int t = 0; t < steps-1; t++) {
			Vector3 newCurvePoint = new Vector3(0,0,0);
			float sumW = 0.0f;
			//newCurvePoint = Pow((1-road),3)*initVexters[0] + 3*road*Pow((1-road),2)*initVexters[1] + 3*Pow(road,2)*(1-road)*initVexters[2] + Pow(road,3)*initVexters[3];
			if (degree==3) {
				newCurvePoint = weights[0]*Pow((1-road),3)*initVexters[0] + weights[1]*3*road*Pow((1-road),2)*initVexters[1] + weights[2]*3*Pow(road,2)*(1-road)*initVexters[2] + weights[3]*Pow(road,3)*initVexters[3];
				sumW = weights[0]*Pow((1-road),3) + weights[1]*3*road*Pow((1-road),2) + weights[2]*3*Pow(road,2)*(1-road) + weights[3]*Pow(road,3);
				newCurvePoint = newCurvePoint/sumW;

			//newCurvePoint = Pow((1-road),2) * initVexters[0] + 2*(1-road)*road * initVexters[1] + Pow(road,2)* initVexters[2];
			} else if (degree == 2) {
				newCurvePoint = weights[0] * Pow((1-road),2) * initVexters[0] + weights[1] *2*(1-road)*road * initVexters[1] + weights[2] *Pow(road,2)* initVexters[2];
				sumW = weights[0]*Pow((1-road),2) + weights[1]*2*(1-road)*road + weights[2]*Pow(road,2);
				newCurvePoint = newCurvePoint/sumW;
			}
			/*
			for (int i=0; i<initVexters.Length; i++) {
				newCurvePoint += weights[i]*initVexters[i] * Bernstein(i, road); 
				sumW += Bernstein(i, road);
			}
			newCurvePoint = newCurvePoint / sumW;
			*/

			//print(newCurvePoint + " " + road);
			curve.Add(newCurvePoint);
			road+=step;
		}
		curve.Insert(0, initVexters[0]);
		curve.Add(initVexters.Last());
		return curve;
	}

		public void Use_DeCastaljau (bool nextIteration) {
		if (nextIteration) {
			//Jei nauja iteracija
			//DeCastelProduct result = DeCastelIter (this.algIterations, this.curvePoints);
			//this.algIterations.Add (result.splineChainList);
			//this.curvePoints = result.curvePoints;

			iterationCounter++;
			lineManager.UpdateData (algIterations[0], curvePoints);

		} else {
			/*
			// Jei tik Updeitinama info
			List<Vector3[]> dummyFirstIter = new List<Vector3[]>{ initVexters };
			List<List<Vector3[]>> dummy_algIterations = new List<List<Vector3[]>> { dummyFirstIter };

			List<Vector3> dummy_CurvePoints = new List<Vector3>();
 
			for (int i = 0; i <= iterationCounter; i++) {
				DeCastelProduct result = DeCastelIter(dummy_algIterations, dummy_CurvePoints);
				dummy_algIterations.Add (result.splineChainList);
				dummy_CurvePoints = result.curvePoints; // newCurvePoints
			}
			this.algIterations = dummy_algIterations;
			this.curvePoints = dummy_CurvePoints;
			lineManager.UpdateData (algIterations[0], curvePoints);
		*/
		}
	}

/* 
		private DeCastelProduct DeCastelIter (List<List<Vector3[]>> algIterations, List<Vector3> curvePoints) {
		List<Vector3[]> splineList = algIterations.Last();
		if (curvePoints.Count == 0) curvePoints = new List<Vector3>{algIterations[0][0][0], algIterations.Last().Last().Last()};

		List<List<Vector3>> iterLvlList = new List<List<Vector3>> ();
		List<Vector3[]> newSplineList = new List<Vector3[]> ();

		int curveIndex = 1;
		foreach (Vector3[] spline in splineList) { // Keliaujam per MidPoint search lygius (lygis = kreives_laipsnis - 1) - Kiek kartu reikes ieskoti midpointu, kol gausim tiese, kurioje ieskosime curvepoint
			List<Vector3> newBegSpline = new List<Vector3> (); //Isvalom
			List<Vector3> newEndSpline = new List<Vector3> (); // Isvalom

			List<Vector3> midPoints = spline.ToList (); // pirmasis iterList

			//Randam lygio midpointus
			for (int lvl = 0; lvl < degree; lvl++) { // [0;3] nusileidziam iki curvePoint
				List<Vector3> newMidPointList = new List<Vector3> (); // Isvalom

				for (int p = 0; p < midPoints.Count - 1; p++) { // Nuo 0 iki priespaskutinio imtinai

					if (lvl < degree - 1) { // Skaiciuojam midPoints
						Vector3 midPoint = FindMidPoint (midPoints[p], midPoints[p + 1]);
						newMidPointList.Add (midPoint);
						// Each spline makes x2 newSpliness
						// BegOfSpline + mpN...iter + cp
						if (lvl == 0 && p == 0) { newBegSpline.Add (spline[0]); } // idedam begPoint
						if (p == 0 && lvl != degree - 1) { newBegSpline.Add (midPoint); } // idedam fisrt MidPoint of N-iter
						// CP + mpN...iter + EndOfSpline
						if (lvl == 0 && p == midPoints.Count - 2) { newEndSpline.Add (spline[degree]); } // idedam endPoint
						if (p == midPoints.Count - 2 && lvl != degree - 1) { newEndSpline.Insert (0, midPoint); } // idedam last MidPoint of N-iter

					} else if (lvl == degree - 1) { //Skaiciuojam Curve Point
						Vector3 curvePoint = FindMidPoint (midPoints[p], midPoints[p + 1]);  //* weight;
						curvePoints.Insert (curveIndex, curvePoint);
						curveIndex += 2;

						newBegSpline.Add (curvePoint); // newBegSpline pabaiga
						newEndSpline.Insert (0, curvePoint); // newEndpline pradzia	
					}
				}
				midPoints = new List<Vector3> (newMidPointList);
				iterLvlList.Add (newMidPointList);
			}
			newSplineList.Add (newBegSpline.ToArray ());
			newSplineList.Add (newEndSpline.ToArray ());
		}
		return new DeCastelProduct(newSplineList, curvePoints);
	}
 */













	private Vector3[] Derationalize(Vector3[] points) {
		List<Vector3> newPoints = new List<Vector3>();
		foreach (Vector3 p in points) {
			Vector3 newP = p / weight;
			newPoints.Add(newP);
		}
		return newPoints.ToArray();
	}

	private Vector3 Rationalize(Vector3 point) {
		return point / weight;
	}

	private Vector3 FindMidPoint (Vector3 point, Vector3 nextPoint) {

		float midPointX = (point.x + nextPoint.x) * 0.5f;
		float midPointY = (point.y + nextPoint.y) * 0.5f;
		float midPointZ = (point.z + nextPoint.z) * 0.5f;
		return new Vector3(midPointX, midPointY, midPointZ);
	}

	private GameObject CreatePoint (Vector3 vector) {
		GameObject point = Instantiate (pointPrefab, vector, Quaternion.identity);
		point.transform.parent = this.transform;
		return point;
	}

}