using System;
using UnityEngine;
namespace BlueRunner {
public enum RunState {Playing,Paused,Won,GameOver}
[Serializable] public sealed class LevelResult {public float seconds;public int defeated,energy,retries;}
public sealed class GameRoot:MonoBehaviour {
 public bool autoStart=true,createCamera=true,showHud=true,demoKeyboard=true;
 public Camera hostCamera;
 public event Action<LevelResult> Completed;
 public RunState State{get;private set;}=RunState.Paused;
 public PlayerController Player{get;set;}public CameraFollow CameraRig{get;set;}
 public int Health{get;private set;}=3;public int Coins{get;private set;}public int TotalCoins{get;set;}public int Defeated{get;private set;}public int Retries{get;private set;}
 public Vector2 Checkpoint{get;private set;}=new Vector2(1.8f,0); public float Elapsed{get;private set;}
 float lastFire=-10;bool built;Font font;GUIStyle label;string notice="W / S 飞行 · J 发射 · 青色碰撞球：撞后加速并偏转";float noticeUntil=6;
 void Start(){if(autoStart)BeginLevel();}
 public void BeginLevel(){if(!createCamera && hostCamera==null){Debug.LogError("Assign hostCamera before BeginLevel.");return;}if(!built){LevelBuilder.Build(this);gameObject.AddComponent<GameAudio>();built=true;}State=RunState.Playing;Player.ExternalControl=!demoKeyboard;Player.Body.simulated=true;}
 void Update(){if(!built)return;if(demoKeyboard&&Input.GetKeyDown(KeyCode.Escape))SetPaused(State==RunState.Playing);
 if(State==RunState.Playing){Elapsed+=Time.deltaTime;if(demoKeyboard&&Input.GetKey(KeyCode.J))Fire();}
 if(demoKeyboard&&Input.GetKeyDown(KeyCode.R))Retry();}
 public void SetPaused(bool value){if(!built||State==RunState.Won)return;State=value?RunState.Paused:RunState.Playing;Player.Body.simulated=!value;Player.ClearInput();}
 public void Fire(){if(State!=RunState.Playing||Elapsed-lastFire<.22f)return;lastFire=Elapsed;
 var b=Art.Sprite("Pulse bolt",Art.Disc,transform,(Vector2)transform.InverseTransformPoint(Player.transform.position)+new Vector2(.7f,.03f),new Vector2(.55f,.13f),Art.Mint,25);b.gameObject.AddComponent<PulseBolt>();GameAudio.Play(Tone.Coin);}
 public void Collect(Vector2 p){Coins++;Effects.Burst(p,Art.Mint,6);GameAudio.Play(Tone.Coin);}
 public void EnemyDefeated(Vector2 p){Defeated++;Effects.Burst(p,Art.Coral,18);GameAudio.Play(Tone.Stomp);}
 public void SetCheckpoint(Vector2 p){Checkpoint=p;Health=3;Notify("补给节点已激活 · 护盾恢复");GameAudio.Play(Tone.Checkpoint);}
 public void Hurt(bool respawn,Vector2 source){if(State!=RunState.Playing||(!respawn&&Player.Invulnerable))return;Health--;GameAudio.Play(Tone.Hurt);if(Health<=0||respawn){Retry();return;}Player.Knockback(source);}
 public void Retry(){if(!built)return;Retries++;Health=3;State=RunState.Playing;Player.Body.simulated=true;Player.Respawn(Checkpoint);CameraRig.Snap();Notify("返回补给节点 · 继续前进");}
 public void Win(){if(State!=RunState.Playing)return;State=RunState.Won;Player.Body.simulated=false;GameAudio.Play(Tone.Win);Completed?.Invoke(new LevelResult{seconds=Elapsed,defeated=Defeated,energy=Coins,retries=Retries});}
 void Notify(string text){notice=text;noticeUntil=Elapsed+3;}
 void OnGUI(){if(!showHud||!built)return;if(label==null){font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Arial"},20);label=new GUIStyle(GUI.skin.label){font=font,fontSize=20};label.normal.textColor=Art.Gold;}
 GUI.color=new Color(.02f,.08f,.2f,.87f);GUI.DrawTexture(new Rect(0,0,Screen.width,74),Texture2D.whiteTexture);GUI.color=Color.white;
 GUI.Label(new Rect(28,18,Screen.width-56,38),"护盾 "+new string('◆',Health)+"     能量 "+Coins+"     击破 "+Defeated+"     距离 "+Mathf.Clamp(Mathf.FloorToInt(Player.transform.localPosition.x),0,120)+" / 120",label);
 if(Elapsed<noticeUntil)GUI.Label(new Rect(28,Screen.height-45,Screen.width-56,40),notice,label);
 if(State==RunState.Paused||State==RunState.Won){GUI.color=new Color(.02f,.07f,.18f,.95f);GUI.DrawTexture(new Rect(Screen.width/2-280,Screen.height/2-80,560,160),Texture2D.whiteTexture);GUI.color=Color.white;
 GUI.Label(new Rect(Screen.width/2-250,Screen.height/2-55,510,130),State==RunState.Won?"本关完成\n用时 "+Elapsed.ToString("F1")+" 秒 · 击破 "+Defeated+" · 重试 "+Retries+"\nR 从补给节点再次试玩":"已暂停\nEsc 继续",label);}}
 void OnDisable(){if(built)SetPaused(true);}void OnDestroy(){if(font)Destroy(font);}
}
public sealed class PulseBolt:MonoBehaviour {
 float life=1.3f;void Update(){var g=GetComponentInParent<GameRoot>();if(g.State!=RunState.Playing)return;float step=25*Time.deltaTime;
 foreach(var hit in Physics2D.CircleCastAll(transform.position,.16f,Vector2.right,step)){var enemy=hit.collider.GetComponent<EnemyActor>();if(enemy!=null){enemy.Defeat();Destroy(gameObject);return;}}
 transform.position+=Vector3.right*step;life-=Time.deltaTime;if(life<=0)Destroy(gameObject);}
}
}
