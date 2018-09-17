using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AproximationSpline : MonoBehaviour {
 
	public int degree = 2;
	public bool isOpenSpline = false;
	public bool rational = false;
	public bool isC2 = false;
	public GameObject pointPrefab;
	public GameObject bezierManagerPrefab;
	public List<Vector3> controlVexList;

	List<GameObject> controlPoints;
	public List<Bezier> bezierManagers;
	private LineManager lineManager;
	private bool initialized;
	private List<float> deltasList;
	

	// Use this for initialization
	void Awake () {
		initialized = false;
	}
	void Start () {	
		Initializer();
		lineManager = GetComponent<LineManager> ();
		lineManager.UpdateData_forSpline (controlVexList);
	}

	// Update is called once per frame
	///*
	void FixedUpdate () {	
		if (initialized) {
			controlVexList = FromGO_toVectorList(controlPoints);

			if (degree == 2) InitSpline_2d();
			else if (isC2 == true) InitSpline_3dC2();
			else InitSpline_3d();

			List<Vector3> controlVexList_forRendering = new List<Vector3>(controlVexList);
			if (!isOpenSpline) controlVexList_forRendering.Add(controlVexList[0]);
			lineManager.UpdateData_forSpline (controlVexList_forRendering);
		}
		
	}
	 //*/


	private void CreateCP(Vector3 newCP) {
		GameObject newPoint = Instantiate (pointPrefab, newCP, Quaternion.identity);
		newPoint.transform.parent = gameObject.transform;
	}

	private void SubDivision2D_PointCreation(Vector3 beg, Vector3 end, float ratio) {
		Vector3 newCP = FindPoint_inBetween(beg, end, ratio);
		CreateCP(newCP);
	}

	private void SubDivision3D_PointCreation(Vector3 past, Vector3 present, Vector3 future) {
		Vector3 newCP = 0.125f*past + 0.75f*present + 0.125f*future;
		CreateCP(newCP);
	}

	public void Subdivide() {
		initialized = false;
		
		List<GameObject> children = new List<GameObject>();
		foreach (Transform child in transform) {
			children.Add(child.gameObject);
		}
		foreach (GameObject child in children) { // naikintuvas 
			GameObject.DestroyImmediate(child); 
		}
		
		if(degree == 2) {
			for (int i = 0; i < controlVexList.Count-1; i++) {
					SubDivision2D_PointCreation(controlVexList[i], controlVexList[i+1], 0.25f);
					SubDivision2D_PointCreation(controlVexList[i], controlVexList[i+1], 0.75f);
			}
			if(!isOpenSpline) {
				SubDivision2D_PointCreation(controlVexList.Last(), controlVexList[0], 0.25f);
				SubDivision2D_PointCreation(controlVexList.Last(), controlVexList[0], 0.75f);
			}
			

		} else if(degree == 3) {
			// Begining
			if(isOpenSpline) { CreateCP(controlVexList[0]); } 
			else { SubDivision3D_PointCreation(controlVexList.Last(), controlVexList[0], controlVexList[1]); }
			SubDivision2D_PointCreation(controlVexList[0], controlVexList[1], 0.5f);

			// Mid
			for (int i = 1; i < controlVexList.Count-1; i++) {
					SubDivision3D_PointCreation(controlVexList[i-1], controlVexList[i], controlVexList[i+1]);
					SubDivision2D_PointCreation(controlVexList[i], controlVexList[i+1], 0.5f);
			}
			// Ending
			if(isOpenSpline){ CreateCP(controlVexList.Last()); } 
			else { 
				SubDivision3D_PointCreation(controlVexList[controlVexList.Count-2], controlVexList.Last(), controlVexList[0]);
				SubDivision2D_PointCreation(controlVexList.Last(), controlVexList[0], 0.5f);
			}
		}
		lineManager.Init();
		Initializer (); // restart all the mechanism
	}


	private void Initializer () {
		// Issivalom
		controlPoints = new List<GameObject>();
		controlVexList = new List<Vector3>();
		bezierManagers = new List<Bezier>();

		foreach (Transform child in gameObject.transform) {
			if ( child.tag == "Point"){
				controlPoints.Add (child.gameObject);
				controlVexList.Add(child.position);
			}
		}
		if (degree == 2) InitSpline_2d();
		else if (isC2 == true) InitSpline_3dC2();
		else InitSpline_3d();
		
		initialized = true;
	}

	private void InitSpline_2d() {
		// HatPoints = ControlPoints
		// Find JointPoints:
		List<Vector3> jointPoints = new List<Vector3>();
		for (int i = 0; i < controlVexList.Count-1; i++){
			jointPoints.Add(FindMidPoint(controlVexList[i], controlVexList[i+1]));
		}
		if(isOpenSpline) { 	// Find Phantom Points
			jointPoints.Insert(0, GetPoint_ofLineExtentionEnd (jointPoints[0], controlVexList[0]));
			jointPoints.Add(GetPoint_ofLineExtentionEnd (jointPoints.Last(), controlVexList.Last()));
		} else{ jointPoints.Add(FindMidPoint(controlVexList.Last(), controlVexList[0])); } // Join spline into circle

		for (int i = 0; i < controlVexList.Count; i++){
			Vector3[] bezSpline;
			if (!isOpenSpline){ // ClosedSpline
				if (i == 0){
					bezSpline = new Vector3[] { jointPoints.Last(), controlVexList[0], jointPoints[0] }; // Begining
				} else if (i == controlVexList.Count-1) {
					bezSpline = new Vector3[] { jointPoints[i-1], controlVexList[i], jointPoints.Last() }; // Ending
				} else {
					bezSpline = new Vector3[] { jointPoints[i-1], controlVexList[i], jointPoints[i] }; // Middle
				}
			} else {// OpenSpline
					bezSpline = new Vector3[] { jointPoints[i], controlVexList[i], jointPoints[i+1] }; // Begining, Middle, Ending
			}
			if (!initialized){
				CreateBezierManager(bezSpline);
			} else {
				bezierManagers[i].Set_initVexters(bezSpline);
			}
		}
	}

	private void InitSpline_3d() {
		// HatPoints = ControlPoints
		List<Vector3[]> splainChain = new List<Vector3[]>();
		for (int i = 0; i < controlVexList.Count-1; i++){
			Vector3 hat1 = FindPoint_inBetween(controlVexList[i], controlVexList[i+1], 1.0f/3.0f);
			Vector3 hat2 = FindPoint_inBetween(controlVexList[i], controlVexList[i+1], 2.0f/3.0f);
			Vector3[] bezSpline = { hat1, hat1, hat2, hat2 }; // Pirma ir paskutine bus pakeistos
			splainChain.Add(bezSpline);
		}

		if (!isOpenSpline){ // Closed Spline
			Vector3 hat1 = FindPoint_inBetween(controlVexList.Last(), controlVexList[0], 1.0f/3.0f);
			Vector3 hat2 = FindPoint_inBetween(controlVexList.Last(), controlVexList[0], 2.0f/3.0f);
			Vector3[] bezSpline = { hat1, hat1, hat2, hat2 }; // Pirma ir paskutine bus pakeistos
			splainChain.Add(bezSpline);
		}

		// Find JointPoints:
		for (int i = 0; i < splainChain.Count; i++) {
			if (!isOpenSpline) { 		// ClosedSpline
				if (i == 0) {
					splainChain[0][0] = FindMidPoint(splainChain.Last()[2], splainChain[0][1]); // Begining
					splainChain[0][3] = FindMidPoint(splainChain[0][2], splainChain[1][1]);

				} else if (i == splainChain.Count-1) {
					splainChain.Last()[0] = FindMidPoint(splainChain[i-1][2], splainChain.Last()[1]); // Ending
					splainChain.Last()[3] = splainChain[0][0];
				} else {
					splainChain[i][0] = FindMidPoint(splainChain[i-1][2], splainChain[i][1]); // Midle
					splainChain[i][3] = FindMidPoint(splainChain[i][2], splainChain[i+1][1]);
				}
			} else if (isOpenSpline) {	// OpenSpline
				if (i == 0) {
					splainChain[0][0] = 2*controlVexList[0] - controlVexList[1]; // Begining
				} else {
					splainChain[i][0] = FindMidPoint(splainChain[i-1][2], splainChain[i][1]); // Midle
				}
				if (i == splainChain.Count-1) {
					splainChain[i][3] = 2 *controlVexList.Last() - controlVexList[i]; // Ending
				} else {
					splainChain[i][3] = FindMidPoint(splainChain[i][2], splainChain[i+1][1]);
				}
			}

			if (!initialized){
				CreateBezierManager(splainChain[i]);
			} else {
				bezierManagers[i].Set_initVexters(splainChain[i]); //updeitinam
			}
		} 
	}

	private void InitSpline_3dC2() {
		CalcDeltas();
		DataFill_forC2();
		//HatPoints = ControlPoints
		//print (controlVexList.Count+" "+ deltasList.Count);
		List<Vector3[]> splainChain = new List<Vector3[]>();
		for (int i = 0; i < controlVexList.Count-1; i++) {
			float prvDlt, dlt, futDlt;
			dlt = deltasList[i];

			if (i == 0) prvDlt = deltasList.Last();
			else prvDlt = deltasList[i-1];
			if (i == deltasList.Count-1) futDlt = deltasList[0];
			else futDlt  = deltasList[i+1];

			float sumDlt = prvDlt + dlt + futDlt;
			
			Vector3 hat1 = ((dlt + futDlt) / sumDlt) * controlVexList[i]  
							+ (prvDlt / sumDlt) * controlVexList[i+1];

			Vector3 hat2 = (futDlt / sumDlt) * controlVexList[i]  
							+ ((prvDlt + dlt) / sumDlt) * controlVexList[i+1];

			Vector3[] bezSpline = { hat1, hat1, hat2, hat2 }; // Pirma ir paskutine bus pakeistos
			splainChain.Add(bezSpline);
		}
		if (!isOpenSpline) { // Closed Spline
			float sumDlt =  deltasList[deltasList.Count-2] + deltasList.Last() + deltasList[0];

			Vector3 hat1 = ((deltasList.Last() + deltasList[0]) / sumDlt) * controlVexList.Last()
							+ (deltasList[deltasList.Count-2] / sumDlt) * controlVexList[0];

			Vector3 hat2 = (deltasList[0] / sumDlt) * controlVexList.Last()  
							+  ((deltasList[deltasList.Count-2] + deltasList.Last()) / sumDlt) * controlVexList[0];

			Vector3[] bezSpline = { hat1, hat1, hat2, hat2 }; // Pirma ir paskutine bus pakeistos
			splainChain.Add(bezSpline);
		}

		// Find JointPoints:
		for (int i = 0; i < splainChain.Count; i++) {
			float prvDlt, dlt, futDlt;
			Vector3[] prvSpline, futSpline;

			//print(i +" "+ deltasList.Count);
			
			dlt = deltasList[i];
			if (i == 0) {
				prvDlt = deltasList.Last();
				prvSpline = splainChain.Last();
			} else { 
				prvDlt = deltasList[i-1];
				prvSpline = splainChain[i-1];
			}
			if (i == splainChain.Count-1) {
				futDlt = deltasList[0];
				futSpline = splainChain[0];
			} else { 
				futDlt  = deltasList[i+1];
				futSpline = splainChain[i+1];
			}

			if (isOpenSpline && i==0) splainChain[i][0] = controlVexList[0];
			else splainChain[i][0] = (dlt / (prvDlt+dlt)) * prvSpline[2] 
									+ (prvDlt / (prvDlt+dlt)) * splainChain[i][1];
					
			if (isOpenSpline && i==splainChain.Count-1) splainChain[i][3] = controlVexList.Last();
			else splainChain[i][3] = (futDlt / (dlt+futDlt)) * splainChain[i][2] 
									+ (dlt / (dlt+futDlt)) * futSpline[1];

			
			if (!initialized) CreateBezierManager(splainChain[i]);
			else bezierManagers[i].Set_initVexters(splainChain[i]); //updeitinam
		}
	} 



	private void CalcDeltas() {
		deltasList = new List<float>();
		for (int i = 0; i < controlVexList.Count-1; i++) {
			deltasList.Add(Vector3.Distance(controlVexList[i], controlVexList[i+1]));
			//if (i==0 || i==controlVexList.Count-2) deltasList.Add(0.0f);
		}
	//	if (isOpenSpline) {
	//		deltasList.Insert(0, deltasList[0]);
	//		deltasList.Add(deltasList.Last());
	//	}
		if (!isOpenSpline) deltasList.Add(Vector3.Distance(controlVexList.Last(),controlVexList[0]));
	}

	private void DataFill_forC2() {
		if (!isOpenSpline) {
			// ?? d[n] = d[0] ??
			//controlVexList.Insert(0, controlVexList.Last()); // d[-1]=d[n]
			//controlVexList.Add(controlVexList[0]); // d[n+1] = d[0]
			//controlVexList.Add(controlVexList[1]); // d[n+2] = d[1]

			//controlVexList.Add(controlVexList.Last()); // d[n+1] = d[0]


			//deltasList.Insert(0, deltasList.Last()); // delt[-1]=delt[n]
			//deltasList.Insert(0, deltasList[deltasList.Count-2]);  // delt[-2] = delt[n-1]
			//deltasList.Add(deltasList[0]); // delt[n+1] = delt[0]
			//deltasList.Add(deltasList[1]); // delt[n+2] = delt[1]

		} else {
			controlVexList.Insert(0, 2f*controlVexList[0] - controlVexList[1]); // d[-1]=2*d[0]-d[1]
			controlVexList.Add(2f*controlVexList.Last() - controlVexList[controlVexList.Count-2]); // d[n+1]=2*d[n]-d[n-1]
			//deltasList [d0...dn-1]
			//deltasList.Insert(0, deltasList[0]); // delt[-1]=delt[0]
			//deltasList.Insert(0, deltasList[2]);  // delt[-2] = delt[1]
			//deltasList.Add(deltasList[deltasList.Count-1]); // delt[n] = delt[n-1]
			//deltasList.Add(deltasList[deltasList.Count-3]); // delt[n+1] = delt[n-2]
			deltasList.Insert(0, 0f);
			deltasList.Add(0);
		}
	}

	private void CreateBezierManager(Vector3[] bezSpline) {
		GameObject bezierManager = Instantiate (bezierManagerPrefab, new Vector3(0,0,0), Quaternion.identity);
		bezierManager.transform.parent = gameObject.transform;
		Bezier bezierScript = bezierManager.GetComponent<Bezier>();
		//bezSpline[1] = bezSpline[1]*weight;
		if (rational) { bezierScript.Init_fromSpline(bezSpline, true); }
		else { bezierScript.Init_fromSpline(bezSpline); }
		bezierManagers.Add(bezierScript);
	}


	private Vector3 FindMidPoint (Vector3 point, Vector3 nextPoint) {
		float midPointX = (point.x + nextPoint.x) * 0.5f;
		float midPointY = (point.y + nextPoint.y) * 0.5f;
		float midPointZ = (point.z + nextPoint.z) * 0.5f;
		return new Vector3(midPointX, midPointY, midPointZ);
	}

	private List<Vector3> FromGO_toVectorList(List<GameObject> GoList) {
		List<Vector3> vecList = new List<Vector3>();
		foreach (GameObject GO in GoList) {
			vecList.Add(GO.transform.position);
		}
		return vecList;
	}

	private Vector3 GetPoint_ofLineExtentionEnd(Vector3 begin, Vector3 mid) {
		Vector3 extentionPoint = new Vector3(0,0,0);
		extentionPoint = mid + (mid - begin);
		return extentionPoint;
	}

	private Vector3 FindPoint_inBetween(Vector3 begin, Vector3 end, float ratio) {
		Vector3 pointInBetween = new Vector3(0,0,0);
		pointInBetween = begin + ratio * (end - begin);
		return pointInBetween;
	}


}

/*
	private void InitSpline_3dC2() {
		CalcDeltas();
		DataFill_forC2();
		//HatPoints = ControlPoints
		print (controlVexList.Count+" "+ deltasList.Count);
		List<Vector3[]> splainChain = new List<Vector3[]>();
		for (int i = 0; i < controlVexList.Count-1; i++) {
			float prvDlt, dlt, futDlt;
			dlt = deltasList[i];

			if (i == 0) prvDlt = deltasList.Last();
			else prvDlt = deltasList[i-1];
			if (i == deltasList.Count-1) futDlt = deltasList[0];
			else futDlt  = deltasList[i+1];

			float sumDlt = prvDlt + dlt + futDlt;
			
			Vector3 hat1 = ((dlt + futDlt) / sumDlt) * controlVexList[i]  
							+ (prvDlt / sumDlt) * controlVexList[i+1];
			Vector3 hat2 = (futDlt / sumDlt) * controlVexList[i]  
							+ ((prvDlt + dlt) / sumDlt) * controlVexList[i+1];

			//!!!! SU OPEN SPLINE PAPILDOMAS VEX GALUOSE! ???
			Vector3[] bezSpline = { hat1, hat1, hat2, hat2 }; // Pirma ir paskutine bus pakeistos
			splainChain.Add(bezSpline);
		}
		if (!isOpenSpline) { // Closed Spline
			float sumDlt =  deltasList[deltasList.Count-2] + deltasList.Last() + deltasList[0];
			Vector3 hat1 = ((deltasList.Last() + deltasList[0]) / sumDlt) * controlVexList.Last()
							+ (deltasList[deltasList.Count-2] / sumDlt) * controlVexList[0];
			Vector3 hat2 = (deltasList[0] / sumDlt) * controlVexList.Last()  
							+  ((deltasList[deltasList.Count-2] + deltasList.Last()) / sumDlt) * controlVexList[0];
			Vector3[] bezSpline = { hat1, hat1, hat2, hat2 }; // Pirma ir paskutine bus pakeistos
			splainChain.Add(bezSpline);
		}

		// Find JointPoints:
		for (int i = 0; i < splainChain.Count; i++) {
			float prvDlt, dlt, futDlt;
			Vector3[] prvSpline, futSpline;
			
			dlt = deltasList[i];
			if (i == 0) {
				prvDlt = deltasList.Last();
				prvSpline = splainChain.Last();
			} else { 
				prvDlt = deltasList[i-1];
				prvSpline = splainChain[i-1];
			}
			if (i == splainChain.Count-1) {
				futDlt = deltasList[0];
				futSpline = splainChain[0];
			} else { 
				futDlt  = deltasList[i+1];
				futSpline = splainChain[i+1];
			}

			if (isOpenSpline && i==0) splainChain[i][0] = controlVexList[0];
			else splainChain[i][0] = (dlt / (prvDlt+dlt)) * prvSpline[2] 
					+ (prvDlt / (prvDlt+dlt)) * splainChain[i][1];
					
			if (isOpenSpline && i==splainChain.Count-1) splainChain[i][3] = controlVexList.Last();
			else splainChain[i][3] = (futDlt / (dlt+futDlt)) * splainChain[i][2] 
					+ (dlt / (dlt+futDlt)) * futSpline[1];
			
			
			if (!initialized) CreateBezierManager(splainChain[i]);
			else bezierManagers[i].Set_initVexters(splainChain[i]); //updeitinam
		}
	} 
 */