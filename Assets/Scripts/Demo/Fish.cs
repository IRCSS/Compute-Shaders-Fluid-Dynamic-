using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fish : MonoBehaviour
{
    float seed;
    Rigidbody rg;
    float current;
    // Start is called before the first frame update
    void Start()
    {
        seed = Random.value * 1000;
        rg = GetComponent<Rigidbody>();

        if(!rg) Debug.LogError("Couldnt find a rigid body on fish object");
    }

    // Update is called once per frame
    void Update()
    {

        Vector3 perlinCoordinate = new Vector3(this.transform.position.x*1.0f, this.transform.position.z*1.0f, Time.time*0.3f  + seed);
        
        Vector3 forceD = new Vector3(Perlin.Fbm(perlinCoordinate, 5), 0, Perlin.Fbm(perlinCoordinate + new Vector3(5.0f, 10.0f, 20.0f), 5));


        if (rg.velocity.magnitude <2.0f)
        rg.AddForce(forceD.normalized * 0.5f);

        this.transform.forward = Vector3.Lerp(this.transform.forward,  rg.velocity, 0.5f);

    }
}
