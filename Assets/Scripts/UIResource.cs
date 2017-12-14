using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIResource : MonoBehaviour
{
	Text amountText;
	Button resourceButton;

	public float amount;

	const float numberWidth = 30f;
	const float decimalWidth = 15f;

	public float totalWidth {
		get {
			if (amount > 0f)
				return amountText.rectTransform.localPosition.x + amountText.rectTransform.rect.width;
			else
				return 0f;
		}
	}

	void Awake () {
		amountText = gameObject.GetComponentInChildren<Text> ();
		resourceButton = gameObject.GetComponentInChildren<Button> ();
	}

	public void Init(ResourceInfo resourceInfo) {
		resourceButton.image.sprite = resourceInfo.sprite;
//		resourceButton.image.color = resourceInfo.colorLight;
		resourceButton.onClick.AddListener(() => Player.instance.DropResource(resourceInfo));
	}

	public void UpdateVisual() {
		if (amount > 0f) {
			float displayAmount = Mathf.Round (amount * 10f) / 10f;

			float textWidth = (float)(displayAmount.ToString ().Length * numberWidth);
			if(displayAmount.ToString().Contains("."))
				textWidth -= (numberWidth - decimalWidth);

			amountText.rectTransform.sizeDelta = new Vector2 (textWidth, amountText.rectTransform.rect.height);
			amountText.text = displayAmount.ToString ();

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