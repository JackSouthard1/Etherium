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

	public void Init(TerrainManager.ResourceInfo resourceInfo) {
		resourceButton.image.sprite = resourceInfo.sprite;
		resourceButton.image.color = resourceInfo.color;
		resourceButton.onClick.AddListener(() => GameObject.FindObjectOfType<Player>().DropResource(resourceInfo));
	}

	public void UpdateVisual() {
		if (amount > 0f) {
			//int displayAmount = Mathf.FloorToInt (amount);

			float textWidth = (float)(amount.ToString ().Length * numberWidth);
			if(amount.ToString().Contains("."))
				textWidth -= (numberWidth - decimalWidth);

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