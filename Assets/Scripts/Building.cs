using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : MonoBehaviour {
	public bool standable = false;
	public float height = 0f;

	public Island island;

	public Transform pad;

	//has to be in Start because subclasses use Awake
	void Start () {
		pad = transform.Find ("Pad");
	}

	public void Init(Island island) {
		this.island = island;
	}
}
