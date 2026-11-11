using UnityEngine;

public class LaserBeam : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private BoxCollider2D boxCollider2D;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        boxCollider2D = GetComponent<BoxCollider2D>();
    }

    void Update()
    {
        if (lineRenderer == null || boxCollider2D == null) return;

        lineRenderer.SetPosition(0, transform.position);

        Vector3 endPosition = transform.position + (Vector3.down * 20);
        lineRenderer.SetPosition(1, endPosition);

        boxCollider2D.size = new Vector2(boxCollider2D.size.x, 20f);

        boxCollider2D.offset = new Vector2(0, -10);
    }
}
