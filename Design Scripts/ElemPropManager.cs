using NUnit.Framework;
using System.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using NUnit.Framework.Constraints;

/* References:
 * - https://www.youtube.com/watch?v=xwnL4meq-j8
*/

public class ElemPropManager {
    // Element property dictionary
    // Dictionary 0: Material string, Dictionary 1
    // Dictionary 1: Shape string, Dictionary 2
    // Dictionary 2: Size string, Float array of perperties
    private Dictionary<string, Dictionary<string, Dictionary<string, float[]>>> elemPropDict = new Dictionary<string, Dictionary<string, Dictionary<string, float[]>>>();

    // Mesh dictionary
    private Dictionary<string, Dictionary<string, Dictionary<string, Mesh>>> elemMeshDict = new Dictionary<string, Dictionary<string, Dictionary<string, Mesh>>>();

    #region Elem Property Dictionary Functions
    // Read csv file
    public void readCSV(string elemPropFp) {
        // Read file as one string
        TextAsset file = Resources.Load(elemPropFp) as TextAsset;
        string content = file.ToString();
        string[] content_lines = content.Split('\n');

        // Go through file until you reach the end
        for (int i = 0; i < content_lines.Length - 1; i++) {
            // Convert to an array of values
            string[] data_vals = content_lines[i].Split(',');

            // If it's the header line, then skip
            if (data_vals[0] == "Material") {
                continue;

            // Otherwise, parse information
            } else { 
                // Determine material, shape, and size
                string elem_mat = data_vals[0];
                string elem_shape = data_vals[1];
                string elem_size = data_vals[2];

                // Property array length (varies by material due to material dimensions)
                // Find first empty string in array
                int prop_length = 0;
                for (int j = 3; j < data_vals.Length; j++) {
                    if (data_vals[j] == "" || data_vals[j] == "\r") {
                        break;
                    } else {
                        prop_length++;
                    }
                }

                // Convert rest of the str array to float array
                string[] elem_prop_str = new string[prop_length];  // Initialize new arr
                Array.Copy(data_vals, 3, elem_prop_str, 0, prop_length);  // Copy arr
                float[] elem_prop = Array.ConvertAll<string, float>(elem_prop_str, float.Parse); // Convert to float

                // If material doesn't exist in dictionary, then add a dictionary for this material
                if (!elemPropDict.ContainsKey(elem_mat)) {
                    elemPropDict.Add(elem_mat, new Dictionary<string, Dictionary<string, float[]>>());
                }

                // Get material dictionary
                Dictionary<string, Dictionary<string, float[]>> mat_dict = elemPropDict[elem_mat];

                // If shape doesn't exist in dictionary, then add a dictionary for this shape
                if (!mat_dict.ContainsKey(elem_shape)) {
                    mat_dict.Add(elem_shape, new Dictionary<string, float[]>());
                }

                // Get shape dictionary
                Dictionary<string, float[]> shape_dict = mat_dict[elem_shape];

                // Add elem properties
                shape_dict[elem_size] = elem_prop;

            }//End if
        }//End while
    }//End Readcsv


    // Get element properties
    public float[] getElemProp(string elem_mat, string elem_shape, string elem_size) {
        return elemPropDict[elem_mat][elem_shape][elem_size];
    }

    // Get string list of materials
    public List<String> getMatList() {
        return new List<String>(elemPropDict.Keys);
    }

    // Get string list of shapes
    public List<String> getShapeList(string elem_mat) {
        return new List<String>(elemPropDict[elem_mat].Keys);
    }

    // Get string list of sizes
    public List<String> getSizeList(string elem_mat, string elem_shape) {
        return new List<String>(elemPropDict[elem_mat][elem_shape].Keys);
    }
    #endregion

    #region Elem Mesh Dictionary Functions
    // Try to get mesh if it has already been generated. Otherwise, return null.
    public Mesh TryToGetMesh(string elem_mat, string elem_shape, string elem_size) {
        // Try to get shape dictionary
        Dictionary<string, Dictionary<string, Mesh>> elemMeshDictShape;
        if (elemMeshDict.TryGetValue(elem_mat, out elemMeshDictShape)) {

            // Try to get size dictionary
            Dictionary<string, Mesh> elemMeshDictSize;
            if (elemMeshDictShape.TryGetValue(elem_shape, out elemMeshDictSize)) {

                // Try to get Mesh
                Mesh value_mesh;
                if (elemMeshDictSize.TryGetValue(elem_size, out value_mesh)) {
                    return value_mesh;

                } else {
                    return null;
                }
            } else {
                return null;
            }
        } else {
            return null;
        }
    }

    // Add mesh to dictionary
    public void AddMeshToDict(string elem_mat, string elem_shape, string elem_size, Mesh mesh) {
        // If material doesn't exist in dictionary, then add a dictionary for this material
        if (!elemMeshDict.ContainsKey(elem_mat)) {
            elemMeshDict.Add(elem_mat, new Dictionary<string, Dictionary<string, Mesh>>());
        }

        // Get material dictionary
        Dictionary<string, Dictionary<string, Mesh>> mat_dict = elemMeshDict[elem_mat];

        // If shape doesn't exist in dictionary, then add a dictionary for this shape
        if (!mat_dict.ContainsKey(elem_shape)) {
            mat_dict.Add(elem_shape, new Dictionary<string, Mesh>());
        }

        // Get shape dictionary
        Dictionary<string, Mesh> shape_dict = mat_dict[elem_shape];

        // Add elem properties
        shape_dict[elem_size] = mesh;
    }
    #endregion
}
