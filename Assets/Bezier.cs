using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Bezier : MonoBehaviour {


	public GameObject pointPrefab; //The prefab
	public float lineDivideScale;
	List<GameObject> basePoints;
	public Vector3[] initVexters;
	public List<List<Vector3[]>> algIterations; // Iteration of DeCasteljau -> Iteration of CurveLevel-1 (last is Spline) -> PointList
	List<Vector3> curvePoints;

	public bool rational = false;

	[Range(-1.0f, 5.0f)]
	public float[] weights;


	private bool inSpline;
	private int degree;
	private LineManager lineManager;
	private int iterationCounter;


	// Use this for initialization

	void Awake () {
		
	}
	void Start () {
		if (this.gameObject.transform.parent == null) {
			inSpline = false;
			basePoints = Create_FirstPointList (); // Game Object List of POINTS
			initVexters = FromGO_toVectorList(basePoints).ToArray();
			Initialization();
		}
	}

	// Update is called once per frame
	void FixedUpdate () {
		if (!inSpline) initVexters = FromGO_toVectorList(basePoints).ToArray();
		Use_DeCastaljau (false);
		curvePoints = UseBerstein(50);
		lineManager.UpdateData (initVexters, curvePoints);
	}

	private void Initialization(){
		iterationCounter = 0;
		List<Vector3[]> firstIter = new List<Vector3[]>{initVexters};
		algIterations = new List<List<Vector3[]>> { firstIter } ;  // List of List of VectorSplineChain
		curvePoints = new List<Vector3>();

		if (rational) {
			//print(degree);
			weights = new float[degree-1];
			for (int w = 0; w < weights.Length; w++) weights[w] = 1.0f; 
		}
		curvePoints = UseBerstein(60);
		
		lineManager = GetComponent<LineManager> ();
		lineManager.UpdateData (firstIter, curvePoints);
		//if (lineDivideScale == 0.0f) { lineDivideScale = 0.5f; } // veikia tik su 0.5
		//for (int i = 0; i < 5; i++) Use_DeCastaljau (true);
	}

	public void Init_fromSpline (Vector3[] initVexters) {
		Set_initVexters(initVexters);
		inSpline = true;
		degree = initVexters.Length - 1;
		Initialization();
	}

	public void Init_fromSpline (Vector3[] initVexters, bool rational) {
		this.rational = true;
		Init_fromSpline (initVexters);
	}

	public void Set_initVexters(Vector3[] initVexters){
		this.initVexters = initVexters;
	}

	public void Set_initVexters(Vector3[] initVexters, float[] weights){
		this.weights = weights;
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

	// ################### BERSTEIN POLYNOMIAL ###########################
	private int Factorial(int i){
    	if (i <= 1) return 1;
    	return i * Factorial(i - 1);
	}
	private float Pow(float value, float power) {
		return Mathf.Pow(value, power);
	}

	private float Bernstein(int iter, float t) { // nenaudoju
		//if (iter == 0) return Pow(1.0f-t, degree);
		//if (degree == iter) return Pow(t,iter);
		int sk = degree-iter;
		return Factorial(degree) / (Factorial(iter) * Factorial(sk)) * Pow(1.0f-t, sk) * Pow(t, iter);
		//return Pow(t, iter)*Pow((1-t), sk);
	}

	private List<Vector3> UseBerstein(int steps) {
		float step = 1.0f/steps;
		float road = step;
		List<Vector3> curve = new List<Vector3>();

		for (int t = 0; t < steps-1; t++) {
			Vector3 newCurvePoint = new Vector3(0,0,0);
			float sumW = 0.0f;
			/* 
			if (degree==3) {
				if (rational) {
					newCurvePoint = Pow((1-road),3)*initVexters[0] + weights[0]*3*road*Pow((1-road),2)*initVexters[1] + weights[1]*3*Pow(road,2)*(1-road)*initVexters[2] + Pow(road,3)*initVexters[3];
					sumW = Pow((1-road),3) + weights[0]*3*road*Pow((1-road),2) + weights[1]*3*Pow(road,2)*(1-road) + Pow(road,3);
					newCurvePoint = newCurvePoint / sumW;
				} else {
					newCurvePoint = Pow((1-road),3)*initVexters[0] + 3*road*Pow((1-road),2)*initVexters[1] + 3*Pow(road,2)*(1-road)*initVexters[2] + Pow(road,3)*initVexters[3];
				}

			//newCurvePoint = Pow((1-road),2) * initVexters[0] + 2*(1-road)*road * initVexters[1] + Pow(road,2)* initVexters[2];
			} else if (degree == 2) {
				if (rational) {
					newCurvePoint = Pow((1-road),2) * initVexters[0] + weights[0]*2*(1-road)*road*initVexters[1] + Pow(road,2)* initVexters[2];
					sumW = Pow((1-road),2) + weights[0]*2*(1-road)*road + Pow(road,2);
					newCurvePoint = newCurvePoint / sumW;
				} else {
					newCurvePoint = Pow((1-road),2) * initVexters[0] + 2*(1-road)*road * initVexters[1] + Pow(road,2)* initVexters[2];
				}
			}
			*/
			
			//print(weights.Length);
			for (int i=0; i<initVexters.Length; i++) {
				if (rational) {
					float w = 1.0f;
					if (i==0 || i==initVexters.Length-1) { w = 1.0f; 
					} else { w = weights[i-1]; }	
					newCurvePoint += w * initVexters[i] * Bernstein(i, road); 
					sumW += w * Bernstein(i, road);
				} else {
					newCurvePoint += initVexters[i] * Bernstein(i, road); 
				}
			}
			if (rational) newCurvePoint = newCurvePoint / sumW;
			
			//print(newCurvePoint + " " + road);
			curve.Add(newCurvePoint);
			road+=step;
		}
		curve.Insert(0, initVexters[0]);
		curve.Add(initVexters.Last());
		return curve;
	}


	// ################### DeCasteljau ###########################
	public void Use_DeCastaljau (bool nextIteration) {
		if (nextIteration) {
			//Jei nauja iteracija
			DeCastelProduct result = DeCastelIter (this.algIterations, this.curvePoints);
			this.algIterations.Add (result.splineChainList);
			//this.curvePoints = result.curvePoints;

			iterationCounter++;
			lineManager.UpdateData (algIterations[0], curvePoints);

		} else {
			// Jei tik Updeitinama info
			List<Vector3[]> dummyFirstIter = new List<Vector3[]>{ initVexters };
			List<List<Vector3[]>> dummy_algIterations = new List<List<Vector3[]>> { dummyFirstIter };

			List<Vector3> dummy_CurvePoints = new List<Vector3>();

			for (int i = 0; i <= iterationCounter; i++) {
				DeCastelProduct result = DeCastelIter(dummy_algIterations, dummy_CurvePoints);
				dummy_algIterations.Add (result.splineChainList);
				//dummy_CurvePoints = result.curvePoints; // newCurvePoints
			}
			this.algIterations = dummy_algIterations;
			//this.curvePoints = dummy_CurvePoints;
			//lineManager.UpdateData (algIterations[0], curvePoints);
		}
	}

	public struct DeCastelProduct {
		public List<Vector3[]> splineChainList;
		public List<Vector3> curvePoints;

		public DeCastelProduct(List<Vector3[]>splineChainList, List<Vector3> curvePoints) {
			this.splineChainList = splineChainList;
			this.curvePoints = curvePoints;
		}
	}

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

			//if (rational) midPoints = Rationalize(midPoints);

			//Randam lygio midpointus
			for (int lvl = 0; lvl < degree; lvl++) { // [0;3] nusileidziam iki curvePoint
				List<Vector3> newMidPointList = new List<Vector3> (); // Isvalom

				for (int p = 0; p < midPoints.Count - 1; p++) { // Nuo 0 iki priespaskutinio imtinai
					//if(rational) midPoints = Rationalize(midPoints);

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
						Vector3 curvePoint;
						if (rational) { curvePoint = FindMidPoint(midPoints[p], midPoints[p + 1]); } //* weight;
						else { curvePoint = FindMidPoint (midPoints[p], midPoints[p + 1]); }
						//curvePoints.Insert (curveIndex, curvePoint);
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

/*


	private Vector3[] Project(Vector3[] points) {
		List<Vector3> newPoints = new List<Vector3>();
		foreach (Vector3 p in points) {
			Vector3 newP = p / weight;
			newPoints.Add(newP);
		}
		return newPoints.ToArray();
	}

	private Vector3[] Rationalize(Vector3[] points) {
		List<Vector3> newPoints = new List<Vector3>();
		for(int i = 1; i < points.Length-1; i++) {
			Vector3 newP = points[i] * weight;
			newPoints.Add(newP);
		}
		newPoints.Insert(0, points[0]);
		newPoints.Add(points.Last());
		return newPoints.ToArray();
	}

	private List<Vector3> Rationalize(List<Vector3> points) {
		List<Vector3> newPoints = new List<Vector3>();
		for(int i = 1; i < points.Count-1; i++) {
			Vector3 newP = points[i] * weight;
			newPoints.Add(newP);
		}
		newPoints.Insert(0, points[0]);
		newPoints.Add(points.Last());
		return newPoints;
	}

	private List<Vector3> Project(List<Vector3> points) {
		List<Vector3> newPoints = new List<Vector3>();
		foreach (Vector3 p in points) {
			Vector3 newP = p / weight;
			newPoints.Add(newP);
		}
		return newPoints;
	}

	private Vector3 Rationalize(Vector3 point) {
		return point / weight;
	}










public GameObject pointPrefab; //The prefab
	public float lineDivideScale;
	public List<List<List<GameObject>> > algIterations; // Iteration of DeCasteljau -> Iteration of CurveLevel-1 (last is Spline) -> PointList
	public List<GameObject> curvePoint_List;
	//public bool openSpline;
	private int degree;
	private LineManager lineManager;
	private int iterationCounter;

	// Use this for initialization
	void Start () {
		iterationCounter = 0;
		algIterations = new List<List<List<GameObject>> > ();
		List<List<GameObject>> list_Lvl = new List<List<GameObject>> ();
		algIterations.Add (list_Lvl);
		algIterations[0].Add (Create_FirstPointList ());

		lineManager = GetComponent<LineManager> ();
		Debug.Log (lineManager);

		lineManager.AddPolygon (algIterations[0]);

		curvePoint_List = new List<GameObject> ();
		curvePoint_List.Add (algIterations[0][0][0]); //begining base point
		curvePoint_List.Add (algIterations[0][0][algIterations[0][0].Count - 1]); //ending base point
		if (lineDivideScale == 0.0f) { lineDivideScale = 0.5f; }

		DeCasteljau (true);
		DeCasteljau (true);
		//DeCasteljau ();
		//DeCasteljau ();
	}

	// Update is called once per frame
	void FixedUpdate () {ina toliau - kitas 

		//Recalculate_Points_onMovement ();
		lineManager.UpdateAllPoints (algIterations, curvePoint_List);

	}
	private void Recalculate_Points_onMovement () {
		List<List<GameObject>> firstIter = new List<List<GameObject>> (algIterations[0]);
		algIterations = new List<List<List<GameObject>> > ();
		algIterations.Add (firstIter);

		for (int i = 0; i <= iterationCounter; i++) {
			DeCasteljau (false);
		}
	}

	private List<GameObject> Create_FirstPointList () {
		GameObject[] initPoints = GameObject.FindGameObjectsWithTag ("Point").OrderBy (go => go.name).ToArray ();
		List<GameObject> pointList = new List<GameObject> ();
		degree = initPoints.Length - 1;
		foreach (var point in initPoints) {
			pointList.Add (point);
		}
		return pointList;
	}

	private void DeCasteljau (bool nextIteration) {
		List<List<GameObject>> iterLvlList = new List<List<GameObject>> ();
		List<GameObject> newMidPointList = new List<GameObject> ();
		List<GameObject> midPointList = new List<GameObject> ();
		List<GameObject> begList = new List<GameObject> ();
		List<GameObject> endList = new List<GameObject> ();

		// Sitas for reikalingas jei kreives laipsnis didesnis nei 2
		for (int lvl = 0; lvl < degree - 1; lvl++) { // Keliaujam per MidPoint search lygius (lygis = kreives_laipsnis - 1) - Kiek kartu reikes ieskoti midpointu, kol gausim tiese, kurioje ieskosime curvepoint
			// Surandami visi einamosios iteracijos MidPoint'ai
			if (lvl == 0) { // Get PointList from last Alg Iteration of last iteration
				int last_IterIndex = algIterations.Count - 1;
				int last_PointListIndex = algIterations[last_IterIndex].Count - 1;
				midPointList = algIterations[last_IterIndex][last_PointListIndex]; // Paimamas praeito to poligono paskutines iteracijos su beg end ir curve point listas
			}
			newMidPointList = new List<GameObject> (); // Isvalom
			//Randam lygio midpointus
			for (int j = 0; j < midPointList.Count - 1; j++) { // Nuo 0 iki priespaskutinio imtinai
				GameObject midPoint = FindMidPoint (midPointList[j], midPointList[j + 1]);
				newMidPointList.Add (midPoint);
				// Pirmas ir paskutinis, nededam pirmo lvl => Beg ir End tasku
				if (degree == 2 || j == 0) { begList.Add (midPoint); } // !!! Skirtumas tarp 2 laipsnio ir visu kitu (2n - gaubiantysis yra kiekvienas midpointas)
				else if (j == midPointList.Count - 2) { endList.Add (midPoint); } // end listas (be End pagrindo)
			}
			midPointList = new List<GameObject> (newMidPointList); // Paimamas praeitos iteracijos listas
			iterLvlList.Add (midPointList);
		}
		//Surandami einamosios iteracijos CurvePoint'ai esantys tarp einamosios iteracijos MidPointu poru
		int curveIndex = 1;
		for (int i = 0; i < midPointList.Count - 1; i++) { // Nuo 0 iki priespaskutinio imtinai
			if (i != 0) { i++; } // Kad butu ieskomi CP tarp MP poru neiskaitant svetimu partenriu
			GameObject curvePoint = FindMidPoint (midPointList[i], midPointList[i + 1]);
			// Papildomas CurveList nauju tasku
			curvePoint_List.Insert (curveIndex, curvePoint);
			curveIndex += 2;
		}
		// Make iteration Spline - add CP and Beg and End points, save as iterLvlList[last]
		// Spline Sukurimas - ruosinys kitam DeCasteljau();
		endList.Reverse ();
		List<GameObject> spline = begList.Concat (endList).ToList ();
		int staticCounter = spline.Count;
		int dynamicIndex = 0;
		curveIndex = 1; // Neimama Beg ir End
		for (int i = 0; i < staticCounter; i++) {
			if (i != 0 & i % (degree - 1) == 0) {
				if (curveIndex == curvePoint_List.Count - 1) { Debug.Log ("!!! ALERT !!! Overheating in Curve Point List !!!"); }
				spline.Insert (i + dynamicIndex, curvePoint_List[curveIndex]);
				dynamicIndex++;
				curveIndex++;
			}
		}
		spline.Insert (0, curvePoint_List[0]); // Add BeginPoint
		spline.Add (curvePoint_List[curvePoint_List.Count - 1]); // Add EndPoint

		iterLvlList.Add (spline);
		algIterations.Add (iterLvlList);

		if (nextIteration == true) {
			iterationCounter++;
			//Update info to LineManager
			lineManager.AddPolygon (iterLvlList);
			lineManager.ReAddCurve (curvePoint_List);
		}
	}

	private GameObject FindMidPoint (GameObject point, GameObject nextPoint) {
		var pointX = point.transform.position.x;
		var pointY = point.transform.position.y;
		var nextX = nextPoint.transform.position.x;
		var nextY = nextPoint.transform.position.y;
		var midPointX = (pointX + nextX) * lineDivideScale;
		var midPointY = (pointY + nextY) * lineDivideScale;
		GameObject midPoint = CreatePoint (midPointX, midPointY);
		return midPoint;
	}

	private GameObject CreatePoint (float x, float y) {
		GameObject point = Instantiate (pointPrefab, new Vector3 (x, y, 0), Quaternion.identity);
		point.transform.parent = this.transform;
		return point;
	}


	private void ResetCoordinates (float cX, float cY, GameObject point) {
		point.transform.position.x = cX;
		point.transform.position.y = cY;
	}


}
 */