using UnityEngine;


// SOLO attuatore fisico
public class Animal : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("POV")]
    [SerializeField] public GameObject modelRoot;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    public AnimalState State { get; private set; }
    public bool IsAlive => State != null && State.IsAlive;

    private WorldGrid    _grid => MapSession.Instance != null ? MapSession.Instance.CurrentMap?.grid : null;
    private RenderConfig _cfg  => WorldSession.Instance != null ? WorldSession.Instance.Renderer.config : null;

    // Chiamato da SimulationRunner per spawnare le SpawnEntry
    public void Initialize(AnimalSpecies species, float worldX, float worldZ, GeneticProfile genes, int id)
    {
        float h = _grid.SampleHeight(worldX / _cfg.cellSize, worldZ / _cfg.cellSize) * _cfg.heightScale;

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
    }

    // Chiamato quando un animale si riproduce
    public void InitializeFromState(AnimalState state)
    {
        State = state;

        float h = _grid.SampleHeight(state.position.x / _cfg.cellSize,
                                     state.position.y / _cfg.cellSize) * _cfg.heightScale;
        transform.position = new Vector3(state.position.x, h, state.position.y);
    }

    // Chiamato in SimulationRunner.TickAnimal dopo aver calcolato il vettore percezione (PerceptionSystem) e calcolato il vettore acceleraziune (SteeringSystem)
    public void ApplySteering(Vector2 velCommand, float dt)
    {
        // velCommand e' gia' la velocita' desiderata (direzione + modulo <= maxSpeed)
        // decisa da SteeringSystem. Qui la integriamo soltanto: nessun clamp di
        // maxSpeed duplicato (Vector2.Lerp non estrapola, quindi resta sotto il max).
        State.velocity = Vector2.Lerp(State.velocity, velCommand, dt * 5f);

        State.position += State.velocity * dt;

        // Questa parte serve per non far uscire gli animali dai bordi
        // Mantiene l'animale dentro 0 <= x <= maxW, e annulla le eventuali componenti della velocità che puntano verso l'esterno
        float maxW = (_grid.size - 1) * _cfg.cellSize;
        if (State.position.x < 0f)    { State.position.x = 0f;   if (State.velocity.x < 0f) State.velocity.x = 0f; }
        if (State.position.x > maxW)  { State.position.x = maxW; if (State.velocity.x > 0f) State.velocity.x = 0f; }
        if (State.position.y < 0f)    { State.position.y = 0f;   if (State.velocity.y < 0f) State.velocity.y = 0f; }
        if (State.position.y > maxW)  { State.position.y = maxW; if (State.velocity.y > 0f) State.velocity.y = 0f; }

        // Spostiamo l'animale se è finito dentro l'acqua
        PushOutOfWater();

        // Cambia concretamente la posizione dell'animale 
        float wx = State.position.x / _cfg.cellSize;
        float wz = State.position.y / _cfg.cellSize;
        float worldY = _grid.SampleHeight(wx, wz) * _cfg.heightScale;
        transform.position = new Vector3(State.position.x, worldY, State.position.y);

        // Rotazione morbida verso il forward della velocità
        if (State.velocity.sqrMagnitude > 0.01f)
        {
            var targetRot = Quaternion.LookRotation(
                new Vector3(State.velocity.x, 0f, State.velocity.y), Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, dt * 8f);
        }

        if (animator != null) animator.SetFloat(SpeedHash, State.velocity.magnitude);
    }

    private void PushOutOfWater()
    {
        int cx = Mathf.RoundToInt(State.position.x / _cfg.cellSize);
        int cy = Mathf.RoundToInt(State.position.y / _cfg.cellSize);

        if (!_grid.IsInside(cx, cy)) return;
        if (!_grid.Get(cx, cy).IsWater) return; // Se non siamo in acqua non c'è problema

        float bestDist = float.MaxValue;
        Vector2 bestPos = State.position;
        bool found = false;

        // Troviamo in un raggio di 2 la cella non-acqua più vicina
        for (int dx = -2; dx <= 2; dx++)
            for (int dy = -2; dy <= 2; dy++)
            {
                int nx = cx + dx, ny = cy + dy;
                if (!_grid.IsInside(nx, ny)) continue;
                if (_grid.Get(nx, ny).IsWater) continue;

                float wx = nx * _cfg.cellSize, wz = ny * _cfg.cellSize;
                float ddx = wx - State.position.x, ddz = wz - State.position.y;
                float dist = ddx * ddx + ddz * ddz;

                if (dist < bestDist) { bestDist = dist; bestPos = new Vector2(wx, wz); found = true; }
            }

        if (!found) return;

        // Se esiste una cella, l'animale viene teletrasportato lì e la magnitudine della velocià ridotta
        // Questo serve per evitare che l'animale si ributti immediatamente in acqua, bloccandolo in un ciclo eterno
        // SteeringSystem cerca di prevenire queste situazioni, ma il metodo è qui per sicurezza
        State.position = bestPos;
        State.velocity *= 0.3f;
    }
}
