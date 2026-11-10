using UnityEngine;
namespace BlueRunner {
public static class LevelBuilder {
 public const float EndX=120;static Transform world;
 public static void Build(GameRoot game){Art.Init();world=new GameObject("Zero gravity flight sector").transform;world.SetParent(game.transform,false);
 Camera camera=game.hostCamera;if(game.createCamera){var go=new GameObject("Flight preview camera");go.transform.SetParent(game.transform,false);camera=go.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=5;camera.backgroundColor=Art.Hex("081735");go.AddComponent<AudioListener>();}
 game.CameraRig=camera.GetComponent<CameraFollow>();if(game.CameraRig==null)game.CameraRig=camera.gameObject.AddComponent<CameraFollow>();Background(camera.transform);
 // Alternating launch directions; upper/lower edge contact overrides the fallback arrow.
 float[] bumperX={17,34,53,70,89,107};float[] bumperY={0,-.8f,.6f,-.6f,.8f,0};
 for(int i=0;i<bumperX.Length;i++){var b=Actor("Gravity bumper "+(i+1),new Vector2(bumperX[i],bumperY[i]));
 Art.Sprite("Bumper halo",Art.Glow,b.transform,Vector2.zero,Vector2.one*2.3f,Art.Mint,10);
 Art.Sprite("Bumper rim",Art.Disc,b.transform,Vector2.zero,Vector2.one*1.36f,Art.Mint,11);
 Art.Sprite("Bumper core",Art.Disc,b.transform,Vector2.zero,Vector2.one*1.16f,Art.Hex("163D87"),12);
 Ring(b.transform,.78f,Art.Hex("619BF2"),11);
 int direction=i%2==0?1:-1;
 var arrow=Art.Sprite("Deflection arrow",Art.Spike,b.transform,Vector2.zero,new Vector2(.34f,.45f),Color.white,13);arrow.transform.localRotation=Quaternion.Euler(0,0,direction>0?-45:225);
 b.AddComponent<CircleCollider2D>().radius=.68f;
 var surface=new PhysicsMaterial2D("Bumper frictionless"){friction=0,bounciness=0};b.GetComponent<CircleCollider2D>().sharedMaterial=surface;
 b.AddComponent<GravityBumper>().fallbackDirection=direction;
 }
 for(int i=0;i<44;i++){float x=5+i*2.5f,y=Mathf.Sin(i*.45f)*2.7f;var c=Actor("Floating energy crystal",new Vector2(x,y));Art.Sprite("Aura",Art.Glow,c.transform,Vector2.zero,Vector2.one,Art.Mint,9);Art.Sprite("Crystal",Art.Coin,c.transform,Vector2.zero,Vector2.one*.38f,Color.white,10);c.AddComponent<CircleCollider2D>().radius=.3f;c.GetComponent<CircleCollider2D>().isTrigger=true;c.AddComponent<CoinActor>();}
 for(int i=0;i<12;i++){float x=12+i*8.3f,y=i%3==0?0:i%3==1?2.5f:-2.5f;var drone=Actor("Orbital combat drone",new Vector2(x,y));Art.Sprite("Drone",Art.Drone,drone.transform,Vector2.zero,new Vector2(1.1f,.8f),Color.white,15);drone.AddComponent<BoxCollider2D>().size=new Vector2(.95f,.7f);drone.GetComponent<BoxCollider2D>().isTrigger=true;drone.AddComponent<Rigidbody2D>().bodyType=RigidbodyType2D.Kinematic;var enemy=drone.AddComponent<EnemyActor>();enemy.left=x-.7f;enemy.right=x+.7f;}
 for(int i=0;i<10;i++){float x=22+i*9,y=i%2==0?1.9f:-1.9f;var mine=Actor("Floating magnetic debris",new Vector2(x,y));Art.Sprite("Warning halo",Art.Glow,mine.transform,Vector2.zero,Vector2.one*2.5f,Art.Coral,8);var body=Art.Box("Armored core",mine.transform,Vector2.zero,Vector2.one*.8f,Art.Hex("5366A4"),10);body.transform.localRotation=Quaternion.Euler(0,0,45);Art.Sprite("Energy core",Art.Disc,mine.transform,Vector2.zero,Vector2.one*.2f,Art.Coral,11);mine.AddComponent<CircleCollider2D>().radius=.57f;mine.GetComponent<CircleCollider2D>().isTrigger=true;mine.AddComponent<SpikeActor>();}
 foreach(float x in new[]{40f,80f}){var checkpoint=Actor("Orbital resupply gate",new Vector2(x,0));Ring(checkpoint.transform,1.3f,Art.Mint,7);checkpoint.AddComponent<BoxCollider2D>().size=new Vector2(.8f,8.5f);checkpoint.GetComponent<BoxCollider2D>().isTrigger=true;checkpoint.AddComponent<CheckpointActor>().spawnPoint=new Vector2(x+.8f,0);}
 var exit=Actor("Sector exit",new Vector2(EndX,0));Ring(exit.transform,2.8f,Art.Mint,6);Ring(exit.transform,3.1f,Art.Hex("285EA0"),5);Art.Sprite("Exit glow",Art.Glow,exit.transform,Vector2.zero,Vector2.one*7,Art.Mint,4);exit.AddComponent<BoxCollider2D>().size=new Vector2(.6f,8.5f);exit.GetComponent<BoxCollider2D>().isTrigger=true;exit.AddComponent<ExitActor>();
 var player=Actor("Blue energy planet",game.Checkpoint);player.AddComponent<Rigidbody2D>();player.AddComponent<CapsuleCollider2D>();game.Player=player.AddComponent<PlayerController>();game.CameraRig.target=player.transform;game.CameraRig.Snap();game.TotalCoins=44;}
 static GameObject Actor(string n,Vector2 p){var g=new GameObject(n);g.transform.SetParent(world,false);g.transform.position=p;return g;}
 static void Ring(Transform parent,float radius,Color color,int order){for(int i=0;i<64;i++){float a=i/64f*Mathf.PI*2;var segment=Art.Box("Ring segment",parent,new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,new Vector2(.055f,radius*.095f),color,order);segment.transform.localRotation=Quaternion.Euler(0,0,a*Mathf.Rad2Deg);}}
 static void Background(Transform camera){Art.Sprite("Deep blue space",Art.Sky,camera,Vector2.zero,new Vector2(60,30),Color.white,-100).transform.localPosition=new Vector3(0,0,20);var random=new System.Random(92);
 for(int i=0;i<220;i++){var star=Art.Sprite("Distant star",Art.Disc,world,new Vector2((float)random.NextDouble()*150-12,(float)random.NextDouble()*12-6),Vector2.one*(.02f+(float)random.NextDouble()*.035f),Art.Hex("84C6FF"),-95);star.gameObject.AddComponent<Parallax>().factor=.94f;}
 for(int i=0;i<5;i++){var planet=Art.Sprite("Background planet",Art.Orb,world,new Vector2(12+i*28,i%2==0?3.5f:-3.5f),Vector2.one*(i%2==0?4.5f:3),Art.Hex("4C78B7"),-90);planet.gameObject.AddComponent<Parallax>().factor=.85f;}
 for(int i=0;i<35;i++){float y=i%2==0?4.2f:-4.2f;var shard=Art.Box("Far floating fragments",world,new Vector2(i*4.8f,y),new Vector2(.4f+(float)random.NextDouble(),.25f),Art.Hex("244778"),-60);shard.transform.rotation=Quaternion.Euler(0,0,i*39);shard.gameObject.AddComponent<Parallax>().factor=.4f;}
 }
}
}
