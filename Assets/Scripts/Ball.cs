using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour {

	// Ball の種類
	public enum BallType{
		Black,
		Pink,
		Green,
		Blue,
		Red,
		BALLSIZE,

		END
	};

	// Ball の状態
	public enum BallStatus{
		INIT,
		WAIT,
		READY,
		RIGING,
		STOP,
		FALLING,
		DESTROY,

		END
	};
		
	// インスペクタに表示するパラメータ
	public BallType ballType = BallType.Red;
	public GameObject particle;

	// ゲームバランス調整用パラメータ
	private float speed;  // Ball の速度
	private float radius; // Ball の半径
	private float margin; // Ball が静止した時に天井やその他の Ball が半径 + margin の範囲内にあればくっつける

	public int Score()
	{
		int score = 0;
		switch (ballType) {

		case BallType.Black:
			score = 150;
			break;

		case BallType.Pink:
			score = 90;
			break;

		case BallType.Green:
			score = 70;
			break;

		case BallType.Blue:
			score = 50;
			break;

		case BallType.Red:
			score = 30;
			break;
		}

		return score;
	}

	// 以下内部パラメータ
	private Manager manager;
	private Transform arrowTrans;

	private Rigidbody2D rigid;

	private List<Ball> links;

	private void SetLink( Ball ball ){
		Debug.Assert (links.Contains (ball) == false , "links contains");
		links.Add (ball);
	}
	private void UnLink( Ball ball ){
		links.Remove (ball);
	}

	private BallStatus status; // 状態
	public BallStatus Status{
		get{ return status; }
	}

	private int groupNo; // グループ番号
	private bool onTop; // 天井にくっついてるか
	private bool searched; // 探索で使う
	private float fireTime;

	// Use this for initialization
	void Start () {

		speed = 6f;
		radius = 0.5f;
		margin = 0.1f;

		manager = GameObject.Find ("Manager").GetComponent<Manager> ();
		arrowTrans = GameObject.Find ("Arrow/ArrowPoint").GetComponent<Transform> ();

		rigid = GetComponent<Rigidbody2D> ();

		links = new List<Ball> ();

		if (transform.position == Vector3.zero) {
			status = BallStatus.READY; // 一番最初のボール
		} else {
			status = BallStatus.INIT;
		}
		groupNo = -1;
		onTop = false;
		searched = false;
	}
	
	// Update is called once per frame
	void Update () {

		if( ! manager.Play) {
			rigid.velocity = Vector2.zero;
			status = BallStatus.END;
			return;
		}

		if (status == BallStatus.READY) {
			transform.position = arrowTrans.position;
		}
	}
		
	void OnCollisionEnter2D( Collision2D col )
	{
		// 上昇中に止まっている Ball か天井にぶつかった
		if ( status == BallStatus.RIGING && (col.gameObject.tag == "Top" || col.gameObject.tag == "Ball")) {
			
			rigid.velocity = Vector2.zero;
			rigid.constraints = RigidbodyConstraints2D.FreezeAll;
			status = BallStatus.STOP;
			gameObject.tag = "Ball";  // 上昇中のタグは "RigingBall"

			// ぶつかった天井や Ball の座標を取得
			Vector2 pos = transform.position;
			Vector2 nearestPos;
			if (col.gameObject.tag == "Top") {
				
				onTop = true;
				nearestPos = new Vector2 (transform.position.x, col.gameObject.transform.position.y+radius);

				Debug.Log ("pos = " + pos + " nearest top pos = " + nearestPos + " d = " + (pos - nearestPos).magnitude + " / on top= " + onTop);
			} else {

				Ball ball = col.gameObject.GetComponent<Ball> ();
				SetLink (ball);
				ball.SetLink (this);

				onTop = false;
				nearestPos = col.gameObject.transform.position;

				Debug.Log ("pos = " + pos + " nearest ball pos = " + nearestPos + " d = " + (pos - nearestPos).magnitude + " / on top = " + onTop);
			}

			// 天井やその他の Ball が半径 + margin の範囲内にあるなら位置を補正してくっつける
			Collider2D[] otherCols = Physics2D.OverlapCircleAll (pos, radius + margin);
			Vector2 secondPos = Vector2.zero; // 2番目に近いオブジェクトの位置
			float minD = System.Single.MaxValue;
			foreach (Collider2D otherCol in otherCols) {

				Vector2 otherPos = Vector2.zero;
				bool hit = false;
				if (otherCol.gameObject.tag == "Top" && !onTop) {

					otherPos = new Vector2 (transform.position.x, otherCol.gameObject.transform.position.y + radius);
					onTop = true;
					hit = true;
			
					Debug.Log ("top pos = " + otherPos + " d = " + (pos - otherPos).magnitude);
				}
				if (otherCol.gameObject.tag == "Ball" && otherCol.gameObject != gameObject && otherCol.gameObject != col.gameObject) {

					Ball ball = otherCol.gameObject.GetComponent<Ball> ();
					SetLink (ball);
					ball.SetLink (this);

					otherPos = otherCol.transform.position;
					hit = true;

					Debug.Log ("ball pos = " + otherPos + " d = " + (pos - otherPos).magnitude);
				}

				if (hit) {				
					float tmpD = (pos - otherPos).magnitude;
					if (tmpD < minD) {
						secondPos = otherPos;
						minD = tmpD;
					}
				}
			}

			//  最初にぶつかったオブジェクト、及び2番目に近いオブジェクトに接するように位置を補正
			if (minD < System.Single.MaxValue && (secondPos - nearestPos).magnitude <= radius * 4f) {
					
				Debug.Log ("second pos = " + secondPos + " d = " + minD + " / on top = " + onTop);

				Vector2 centerVec = (secondPos - nearestPos) / 2f; // 中点
				Vector2 normalVec = new Vector2 (centerVec.y, -centerVec.x).normalized; // 法線ベクトル

				float height = Mathf.Sqrt ( radius * 2f - centerVec.magnitude * centerVec.magnitude);
				Vector2 newPos1 = nearestPos + centerVec + normalVec * height;
				Vector2 newPos2 = nearestPos + centerVec - normalVec * height;
				if ((newPos1 - pos).magnitude < (newPos2 - pos).magnitude) {
					transform.position = newPos1;
				} else {
					transform.position = newPos2;
				}

				Debug.Log ("normal = " + normalVec + " / new pos1 = " + newPos1 + " / new pos2 = " + newPos2);

				// 念の為、位置を補正したらぶつかったオブジェクトが無いか再確認
				otherCols = Physics2D.OverlapCircleAll (transform.position, radius);
				foreach (Collider2D otherCol in otherCols) {

					if (otherCol.gameObject.tag == "Top" && !onTop) {
							
						onTop = true;
	
						Debug.Assert (false, "Top");
					} 
					if (otherCol.gameObject.tag == "Ball" && otherCol.gameObject != gameObject) {

						Ball ball = otherCol.gameObject.GetComponent<Ball> ();
						if (!links.Contains (ball)) {
							SetLink (ball);
							ball.SetLink (this);

							Debug.Assert (false, "Ball");
							Debug.Log ("pos = " + otherCol.transform.position);
						}
					}
				}
			}

			Debug.Log( "result: grpup = " + groupNo + " pos = " + transform.position + " status = " + status + " links = " + links.Count );

			manager.DestroyBalls (this);
		} 

	}

	void OnTriggerEnter2D (Collider2D coll)
	{
		// 上昇中に逆行して下に落ちた
		if( status == BallStatus.RIGING && coll.tag == "Bottom") {
			Destroy (gameObject);
		}

		// 落下中に下まで落ちた
		if( status == BallStatus.FALLING && coll.tag == "Bottom") {
			manager.BallGrounded (this);
			Destroy (gameObject);
		}
	}

	void OnTriggerStay2D (Collider2D coll)
	{
		// ゲームオーバーのラインを超えた
		if( status == BallStatus.STOP && coll.tag == "Danger") {
			manager.GameOver ();
		}
	}

	// 天井と一緒に落ちる
	public void Down( float d )
	{
		if ( status != BallStatus.STOP ) {
			return;
		}

		Vector2 pos = transform.position;
		pos.y -=d;
		transform.position = pos;
	}
		
	// 発射待ち開始
	public void Wait()
	{
		if ( status != BallStatus.INIT ) {
			return;
		}
		status = BallStatus.WAIT;
		GetComponent<Animator>().SetBool("FadeOut", true);
	}


	// 発射準備完了
	public void Ready(){

		if (status == BallStatus.WAIT) {
			transform.localScale = new Vector3 (1, 1, 1);
			transform.position = arrowTrans.position;
			status = BallStatus.READY;
			manager.NextBallReady ();
		} 
	}

	// 発射
	public void Fire( float angle )
	{
		if ( status != BallStatus.READY) {
			return;
		}

		Quaternion rotation = Quaternion.Euler (new Vector3 (0, 0, angle));
		Vector3 vec = new Vector2 (0, speed);
		rigid.velocity = rotation * vec;
		GetComponent<CircleCollider2D> ().enabled = true;
		status = BallStatus.RIGING;
	}

	// 再帰を使った探索の初期化
	public void InitSearch()
	{
		searched = false;
	}

	// グループ番号の初期化
	public void InitGroupNo()
	{
		groupNo = -1;
	}
		
	// target と同タイプの Ball が何個連結されてるか探索して個数を返す
	public int CountConnectedSameBalls( Ball target )
	{
		if ( status != BallStatus.STOP ) {
			return 0;
		}

		if ( searched ){
			return 0;
		}

		if ( ballType != target.ballType ) {
			return 0;
		}

		searched = true;
		int ret = 1;
		foreach (Ball ball in links) {
			ret += ball.CountConnectedSameBalls (target);
		}

		return ret;
	}

	// target と同タイプなら消す。子も探索して消す
	public bool DestroyBalls( Ball target )
	{
		if ( status != BallStatus.STOP ) {
			return false;
		}

		if ( searched ){
			return false;
		}

		if ( ballType != target.ballType ) {
			return false;
		}
			
		searched = true;
		status = BallStatus.DESTROY;
		foreach (Ball ball in links) {
			ball.DestroyBalls (target);
		}

		return true;
	}
			
	// グループ番号を gNo にセットする。子も探索して番号をセットする
	public bool SetGroupNo( int gNo )
	{
		if ( status != BallStatus.STOP ) {
			return false;
		}

		if ( searched ){
			return false;
		}
			
		searched = true;
		groupNo = gNo;
		foreach (Ball ball in links) {
			ball.SetGroupNo (gNo);
		}

		return true;
	}

	// 天井にくっついているか?
	public bool IsOnTop( int gNo )
	{
		if ( status != BallStatus.STOP ) {
			return false;
		}

		Debug.Assert (gNo != -1, "invalid group no");

		if (gNo != groupNo) {
			return false;
		}

		if ( ! onTop ){
			return false;
		}

		return true;
	}

	// 落下指示
	public bool Fall( int gNo )
	{
		if ( status != BallStatus.STOP ) {
			return false;
		}

		Debug.Assert (gNo != -1, "invalid group no");

		if (gNo != groupNo) {
			return false;
		}

		status = BallStatus.FALLING;
		rigid.gravityScale = 1f;
		rigid.constraints = RigidbodyConstraints2D.None;
		gameObject.layer = 9; // FallingBall

		foreach (Ball ball in links) {
			ball.UnLink (this);
		}

		return true;
	}

	// 状態が DESTROY なら子とのリンクを切ってから消す
	public void ExecDestroy()
	{
		if (status != BallStatus.DESTROY) {
			return;
		}

		foreach (Ball ball in links) {
			ball.UnLink (this);
		}
		Destroy (gameObject);
		GameObject obj = Instantiate (particle, transform.position, Quaternion.identity);
		Destroy (obj, 1f);
	}
}
