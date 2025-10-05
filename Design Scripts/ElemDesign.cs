using NUnit.Framework.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

public class ElemDesign : MonoBehaviour {
    // Element Property Excel File Path
    [SerializeField] private string elemPropFp;

    // SAP2000 End Joint Position in SAP2000 coordinate system
    [SerializeField] private Vector3 joint_i_pos_sap;
    [SerializeField] private Vector3 joint_j_pos_sap;

    // GUI Panels
    [SerializeField] private GameObject utilPanel;
    [SerializeField] private GameObject matPanel;
    [SerializeField] private GameObject shapePanel;
    [SerializeField] private GameObject sizePanel;

    // Get dropdowns
    private TMP_Dropdown matDrop;
    private TMP_Dropdown shapeDrop;
    private TMP_Dropdown sizeDrop;

    // Load Manager and Elem Property Manager
    private ElemPropManager elemPropManager;

    // Element Properties
    private string currMat;
    private string currShape;
    private string currSize;
    private float[] currProps;

    // Get parent object and end points of parent object
    private GameObject parentObj;
    [SerializeField] private GameObject point1Obj;
    [SerializeField] private GameObject point2Obj;
    private Vector3 point1;
    private Vector3 point2;

    // Line Renderers
    private LineRenderer beamRenderer;  // Beam line
    private LineRenderer deflectionLineRenderer; // Deflection line

    // Lines to use
    private GameObject originalLine;  // Deflected line
    private GameObject redLine; // Beam line

    // Deflection Mesh Game Object and transparent material
    private GameObject deflectionFillObj;
    [SerializeField] private Material fillMaterial;

    // Deflection Number of points along curve and vertical deflection scale
    [SerializeField] private int defl_num_pts = 11;
    [SerializeField] private float verticalDeflectionScale = 50;

    // Deflection Text
    [SerializeField] private GameObject deflTextCanvas;

    // Beam Mesh Obj
    private GameObject beamMeshObj;
    [SerializeField] private Material beamMeshMaterial;

    // HSS Beam
    [SerializeField] private int num_pts_curve_hss = 3;

    // Scaling from real life to unity
    private float real2unity_scale;


    // Start is called before the first frame update
    void Start() {
        // Set parent object and point locations
        parentObj = this.gameObject;
        point1 = point1Obj.transform.position;
        point2 = point2Obj.transform.position;
        //Debug.Log(parentObj);
        //Debug.Log(parentObj.transform.position);

        // Calculate real2unity scale
        float real_dist = Vector3.Distance(joint_i_pos_sap, joint_j_pos_sap);  // Length of beam in inches
        float unity_dist = Vector3.Distance(point1, point2);    // Length of beam in unity units
        real2unity_scale = unity_dist / real_dist;  // Scaling factor

        // Set up elem property manager
        elemPropManager = new ElemPropManager();
        elemPropManager.readCSV(elemPropFp);

        // Set up deflection rendering
        originalLine = GameObject.Find("Original Line");
        redLine = GameObject.Find("Red Original Line");
        createBeamLine();
        SetUpDeflection();

        // Set up beam mesh
        CreateBeamMesh();

        // Set default material, shape, and size for beginning beam mesh
        currMat = elemPropManager.getMatList()[0];
        currShape = elemPropManager.getShapeList(currMat)[0];
        currSize = elemPropManager.getSizeList(currMat, currShape)[0];

        // Get dropdowns
        matDrop = matPanel.transform.GetChild(3).GetComponent<TMP_Dropdown>();
        shapeDrop = shapePanel.transform.GetChild(3).GetComponent<TMP_Dropdown>();
        sizeDrop = sizePanel.transform.GetChild(3).GetComponent<TMP_Dropdown>();

        /*
        changeMaterial(0);
        changeShape(1);
        changeSize(0);
        */

    }

    // Update is called once per frame
    void Update() {
    }


    #region Dropdown Change Functions
    // Set up material dropdown
    public void setupDropdowns() {
        // Set up initial material dropdown
        matPanel.SetActive(true);
        matDrop.enabled = false;
        List<string> mat_list = elemPropManager.getMatList();
        matDrop.ClearOptions();
        matDrop.AddOptions(mat_list);
        matDrop.RefreshShownValue();
        matDrop.enabled = true;

        // Set up shape and size panels
        changeMaterial(0);
    }

    // Change Material
    public void changeMaterial(int mat_i) {
        // Update Material
        currMat = elemPropManager.getMatList()[mat_i];

        // Set up shape panel
        shapePanel.SetActive(true);
        shapeDrop.enabled = false;
        List<string> shape_list = elemPropManager.getShapeList(currMat);
        shapeDrop.ClearOptions();
        shapeDrop.AddOptions(shape_list);
        shapeDrop.RefreshShownValue();
        shapeDrop.enabled = true;

        changeShape(0);
    }

    // Change Shape
    public void changeShape(int shape_i) {
        // Update Shape
        currShape = elemPropManager.getShapeList(currMat)[shape_i];

        // Set up size panel
        sizePanel.SetActive(true);
        sizeDrop.enabled = false;
        List<string> size_list = elemPropManager.getSizeList(currMat, currShape);
        sizeDrop.ClearOptions();
        sizeDrop.AddOptions(size_list);
        sizeDrop.RefreshShownValue();
        sizeDrop.enabled = true;

        changeSize(0);
    }

    // Change Size
    public void changeSize(int size_i) {
        // Get new current size
        currSize = elemPropManager.getSizeList(currMat, currShape)[size_i];

        // Get Properties of current material, shape, size
        currProps = elemPropManager.getElemProp(currMat, currShape, currSize);

        // Draw original beam line
        beamRenderer.enabled = true;

        // Draw Deflection
        float[] end_pt_defl_rot_vals = currProps.Take(12).ToArray<float>();
        float max_defl = CreateDeflection(end_pt_defl_rot_vals);
        deflectionLineRenderer.enabled = true;
        deflectionFillObj.GetComponent<MeshRenderer>().enabled = true;

        // Update max deflection text
        deflTextCanvas.SetActive(true);
        TextMeshProUGUI deflectionText = deflTextCanvas.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
        decimal max_defl_rounded = Math.Round((decimal)max_defl, 3);
        deflectionText.SetText(max_defl_rounded.ToString());

        // Update utilization ratio text
        utilPanel.SetActive(true);
        TextMeshProUGUI ratioVal = utilPanel.transform.GetChild(2).gameObject.GetComponent<TextMeshProUGUI>();
        ratioVal.SetText(currProps[12].ToString());

        // Update beam mesh
        UpdateBeamMesh();
    }
    #endregion

    #region Deflection Rendering
    // Create a beam line
    private void createBeamLine() {
        // Crate beam line from the redline at the location of the parent object
        GameObject beamLine = Instantiate(redLine, parentObj.transform);
        beamLine.name = "BeamLine";

        // Get beam line renderer
        LineRenderer beamLineRenderer = beamLine.GetComponent<LineRenderer>();

        // Get end points for beam line (center +- 1/2 beam length)
        Vector3[] points2 = { point1, point2 };

        // Draw line, disable it, and set variable
        beamLineRenderer.SetPositions(points2);
        beamLineRenderer.enabled = false;
        beamRenderer = beamLineRenderer;
    }

    // Set up deflection line
    private void SetUpDeflection() {
        // Create Deflection Line
        GameObject deflectionLine = Instantiate(originalLine, parentObj.transform);
        deflectionLine.name = "Deflection";

        // Get deflection line renderer and set it to global variable
        deflectionLineRenderer = deflectionLine.GetComponent<LineRenderer>();

        // Create the mesh
        // Make mesh fill game object and set up the material and create a new mesh for it
        deflectionFillObj = new GameObject("Deflection Fill");
        MeshRenderer deflectionMeshRenderer = deflectionFillObj.AddComponent<MeshRenderer>();
        deflectionMeshRenderer.material = fillMaterial;
        MeshFilter meshFilter = deflectionFillObj.AddComponent<MeshFilter>();
    }

    // Create deflection line
    private float CreateDeflection(float[] end_pt_defl_rot_vals) {
        // Create deflection mesh
        DeflMesh dm = new DeflMesh(point1, point2, end_pt_defl_rot_vals, joint_i_pos_sap, joint_j_pos_sap, verticalDeflectionScale, defl_num_pts, real2unity_scale);
        var (deflectionPoints, mesh, max_defl) = dm.CreateDeflMesh();

        // Set position count, position, and enable it to true
        deflectionLineRenderer.positionCount = defl_num_pts;
        deflectionLineRenderer.SetPositions(deflectionPoints);
        deflectionLineRenderer.enabled = false;

        // Set up deflection mesh
        //AssetDatabase.CreateAsset(mesh, "Assets/hello.asset");
        deflectionFillObj.GetComponent<MeshFilter>().mesh = mesh;
        deflectionFillObj.GetComponent<MeshRenderer>().enabled = false;

        // Return max deflection
        return max_defl;
    }
    #endregion

    #region Beam Mesh Rendering
    private void CreateBeamMesh() {
        // Turn off original beam mesh
        parentObj.transform.GetChild(0).gameObject.SetActive(false);

        // Make mesh fill game object and set up the material and create a new mesh for it
        beamMeshObj = new GameObject("beamMeshObj");
        MeshRenderer beamMeshRenderer = beamMeshObj.AddComponent<MeshRenderer>();
        beamMeshRenderer.material = beamMeshMaterial;
        MeshFilter meshFilter = beamMeshObj.AddComponent<MeshFilter>();
        beamMeshRenderer.enabled = false;
    }

    private void UpdateBeamMesh() {
        // Turn off beam mesh renderer
        beamMeshObj.GetComponent<MeshRenderer>().enabled = false;
        beamMeshObj.GetComponent<MeshFilter>().mesh.Clear();

        // Initialize Mesh
        Mesh new_mesh = new Mesh();

        // Try to get mesh if it has already been generated
        Mesh mesh_val = elemPropManager.TryToGetMesh(currMat, currShape, currSize);

        if (mesh_val != null) {
            new_mesh = mesh_val;

        // Otherwise, generate the new mesh
        } else {
            // If shape is W
            if (currShape == "W") {
                // Get dimensions
                float w = currProps[13] * real2unity_scale;
                float h = currProps[14] * real2unity_scale;
                float tf = currProps[15] * real2unity_scale;
                float tw = currProps[16] * real2unity_scale;

                // Create w mesh
                WMesh wm = new WMesh(point1, point2, w, h, tf, tw);
                new_mesh = wm.createWMesh();


                // If shape is an HSS
            } else if (currShape == "HSS") {
                // Get dimensions
                float w = currProps[13] * real2unity_scale;
                float h = currProps[14] * real2unity_scale;
                float t = currProps[15] * real2unity_scale;

                // Create HSS mesh
                HSSMesh hm = new HSSMesh(point1, point2, w, h, t, num_pts_curve_hss);
                new_mesh = hm.createHSSMesh();


                // If shape is built up I beam
            } else if (currShape == "Built up I Beam") {
                // Get W beam shape
                int w_beam_size_index = (int)currProps[13];
                string w_beam_size = elemPropManager.getSizeList("Steel", "W")[w_beam_size_index];
                float[] w_beam_props = elemPropManager.getElemProp("Steel", "W", w_beam_size);

                // Get W beam dimensions
                float w = w_beam_props[13] * real2unity_scale;
                float h = w_beam_props[14] * real2unity_scale;
                float tf = w_beam_props[15] * real2unity_scale;
                float tw = w_beam_props[16] * real2unity_scale;

                // Get plate height
                float h_pl = currProps[14] * real2unity_scale;

                // Create Built Up W Mesh
                BuiltUpWMesh buwm = new BuiltUpWMesh(point1, point2, w, h, tf, tw, h_pl);
                new_mesh = buwm.createBuiltUpWMesh();
            }

            // Add mesh to element dictionary, so we don't have to generate it next time
            elemPropManager.AddMeshToDict(currMat, currShape, currSize, new_mesh);
        }

        //AssetDatabase.CreateAsset(new_mesh, "Assets/hello.asset");

        // Update beam mesh and turn on renderer
        beamMeshObj.GetComponent<MeshFilter>().mesh = new_mesh;
        beamMeshObj.GetComponent<MeshRenderer>().enabled = true;
    }
    #endregion
}
