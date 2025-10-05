using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

public class DeflMesh : MeshDesigner{
    // Get deflection and rotation for end points
    float[] end_pt_defl_rot_vals;

    // SAP2000 coords for end point i and end point j
    Vector3 joint_i_pos_sap;
    Vector3 joint_j_pos_sap;

    // Vertical deflection scale and num pts for deflection line
    float verticalDeflectionScale;
    int num_pts;

    // Convert inches to unity units
    float sap2unity_scale;

    // Set up mesh designer
    public DeflMesh(Vector3 p_point1, Vector3 p_point2, float[] p_end_pt_defl_rot_vals, Vector3 p_joint_i_pos_sap, Vector3 p_joint_j_pos_sap, float p_verticalDeflectionScale, int p_num_pts, float p_sap2unity_scale) : base(p_point1, p_point2){
        end_pt_defl_rot_vals = p_end_pt_defl_rot_vals;

        joint_i_pos_sap = p_joint_i_pos_sap;
        joint_j_pos_sap = p_joint_j_pos_sap;

        verticalDeflectionScale = p_verticalDeflectionScale;
        num_pts = p_num_pts;

        sap2unity_scale = p_sap2unity_scale;
    }

    // Interpolate along a 3rd degree polynomial given 2 points and 2 angles
    private float[] GetPtsAlongPoly(float x1, float x2, float y1, float y2, float rot1, float rot2) {
        // End Point 1: (x1, y1)
        // End Point 2: (x2, y2)
        // Angles: rot1 and rot2 in radians
        // num_pts: Number of evenly spaced points along the curve you want

        // Convert Angles to Slopes
        float m1 = (float)(Math.Sin(rot1) / Math.Cos(rot1));
        float m2 = (float)(Math.Sin(rot2) / Math.Cos(rot2));

        // Get powers of x1 and x2
        float x1_2 = (float)Math.Pow(x1, 2);  // x1^2
        float x1_3 = (float)Math.Pow(x1, 3);  // x1^3
        float x2_2 = (float)Math.Pow(x2, 2);  // x2^2
        float x2_3 = (float)Math.Pow(x2, 3);  // x2^3

        // Get constants
        float a = (m1 * x1 - m1 * x2 + m2 * x1 - m2 * x2 - 2 * y1 + 2 * y2) / (x1_3 - 3 * x1_2 * x2 + 3 * x1 * x2_2 - x2_3);
        float b = (-m1 * x1_2 - m1 * x1 * x2 + 2 * m1 * x2_2 - 2 * m2 * x1_2 + m2 * x1 * x2 + m2 * x2_2 + 3 * x1 * y1 - 3 * x1 * y2 + 3 * x2 * y1 - 3 * x2 * y2) / (x1_3 - 3 * x1_2 * x2 + 3 * x1 * x2_2 - x2_3);
        float c = (2 * m1 * x1_2 * x2 - m1 * x1 * x2_2 - m1 * x2_3 + m2 * x1_3 + m2 * x1_2 * x2 - 2 * m2 * x1 * x2_2 - 6 * x1 * x2 * y1 + 6 * x1 * x2 * y2) / (x1_3 - 3 * x1_2 * x2 + 3 * x1 * x2_2 - x2_3);
        float d = (-m1 * x1_2 * x2_2 + m1 * x1 * x2_3 - m2 * x1_3 * x2 + m2 * x1_2 * x2_2 + x1_3 * y2 - 3 * x1_2 * x2 * y2 + 3 * x1 * x2_2 * y1 - x2_3 * y1) / (x1_3 - 3 * x1_2 * x2 + 3 * x1 * x2_2 - x2_3);

        // Initialize points along curve
        float[] pts_along_poly = new float[num_pts]; // One extra pt at the end

        // Go through and calculate the y value at each point
        for (int i = 0; i < num_pts; i++) {
            float x = x1 + (x2 - x1) / (num_pts - 1) * i;
            pts_along_poly[i] = a * (float)Math.Pow(x, 3) + b * (float)Math.Pow(x, 2) + c * x + d;
        }

        // Return pts along poly
        return pts_along_poly;
    }


    // Calculate deflection points
    private (Vector3[], Vector3[], float) CalculateDeflection() {
        // end_pt_defl_rot_vals: End Pt deflection and rotation vals in this order:
        //      iu1, iu2, iu3, ir1, ir2, ir3, ju1, ju2, ju3, jr1, jr2, jr3
        // num_pts: Number of evenly spaced points along the curve you want
        // SAP2000 +x axis = Unity -x axis => Unity +x axis = SAP2000 -x axis
        // SAP2000 +y axis = Unity -z axis => Unity +z axis = SAP2000 -y axis
        // SAP2000 +z axis = Unity +y axis => Unity +y axis = SAP2000 +z axis

        // Extract values in SAP2000 Coord sys
        Vector3 defl1_sap = new Vector3(end_pt_defl_rot_vals[0], end_pt_defl_rot_vals[1], end_pt_defl_rot_vals[2]);
        Vector3 rot1_sap = new Vector3(end_pt_defl_rot_vals[3], end_pt_defl_rot_vals[4], end_pt_defl_rot_vals[5]);
        Vector3 defl2_sap = new Vector3(end_pt_defl_rot_vals[6], end_pt_defl_rot_vals[7], end_pt_defl_rot_vals[8]);
        Vector3 rot2_sap = new Vector3(end_pt_defl_rot_vals[9], end_pt_defl_rot_vals[10], end_pt_defl_rot_vals[11]);

        // Get deflected joint 1 and joint j in SAP2000 coords
        Vector3 defl_i_sap = joint_i_pos_sap + defl1_sap;
        Vector3 defl_j_sap = joint_j_pos_sap + defl2_sap;

        // Solve deflection values for Sap2000 z axis.
        // These will be the deflections used for Unity y axis.
        float[] z_defl_pts = GetPtsAlongPoly(defl_i_sap[1], defl_j_sap[1], defl_i_sap[2], defl_j_sap[2], rot1_sap[0], rot2_sap[0]);

        // Convert from the SAP2000 coord sys to Unity coord sys
        // For this element. i is for point2 and j is for point1 due to SAP2000 coord sys
        Vector3 defl1 = new Vector3(-defl2_sap.x, defl2_sap.z, -defl2_sap.y);  // Deflection at point 1
        Vector3 defl2 = new Vector3(-defl1_sap.x, defl1_sap.z, -defl1_sap.y);  // Deflection at Point 2

        // Get deflected point 1 and point 2 in Unity coords. 
        Vector3 defl_pt1 = point1 + defl1 * sap2unity_scale;
        Vector3 defl_pt2 = point2 + defl2 * sap2unity_scale;

        // Initialize deflection points
        Vector3[] deflectionPoints = new Vector3[num_pts];
        Vector3[] zPoints = new Vector3[num_pts];

        // Get deflection points
        for (int i = 0; i < num_pts; i++) {
            float x = defl_pt1.x;  // Point1.x and Point2.x values are very similar so just use Point1.x
            float z = defl_pt2.z - (defl_pt2.z - defl_pt1.z) / (num_pts - 1) * i;  // Solving for it backwards so the deflections are inverted
            float y = defl_pt1.y + z_defl_pts[i] * sap2unity_scale * verticalDeflectionScale;

            deflectionPoints[i] = new Vector3(x, y, z);
            zPoints[i] = new Vector3(x, defl_pt1.y, z);
        }

        // Get max abs deflection
        float max_defl = z_defl_pts.Max(x => Math.Abs(x));

        // Return deflection points, z points, and max deflection in inches in sap2000 units
        return (deflectionPoints, zPoints, max_defl);
    }

    // Create deflection line (there must be only 11 deflection points)
    public (Vector3[], Mesh, float) CreateDeflMesh() {
        // Calculate Deflection Points
        var (deflectionPoints, zPoints, max_defl) = CalculateDeflection();

        // Reverse lists to be from left to right
        Vector3[] defl_pts2 = deflectionPoints.Reverse<Vector3>().ToArray<Vector3>();
        Vector3[] z_pts2 = zPoints.Reverse<Vector3>().ToArray<Vector3>();

        // Put together vertices list
        Vector3[] vert_list = defl_pts2.Concat(z_pts2).ToArray<Vector3>();

        // Create the mesh
        Mesh mesh = new Mesh();
        mesh.name = "Deflection Fill";

        // Vertices
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.vertices = vert_list;

        // Triangles
        int[] triangles = createOpenLoopFaces(0, num_pts, num_pts);
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return (deflectionPoints, mesh, max_defl);
    }

}
