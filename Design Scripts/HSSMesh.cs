using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.UIElements;

public class HSSMesh : MeshDesigner {
    // HSS ends consist of one outside edge and one inside edge. The corners are rounded to have a radius of 2t.

    // Dimensions of HSS. width, height, thickness
    float w_hss;
    float h_hss;
    float t_hss;

    // Number of points in the middle of the curve of the rounded corners of HSS
    int num_pts_curve;

    // Number of vert for one edge.
    int num_vert_edge;

    // Initializer
    public HSSMesh(Vector3 p_point1, Vector3 p_point2, float p_w, float p_h, float p_t, int p_num_pts_curve) : base(p_point1, p_point2) {
        w_hss = p_w;
        h_hss = p_h;
        t_hss = p_t;
        num_pts_curve = p_num_pts_curve;

        // Number of vert for one edge
        num_vert_edge = 8 + 4 * num_pts_curve;
    }

    // Calcualte Edge for HSS
    private Vector3[] CalculateHSSEdge(float w, float h, float t, Vector3 pt) {
        // Initialize vert_list
        Vector3[] vert_list = new Vector3[num_vert_edge];

        // Equation of circle is: y^2 = r^2 - x^2
        for (int j = 0; j < num_pts_curve + 2; j++) {
            // Top left corner
            float x0 = -w / 2 + 2 * t / (num_pts_curve + 1) * j;
            float y0 = h / 2 - 2 * t + (float)Math.Sqrt((float)Math.Pow(2 * t, 2) - (float)Math.Pow(2 * t - 2 * t / (num_pts_curve + 1) * j, 2));
            Vector3 vert0 = new Vector3(x0, y0, 0) + pt;
            vert_list[j] = vert0;

            // Top right corner
            float x1 = w / 2 - 2 * t + 2 * t / (num_pts_curve + 1) * j;
            float y1 = h / 2 - 2 * t + (float)Math.Sqrt((float)Math.Pow(2 * t, 2) - (float)Math.Pow(2 * t / (num_pts_curve + 1) * j, 2));
            Vector3 vert1 = new Vector3(x1, y1, 0) + pt;
            vert_list[num_pts_curve + 2 + j] = vert1;

            // Bottom right corner
            float x2 = w / 2 - 2 * t / (num_pts_curve + 1) * j;
            float y2 = -h / 2 + 2 * t - (float)Math.Sqrt((float)Math.Pow(2 * t, 2) - (float)Math.Pow(2 * t - 2 * t / (num_pts_curve + 1) * j, 2));
            Vector3 vert2 = new Vector3(x2, y2, 0) + pt;
            vert_list[2 * (num_pts_curve + 2) + j] = vert2;

            // Bottom left corner
            float x3 = -w / 2 + 2 * t - 2 * t / (num_pts_curve + 1) * j;
            float y3 = -h / 2 + 2 * t - (float)Math.Sqrt((float)Math.Pow(2 * t, 2) - (float)Math.Pow(2 * t / (num_pts_curve + 1) * j, 2));
            Vector3 vert3 = new Vector3(x3, y3, 0) + pt;
            vert_list[3 * (num_pts_curve + 2) + j] = vert3;
        }

        return vert_list;
    }

    private (Vector3[], Vector3[], Vector3[], Vector3[]) CalculateHSSVerts() {
        // There are 4 edges in an HSS: outside edge and inside edge on each end of the beam
        // Edge 1: Outside edge near point 1
        Vector3[] vert_list_o1 = CalculateHSSEdge(w_hss, h_hss, t_hss, point1);

        // Edge 2: Outside edge near point 2
        Vector3[] vert_list_o2 = CalculateHSSEdge(w_hss, h_hss, t_hss, point2);

        // Edge 3: Inside edge near point 1
        Vector3[] vert_list_i1 = CalculateHSSEdge(w_hss - 2 * t_hss, h_hss - 2 * t_hss, t_hss, point1);

        // Edge 4: Inside edge near point 2
        Vector3[] vert_list_i2 = CalculateHSSEdge(w_hss - 2 * t_hss, h_hss - 2 * t_hss, t_hss, point2);

        return (vert_list_o1, vert_list_o2, vert_list_i1, vert_list_i2);
    }

    public Mesh createHSSMesh() {
        // Get vertice lists
        var (vert_list_o1, vert_list_o2, vert_list_i1, vert_list_i2) = CalculateHSSVerts();

        // Create the mesh
        Mesh mesh = new Mesh();
        mesh.name = "HSS Beam";
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;

        // Set vertices
        Vector3[] vert_list_total = vert_list_o1.Concat(vert_list_o2).Concat(vert_list_i1).Concat(vert_list_i2).ToArray();
        mesh.vertices = vert_list_total;

        // Outer and inner shells of triangles
        int[] tri_out_shell = createClosedLoopFaces(0, num_vert_edge, num_vert_edge);
        int[] tri_in_shell = createClosedLoopFaces(2 * num_vert_edge, 3 * num_vert_edge, num_vert_edge);

        // End face triangles
        int[] tri_end1 = createClosedLoopFaces(0, 2 * num_vert_edge, num_vert_edge);
        int[] tri_end2 = createClosedLoopFaces(num_vert_edge, 3 * num_vert_edge, num_vert_edge);

        // Put triangle lists together
        int[] triangles = tri_out_shell.Concat(tri_in_shell).Concat(tri_end1).Concat(tri_end2).ToArray();

        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }
}
