﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControls : MonoBehaviour
{
    //inputs
    public Controls controls;

    [SerializeField]
    Vector2 inputs;

    [HideInInspector]
    public Vector2 inputNormalized;
    [HideInInspector]
    public float rotation;
    bool run = true, jump;
    [HideInInspector]
    public bool steer, autoRun;
    public LayerMask groundMask;
    public MoveState moveState = MoveState.locomotion;
    public float smoothBlend = 0.1f;
    Animator anim;


    //velocity
    Vector3 velocity;
    float gravity = -18, velocityY, terminalVelocity = -25;
    float fallMult;

    //Running
    float currentSpeed;
    public float baseSpeed = 1, runSpeed = 4, rotateSpeed = 2;

    //ground
    Vector3 forwardDirection, collisionPoint;
    float slopeAngle, directionAngle, forwardAngle, strafeAngle;
    float forwardMult, strafeMult;
    Ray groundRay;
    RaycastHit groundHit;

    //Jumping
    [SerializeField]
    bool jumping;
    float jumpSpeed, jumpHeight = 3;
    Vector3 jumpDirection;

    //swimming
    float swimSpeed = 2, swimLevel = 1.25f;
    public float waterSurface, d_fromWaterSurface;
    public bool inWater;

    //Debug
    public bool showGroundRay, showMoveDirection, showForwardDirection, showStrafeDirection, showFallNormal, showSwimNormal;

    //references
    CharacterController controller;
    public Transform groundDirection, moveDirection, fallDirection, swimDirection;
    [HideInInspector]
    public CameraController mainCam;
    

    void Start()
    {
        controller = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        anim.SetBool("jump", jumping);// jumps

        if(Input.GetKeyDown(KeyCode.O)){
            anim.SetTrigger("1slash");
        }

        GetInputs();
        GetSwimDirection();

        if (inWater)
            GetWaterlevel();

        switch (moveState)
        {
            case MoveState.locomotion:
                anim.SetBool("swimming", false);
                anim.SetBool("treadWater", false);
                Locomotion();
                break;

            case MoveState.swimming:
            anim.SetBool("swimming", true);
                Swimming();
                break;
        }
    }


    void Locomotion()
    {
        GroundDirection();

        //running and walking
        if (controller.isGrounded && slopeAngle <= controller.slopeLimit)
        {
            currentSpeed = baseSpeed;

            if (run)
            {
                //xox
                //anim.SetTrigger("run");
                currentSpeed *= runSpeed;

                if (inputNormalized.y < 0)
                    currentSpeed = currentSpeed / 2;
            }
        }
        else if(!controller.isGrounded || slopeAngle > controller.slopeLimit)
        {
            inputNormalized = Vector2.Lerp(inputNormalized, Vector2.zero, 0.025f);
            currentSpeed = Mathf.Lerp(currentSpeed, 0, 0.025f);
        }

        //Rotating
        Vector3 characterRotation = transform.eulerAngles + new Vector3(0, rotation * rotateSpeed, 0);
        transform.eulerAngles = characterRotation;

        //Press space to Jump
        if (jump && controller.isGrounded && slopeAngle <= controller.slopeLimit && !jumping)
           
            Jump();

        //apply gravity if not grounded
        if (!controller.isGrounded && velocityY > terminalVelocity)
            velocityY += gravity * Time.deltaTime;
        else if (controller.isGrounded && slopeAngle > controller.slopeLimit)
            velocityY = Mathf.Lerp(velocityY, terminalVelocity, 0.25f);

        //checking WaterLevel
        if (inWater){       
           
            //setting ground ray
            groundRay.origin = transform.position + collisionPoint + Vector3.up * 0.05f;
            groundRay.direction = Vector3.down;

            //if (Physics.Raycast(groundRay, out groundHit, 0.15f))
            //    currentSpeed = Mathf.Lerp(currentSpeed, baseSpeed, d_fromWaterSurface / swimLevel);


            if (d_fromWaterSurface >= swimLevel) {
                
                

                if (jumping){
                    // anim.SetBool("treadWater",false);
                    //  anim.SetBool("jump",true);
                    jumping = false;
                    //anim.SetBool("jump", true);
                    anim.SetBool("treadWater",true);
                    
                   
                
            }
            // swimming anim here
                
                 moveState = MoveState.swimming;
                 anim.SetBool("jump",false); // jumping/ falling into water -> instantly into treadWater = good!
                 anim.SetBool("treadWater",true);
            }
        }

        //Applying inputs
        if (!jumping)
        {
            velocity = groundDirection.forward * inputNormalized.y * forwardMult + groundDirection.right * inputNormalized.x * strafeMult; //Applying movement direction inputs
            velocity *= currentSpeed; //Applying current move speed
            velocity += fallDirection.up * (velocityY * fallMult); //Gravity
        }
        else
            velocity = jumpDirection * jumpSpeed + Vector3.up * velocityY;

        //moving controller
        controller.Move(velocity * Time.deltaTime);

        if(controller.isGrounded)
        {
            //stop jumping if grounded
            if(jumping)
            anim.SetBool("jump", false); // don't remove. This Sets false on a ground jump :)
               
                jumping = false;

            // stop gravity if grounded
            velocityY = 0;
        }
    }

    void GroundDirection()
    {
        //SETTING FORWARDDIRECTION
        //Setting forwardDirection to controller position
        forwardDirection = transform.position;

        //Setting forwardDirection based on control input.
        if (inputNormalized.magnitude > 0)
            forwardDirection += transform.forward * inputNormalized.y + transform.right * inputNormalized.x;
        else
            forwardDirection += transform.forward;

        //Setting groundDirection to look in the forwardDirection normal
        moveDirection.LookAt(forwardDirection);
        fallDirection.rotation = transform.rotation;
        groundDirection.rotation = transform.rotation;

        //setting ground ray
        groundRay.origin = transform.position + collisionPoint + Vector3.up * 0.05f;
        groundRay.direction = Vector3.down;
        
        if(showGroundRay)
            Debug.DrawLine(groundRay.origin, groundRay.origin + Vector3.down * 0.3f, Color.red);

        forwardMult = 1;
        fallMult = 1;
        strafeMult = 1;

        if (Physics.Raycast(groundRay, out groundHit, 0.3f, groundMask))
        {
            //Getting angles
            slopeAngle = Vector3.Angle(transform.up, groundHit.normal);
            directionAngle = Vector3.Angle(moveDirection.forward, groundHit.normal) - 90;

            if (directionAngle < 0 && slopeAngle <= controller.slopeLimit)
            {
                forwardAngle = Vector3.Angle(transform.forward, groundHit.normal) - 90; //Chekcing the forwardAngle against the slope
                forwardMult = 1 / Mathf.Cos(forwardAngle * Mathf.Deg2Rad); //Applying the forward movement multiplier based on the forwardAngle
                groundDirection.eulerAngles += new Vector3(-forwardAngle, 0, 0); //Rotating groundDirection X

                strafeAngle = Vector3.Angle(groundDirection.right, groundHit.normal) - 90; //Checking the strafeAngle against the slope
                strafeMult = 1 / Mathf.Cos(strafeAngle * Mathf.Deg2Rad); //Applying the strafe movement multiplier based on the strafeAngle
                groundDirection.eulerAngles += new Vector3(0, 0, strafeAngle);
            }
            else if(slopeAngle > controller.slopeLimit)
            {
                float groundDistance = Vector3.Distance(groundRay.origin, groundHit.point);

                if(groundDistance <= 0.1f)
                {
                    fallMult = 1 / Mathf.Cos((90 - slopeAngle) * Mathf.Deg2Rad);

                    Vector3 groundCross = Vector3.Cross(groundHit.normal, Vector3.up);
                    fallDirection.rotation = Quaternion.FromToRotation(transform.up, Vector3.Cross(groundCross, groundHit.normal));
                }
            }
        }


        DebugGroundNormals();
    }

    void Jump(){
       
        //set Jumping to true
        if(!jumping){
            jumping = true;
             //anim.SetBool("jump", true); - Jumping on land
        } 


        switch(moveState)
        {
            
            case MoveState.locomotion:
                //Set jump direction and speed
                jumpDirection = (transform.forward * inputs.y + transform.right * inputs.x).normalized;
                jumpSpeed = currentSpeed;

                //set velocity Y
                velocityY = Mathf.Sqrt(-gravity * jumpHeight);

                break;

            case MoveState.swimming:
                
                //Set jump direction and speed
                jumpDirection = (transform.forward * inputs.y + transform.right * inputs.x).normalized;
                jumpSpeed = swimSpeed;

                //set velocity Y
                velocityY = Mathf.Sqrt(-gravity * jumpHeight * 1.25f);
                break;
        }
    }

    void GetInputs()
    {
        if (controls.autoRun.GetControlBindingDown())
            autoRun = !autoRun;

        //FORWARDS BACKWARDS CONTROLS  
        inputs.y = Axis(controls.forwards.GetControlBinding(), controls.backwards.GetControlBinding());
        anim.SetFloat("fowardsBackwards", inputs.y * currentSpeed, smoothBlend,  Time.deltaTime);

        anim.SetBool("bothMouseButtons", false);
        //if both mouse buttons pressed, get animator play run fw
        if(Input.GetMouseButton(0) && Input.GetMouseButton(1)){
            print("0 & 1 held down");
            anim.SetBool("bothMouseButtons", true);
        }

               

        if (inputs.y != 0 && !mainCam.autoRunReset){
            autoRun = false;
            
        }

        if(autoRun)
        {
            inputs.y += Axis(true, false);

            inputs.y = Mathf.Clamp(inputs.y, -1, 1);
        }

        //STRAFE LEFT RIGHT
        inputs.x = Axis(controls.strafeRight.GetControlBinding(), controls.strafeLeft.GetControlBinding());
        anim.SetBool("strafeRight", controls.strafeRight.GetControlBinding());
        anim.SetBool("strafeLeft", controls.strafeLeft.GetControlBinding());
        anim.SetFloat("leftRight", inputs.x * currentSpeed, smoothBlend,  Time.deltaTime);

        if(steer)
        {
            inputs.x += Axis(controls.rotateRight.GetControlBinding(), controls.rotateLeft.GetControlBinding());

            inputs.x = Mathf.Clamp(inputs.x, -1, 1);
        }

        //ROTATE LEFT RIGHT
        if (steer)
            rotation = Input.GetAxis("Mouse X") * mainCam.CameraSpeed;
        else
            rotation = Axis(controls.rotateRight.GetControlBinding(), controls.rotateLeft.GetControlBinding());

        //ToggleRun
        if (controls.walkRun.GetControlBindingDown())
            run = !run;

        //Jumping
        jump = controls.jump.GetControlBinding();

        inputNormalized = inputs.normalized;
    }

    void GetSwimDirection()
    {
        if (steer)
            swimDirection.eulerAngles = transform.eulerAngles + new Vector3(mainCam.tilt.eulerAngles.x, 0, 0);
    }

    void Swimming()
    {
        if(!inWater && !jumping)
        {
            
            velocityY = 0;
            velocity = new Vector3(velocity.x, velocityY, velocity.z);
            jumpDirection = velocity;
            jumpSpeed = swimSpeed / 2;
            jumping = true;
            moveState = MoveState.locomotion;

        }

        //Rotating
        Vector3 characterRotation = transform.eulerAngles + new Vector3(0, rotation * rotateSpeed, 0);
        transform.eulerAngles = characterRotation;

        //setting ground ray
        groundRay.origin = transform.position + collisionPoint + Vector3.up * 0.05f;
        groundRay.direction = Vector3.down;

        if (showGroundRay)
            Debug.DrawLine(groundRay.origin, groundRay.origin + Vector3.down * 0.15f, Color.red);

        if (!jumping && jump && d_fromWaterSurface <= swimLevel){
             anim.SetBool("treadWater", false); // Correntx2 - found it!
              anim.SetBool("swimming", false);
            Jump();
        }



        if (!jumping)
        {
            velocity = swimDirection.forward * inputNormalized.y + swimDirection.right * inputNormalized.x;

            velocity.y += Axis(jump, controls.sit.GetControlBinding());

            velocity.y = Mathf.Clamp(velocity.y, -1, 1);

            velocity *= swimSpeed;

            controller.Move(velocity * Time.deltaTime);

            if (Physics.Raycast(groundRay, out groundHit, 0.15f, groundMask)){
                if (d_fromWaterSurface < swimLevel){
                    moveState = MoveState.locomotion;
                   
                }
            }
            else
            {
                transform.position = new Vector3(transform.position.x, Mathf.Clamp(transform.position.y, float.MinValue, waterSurface - swimLevel), transform.position.z);
            }
        }
        else
        {
            //Jump
            if (velocityY > terminalVelocity)
                velocityY += gravity * Time.deltaTime;

            velocity = jumpDirection * jumpSpeed + Vector3.up * velocityY;

            controller.Move(velocity * Time.deltaTime);

            if (Physics.Raycast(groundRay, out groundHit, 0.15f, groundMask))
            {
                if (d_fromWaterSurface < swimLevel){
                    moveState = MoveState.locomotion;
                       
                
                    //anim.SetBool("swimming", true);
                }
            }

            if (d_fromWaterSurface >= swimLevel){
                jumping = false;
                          anim.SetBool("treadWater", true); // Jumping from water surface removes treadWater
                         anim.SetBool("jump", true); // Correct!
               // swimming
             }
                
        }
    }

    void GetWaterlevel()
    {
        d_fromWaterSurface = waterSurface - transform.position.y;
        //d_fromWaterSurface = Mathf.Clamp(d_fromWaterSurface, 0, float.MaxValue);
    }

    public float Axis(bool pos, bool neg)
    {
        float axis = 0;

        if (pos)
            axis += 1;

        if (neg)
            axis -= 1;

        return axis;
    }

    void DebugGroundNormals()
    {
        Vector3 lineStart = transform.position + Vector3.up * 0.05f;

        if (showMoveDirection)
            Debug.DrawLine(lineStart, lineStart + moveDirection.forward, Color.cyan);

        if (showForwardDirection)
            Debug.DrawLine(lineStart - groundDirection.forward * 0.5f, lineStart + groundDirection.forward * 0.5f, Color.blue);

        if (showStrafeDirection)
            Debug.DrawLine(lineStart - groundDirection.right * 0.5f, lineStart + groundDirection.right * 0.5f, Color.red);

        if (showFallNormal)
            Debug.DrawLine(lineStart, lineStart + fallDirection.up * 0.5f, Color.green);

        if (showSwimNormal)
            Debug.DrawLine(lineStart, lineStart + swimDirection.forward, Color.magenta);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if(hit.point.y <= transform.position.y + 0.25f)
        {
            collisionPoint = hit.point;
            collisionPoint = collisionPoint - transform.position;
        }
    }

    public enum MoveState { locomotion, swimming }
}
