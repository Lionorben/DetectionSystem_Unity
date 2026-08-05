using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AISensor : MonoBehaviour
{
    [SerializeField] private bool showGizmos = false;
    [SerializeField] private float distance = 10f;
    [SerializeField] private float angle = 30f;
    [SerializeField] private float height = 1.0f;
    [SerializeField] private Color meshColor = Color.red;
    [SerializeField] private int scanFrequency = 30;
    [SerializeField] private LayerMask layers;
    [SerializeField] private bool alwaysCheckOcclusion = true;
    [SerializeField] private LayerMask occlusionLayers;

    [Header("Visualization")]
    [SerializeField] private MeshFilter viewMeshFilter;
    [SerializeField] private float meshResolution = 1f;
    [SerializeField] private int edgeResolveIterations = 4;
    [SerializeField] private float edgeDstThreshold = 0.5f;

    public float Distance { get => distance; set => distance = value; }
    public float Angle { get => angle; set => angle = value; }
    public float Height { get => height; set => height = value; }

    public struct ViewCastInfo
    {
        public bool hit;
        public Vector3 point;
        public float dst;
        public float angle;

        public ViewCastInfo(bool _hit, Vector3 _point, float _dst, float _angle)
        {
            hit = _hit;
            point = _point;
            dst = _dst;
            angle = _angle;
        }
    }

    public struct EdgeInfo
    {
        public Vector3 pointA;
        public Vector3 pointB;

        public EdgeInfo(Vector3 _pointA, Vector3 _pointB)
        {
            pointA = _pointA;
            pointB = _pointB;
        }
    }

    private int overlapSphereID = -1;

    private List<GameObject> objectsInSight = new List<GameObject>();
    public List<GameObject> ObjectsInSight => objectsInSight;

    private List<GameObject> objectsInRadius = new List<GameObject>();
    public List<GameObject> ObjectsInRadius => objectsInRadius;

    private Collider[] colliders;
    private Mesh mesh;
    private int count;
    private float scanInterval;
    private float scanTimer;
    private Material viewMaterial;

    void Start()
    {
        overlapSphereID = -1;
        scanInterval = 1.0f / scanFrequency;
        
        mesh = new Mesh();
        mesh.name = "View Mesh";
        if (viewMeshFilter != null)
        {
            viewMeshFilter.mesh = mesh;
            MeshRenderer renderer = viewMeshFilter.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                viewMaterial = renderer.material;
                viewMaterial.color = meshColor;
            }
        }
    }

    public void UpdateMeshColor(Color color)
    {
        meshColor = color;
        if (viewMaterial != null)
        {
            viewMaterial.color = color;
        }
    }

    private bool JobsEnabled = true;
    void Update()
    {
        if (JobsEnabled)
        {
            PrepareScan();
            if (AISensorManager.Instance.OverlapSphereJobHandle.IsCompleted)
            {
                Scan();
            }
        }
        else
        {
            scanTimer -= Time.deltaTime;
            if (scanTimer < 0)
            {
                scanTimer += scanInterval;
                Scan();
            }
        }
    }

    void LateUpdate()
    {
        DrawFieldOfView();
    }

    /// <summary>
    /// Dynamically constructs the field of view visualization mesh based on the sensor's parameters,
    /// raycasting outward to detect occlusions and creating geometry to match the visible area.
    /// </summary>
    void DrawFieldOfView()
    {
        int stepCount = Mathf.Max(1, Mathf.RoundToInt(angle * 2 * meshResolution));
        float stepAngleSize = (angle * 2) / stepCount;

        List<Vector3> viewPoints = new List<Vector3>();
        ViewCastInfo oldViewCast = new ViewCastInfo();

        for (int i = 0; i <= stepCount; i++)
        {
            float currentAngle = -angle + stepAngleSize * i;
            ViewCastInfo newViewCast = ViewCast(currentAngle);

            if (i > 0)
            {
                bool edgeDstThresholdExceeded = Mathf.Abs(oldViewCast.dst - newViewCast.dst) > edgeDstThreshold;
                if (oldViewCast.hit != newViewCast.hit || (oldViewCast.hit && newViewCast.hit && edgeDstThresholdExceeded))
                {
                    EdgeInfo edge = FindEdge(oldViewCast, newViewCast);
                    if (edge.pointA != Vector3.zero)
                    {
                        Vector3 localA = transform.InverseTransformPoint(edge.pointA);
                        localA.y = 0;
                        viewPoints.Add(localA);
                    }
                    if (edge.pointB != Vector3.zero)
                    {
                        Vector3 localB = transform.InverseTransformPoint(edge.pointB);
                        localB.y = 0;
                        viewPoints.Add(localB);
                    }
                }
            }

            Vector3 localHit = transform.InverseTransformPoint(newViewCast.point);
            localHit.y = 0;
            viewPoints.Add(localHit);

            oldViewCast = newViewCast;
        }

        if (viewPoints.Count < 2) return;

        int segments = viewPoints.Count - 1;
        int numTriangles = (segments * 4) + 2 + 2;
        int numVertices = numTriangles * 3;

        Vector3[] vertices = new Vector3[numVertices];
        int[] triangles = new int[numVertices];

        Vector3 bottomCenter = Vector3.down * (height / 2);
        Vector3 topCenter = bottomCenter + Vector3.up * height;

        int vert = 0;

        // left side
        Vector3 bottomLeftFirst = bottomCenter + viewPoints[0];
        Vector3 topLeftFirst = bottomLeftFirst + Vector3.up * height;

        vertices[vert++] = bottomCenter;
        vertices[vert++] = bottomLeftFirst;
        vertices[vert++] = topLeftFirst;

        vertices[vert++] = topLeftFirst;
        vertices[vert++] = topCenter;
        vertices[vert++] = bottomCenter;

        // right side
        Vector3 bottomRightLast = bottomCenter + viewPoints[segments];
        Vector3 topRightLast = bottomRightLast + Vector3.up * height;

        vertices[vert++] = bottomCenter;
        vertices[vert++] = topCenter;
        vertices[vert++] = topRightLast;

        vertices[vert++] = topRightLast;
        vertices[vert++] = bottomRightLast;
        vertices[vert++] = bottomCenter;

        for (int i = 0; i < segments; i++)
        {
            Vector3 bottomLeft = bottomCenter + viewPoints[i];
            Vector3 bottomRight = bottomCenter + viewPoints[i + 1];
            Vector3 topLeft = bottomLeft + Vector3.up * height;
            Vector3 topRight = bottomRight + Vector3.up * height;

            //far side
            vertices[vert++] = bottomLeft;
            vertices[vert++] = bottomRight;
            vertices[vert++] = topRight;

            vertices[vert++] = topRight;
            vertices[vert++] = topLeft;
            vertices[vert++] = bottomLeft;

            //top
            vertices[vert++] = topCenter;
            vertices[vert++] = topLeft;
            vertices[vert++] = topRight;

            //bottom
            vertices[vert++] = bottomCenter;
            vertices[vert++] = bottomRight;
            vertices[vert++] = bottomLeft;
        }

        for (int i = 0; i < numVertices; i++)
        {
            triangles[i] = i;
        }

        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "View Mesh";
        }
        
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        if (viewMeshFilter != null && viewMeshFilter.sharedMesh != mesh)
        {
            viewMeshFilter.mesh = mesh;
        }
    }

    /// <summary>
    /// Casts a ray at a specific angle to determine if there's an obstruction within the sensor's distance.
    /// </summary>
    ViewCastInfo ViewCast(float globalAngle)
    {
        Vector3 dir = Quaternion.Euler(0, globalAngle, 0) * transform.forward;
        Vector3 origin = transform.position;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, distance, occlusionLayers))
        {
            return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
        }
        else
        {
            return new ViewCastInfo(false, origin + dir * distance, distance, globalAngle);
        }
    }

    /// <summary>
    /// Uses iterative binary search to find the exact edge of an occluding object,
    /// allowing the field of view mesh to wrap tightly around corners and obstacles.
    /// </summary>
    EdgeInfo FindEdge(ViewCastInfo minViewCast, ViewCastInfo maxViewCast)
    {
        float minAngle = minViewCast.angle;
        float maxAngle = maxViewCast.angle;
        Vector3 minPoint = Vector3.zero;
        Vector3 maxPoint = Vector3.zero;

        for (int i = 0; i < edgeResolveIterations; i++)
        {
            float angle = (minAngle + maxAngle) / 2;
            ViewCastInfo newViewCast = ViewCast(angle);

            bool edgeDstThresholdExceeded = Mathf.Abs(minViewCast.dst - newViewCast.dst) > edgeDstThreshold;
            if (newViewCast.hit == minViewCast.hit && !edgeDstThresholdExceeded)
            {
                minAngle = angle;
                minPoint = newViewCast.point;
            }
            else
            {
                maxAngle = angle;
                maxPoint = newViewCast.point;
            }
        }

        return new EdgeInfo(minPoint, maxPoint);
    }

    /// <summary>
    /// Queues an overlap sphere command into the AISensorManager for the current frame to batch collision queries.
    /// </summary>
    private void PrepareScan()
    {
        overlapSphereID = AISensorManager.Instance.AddOverlapSphere(transform.position, distance, layers, overlapSphereID);
    }

    /// <summary>
    /// Processes the results of the overlap sphere query to determine which objects are within radius,
    /// and further checks which of those are directly within the line of sight.
    /// </summary>
    private void Scan()
    {
        if (JobsEnabled)
        {
            colliders = AISensorManager.Instance.GetOverlapSphereResults(overlapSphereID);
            count = colliders == null ? 0 : colliders.Length;
        }
        else
        {
            count = Physics.OverlapSphereNonAlloc(transform.position, distance, colliders, layers, QueryTriggerInteraction.Collide);
        }

        objectsInSight.Clear();
        ObjectsInRadius.Clear();
        for (int i = 0; i < count; i++)
        {
            if (colliders[i] == null)
            {
                return;
            }

            GameObject obj = colliders[i].gameObject;
            if (obj == gameObject)
            {
                continue;
            }
            Vector3 origin = transform.position;
            Vector3 dest = obj.transform.position;
            if (alwaysCheckOcclusion && Physics.Linecast(origin, dest, occlusionLayers)) 
            {
                //object is occluded so we wont even add them to our objects in radius
                continue;
            }

            ObjectsInRadius.Add(obj);
            if (IsInSight(obj))
            {
                objectsInSight.Add(obj);
            }
        }
    }

    private void OnDestroy()
    {
        if (overlapSphereID != -1)
            AISensorManager.Instance.RemoveOverlapSphere(overlapSphereID);
    }

    /// <summary>
    /// Determines whether a specific game object is within the sensor's line of sight.
    /// Evaluates height, angular bounds (FOV), and performs a linecast to check for occluding geometry.
    /// </summary>
    public bool IsInSight(GameObject obj)
    {
        Vector3 origin = transform.position;
        Vector3 dest = obj.transform.position;
        Vector3 direction = dest - origin;

        if (direction.y < 0 || direction.y > height)
        {
            return false;
        }

        direction.y = 0;
        float deltaAngle = Vector3.Angle(direction, transform.forward);
        if (deltaAngle > angle)
        {
            return false;
        }
        origin.y += height / 2;
        dest.y = origin.y;
        if (!alwaysCheckOcclusion && Physics.Linecast(origin, dest, occlusionLayers))
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// Creates a default, un-occluded wedge mesh representing the sensor's maximum possible field of view.
    /// </summary>
    public Mesh CreateWedgeMesh()
    {
        Mesh mesh = new Mesh();

        int segments = 10;
        int numTriangles = (segments * 4) + 2 + 2;
        int numVertices = numTriangles * 3;

        Vector3[] vertices = new Vector3[numVertices];
        int[] triangles = new int[numVertices];

        Vector3 bottomCenter = Vector3.down * (height / 2);
        Vector3 bottomLeft = bottomCenter + Quaternion.Euler(0, -angle, 0) * Vector3.forward * distance;
        Vector3 bottomRight = bottomCenter + Quaternion.Euler(0, angle, 0) * Vector3.forward * distance;

        Vector3 topCenter = bottomCenter + Vector3.up * (height);
        Vector3 topRight = bottomRight + Vector3.up * (height);
        Vector3 topLeft = bottomLeft + Vector3.up * (height);

        int vert = 0;

        //left side
        vertices[vert++] = bottomCenter;
        vertices[vert++] = bottomLeft;
        vertices[vert++] = topLeft;

        vertices[vert++] = topLeft;
        vertices[vert++] = topCenter;
        vertices[vert++] = bottomCenter;
        //right side
        vertices[vert++] = bottomCenter;
        vertices[vert++] = topCenter;
        vertices[vert++] = topRight;

        vertices[vert++] = topRight;
        vertices[vert++] = bottomRight;
        vertices[vert++] = bottomCenter;

        float currentAngle = -angle;
        float deltaAngle = (angle * 2) / segments;
        for (int i = 0; i < segments; i++)
        {

            bottomLeft = bottomCenter + Quaternion.Euler(0, currentAngle, 0) * Vector3.forward * distance;
            bottomRight = bottomCenter + Quaternion.Euler(0, currentAngle + deltaAngle, 0) * Vector3.forward * distance;


            topRight = bottomRight + Vector3.up * (height);
            topLeft = bottomLeft + Vector3.up * (height);

            //far side
            vertices[vert++] = bottomLeft;
            vertices[vert++] = bottomRight;
            vertices[vert++] = topRight;

            vertices[vert++] = topRight;
            vertices[vert++] = topLeft;
            vertices[vert++] = bottomLeft;
            //top
            vertices[vert++] = topCenter;
            vertices[vert++] = topLeft;
            vertices[vert++] = topRight;
            //bottom
            vertices[vert++] = bottomCenter;
            vertices[vert++] = bottomRight;
            vertices[vert++] = bottomLeft;

            currentAngle += deltaAngle;
        }


        for (int i = 0; i < numVertices; i++)
        {
            triangles[i] = i;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }

    private void OnValidate()
    {

        mesh = CreateWedgeMesh();


        scanInterval = 1.0f / scanFrequency;
    }

    private void OnDrawGizmos()
    {
        if (showGizmos == false)
        {
            return;
        }
        if (mesh)
        {
            Gizmos.color = meshColor;
            Gizmos.DrawMesh(mesh, transform.position, transform.rotation);
        }
        Gizmos.color = Color.green;
        foreach (var obj in objectsInSight)
        {
            Gizmos.DrawSphere(obj.transform.position, 0.2f);
        }
    }

    public int Fliter(GameObject[] buffer, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        int count = 0;
        foreach (var obj in objectsInSight)
        {
            if (obj.layer == layer)
            {
                Debug.Log("obj found");
                buffer[count++] = obj;
            }

            if (buffer.Length == count)
            {
                Debug.LogError("buffer is full");
                break; // buffer is full
            }
        }

        return count;
    }
}
