using UnityEngine;
namespace BlueRunner {
// A solid circular collider. Only contact entry activates it; each orb has its own cooldown.
[RequireComponent(typeof(CircleCollider2D))]
public sealed class GravityBumper:MonoBehaviour {
 public int fallbackDirection=1;public float recharge=1f;
 float availableAt;GameRoot game;Transform halo;
 void Awake(){game=GetComponentInParent<GameRoot>();}
 void Start(){halo=transform.Find("Bumper halo");}
 void Update(){if(halo!=null){float pulse=1+Mathf.Sin(game.Elapsed*3)*.06f;halo.localScale=Vector3.one*2.3f*pulse;}}
 void OnCollisionEnter2D(Collision2D hit){var player=hit.collider.GetComponent<PlayerController>();if(player==null||game.State!=RunState.Playing||game.Elapsed<availableAt)return;
 if(player.ApplyBumper(transform.position,GetComponent<CircleCollider2D>().radius,fallbackDirection))availableAt=game.Elapsed+recharge;
 }
}
}
