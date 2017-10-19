using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
	public Transform bar;

	public void UpdateBar(float curHeatlh, float maxHealth) {
		if (curHeatlh > 0) {
			bar.transform.localScale = new Vector3 (curHeatlh / maxHealth, 1f, 1f);
		} else {
			bar.gameObject.SetActive (false);
		}
	}
}