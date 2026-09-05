using UnityEngine;
using UnityEngine.Rendering;

namespace W1.Presentation
{
    /// <summary>
    /// Ten-tile-wide low-poly lava visual study for placement below a Tilemap.
    /// This component is presentation-only: it creates no collider, hazard, or gameplay state.
    /// </summary>
    [ExecuteAlways]
    public sealed class LowPolyLavaPreview : MonoBehaviour
    {
        private const string PreviewShaderName = "W1/Low Poly Fire Preview";

        [Header("Tile alignment")]
        [SerializeField, Range(1, 32)] private int widthInTiles = 10;
        [SerializeField, Min(0.1f)] private float tileSize = 1f;
        [SerializeField, Min(0.1f)] private float depth = 3.5f;

        [Header("Surface")]
        [SerializeField, Range(0f, 0.25f)] private float surfaceMotion = 0.10f;
        [SerializeField, Min(0f)] private float animationSpeed = 1f;
        [SerializeField, Range(0f, 1f)] private float bodyOpacity = 1f;

        [Header("Preview framing")]
        [SerializeField] private bool showBackdrop = true;
        [SerializeField] private bool showRockBanks = true;

        [Header("Activity")]
        [SerializeField, Range(0, 24)] private int bubbleCount = 9;
        [SerializeField, Range(0, 32)] private int flameParticleCount = 14;

        private Material previewMaterial;

        private void OnEnable()
        {
            EnsureMaterial();
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        private void OnDisable()
        {
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;

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
            widthInTiles = Mathf.Clamp(widthInTiles, 1, 32);
            tileSize = Mathf.Max(0.1f, tileSize);
            depth = Mathf.Max(0.1f, depth);
            bubbleCount = Mathf.Clamp(bubbleCount, 0, 24);
            flameParticleCount = Mathf.Clamp(flameParticleCount, 0, 32);
        }

        private void OnRenderObject()
        {
            // Built-in render pipeline callback. URP/HDRP use OnEndCameraRendering below.
            if (GraphicsSettings.currentRenderPipeline == null)
            {
                RenderForCamera(Camera.current);
            }
        }

        private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (GraphicsSettings.currentRenderPipeline != null)
            {
                RenderForCamera(camera);
            }
        }

        private void RenderForCamera(Camera camera)
        {
            EnsureMaterial();
            if (previewMaterial == null || camera == null)
            {
                return;
            }

            float time = (Application.isPlaying ? Time.time : Time.realtimeSinceStartup) * animationSpeed;
            previewMaterial.SetPass(0);

            GL.PushMatrix();
            GL.LoadProjectionMatrix(camera.projectionMatrix);
            GL.modelview = camera.worldToCameraMatrix;
            GL.MultMatrix(transform.localToWorldMatrix);
            if (showBackdrop)
            {
                DrawBackdrop();
            }
            DrawLavaGlow(time);
            DrawLavaBody(time);
            DrawFlowBands(time);
            DrawSurfaceCrust(time);
            DrawBubbles(time);
            DrawEmbers(time);
            if (showRockBanks)
            {
                DrawRockBanks();
            }
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

        private void DrawBackdrop()
        {
            float halfWidth = widthInTiles * tileSize * 0.5f + 5f;
            GL.Begin(GL.TRIANGLES);
            Quad(
                new Vector2(-halfWidth, -depth - 2f),
                new Vector2(-halfWidth, 7f),
                new Vector2(halfWidth, 7f),
                new Vector2(halfWidth, -depth - 2f),
                new Color(0.012f, 0.002f, 0.001f, 1f),
                new Color(0.07f, 0.008f, 0.003f, 1f));
            GL.End();
        }

        private void DrawLavaGlow(float time)
        {
            float halfWidth = widthInTiles * tileSize * 0.5f;
            float pulse = 1f + Mathf.Sin(time * 2.3f) * 0.035f;
            DrawEllipse(new Vector2(0f, -0.15f), new Vector2(halfWidth + 1.4f, 2.25f) * pulse,
                new Color(1f, 0.16f, 0.005f, 0.13f), 34);
            DrawEllipse(new Vector2(0f, -0.05f), new Vector2(halfWidth + 0.45f, 0.9f) * pulse,
                new Color(1f, 0.62f, 0.025f, 0.18f), 30);
        }

        private void DrawLavaBody(float time)
        {
            float halfWidth = widthInTiles * tileSize * 0.5f;
            float bottom = -depth;
            int segmentCount = Mathf.Max(12, widthInTiles * 3);
            float segmentWidth = halfWidth * 2f / segmentCount;
            Color topColor = new(1f, 0.33f, 0.008f, bodyOpacity);
            Color bottomColor = new(0.17f, 0.006f, 0.001f, bodyOpacity);

            GL.Begin(GL.TRIANGLES);
            for (int segment = 0; segment < segmentCount; segment++)
            {
                float x0 = -halfWidth + segment * segmentWidth;
                float x1 = x0 + segmentWidth;
                float y0 = SurfaceAtX(x0, time);
                float y1 = SurfaceAtX(x1, time);

                Vertex(new Vector2(x0, y0), topColor);
                Vertex(new Vector2(x0, bottom), bottomColor);
                Vertex(new Vector2(x1, bottom), bottomColor);

                Vertex(new Vector2(x0, y0), topColor);
                Vertex(new Vector2(x1, bottom), bottomColor);
                Vertex(new Vector2(x1, y1), topColor);
            }
            GL.End();
        }

        private void DrawFlowBands(float time)
        {
            DrawFlowBand(time, 0.4f, -0.68f, 0.34f, new Color(1f, 0.62f, 0.02f, 0.34f));
            DrawFlowBand(time, 2.1f, -1.48f, 0.46f, new Color(0.92f, 0.10f, 0.002f, 0.38f));
            DrawFlowBand(time, 4.7f, -2.35f, 0.38f, new Color(1f, 0.25f, 0.004f, 0.28f));
        }

        private void DrawFlowBand(float time, float phase, float centerY, float thickness, Color color)
        {
            float halfWidth = widthInTiles * tileSize * 0.5f;
            const int segments = 12;
            float segmentWidth = halfWidth * 2f / segments;

            GL.Begin(GL.TRIANGLES);
            for (int i = 0; i < segments; i++)
            {
                float x0 = -halfWidth + i * segmentWidth;
                float x1 = x0 + segmentWidth;
                float wave0 = Mathf.Sin(x0 * 0.72f + time * 0.28f + phase) * 0.16f
                    + Mathf.Sin(x0 * 1.31f - time * 0.16f + phase) * 0.05f;
                float wave1 = Mathf.Sin(x1 * 0.72f + time * 0.28f + phase) * 0.16f
                    + Mathf.Sin(x1 * 1.31f - time * 0.16f + phase) * 0.05f;

                Color faded = new(color.r, color.g, color.b, color.a * 0.2f);
                Vertex(new Vector2(x0, centerY + wave0 + thickness), faded);
                Vertex(new Vector2(x0, centerY + wave0 - thickness), faded);
                Vertex(new Vector2(x1, centerY + wave1 - thickness), faded);

                Vertex(new Vector2(x0, centerY + wave0 + thickness), faded);
                Vertex(new Vector2(x1, centerY + wave1 - thickness), faded);
                Vertex(new Vector2(x1, centerY + wave1 + thickness), faded);
            }
            GL.End();
        }

        private void DrawSurfaceCrust(float time)
        {
            float halfWidth = widthInTiles * tileSize * 0.5f;
            int segmentCount = Mathf.Max(16, widthInTiles * 4);
            float segmentWidth = halfWidth * 2f / segmentCount;
            const float thickness = 0.11f;
            Color hot = new(1f, 0.86f, 0.08f, 1f);
            Color dark = new(0.82f, 0.085f, 0.002f, 1f);

            GL.Begin(GL.TRIANGLES);
            for (int segment = 0; segment < segmentCount; segment++)
            {
                float x0 = -halfWidth + segment * segmentWidth;
                float x1 = x0 + segmentWidth;
                float y0 = SurfaceAtX(x0, time);
                float y1 = SurfaceAtX(x1, time);

                Vertex(new Vector2(x0, y0), hot);
                Vertex(new Vector2(x0, y0 - thickness), dark);
                Vertex(new Vector2(x1, y1 - thickness), dark);

                Vertex(new Vector2(x0, y0), hot);
                Vertex(new Vector2(x1, y1 - thickness), dark);
                Vertex(new Vector2(x1, y1), hot);
            }
            GL.End();
        }

        private void DrawBubbles(float time)
        {
            float width = widthInTiles * tileSize;
            float halfWidth = width * 0.5f;

            for (int i = 0; i < bubbleCount; i++)
            {
                float seed = i + 1f;
                float progress = Mathf.Repeat(time * Mathf.Lerp(0.10f, 0.21f, Hash01(seed * 2.2f))
                    + Hash01(seed * 8.4f), 1f);
                float x = -halfWidth + tileSize * 0.4f + Hash01(seed * 5.9f) * (width - tileSize * 0.8f);
                float localSurface = SurfaceAtX(x, time);

                if (progress < 0.72f)
                {
                    float rise = progress / 0.72f;
                    float radius = Mathf.Lerp(0.035f, 0.24f, rise)
                        * Mathf.Lerp(0.7f, 1.25f, Hash01(seed * 3.7f));
                    DrawPolygonDisc(new Vector2(x, localSurface + radius * 0.38f),
                        radius, 7, new Color(1f, 0.72f, 0.045f, 0.92f));
                    DrawPolygonDisc(new Vector2(x - radius * 0.18f, localSurface + radius * 0.58f),
                        radius * 0.45f, 6, new Color(0.42f, 0.018f, 0.001f, 0.68f));
                }
                else
                {
                    float burst = (progress - 0.72f) / 0.28f;
                    float fade = 1f - burst;
                    float spread = Mathf.Lerp(0.10f, 0.42f, burst);
                    float rise = Mathf.Lerp(0.12f, 0.75f, burst);
                    Color droplet = new(1f, 0.46f, 0.008f, fade);

                    DrawPolygonOval(new Vector2(x - spread, localSurface + rise),
                        new Vector2(0.07f, 0.11f) * fade, 6, droplet);
                    DrawPolygonOval(new Vector2(x + spread * 0.65f, localSurface + rise * 0.82f),
                        new Vector2(0.055f, 0.09f) * fade, 6, droplet);
                    DrawPolygonOval(new Vector2(x + spread * 0.08f, localSurface + rise * 1.15f),
                        new Vector2(0.045f, 0.08f) * fade, 6, droplet);
                }
            }
        }

        private void DrawEmbers(float time)
        {
            float width = widthInTiles * tileSize;
            float halfWidth = width * 0.5f;

            for (int i = 0; i < flameParticleCount; i++)
            {
                float seed = i + 31f;
                float progress = Mathf.Repeat(time * Mathf.Lerp(0.17f, 0.34f, Hash01(seed * 1.7f))
                    + Hash01(seed * 7.9f), 1f);
                float x = -halfWidth + Hash01(seed * 4.5f) * width;
                x += Mathf.Sin(time * 2.2f + seed) * 0.11f * progress;
                float y = SurfaceAtX(x, time) + progress * Mathf.Lerp(0.75f, 1.65f, Hash01(seed * 2.8f));
                float fade = Mathf.Sin(progress * Mathf.PI);
                float size = Mathf.Lerp(0.14f, 0.025f, progress)
                    * Mathf.Lerp(0.7f, 1.35f, Hash01(seed * 9.2f));
                Color color = Color.Lerp(
                    new Color(1f, 0.86f, 0.05f, fade * 0.85f),
                    new Color(1f, 0.08f, 0.003f, fade * 0.55f),
                    progress);

                DrawPolygonOval(new Vector2(x, y), new Vector2(size * 0.65f, size * 1.25f), 6, color);
            }
        }

        private void DrawRockBanks()
        {
            float halfWidth = widthInTiles * tileSize * 0.5f;
            Color rock = new(0.018f, 0.007f, 0.005f, 1f);
            GL.Begin(GL.TRIANGLES);
            Triangle(new Vector2(-halfWidth - 2.4f, -depth), new Vector2(-halfWidth - 2.4f, 1.35f),
                new Vector2(-halfWidth, 0.08f), rock);
            Triangle(new Vector2(-halfWidth - 2.4f, -depth), new Vector2(-halfWidth, 0.08f),
                new Vector2(-halfWidth, -depth), rock);
            Triangle(new Vector2(halfWidth, -depth), new Vector2(halfWidth, 0.08f),
                new Vector2(halfWidth + 2.4f, 1.35f), rock);
            Triangle(new Vector2(halfWidth, -depth), new Vector2(halfWidth + 2.4f, 1.35f),
                new Vector2(halfWidth + 2.4f, -depth), rock);
            GL.End();
        }

        private float SurfaceAtX(float x, float time)
        {
            float halfWidth = widthInTiles * tileSize * 0.5f;
            float gridX = Mathf.Clamp((x + halfWidth) / tileSize, 0f, widthInTiles);
            int left = Mathf.FloorToInt(gridX);
            int right = Mathf.Min(left + 1, widthInTiles);
            float blend = gridX - left;
            return Mathf.Lerp(SurfaceY(left, time), SurfaceY(right, time), blend);
        }

        private float SurfaceY(int pointIndex, float time)
        {
            float steppedTime = Mathf.Floor(time * 8f) * 0.125f;
            float slow = Mathf.Sin(steppedTime * 1.7f + pointIndex * 1.41f);
            float secondary = Mathf.Sin(steppedTime * 2.9f + pointIndex * 0.63f) * 0.35f;
            return (slow + secondary) * surfaceMotion;
        }

        private static void DrawEllipse(Vector2 center, Vector2 radius, Color centerColor, int segments)
        {
            Color edge = new(centerColor.r, centerColor.g, centerColor.b, 0f);
            GL.Begin(GL.TRIANGLES);
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                float b = (i + 1) * Mathf.PI * 2f / segments;
                Vertex(center, centerColor);
                Vertex(center + new Vector2(Mathf.Cos(a) * radius.x, Mathf.Sin(a) * radius.y), edge);
                Vertex(center + new Vector2(Mathf.Cos(b) * radius.x, Mathf.Sin(b) * radius.y), edge);
            }
            GL.End();
        }

        private static void DrawPolygonDisc(Vector2 center, float radius, int sides, Color color)
        {
            GL.Begin(GL.TRIANGLES);
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                float b = (i + 1) * Mathf.PI * 2f / sides;
                Vertex(center, color);
                Vertex(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, color);
                Vertex(center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, color);
            }
            GL.End();
        }

        private static void DrawPolygonOval(Vector2 center, Vector2 radius, int sides, Color color)
        {
            GL.Begin(GL.TRIANGLES);
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                float b = (i + 1) * Mathf.PI * 2f / sides;
                Vertex(center, color);
                Vertex(center + new Vector2(Mathf.Cos(a) * radius.x, Mathf.Sin(a) * radius.y), color);
                Vertex(center + new Vector2(Mathf.Cos(b) * radius.x, Mathf.Sin(b) * radius.y), color);
            }
            GL.End();
        }

        private static void Quad(Vector2 bottomLeft, Vector2 topLeft, Vector2 topRight, Vector2 bottomRight,
            Color bottomColor, Color topColor)
        {
            Vertex(bottomLeft, bottomColor);
            Vertex(topLeft, topColor);
            Vertex(topRight, topColor);
            Vertex(bottomLeft, bottomColor);
            Vertex(topRight, topColor);
            Vertex(bottomRight, bottomColor);
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
