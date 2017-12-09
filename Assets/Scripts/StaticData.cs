using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticData{

	public static List<int> hiScore;

	public static void SetHiScore(int sc)
	{
		Debug.Log ("SetHiScore");

		if( hiScore == null ){
			hiScore = new List<int>();
			for (int i = 1; i <= 10; ++i) {
				hiScore.Add (i * 2000);
			}
		}
		hiScore.Add(sc);
		hiScore.Sort( (x,y)=> y-x );
		hiScore.RemoveAt( hiScore.Count-1 );

		foreach (var i in hiScore) {
			Debug.Log (i);
		}
	}
}