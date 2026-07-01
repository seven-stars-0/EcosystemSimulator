using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Camera con tre modalità:
//   - Free:   orbit + pan + zoom liberi
//   - Follow: pivot incollato all'animale, orbit e zoom attivi, camera punta esattamente l'animale
//   - POV:    camera posizionata agli "occhi" dell'animale, guarda dove guarda lui
//
// La modalità POV non usa pivot/arm, ma sovrascrive direttamente la Camera.main transform.
public class WorldCamera : MonoBehaviour
{
    [Header("References")]
    public Transform pivot;
    public Transform cameraArm;
    public Camera    mainCamera;

    [Header("Orbit")]
    public float orbitSpeedX = 0.3f;
    public float orbitSpeedY = 0.2f;
    public float pitchMin    = 10f;
    public float pitchMax    = 85f;

    [Header("Zoom")]
    public float zoomSpeed   = 3f;
    public float zoomMin     = 5f;
    public float zoomMax     = 150f;
    public float zoomDamping = 8f;

    [Header("Pan")]
    public float panSpeed   = 0.05f;
    public float panDamping = 8f;

    [Header("Arrow pan")]
    public float arrowSpeed = 20f;

    [Header("Bounds")]
    public float worldSizeX = 128f;
    public float worldSizeZ = 128f;

    [Header("POV")]
    public float povEyeHeight = 0.8f;
    public float povSensitivity = 0.3f;

    [Header("Follow indicator")]
    [SerializeField] private Transform followIndicator;
    [SerializeField] private float indicatorHeight = 2f;
    [SerializeField] private float indicatorSpinSpeed = 0f;    // gradi/s attorno alla verticale (0 = orientamento fisso)
    [SerializeField] private float indicatorBob = 0.12f;
    private Quaternion _indicatorBaseRot = Quaternion.identity;  // orientamento iniziale (es. 90° su X)
    private float _indicatorSpin;

    private float   _yaw, _pitch;
    private float   _targetZoom, _currentZoom;
    private Vector3 _targetPivot;

    // Follow state
    private Transform _followTarget;   // null = free mode

    // POV state
    private bool  _povMode;
    private float _povYaw, _povPitch;
    private int _animalSelfLayer = -1;
    private GameObject _povAnimalModel;

    // Cache
    private bool _isPointerOverUI;

    private void Awake()
    {
        if (pivot      == null) pivot      = transform.parent;
        if (mainCamera == null) mainCamera = Camera.main;

        Vector3 angles = pivot.eulerAngles;
        _yaw   = angles.y;
        _pitch = angles.x;

        _targetZoom  = _currentZoom = Mathf.Abs(cameraArm.localPosition.z);
        _targetPivot = pivot.position;

        _animalSelfLayer = LayerMask.NameToLayer("AnimalSelf");

        if (followIndicator != null) _indicatorBaseRot = followIndicator.rotation;
    }

    // ── API pubblica ──────────────────────────────────────────────────────────

    public void SetWorldBounds(float sizeX, float sizeZ)
    {
        worldSizeX = sizeX;
        worldSizeZ = sizeZ;
        zoomMax    = Mathf.Max(sizeX, sizeZ) * 0.8f;
    }

    public void MovePivotTo(Vector3 position)
    {
        _targetPivot   = position;
        pivot.position = position;
    }

    /// <summary>
    /// Imposta il target da seguire (modalità Follow).
    /// Passa null per tornare al free mode.
    /// </summary>
    public void SetFollowTarget(Transform target)
    {
        _followTarget = target;

        if (target == null)
        {
            // Esci anche da POV se si deseleziona l'animale
            ExitPOV();
            _targetPivot = pivot.position;
        }
    }

    /// <summary>
    /// Entra/esce dalla modalità POV.
    /// Richiede che _followTarget sia già impostato.
    /// </summary>
    public void TogglePOV()
    {
        if (_followTarget == null) return;

        if (_povMode) ExitPOV();
        else          EnterPOV();
    }

    public bool IsInPOV => _povMode;

    // ── Update ────────────────────────────────────────────────────────────────

    private void Update()
    {
        _isPointerOverUI = EventSystem.current != null
                           && EventSystem.current.IsPointerOverGameObject();

        if (_povMode)
            UpdatePOV();
        else if (_followTarget != null)
            UpdateFollow();
        else
            UpdateFree();

        UpdateIndicator();
    }

    // ── Modalità Free ─────────────────────────────────────────────────────────

    private void UpdateFree()
    {
        if (!_isPointerOverUI)
        {
            HandleOrbit();
            HandleMousePan();
            HandleZoom();
        }

        if (!InputGuard.CameraInputBlocked)
            HandleArrowPan();

        ApplyTransformsFree();
    }

    // ── Modalità Follow ───────────────────────────────────────────────────────

    private void UpdateFollow()
    {
        // Pivot = posizione animale, aggiornato ogni frame senza lerp
        Vector3 animalPos = _followTarget.position;
        pivot.position    = animalPos;
        _targetPivot      = animalPos;

        // Input: solo orbit e zoom (no pan in follow mode)
        if (!_isPointerOverUI)
        {
            HandleOrbit();
            HandleZoom();
        }

        ApplyTransformsFollow(animalPos);
    }

    // ── Modalità POV ─────────────────────────────────────────────────────────
    private void EnterPOV()
    {
        _povMode  = true;
        _povYaw   = _followTarget.eulerAngles.y;
        _povPitch = 0f;

        mainCamera.transform.SetParent(null, worldPositionStays: true);

        var animal = _followTarget.GetComponent<Animal>();
        if (animal != null && animal.modelRoot != null)
        {
            _povAnimalModel = animal.modelRoot;
            // Applica il layer ricorsivamente a tutto il sottoalbero
            SetLayerRecursive(_povAnimalModel, _animalSelfLayer);
        }

        mainCamera.cullingMask &= ~(1 << _animalSelfLayer);
    }

    private void ExitPOV()
    {
        if (!_povMode) return;
        _povMode = false;

        if (_povAnimalModel != null)
        {
            // Ripristina il layer Default (0) ricorsivamente
            SetLayerRecursive(_povAnimalModel, 0);
            _povAnimalModel = null;
        }

        mainCamera.cullingMask = -1;

        mainCamera.transform.SetParent(cameraArm, worldPositionStays: false);
        mainCamera.transform.localPosition = Vector3.zero;
        mainCamera.transform.localRotation = Quaternion.identity;
    }

    /// <summary>Imposta il layer sul GO e su tutti i suoi figli in modo ricorsivo.</summary>
    private static void SetLayerRecursive(GameObject root, int layer)
    {
        root.layer = layer;
        foreach (Transform child in root.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
    
    private void UpdatePOV()
    {
        if (_followTarget == null) { ExitPOV(); return; }

        // Mouse look sovrapposto alla rotazione dell'animale
        if (!_isPointerOverUI && Mouse.current != null && Mouse.current.rightButton.isPressed)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            _povYaw += delta.x * povSensitivity;
            _povPitch = Mathf.Clamp(_povPitch - delta.y * povSensitivity, -80f, 80f);
        }
        else
        {
            // Segue il forward dell'animale in modo fluido, senza scatti
            _povYaw = Mathf.LerpAngle(_povYaw, _followTarget.eulerAngles.y, Time.deltaTime * 5f);
        }

        mainCamera.transform.position = _followTarget.position + Vector3.up * povEyeHeight;
        mainCamera.transform.rotation = Quaternion.Euler(_povPitch, _povYaw, 0f);
    }

    // ── Transform application ─────────────────────────────────────────────────

    private void ApplyTransformsFree()
    {
        float dt       = Time.deltaTime;
        pivot.position = Vector3.Lerp(pivot.position, _targetPivot, panDamping * dt);
        pivot.rotation = Quaternion.Euler(_pitch, _yaw, 0f);

        _currentZoom         = Mathf.Lerp(_currentZoom, _targetZoom, zoomDamping * dt);
        Vector3 arm          = cameraArm.localPosition;
        arm.z                = -_currentZoom;
        cameraArm.localPosition = arm;

        // Assicura che la camera guardi il pivot (ridondante ma infallibile)
        if (!_povMode)
            mainCamera.transform.LookAt(pivot.position);
    }

    private void ApplyTransformsFollow(Vector3 animalPos)
    {
        // Pivot già settato sopra: pivot.position = animalPos
        pivot.rotation = Quaternion.Euler(_pitch, _yaw, 0f);

        _currentZoom         = Mathf.Lerp(_currentZoom, _targetZoom, zoomDamping * Time.deltaTime);
        Vector3 arm          = cameraArm.localPosition;
        arm.z                = -_currentZoom;
        cameraArm.localPosition = arm;

        // LookAt esplicito: la camera punta esattamente l'animale, qualunque cosa
        mainCamera.transform.LookAt(animalPos);
    }

    // ── Follow indicator ──────────────────────────────────────────────────────

    // Triangolino sopra l'animale seguito; nascosto in POV e quando non si segue.
    // _followTarget == null vale anche per un animale DISTRUTTO (morto) -> si nasconde.
    private void UpdateIndicator()
    {
        if (followIndicator == null) return;

        bool show = _followTarget != null && !_povMode;
        if (followIndicator.gameObject.activeSelf != show)
            followIndicator.gameObject.SetActive(show);
        if (!show) return;

        float bob = Mathf.Sin(Time.time * 3f) * indicatorBob;
        followIndicator.position = _followTarget.position + Vector3.up * (indicatorHeight + bob);

        // Orientamento BLOCCATO su quello iniziale (es. piramide ruotata 90° su X
        // per puntare in basso) + eventuale spin SOLO attorno alla verticale del
        // mondo. Cosi' la punta resta perpendicolare al terreno: niente drift su X.
        _indicatorSpin += indicatorSpinSpeed * Time.deltaTime;
        followIndicator.rotation = Quaternion.AngleAxis(_indicatorSpin, Vector3.up) * _indicatorBaseRot;
    }

    // ── Input handlers ────────────────────────────────────────────────────────

    private void HandleOrbit()
    {
        if (Mouse.current == null || !Mouse.current.rightButton.isPressed) return;
        Vector2 delta = Mouse.current.delta.ReadValue();
        _yaw   += delta.x * orbitSpeedX;
        _pitch  = Mathf.Clamp(_pitch - delta.y * orbitSpeedY, pitchMin, pitchMax);
    }

    private void HandleMousePan()
    {
        if (Mouse.current == null || !Mouse.current.middleButton.isPressed) return;
        Vector2 delta = Mouse.current.delta.ReadValue();
        ApplyPanDelta(-delta.x, -delta.y, panSpeed * (_currentZoom / 20f));
    }

    private void HandleArrowPan()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        float dx = 0f, dz = 0f;
        if (kb.leftArrowKey .isPressed || kb.aKey.isPressed) dx -= 1f;
        if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) dx += 1f;
        if (kb.upArrowKey   .isPressed || kb.wKey.isPressed) dz += 1f;
        if (kb.downArrowKey .isPressed || kb.sKey.isPressed) dz -= 1f;
        if (dx == 0f && dz == 0f) return;

        float speed = arrowSpeed * (_currentZoom / 20f) * Time.deltaTime;
        ApplyPanDelta(dx, dz, speed);
    }

    private void HandleZoom()
    {
        if (Mouse.current == null) return;
        float scroll = Mouse.current.scroll.ReadValue().y;
        _targetZoom -= scroll * zoomSpeed;
        _targetZoom  = Mathf.Clamp(_targetZoom, zoomMin, zoomMax);
    }

    private void ApplyPanDelta(float dx, float dz, float speed)
    {
        Quaternion yawOnly = Quaternion.Euler(0f, _yaw, 0f);
        _targetPivot += (yawOnly * Vector3.right   * dx +
                         yawOnly * Vector3.forward * dz) * speed;
        _targetPivot.x = Mathf.Clamp(_targetPivot.x, 0f, worldSizeX);
        _targetPivot.z = Mathf.Clamp(_targetPivot.z, 0f, worldSizeZ);
    }

    /// <summary>
    /// Copia i valori da CameraSettings nei campi pubblici di WorldCamera.
    /// Chiamato da AppSettingsManager al caricamento e da CameraPanel ad ogni modifica.
    /// </summary>
    public void ApplySettings(CameraSettings s)
    {
        orbitSpeedX = s.orbitSpeedX;
        orbitSpeedY = s.orbitSpeedY;
        pitchMin = s.pitchMin;
        pitchMax = s.pitchMax;
        zoomSpeed = s.zoomSpeed;
        panSpeed = s.panSpeed;
        panDamping = s.panDamping;
        arrowSpeed = s.arrowSpeed;
        povEyeHeight = s.povEyeHeight;
        povSensitivity = s.povSensitivity;
    }

    /// <summary>
    /// Copia i valori correnti di WorldCamera dentro un CameraSettings.
    /// Usato da CameraPanel per leggere i valori attuali prima del Bind.
    /// </summary>
    public void SaveToSettings(CameraSettings s)
    {
        s.orbitSpeedX = orbitSpeedX;
        s.orbitSpeedY = orbitSpeedY;
        s.pitchMin = pitchMin;
        s.pitchMax = pitchMax;
        s.zoomSpeed = zoomSpeed;
        s.panSpeed = panSpeed;
        s.panDamping = panDamping;
        s.arrowSpeed = arrowSpeed;
        s.povEyeHeight = povEyeHeight;
        s.povSensitivity = povSensitivity;
    }
}