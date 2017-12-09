using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class TitleManager : MonoBehaviour {

	void Awake()
	{
		if (Application.platform == RuntimePlatform.WindowsPlayer) {
			Screen.SetResolution (1024, 768, false);
		}
	}
		
	// Use this for initialization
	void Start () {
	}

	// Update is called once per frame
	void Update () {

		if (Input.GetKey (KeyCode.Escape)) {
			Application.Quit ();
		}

		if ( Input.GetMouseButtonDown( 0 ) ) {
			SceneManager.LoadScene ("Game");
		}
	}
}
