using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : MonoBehaviour {
	public bool standable = false;
	public float height = 0f;

	public Island island;

	public void Init(Island island) {
		this.island = island;
	}
}
