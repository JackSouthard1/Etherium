using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeWeapon : Weapon {

	public override void Attack (Vector2 direction, Vector2 anchor) {
		List<Body> enemies = new List<Body> ();
		List<Vector2> affectedPositions = new List<Vector2> ();
		affectedPositions.Add (anchor + direction);

		for (int i = 0; i < affectedPositions.Count; i++) {
			RaycastHit hit;
			if (Physics.Raycast (new Vector3 (affectedPositions[i].x, 5f, affectedPositions[i].y), Vector3.down, out hit, 5f)) {
				if (hit.collider.gameObject.layer == 8) {
					hit.collider.gameObject.GetComponent<Body> ().TakeDamage (damage);
				}
			}
		}
	}
}
