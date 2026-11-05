using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class ArrowElement : VisualElement
{
    bool m_PointLeft;

    [UxmlAttribute]
    public bool PointLeft
    {
        get => m_PointLeft;
        set { m_PointLeft = value; MarkDirtyRepaint(); }
    }

    public ArrowElement()
    {
        style.width = 60;
        style.height = 60;
        generateVisualContent += OnGenerate;
    }

    void OnGenerate(MeshGenerationContext ctx)
    {
        var p = ctx.painter2D;
        float w = contentRect.width, h = contentRect.height;

        p.fillColor = Color.white;
        p.BeginPath();
        if (PointLeft)
        {
            p.MoveTo(new Vector2(w * 0.75f, h * 0.1f));
            p.LineTo(new Vector2(w * 0.25f, h * 0.5f));
            p.LineTo(new Vector2(w * 0.75f, h * 0.9f));
        }
        else
        {
            p.MoveTo(new Vector2(w * 0.25f, h * 0.1f));
            p.LineTo(new Vector2(w * 0.75f, h * 0.5f));
            p.LineTo(new Vector2(w * 0.25f, h * 0.9f));
        }
        p.ClosePath();
        p.Fill();
    }
}