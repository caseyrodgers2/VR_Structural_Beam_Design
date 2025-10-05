using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BuiltUpWMesh : MeshDesigner {
    // Assume one plate on top of the W beam and one plate right below the W beam.
    // Assume plate width matches the width of the W beam.

    // Dimensions of W shape beam. width, height, thickness flange, thickness web
    float w_wBeam;
    float h_wBeam;
    float tf_wBeam;
    float tw_wBeam;

    // Dimensions of plate. Width, height
    float h_pl;

    // Initializer
    public BuiltUpWMesh(Vector3 p_point1, Vector3 p_point2, float p_w_wbeam, float p_h_wbeam, float p_tf_wbeam, float p_tw_wbeam, float p_h_pl) : base(p_point1, p_point2) {
        // Get W beam dimensions
        w_wBeam = p_w_wbeam;
        h_wBeam = p_h_wbeam;
        tf_wBeam = p_tf_wbeam;
        tw_wBeam = p_tw_wbeam;

        // Get plate dimensions
        h_pl = p_h_pl;
    }

    public Mesh createBuiltUpWMesh() {
        // Create the W beam mesh
        WMesh wm = new WMesh(point1, point2, w_wBeam, h_wBeam, tf_wBeam, tw_wBeam);
        Mesh w_beam_mesh = wm.createWMesh();

        // Create the top plate
        Vector3 added_height1 = new Vector3(0, h_wBeam / 2 + h_pl / 2, 0);
        PlateMesh pm1 = new PlateMesh(point1 + added_height1, point2 + added_height1, w_wBeam, h_pl);
        Mesh plate_mesh1 = pm1.createPlateMesh();

        // Create the bottom plate
        Vector3 added_height2 = new Vector3(0, -h_wBeam / 2 - h_pl / 2, 0);
        PlateMesh pm2 = new PlateMesh(point1 + added_height2, point2 + added_height2, w_wBeam, h_pl);
        Mesh plate_mesh2 = pm2.createPlateMesh();

        // Create the new mesh
        Mesh mesh = new Mesh();
        mesh.name = "Built up W Mesh";
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;

        // Set vertices
        Vector3[] new_vert_list = w_beam_mesh.vertices.Concat(plate_mesh1.vertices).Concat(plate_mesh2.vertices).ToArray();
        mesh.vertices = new_vert_list;

        // W beam triangles 
        int[] w_beam_tri = w_beam_mesh.triangles;

        // Top plate triangles. Adjust vertices to match master list
        int[] plate1_tri = plate_mesh1.triangles;
        for (int i = 0; i < plate1_tri.Length; i++) {
            plate1_tri[i] += w_beam_mesh.vertices.Length;
        }

        // Bottom plate triangles. Adjust vertices to match master list
        int[] plate2_tri = plate_mesh2.triangles;
        for (int i = 0; i < plate2_tri.Length; i++) {
            plate2_tri[i] += w_beam_mesh.vertices.Length + plate_mesh1.vertices.Length;
        }

        // Combine triangle lists
        int[] triangles = w_beam_tri.Concat(plate1_tri).Concat(plate2_tri).ToArray();

        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }
}
