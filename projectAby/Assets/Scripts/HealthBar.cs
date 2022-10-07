using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public GameObject fill;
    public GameObject guard;

    private float fillWidth;
    private const float healthBarMaxLen = 3.3f;
    private const float maxHealth = 100;
    private Entity entity;

    private void Start()
    {
        fillWidth = fill.GetComponent<Image>().sprite.bounds.size.x;
        entity = gameObject.GetComponent<Entity>();
    }

    private void Update()
    {
        UpdateBar();
    }

    private void UpdateBar()
    {
        if (entity.guard.Item1)
        {
            guard.SetActive(true);
        }
        else guard.SetActive(false);

        float php = entity.health / maxHealth;
        float healthFillLen = healthBarMaxLen * php;
        float pixelXSprite = 1 / fillWidth;
        float scale = pixelXSprite * healthFillLen;
        fill.GetComponent<RectTransform>().sizeDelta = new Vector2(scale * fillWidth, 0.2f);
    }
}
