using UnityEngine;

/// <summary>One grid-sized freezing-ground cell used by the reusable prefab.</summary>
public sealed class FreezingGroundCell2D : MonoBehaviour
{
    public Vector2 WorldCenter => transform.position;
}
