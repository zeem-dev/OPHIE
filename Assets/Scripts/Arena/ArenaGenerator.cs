// ============================================================
//  OPHIO — ArenaGenerator
//  Environment | Arena Core
//  Procedurally generates the Harvest Games arena at runtime.
//  Attach to an empty "ArenaGenerator" GameObject in Arena scene.
//  Assign ArenaLayoutData ScriptableObject in the Inspector.
//  Right-click component > "Generate Arena" to preview in Editor.
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace OPHIO.Arena
{
    [DisallowMultipleComponent]
    public class ArenaGenerator : MonoBehaviour
    {
        [Header("Config")]
        public ArenaLayoutData layoutData;

        [Header("Level (0-9) — drives difficulty scale")]
        [Range(0, 9)]
        public int levelIndex = 0;

        // Runtime-populated — ArenaManager / EnemySpawner can read these
        [HideInInspector] public List<Transform> generatedSpawnPoints = new();
        [HideInInspector] public List<InfectionHazardZone> hazardZones = new();
        [HideInInspector] public List<DestructibleObject> destructibles = new();

        Transform _floorParent;
        Transform _coverParent;
        Transform _hazardParent;
        Transform _spawnParent;
        Transform _vfxParent;
        Transform _wallParent;

        readonly List<Vector3> _placed = new();

        float DifficultyMult =>
            layoutData.difficultyScale != null
                ? layoutData.difficultyScale.Evaluate(levelIndex / 9f)
                : 1f;

        void Awake()
        {
            if (layoutData != null)
                Generate();
        }

        // ── Public ───────────────────────────────────────────────────────────

        [ContextMenu("Generate Arena")]
        public void Generate()
        {
            if (layoutData == null)
            {
                Debug.LogError("[ArenaGenerator] ArenaLayoutData not assigned.");
                return;
            }

            ClearArena();
            _placed.Clear();

            _floorParent  = MakeParent("Floor");
            _coverParent  = MakeParent("Cover");
            _hazardParent = MakeParent("Hazards");
            _spawnParent  = MakeParent("SpawnPoints");
            _vfxParent    = MakeParent("SporeVFX");
            _wallParent   = MakeParent("Walls");

            BuildFloor();
            BuildBoundaryWalls();
            PlaceCover();
            PlaceDestructibles();
            PlaceInfectionZones();
            PlaceSporeVFX();
            PlaceSpawnPoints();

            Debug.Log($"[ArenaGenerator] Arena ready — Level {levelIndex + 1}, Difficulty x{DifficultyMult:F2}");
        }

        [ContextMenu("Clear Arena")]
        public void ClearArena()
        {
            generatedSpawnPoints.Clear();
            hazardZones.Clear();
            destructibles.Clear();

            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);
        }

        // ── Floor ─────────────────────────────────────────────────────────────

        void BuildFloor()
        {
            if (layoutData.floorTilePrefabs == null || layoutData.floorTilePrefabs.Length == 0) return;

            float r = layoutData.arenaRadius;
            float t = layoutData.tileSize;
            int   n = Mathf.CeilToInt(r * 2f / t);

            for (int x = -n; x <= n; x++)
            {
                for (int z = -n; z <= n; z++)
                {
                    Vector3 pos = new Vector3(x * t, 0f, z * t);
                    if (pos.magnitude > r) continue;

                    GameObject prefab = layoutData.floorTilePrefabs[
                        Random.Range(0, layoutData.floorTilePrefabs.Length)];
                    if (prefab == null) continue;

                    Quaternion rot = Quaternion.Euler(0f, Random.Range(0, 4) * 90f, 0f);
                    Instantiate(prefab, transform.position + pos, rot, _floorParent)
                        .name = $"Tile_{x}_{z}";
                }
            }
        }

        // ── Boundary Walls ────────────────────────────────────────────────────

        void BuildBoundaryWalls()
        {
            if (layoutData.wallSegmentPrefab == null) return;

            float r    = layoutData.arenaRadius;
            int   segs = layoutData.wallSegmentCount;

            for (int i = 0; i < segs; i++)
            {
                float   angle = i * (360f / segs) * Mathf.Deg2Rad;
                Vector3 pos   = transform.position + new Vector3(Mathf.Sin(angle) * r, 0f, Mathf.Cos(angle) * r);
                Quaternion rot = Quaternion.LookRotation(-new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)), Vector3.up);

                Instantiate(layoutData.wallSegmentPrefab, pos, rot, _wallParent).name = $"Wall_{i}";
            }
        }

        // ── Cover ─────────────────────────────────────────────────────────────

        void PlaceCover()
        {
            if (layoutData.coverPrefabs == null || layoutData.coverPrefabs.Length == 0) return;

            int count = Mathf.RoundToInt(layoutData.coverCount * DifficultyMult);
            PlaceObjects(layoutData.coverPrefabs, count, layoutData.minCoverSpacing,
                layoutData.arenaRadius * 0.85f, _coverParent, "Cover", null);
        }

        // ── Destructibles ─────────────────────────────────────────────────────

        void PlaceDestructibles()
        {
            if (layoutData.destructiblePrefabs == null || layoutData.destructiblePrefabs.Length == 0) return;

            int count = Mathf.RoundToInt(layoutData.destructibleCount * DifficultyMult);
            List<GameObject> placed = new();
            PlaceObjects(layoutData.destructiblePrefabs, count, layoutData.minCoverSpacing,
                layoutData.arenaRadius * 0.8f, _coverParent, "Destructible", placed);

            destructibles.Clear();
            foreach (GameObject go in placed)
            {
                DestructibleObject d = go.GetComponent<DestructibleObject>();
                if (d != null) destructibles.Add(d);
            }
        }

        // ── Infection Hazard Zones ────────────────────────────────────────────

        void PlaceInfectionZones()
        {
            if (layoutData.infectionZonePrefab == null) return;

            hazardZones.Clear();
            int count = Mathf.RoundToInt(layoutData.infectionZoneCount * DifficultyMult);

            for (int i = 0; i < count; i++)
            {
                Vector3 pos = RandomPos(layoutData.arenaRadius * 0.75f, 5f);
                if (pos == Vector3.zero) continue;

                GameObject go = Instantiate(layoutData.infectionZonePrefab,
                    transform.position + pos, Quaternion.identity, _hazardParent);
                go.name = $"InfectionZone_{i}";

                InfectionHazardZone zone = go.GetComponent<InfectionHazardZone>();
                if (zone != null)
                {
                    zone.zoneRadius = layoutData.infectionZoneRadius;
                    zone.RefreshCollider();
                    hazardZones.Add(zone);
                }

                _placed.Add(pos);
            }
        }

        // ── Spore VFX ─────────────────────────────────────────────────────────

        void PlaceSporeVFX()
        {
            if (layoutData.sporeFXPrefab == null) return;

            int count = Mathf.RoundToInt(layoutData.sporeFXCount * DifficultyMult);
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = RandomPos(layoutData.arenaRadius * 0.9f, 2f);
                if (pos == Vector3.zero) continue;

                Instantiate(layoutData.sporeFXPrefab,
                    transform.position + pos, Quaternion.identity, _vfxParent).name = $"SporeFX_{i}";

                _placed.Add(pos);
            }
        }

        // ── Spawn Points ──────────────────────────────────────────────────────

        void PlaceSpawnPoints()
        {
            generatedSpawnPoints.Clear();
            int count = Mathf.Clamp(
                Mathf.RoundToInt(layoutData.spawnPointCount * DifficultyMult), 4, 24);

            for (int i = 0; i < count; i++)
            {
                float   angle = i * (360f / count) * Mathf.Deg2Rad;
                float   r     = layoutData.spawnRingRadius;
                Vector3 pos   = transform.position + new Vector3(Mathf.Sin(angle) * r, 0f, Mathf.Cos(angle) * r);

                GameObject sp = new GameObject($"SpawnPoint_{i}");
                sp.transform.SetParent(_spawnParent);
                sp.transform.position = pos;
                sp.transform.rotation = Quaternion.LookRotation(
                    -(pos - transform.position).normalized, Vector3.up);

                generatedSpawnPoints.Add(sp.transform);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        void PlaceObjects(GameObject[] prefabs, int count, float minSpacing,
            float maxRadius, Transform parent, string prefix, List<GameObject> outList)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = Vector3.zero;
                bool found  = false;

                for (int a = 0; a < count * 10; a++)
                {
                    Vector3 candidate = RandomPos(maxRadius, 0f);
                    if (IsFarEnough(candidate, minSpacing))
                    {
                        pos   = candidate;
                        found = true;
                        break;
                    }
                }

                if (!found) continue;

                GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
                if (prefab == null) continue;

                Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                GameObject go  = Instantiate(prefab, transform.position + pos, rot, parent);
                go.name = $"{prefix}_{i}";

                _placed.Add(pos);
                outList?.Add(go);
            }
        }

        Vector3 RandomPos(float maxRadius, float minRadius)
        {
            for (int i = 0; i < 30; i++)
            {
                float   angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float   dist  = Random.Range(minRadius, maxRadius);
                Vector3 pos   = new Vector3(Mathf.Sin(angle) * dist, 0f, Mathf.Cos(angle) * dist);
                if (IsFarEnough(pos, 2f)) return pos;
            }
            return Vector3.zero;
        }

        bool IsFarEnough(Vector3 pos, float minDist)
        {
            foreach (Vector3 p in _placed)
                if (Vector3.Distance(pos, p) < minDist) return false;
            return true;
        }

        Transform MakeParent(string n)
        {
            GameObject go = new GameObject(n);
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            return go.transform;
        }

        void OnDrawGizmosSelected()
        {
            if (layoutData == null) return;
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.12f);
            Gizmos.DrawSphere(transform.position, layoutData.arenaRadius);
            Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, layoutData.spawnRingRadius);
        }
    }
}
