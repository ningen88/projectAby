using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bilboard : MonoBehaviour
{
    public Camera cam;

    private void LateUpdate()
    {
        transform.LookAt(transform.position + cam.transform.forward);
    }
}
