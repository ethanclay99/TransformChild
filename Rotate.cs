using UnityEngine;
using System;

public class Rotate : MonoBehaviour
{
    //Public variables for extra utility
    public bool keepRotating;
    public bool asLocalVector;
    public Vector3 axis;
    public float speed;

    //System vectors for faster computation
    private System.Numerics.Vector4 fastQuat;
    private System.Numerics.Vector4 q1;
    private System.Numerics.Vector4 q2;
    private System.Numerics.Vector4 q3;
    private System.Numerics.Vector4 q4;

    private void Start()
    {
        keepRotating = true;
    }

    private void Update()
    {
        if (keepRotating)
        {
            float angle = speed / 1000;

            //Find normalized world vector for given Vector3 
            //(world vector unless asLocalVector == true)
            Vector3 Cor = axis;
            if (asLocalVector)
            {
                Cor = (axis.x * transform.right)
                    + (axis.y * transform.up)
                    + (axis.z * transform.forward);
            }
            Cor.Normalize();

            // Transform values into a usable quaternion
            Quaternion q = new Quaternion
            {
                x = Cor.x * Mathf.Sin(angle),
                y = Cor.y * Mathf.Sin(angle),
                z = Cor.z * Mathf.Sin(angle),
                w = Mathf.Cos(angle)
            };

            //Apply quaternion on this gameObject
            gameObject.transform.rotation = 
                HamiltonProduct(Quaternion.Normalize(q), transform.rotation);
        }
    }

    /*
     * HamiltonProduct() is a helper function used to compute Quaternion
     * multiplication, and is referenced twice to aid in RotationUpdate().
     * System numeric vectors are used for extra hardware optimizations.
     */
    Quaternion HamiltonProduct(Quaternion quat1, Quaternion quat2)
    {
        fastQuat = new System.Numerics.Vector4(quat1.x, quat1.y, quat1.z, quat1.w);
        float[] vec4 = { quat2.w, quat2.x, quat2.y, quat2.z };

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
}