using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fish : MonoBehaviour
{
    float seed;
    Rigidbody rg;
    float current;

    Material mat;
    Material shadow_depth_mat;
    float period;
    float materialTime = 0;

    Camera main_cam;


    PersianGardenDemoSceneMaster ref_demoMaster;

    // Start is called before the first frame update
    void Start()
    {
        seed = Random.value * 1000;
        rg = GetComponent<Rigidbody>();


        if(!rg) Debug.LogError("Couldnt find a rigid body on fish object");
        Renderer r = transform.GetChild(0).GetComponent<Renderer>();

        if (!r) Debug.LogError("couldnt find a renderer");

        mat = r.material;
        period = Random.Range(1.0f, 5.0f);

        main_cam = Camera.main;

        ref_demoMaster = FindObjectOfType<PersianGardenDemoSceneMaster>();
         
    }

    public Renderer GetRenderer()
    {
        Renderer r = transform.GetChild(0).GetComponent<Renderer>();

        if (!r) Debug.LogError("couldnt find a renderer");
        return r;
    }

    public Material GetFishDepthMat()
    {
        if (!shadow_depth_mat) shadow_depth_mat = new Material(Shader.Find("Unlit/FishShadowDepthShader"));

        if (!shadow_depth_mat) Debug.LogError("couldnt construct a material for drawing the depth of the fish to the light depth map");

        return shadow_depth_mat;
    }
    float smoothstep(float a, float b, float x)
    {
        float t = Mathf.Clamp01((x - a) / (b - a));
        return t * t * (3.0f - (2.0f * t));
    }


    // Update is called once per frame
    void Update()
    {


        RaycastHit results;
        Ray ray = main_cam.ScreenPointToRay(Input.mousePosition);

        float shouldFlee = 0.0f;

        Vector3 fleeDirection = Vector3.zero;

        if (Physics.Raycast(ray, out results, 100.0f))
        {

            fleeDirection = this.transform.position - results.point;
            fleeDirection.y = 0.0f;
            float distanceToHand = fleeDirection.magnitude;



            shouldFlee = smoothstep(0.45f, 0.02f, distanceToHand)  *(Input.GetKey(ref_demoMaster.fluid_simulater.ApplyForceKey) ? 1.0f : 0.0f);



        }

        

        Vector3 perlinCoordinate = new Vector3(this.transform.position.x*2.0f, this.transform.position.z*2.0f, Time.time*0.15f  + seed);
        
        Vector3 forceD = new Vector3(Perlin.Fbm(perlinCoordinate, 5), 0, Perlin.Fbm(perlinCoordinate + new Vector3(5.0f, 10.0f, 20.0f), 5));

        Vector3 disToCenter= new Vector3(this.transform.position.x, 0.0f, this.transform.position.z);
          

        Vector3 round = Vector3.Cross(disToCenter, Vector3.up);

        forceD =Vector3.Lerp(forceD, -disToCenter + round, smoothstep(0.7f, 1.25f,  disToCenter.magnitude)) /* round * disToCenter.magnitude + */  /** disToCenter.magnitude*/;

        forceD += fleeDirection * shouldFlee;

        float aligness = Vector3.Dot(forceD, this.transform.forward) * 0.5f + 0.5f;
         

        forceD = Vector3.Lerp(this.transform.forward, forceD, Mathf.Lerp(0.001f, 1.0f, aligness));



        float stopper = Mathf.Sin(Time.time * period + seed * 0.006f) + Mathf.Sin(Time.time  + seed * 0.006f+1.521f);
              stopper = smoothstep(-1.0f, 1.0f, stopper);

        if (rg.velocity.magnitude <3.0f + shouldFlee*7.0f)
        rg.AddForce(forceD.normalized * 1.0f * (stopper + shouldFlee*4.0f));

        this.transform.forward = Vector3.Lerp(this.transform.forward, rg.velocity, 0.4f);

        float movementSpeed = Mathf.Clamp01((rg.velocity.magnitude * 5.0f + rg.angularVelocity.magnitude) / 5.0f);
        materialTime += Mathf.Lerp(0.0f, 0.4f, movementSpeed);

        //materialTime = materialTime % 10000.0f;

        mat.SetFloat("_FishTime",      materialTime);
        mat.SetFloat("_movementSpeed", movementSpeed);

        shadow_depth_mat.SetFloat("_FishTime",      materialTime);
        shadow_depth_mat.SetFloat("_movementSpeed", movementSpeed);

        


    }

    private void OnDestroy()
    {
        Destroy(mat);
        Destroy(shadow_depth_mat);
    }


}
