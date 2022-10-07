using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShootingEffects : MonoBehaviour
{ 
    [SerializeField] ParticleSystem[] muzzleFlash;
    [SerializeField] GameObject firePoint;
    [SerializeField] TrailRenderer bulletEffect;

    private TrailRenderer tracer;

    public void StartShooting(Vector3 endPos)
    {       
        foreach(var flash in muzzleFlash)
        {
            flash.Emit(1);
        }

        bulletEffect.emitting = true;
        tracer = Instantiate(bulletEffect, firePoint.transform.position, Quaternion.identity);
        tracer.AddPosition(firePoint.transform.position);
        tracer.transform.position = endPos;
    }

    public void StopShooting()
    {
        bulletEffect.emitting = false;
        tracer.autodestruct = true;
    }
}
