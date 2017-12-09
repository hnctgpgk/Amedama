using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour {

	// Use this for initialization
	void Start () {

		for(int i = 1; i <= StaticData.hiScore.Count; ++i ){
			GameObject.Find ( "Canvas/HiScore/HS" + i).GetComponent<Text> ().text 
			= i.ToString().PadLeft (2, ' ') + "位: " + StaticData.hiScore[i-1].ToString ().PadLeft (7, '0');
		}
	}

	// Update is called once per frame
	void Update () {

		if (Input.GetKey (KeyCode.Escape)) {
			Application.Quit ();
		}

		if ( Input.GetMouseButtonDown( 0 ) ) {
			SceneManager.LoadScene ("Title");
		}
	}
}
