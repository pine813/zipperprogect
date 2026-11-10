using System;using System.Collections;using System.Collections.Generic;using System.IO;using UnityEngine;
namespace BlueRunner {public sealed class RunnerChecks:MonoBehaviour {
 [Serializable]class Report{public bool passed;public List<string> checks=new List<string>();public List<string> errors=new List<string>();}Report report=new Report();string output;GameRoot g;bool active;int events;
 void Awake(){var args=Environment.GetCommandLineArgs();active=Array.IndexOf(args,"--flight-test")>=0;if(!active){enabled=false;return;}int i=Array.IndexOf(args,"--test-output");output=args[i+1];Directory.CreateDirectory(output);Application.logMessageReceived+=(t,s,k)=>{if(k==LogType.Error||k==LogType.Exception)report.errors.Add(t+"\n"+s);};}
 void Check(bool ok,string text){if(ok)report.checks.Add(text);else report.errors.Add(text);}
 void Capture(string name){var c=g.CameraRig.GetComponent<Camera>();var rt=new RenderTexture(1440,900,24);var old=RenderTexture.active;c.targetTexture=rt;c.Render();RenderTexture.active=rt;var tex=new Texture2D(1440,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1440,900),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(output,name),tex.EncodeToPNG());c.targetTexture=null;RenderTexture.active=old;rt.Release();Destroy(rt);Destroy(tex);}
 IEnumerator Start(){if(!active)yield break;yield return null;g=GetComponent<GameRoot>();g.demoKeyboard=false;g.Player.ExternalControl=true;g.Completed+=r=>events++;
 float y=g.Player.Body.position.y;yield return new WaitForSeconds(.6f);Check(Mathf.Abs(g.Player.Body.position.y-y)<.01f&&g.Player.Body.gravityScale==0,"No gravity: released controls preserve altitude");
 bool noGround=true;foreach(var c in FindObjectsOfType<Collider2D>())if(!c.isTrigger&&c.GetComponent<PlayerController>()==null&&c.GetComponent<GravityBumper>()==null)noGround=false;Check(noGround,"No ground or platforms; solid colliders are player and gravity bumpers");Capture("flight-start.png");
 g.Player.FeedFlightInput(1,0,false);yield return new WaitForSeconds(.35f);Check(g.Player.Body.position.y>y+1,"Upward thrust controls altitude");g.Player.FeedFlightInput(-1,0,false);yield return new WaitForSeconds(.55f);Check(g.Player.Body.velocity.y< -5,"Downward thrust reverses vertical motion");g.Player.FeedFlightInput(0,0,false);yield return new WaitForSeconds(.35f);Check(Mathf.Abs(g.Player.Body.velocity.y)<.01f,"Release stabilizes drift without falling");
 g.Player.Respawn(new Vector2(2,0));g.Player.FeedFlightInput(0,0,true);yield return new WaitForSeconds(.1f);Check(g.Player.Dashing&&g.Player.Body.velocity.x>10,"Dash accelerates flight");g.SetPaused(true);var before=g.Player.Body.position;yield return new WaitForSeconds(.2f);Check(g.Player.Body.position==before&&Time.timeScale==1,"Pause is local to level");g.SetPaused(false);
 g.Player.FeedFlightInput(1,0,false);yield return new WaitForSeconds(1.2f);Check(g.Player.Body.position.y<=3.85f,"Upper navigation limit prevents leaving view");g.Player.Respawn(new Vector2(-8,0));yield return new WaitForSeconds(1.5f);
 int health=g.Health;g.Hurt(false,g.Player.transform.position+Vector3.up);Check(g.Health==health-1,"Enemy contact removes one shield");g.Hurt(false,g.Player.transform.position);Check(g.Health==health-1,"Contact invulnerability blocks repeated damage");g.Player.Respawn(new Vector2(2,0));
  var bumpers=FindObjectsOfType<GravityBumper>();Check(bumpers.Length==6,"Six solid gravity bumpers placed in level");
 var bumper=GameObject.Find("Gravity bumper 1").GetComponent<GravityBumper>();Vector2 center=bumper.transform.position;
 g.Player.Respawn(center+new Vector2(-2,.3f));g.Player.FeedFlightInput(0,0,false);int contacts=g.Player.BumperHits;
 yield return new WaitForSeconds(.26f);
 Check(g.Player.BumperHits==contacts+1&&g.Player.CollisionBoosting,"Real solid collision activates one boost");
 Check(g.Player.Body.velocity.x>9&&g.Player.Body.velocity.y>2,"Upper contact accelerates and deflects upward");Capture("bumper-impact.png");
 Check(!g.Player.ApplyBumper(center,.68f,1),"Short contact lock rejects immediate repeated impulse");
 yield return new WaitForSeconds(.1f);Check(g.Player.BumperHits==contacts+1,"One contact cannot repeatedly stack boosts");
 g.Player.FeedFlightInput(-1,0,false);yield return new WaitForSeconds(.4f);Check(g.Player.Body.velocity.y<0,"Player can steer against deflection");
 g.Player.Respawn(new Vector2(-8,0));yield return new WaitForSeconds(1.1f);
 g.Player.Respawn(center+new Vector2(-2,-.3f));g.Player.FeedFlightInput(0,0,false);contacts=g.Player.BumperHits;yield return new WaitForSeconds(.26f);
 Check(g.Player.BumperHits==contacts+1&&g.Player.Body.velocity.y< -2,"Lower contact deflects downward after recharge");
 g.Player.FeedFlightInput(1,1,true);yield return new WaitForSeconds(.08f);Check(g.Player.Body.velocity.magnitude<=PlayerController.MaximumFlightSpeed+.01f,"Dash plus collision boost respects speed limit");
 g.Player.Respawn(new Vector2(2,0));
 bool captured=false,retried=false;float start=Time.realtimeSinceStartup;while(g.State!=RunState.Won&&Time.realtimeSinceStartup-start<60){g.Player.FeedFlightInput(Mathf.Clamp(-g.Player.Body.position.y*2,-1,1),0,false);g.Fire();float x=g.Player.Body.position.x;if(x>30&&!captured){Capture("flight-action.png");captured=true;}if(x>43&&!retried){g.Retry();retried=true;Check(g.Checkpoint.x>40&&g.Player.Body.position.x>40,"Retry restores airborne checkpoint");}yield return null;}
 Check(g.State==RunState.Won,"Complete entire flight route with real physics");Check(g.Defeated>0,"Energy bolts defeat airborne drones");Check(events==1,"Single level completion event");Check(g.CameraRig.transform.position.x>100,"Camera follows continuous forward flight");Capture("flight-finish.png");report.passed=report.errors.Count==0;File.WriteAllText(Path.Combine(output,"flight-report.json"),JsonUtility.ToJson(report,true));Application.Quit(report.passed?0:1);
 }
}}
