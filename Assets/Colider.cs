using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Colider : MonoBehaviour {

	public GameObject colPointPrefab;
	
	List<GameObject[]> collisionPointPairs;
	public bool globalCollision;
	public float minimalDistanceThreshold = 15.0f;
	private List<GameObject> splinesOfScene;
	private List<List<Bezier>> sceneSplinesBeziers;

	// Use this for initialization
	void Start () {
		globalCollision = false;
		collisionPointPairs = new List<GameObject[]>();
		// get all Splines
		StartCoroutine(WaitForLoad());
		sceneSplinesBeziers = new List<List<Bezier>>();
		splinesOfScene = GameObject.FindGameObjectsWithTag ("Spline").ToList();

		foreach (GameObject spline in splinesOfScene) {
			if (spline.transform.name == "AproximationSpline") {
				sceneSplinesBeziers.Add(spline.GetComponent<AproximationSpline>().bezierManagers);
			} else if (spline.transform.name == "InterpolationSpline") {
				sceneSplinesBeziers.Add(spline.GetComponent<InterpolationSpline>().bezierManagers);
			}	
		}
		StartCoroutine (CollisionUpdateChecker());
	}

	private IEnumerator WaitForLoad() {
        yield return new WaitForSeconds(5.0f);
    }

	private IEnumerator CollisionUpdateChecker() {
		while (true) {
			globalCollision = GlobalCollisionCheck();
			if (globalCollision) print("!!! Collision detected !!!");
			yield return new WaitForSeconds(0.7f);
		}
	}

	private bool GlobalCollisionCheck() {
		// Spline => //bezier => //iterationList //(array) splineChain=> //(Vector3) chainFragment 
		foreach (List<Bezier> splineBeziers in sceneSplinesBeziers) {
			foreach (Bezier bezierManager in splineBeziers) {
				int targetedIter = 0;
				foreach (List<Vector3[]> iteration in bezierManager.algIterations) {
				
					foreach (Vector3[] fragment in iteration) {
						BoundingCube checkerBox = new BoundingCube(fragment);
						// Now search all the targets

						foreach (List<Bezier> splineBeziersTarget in sceneSplinesBeziers) {
							if (!splineBeziers.Equals(splineBeziersTarget)) {

								foreach (Bezier bezierManagerTarget in splineBeziersTarget) {
									foreach (Vector3[] fragmentTarget in bezierManagerTarget.algIterations[targetedIter]) {
										BoundingCube targetBox = new BoundingCube(fragmentTarget);

										var collisionPoints = CheckCollision(checkerBox, targetBox);
										if (collisionPoints != null) {
											DeleteAllCollPoints();
											CreateCollsisionPoitns(collisionPoints);
											return true;
										} 
										//if (CheckCollision(checkerBox, targetBox)){
										//	return true;
										//}
									}
								}
							}
						}// End of Targeting			
					}
					targetedIter++;
				}
			}
		}
		print("...No Collision...");
		DeleteAllCollPoints();
		return false;
		// get list of splineChain of each Objcet
		// check if minimal boundries are coliding
	}

	private void DeleteOldPoints() {

	}

	private void DeleteAllCollPoints() {
		foreach (var pair in collisionPointPairs) {
			Destroy(pair[0]);
			Destroy(pair[1]);
		}
		collisionPointPairs = new List<GameObject[]>();
	}

	private void CreateCollsisionPoitns(Vector2[] colPoints) {
		GameObject[] pair = new GameObject[2];
		for (int i =0; i < colPoints.Length; i++) {
			GameObject point = Instantiate (colPointPrefab, colPoints[i], Quaternion.identity);
			point.transform.parent = this.transform;
			pair[i] = point;
		}
		collisionPointPairs.Add(pair);
	}

	private Vector2[] CheckCollision(BoundingCube A, BoundingCube B) {
		//print (Mathf.Abs(box2.xMax - box1.xMin) +" @ "+ Mathf.Abs(box2.xMin - box1.xMax) +" @ "+ Mathf.Abs(box2.yMax - box1.yMin) +" @ "+ Mathf.Abs(box2.yMin - box1.yMax));
		// Check if colliding

 
				if (Mathf.Abs(B.xMax - A.xMin) < minimalDistanceThreshold
					&& Mathf.Abs(B.yMin - A.yMax) < minimalDistanceThreshold) 
						return new Vector2[] { new Vector2( A.xMin, B.yMin ), new Vector2( B.xMax, A.yMax) };

				else if (Mathf.Abs(B.xMin - A.xMax) < minimalDistanceThreshold
						&& Mathf.Abs(B.yMin - A.yMax) < minimalDistanceThreshold) 
							return new Vector2[] { new Vector2( A.xMax, B.yMin ), new Vector2( B.xMin, A.yMax) };

				else if (Mathf.Abs(B.xMin - A.xMax) < minimalDistanceThreshold
						&& Mathf.Abs(B.yMax - A.yMin) < minimalDistanceThreshold) 
							return new Vector2[] { new Vector2( B.xMin, A.yMin ), new Vector2( A.xMax, B.yMax) };

				else if (Mathf.Abs(B.xMax - A.xMin) < minimalDistanceThreshold 
						&& Mathf.Abs(B.yMax - A.yMin) < minimalDistanceThreshold)
							return new Vector2[] { new Vector2( B.xMax, A.yMin ), new Vector2( A.xMin, B.yMax) };
			
			return null;
	}

	public struct BoundingCube { // Vector3 x 8 cubic box
		public float xMin, xMax, yMin, yMax, zMin, zMax;
		public float minimalThreshold;

		public BoundingCube(Vector3[] spline) {
			List<float> xList = new List<float>(); 
			List<float> yList = new List<float>(); 
			List<float> zList = new List<float>(); 
			foreach (Vector3 point in spline){
				xList.Add(point.x);
				yList.Add(point.y);
				zList.Add(point.z);
			}
			xMin = xList.Min(); xMax = xList.Max();
			yMin = yList.Min(); yMax = yList.Max();
			zMin = zList.Min(); zMax = zList.Max();

			minimalThreshold = Mathf.Abs(xMin - yMax); // For 2D

		}
	}
}

/*
						List<Vector3> vecList = new List<Vector3>() {new Vector3(checkerBox.xMin, checkerBox.yMin, 0),
																	new Vector3(checkerBox.xMax, checkerBox.yMin, 0),
																	new Vector3(checkerBox.xMax, checkerBox.yMax, 0),
																	new Vector3(checkerBox.xMin, checkerBox.yMax, 0),
																	new Vector3(checkerBox.xMin, checkerBox.yMin, 0)};
						bezierManager.gameObject.GetComponent<LineManager>().UpdateData_forSpline (vecList);




								if (!(B.xMax < A.xMin || B.xMin > A.xMax) 
			&& !(B.yMax < A.yMin || B.yMin > A.yMax) 
				||
			(Mathf.Abs(B.xMax - A.xMin) < minimalDistanceThreshold
			&& Mathf.Abs(B.yMin - A.yMax) < minimalDistanceThreshold) 
			||
			(Mathf.Abs(B.xMin - A.xMax) < minimalDistanceThreshold
			&& Mathf.Abs(B.yMin - A.yMax) < minimalDistanceThreshold) 
			||
			(Mathf.Abs(B.xMin - A.xMax) < minimalDistanceThreshold
			&& Mathf.Abs(B.yMax - A.yMin) < minimalDistanceThreshold) 
			||
			(Mathf.Abs(B.xMax - A.xMin) < minimalDistanceThreshold 
			&& Mathf.Abs(B.yMax - A.yMin) < minimalDistanceThreshold)
			)
*/