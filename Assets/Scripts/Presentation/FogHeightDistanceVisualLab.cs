using UnityEngine;

namespace W1.Presentation
{
    /// <summary>
    /// Presentation-only controls for the fog silhouette visual lab. This component
    /// owns no collision, surface semantic, reset, mirror, or gameplay state.
    /// </summary>
    [ExecuteAlways]
    public sealed class FogHeightDistanceVisualLab : MonoBehaviour
    {
        private static readonly int HeightFogStartId = Shader.PropertyToID("_HeightFogStart");
        private static readonly int HeightFogEndId = Shader.PropertyToID("_HeightFogEnd");
        private static readonly int DistanceFogStartId = Shader.PropertyToID("_DistanceFogStart");
        private static readonly int DistanceFogEndId = Shader.PropertyToID("_DistanceFogEnd");
        private static readonly int HeightFogStrengthId = Shader.PropertyToID("_HeightFogStrength");
        private static readonly int DistanceFogStrengthId = Shader.PropertyToID("_DistanceFogStrength");
        private static readonly int ColorFogStrengthId = Shader.PropertyToID("_ColorFogStrength");
        private static readonly int AlphaFadeStrengthId = Shader.PropertyToID("_AlphaFadeStrength");

        [Header("Visual samples")]
        [SerializeField] private SpriteRenderer[] treeRenderers;

        [Header("World-height fog")]
        [SerializeField] private float heightFogStart = -4f;
        [SerializeField] private float heightFogEnd = 5f;
        [SerializeField, Range(0f, 1f)] private float heightFogStrength = 0.65f;

        [Header("Camera-depth fog")]
        [SerializeField] private float distanceFogStart = 8f;
        [SerializeField] private float distanceFogEnd = 20f;
        [SerializeField, Range(0f, 1f)] private float distanceFogStrength = 0.8f;

        [Header("Result")]
        [SerializeField, Range(0f, 1f)] private float colorFogStrength = 0.9f;
        [SerializeField, Range(0f, 1f)] private float alphaFadeStrength = 0.28f;
        [SerializeField] private bool autoDemonstrate = true;

        private MaterialPropertyBlock propertyBlock;

        private void OnEnable()
        {
            propertyBlock = new MaterialPropertyBlock();
            ApplyProperties();
        }

        private void OnValidate()
        {
            if (heightFogEnd <= heightFogStart)
            {
                heightFogEnd = heightFogStart + 0.01f;
            }

            if (distanceFogEnd <= distanceFogStart)
            {
                distanceFogEnd = distanceFogStart + 0.01f;
            }

            ApplyProperties();
        }

        private void Update()
        {
            if (Application.isPlaying && autoDemonstrate)
            {
                float cycle = Mathf.Sin(Time.time * 0.45f) * 0.5f + 0.5f;
                heightFogStrength = Mathf.Lerp(0.2f, 0.85f, cycle);
                distanceFogStrength = Mathf.Lerp(0.25f, 1f, cycle);
            }

            ApplyProperties();
        }

        private void ApplyProperties()
        {
            if (treeRenderers == null)
            {
                return;
            }

            propertyBlock ??= new MaterialPropertyBlock();
            foreach (SpriteRenderer treeRenderer in treeRenderers)
            {
                if (treeRenderer == null)
                {
                    continue;
                }

                treeRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat(HeightFogStartId, heightFogStart);
                propertyBlock.SetFloat(HeightFogEndId, heightFogEnd);
                propertyBlock.SetFloat(DistanceFogStartId, distanceFogStart);
                propertyBlock.SetFloat(DistanceFogEndId, distanceFogEnd);
                propertyBlock.SetFloat(HeightFogStrengthId, heightFogStrength);
                propertyBlock.SetFloat(DistanceFogStrengthId, distanceFogStrength);
                propertyBlock.SetFloat(ColorFogStrengthId, colorFogStrength);
                propertyBlock.SetFloat(AlphaFadeStrengthId, alphaFadeStrength);
                treeRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            const float panelWidth = 330f;
            GUILayout.BeginArea(new Rect(24f, 24f, panelWidth, 305f), GUI.skin.box);
            GUILayout.Label("Fog + silhouette visual lab");
            GUILayout.Label("同一棵树，世界高度与相机深度共同决定雾化。\n远处副本会更接近背景色并逐渐透明。");

            autoDemonstrate = GUILayout.Toggle(autoDemonstrate, " 自动往返演示");
            GUILayout.Space(8f);

            GUILayout.Label($"高度雾强度  {heightFogStrength:0.00}");
            heightFogStrength = GUILayout.HorizontalSlider(heightFogStrength, 0f, 1f);
            GUILayout.Label($"距离雾强度  {distanceFogStrength:0.00}");
            distanceFogStrength = GUILayout.HorizontalSlider(distanceFogStrength, 0f, 1f);
            GUILayout.Label($"颜色雾化  {colorFogStrength:0.00}");
            colorFogStrength = GUILayout.HorizontalSlider(colorFogStrength, 0f, 1f);
            GUILayout.Label($"透明渐隐  {alphaFadeStrength:0.00}");
            alphaFadeStrength = GUILayout.HorizontalSlider(alphaFadeStrength, 0f, 0.8f);
            GUILayout.EndArea();
        }
    }
}
