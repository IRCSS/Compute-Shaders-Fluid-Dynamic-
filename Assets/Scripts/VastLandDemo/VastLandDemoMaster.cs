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

    public RenderTexture             obstcleMap;

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
        // -------------------------------------------
        obstcleMap = new RenderTexture((int)fluid_simulater.simulation_dimension, (int)fluid_simulater.simulation_dimension, 8)
        {
            graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16_SFloat,
            format = RenderTextureFormat.RFloat
        };
        obstcleMap.Create();


        ConstructObstcleMap(ref obstcleMap);


        fluid_simulater.UpdateArbitaryBoundaryOffsets(obstcleMap, resources);
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
        fluid_simulater.HandleArbitaryBoundary(resources.velocity_buffer, resources.boundary_velocity_offset_buffer, FieldType.Velocity);
        fluid_simulater.Diffuse                (resources.velocity_buffer                                   );
        fluid_simulater.HandleCornerBoundaries (resources.velocity_buffer, FieldType.Velocity               );
        fluid_simulater.HandleArbitaryBoundary(resources.velocity_buffer, resources.boundary_velocity_offset_buffer, FieldType.Velocity);
        fluid_simulater.Project                (resources.velocity_buffer, resources.divergence_buffer, resources.pressure_buffer);
        fluid_simulater.Advect                 (resources.velocity_buffer, resources.velocity_buffer, 0.999f);
        fluid_simulater.HandleCornerBoundaries (resources.velocity_buffer, FieldType.Velocity               );
        fluid_simulater.HandleArbitaryBoundary(resources.velocity_buffer, resources.boundary_velocity_offset_buffer, FieldType.Velocity);
        fluid_simulater.Project                (resources.velocity_buffer, resources.divergence_buffer, resources.pressure_buffer);



        fluid_simulater.AddDye                 (resources.dye_buffer                                        );
        fluid_simulater.Advect                 (resources.dye_buffer, resources.velocity_buffer, 0.992f);
        fluid_simulater.HandleCornerBoundaries (resources.dye_buffer, FieldType.Dye                         );
        fluid_simulater.HandleArbitaryBoundary(resources.dye_buffer, resources.boundary_dye_offset_buffer, FieldType.Dye);
        fluid_simulater.Diffuse                (resources.dye_buffer                                        );
        fluid_simulater.HandleCornerBoundaries (resources.dye_buffer, FieldType.Dye                         );
        fluid_simulater.HandleArbitaryBoundary(resources.dye_buffer, resources.boundary_dye_offset_buffer, FieldType.Dye);

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


    Matrix4x4 ConstructTopDownOrthoCameraMatrix(SimulationDomainIndicator domainIndicator, Vector3 globalMin, Vector3 globalMax)
    {
        Vector3 halfPoint = domainIndicator.leftBottom.position + (domainIndicator.rightUp.position - domainIndicator.leftBottom.position) * 0.5f; // find mid point (A+B)/2
        Vector3 offset    = Vector3.Cross( (domainIndicator.leftUp.position - domainIndicator.leftBottom.position).normalized, (domainIndicator.rightBottom.position - domainIndicator.leftBottom.position).normalized);

        Vector3 cameraPos = halfPoint + offset * 10.0f;
        Vector3 forward   = -offset;
        Vector3 right     = (domainIndicator.rightBottom.position - domainIndicator.leftBottom.position).normalized;
        Vector3 up        = Vector3.Cross(forward, right);

        
        Matrix4x4 cameraToWorld = new Matrix4x4(forward, up, right, new Vector4(cameraPos.x, cameraPos.y, cameraPos.z, 1.0f));
        Matrix4x4 worldToCamera = cameraToWorld.inverse;
        
        // ------
        float scale_xy = (domainIndicator.rightBottom.position - domainIndicator.leftBottom.position).magnitude *0.5f;
        

        Vector3 minCamSpace = worldToCamera * new Vector4(globalMin.x, globalMin.y, globalMin.z, 1.0f);
        Vector3 maxCamSpace = worldToCamera * new Vector4(globalMax.x, globalMax.y, globalMax.z, 1.0f);
        
        float midPoinZ = Mathf.Abs(maxCamSpace.x + minCamSpace.x) * 0.5f;          // after the transformation the mesh is always on the positive side, since the camera is placed that way, however it can be that the min and max switch places, if the camera is rotated 
        float scaleZ   = Mathf.Abs(maxCamSpace.x - minCamSpace.x) * 0.5f;



        Matrix4x4 permutationMatrix = new Matrix4x4(new Vector4(    0.0f,    0.0f,    1.0f, 0.0f),
                                                    new Vector4(    0.0f,    1.0f,    0.0f, 0.0f),
                                                    new Vector4(    1.0f,    0.0f,    0.0f, 0.0f),
                                                    new Vector4(    0.0f,    0.0f,    0.0f, 1.0f));



        Matrix4x4 orthoProjection = new Matrix4x4(new Vector4(1.0f / scale_xy,            0.0f,          0.0f, 0.0f),
                                                  new Vector4(           0.0f, 1.0f / scale_xy,          0.0f, 0.0f),
                                                  new Vector4(           0.0f,            0.0f,  1.0f/ scaleZ, 0.0f),
                                                  new Vector4(           0.0f,            0.0f, -midPoinZ/ scaleZ, 1.0f));
        
        return orthoProjection * permutationMatrix * worldToCamera;
    }

    void ConstructObstcleMap(ref RenderTexture target)
    {
        if (!target)
        {
            Debug.LogError("ERROR: The function Construct Obstcle Map requires an already allocated render target");
            return;
        }

        if (!target.IsCreated()) target.Create();

        Material clearBufferMat = new Material(Shader.Find("Unlit/ClearBuffer"));

        Graphics.Blit(null, target, clearBufferMat, 0);


        GameObject[] toRender = GameObject.FindGameObjectsWithTag("FluidObstcles");


        Vector3 globalMin = new Vector3( 10000.0f,  1000.0f,  10000.0f);
        Vector3 globalMax = new Vector3(-10000.0f, -1000.0f, -10000.0f);

        // Find min max in world space of all objects
        foreach (GameObject gb in toRender)
        {
            MeshFilter mf = gb.GetComponent<MeshFilter>();
            if (!mf)
            {
                Debug.LogWarning("WARNING: A mesh was taged as fluid obstcle which doesnt have a mesh filter");
                continue;
            }

            Mesh meshToRender = mf.sharedMesh;
            if (!meshToRender)
            {
                Debug.LogWarning("WARNING: A mesh was taged as fluid obstcle which doesnt have a mesh");
                continue;
            }

            Vector3 max = gb.transform.TransformPoint(meshToRender.bounds.max);
            Vector3 min = gb.transform.TransformPoint(meshToRender.bounds.min);

            globalMin = new Vector3(min.x < globalMin.x ? min.x : globalMin.x,
                                    min.y < globalMin.y ? min.y : globalMin.y,
                                    min.z < globalMin.z ? min.z : globalMin.z);

            globalMax = new Vector3(max.x > globalMax.x ? max.x : globalMax.x,
                                    max.y > globalMax.y ? max.y : globalMax.y,
                                    max.z > globalMax.z ? max.z : globalMax.z);

        }


        Matrix4x4 cameraMatrix = ConstructTopDownOrthoCameraMatrix(corners, globalMin, globalMax);
        

        Material constructObstcleDepth = new Material(Shader.Find("Unlit/ObstclesDepthMap"));
        Material constructObstcleMask = new Material(Shader.Find("Unlit/ConstructObstcleMap"));

        Graphics.SetRenderTarget(target);

        constructObstcleDepth.SetPass(0);
        foreach (GameObject gb in toRender)
        {
            MeshFilter mf = gb.GetComponent<MeshFilter>();
            Mesh meshToRender = mf.sharedMesh;

            Matrix4x4 MVP = cameraMatrix * gb.transform.localToWorldMatrix;
        
            Shader.SetGlobalMatrix("Obstcle_MVP", MVP);


            Graphics.DrawMeshNow(meshToRender, Matrix4x4.identity, 0);
        }
        Graphics.SetRenderTarget(null);

        RenderTexture temp = RenderTexture.GetTemporary( target.width, target.height);
        temp.Create();

        Vector3 halfPoint = corners.leftBottom.position + (corners.rightUp.position - corners.leftBottom.position) * 0.5f; // find mid point (A+B)/2
        halfPoint = cameraMatrix * new Vector4(halfPoint.x, halfPoint.y, halfPoint.z, 1.0f);

        Graphics.Blit(target, temp);

        constructObstcleMask.SetTexture("_ObstcleDepthMap", temp);
        constructObstcleMask.SetFloat  ("_simulationDepth", halfPoint.z);
        Graphics.Blit(temp, target, constructObstcleMask);


        temp.Release();

    }



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

    void GenerateDebugClipSpacePlane(ref Mesh toPopulate)
    {
        toPopulate.vertices = new Vector3[] { new Vector3(-1.0f, -1.0f, 0.5f), new Vector3(1.0f, -1.0f, 0.5f), new Vector3(1.0f, 1.0f, 0.5f), new Vector3(-1.0f, 1.0f, 0.5f)};
        toPopulate.triangles = new int[] { 0, 3, 1,    // First  Triangle
                                           1, 3, 2 };  // Second Triangle
    }
}

