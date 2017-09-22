using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mind : MonoBehaviour {
	public Body body;
	protected GameManager gm;
	public bool myTurn = false;
	public bool active = false;

	void Start () {
		body = GetComponentInParent<Body> ();
		gm = GameObject.Find ("GameManager").GetComponent<GameManager> ();
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

	protected void RelayAction (Vector2 direction) {
		body.StartAction (direction);
		myTurn = false;
	}

	protected void Idle () {
		myTurn = false;
		body.TurnEnd ();
	}
}
