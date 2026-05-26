// ============================================================
//  OPHIO — ArenaLayoutData
//  Environment | Arena Core
//  ScriptableObject holding all tunable arena generation values.
//  Create > OPHIO > Arena Layout Data
//  Assign one asset per level (or share across levels and let
//  ArenaGenerator.levelIndex drive DifficultyMult).
// ============================================================

using UnityEngine;

namespace OPHIO.Arena
{
    [CreateAssetMenu(fileName = "ArenaLayoutData", menuName = "OPHIO/Arena Layout Data")]
    public class ArenaLayoutData : ScriptableObject
    {
        [Header("Arena Size")]
        public float arenaRadius = 30f;

        [Header("Floor Tiles")]
        [Tooltip("2-3 floor tile variants — randomly selected per cell")]
        public GameObject[] floorTilePrefabs;
        public float tileSize = 4f;

        [Header("Boundary Walls")]
        public GameObject wallSegmentPrefab;
        public int wallSegmentCount = 24;

        [Header("Cover Objects")]
        [Tooltip("Pillars, crates, fungal debris — non-destructible cover")]
        public GameObject[] coverPrefabs;
        [Range(5, 40)] public int   coverCount      = 20;
        public float               minCoverSpacing  = 3f;

        [Header("Destructible Objects")]
        [Tooltip("Breakable props — DestructibleObject component required")]
        public GameObject[] destructiblePrefabs;
        [Range(0, 20)] public int destructibleCount = 10;

        [Header("Infection Hazard Zones")]
        [Tooltip("Prefab must have InfectionHazardZone component")]
        public GameObject infectionZonePrefab;
        [Range(0, 8)]  public int   infectionZoneCount  = 4;
        public float               infectionZoneRadius  = 3f;

        [Header("Spore VFX")]
        [Tooltip("Particle system prefab — purely visual, no damage")]
        public GameObject sporeFXPrefab;
        [Range(0, 15)] public int sporeFXCount = 8;

        [Header("Enemy Spawn Points")]
        [Range(4, 20)] public int spawnPointCount = 8;
        [Tooltip("Spawn points are evenly distributed on this ring")]
        public float spawnRingRadius = 26f;

        [Header("Difficulty Scale")]
        [Tooltip("X axis = level fraction (0-1), Y axis = count multiplier.\n" +
                 "Default: flat 1.0 across all levels.")]
        public AnimationCurve difficultyScale = AnimationCurve.Linear(0f, 1f, 1f, 2f);
    }
}
