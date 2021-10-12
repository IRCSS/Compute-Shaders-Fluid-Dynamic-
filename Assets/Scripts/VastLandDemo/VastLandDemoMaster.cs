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

    public Color                     fogColor;
    public int                       fogStackDepth = 1;
    public float                     distanceBetwenPlanes= 0.015f;
    public SimulationDomainIndicator corners      ;

    // ---------------------------------------------
    // PRIVATE

    private FluidGPUResources resources;
    private Material[]        fogRenderStackMats;
    private Camera            main_cam;
    private RenderTexture     fogBuffer;
    private GameObject        fogCollider;


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
        fluid_simulater.SubmitMousePosOverrideDelegate(GetMousePosInSimulationSpaceUnitValue);

        fogBuffer = new RenderTexture((int) fluid_simulater.canvas_dimension, (int)fluid_simulater.canvas_dimension, 0)
        {
            enableRandomWrite = true,
            useMipMap         = true,
            graphicsFormat    = UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat,
            filterMode        = FilterMode.Trilinear,
            anisoLevel        = 7,
            format            = RenderTextureFormat.RFloat,
            wrapMode          = TextureWrapMode.Clamp,
        };

        fogBuffer.Create();
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
            gb.transform.position = new Vector3(0.0f, (float)i * distanceBetwenPlanes, 0.0f);
            gb.AddComponent<MeshFilter>().sharedMesh = fogMeshBase;

            if (i != 0) continue;
            fogCollider = gb;
            fogCollider.AddComponent<MeshCollider>().sharedMesh = fogMeshBase;

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

        fluid_simulater.CopyPressureBufferToTexture (fogBuffer, resources.dye_buffer);
        Shader.SetGlobalTexture("_fogBuffer", fogBuffer);
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
        Shader.SetGlobalColor("_fogColor", fogColor);
    }



    // ---------------------------------------------
    // HELPER FUNCTIONS





    Vector2 GetMousePosInSimulationSpaceUnitValue(ref bool isInBound)
    {
        RaycastHit results;
        Ray ray = main_cam.ScreenPointToRay(Input.mousePosition);

        isInBound = false;
        if (Physics.Raycast(ray, out results, 100.0f))
        {

            if (results.collider.gameObject != fogCollider) return new Vector2(-20.0f, -20.0f);

            Vector2 hitPositionInSimulationSpace = new Vector2(results.point.z, results.point.x);
                    hitPositionInSimulationSpace = hitPositionInSimulationSpace - new Vector2(corners.leftBottom.position.z, corners.leftBottom.position.x);

            Vector2 span = new Vector2(corners.rightUp.position.z, corners.rightUp.position.x) - new Vector2(corners.leftBottom.position.z, corners.leftBottom.position.x);

             hitPositionInSimulationSpace = new Vector2(hitPositionInSimulationSpace.x / Mathf.Abs(span.x), hitPositionInSimulationSpace.y / Mathf.Abs(span.y));
            print(hitPositionInSimulationSpace);
            isInBound = true;
            return hitPositionInSimulationSpace;

        }

        return new Vector2(-20.0f, -20.0f);
    }

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
        toPopulate.uv        = new Vector2[] { new Vector2(0.0f, 0.0f), new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(1.0f, 0.0f) };

    }
}

