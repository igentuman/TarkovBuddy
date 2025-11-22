# **Tarkov Buddy -- Technical Specification**

A desktop assistant application for Escape from Tarkov players,
providing real‑time visual recognition, map assistance, quest guidance,
stash analytics, and item evaluation.

------------------------------------------------------------------------

## **1. Purpose**

Tarkov Buddy enhances gameplay by analyzing the user's screen in real
time and providing contextual help: - Detect map and spawn point
automatically. - Show interactive map overlays. - Display quest
objectives and TODO lists. - Evaluate loot value (hideout needs, quest
needs, flea price). - Index items in stash and track how many are
needed. - Recognize text and UI windows. - Provide extensible modules
for future features.

------------------------------------------------------------------------

## **2. Core Features**

### **2.1 Map & Spawn Recognition**

-   Capture screen when raid loads.
-   OCR reads extraction list (always shown at spawn).
-   Match extraction set to a known map.
-   Determine spawn area by intersections of extractions.
-   Hotkey to open overlay with a high‑resolution PNG map.
-   User manually refines spawn point via clicking on map.

### **2.2 State Machine**

Application maintains a global game state: - **In Launcher** - **In
Lobby** - **In Hideout** - **Loading Raid** - **In Raid** -
**Extracted** - **Died** - **Inventory / Stash Screen** - **Flea Market
Screen** Each state enables or disables specific modules.

### **2.3 Quest System Integration**

When entering a raid: - OCR extracts active quests for this map. -
Application loads quest locations (JSON database). - Shows TODO list
overlay: - Quest name - Objective list - Approx. locations on the map -
Optional route suggestion (future)

### **2.4 Item Evaluation / Loot Helper**

When user inspects an item: - OCR reads item name from item properties
window. - App displays: - **Required for Hideout** -- how many total vs
user-owned. - **Required for Quests** -- which quests and how many. -
**Average flea market price** (from periodically refreshed local DB). -
**Should loot?** (Yes/No recommendation based on inventory)

### **2.5 Stash Indexing**

While player is in stash: - App scans stash grid area. - OCR +
object-detection identifies all visible items. - Builds **Stash
Inventory Database**:
`json   {     "Bolts": 14,     "Screw Nuts": 6,     "Crickent Lighter": 3,     "CPU Fan": 12   }` -
Used to determine if player already has enough for future needs.

### **2.6 Search & Filters Support**

If player uses the in-game search/filer UI: - OCR extracts filter
text. - App limits detection to matching items only for faster
execution.

------------------------------------------------------------------------

## **3. Architecture**

### **3.1 High-Level Structure**

    /TarkovBuddy
     ├── Core
     │    ├── AppStateMachine
     │    ├── ScreenCaptureService
     │    ├── OcrProcessingService
     │    ├── ObjectDetectionService
     │    ├── OverlayRenderer
     │    └── HotkeyManager
     ├── Modules
     │    ├── MapRecognition
     │    ├── SpawnInference
     │    ├── QuestAssistant
     │    ├── LootEvaluator
     │    ├── StashIndexer
     │    └── FleaPriceProvider
     ├── Data
     │    ├── maps/
     │    ├── quests/
     │    ├── hideout/
     │    ├── flea_prices/
     │    └── items/
     └── UI
          ├── Overlay
          ├── SettingsWindow
          └── Logs

### **3.2 Multithreading Model**

All major systems run independently:

**Thread: Screen Capture**\
- Captures screen every 33ms (30 FPS) or when triggered.

**Thread: OCR**\
- Receives frames → processes with Tesseract / EasyOCR / PaddleOCR.

**Thread: Object Detection (ONNX)**\
- YOLOv8n model for detecting item icons & UI elements.

**Thread: State Machine**\
- Coordinates all modules.

**Thread: Overlay Renderer**\
- Draws transparent window using DirectX.

**Thread: Network Updater** (optional)\
- Refresh flea market prices daily.

------------------------------------------------------------------------

## **4. AI / OCR / Recognition Technologies**

### **4.1 OCR**

Recommended engines: - **EasyOCR** (fast, supports Cyrillic + English +
numbers) - **Tesseract 5** (good for clean text) - **PaddleOCR**
(excellent accuracy)

Custom trained character whitelist:

    ABCDEFGHIJKLMNOPQRSTUVWXYZ
    abcdefghijklmnopqrstuvwxyz
    0123456789-()[]:.

### **4.2 Object Detection**

Used for: - Item icons in stash - Map loading screen detection -
Healing/energy/hydration UI detection - Item properties window

Recommended: - **YOLOv8n / YOLOv8s ONNX** - Input: 640×640 - Approx.
60--120 FPS on modern GPU

------------------------------------------------------------------------

## **5. Overlay System**

### **Requirements**

-   Transparent always-on-top window
-   No click-blocking (click-through)
-   Hotkeys for toggling map, TODO, stash helper
-   DirectX 11 or WinUI3 Window with WS_EX_TRANSPARENT

### **Components**

-   Minimap / full map
-   Quest checklist
-   Loot evaluator popup
-   Stash statistics sidebar

------------------------------------------------------------------------

## **6. Technical Data Structures**

### **6.1 Item Database Entry**

``` json
{
  "id": "bolts",
  "name": "Bolts",
  "hideout_required_total": 10,
  "quests_required": {
    "Farming Part 1": 2,
    "Fertilizers": 1
  },
  "avg_flea_price": 14500
}
```

### **6.2 Player Stash Inventory**

``` json
{
  "Bolts": 14,
  "Screw Nuts": 6,
  "CPU Fan": 12
}
```

### **6.3 Spawn Detection Profile**

``` json
{
  "map": "Woods",
  "extractions": ["UN Roadblock", "RUAF", "Outskirts"],
  "possible_spawns": ["South Road", "Lake Area"]
}
```

------------------------------------------------------------------------

## **7. State Machine Logic**

    [Launcher]
         ↓ (detect Main Menu)
    [Lobby]
         ↓ (open character inventory)
    [Hideout] ←→ [Lobby]
         ↓ (press Ready)
    [Loading Raid]
         ↓ (detect loading screen gone)
    [In Raid]
         ↓ (extraction detected or death)
    [Extracted] or [Died]

Each transition triggers: - Start/stop OCR - Clear overlays - Refresh
quest list - Run stash scan (when opening inventory)

------------------------------------------------------------------------

## **8. Extensibility**

### **Future planned modules:**

-   **Live audio alerts** (sniper zone, dehydration warning)
-   **Automatic compass direction overlay**
-   **Grenade trajectory predictor**
-   **AI route planner for quests**
-   **Dynamic risk heatmap (based on community data)**

Architecture supports plugin-style modules:

    IModule
    {
        void OnStateEnter(GameState state);
        void OnFrame(FrameData frame);
        void OnHotkey(HotkeyAction action);
    }

------------------------------------------------------------------------

## **9. Tech Stack**

-   **Language:** C#
-   **Runtime:** .NET 8
-   **GUI:** WinUI 3 or WPF + D3D overlay
-   **OCR:** EasyOCR/Tesseract
-   **Detection:** ONNX Runtime, YOLOv8 models
-   **Screen capture:** Windows Graphics Capture API
-   **Configuration:** JSON files
-   **Logging:** Serilog

------------------------------------------------------------------------

## **10. Performance Considerations**

-   Frame skipping & throttling to avoid GPU overload.
-   Use async pipelines instead of synchronous blocking.
-   Cache OCR results when UI stays static.
-   Multi-stage processing:
    -   First detect UI elements.
    -   Then run OCR on cropped regions only.
-   Lightweight YOLO model for real-time speed.

------------------------------------------------------------------------

## **11. Security & Safety**

-   No game memory reading.
-   No code injection.
-   No network manipulation.
-   No packet editing.
-   Only screen reading → safe, external assistant-style.

------------------------------------------------------------------------

# **End of Technical Specification**
