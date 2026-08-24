using UnityEngine;

/// <summary>
/// Explicit gameplay meaning for a collider-backed surface. Systems must query
/// this component instead of inferring rules from object, Tilemap, Tile or Sprite names.
/// </summary>
public sealed class SurfaceSemantic2D : MonoBehaviour
{
    public enum SurfaceType
    {
        StaticSolid,
        FrozenGround,
        OneWayPlatform,
        SpecialMirrorWall,
        Hazard,
        DynamicSurface,
        Conveyor,
        FreezingGround
    }

    [SerializeField] private SurfaceType surfaceType = SurfaceType.StaticSolid;
    [SerializeField] private bool isStatic = true;
    [SerializeField] private bool isSafe = true;

    public SurfaceType Type => surfaceType;
    public bool IsStatic => isStatic;
    public bool IsSafe => isSafe;

    public void Configure(SurfaceType type, bool staticSurface, bool safeSurface)
    {
        surfaceType = type;
        isStatic = staticSurface;
        isSafe = safeSurface;
    }

    public static bool TryGet(Collider2D collider, out SurfaceSemantic2D semantic)
    {
        semantic = collider != null ? collider.GetComponent<SurfaceSemantic2D>() : null;
        return semantic != null;
    }
}
