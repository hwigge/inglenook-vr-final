using UnityEngine;
using Oculus.Interaction;
using UnityEngine.Splines;
using Unity.Mathematics;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
public class Cart : MonoBehaviour, ITransformer
{
    protected ConfigurableJoint joint;
    public float detachForceThreshhold = 0.1f;
    protected Grabbable grabbable;
    protected SplineContainer splineContainer;
    protected UnityEngine.Vector3 trainPos;
    protected ArrayList splines;
    protected Spline[] splinesArray;
    protected List<ConfigurableJoint> joints = new List<ConfigurableJoint>();
    protected Transform[] allChildren;
    protected Transform rodTransform, handTransform;
    public bool isTrain = false;
    // Start is called before the first frame update
    void Start()
    {
        GetSpline();
        //StartCouple();
        FindAttachTransforms();
    }

    public void OnCollisionEnter(Collision collision)
    {

        Debug.Log("Collision with: " + collision.gameObject.name);
        Cart otherComponent = collision.gameObject.GetComponentInParent<Cart>();
        /* Transform otherTransform = otherComponent.transform;
        float distToHand = Vector3.Distance(handTransform.position, otherTransform.position);
        float distToRod = Vector3.Distance(rodTransform.position, otherTransform.position);
        if (distToHand > distToRod)
        {
            Debug.Log("Det borde vara next!");
        }
        else
        {
            Debug.Log("Det borde vara prev!");
        }  */
        Debug.Log("othercomp is: " + otherComponent);
        if (otherComponent != null)
        {
            Debug.Log("if sats för joints nådd");
            CreateJoint(otherComponent.GetComponent<Rigidbody>());
        }
        else
        {
            Debug.Log("if sats ej nådd. isconn: " + IsConnected());
        }
    }

    protected void LateUpdate()
    {
        SnapCartToTrack();
    }

    /* private void StartCouple()
    {
        if (next == null) //om inte uppdaterad genom inspectorn
        {
            CoupleNext(new NullCart()); //"koppla" med NullCart
        }
        else
        {
            CoupleNext(next); //annars, koppla med nästa
        }
        if (prev == null) //om inte uppdaterad genom inspectorn
        {
            CoupleNext(new NullCart()); //"koppla" med NullCart
        }
        else
        {
            CoupleNext(next); //annars, koppla med nästa
        }
    } */

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
        ConfigurableJoint newJoint = gameObject.AddComponent<ConfigurableJoint>();
        newJoint.connectedBody = connectedBody;
        SetupJointLimits(newJoint);
        joints.Add(newJoint);
        Debug.Log("Joint created with: " + connectedBody.name);
    }
    public void SetupJointLimits(ConfigurableJoint joint)
    {
        joint.xMotion = ConfigurableJointMotion.Limited;
        joint.yMotion = ConfigurableJointMotion.Limited;
        joint.zMotion = ConfigurableJointMotion.Limited;

        joint.angularXMotion = ConfigurableJointMotion.Locked;
        joint.angularYMotion = ConfigurableJointMotion.Locked;
        joint.angularZMotion = ConfigurableJointMotion.Locked;

        var spring = joint.linearLimitSpring;
        spring.spring = 1000f;
        spring.spring = 0.5f;
        spring.damper = 50f;
        joint.linearLimitSpring = spring;
    }

    protected bool ShouldDetachDistance(ConfigurableJoint joint)
    {
        if (joint == null || joint.connectedBody == null)
            return false;

        float distance = Vector3.Distance(transform.position, joint.connectedBody.transform.position);
        Debug.Log($"Distance for joint: {distance}");
        float distanceThreshold = 3.0f;

        return distance > distanceThreshold;
    }
    void Update()
    {
        //SnapCartToTrack();
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
                joints.RemoveAt(i);
                Debug.Log($"Joint {i} destroyed due to distance");
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
    public void SplitTrain()
    {
        //do something
    }

    public void CoupleNext(TrainCart next)
    {
        //do something
    }

    public void CouplePrev(TrainCart prev)
    {
        //do something
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
