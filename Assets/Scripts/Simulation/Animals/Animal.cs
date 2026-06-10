using UnityEngine;

public class Animal : MonoBehaviour
{
    [Header("Species (documentation only — set by SimulationRunner at runtime)")]
    [SerializeField] private AnimalSpecies defaultSpecies = AnimalSpecies.Prey;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("POV")]
    [SerializeField] public GameObject modelRoot;
    public GameObject ModelRoot => modelRoot;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    public AnimalState State { get; private set; }
    public bool IsAlive => State != null && State.IsAlive;

    private WorldGrid _grid;
    private RenderConfig _cfg;
    private SimulationSettings _settings;

    // ── Init ──────────────────────────────────────────────────────────────────

    public void Initialize(
        AnimalSpecies species, float worldX, float worldZ,
        GeneticProfile genes, WorldGrid grid,
        RenderConfig cfg, SimulationSettings settings, int id)
    {
        _grid = grid;
        _cfg = cfg;
        _settings = settings;

        float h = grid.SampleHeight(worldX / cfg.cellSize, worldZ / cfg.cellSize)
                  * cfg.heightScale;

        State = new AnimalState
        {
            id = id,
            species = species,
            genes = genes,
            energy = 0.85f,
            position = new Vector2(worldX, worldZ),
            velocity = Random.insideUnitCircle * 0.5f,
        };

        transform.position = new Vector3(worldX, h, worldZ);
        ApplyBodySizeScale(genes.bodySize);
    }

    public void InitializeFromState(
        AnimalState state, WorldGrid grid, RenderConfig cfg, SimulationSettings settings)
    {
        State = state;
        _grid = grid;
        _cfg = cfg;
        _settings = settings;

        float h = grid.SampleHeight(state.position.x / cfg.cellSize,
                                    state.position.y / cfg.cellSize) * cfg.heightScale;

        transform.position = new Vector3(state.position.x, h, state.position.y);
        ApplyBodySizeScale(state.genes.bodySize);
    }

    // bodySize=1.0 → scale=1.0 | bodySize=0.5 → ≈0.71 | bodySize=1.8 → ≈1.34
    private void ApplyBodySizeScale(float bodySize)
    {
        float scale = Mathf.Pow(Mathf.Max(0.1f, bodySize), 0.5f);
        transform.localScale = Vector3.one * scale;
    }

    // ── Movement ──────────────────────────────────────────────────────────────

    public void ApplySteering(Vector2 acceleration, float dt)
    {
        State.velocity = Vector2.Lerp(State.velocity, acceleration, dt * 5f);

        float maxSpeed = State.genes.EffectiveMaxSpeed;
        if (State.velocity.sqrMagnitude > maxSpeed * maxSpeed)
            State.velocity = State.velocity.normalized * maxSpeed;

        State.position += State.velocity * dt;

        float maxW = (_grid.size - 1) * _cfg.cellSize;

        if (State.position.x < 0f) { State.position.x = 0f; if (State.velocity.x < 0f) State.velocity.x = 0f; }
        if (State.position.x > maxW) { State.position.x = maxW; if (State.velocity.x > 0f) State.velocity.x = 0f; }
        if (State.position.y < 0f) { State.position.y = 0f; if (State.velocity.y < 0f) State.velocity.y = 0f; }
        if (State.position.y > maxW) { State.position.y = maxW; if (State.velocity.y > 0f) State.velocity.y = 0f; }

        PushOutOfWater();

        float wx = State.position.x / _cfg.cellSize;
        float wz = State.position.y / _cfg.cellSize;
        float worldY = _grid.SampleHeight(wx, wz) * _cfg.heightScale;
        transform.position = new Vector3(State.position.x, worldY, State.position.y);

        if (State.velocity.sqrMagnitude > 0.01f)
        {
            var targetRot = Quaternion.LookRotation(
                new Vector3(State.velocity.x, 0f, State.velocity.y), Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, dt * 8f);
        }

        if (animator != null)
            animator.SetFloat(SpeedHash, State.velocity.magnitude);
    }

    private void PushOutOfWater()
    {
        int cx = Mathf.RoundToInt(State.position.x / _cfg.cellSize);
        int cy = Mathf.RoundToInt(State.position.y / _cfg.cellSize);

        if (!_grid.IsInside(cx, cy)) return;
        if (!_grid.Get(cx, cy).IsWater) return;

        float bestDist = float.MaxValue;
        Vector2 bestPos = State.position;
        bool found = false;

        for (int dx = -2; dx <= 2; dx++)
            for (int dy = -2; dy <= 2; dy++)
            {
                int nx = cx + dx, ny = cy + dy;
                if (!_grid.IsInside(nx, ny)) continue;
                if (_grid.Get(nx, ny).IsWater) continue;

                float wx = nx * _cfg.cellSize;
                float wz = ny * _cfg.cellSize;
                float ddx = wx - State.position.x;
                float ddz = wz - State.position.y;
                float dist = ddx * ddx + ddz * ddz;

                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestPos = new Vector2(wx, wz);
                    found = true;
                }
            }

        if (!found) return;
        State.position = bestPos;
        State.velocity *= 0.3f;
    }

    // ── Feeding / Drinking ────────────────────────────────────────────────────

    public bool TryEatFruit(PlantManager plantMgr, SimulationSettings s)
    {
        int cx = Mathf.RoundToInt(State.position.x / _cfg.cellSize);
        int cy = Mathf.RoundToInt(State.position.y / _cfg.cellSize);
        if (!plantMgr.TryEat(cx, cy, s)) return false;
        MetabolismSystem.EatFruit(State, s);
        return true;
    }

    public bool TryDrink(SimulationSettings s, float dt)
    {
        int cx = Mathf.RoundToInt(State.position.x / _cfg.cellSize);
        int cy = Mathf.RoundToInt(State.position.y / _cfg.cellSize);
        int scanR = Mathf.CeilToInt(s.drinkingRange / _cfg.cellSize);

        for (int dx = -scanR; dx <= scanR; dx++)
            for (int dy = -scanR; dy <= scanR; dy++)
            {
                int nx = cx + dx, ny = cy + dy;
                if (!_grid.IsInside(nx, ny)) continue;

                float wx = nx * _cfg.cellSize;
                float wz = ny * _cfg.cellSize;
                float dist = Vector2.Distance(State.position, new Vector2(wx, wz));

                if (dist <= s.drinkingRange && _grid.Get(nx, ny).IsWater)
                {
                    MetabolismSystem.Drink(State, s, dt);
                    return true;
                }
            }
        return false;
    }
}