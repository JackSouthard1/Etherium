using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : MonoBehaviour {
	public bool standable = false;
	public float height = 0f;

	public Island island;

	public Transform pad;

	public int supply;

	public bool isActive = true;

	//has to be in Start because subclasses use Awake
	void Start () {
		pad = transform.Find ("Pad");
	}

	public void Init(Island island) {
		this.island = island;
	}

	public virtual void TurnEnd() {
		if (supply <= 0) {
			isActive = false;
		}
	}
}
