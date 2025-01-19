using UnityEngine;
using Oculus.Interaction;
using UnityEngine.Splines;
using Unity.Mathematics;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using Unity.VisualScripting;
using System.Diagnostics.Tracing;
public class Cart : MonoBehaviour, ITransformer
{
    protected HingeJoint joint;
    protected Grabbable grabbable;
    protected SplineContainer splineContainer;
    protected UnityEngine.Vector3 trainPos;
    protected ArrayList splines;
    protected Spline[] splinesArray;
    protected List<HingeJoint> joints = new List<HingeJoint>();
    protected Transform[] allChildren;
    protected Transform rodTransform, handTransform;
    public bool isTrain = false;
    public Cart next = null, prev = null;
    public bool isEnd = false;
    private int count;
    public String id;
    String winConCheck = "";
    Transform startTransform;
    public AudioSource attachSound, detachSound, winSound;
    // Start is called before the first frame update
    void Start()
    {
        GetSpline();
        FindAttachTransforms();
        startTransform = gameObject.transform;
    }

    public void OnTriggerEnter(Collider collider)
    {
        //Debug.Log("Trigger entered");
        if (collider.name.Equals("HandTransform") && next == null)
        {

            Cart connectedBody = collider.gameObject.GetComponentInParent<Cart>();
            CreateJoint(connectedBody.GetComponent<Rigidbody>());
            attachSound.Play(0);
            next = connectedBody;
            connectedBody.prev = gameObject.GetComponent<Cart>();
        }
        //Cart otherComponent = collision.gameObject.GetComponentInParent<Cart>();
        /* Transform otherTransform = otherComponent.transform;                //här börjar wack
        float distToHand = Vector3.Distance(handTransform.position, otherTransform.position);
        float distToRod = Vector3.Distance(rodTransform.position, otherTransform.position);
        if (distToHand > distToRod)
        {
            Debug.Log("Det borde vara next!");
        }
        else
        {
            Debug.Log("Det borde vara prev!");
        }  */                                                                  //här slutar wack
        /* Debug.Log("othercomp is: " + otherComponent);
        if (otherComponent != null)
        {
            Debug.Log("if sats för joints nådd");
            CreateJoint(otherComponent.GetComponent<Rigidbody>());
        }
        else
        {
            Debug.Log("if sats ej nådd. isconn: " + IsConnected());
        } */
    }

    protected void LateUpdate()
    {
        if (isEnd == false)
        {
            SnapCartToTrack();
        }
    }

    public void FixedUpdate()
    {
        gameObject.GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
        if (isTrain)
        {
            if (next != null)
            {
                winConCheck = "";
                Cart iterator = next;
                count = 1;
                winConCheck = winConCheck + iterator.id;
                while (iterator.next != null)
                {
                    iterator = iterator.next;
                    count++;
                    winConCheck = winConCheck + iterator.id;
                    if (winConCheck.Contains("12345"))
                    {
                        winSound.Play(0);
                    }
                }
            }
            else
            {
                winConCheck = "";
                count = 0;
            }
            Debug.Log(winConCheck);
        }
    }

    new public void SnapCartToTrack()
    {
        trainPos = gameObject.transform.position;
        Vector3 nearestWorldPosition;
        Vector3 splineTrainPos = splineContainer.transform.InverseTransformPoint(trainPos);
        Vector3 currNearestV3 = new Vector3(0, 0, 0);
        Vector3 currNearestRotation = new Vector3(0, 0, 0); //trying out rotation
        float currNearestDist = 0;
        bool hasLooped = false;

        foreach (Spline s in splinesArray)
        {
            SplineUtility.GetNearestPoint<Spline>(s, splineTrainPos, out float3 nearestPoint, out float t);
            float3 tangent = SplineUtility.EvaluateTangent<Spline>(s, t); //trying out rotation
            float dist = Vector3.Distance(nearestPoint, splineTrainPos);
            if (!hasLooped || dist < currNearestDist)
            {
                currNearestV3 = nearestPoint;
                currNearestRotation = tangent; //trying out rotation
                currNearestDist = dist;
                hasLooped = true;

            }
        }
        nearestWorldPosition = splineContainer.transform.TransformPoint(currNearestV3);
        nearestWorldPosition = new Vector3(nearestWorldPosition.x, nearestWorldPosition.y + 0.05f, nearestWorldPosition.z);
        Quaternion rot = Quaternion.LookRotation(currNearestRotation, Vector3.up); //trying out rotation
        gameObject.transform.rotation = rot; //trying out rotation
        //Debug.Log(rot.ToString());
        gameObject.transform.position = nearestWorldPosition;
    }

    public void CreateJoint(Rigidbody connectedBody)
    {
        HingeJoint newJoint = gameObject.AddComponent<HingeJoint>();
        Vector3 newAnchor = new Vector3(0, 0, 0.5228f);
        newJoint.anchor = newAnchor;
        newJoint.connectedBody = connectedBody;
        SetupJointLimits(newJoint);
        joints.Add(newJoint);
        //Debug.Log("Joint created with: " + connectedBody.name);
    }
    public void SetupJointLimits(HingeJoint joint)
    {
        joint.useLimits = true;
        JointLimits limits = joint.limits;
        limits.min = 10;
        limits.max = 10;
        limits.bounciness = 0;
        limits.bounceMinVelocity = 0;
        joint.limits = limits;
    }

    protected bool ShouldDetachDistance(HingeJoint joint)
    {
        if (joint == null || joint.connectedBody == null)
            return false;

        float distance = Vector3.Distance(transform.position, joint.connectedBody.transform.position);
        //Debug.Log($"Distance for joint: {distance}");
        float distanceThreshold = 1.5f;

        return distance > distanceThreshold;
    }
    void Update()
    {
        if (joints.Count > 0)
        {
            for (int i = joints.Count - 1; i >= 0; i--)
            {
                if (joints[i] != null && ShouldDetachDistance(joints[i]))
                {
                    Detach();
                    break; // Exit the loop after detaching one joint
                }
            }
        }
    }

    public void Detach()
    {
        for (int i = joints.Count - 1; i >= 0; i--)
        {
            if (ShouldDetachDistance(joints[i]))
            {
                Destroy(joints[i]);
                next.GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
                next.prev = null;
                next = null;
                gameObject.GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
                joints.RemoveAt(i);
                detachSound.Play(0);
                //Debug.Log($"Joint {i} destroyed due to distance");
            }
        }
    }
    public bool IsConnected()
    {
        return joints.Any(j => j != null && j.connectedBody != null);
    }

    public void BeginTransform()
    {

    }

    public void UpdateTransform()
    {
        foreach (var joint in joints)
        {
            if (joint != null && ShouldDetachDistance(joint))
            {
                Detach();
                break; // Exit the loop after detaching one joint
            }
        }
    }

    public void EndTransform()
    {
        foreach (var joint in joints)
        {
            if (ShouldDetachDistance(joint))
            {
                Detach();
                break; // Exit the loop after detaching one joint
            }
        }
    }

    public void Initialize(IGrabbable grabbable)
    {
        throw new System.NotImplementedException();
    }
    protected void FindAttachTransforms()
    {
        allChildren = gameObject.GetComponentsInChildren<Transform>();
        rodTransform = Array.Find<Transform>(allChildren, c => c.gameObject.name == "rodTransform");
        if (!isTrain)
        {
            handTransform = Array.Find<Transform>(allChildren, c => c.gameObject.name == "handTransform");
        }
    }
    protected void GetSpline()
    {
        splinesArray = Game.INSTANCE.GetSplinesArray();
        splineContainer = Game.INSTANCE.GetSplineContainer();
    }
}
