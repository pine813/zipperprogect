using UnityEngine;
namespace BlueRunner {
[RequireComponent(typeof(Rigidbody2D),typeof(CapsuleCollider2D))]
public sealed class PlayerController:MonoBehaviour {
 public float cruiseSpeed=6.8f,verticalSpeed=5.8f,acceleration=24f,dashSpeed=18f;
 public bool ExternalControl{get;set;}public Rigidbody2D Body{get;private set;}
 public bool Dashing=>dashTime>0;public bool Invulnerable=>invincible>0||Dashing;
 public float DashReady=>1-Mathf.Clamp01(cooldown/.85f);
 public bool CollisionBoosting=>boostRemaining>0;
 public int BumperHits{get;private set;}
 public const float MaximumFlightSpeed=18f;
 float boostRemaining,bumperLock;Vector2 boostVelocity;
 float vertical,speedAxis,dashTime,cooldown,invincible;bool dashQueued;Transform visual;SpriteRenderer sprite;GameRoot game;
 void Awake(){game=GetComponentInParent<GameRoot>();Body=GetComponent<Rigidbody2D>();Body.gravityScale=0;Body.freezeRotation=true;Body.interpolation=RigidbodyInterpolation2D.Interpolate;Body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
 GetComponent<CapsuleCollider2D>().size=new Vector2(.72f,.72f);
 visual=new GameObject("Flight visual").transform;visual.SetParent(transform,false);sprite=Art.Sprite("Blue energy planet",Art.Orb,visual,Vector2.zero,Vector2.one*1.2f,Color.white,20);
 var tail=gameObject.AddComponent<TrailRenderer>();tail.time=.22f;tail.startWidth=.25f;tail.endWidth=0;tail.material=new Material(Shader.Find("Sprites/Default"));tail.startColor=new Color(.3f,.8f,1,.5f);tail.endColor=new Color(.2f,.4f,1,0);tail.sortingOrder=18;
 }
 void Update(){if(game.State!=RunState.Playing)return;if(!ExternalControl){float up=(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)||Input.GetKey(KeyCode.Space)?1:0)-(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0);float speed=(Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0);FeedFlightInput(up,speed,Input.GetKeyDown(KeyCode.LeftShift)||Input.GetKeyDown(KeyCode.K));}
 visual.localRotation=Quaternion.Euler(0,0,-Body.velocity.y*2.5f);visual.localScale=new Vector3(Dashing?1.25f:1,Dashing?.85f:1,1);sprite.color=invincible>0&&(int)(Time.time*15)%2==0?new Color(1,1,1,.35f):Color.white;
 }
 public void FeedFlightInput(float up,float forward,bool dashPressed){vertical=Mathf.Clamp(up,-1,1);speedAxis=Mathf.Clamp(forward,-1,1);dashQueued|=dashPressed;}
 public void ClearInput(){vertical=speedAxis=0;dashQueued=false;}
 void FixedUpdate(){if(game.State!=RunState.Playing)return;float dt=Time.fixedDeltaTime;cooldown=Mathf.Max(0,cooldown-dt);invincible=Mathf.Max(0,invincible-dt);boostRemaining=Mathf.Max(0,boostRemaining-dt);bumperLock=Mathf.Max(0,bumperLock-dt);
 if(dashQueued&&cooldown<=0){dashTime=.18f;cooldown=.85f;GameAudio.Play(Tone.Dash);}dashQueued=false;
 Vector2 v=Body.velocity;float desiredX=cruiseSpeed+(speedAxis>=0?speedAxis*2.4f:speedAxis*3);
 float blend=boostRemaining/.9f;
 float targetX=Mathf.Lerp(desiredX,Mathf.Max(desiredX,boostVelocity.x),blend);
 float targetY=vertical*verticalSpeed+boostVelocity.y*blend;
 v.x=Mathf.MoveTowards(v.x,Dashing?dashSpeed:targetX,60*dt);v.y=Mathf.MoveTowards(v.y,targetY,acceleration*dt);
 v=Vector2.ClampMagnitude(v,MaximumFlightSpeed);
 if(Dashing)dashTime=Mathf.Max(0,dashTime-dt);Body.velocity=v;
 // Soft navigation limits, not floors: no gravity or grounded/jump states.
 if(Body.position.y>3.7f){Body.position=new Vector2(Body.position.x,3.7f);if(v.y>0)Body.velocity=new Vector2(v.x,0);}
 if(Body.position.y< -3.7f){Body.position=new Vector2(Body.position.x,-3.7f);if(v.y<0)Body.velocity=new Vector2(v.x,0);}
 }
 public bool ApplyBumper(Vector2 center,float radius,int fallbackDirection){
 if(game.State!=RunState.Playing||bumperLock>0)return false;
 float offset=Body.position.y-center.y;
 float sign=Mathf.Abs(offset)>.12f?Mathf.Sign(offset):(fallbackDirection<0?-1:1);
 // Leave along the outward tangent. Forward travel remains readable in a scrolling level.
 Body.position=center+new Vector2(-.58f,sign*.82f).normalized*(radius+.43f);
 float speed=Mathf.Clamp(Mathf.Max(13f,Body.velocity.magnitude*1.3f),13f,MaximumFlightSpeed);
 boostVelocity=new Vector2(.82f,sign*.58f).normalized*speed;Body.velocity=boostVelocity;
 boostRemaining=.9f;bumperLock=.16f;BumperHits++;invincible=Mathf.Max(invincible,.25f);
 Effects.Burst(Body.position,Art.Mint,16);GameAudio.Play(Tone.Dash);return true;
 }
 public void Knockback(Vector2 source){invincible=1.3f;Body.velocity=new Vector2(cruiseSpeed*.65f,transform.position.y>=source.y?2.5f:-2.5f);}
 public void Respawn(Vector2 position){Body.position=position;transform.position=position;Body.velocity=Vector2.right*cruiseSpeed;dashTime=cooldown=boostRemaining=bumperLock=0;invincible=1.4f;ClearInput();GetComponent<TrailRenderer>().Clear();}
 void OnDestroy(){var t=GetComponent<TrailRenderer>();if(t&&t.sharedMaterial)Destroy(t.sharedMaterial);}
}}
