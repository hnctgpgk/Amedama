using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Manager : MonoBehaviour {

	// インスペクタに表示するパラメータ
	public GameObject[] balls;

	public AudioClip touchClip;
	public AudioClip destroyClip;
	public AudioClip gameoverClip;

	// ゲームバランス調整用パラメータ
	private int N; // N 個以上の Ball が連結したら消す

	private int allClearBonus; // 全消しボーナス点

	private float maxMsgTime; // メッセージの表示時間

	private float nextDownTime;  // 天井を落とすタイミング
	private float nextDownTime2; // nextDownTime 秒経過すると nextDownTime2 秒の間 downSpeed の速さで落ちる
	private float downSpeed;

	private int chengeBackScore; // 何点ごとに背景を変えるか

	// 以下内部パラメータ
	private GameObject top;
	private GameObject[] back;

	private Text scoretext;
	private Text[] destroyedBallText;
	private Text msgtext;

	private AudioSource asrc;

	private Vector2 nextNextBallPos;
	private GameObject nextBall;
	private GameObject nextNextBall;

	private int score;
	private int[] destroyedBall;

	private bool readyFire;

	private bool play;
	public bool Play{
		get{ return play;}
	}

	private int clearBonus;

	private int combo;
	private int comboBonus;

	private float msgTime;

	private bool down;
	private float downTime;

	private int backNo;

	private bool cancelGameOver;
	private float cancelGameOverTime;

	// Use this for initialization
	void Start () {

		System.Func<string,Text> getText = (s) => {
			return GameObject.Find ( "Canvas/" + s).GetComponent<Text> ();
		};

		N = 3;

		allClearBonus = 1500;

		maxMsgTime = 2f;

		nextDownTime2 = 1f;
		nextDownTime = 121.906f/2f - nextDownTime2; // BGM の長さ : 121.906 秒
		downSpeed = 1f;

		chengeBackScore = 10000;

		top = GameObject.Find ("Top");

		back = new GameObject[3];
		for (int i = 0; i < 3; ++i) {
			back [i] = GameObject.Find ("Back"+(i+1));
		};

		scoretext = getText("ScoreTitle/Score");
		msgtext = getText ("Message");

		destroyedBallText = new Text[(int)(Ball.BallType.BALLSIZE)];
		destroyedBallText[0] = getText("ScoreTitle/BlackBall/BlackBallCount");
		destroyedBallText[1] = getText("ScoreTitle/PinkBall/PinkBallCount");
		destroyedBallText[2] = getText("ScoreTitle/GreenBall/GreenBallCount");
		destroyedBallText[3] = getText("ScoreTitle/BlueBall/BlueBallCount");
		destroyedBallText[4] = getText("ScoreTitle/RedBall/RedBallCount");

		asrc = GetComponent<AudioSource>();

		nextNextBallPos = new Vector2 (-5.5f, 4.0f);

		nextBall = CreateBall ( Vector3.zero ); // zero の場合最初からREADY状態になる
		nextNextBall = CreateBall (nextNextBallPos);

		score = 0;
		destroyedBall = new int[(int)(Ball.BallType.BALLSIZE)];
		AddScore (0);

		readyFire = true;

		play = true;

		clearBonus = 0;

		combo = 0;
		comboBonus = 0;

		msgTime = 0;
		msgtext.enabled = false;

		down = false;
		downTime = Time.time;

		backNo = 0;

		cancelGameOver = false;
		cancelGameOverTime = 0f;

		ShowMsg ("スタート");
	}
	
	// Update is called once per frame
	void Update () {

		if (Input.GetKey (KeyCode.Escape)) {
			SceneManager.LoadScene ("Title");
		}

		if (!play) {

			if ( Time.time >= cancelGameOverTime && Input.GetMouseButtonDown (0) ) {
				cancelGameOver = true;
			}

			return;
		}

		if ( !down ){ // 天井落下待ち

			if (Time.time - downTime >= nextDownTime) {
				down = true;
				downTime = Time.time;
			}
		}
		else{ // 天井落下中
			
			Vector3 pos = top.transform.position;
			pos.y -= downSpeed * Time.deltaTime;
			top.transform.position = pos;

			// ボールも落とす
			GameObject[] objs = GameObject.FindGameObjectsWithTag ("Ball");
			foreach (GameObject obj in objs) {
				obj.GetComponent<Ball> ().Down (downSpeed * Time.deltaTime);
			}

			if (Time.time - downTime >= nextDownTime2) {
				down = false;
				downTime = Time.time;

				if (top.transform.position.y <= 3.5f) {
					GameOver ();
				}
			}
		}
			
		// メッセージ表示中
		if ( msgtext.enabled && Time.time - msgTime >= maxMsgTime) {
			
			msgtext.enabled = false;

			if (combo > 0) { // コンボ表示

				combo = 0;
				comboBonus = 0;

				if (clearBonus > 0 ) {
					
					ShowMsg ("全消し！ +" + clearBonus);
				}

			} else if ( clearBonus > 0 ) { // 全消し表示
				clearBonus = 0;
			}
		}
	}

	// (Arrow からコールバックされる) Ball を angle の角度で発射
	public void Fire( float angle )
	{
		if (! readyFire) {
			return;
		}

		nextBall.GetComponent<Ball> ().Fire (angle);
		readyFire = false;
		nextNextBall.GetComponent<Ball> ().Wait (); // 次のBallの装填開始
	}

	// (Ball からコールバックされる) 発射準備完了 
	public void NextBallReady()
	{
		nextBall = nextNextBall;
		readyFire = true;
		nextNextBall = CreateBall (nextNextBallPos);
	}
		
	// (Ball からコールバックされる) target に同タイプの Ball が何個くっついているか調べて、N 個以上くっついていたらまとめて消す
	public void DestroyBalls( Ball target )
	{
		asrc.PlayOneShot (touchClip);

		// 探索初期化
		System.Action< List<Ball> > InitSearch = (bs) => {
			foreach (Ball ball in bs) {
				ball.InitSearch ();
			}
		};

		// 全消しチェック
		System.Func< List<Ball> , bool > CheckAllClear = (bs) =>{
			foreach (Ball ball in bs) {
				if (ball.Status == Ball.BallStatus.STOP ) {
					return false;
				}
			}
			return true;
		};

		List<Ball> balls = new List<Ball> ();
		GameObject[] objs = GameObject.FindGameObjectsWithTag("Ball");
		foreach( GameObject obj in objs ){
			balls.Add (obj.GetComponent<Ball> ());
		}

		InitSearch (balls);
		int n = target.CountConnectedSameBalls (target);

		if (n >= N) {
			
			Debug.Log ("----------------------- Destroy n = " + n);

			asrc.PlayOneShot (destroyClip);
			destroyedBall [(int)(target.ballType)] += n;
			AddScore (n * target.Score ());

			// 消す Ball に DESTROY フラグを付ける
			InitSearch (balls);
			target.DestroyBalls (target);
	
			// 再グループ化
			InitSearch (balls);
			foreach (Ball ball in balls) {
				ball.InitGroupNo ();
			}
			int groupNo = 0;
			foreach (Ball ball in balls) {
				if (ball.SetGroupNo (groupNo)) {
					++groupNo;
				}
			}
				
			// 天井から離れているグループを落下させる
			bool fall = false;
			for (int i = 0; i < groupNo; ++i) {

				// 同グループ内のすべての Ball が天井に付いてないか調べる
				bool tmpfall = true;
				foreach (Ball ball in balls) {
					if (ball.IsOnTop (i)) {
						tmpfall = false;
						break;
					}
				}

				if (tmpfall) {
					fall = true;
					foreach (Ball ball in balls) {
						ball.Fall (i);
					}
				}
			}
				
			// 全消しボーナスの処理
			if ( CheckAllClear ( balls )) {

				clearBonus += allClearBonus;
				AddScore (allClearBonus);

				if (!fall) { // もし Ball が落ちるならコンボ処理が終わってから全消しメッセージ表示
					ShowMsg ("全消し！ +" + clearBonus);
				} 
			}

			// DESTROY 状態の BALL を削除
			foreach (Ball ball in balls) {
				ball.ExecDestroy ();
			}
		}
	}

	// (Ball からコールバックされる) Ball が下まで落ちた
	public void BallGrounded( Ball target )
	{	
		if (combo %5 == 0) { // 音声の長さで要調整
			asrc.PlayOneShot (destroyClip);
		}

		combo++;
		int bs = (int)(target.Score () * combo * 1.5f);
		comboBonus += bs;
		destroyedBall [(int)(target.ballType)]++;
		AddScore ( bs );

		ShowMsg ( combo + " COMBO +" + comboBonus.ToString () );
	}

	public void GameOver()
	{
		play = false;
		asrc.Stop ();
		StaticData.SetHiScore (score);

		StartCoroutine ("GameOverWait");
	}

	IEnumerator GameOverWait(){

		cancelGameOverTime = Time.time + 1f;
		cancelGameOver = false;
		asrc.PlayOneShot (gameoverClip);

		while ( ! cancelGameOver && top.transform.position.y >= 0) {
			
			Vector3 pos = top.transform.position;
			pos.y -= 1f;
			top.transform.position = pos;
			yield return new WaitForSeconds (0.05f);
		}

		while ( ! cancelGameOver && asrc.isPlaying ) {
			yield return new WaitForSeconds (0.05f);
		}

		if (cancelGameOver) {
			asrc.Stop ();
		}

		SceneManager.LoadScene ("GameOver");
	}

	private GameObject CreateBall( Vector2 pos )
	{
		return Instantiate (balls [(int)Random.Range(0,(int)Ball.BallType.BALLSIZE)], pos, Quaternion.identity);
	}

	private void ShowMsg( string msg )
	{
		msgTime = Time.time;
		msgtext.enabled = true;
		msgtext.text = msg;
	}

	private void AddScore( int s )
	{
		score += s;
		scoretext.text = score.ToString ().PadLeft (7, '0');

		for (int i = 0; i < (int)Ball.BallType.BALLSIZE; ++i) {
			destroyedBallText [i].text = "x " + destroyedBall [i];
		}

		if (score >= (backNo + 1) * chengeBackScore) {
			back [backNo % back.Length].SetActive (false);
			backNo++;
			back [backNo % back.Length].SetActive (true);
		}
	}
}
