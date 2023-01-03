using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class electricField : MonoBehaviour
{
    public GameObject arrow;
    public int gridResolution = 5;
    public float gridWidth = 4;
    public static float maxDistance;
    public int fieldLineResolution = 5;
    static GameObject[] arrows;
    public GameObject test;
    public GameObject charge;
    public float spawnInterval = 0.5f;
    public static float spawnInterval1 = 0.5f;
    public float lifeTime = 0.5f;
    public static float lifeTime1 = 0.5f;
    public GameObject tracer;
    Vector3[] sphereTracerSpawnPositions;
    public int tracerCount;
    public static Vector3[] backgroundField;
    public static float maxField = 0;
    public static float minField;
/*    public Color startColor = Color.black;
    public Color endColor = Color.black;*/
    public static bool fieldLinesToggle = false;

    public float px;
    public float py;
    public float pz;

    public float vecx;
    public float vecy;
    public float vecz;

    // Initialize the electric field grid in the scene
    void Start()
    {
        // Think this is the max distance before a particle is removed (?)
        maxDistance = Mathf.Sqrt(3) * gridWidth;
        lifeTime1 = lifeTime;
        spawnInterval1 = spawnInterval;
        InvokeRepeating("invokedSpawnTracers", 0, spawnInterval);

        // Generate the grid mesh for the arrows
        Vector3[,,] grid = createGrid(gridResolution, gridWidth, transform.position);

        // Define arrow object
        arrows = new GameObject[gridResolution * gridResolution * gridResolution];

        // Populate the grid mesh (index allows for 1D selection of arrows in mesh)
        for (int i = 0, index = 0; i < grid.GetLength(0); i++)
        {
            for (int j = 0; j < grid.GetLength(0); j++)
            {
                for (int k = 0; k < grid.GetLength(0); k++)
                {
                    arrows[index] = Instantiate(arrow, grid[i, j, k], Quaternion.identity);
                    arrows[index].transform.localScale = Vector3.zero;
                    index++;
                }
            }
        }

        // Determine where the tracer is spawned (based on the "charge" sphere attached to controller)
        sphereTracerSpawnPositions = findMeshTracerSpawnPositions(charge);

        // Initialize some prescribed background static field to the mesh
        backgroundField = new Vector3[arrows.Length];
        for (int i = 0; i < backgroundField.Length; i++)
        {
            px = arrows[i].transform.position[0];
            py = arrows[i].transform.position[1];
            pz = arrows[i].transform.position[2];

            vecx = 4 * (2 * px * pz - py * (px * px + py * py + pz * pz - 1)) / ((1 + px * px + py * py + pz * pz) * (1 + px * px + py * py + pz * pz));
            vecy = 4 * (2 * py * pz + px * (px * px + py * py + pz * pz - 1)) / ((1 + px * px + py * py + pz * pz) * (1 + px * px + py * py + pz * pz));
            vecz = 1 - 8 * (px * px + py * py) / ((1 + px * px + py * py + pz * pz) * (1 + px * px + py * py + pz * pz));
                
            backgroundField[i][0] = vecx;
            backgroundField[i][1] = vecy;
            backgroundField[i][2] = vecz;
        }

    }

    
    // Method to update the background field
    /*
    public static void updateBackgroundField()
    {
        Vector3[] fields = new Vector3[arrows.Length];

        for (int i = 0; i < arrows.Length; i++)
        {
            fields[i] = testParticle.calculateStaticField(arrows[i].transform.position);
        }

        backgroundField = fields;
        
        potential.updateBackgroundPotentials();


    }
    */
    

    // Method to invoke tracer spawn
    void invokedSpawnTracers()
    {
        spawnTracers(tracer, sphereTracerSpawnPositions, lifeTime, tracerCount);
    }

    // Method to spawn tracer particles (for the discrete collection of point charges)
    public static void spawnTracers(GameObject tracer, Vector3[] tracerSpawnPositions, float tracerLifetime, int tracerCount)
    {
        if (fieldLinesToggle)
        {
            GameObject[] charges = GameObject.FindGameObjectsWithTag("Charge");
            
            for (int i = 0; i < charges.Length; i++)
            {
                if (charges[i].GetComponent<Charges>().charge > 0 && !charges[i].Equals(leftControl.model) && !charges[i].Equals(rightControl.objectInHand) && !charges[i].transform.parent)
                {
                    int interval = (tracerSpawnPositions.Length) / tracerCount;

                    for (int z = 0; z < tracerCount; z++)
                    {
                        int j = z * interval;
                        GameObject tracerInstance = Instantiate(tracer, charges[i].transform.position + tracerSpawnPositions[j], Quaternion.identity, charges[i].transform);
                        Destroy(tracerInstance, tracerLifetime);
                    }
                }
            }
        }
    }

    // Method to spawn tracer particles (for the extended body charge distributions)
    public static void spawnTracersForExtendedObjects(GameObject tracer, Vector3[] tracerSpawnPositions, float tracerLifetime, int tracerCount, GameObject gameObject)
    {
        if (fieldLinesToggle)
        {
            if (gameObject.GetComponent<extendedObject>().charge > 0 && !gameObject.Equals(leftControl.model) && !gameObject.Equals(rightControl.objectInHand))
            {
                int interval = (tracerSpawnPositions.Length) / tracerCount;
                for (int z = 0; z < tracerCount; z++)
                {
                    int j = z * interval;
                    GameObject tracerInstance = Instantiate(tracer, tracerSpawnPositions[j], Quaternion.identity);
                    Destroy(tracerInstance, tracerLifetime);
                }
            }
        }
    }

    // Method to find the position to initialize the field line tracers?
    public static Vector3[] findMeshTracerSpawnPositions(GameObject gameObject)
    {
        if (!gameObject.GetComponent<MeshFilter>().sharedMesh)
        {
            return null;
        }

        Vector3[] vertices = gameObject.GetComponent<MeshFilter>().sharedMesh.vertices;
        Vector3[] normals = gameObject.GetComponent<MeshFilter>().sharedMesh.normals;

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = gameObject.transform.TransformPoint(vertices[i]);
            vertices[i] += gameObject.transform.TransformDirection(normals[i]).normalized * 0.04f;
        }
        return vertices;
    }

    // Method to toggle on and off the field lines from positive to negative charges
    public static void toggleFieldLines()
    {
        fieldLinesToggle = !fieldLinesToggle;
    }

    // Update script that runs on the electric field object 
    void Update()
    {

        // Check if the field lines have been toggled by the user
        if (Input.GetKeyUp(KeyCode.X))
        {
            toggleFieldLines();

        }

        // Update the magnitude and direction of the arrows in the grid mesh
        // Magnitude is scaled by the minimum and maximum values of the field

        // Initialize the max value to zero
        maxField = 1;

        // Initialize the min value of the field
        minField = 0; //(backgroundField[0] + testParticle.calculateNonStaticField(arrows[0].transform.position)).magnitude;

        // Loop over grid mesh and update the direction and angle of arrows
        for (int i = 0; i < arrows.Length; i++)
        {
            Vector3 field;
            field = backgroundField[i];
            
            
            if (field.magnitude > maxField)
            {
                maxField = field.magnitude;
            }
            if (field.magnitude < minField)
            {
                minField = field.magnitude;
            }
            

            arrows[i].GetComponent<arrows>().field = field;

        }
    }

    public static Vector3[,,] createGrid(int resolution, float width, Vector3 position)
    {
        Vector3[,,] grid = new Vector3[resolution, resolution, resolution];

        float spacing = (float) width / resolution;

        for (int i = 0; i < resolution; i++)
        {
            for (int j = 0; j < resolution; j++)
            {
                for (int k = 0; k < resolution; k++)
                {
                    grid[i, j, k] = position + new Vector3(spacing * i - width/2, spacing * j - width / 2, spacing * k - width / 2);
                }
            }
        }

        return grid;
    }

    Vector3[,] createSphereicalGrid(int resolution, float radius, Vector3 position)
    {
        Vector3[,] grid = new Vector3[resolution + 1, resolution + 1];

        float spacing = 2 * Mathf.PI / resolution;
        for (int i = 0; i < grid.GetLength(0); i++)
        {
            for (int j = 0; j < grid.GetLength(1); j++)
            {
                Vector3 offset = new Vector3(radius * Mathf.Sin(spacing * j / 2.0f) * Mathf.Cos(spacing * i), radius * Mathf.Sin(spacing * j / 2.0f) * Mathf.Sin(spacing * i), radius * Mathf.Cos(spacing * j / 2.0f));
                grid[i, j] = position + offset;
            }
        }

        return grid;
    }

}
