using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceLerp : MonoBehaviour
{
	Vector3 startPos;
	Vector3 endPos;
	float initialDelay;

	const float animationTime = 0.2f;
	public const float delay = 0.1f;

	public System.Action cb;

	public void Init(Vector3 start, Vector3 end, int id) {
		startPos = start;
		endPos = end;
		initialDelay = id * delay;

		StartCoroutine (Animate());
	}

	IEnumerator Animate() {
		Renderer rend = gameObject.GetComponentInChildren<Renderer> ();
		rend.enabled = false;

		yield return new WaitForSeconds (initialDelay);

		rend.enabled = true;
		float t = 0f;
		while(t < animationTime) {
			transform.position = Vector3.Lerp (startPos, endPos, t / animationTime);
			yield return null;
			t += Time.deltaTime;
		}

		if (cb != null) {
			cb ();
		}

		Destroy (gameObject);
	}
}