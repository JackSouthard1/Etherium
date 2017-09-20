using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mind : MonoBehaviour {
	public Body body;
	public bool myTurn = false;
	public bool active = false;

	void Start () {
		body = GetComponentInParent<Body> ();
		MindStart ();
	}

	protected virtual void MindStart () {}

	public virtual void TurnStart () {
		if (active) {
			myTurn = true;
		} else {
			myTurn = false;
			body.TurnEnd ();
		}
	}

	protected void RelayMove (Vector2 direction) {
		body.Move (direction);
		myTurn = false;
		body.TurnEnd ();
	}

	protected void Idle () {
		myTurn = false;
		body.TurnEnd ();
	}
}
