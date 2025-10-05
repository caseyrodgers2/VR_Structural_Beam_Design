using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MeshDesigner {
    // End points of beam. Assumed to be center of end of beam.
    protected Vector3 point1;
    protected Vector3 point2;

    // Constructor
    public MeshDesigner(Vector3 p_point1, Vector3 p_point2) {
        point1 = p_point1;
        point2 = p_point2;
    }

    // Create triangles to create an open loop of faces between two vertex lists
    protected int[] createOpenLoopFaces(int k1, int k2, int v_list_length) {
        // k1 = start of vertex list 1 from beginning of master mesh vertex list
        // k2 = start of vertex list 2 from beginning of master mesh vertex list
        // v_list_length = length of both vertex list 1 and 2. Both lists should have the same length!

        // Create triangles list
        int[] triangles = new int[(2 * v_list_length - 2) * 3];

        // Create the triangles
        for (int i = 0; i < v_list_length - 1; i++) {
            // First half of triangles
            //Debug.Log((i - 1) * 3);
            triangles[i * 3] = i + k1;
            triangles[i * 3 + 1] = i + k2;
            triangles[i * 3 + 2] = i + 1 + k2;

            // Second half of triangles
            //Debug.Log((num_pts + i - 2) * 3);
            triangles[(v_list_length - 1 + i) * 3] = i + 1 + k2;
            triangles[(v_list_length - 1 + i) * 3 + 1] = i + 1 + k1;
            triangles[(v_list_length - 1+ i) * 3 + 2] = i + k1;
        }

        return triangles;
    }

    // Create triangles to create a closed loop of faces between two vertex lists
    protected int[] createClosedLoopFaces(int k1, int k2, int v_list_length) {
        // k1 = start of vertex list 1 from beginning of master mesh vertex list
        // k2 = start of vertex list 2 from beginning of master mesh vertex list
        // v_list_length = length of both vertex list 1 and 2. Both lists should have the same length!

        // Create triangles list
        int[] triangles = new int[(2 * v_list_length) * 3];

        for (int i = 0; i < v_list_length; i++) {
            // First half of the triangles
            triangles[i * 3] = i + k1;
            triangles[i * 3 + 1] = i + k2;

            int last_v = i + 1 + k2;
            if (i == v_list_length - 1) {
                last_v = k2;
            }
            triangles[i * 3 + 2] = last_v;

            // Second half of the traingles
            triangles[(i + v_list_length) * 3] = i + k2;
            triangles[(i + v_list_length) * 3 + 1] = i + k1;

            int last_v2 = i - 1 + k1;
            if (i == 0) {
                last_v2 = k1 + v_list_length - 1;
            }
            triangles[(i + v_list_length) * 3 + 2] = last_v2;
        }

        return triangles;
    }

    // Create triangles for an outer shell that has sharp edges.
    public int[] CreateWOuterTri(int[] outlier_i_list, int[] num_plane_vert_list, int end_count) {
        // int[] outlier_i_list = list of size n,
        //      where each element represents the starting index of a plane that has a unique number of plane vertices
        // int[] num_plane_vert_list = list of size n + 1,
        //      where each element is the number of plane vertices corresponding with the outlier_i_list.
        //      The reason it is n+1 is because the extra element is the number of plane vertices for every other plane
        //      not mentioned in the outlier_i list. This list MUST have at least one element.
        // int end_count = when to end the count

        // Initialize triangles array and count
        int[] triangles = { };
        int count = 0;

        // Go through each side and create an open plane
        while (count < end_count) {
            // Length of vertex list for the current plane. Set to default.
            int num_plane_vert = num_plane_vert_list[num_plane_vert_list.Length - 1];

            // Check if it's an outlier. If so, then set the number to the corresponding one int he num_plane_vert_list
            for (int j = 0; j < outlier_i_list.Length; j++) {
                if (count == outlier_i_list[j]) {
                    num_plane_vert = num_plane_vert_list[j];
                }
            }

            // Create triangles array for current plane
            int[] curr_tri = createOpenLoopFaces(count, count + end_count, num_plane_vert);

            // Add current plane triangles to master triangles list
            triangles = triangles.Concat(curr_tri).ToArray();

            // Update count
            count += num_plane_vert;
        }

        // Return triangles
        return triangles;
    }
}
