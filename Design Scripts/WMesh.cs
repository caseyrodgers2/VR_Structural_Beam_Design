using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WMesh : MeshDesigner {
    // Imagine a grid with four horizontal lines: each corresponding with a top / bottom line of a flange.
    // Add four vertical lines: the outer 2 lines define the width of the w beam. The inner 2 lines define the web of the beam.
    // The two inner horizontal lines have 4 vertices each at the intersections between the vertical lines and the horizontal line
    // The two outer horizontal lines have 3 vertices each: two on the outside and one at the centerline
    // This gives a total of 4 * 2 + 3 * 2 vertices = 14 vertices.
    // All outside edges will have two vertices to make the normals sharp.

    // Dimensions of W shape beam. width, height, thickness flange, thickness web
    float w_wBeam;
    float h_wBeam;
    float tf_wBeam;
    float tw_wBeam;

    // Initializer
    public WMesh(Vector3 p_point1, Vector3 p_point2, float p_w, float p_h, float p_tf, float p_tw) : base(p_point1, p_point2) {
        w_wBeam = p_w;
        h_wBeam = p_h;
        tf_wBeam = p_tf;
        tw_wBeam = p_tw;
    }

    // Calcualte vertex positions for outer shell faces
    private Vector3[] CalculateWVertsOuterFace(Vector3 pt) {
        // Initialize vertice list and normals list
        Vector3[] verts = new Vector3[26];

        // List of potential x and y values
        float[] x_vals = { -w_wBeam / 2, -tw_wBeam / 2, tw_wBeam / 2, w_wBeam / 2 };
        float[] y_vals = { h_wBeam / 2, h_wBeam / 2 - tf_wBeam, -h_wBeam / 2 + tf_wBeam, -h_wBeam / 2 };

        // Top of Beam
        verts[0] = new Vector3(x_vals[0], y_vals[0], 0);
        verts[1] = new Vector3(0, y_vals[0], 0);
        verts[3] = new Vector3(x_vals[3], y_vals[0], 0);

        // Right side of beam
        verts[5] = new Vector3(x_vals[3], y_vals[1], 0);
        verts[7] = new Vector3(x_vals[2], y_vals[1], 0);
        verts[9] = new Vector3(x_vals[2], y_vals[2], 0);
        verts[11] = new Vector3(x_vals[3], y_vals[2], 0);

        // Bottom of beam
        verts[13] = new Vector3(x_vals[3], y_vals[3], 0);
        verts[14] = new Vector3(0, y_vals[3], 0);
        verts[15] = new Vector3(x_vals[0], y_vals[3], 0);

        // Left side of beam
        verts[17] = new Vector3(x_vals[0], y_vals[2], 0);
        verts[19] = new Vector3(x_vals[1], y_vals[2], 0);
        verts[21] = new Vector3(x_vals[1], y_vals[1], 0);
        verts[23] = new Vector3(x_vals[0], y_vals[1], 0);

        // Make duplicates of vertices on the outside edges
        for (int i = 3; i < 14; i += 2) {
            verts[i - 1] = verts[i];
        }

        for (int i = 15; i < 24; i += 2) {
            verts[i + 1] = verts[i];
        }

        verts[25] = verts[0];

        // Add point position to each vert
        for (int i = 0; i < verts.Length; i++) {
            verts[i] += pt;
        }

        // Return vertice list
        return verts;

    }

    private Vector3[] CalculateWVertsEndFace(Vector3 pt) {
        // Initialize vertice list and normals list
        Vector3[] verts = new Vector3[14];

        // List of potential x and y values
        float[] x_vals = { -w_wBeam / 2, -tw_wBeam / 2, tw_wBeam / 2, w_wBeam / 2 };
        float[] y_vals = { h_wBeam / 2, h_wBeam / 2 - tf_wBeam, -h_wBeam / 2 + tf_wBeam, -h_wBeam / 2 };

        // Top of Beam
        verts[0] = new Vector3(x_vals[0], y_vals[0], 0);
        verts[1] = new Vector3(0, y_vals[0], 0);
        verts[2] = new Vector3(x_vals[3], y_vals[0], 0);

        // Right side of beam
        verts[3] = new Vector3(x_vals[3], y_vals[1], 0);
        verts[4] = new Vector3(x_vals[2], y_vals[1], 0);
        verts[5] = new Vector3(x_vals[2], y_vals[2], 0);
        verts[6] = new Vector3(x_vals[3], y_vals[2], 0);

        // Bottom of beam
        verts[7] = new Vector3(x_vals[3], y_vals[3], 0);
        verts[8] = new Vector3(0, y_vals[3], 0);
        verts[9] = new Vector3(x_vals[0], y_vals[3], 0);

        // Left side of beam
        verts[10] = new Vector3(x_vals[0], y_vals[2], 0);
        verts[11] = new Vector3(x_vals[1], y_vals[2], 0);
        verts[12] = new Vector3(x_vals[1], y_vals[1], 0);
        verts[13] = new Vector3(x_vals[0], y_vals[1], 0);

        // Add point position to each vert
        for (int i = 0; i < verts.Length; i++) {
            verts[i] += pt;
        }

        // Return vertice list
        return verts;
    }

    private int[] WEndFaceTri(int k) {
        // k = Starting Index of top left vertex

        // Top row
        int[] tri_top = { 0, 12, 13, 0, 1, 12, 1, 4, 12, 1, 2, 4, 2, 3, 4 };

        // Middle
        int[] tri_mid = {12, 5, 11, 12, 4, 5};

        // Bottom row
        int[] tri_bot = { 10, 11, 9, 11, 8, 9, 11, 5, 8, 5, 7, 8, 5, 6, 7};

        // Combine triangle arrays
        int[] triangles = tri_top.Concat(tri_mid).Concat(tri_bot).ToArray();

        // Add constant to all values in array
        for (int i = 0; i < triangles.Length; i++) {
            triangles[i] += k;
        }

        // Return triangles
        return triangles;
    }

    public Mesh createWMesh() {
        // Get vertice lists. Two for each end face.
        // First two are for the outer shell, second two are for the end faces.
        Vector3[] verts1 = CalculateWVertsOuterFace(point1); // Length 26
        Vector3[] verts2 = CalculateWVertsOuterFace(point2); // Length 26

        Vector3[] verts3 = CalculateWVertsEndFace(point1); // Length 14
        Vector3[] verts4 = CalculateWVertsEndFace(point2); // Length 14

        // Create the mesh
        Mesh mesh = new Mesh();
        mesh.name = "W Beam";
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;

        // Set vertices
        Vector3[] vert_list_total = verts1.Concat(verts2).Concat(verts3).Concat(verts4).ToArray();
        mesh.vertices = vert_list_total;

        // Outer shell of triangles
        int[] outliers_i_list = { 0, 13 };
        int[] num_plane_vert_list = { 3, 3, 2 };
        int[] tri_out_shell = CreateWOuterTri(outliers_i_list, num_plane_vert_list, 26);

        // End face triangles
        int[] tri_end1 = WEndFaceTri(26 + 26);
        int[] tri_end2 = WEndFaceTri(26 + 26 + 14);

        // Put triangle lists together
        int[] triangles = tri_out_shell.Concat(tri_end1).Concat(tri_end2).ToArray();

        mesh.triangles = triangles;

        // Set normals
        mesh.RecalculateNormals();

        return mesh;
    }


}
