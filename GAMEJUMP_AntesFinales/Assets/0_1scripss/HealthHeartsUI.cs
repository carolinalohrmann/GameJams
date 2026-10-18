using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthHeartsUI : MonoBehaviour
{
    public player player;
    public Sprite heart;
    public RectTransform container;
    readonly List<Image> hearts = new List<Image>();
    void LateUpdate()
    {
        if (player == null || heart == null || container == null) return;
        while (hearts.Count < player.maxHealth)
        {
            var image = new GameObject("Corazon " + (hearts.Count + 1), typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(container, false);
            image.sprite = heart;
            image.preserveAspect = true;
            image.raycastTarget = false;
            hearts.Add(image);
        }
        float size = Mathf.Min(28, container.rect.width / Mathf.Max(1, player.maxHealth) - 3);
        for (int i = 0; i < hearts.Count; i++)
        {
            hearts[i].gameObject.SetActive(i < player.maxHealth);
            var rect = hearts[i].rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 0.5f);
            rect.anchoredPosition = new Vector2(i * (size + 3), 0);
            rect.sizeDelta = new Vector2(size, size);
            hearts[i].color = new Color(1, 1, 1, i < player.Health ? 1 : 0.18f);
        }
    }
}
