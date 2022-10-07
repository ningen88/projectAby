using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class damageEffect : MonoBehaviour
{    
    [SerializeField] ParticleSystem bloodEffect;

    private Entity entity;
    private int lastHealth;

    private void Start()
    {
        entity = gameObject.GetComponent<Entity>();
        lastHealth = entity.health;
    }

    void Update()
    {
        if(entity.health < lastHealth)
        {
            bloodEffect.Emit(1);
            lastHealth = entity.health;
        }
    }
}
