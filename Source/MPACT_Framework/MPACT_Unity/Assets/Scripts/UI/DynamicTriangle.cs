using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class TriangleUI : Graphic
{
    [SerializeField] private float lineThickness = 1; 
    [SerializeField] private RectTransform pointA;
    [SerializeField] private RectTransform pointB;
    [SerializeField] private RectTransform pointC;
    [SerializeField] private Slider slider;

    protected override void Awake()
    {
        base.Awake();
        this.material = defaultMaterial;
    }
    
    protected override void Start()
    {
        base.Start();
        UpdateTriangle();
    }
    
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;

        // Add vertices for the triangle itself
        vertex.position = GetLocalPosition(pointA);
        vh.AddVert(vertex);

        vertex.position = GetLocalPosition(pointB);
        vh.AddVert(vertex);

        vertex.position = GetLocalPosition(pointC);
        vh.AddVert(vertex);

        vh.AddTriangle(0, 1, 2);
    
        // Borders (lines) 
        vertex.color = Color.black;

        // Border A-B
        AddLine(vh, GetLocalPosition(pointA), GetLocalPosition(pointB), vertex.color);

        // Border B-C
        AddLine(vh, GetLocalPosition(pointB), GetLocalPosition(pointC), vertex.color);

        // Border C-A
        AddLine(vh, GetLocalPosition(pointC), GetLocalPosition(pointA), vertex.color);
    }

    void AddLine(VertexHelper vh, Vector2 start, Vector2 end, Color color)
    {
        Vector2 perpendicular = (start - end).normalized * lineThickness;
        Vector2 left = new Vector2(-perpendicular.y, perpendicular.x);
        Vector2 right = new Vector2(perpendicular.y, -perpendicular.x);

        AddQuad(vh, start + left, start + right, end + right, end + left, color);
    }

    void AddQuad(VertexHelper vh, Vector2 bl, Vector2 br, Vector2 tr, Vector2 tl, Color color)
    {
        UIVertex vert = UIVertex.simpleVert;
        vert.color = color;

        vert.position = bl;
        vh.AddVert(vert);
        vert.position = br;
        vh.AddVert(vert);
        vert.position = tr;
        vh.AddVert(vert);
        vert.position = tl;
        vh.AddVert(vert);

        int startIndex = vh.currentVertCount - 4;
        vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        vh.AddTriangle(startIndex, startIndex + 2, startIndex + 3);
    }

    private Vector2 GetLocalPosition(RectTransform rectTransform)
    {
        return rectTransform.localPosition + (Vector3)rectTransform.rect.center;
    }

    public void UpdateTriangle()
    {
        float goal = pointA.GetComponent<DraggablePoint>().Value;
        float group = pointB.GetComponent<DraggablePoint>().Value;
        float interact = pointC.GetComponent<DraggablePoint>().Value;
        float connectivity = slider.value;
        color = new Color(goal, interact, group, 0.4f + (connectivity / 2f));
        SetAllDirty();
    }
}
