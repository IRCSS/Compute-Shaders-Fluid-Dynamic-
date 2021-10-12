using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public struct SimulationDomainIndicator
{
    public Transform leftBottom, rightBottom, rightUp, leftUp;

    public SimulationDomainIndicator(Transform _leftBottom, Transform _rightBottom, Transform _rightUp, Transform _leftUp)
    {
        leftBottom  = _leftBottom  ;
        rightBottom = _rightBottom ;
        rightUp     = _rightUp     ;
        leftUp      = _leftUp      ;
    }

    public SimulationDomainIndicator(SimulationDomainIndicator other)
    {
        leftBottom  = other.leftBottom ;
        rightBottom = other.rightBottom;
        rightUp     = other.rightUp    ;
        leftUp      = other.leftUp     ;
    }

}

public class VastLandDemoMaster : MonoBehaviour
{

    // ---------------------------------------------
    // PUBLIC

    public FluidSimulater fluid_simulater;

    public int                       fogStackDepth = 1;
    public SimulationDomainIndicator corners      ;

    // ---------------------------------------------
    // PRIVATE

    private FluidGPUResources resources;
    private Material[]        fogRenderStackMats;
    private Camera            main_cam;

    // ------------------------------------------------------------------
    // INITALISATION
    void Start()
    {


        main_cam = Camera.main;
        if (main_cam == null) Debug.LogError("Could not find main camera, make sure the camera is tagged as main");

        //--------------------------------------------
        fluid_simulater.Initialize();
        resources = new FluidGPUResources(fluid_simulater);
        resources.Create();

        //--------------------------------------------
        // Generate fog Mesh

        Mesh fogMeshBase = new Mesh();
        GeneratePlaneMesh(ref fogMeshBase);

        fogRenderStackMats = new Material[fogStackDepth];

        Shader s = Shader.Find("Unlit/VastlandFog");
        if (!s) { Debug.LogError("Couldnt find the Vastland Fog shader!"); return; }

        for (int i = 0; i < fogStackDepth; i++)
        {
            fogRenderStackMats[i] = new Material(s);
            fogRenderStackMats[i].SetFloat("StackDepth", (float)i / (float)fogStackDepth);

            GameObject gb = new GameObject("FogStack_" + i.ToString());
            MeshRenderer mr = gb.AddComponent<MeshRenderer>();
            mr.sharedMaterial = fogRenderStackMats[i];
            gb.transform.position = new Vector3(0.0f, (float)i * 0.025f, 0.0f);
            gb.AddComponent<MeshFilter>().sharedMesh = fogMeshBase;
        }


        //--------------------------------------------


        fluid_simulater.AddUserForce           (resources.velocity_buffer                                   );
        fluid_simulater.HandleCornerBoundaries (resources.velocity_buffer, FieldType.Velocity               );
        fluid_simulater.Diffuse                (resources.velocity_buffer                                   );
        fluid_simulater.HandleCornerBoundaries (resources.velocity_buffer, FieldType.Velocity               );
        fluid_simulater.Project                (resources.velocity_buffer, resources.divergence_buffer, resources.pressure_buffer);
        fluid_simulater.Advect                 (resources.velocity_buffer, resources.velocity_buffer, 0.999f);
        fluid_simulater.HandleCornerBoundaries (resources.velocity_buffer, FieldType.Velocity               );
        fluid_simulater.Project                (resources.velocity_buffer, resources.divergence_buffer, resources.pressure_buffer);



        fluid_simulater.AddDye                 (resources.dye_buffer                                        );
        fluid_simulater.Advect                 (resources.dye_buffer, resources.velocity_buffer, 0.992f);
        fluid_simulater.HandleCornerBoundaries (resources.dye_buffer, FieldType.Dye                         );
        fluid_simulater.Diffuse                (resources.dye_buffer                                        );
        fluid_simulater.HandleCornerBoundaries (resources.dye_buffer, FieldType.Dye                         );

        fluid_simulater.Visualiuse             (resources.dye_buffer);

        fluid_simulater.BindCommandBuffer();

    }


    // ------------------------------------------------------------------
    // DESTRUCTOR
    void OnDisable()
    {
        fluid_simulater.Release();
        resources.Release();
    }

    // ------------------------------------------------------------------
    // LOOP
    void Update()
    {
        fluid_simulater.Tick(Time.deltaTime);
    }



    // ---------------------------------------------
    // HELPER FUNCTIONS

    //    3---------2
    //    | .       |
    //    |   .     |
    //    |     .   |
    //    |       . |
    //    0---------1

    void GeneratePlaneMesh(ref Mesh toPopulate)
    {
        toPopulate.vertices  = new Vector3[] { corners.leftBottom.position, corners.rightBottom.position, corners.rightUp.position, corners.leftUp.position };
        toPopulate.triangles = new int[] { 0, 3, 1,    // First  Triangle
                                           1, 3, 2 };  // Second Triangle
        toPopulate.uv        = new Vector2[] { new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 1.0f) };

    }
}

