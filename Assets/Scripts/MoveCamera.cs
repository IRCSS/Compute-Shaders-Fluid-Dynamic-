using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Moves Camera similar to how camera is moved in the Unity view port. Drop the scrip on the game object which has the camera and you wish to move. 

public class MoveCamera : MonoBehaviour
{
    public float lateralSpeed = 0.0015f;
    public float LongitudinalSpeed = 0.0008f;
    public float verticalSpeed = 0.001f;
    public float mouseMoevementSpeed = 0.2f;
    public float fovChangeSpeed = 0.0008f;
    public float ShiftMultiplyer = 4f;
    public bool InvertedY = false;

    // ------------------------------------------------------

    // these need to be > 0 and <= 1. closer to 0 is smoother movement 
    public float verticalFilter = 0.003f;
    public float lateralFilter = 0.005f;
    public float LongitudinalFilter = 0.005f;
    public float mouseFilter = 0.04f;
    public float fovFilter = 0.0001f;

    // ------------------------------------------------------

    private float vertical;
    private float lateral;
    private float longitudinal;
    private float speedMultiplyer = 1f;
    private float inversion = -1f;
    private float fovGain;


    private GameObject TransformDummy;
    private Camera mainCam;


    void Start()
    {
        if (InvertedY) inversion = 1f;
        else inversion = -1f;

        if (TransformDummy == null) TransformDummy = new GameObject("TransformDummy");
        TransformDummy.transform.rotation = this.transform.rotation;


        mainCam = this.GetComponent<Camera>();
        if (mainCam == null) mainCam = this.transform.GetComponentInChildren<Camera>();
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) Debug.LogWarning("the Class Move Camera could not find any camera.");

    }

    void Update()
    {

        // -------------------------------------------------------------------------------------------------------


        // ========================================
        // Speed Multiplyer
        speedMultiplyer = Mathf.Lerp(speedMultiplyer, 1f, 0.1f);
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.Mouse1))
        {
            speedMultiplyer += 0.2f * timeStep();
            speedMultiplyer = Mathf.Clamp(speedMultiplyer, 1f, ShiftMultiplyer);
        }



        // ========================================
        // Left Right Movement

        lateral = Mathf.Lerp(lateral, 0f, lateralFilter * timeStep());
        lateral = Mathf.Clamp(lateral, -1f, 1f);

        float x = Input.GetAxis("Horizontal");

        if (Input.GetKey(KeyCode.Mouse1)) lateral += x * lateralSpeed * timeStep();

        this.transform.position += this.transform.right * lateral * speedMultiplyer * timeStep();

        // ========================================
        // Foward Backward Movement

        longitudinal = Mathf.Lerp(longitudinal, 0f, LongitudinalFilter * timeStep());
        longitudinal = Mathf.Clamp(longitudinal, -1f, 1f);

        float y = Input.GetAxis("Vertical");

        if (Input.GetKey(KeyCode.Mouse1)) longitudinal += y * LongitudinalSpeed * timeStep();
        this.transform.position += this.transform.forward * longitudinal * speedMultiplyer;

        // ========================================
        // Up Down Movement

        vertical = Mathf.Lerp(vertical, 0f, verticalFilter);

        if (Input.GetKey(KeyCode.E) && Input.GetKey(KeyCode.Mouse1)) vertical += verticalSpeed * timeStep(); // UP 
        if (Input.GetKey(KeyCode.Q) && Input.GetKey(KeyCode.Mouse1)) vertical -= verticalSpeed * timeStep(); // DOWN 


        this.transform.position += this.transform.up * vertical * speedMultiplyer * timeStep();

        // ========================================
        // Mouse Movement X

        float mouseX = Input.GetAxis("Mouse X");
        if (Input.GetKey(KeyCode.Mouse1)) TransformDummy.transform.Rotate(Vector3.up * mouseX * mouseMoevementSpeed * timeStep(), Space.World);


        // ========================================
        // Mouse Movement Y

        float mouseY = Input.GetAxis("Mouse Y");
        if (Input.GetKey(KeyCode.Mouse1)) TransformDummy.transform.Rotate(TransformDummy.transform.right * -1f * mouseY * mouseMoevementSpeed * timeStep(), Space.World);


        this.transform.rotation = Quaternion.Slerp(this.transform.rotation, TransformDummy.transform.rotation, mouseFilter * timeStep());


        if (mainCam == null) return;

        // FOV
        fovGain = Mathf.Lerp(fovGain, 0f, fovFilter * timeStep());
        fovGain = Mathf.Clamp(fovGain, -1f, 1f);

        float mScrollDelta = Input.mouseScrollDelta.y * -1f;
        if (Input.GetKey(KeyCode.Mouse1)) fovGain += mScrollDelta * fovChangeSpeed * timeStep();
        mainCam.fieldOfView += fovGain;

    }

    float timeStep()
    {
        return Time.deltaTime / 0.0066f;
    }

}