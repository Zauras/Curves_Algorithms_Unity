using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class InterpolationSpline : MonoBehaviour {
 
	public int degree = 3;
	public bool isOpenSpline = false;
	public bool withAcceleration = false;
	public bool isC2 =  false;
	public GameObject pointPrefab;
	public GameObject bezierManagerPrefab;

	public List<GameObject> controlPoints;
	public List<Vector3> controlVexList;

	public List<Bezier> bezierManagers;
	private List<Vector3[]> bezierFragments;
	private LineManager lineManager;

	private bool initialized;


	// Use this for initialization
	void Awake () {
		initialized = false;
	}
	void Start () {
		Initializer();
		lineManager = GetComponent<LineManager> ();
		lineManager.UpdateData_forSpline (controlVexList);
	}

	
	void FixedUpdate () {
		controlVexList = FromGO_toVectorList(controlPoints);
		if (degree == 2){
			InitSpline_2d();
		} else {
			InitSpline_3d(); 
		}
		List<Vector3> controlVexList_forRendering = new List<Vector3>(controlVexList);
		if (!isOpenSpline) controlVexList_forRendering.Add(controlVexList[0]);
		lineManager.UpdateData_forSpline (controlVexList_forRendering);
	}
	
	
	private void CreateCP(Vector3 newCP) {
		GameObject newPoint = Instantiate (pointPrefab, newCP, Quaternion.identity);
		newPoint.transform.parent = gameObject.transform;
	}

	private void Subdivision_InsertNewCP(Vector3 past, Vector3 present, Vector3 future, Vector3 futurePlus) {
		Vector3 newCP = -0.0625f*past + 0.5625f*present + 0.5625f*future - 0.0625f*futurePlus;
		CreateCP(newCP);
	}

	public void Subdivide() {
		initialized = false;
		// Issivalom spline
		List<GameObject> children = new List<GameObject>();
		foreach (Transform child in transform) {
			children.Add(child.gameObject);
		}
		foreach (GameObject child in children) { // naikintuvas 
				GameObject.DestroyImmediate(child); 
		}
		
		int iterCount = controlVexList.Count-1;
		if (!isOpenSpline) iterCount += 1;

		for (int i = 0; i < iterCount; i++) {
			Vector3 prevCVex;
			Vector3 nextCVex;
			Vector3 nextNextCVex;
			if (i == 0) { 
				prevCVex = controlVexList.Last();
				if(isOpenSpline) {
					//prevCVex = GetPoint_ofLineExtentionEnd(FindPoint_inBetween (controlVexList[0], controlVexList[1], 4.0f/5.0f), controlVexList[0]);
					prevCVex=new Vector3(0,0,0);
				}
			} else { prevCVex = controlVexList[i-1]; }

			// next && nextNext
			if (!isOpenSpline && i+2 == iterCount) { //
				nextCVex 	 = controlVexList.Last();
				nextNextCVex = controlVexList[0];

			} else if (i+1 == iterCount) {
				if (isOpenSpline) {
					nextCVex 	 = controlVexList.Last();
					nextNextCVex = nextNextCVex = GetPoint_ofLineExtentionEnd(FindPoint_inBetween (controlVexList[iterCount], controlVexList.Last(), 2.0f/3.0f), controlVexList[0]);
				}
				else {
					nextCVex 	 = controlVexList[0];
					nextNextCVex = controlVexList[1];
				}
			} else { 
				nextCVex = controlVexList[i+1];
				nextNextCVex = controlVexList[i+2];
			}


			CreateCP(controlVexList[i]);
			Subdivision_InsertNewCP(prevCVex, controlVexList[i], nextCVex, nextNextCVex);
			if (isOpenSpline && i+1 == iterCount) CreateCP(controlVexList.Last());

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

		if (degree == 2){
			InitSpline_2d();
		} else {
			InitSpline_3d();
		}
		initialized = true;

	}

	private void InitSpline_2d() {
		// JointPoints = ControlPoints
		// Find HatPoints:
		int iterCount = controlVexList.Count-1;
		if (!isOpenSpline) iterCount += 1;

		for (int i = 0; i < iterCount; i++) {
			Vector3 prevCVex;
			Vector3 nextCVex;
			Vector3 nextNextCVex;
			Vector3 cVex = controlVexList[i];

			if (i == 0) { prevCVex = controlVexList.Last();
			} else {	  prevCVex = controlVexList[i-1]; }

			if (i+2 == iterCount) {
				nextCVex 	 = controlVexList[i+1];
				nextNextCVex = controlVexList.Last();
				if (!isOpenSpline) {
					nextCVex 	 = controlVexList.Last();
					nextNextCVex = controlVexList[0];
				}
			} else if (i+1 == iterCount) {
				nextCVex 	 = controlVexList.Last();
				nextNextCVex = controlVexList[0];
				if (!isOpenSpline) {
					nextCVex 	 = controlVexList[0];
					nextNextCVex = controlVexList[1];
				}
			} else { 
				nextCVex = controlVexList[i+1];
				nextNextCVex = controlVexList[i+2];
			}

			Vector3 cV_det;
			Vector3 hat;
			if (withAcceleration) {
				cV_det = Derivative_withAcceleration (prevCVex, cVex, nextCVex);			
				hat    = cVex + (cV_det * (Pow(Vector3.Distance(cVex, nextCVex), 0.5f) / (float)degree));
			} else {
				cV_det = Derivative(prevCVex, nextCVex);
				hat = cVex + 0.5f*cV_det;
			}
			Vector3[] bezSpline = { controlVexList[i], hat, nextCVex };
			bezierFragments.Add(bezSpline);
		}
		if (!initialized) {
			InitBezierManagers();
		} else { ResetBezierVexters(); } 
	}

	private void InitSpline_3d() {
		bezierFragments = new List<Vector3[]>();
		// JointPoints = ControlPoints
		// Find HatPoints:
		int iterCount = controlVexList.Count-1;
		if (!isOpenSpline) iterCount += 1;

		for (int i = 0; i < iterCount; i++) {
			Vector3 prevCVex;
			Vector3 nextCVex;
			Vector3 nextNextCVex;

			if (i == 0) { prevCVex = controlVexList.Last();
			} else {	  prevCVex = controlVexList[i-1]; }

			if (i+2 == iterCount) {
				nextCVex 	 = controlVexList[i+1];
				nextNextCVex = controlVexList.Last();
				if (!isOpenSpline) {
					nextCVex 	 = controlVexList.Last();
					nextNextCVex = controlVexList[0];
				}
			} else if (i+1 == iterCount) {
				nextCVex 	 = controlVexList.Last();
				nextNextCVex = controlVexList[0];
				if (!isOpenSpline) {
					nextCVex 	 = controlVexList[0];
					nextNextCVex = controlVexList[1];
				}
			} else { 
				nextCVex = controlVexList[i+1];
				nextNextCVex = controlVexList[i+2];
			}
			
			Vector3[] bezSpline;
			// Su Pagreiciu ir skirtingais kelio atstumais ar be:
			if (withAcceleration) {
				Vector3 cV_det   = Derivative_withAcceleration (prevCVex, controlVexList[i], nextCVex);
				Vector3 cVnext_det = Derivative_withAcceleration (controlVexList[i], nextCVex, nextNextCVex);
					
				if (isOpenSpline && (i == 0 || i == iterCount-1)) { // Atviro splino galu uztaisymas:
					Vector3 hat;
					if (i == 0) { 
						hat = nextCVex		    - (cVnext_det  * ( Pow(Vector3.Distance(controlVexList[i], nextCVex), 0.5f) / 2.0f));
					} else { 	  
						hat = controlVexList[i] + (cV_det 	   * ( Pow(Vector3.Distance(controlVexList[i], nextCVex), 0.5f) / 2.0f));
					}
					bezSpline = new Vector3[] { controlVexList[i], hat, nextCVex };
				
				} else {			
					Vector3 hat1 = controlVexList[i] + (cV_det 	   * ( Pow(Vector3.Distance(controlVexList[i], nextCVex), 0.5f) / 3.0f));
					Vector3 hat2 = nextCVex			 - (cVnext_det * ( Pow(Vector3.Distance(controlVexList[i], nextCVex), 0.5f) / 3.0f));
					bezSpline = new Vector3[] { controlVexList[i], hat1, hat2, nextCVex };
				}

			} else { // hat = cV_det + 1/degreef * (1/2f * (cVprev - cVnext))
				Vector3 cV_det = Derivative (prevCVex, nextCVex);
				Vector3 cVnext_det = Derivative (controlVexList[i], nextNextCVex);

				if (isOpenSpline && (i == 0 || i == iterCount-1)) { // Atviro splino galu uztaisymas:
					Vector3 hat;
					if (i == 0) { 
						hat = nextCVex			- (cVnext_det * 0.5f);
					} else { 	  
						hat = controlVexList[i] + (cV_det     * 0.5f);
					}
					bezSpline = new Vector3[] { controlVexList[i], hat, nextCVex };
				} else {
					Vector3 hat1 = controlVexList[i] + (cV_det 	   * (1.0f / 3.0f));
					Vector3 hat2 = nextCVex 		 - (cVnext_det * (1.0f / 3.0f));
					bezSpline = new Vector3[] { controlVexList[i], hat1, hat2, nextCVex };
				}
			}
			bezierFragments.Add(bezSpline);
		}
		if (isC2) MakeC2();
		if (!initialized) {
			InitBezierManagers();
		} else { ResetBezierVexters(); } 
	}


	private void MakeC2() {
		// Gaunam b_new[2]
		List<Vector3> midHats = new List<Vector3>();

		for (int i = 0; i < bezierFragments.Count; i++) {
			if (isOpenSpline && (i == 0 || i == bezierFragments.Count-1)) {			
				Vector3 hat1 = 1.0f/3.0f*bezierFragments[i][0] + 2.0f/3.0f*bezierFragments[i][1];
				Vector3 hat2 = 1.0f/3.0f*bezierFragments[i][1] + 2.0f/3.0f*bezierFragments[i][2];
				bezierFragments[i] = new Vector3[] { bezierFragments[i][0],  hat1, hat2, bezierFragments[i][2] };
			}
			midHats.Add( 0.5f*(bezierFragments[i][1] + bezierFragments[i][2]) );
		}

		int iterCount = bezierFragments.Count;
		for (int i = 0; i < iterCount; i++) {
			if (!isOpenSpline || !(i == 0 || i == bezierFragments.Count-1)) {
				Vector3 prevHat, nextHat, nextCVex;
				Vector3 cVex = controlVexList[i];

				if (i == 0) { prevHat = midHats.Last();
				} else {	  prevHat = midHats[i-1]; }
				if (i+1 == iterCount) { nextHat = midHats[0];
				} else { 				nextHat = midHats[i+1]; }

				if (i+1 == iterCount) { nextCVex = controlVexList[0];
				} else { 				nextCVex = controlVexList[i+1]; }

				Vector3 hat1 = cVex     + 0.25f * (midHats[i]-prevHat);
				Vector3 hat2 = nextCVex + 0.25f * (midHats[i]-nextHat);
				Vector3[] newBezier = new Vector3[] { bezierFragments[i][0],  hat1 , midHats[i] , hat2, bezierFragments[i][3] };

				bezierFragments[i] = newBezier;
			}
		}
	}

	private Vector3[] GetCircledIndexes(List<Vector3> vList, int iterCount, int i) {
		Vector3 prevCVex, nextCVex, nextNextCVex;
		Vector3 cVex = vList[i];

		if (i == 0) { prevCVex = vList.Last();
		} else {	  prevCVex = vList[i-1]; }

		if (i+2 == iterCount) {
			nextCVex 	 = vList[i+1];
			nextNextCVex = vList.Last();
			if (!isOpenSpline) {
				nextCVex 	 = vList.Last();
				nextNextCVex = vList[0];
			}
		} else if (i+1 == iterCount) {
			nextCVex 	 = vList.Last();
			nextNextCVex = vList[0];
			if (!isOpenSpline) {
				nextCVex 	 = vList[0];
				nextNextCVex = vList[1];
			}
		} else { 
			nextCVex = vList[i+1];
			nextNextCVex = vList[i+2];
		}
		 return new Vector3[] {prevCVex, nextCVex, nextNextCVex};
	}

	private void ResetBezierVexters() {
		for (int i = 0; i < bezierManagers.Count; i++) {
			bezierManagers[i].Set_initVexters(bezierFragments[i]); 
		}
	}

	private Vector3 Derivative_withAcceleration (Vector3 prevPoint, Vector3 thisPoint, Vector3 nextPoint) {
		float deltaPrev = Pow(Vector3.Distance (prevPoint, thisPoint), 0.5f);
		float deltaThis = Pow(Vector3.Distance (thisPoint, nextPoint), 0.5f);
		
		Vector3 present = (deltaThis - deltaPrev) / (deltaPrev * deltaThis) * thisPoint;
		Vector3 future 	= deltaPrev / (deltaThis * (deltaPrev + deltaThis)) * nextPoint;
		Vector3 past 	= deltaThis / (deltaPrev * (deltaPrev + deltaThis)) * prevPoint;
		Vector3 accelerated_Derivate = present + future - past;

		return accelerated_Derivate;
	}

	private Vector3 Derivative (Vector3 prevPoint, Vector3 nextPoint) {
		Vector3 derivative = (nextPoint - prevPoint) / 2.0f;
		return derivative;
	}

	private float Pow(float value, float power) {
		float powValue = Mathf.Pow(Mathf.Abs(value), power);
		if (value < 0) powValue *= (-1.0f);
		return powValue;
	}


	private void InitBezierManagers() {
		foreach (Vector3[] bezierCP in bezierFragments) {
			GameObject bezierManager = Instantiate (bezierManagerPrefab, new Vector3(0,0,0), Quaternion.identity);
			bezierManager.transform.parent = this.transform;
			Bezier bezierScript = bezierManager.GetComponent<Bezier>();
			bezierScript.Init_fromSpline(bezierCP);
			bezierManagers.Add(bezierScript);
		}
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
