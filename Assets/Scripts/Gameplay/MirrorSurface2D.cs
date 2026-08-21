using UnityEngine;

public sealed class MirrorSurface2D : MonoBehaviour
{
    public enum SurfaceKind { Ground, SpecialWall }
    public SurfaceKind kind;
    public bool safe = true;
}
