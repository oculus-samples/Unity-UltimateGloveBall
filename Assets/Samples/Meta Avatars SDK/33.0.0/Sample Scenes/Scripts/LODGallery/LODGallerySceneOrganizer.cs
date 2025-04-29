#nullable enable

using UnityEngine;

/**
 * This script arranges game objects in an arc pattern with multiple rows,
 * calculating positions and rotations based on parameters. It provides a 2D
 * array of game object containers and visualizes the arrangement with gizmos.
 */
public class LODGallerySceneOrganizer : MonoBehaviour
{
    [SerializeField] private float radius = 4f;
    [SerializeField] private float startAngle = -40f;
    [SerializeField] private float endAngle = 40f;
    [SerializeField, Range(1, 10)]
    private int countPerRow = 5;

    [SerializeField, Range(1, 10)]
    private int rowCount = 3;

    [SerializeField]
    private Vector3 spacingBetweenRows = new Vector3(0.0f, 1.5f, 0.0f);

    [SerializeField] private bool gizmosEnabled = true;

    private Vector3 OffsetFromTarget(float currentAngle)
    {
        float x = Mathf.Sin(Mathf.Deg2Rad * currentAngle) * radius;
        float y = 0f;
        float z = Mathf.Cos(Mathf.Deg2Rad * currentAngle) * radius;

        Vector3 offset = new Vector3(x, y, z);

        return offset;
    }

    public GameObject[][] GetArrangedGameObjects()
    {
        GameObject[][] containers = new GameObject[rowCount][];

        for (int row = 0; row < rowCount; row++)
        {
            containers[row] = new GameObject[countPerRow];

            float totalAngle = endAngle - startAngle;
            float angleStep = totalAngle / (countPerRow - 1);

            for (int rowCounter = 0; rowCounter < countPerRow; rowCounter++)
            {
                Vector3 offset = OffsetFromTarget(startAngle + rowCounter * angleStep);
                Vector3 position = transform.position + offset + (row * spacingBetweenRows);

                GameObject obj = new GameObject($"Container[{row}][{rowCounter}]");
                obj.transform.position = position;
                obj.transform.rotation = Quaternion.LookRotation(transform.position - position);

                containers[row][rowCounter] = obj;
            }
        }

        return containers;
    }

    private void OnDrawGizmos()
    {
        if (gizmosEnabled)
        {
            Gizmos.color = Color.yellow; ;
            Gizmos.DrawWireSphere(base.transform.position, 2f);

            Gizmos.color = Color.red;
            for (int row = 0; row < rowCount; row++)
            {
                float totalAngle = endAngle - startAngle;
                float angleStep = totalAngle / (countPerRow - 1);

                for (int rowCounter = 0; rowCounter < countPerRow; rowCounter++)
                {
                    Vector3 offset = OffsetFromTarget(startAngle + rowCounter * angleStep);
                    Vector3 position = transform.position + offset + (row * spacingBetweenRows);

                    Gizmos.DrawWireSphere(position, 1f);
                }
            }
        }
    }
}
