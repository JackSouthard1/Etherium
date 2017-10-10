using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeWeapon : Weapon {
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

	public override void Attack (Vector2 direction, Vector2 anchor) {
//		List<Body> enemies = new List<Body> ();
		List<Vector2> affectedPositions = new List<Vector2> ();
		affectedPositions.Add (anchor + direction);


		for (int i = 0; i < affectedPositions.Count; i++) {
			RaycastHit hit;
			if (Physics.Raycast (new Vector3 (affectedPositions[i].x, 5f, affectedPositions[i].y), Vector3.down, out hit, 5f)) {
				if (hit.collider.gameObject.layer == 8) {
					endRay = hit.collider.transform.Find("Model").Find("Character").Find("Core").position;
					hitBodies.Add (hit.collider.gameObject.GetComponent<Body> ());

					StartCoroutine (RenderAttack());
				}
			}
		}
	}

	IEnumerator RenderAttack () {
		attackAnimation.Play ();
		yield return new WaitForSeconds (0.25f);

		lr.enabled = true;
		lr.SetPosition (0, transform.position);
		lr.SetPosition (1, endRay);


		yield return new WaitForSeconds (0.05f);

		AssignDamage ();
		lr.enabled = false;

		body.CompleteAction ();
	}

	void AssignDamage () {
		foreach (var body in hitBodies) {
			body.TakeDamage (info.damage);
		}
		hitBodies.Clear ();
	}
}
