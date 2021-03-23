using System;
using Unity.Mathematics;
using UnityEngine;

public class TransformChild : MonoBehaviour
{
    // Public variables
    public Transform parent;
    public bool moveWith = true;
    public bool rotateWith = true;
    public bool rotateChild = true;
    public float multiplier;
    private bool hasRB;
    
    // Variables for position update
    private Vector3 previousPos;
    private Vector3 newPos;

    // Variables for rotation update
    private Quaternion previousRot;
    private Quaternion newRot;
    private float magnitude;
    private System.Numerics.Vector4 fastQuat;
    private System.Numerics.Vector4 q1;
    private System.Numerics.Vector4 q2;
    private System.Numerics.Vector4 q3;
    private System.Numerics.Vector4 q4;

    // Variables for accessibility
    private Quaternion change;
    private bool customRot;
    private Quaternion custom;

    // Start is called before the first frame update
    void Start()
    {
        hasRB = GetComponent<Rigidbody>() == null;
        Debug.Assert(hasRB, "The Child must not have a Rigidbody component.");
        if (!hasRB)
        {
            moveWith = false;
            rotateWith = false;
        }
        previousPos = parent.localPosition;
        previousRot = parent.localRotation;
    }

    // Update is called once per frame
    void Update()
    {
        newPos = parent.localPosition;
        newRot = parent.localRotation;
        if (moveWith)
            UpdatePosition();
        if (rotateWith || customRot)
            UpdateRotation();
        previousPos = newPos;
        previousRot = newRot;
    } /* End Update() */

    /*
     * UpdatePosition() sets the child's transform.position vector to the
     * parent's plus some offset. Therefore, if the parent is moved in some
     * direction with some magnitude, the child will also move in that
     * direction with that magnitude.
     */
    void UpdatePosition()
    {
        if (!Vector3.Equals(newPos, previousPos))
            transform.position += (newPos - previousPos);
    } /* End UpdatePosition() */

    /*
     * UpdateRotation() computes the childrens' new rotation and position
     * after the parent changes rotation. In this code, 
     * the child rotates with respect to the parent.
     */
    void UpdateRotation()
    {
        magnitude = (transform.localPosition - parent.localPosition).magnitude;
        previousRot = Quaternion.Inverse(previousRot);
        change = Quaternion.Normalize(ChangeAngle(HamiltonProduct(newRot, previousRot), multiplier));
        if (!Quaternion.Equals(previousRot, newRot))
        {
            Quaternion tran;
            if (customRot)
                tran = Quaternion.Normalize(custom);
            else
                tran = change;
            Vector3 v1 = Vector3.Normalize(transform.localPosition - parent.localPosition);
            transform.localPosition = (ApplyRotation(tran, v1) * magnitude) + parent.localPosition;

            if (rotateChild || customRot)
                transform.localRotation = HamiltonProduct(tran, transform.localRotation);
        }
    } /* End UpdateRotation() */
    
    /*
     * ChangeAngle() is a helper function used to change the angle of
     * rotation in the given Quaternion. It is referenced once in
     * UpdateRotation() for position computation.
     */
    Quaternion ChangeAngle(Quaternion q, float change)
    {
        if (change != 1)
        {
            q = Quaternion.Normalize(q);
            if (q.w != 0)
            {
                float doubleW = change * q.w;
                q.w = (float)Math.Cos(Math.Acos(q.w) * 2);
                q.x *= doubleW;
                q.y *= doubleW;
                q.z *= doubleW;
            }
            else
            {
                Debug.Log("Cos(Theta): " + q.w);
            }
        }
        return q;
    } /* End ChangeAngle() */

    /*
     * HamiltonProduct() is a helper function used to compute Quaternion
     * multiplication, and is referenced twice to aid in RotationUpdate().
     * System numeric vectors are used for extra hardware optimizations.
     */
    Quaternion HamiltonProduct(Quaternion quat1, Quaternion quat2)
    {
        fastQuat = new System.Numerics.Vector4(quat1.x, quat1.y, quat1.z, quat1.w);
        float[] vec4 = {quat2.w, quat2.x, quat2.y, quat2.z};

        q1 = fastQuat * vec4[0];
        q2 = fastQuat * vec4[1];
        q3 = fastQuat * vec4[2];
        q4 = fastQuat * vec4[3];

        return new Quaternion
        {
            w = q1.W - q2.X - q3.Y - q4.Z,
            x = q2.W + q1.X + q4.Y - q3.Z,
            y = q3.W + q1.Y + q2.Z - q4.X,
            z = q4.W + q1.Z + q3.X - q2.Y
        };
    } /* End HamiltonProduct() */

    /*
     * ApplyRotation() is a helper function that turns the transform Quaternion
     * into a 3x3 transformation matrix, which is then applied onto the child's
     * position vector to get the new position after the parent rotates.
     */
    public Vector3 ApplyRotation(Quaternion q, Vector3 v1)
    {
        fastQuat = new System.Numerics.Vector4(q.x, q.y, q.z, q.w);
        float[] vec3 = { q.x, q.y, q.z };
        q1 = fastQuat * vec3[0]; //X * X,Y,Z,W
        q2 = fastQuat * vec3[1]; //Y * X,Y,Z,W 
        q3 = fastQuat * vec3[2]; //Z * X,Y,Z,W

        return new Vector3
        {
            x = (v1.x*(1 - 2*(q2.Y + q3.Z))) + (2*v1.y*(q1.Y - q3.W)) + (2*v1.z*(q1.Z + q2.W)),
            y = (2*v1.x*(q1.Y + q3.W)) + (v1.y*(1 - 2*(q1.X + q3.Z))) + (2*v1.z*(q2.Z - q1.W)),
            z = (2*v1.x*(q1.Z - q2.W)) + (2*v1.y*(q2.Z + q1.W)) + (v1.z*(1 - 2*(q1.X + q2.Y)))
        };
    } /* End ApplyRotation() */


    /*
     * The rest of these functions are meant to increase accessibility:
     * GetParentChangeQuat allows other objects to see the change quaternion
     * of the parent object between frames.
     */

    public Quaternion GetParentChangeQuat()
    {
        return change;
    } /* End GetParentChangeQuat() */

    /* 
     * ApplyCustom() allows outside scripts to provide rotation info
     * to apply to this object. Once the angle and normal vector are given,
     * this function formulates a suitable quaternion and applies it to the 
     * object
     */

    public void ApplyCustom(Vector3 axis, float angle)
    {
        Vector3.Normalize(axis);
        custom = new Quaternion
        {
            w = Mathf.Cos(angle),
            x = axis.x * Mathf.Sin(angle),
            y = axis.y * Mathf.Sin(angle),
            z = axis.z * Mathf.Sin(angle)
        };
    } /* End ApplyCustom() */

    /*
     * StopChild must be called before applying a custom rotation.
     * It essentially disconnects the child from the parent, so that the
     * child can operate on the custom values more freely
     */

    public void StopChild()
    {
        rotateChild = false;
        rotateWith = false;
        customRot = true;
    } /* End StopChild() */


    /*
     * Call restart child to reappend the child object to the parent.
     * This should be done if custom rotation is finished and we want to
     * continue with the parent function
     */
    public void RestartChild()
    {
        rotateChild = true;
        rotateWith = true;
        customRot = false;
    } /* End RestartChild() */
}