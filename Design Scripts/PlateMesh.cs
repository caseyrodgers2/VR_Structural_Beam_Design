using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlateMesh : MeshDesigner {
    // Dimensions of plate. Width, height
    float w_pl;
    float h_pl;

    // Initializer
    public PlateMesh(Vector3 p_point1, Vector3 p_point2, float p_w, float p_h) : base(p_point1, p_point2) {
        w_pl = p_w;
        h_pl = p_h;
    }

    private Vector3[] CalculatePlateOuterFaceVerts(Vector3 pt) {
        // Initialize vertice list
        Vector3[] verts = new Vector3[8];

        // One vertex in each corner
        verts[0] = new Vector3(-w_pl / 2, h_pl / 2, 0) + pt;
        verts[2] = new Vector3(w_pl / 2, h_pl / 2, 0) + pt;
        verts[4] = new Vector3(w_pl / 2, -h_pl / 2, 0) + pt;
        verts[6] = new Vector3(-w_pl / 2, -h_pl / 2, 0) + pt;

        // Duplicate vertices for sharp edges
        for (int i = 2; i < 8; i += 2) {
            verts[i - 1] = verts[i];
        }

        verts[7] = verts[0];

        // Return
        return verts;
    }

    private Vector3[] CalculatePlateEndFaceVerts(Vector3 pt) {
        // Initialize vertice list
        Vector3[] verts = new Vector3[4];

        // One vertex in each corner
        verts[0] = new Vector3(-w_pl / 2, h_pl / 2, 0) + pt;
        verts[1] = new Vector3(w_pl / 2, h_pl / 2, 0) + pt;
        verts[2] = new Vector3(w_pl / 2, -h_pl / 2, 0) + pt;
        verts[3] = new Vector3(-w_pl / 2, -h_pl / 2, 0) + pt;

        // Return
        return verts;
    }

    public Mesh createPlateMesh() {
        // Get vertice lists. Two for each end face. One each for the outer shell. One each for the end face planes.
        Vector3[] verts1 = CalculatePlateOuterFaceVerts(point1); // Length = 8
        Vector3[] verts2 = CalculatePlateOuterFaceVerts(point2); // Length = 8
        Vector3[] verts3 = CalculatePlateEndFaceVerts(point1); // Length = 4
        Vector3[] verts4 = CalculatePlateEndFaceVerts(point2); // Length = 4

        // Create the mesh
        Mesh mesh = new Mesh();
        mesh.name = "Plate Mesh";
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;

        // Set vertices
        Vector3[] vert_list_total = verts1.Concat(verts2).Concat(verts3).Concat(verts4).ToArray();
        mesh.vertices = vert_list_total;

        // Outer shell of triangles
        int[] outliers_i_list = { };
        int[] num_plane_vert_list = { 2 };
        int[] tri_out_shell = CreateWOuterTri(outliers_i_list, num_plane_vert_list, 8);

        // End face triangles
        int[] tri_end1 = { 16, 18, 19, 16, 17, 18 };
        int[] tri_end2 = { 20, 22, 23, 20, 21, 22 };

        // Put triangle lists together
        int[] triangles = tri_out_shell.Concat(tri_end1).Concat(tri_end2).ToArray();

        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }
}
