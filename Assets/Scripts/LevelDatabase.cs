using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LevelDatabase", menuName = "Arrow Cube Escape/Level Database")]
public class LevelDatabase : ScriptableObject
{
    [Header("Levels")]
    public List<ArrowLevelData> levels = new List<ArrowLevelData>();

    [Header("Runtime Materials")]
    public Material centralCubeMaterial;
    public Material inkMaterial;
    public Material headMaterial;
    public Material outlineMaterial;
}
