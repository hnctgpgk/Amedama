using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arrow : MonoBehaviour {

	// ゲームバランス調整用パラメータ
	private float range; // 角度の範囲

	// 以下内部パラメータ
	private Manager manager;

	// Use this for initialization
	void Start () {
		
		manager = GameObject.Find ("Manager").GetComponent<Manager> ();

		range = 15f;
	}
	
	// Update is called once per frame
	void Update () {

		if( ! manager.Play) {
			return;
		}

		Vector3 pos = Camera.main.WorldToScreenPoint (transform.position);
		Vector3 d = Input.mousePosition - pos;
		float angle = -90 + Mathf.Clamp (Mathf.Atan2 (d.y, d.x) * Mathf.Rad2Deg, range, 180f - range);
		transform.eulerAngles = new Vector3 (0, 0, angle);

		if( Input.GetMouseButton( 0 ) ){
			manager.Fire( angle );
		}
	}
}
