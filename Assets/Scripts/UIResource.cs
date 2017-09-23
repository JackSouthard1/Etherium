using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIResource : MonoBehaviour
{
	Text amountText;
	Button resourceButton;

	public int amount;

	public float totalWidth {
		get {
			if (amount > 0)
				return amountText.rectTransform.localPosition.x + amountText.rectTransform.rect.width;
			else
				return 0f;
		}
	}

	void Awake () {
		amountText = gameObject.GetComponentInChildren<Text> ();
		resourceButton = gameObject.GetComponentInChildren<Button> ();
	}

	public void Init(TerrainManager.ResourceInfo resourceInfo) {
		resourceButton.image.sprite = resourceInfo.sprite;
		resourceButton.onClick.AddListener(() => GameObject.FindObjectOfType<Player>().DropResource(resourceInfo));
	}

	public void UpdateVisual() {
		if (amount > 0) {
			float textWidth = (float)(amount.ToString ().Length * amountText.fontSize);
			amountText.rectTransform.sizeDelta = new Vector2 (textWidth, amountText.rectTransform.rect.height);
			amountText.text = amount.ToString ();

			SetVisible (true);
		} else {
			SetVisible (false);
		}
	}

	void SetVisible(bool isVisible) {
		amountText.enabled = isVisible;

		resourceButton.enabled = isVisible;
		resourceButton.image.enabled = isVisible;
	}
}