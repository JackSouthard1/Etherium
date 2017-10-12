using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicWeapon : Weapon {
	Vector3 endRay;
	LineRenderer lr;
	Animation attackAnimation;

	List<Body> hitBodies = new List<Body>();

	public override void ChildStart () {
		attackAnimation = transform.parent.GetComponentInChildren<Animation> ();
		lr = GetComponent<LineRenderer> ();
		lr.enabled = false;
		lr.positionCount = 2;
	}

	public override void UpdateWeapon () {
		base.UpdateWeapon ();

		if (info.ToIndex () != 0) {
			attackAnimation = transform.GetChild(transform.childCount - 1).GetComponent<Animation> ();
		} else {
			attackAnimation = transform.parent.GetComponentInChildren<Animation> ();
		}
	}

	public override void Attack (Vector2 direction, Vector2 anchor) {
//		List<Body> enemies = new List<Body> ();
		List<Vector2> affectedPositions = new List<Vector2> ();
		for (int i = 1; i <= info.range; i++) {
			affectedPositions.Add (anchor + (direction * i));
		}

		for (int i = 0; i < affectedPositions.Count; i++) {
			RaycastHit hit;
			if (Physics.Raycast (new Vector3 (affectedPositions[i].x, 5f, affectedPositions[i].y), Vector3.down, out hit, 5f)) {
				if (hit.collider.gameObject.layer == 8) {
					endRay = hit.collider.transform.Find("Model").Find("Character").Find("Core").position;
					hitBodies.Add (hit.collider.gameObject.GetComponent<Body> ());

					StartCoroutine (RenderAttack());

					if (!info.passesThroughEnemies) {
						return;
					}
				}
			}
		}
	}

	IEnumerator RenderAttack () {
		attackAnimation.Play ();
		yield return new WaitForSeconds (info.initialDelay);

		for (int i = 0; i < info.shotsPerAttack; i++) {
			lr.enabled = true;
			lr.SetPosition (0, transform.position);
			lr.SetPosition (1, endRay);
			yield return new WaitForSeconds (0.05f);
			lr.enabled = false;

			if (info.shotsPerAttack > 1) {
				yield return new WaitForSeconds (info.rateOfFirePerAttack);
			}
		}
		
		AssignDamage ();

		body.CompleteAction ();
	}

	void AssignDamage () {
		foreach (var body in hitBodies) {
			body.TakeDamage (info.damage);
		}
		hitBodies.Clear ();
	}
}
