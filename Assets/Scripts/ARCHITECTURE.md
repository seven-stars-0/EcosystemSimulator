# EcoSim — Architettura del Progetto

EcoSim è un simulatore di ecosistema evolutivo con editor di mappe integrato.
Il giocatore modella un territorio, vi posiziona animali e piante, poi avvia
una simulazione in cui le specie interagiscono e si evolvono per selezione
naturale. Il progetto è sviluppato in Unity (URP, C#, New Input System).

---

## Principio organizzativo

Il codice è organizzato in **layer**: ogni layer dipende solo dal layer
immediatamente sotto di sé, mai da quello sopra.

```
┌─────────────────────────────────────────────────────────┐
│  UI          Schermate, popup, widget                   │
│  (dipende solo da Facade)                               │
└───────────────────────┬─────────────────────────────────┘
                        │
┌───────────────────────▼─────────────────────────────────┐
│  Facade      WorldSession · SimulationSession           │
│  (punto d'ingresso unico per la UI verso i sistemi)     │
└──────────┬────────────────────────────┬─────────────────┘
           │                           │
┌──────────▼──────────┐   ┌────────────▼────────────────┐
│  World Systems      │   │  Simulation Systems         │
│  WorldBuilder       │   │  SimulationRunner           │
│  WorldRenderer      │   │  PlantManager               │
│  WorldEditor        │   │  PerceptionSystem           │
│  WorldCamera        │   │  SteeringSystem             │
└─────────────────────┘   │  MetabolismSystem           │
                          │  ReproductionSystem         │
                          └─────────────────────────────┘
                                        │
┌───────────────────────────────────────▼─────────────────┐
│  Data        MapData · WorldGrid · SimulationSettings   │
│  (C# puro, nessuna dipendenza da Unity)                 │
└─────────────────────────────────────────────────────────┘
```

**Regola pratica**: se un file in `UI/` ha un `using` verso `WorldBuilder`
o `SimulationRunner`, è un bug architetturale. La UI parla solo con
`WorldSession` e `SimulationSession`.

---

## Layer Data — `Data/`

Strutture dati serializzabili, nessuna dipendenza da MonoBehaviour o Unity.
Sono il "documento" che descrive una mappa e le sue impostazioni.

### `MapData`
Il contenitore principale di tutto ciò che riguarda una mappa salvata.
Contiene tre parti:
- `MapMetadata` — nome, dimensione griglia, timestamp, flag `isDirty`
- `WorldGrid` — la griglia del terreno con tutte le celle
- `SimulationSettings` — tutti i parametri della simulazione (fame, sete,
  genetica, predazione, ecc.)
- `List<SpawnEntry>` — le entità piazzate dal giocatore (animali e piante),
  con coordinate mondo precise

### `WorldGrid`
Griglia bidimensionale di `CellData`. Ogni cella ha:
- `height` — altezza del terreno (negativa = acqua)
- `fertility` — fertilità del suolo (0–1), usata dalla crescita delle piante
- `obstacle` — tipo di ostacolo (nessuno, albero, roccia)
- `gradientX/Y`, `slope` — pre-calcolati al salvataggio, usati dalla
  percezione degli animali per evitare/cercare pendenze

Espone `SampleHeight(x, z)` per l'interpolazione bilineare dell'altezza
in coordinate continue (gli animali si muovono su coordinate reali, non
su griglia intera).

### `SimulationSettings`
Un oggetto `[Serializable]` con tutti i parametri numerici della simulazione.
Viene serializzato dentro `MapData`: ogni mappa ha le sue impostazioni.
Viene modificato dall'utente tramite `ParameterPanel` nella schermata Settings.

---

## Layer Data — `Simulation/Genetics/`

### `GeneticProfile`
Il vettore genetico di un animale. Contiene 11 geni divisi in due gruppi:

**Pesi comportamentali** (scalano i vettori di percezione nello steering):
- `w_food` — attrazione verso il cibo
- `w_water` — attrazione verso l'acqua
- `w_social` — attrazione/repulsione verso i conspecifici (può essere negativo)
- `w_slope` — attrazione/repulsione verso le pendenze
- `w_curiosity` — peso del vettore random (esplorazione)
- `w_flee` — fuga dai predatori (solo prede)

**Parametri fisiologici**:
- `maxSpeed` — velocità massima base
- `visionRange` — raggio di percezione
- `metabolismMult` — moltiplicatore del costo energetico
- `reproductionThreshold` — energia minima per riprodursi
- `bodySize` — dimensione corporea (influenza velocità effettiva, capacità
  energetica e scala visiva del prefab)

`EffectiveMaxSpeed = maxSpeed / bodySize^0.4` — animali più grandi sono
più lenti. `EnergyMax = 1 + (bodySize - 1) * 0.5`.

### `GeneticOps`
Operatori genetici puri (nessuno stato):
- `Reproduce(parentA, parentB, settings)` — crossover uniforme (lerp
  casuale per ogni gene) seguito da mutazione gaussiana con probabilità
  `mutationRate`. I valori risultanti vengono clampati a range biologicamente
  sensati.

---

## Layer World Systems — `Simulation/WorldBuilder.cs`

`WorldBuilder` è il costruttore del mondo 3D. Legge `MapData` e istanzia i
GameObject corrispondenti nella scena Unity. È l'unico sistema che crea e
distrugge prefab.

**Modalità editor** (`BuildForEditor`): istanzia tutti gli spawn entry come
prefab statici. Abilita `WorldEditor` per l'interazione dell'utente.

**Modalità simulazione** (`BuildForSimulation`): istanzia solo terrain e
ostacoli. Animali e piante non vengono toccati — `SimulationRunner` e
`PlantManager` li gestiscono autonomamente con la loro logica.

`WorldBuilder` mantiene due dizionari runtime per tracciare i GO istanziati
(`_obstacleObjects`, `_entityObjects`) e offre API a `SpawnTool` per
piazzare/rimuovere entità durante l'editing.

`SyncEntitiesHeight()` viene chiamato da `TerrainTool` dopo ogni modifica
al terreno: aggiorna la Y di tutti i GO istanziati e rimuove quelli finiti
sott'acqua.

---

## Layer World Systems — `Rendering/`

### `WorldRenderer`
Coordina le view grafiche del terreno. Mantiene un flag `DirtyFlags` e
in `LateUpdate` refresha solo le view marcate dirty. Espone API per
`SpawnTool` (`SetSpawnOverlayVisible`) e per i tool in generale
(`MarkDirty`).

### `TerrainView`
Genera la mesh del terreno da `WorldGrid`. Ogni vertice ha posizione
(x, height*heightScale, z) e un colore calcolato dall'altezza (gradiente
da acqua a neve). Ha un `MeshCollider` per il raycast dell'editor.

### `SpawnOverlayView`
Mesh semi-trasparente sovrapposta al terreno, visibile solo quando
`SpawnTool` è attivo. Colora ogni cella in verde (si può piazzare) o
rosso (non si può), usando la stessa geometria del terreno ma con un
offset Y per evitare z-fighting. Ogni cella ha 4 vertici non condivisi
così ogni quad ha il suo colore indipendente.

---

## Layer World Systems — `Editing/`

### `WorldEditor`
MonoBehaviour che cattura l'input del mouse e lo delega all'`IEditorTool`
attivo. Prima di processare qualsiasi click controlla
`EventSystem.IsPointerOverGameObject()` per non interferire con la UI.
Gestisce il ciclo drag: `OnDragStart` → `OnDrag` → `OnDragEnd`.

### `TerrainTool`
Modifica le altezze della `WorldGrid` con un pennello gaussiano. Parametri
(forza, raggio, alza/abbassa, smooth) esposti a `TerrainToolBar`.
Dopo ogni `Apply` chiama `WorldBuilder.SyncEntitiesHeight()` e marca
dirty il terreno. Al termine del drag ricalcola i gradienti e marca dirty
`SpawnOverlay`.

### `SpawnTool`
Piazza e rimuove entità ed ostacoli sulla mappa. In modalità normale
delega a `WorldBuilder.PlaceObstacle/PlaceEntity`. In erase mode delega
a `WorldBuilder.EraseObstacle/EraseEntity`. Attivando il tool mostra
`SpawnOverlayView`; disattivandolo la nasconde. Non riceve riferimenti
a sistemi Unity nel costruttore — accede a `WorldSession.Instance`
direttamente (accettabile per un tool di editing).

---

## Layer Simulation Systems — `Simulation/Agents/`

Il sistema di simulazione degli animali è completamente **data-driven** e
**stateless** lato sistemi: ogni sistema (Perception, Steering, Metabolism,
Reproduction) è una classe statica con metodi puri che ricevono lo stato
e restituiscono un risultato. Lo stato vive in `AnimalState`; il
MonoBehaviour `Animal` fa da bridge tra il dato e Unity.

### `AnimalState`
Stato runtime di un animale: energia, fame, sete, posizione XZ, velocità,
direzione wander, cooldown riproduzione e attacco, età, numero di figli.
Non è un MonoBehaviour: è un oggetto C# puro gestito da `SimulationRunner`.

### `Animal` (MonoBehaviour)
Il componente Unity sull'animale. Contiene `AnimalState` e i riferimenti
a `WorldGrid`, `RenderConfig` e `SimulationSettings` necessari per
muoversi nel mondo. `SimulationRunner` chiama `ApplySteering(accel, dt)`
ogni frame.

`ApplySteering` esegue: integrazione della velocità → clamp a maxSpeed →
aggiornamento posizione → clamp ai bordi con azzeramento velocità →
`PushOutOfWater` (se l'animale finisce in acqua per effetto della
separazione, viene respinto sulla cella terra più vicina) → campionamento
dell'altezza da `WorldGrid.SampleHeight` → rotazione verso la direzione
di movimento → aggiornamento parametro Animator `Speed`.

La scala visiva del prefab viene impostata a `bodySize^0.5` al momento
dell'inizializzazione: animali con bodySize=2 sono ~1.4× più grandi di
quelli con bodySize=1.

### `PerceptionData`
Struct con tutti i vettori di percezione calcolati per un animale in un
frame: `toFood`, `toWater`, `socialVector`, `separationVector`,
`fleeVector`, `mateVector`, `slopeVector`, `wanderVector`,
`currentSlope`. Tutti in spazio XZ (2D).

### `PerceptionSystem`
Calcola `PerceptionData` per un animale. Logica per vettore:

- **Food (prede)**: cerca la cella con frutto più vicina tramite
  `PlantManager.GetFruitCellsInRadius`. La magnitudine del vettore
  decade linearmente con la distanza: `1 - dist/range`.
- **Food (predatori)**: cerca la preda più vicina nella `SpatialGrid`.
- **Water**: cerca la cella acqua più vicina nel range. Se non ne trova,
  usa la cella con altezza minima come euristica (le valli portano
  all'acqua). Il vettore ha magnitudine ridotta (0.5) in questo caso.
- **Social + Separation + Flee**: un unico loop sugli animali vicini
  (ottimizzazione: tre comportamenti, un solo passaggio). Coesione
  sociale = media pesata verso i conspecifici. Separazione = forza di
  allontanamento proporzionale alla violazione del personal space.
  Fuga = vettore opposto al centro di massa dei predatori visibili.
- **Mate**: cerca il conspecifico più vicino che soddisfa le condizioni
  di riproduzione. Attivo solo quando `AnimalState.CanMate()` è true.
- **Wander**: random walk smussato, direzione ruotata di ±120°/s (scalato
  con `dt` simulazione). Motore dell'esplorazione quando gli altri stimoli
  sono deboli.
- **Slope**: legge `gradientX/Y` pre-calcolati da `CellData`. O(1).

### `SteeringSystem`
Combina i vettori di `PerceptionData` con i geni e lo stato interno in un
singolo vettore accelerazione. Formula:

```
accel = toFood  * w_food  * hungerUrgency    // solo se hunger > 0.15
      + toWater * w_water * thirstUrgency    // solo se thirst > 0.15
      + (mateVector * mateBoost  OR  socialVector * w_social)
      + separationVector                     // sempre, indipendente dai geni
      + fleeVector * w_flee                  // solo prede, se predatore vicino
      + slopeVector * w_slope
      + wanderVector * w_curiosity
      + borderRepulsion                      // forza morbida vicino ai bordi
      + waterRepulsion                       // forza morbida vicino all'acqua
```

`hungerUrgency = hunger * urgencyMax` — parte da 0 (non urgente) e sale
fino a `urgencyMax`. Questo garantisce che animali sazi non vengano
costantemente attratti da cibo/acqua, risolvendo il problema storico dei
predatori incollati alla costa.

### `MetabolismSystem`
Aggiorna fame, sete, energia e cooldown ogni tick. Costi energetici:
base metabolico + costo proporzionale alla velocità + costo proporzionale
alla pendenza × velocità. Oltre soglia 0.8 di fame/sete, l'energia cala
più rapidamente (danno da digiuno/disidratazione).

### `ReproductionSystem`
`TryMate(a, b, settings)` — verifica condizioni (energia, cooldown,
stessa specie, distanza di accoppiamento) e restituisce un
`GeneticProfile` figlio se le condizioni sono soddisfatte. Entrambi i
genitori pagano `offspringEnergyFraction/2` di energia e iniziano il
cooldown. La prole nasce con cooldown pieno (evita esplosione demografica).

---

## Layer Simulation Systems — `Simulation/Core/`

### `SimulationRunner`
Loop principale della simulazione. Ogni frame (scalato da `timeScale`):
1. `PlantManager.Tick(dt)` — gestione piante
2. Rebuild `SpatialGrid` — O(n), una volta per frame
3. Per ogni animale: percezione → steering → movimento → metabolismo →
   azione specie-specifica (mangia frutto / attacca preda) → bevi →
   riproduci → controlla morte
4. Spawn prole (fuori dal loop principale per evitare modifica durante iterazione)
5. Rimozione morti con decremento O(1) dei contatori

Mantiene contatori O(1) per prede, predatori e i rispettivi massimi storici
(`MaxPreyCount`, `MaxPredatorCount`), esposti a `SimulationHUD`.

**Attacco predatori**: il predatore deve avere fame, il suo `attackCooldown`
deve essere 0. All'attacco: knockback sulla preda (velocità nella direzione
opposta al predatore), EatPrey sul predatore, imposta `attackCooldown`.
La preda viene rimossa nel batch post-loop.

### `SpatialGrid<T>`
Griglia spaziale per query di prossimità O(k) dove k = elementi nel bucket.
Rebuild ogni frame in O(n). Usata da `PerceptionSystem` per trovare animali
vicini senza iterare tutta la lista. Bucket size ≈ drinkingRange.

---

## Layer Simulation Systems — `Simulation/Plants/`

### `PlantManager`
Gestisce il ciclo di vita delle piante procedurali e di quelle piazzate
dall'editor. Ogni 2 secondi di simulazione (`GROWTH_TICK_INTERVAL`):

- **Crescita spontanea**: per ogni cella terra sopra `plantMinHeight`,
  probabilità = `fertility × plantGrowthRate × dt`. Nuove piante
  nascono senza frutti.
- **Crescita frutti**: il timer frutto decrementa moltiplicato per la
  fertilità della cella (terra più fertile → frutti più veloci).
  Quando scade: `hasFruit = true`, indicatore rosso visibile.
- **Morte naturale** (solo piante procedurali): probabilità proporzionale
  a `(1 - fertility)`.

Le piante piazzate dall'editor (`isPermanent = true`) non muoiono mai.

Quando una preda mangia un frutto: `TryEat(cx, cy)` → `hasFruit = false`
→ timer ripartito → indicatore rosso nascosto.

`ActivePlantCount` è un contatore O(1) (incrementato in `SpawnPlant`,
decrementato in `KillPlant`), esposto a `SimulationHUD`.

---

## Layer Facade — `Core/`

I facade sono l'unico punto di contatto tra UI e sistemi. Nascondono
la complessità interna e garantiscono che aggiungere una nuova schermata
non richieda di toccare WorldBuilder, SimulationRunner o altri sistemi.

### `WorldSession`
Coordina tutti i sistemi del mondo 3D. Due entry point:
- `EnterEditor(map)` — costruisce il mondo in modalità editing
- `EnterSimulation(map)` — costruisce il mondo per la simulazione
- `Exit()` — smonta il mondo, resetta la camera, pulisce MapSession

Espone `Builder`, `Renderer`, `Editor`, `Camera` come proprietà pubbliche.
È un singleton accessibile via `WorldSession.Instance`.

### `SimulationSession`
Coordina la simulazione. Entry point: `Begin(data)`, `Stop()`,
`Pause()`, `Resume()`. Espone `TimeScale` e `Paused` come proprietà.

Rileva l'estinzione tramite **polling** in `Update()` (non eventi da
`SimulationRunner`): se dopo 5 secondi entrambi i contatori sono 0, solleva
`OnExtinctionEvent` (evento statico con dati) e si mette in pausa.
`_extinctionFired` previene la notifica multipla e viene resettato a ogni
`Begin()`.

### `MapSession`
Singleton che tiene il riferimento alla `MapData` correntemente caricata.
`MarkDirty()` setta `metadata.isDirty = true`.

---

## Layer Save System — `SaveSystem/`

### `MapSaveManager`
Serializza/deserializza `MapData` in JSON (Newtonsoft.Json) su disco.
- **Editor**: salva in `Application.dataPath/Maps/`
- **Build**: salva in `Application.persistentDataPath/Maps/`

Prima di salvare: ricalcola i gradienti della griglia (slope pre-computati
una volta sola per il runtime). Gestisce il rename del file se il nome
della mappa cambia.

---

## Layer Camera — `Camera/`

### `WorldCamera`
Tre modalità operative:

**Free**: orbit (tasto destro mouse), pan (tasto centrale + frecce),
zoom (scroll). Input bloccato se `EventSystem.IsPointerOverGameObject()`
o se `InputGuard.CameraInputBlocked` è true.

**Follow**: pivot posizionato direttamente sulla `Transform` dell'animale
ogni frame (nessun lerp — la camera è sempre esattamente sull'animale).
`mainCamera.LookAt(animalPos)` garantisce che l'animale sia sempre al
centro dell'immagine. Orbit e zoom restano funzionanti.

**POV** (prima persona): la camera viene staccata dalla gerarchia
(`SetParent(null)`) — prima di disabilitare il pivot, altrimenti la
camera verrebbe disabilitata con esso. Posizionata agli "occhi"
dell'animale con un offset Y. Yaw segue l'animale, tasto destro mouse
per guardarsi liberamente. Al layer `AnimalSelf` viene applicato
ricorsivamente all'intera mesh dell'animale seguito: la camera esclude
quel layer (`cullingMask &= ~AnimalSelf`) così non si vede la mesh
dall'interno.

---

## Layer UI — `UI/`

### `UIScreen` (classe base)
Ogni schermata è un `GameObject` con `CanvasGroup` e un componente che
estende `UIScreen`. `Show()` attiva il GO e setta alpha=1. `Hide()` setta
`interactable/blocksRaycasts = false` e avvia un coroutine di fade-out.
`HideImmediate()` nasconde senza animazione.

**Inizializzazione**: `UIScreen.Awake()` acquisisce il `CanvasGroup` ma
non nasconde il GO. È `UIManager.Awake()` che forza `SetActive(true)`
su ogni schermata (per garantire che `Awake` giri), poi chiama
`HideImmediate()`. Questo risolve il bug storico dove schermate inattive
nell'Inspector non avevano `Awake` girato e `Show()` re-triggerava `Awake`
nascondendole immediatamente.

### `UIManager`
Singleton che gestisce la navigazione a stack (`Show`, `GoBack`,
`GoToMain`) e la creazione dei popup. `NavigateClean<T>()` svuota la
history e naviga direttamente al target: usato dopo la fine di una
simulazione per evitare che history residue causino `GoBack()` inattesi.

Forza l'inizializzazione dei popup (`ForceInitPopup`) in `Awake`: tutti
i popup devono avere il loro `Awake` girato prima di essere usati.

### Schermate principali
- **MainScreen**: punto di ingresso, pulsante Explore e Quit.
- **MapSelectionScreen**: lista delle mappe salvate. Ogni `MapRow` ha
  pulsanti Play, Edit, Delete. "+" apre `EnterValuePopup` per la
  dimensione, poi entra in EditorHUD.
- **EditorHUD**: HUD dell'editor. ToggleGroup per selezionare TerrainTool
  o SpawnTool. Pulsanti Save (apre popup per il nome) e Back (confirm se
  dirty). Delega la gestione dei tool a `WorldEditor` e i toolbar a
  `TerrainToolBar`/`SpawnToolBar`.
- **SimulationHUD**: HUD della simulazione. Slider velocità, pause/resume,
  stop (con confirm), statistiche (prede, predatori, piante, tempo).
  Click su animale → camera follow. Pulsante POV (visibile solo con
  animale selezionato).
- **SettingsScreen**: pannello `ParameterPanel` con tutti i parametri di
  simulazione e `FertilityPanel` per la fertilità del terreno.

### Popup
- **ConfirmPopup**: dialogo generico sì/no.
- **EnterValuePopup**: raccoglie un valore stringa (per il nome mappa) o
  un intero via slider (per la dimensione mappa). Blocca la camera tramite
  `InputGuard` mentre è aperto.
- **ExtinctionPopup**: appare quando tutti gli animali muoiono. Mostra
  tempo simulazione, massimo prede e predatori. Pulsanti: Quit (torna
  alla selezione), Edit Map (torna all'editor), Restart (riavvia la
  simulazione con gli stessi parametri). Si iscrive a
  `SimulationSession.OnExtinctionEvent` in `Awake` (non in `OnEnable`)
  per resistere ai cicli `SetActive` di `ForceInitPopup`.

---

## Flusso di navigazione tipico

```
App start
  → UIManager.Start → MainScreen

MainScreen → "Explore"
  → MapSelectionScreen

MapSelectionScreen → "+" New
  → EnterValuePopup (dimensione griglia)
  → MapData.CreateEmpty(gridSize)
  → EditorHUD.PrepareForMap → EditorHUD

EditorHUD → "Save"
  → EnterValuePopup (nome mappa)
  → MapSaveManager.Save → GoBack → MapSelectionScreen

MapSelectionScreen → "Play"
  → WorldSession.EnterSimulation
  → SimulationSession.Begin
  → SimulationHUD

SimulationHUD → animali si estinguono
  → SimulationSession.OnExtinctionEvent
  → ExtinctionPopup

ExtinctionPopup → "Restart"
  → WorldSession.EnterSimulation
  → SimulationSession.Begin
  → NavigateClean<SimulationHUD>
```

---

## File di supporto

- `InputGuard.cs` — flag statico `CameraInputBlocked`. Settato a `true`
  da `SettingsScreen.OnShow()` e dai popup mentre sono aperti. Letto da
  `WorldCamera` per bloccare le frecce.
- `AppState.cs` — eventuali stati globali trasversali (estendibile).
- `RenderConfig.cs` — `cellSize` e `heightScale`: le due costanti che
  convertono tra coordinate griglia e coordinate mondo. Usate da quasi
  tutti i sistemi.
- `CellHit.cs` — struct restituita dal raycast di `WorldEditor`:
  coordinate griglia (x, y), posizione mondo del hit, normale, riferimento
  alla `CellData`.

---

## Prefab

```
Prey.prefab / Predator.prefab
  Root         Animal · AnimalPreset(species) · Collider
  └─ Model     MeshFilter · MeshRenderer
  └─ [opz.] ModelRoot  (campo su Animal per POV layer switch)

Plant_Sim.prefab  (piante procedurali, gestite da PlantManager)
  Root
  └─ Mesh
  └─ FruitIndicator  ← nome esatto richiesto; GameObject con sfera rossa

Plant_Editor.prefab  (piante statiche nell'editor, nessuna logica)
  Root
  └─ Mesh

Tree_N.prefab / Rock_N.prefab  (ostacoli, array random in WorldBuilder)
  Root
  └─ Mesh
```