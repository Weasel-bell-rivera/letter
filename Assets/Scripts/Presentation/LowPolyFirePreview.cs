using UnityEngine;

namespace W1.Presentation
{
    /// <summary>
    /// Self-contained low-poly fire study. It deliberately owns no gameplay state,
    /// collision, damage, lighting, or reset behavior.
    /// </summary>
    [ExecuteAlways]
    public sealed class LowPolyFirePreview : MonoBehaviour
    {
        private const string PreviewShaderName = "W1/Low Poly Fire Preview";

        [Header("Shape")]
        [SerializeField, Min(0.1f)] private float flameWidth = 2.5f;
        [SerializeField, Min(0.1f)] private float flameHeight = 4.8f;
        [SerializeField, Range(0f, 0.4f)] private float sway = 0.16f;

        [Header("Animation")]
        [SerializeField, Min(0f)] private float animationSpeed = 1.2f;
        [SerializeField, Range(0, 40)] private int sparkCount = 18;

        private Material previewMaterial;

        private static readonly Vector2[] OuterContour =
        {
            new(-0.82f, 0.02f), new(-0.62f, 0.28f), new(-0.50f, 0.52f),
            new(-0.58f, 0.70f), new(-0.36f, 0.90f), new(-0.42f, 1.16f),
            new(-0.18f, 1.38f), new(-0.08f, 1.66f), new(-0.10f, 2.18f),
            new(0.25f, 1.90f), new(0.46f, 1.55f), new(0.38f, 1.20f),
            new(0.60f, 0.78f), new(0.72f, 0.48f), new(0.66f, 0.10f),
            new(0.08f, -0.06f)
        };

        private void OnEnable()
        {
            EnsureMaterial();
        }

        private void OnDisable()
        {
            if (previewMaterial == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(previewMaterial);
            }
            else
            {
                DestroyImmediate(previewMaterial);
            }
        }

        private void OnValidate()
        {
            flameWidth = Mathf.Max(0.1f, flameWidth);
            flameHeight = Mathf.Max(0.1f, flameHeight);
            sparkCount = Mathf.Clamp(sparkCount, 0, 40);
        }

        private void OnRenderObject()
        {
            EnsureMaterial();
            if (previewMaterial == null || Camera.current == null)
            {
                return;
            }

            float time = GetAnimationTime() * animationSpeed;
            previewMaterial.SetPass(0);

            GL.PushMatrix();
            GL.MultMatrix(transform.localToWorldMatrix);

            DrawBackdrop();
            DrawGlow(time);
            DrawCoalBed(time);
            DrawFlameLayer(time, 1f, 0f,
                new Color(1f, 0.16f, 0.015f, 0.92f),
                new Color(1f, 0.52f, 0.025f, 0.98f));
            DrawFlameLayer(time, 0.72f, 1.7f,
                new Color(1f, 0.42f, 0.015f, 0.95f),
                new Color(1f, 0.80f, 0.02f, 1f));
            DrawFlameLayer(time, 0.43f, 3.4f,
                new Color(1f, 0.78f, 0.04f, 0.98f),
                new Color(1f, 0.98f, 0.34f, 1f));
            DrawSparks(time);

            GL.PopMatrix();
        }

        private void EnsureMaterial()
        {
            if (previewMaterial != null)
            {
                return;
            }

            Shader shader = Shader.Find(PreviewShaderName);
            if (shader == null)
            {
                return;
            }

            previewMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private static float GetAnimationTime()
        {
            return Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
        }

        private static void DrawBackdrop()
        {
            GL.Begin(GL.TRIANGLES);
            Vertex(new Vector2(-20f, -12f), new Color(0.012f, 0.003f, 0.002f, 1f));
            Vertex(new Vector2(-20f, 12f), new Color(0.055f, 0.006f, 0.003f, 1f));
            Vertex(new Vector2(20f, 12f), new Color(0.018f, 0.002f, 0.002f, 1f));
            Vertex(new Vector2(-20f, -12f), new Color(0.012f, 0.003f, 0.002f, 1f));
            Vertex(new Vector2(20f, 12f), new Color(0.018f, 0.002f, 0.002f, 1f));
            Vertex(new Vector2(20f, -12f), new Color(0.004f, 0.001f, 0.001f, 1f));
            GL.End();
        }

        private void DrawGlow(float time)
        {
            float pulse = 1f + Mathf.Sin(time * 4.1f) * 0.055f;
            DrawEllipse(new Vector2(0f, 1.25f), new Vector2(4.3f, 4.1f) * pulse,
                new Color(1f, 0.12f, 0f, 0.11f), 28);
            DrawEllipse(new Vector2(0f, 0.75f), new Vector2(2.9f, 2.45f) * pulse,
                new Color(1f, 0.38f, 0f, 0.15f), 24);
            DrawEllipse(new Vector2(0f, 0.42f), new Vector2(1.95f, 1.35f) * pulse,
                new Color(1f, 0.78f, 0.04f, 0.20f), 20);
        }

        private static void DrawCoalBed(float time)
        {
            float pulse = 0.82f + Mathf.Sin(time * 3.7f) * 0.08f;
            GL.Begin(GL.TRIANGLES);
            Triangle(new Vector2(-1.35f, -0.08f), new Vector2(-0.72f, 0.20f), new Vector2(-0.12f, -0.14f),
                new Color(0.12f, 0.012f, 0.005f, 1f));
            Triangle(new Vector2(-0.35f, -0.16f), new Vector2(0.42f, 0.18f), new Vector2(1.24f, -0.12f),
                new Color(0.18f, 0.018f, 0.004f, 1f));
            Triangle(new Vector2(-0.72f, -0.03f), new Vector2(0.04f, 0.28f), new Vector2(0.72f, -0.04f),
                new Color(1f, 0.18f * pulse, 0.005f, 0.88f));
            GL.End();
        }

        private void DrawFlameLayer(float time, float layerScale, float phase, Color edge, Color core)
        {
            Vector2 center = new(0f, flameHeight * (0.35f + 0.04f * layerScale));
            float layerWidth = flameWidth * layerScale;
            float layerHeight = flameHeight * layerScale;
            float layerOffset = (1f - layerScale) * -0.03f;

            GL.Begin(GL.TRIANGLES);
            for (int i = 0; i < OuterContour.Length; i++)
            {
                int next = (i + 1) % OuterContour.Length;
                float facet = 0.82f + Hash01(i * 5.17f + phase) * 0.25f;
                Color facetCore = new(core.r * facet, core.g * facet, core.b * facet, core.a);

                Vertex(center, facetCore);
                Vertex(DeformPoint(OuterContour[i], i, time, phase, layerWidth, layerHeight, layerOffset), edge);
                Vertex(DeformPoint(OuterContour[next], next, time, phase, layerWidth, layerHeight, layerOffset), edge);
            }
            GL.End();
        }

        private Vector2 DeformPoint(Vector2 point, int index, float time, float phase,
            float width, float height, float verticalOffset)
        {
            float normalizedHeight = Mathf.InverseLerp(-0.1f, 2.2f, point.y);
            float steppedTime = Mathf.Floor(time * 10f) * 0.1f;
            float lateral = Mathf.Sin(steppedTime * 2.7f + index * 1.91f + phase) * sway * normalizedHeight;
            float flicker = 1f + Mathf.Sin(steppedTime * 3.9f + index * 0.73f + phase) * 0.035f;
            return new Vector2(point.x * width + lateral, point.y * height * 0.48f * flicker + verticalOffset);
        }

        private void DrawSparks(float time)
        {
            GL.Begin(GL.TRIANGLES);
            for (int i = 0; i < sparkCount; i++)
            {
                float seed = i + 1f;
                float progress = Mathf.Repeat(time * Mathf.Lerp(0.13f, 0.29f, Hash01(seed * 4.7f))
                    + Hash01(seed * 9.1f), 1f);
                float fade = Mathf.Sin(progress * Mathf.PI);
                float y = 0.55f + progress * 5.0f;
                float spread = Mathf.Lerp(0.35f, 1.2f, progress);
                float x = (Hash01(seed * 2.3f) - 0.5f) * spread
                    + Mathf.Sin(time * 2.1f + seed) * 0.15f * progress;
                float size = Mathf.Lerp(0.14f, 0.035f, progress) * Mathf.Lerp(0.65f, 1.3f, Hash01(seed));
                Color color = Color.Lerp(new Color(1f, 0.72f, 0.03f, fade),
                    new Color(1f, 0.035f, 0.005f, fade * 0.75f), progress);

                Vector2 center = new(x, y);
                float tilt = (Hash01(seed * 8.2f) - 0.5f) * size;
                Vertex(center + new Vector2(-size, -size * 0.65f), color);
                Vertex(center + new Vector2(size + tilt, -size * 0.25f), color);
                Vertex(center + new Vector2(tilt, size * 1.45f), color);
            }
            GL.End();
        }

        private static void DrawEllipse(Vector2 center, Vector2 radius, Color centerColor, int segments)
        {
            Color edgeColor = new(centerColor.r, centerColor.g, centerColor.b, 0f);
            GL.Begin(GL.TRIANGLES);
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                float b = (i + 1) * Mathf.PI * 2f / segments;
                Vertex(center, centerColor);
                Vertex(center + new Vector2(Mathf.Cos(a) * radius.x, Mathf.Sin(a) * radius.y), edgeColor);
                Vertex(center + new Vector2(Mathf.Cos(b) * radius.x, Mathf.Sin(b) * radius.y), edgeColor);
            }
            GL.End();
        }

        private static void Triangle(Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            Vertex(a, color);
            Vertex(b, color);
            Vertex(c, color);
        }

        private static void Vertex(Vector2 point, Color color)
        {
            GL.Color(color);
            GL.Vertex3(point.x, point.y, 0f);
        }

        private static float Hash01(float value)
        {
            return Mathf.Repeat(Mathf.Sin(value * 12.9898f) * 43758.5453f, 1f);
        }
    }
}
