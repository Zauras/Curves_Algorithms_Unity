using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonManager : MonoBehaviour {

//	private Button button;
	private List<Bezier> bezierManagers;
	private List<AproximationSpline> aporxSplines;
	private List<InterpolationSpline> interSplines;

	// Use this for initialization
	void Start () {
		//button = this.GameObject.GetComponent<Button>();

		bezierManagers = new List<Bezier>();
		aporxSplines = new List<AproximationSpline>();
		interSplines = new List<InterpolationSpline>();
		GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
		foreach (GameObject obj in allObjects){
			if (obj.transform.tag == "BezierManager"){
				bezierManagers.Add(obj.GetComponent<Bezier>());
			}
			if (obj.transform.tag == "Spline"){
				if(obj.GetComponent<AproximationSpline>() != null) {
					aporxSplines.Add(obj.GetComponent<AproximationSpline>());
				} else if(obj.GetComponent<InterpolationSpline>() != null) {
					interSplines.Add(obj.GetComponent<InterpolationSpline>());
				}
			}
		}
	}

	public void Iterate_DeCastaljau() {
		foreach (Bezier bezier in bezierManagers){
			bezier.Use_DeCastaljau(true);
		}
	}

	public void SubdivideSplines () {
		foreach (AproximationSpline splineApx in aporxSplines) {
			splineApx.Subdivide();
		}
		foreach (InterpolationSpline splineInt in interSplines) {
			splineInt.Subdivide();
		}
	}




}
