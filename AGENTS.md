# AGENTS.md — Dugout '26 Project Context & Architectural Handoff

## 1. Project Overview & Background

**Dugout '26** (`Dugout26.csproj`) is a .NET 10 terminal-based collection tracking application for **2026 Baseball Series 1 & Series 2** (700 base cards + insert sets + parallel hits + pull odds catalog).

### Project Rename Context
- **Original Name**: `ToppsTracker`
- **Current Name**: `Dugout '26` / `Dugout26` (renamed to eliminate trademarked brand references).
- **Environment Variables**: Primary environment variables use `DUGOUT_` prefix (`DUGOUT_CHECKLIST_DIR`, `DUGOUT_CHECKLIST_PDF`, `DUGOUT_ODDS_PDF`, `DUGOUT_S2_CHECKLIST_PDF`, `DUGOUT_S2_ODDS_PDF`), with backward-compatible fallback to `TOPPS_` prefix.

---

## 2. Directory & Module Architecture

The codebase has been refactored into clean, single-responsibility static modules:

```
Dugout26/
├── Program.cs                # App entrypoint & PDF checklist bootstrapper (53 lines)
├── StorageService.cs         # JSON load/save + atomic file backups (.tmp -> .bak -> target)
├── ChecklistPdfParser.cs     # PDF text extraction & roster/insert hydration (PdfPig / pdftotext)
├── OddsPdfParser.cs          # PDF odds parsing & 1-in-X pull probability matching
├── ConsoleUi.cs              # Spectre.Console tables, 2-frame layout, interactive prompts, series reports
├── StatsRenderer.cs          # Arcade Stats Dashboard panel, visual progress bars, BreakdownChart
├── CommandHandler.cs         # Insert set logic, query filters, custom inserts, code generators
├── CliRouter.cs              # Main REPL command prompt loop (DUGOUT>) and command router
├── AnsiAnimation.cs          # DOS typewriter header, blinking cursor, slow-reveal engine, batter animation
├── VariantParser.cs          # Regex metadata extractor for typed parallel variants
├── Data Models:
│   ├── Card.cs               # Base card model (Id, PlayerName, IsOwned, Variants)
│   ├── CardVariant.cs        # Parallel variant model (Name, Rarity, SerialNum, PrintRun, IsAuto, IsRelic, Is1Of1)
│   ├── InsertSet.cs          # Insert set model (Code, Name, Category, IsOwned, OwnedCards)
│   ├── InsertCardInfo.cs     # Insert card number & player info
│   ├── OddsEntry.cs          # Pull odds model (Name, OddsText, OneIn, Rarity)
│   ├── CollectionFile.cs     # Schema container (_schemaVersion, Cards)
│   └── InsertSetFile.cs      # Schema container (_schemaVersion, Sets)
├── Dugout26.csproj           # Main application project file
└── Dugout26.Tests/           # xUnit test suite (42 unit tests)
    └── Dugout26.Tests.csproj
```

---

## 3. Key Design Decisions & Behavioral Directives

### 2-Frame Split Terminal Layout
1. **Top Frame (Fixed Dashboard & Menu)**:
   - Locked at the top of the terminal screen (`0,0`).
   - Renders live scoreboard stats: `BASE: 423/700 (60.5%) | S1: 280/350 | S2: 143/350 | HITS: 12`.
   - Displays Quick Command Reference.
2. **Bottom Frame (Command Terminal)**:
   - Separated by a cyan rule (`─── ⚾ DUGOUT COMMAND TERMINAL ⚾ ────────────────────────`).
   - The `DUGOUT> ` prompt and command outputs execute cleanly in this frame without erasing top stats.

### Retro DOS / NES Animations
- **Startup Typewriter Banner**: `PlayTypewriterHeader(colDelayMs: 25)` sweeps the 8-bit ASCII `DUGOUT '26` logo column-by-column, followed by 3 green DOS cursor blinks (`█`).
- **Slow Line-by-Line Menu Reveal**: `SlowRevealRenderable` captures Spectre renderables (`Table`, `Panel`, `BreakdownChart`) into ANSI strings and rolls them line-by-line from top to bottom (55ms-65ms per line).
- **Batter Swinging Animation**: `anim` / `swing` plays an 8-frame ASCII animation of pitcher windup, fastball, swing (`💥 CRACK! 💥`), ball soaring over the fence, and home run celebration.

### Series 1 & Series 2 Breakdown
- **Series 1**: Cards `#1` to `#350`.
- **Series 2**: Cards `#351` to `#700`.
- **Commands**:
  - `missing s1` / `missing series1`: Filter missing base cards to Series 1.
  - `missing s2` / `missing series2`: Filter missing base cards to Series 2.
  - `missing 25-80`: Range filter.
  - `series` / `s`: Displays the Series 1 & Series 2 Completeness Report table.

### Data Storage & Schema Integrity
- **Atomic File Writes**: `StorageService.cs` writes to `.tmp` first, copies existing file to `.bak`, then replaces target file.
- **Schema Versioning**: `_schemaVersion: 1`. Old bare JSON arrays are auto-migrated on load.
- **Typed Parallel Variants**: `CardVariant` fields: `Name`, `Rarity`, `SerialNum` (`string?`), `PrintRun` (`int?`), `IsAuto` (`bool`), `IsRelic` (`bool`), `Is1Of1` (`bool`).

---

## 4. Environment Variables

| Variable | Default Value | Purpose |
|---|---|---|
| `DUGOUT_CHECKLIST_DIR` | `checklistInputs` | Folder containing PDF checklists |
| `DUGOUT_CHECKLIST_PDF` | `checklistInputs/2026_Topps_Series_1_Baseball_Checklist.pdf` | Series 1 base/insert checklist |
| `DUGOUT_ODDS_PDF` | `checklistInputs/2026_Topps_Baseball_Series_1_Odds.pdf` | Series 1 odds PDF |
| `DUGOUT_S2_CHECKLIST_PDF` | `checklistInputs/2026_Topps_Series_2_Baseball_Checklist_5-11.pdf` | Series 2 base/insert checklist |
| `DUGOUT_S2_ODDS_PDF` | `checklistInputs/2026_Topps_Baseball_Series_2_Odds.pdf` | Series 2 odds PDF |

*(Note: Legacy `TOPPS_` variable names are supported as fallback).*

---

## 5. Building & Running

### Build Main Project
```bash
dotnet build Dugout26.csproj
```

### Run Application
```bash
dotnet run --project Dugout26.csproj
```

### Run Test Suite
```bash
dotnet test Dugout26.Tests/Dugout26.Tests.csproj
```

---

## 6. Build Constraints for AI Agents

1. **Test Compilation Exclusion**: In `Dugout26.csproj`, `<Compile Remove="Dugout26.Tests\**" />` MUST remain present so the main build does not attempt to compile xUnit test files.
2. **Spectre Markup Escaping**:
   - In Spectre Markup strings, brackets `[` and `]` MUST be escaped as `[[` and `]]` (e.g. `[[AUTO]]`, `[[RELIC]]`, `<range>`).
   - Use Spectre `Color` enums supported by Spectre.Console 0.49.1 (e.g. `Color.Cyan1`, `Color.Magenta1`, `Color.Gold1`).

---

## 7. Ponytail Ruleset (Decision Ladder)

All AI agents working in this repository MUST follow the **Ponytail Decision Ladder** before writing or modifying code to prevent over-engineering and code bloat:

1. **Does this need to exist at all? (YAGNI)**: Skip unnecessary or speculative abstractions.
2. **Is it already in this codebase?**: Reuse existing helpers, static modules, and models.
3. **Does the standard library handle it?**: Use .NET standard library & LINQ primitives.
4. **Is there a native platform/OS feature for it?**: Use environment/OS features when available.
5. **Does an installed dependency solve it?**: Leverage installed libraries (`Spectre.Console`, `PdfPig`).
6. **Can it be one line?**: Prefer concise, clean expressions.
7. **Write minimum code**: Only write minimal necessary code as a last resort.

*Note: Ponytail prioritizes simplicity but never sacrifices security, data validation, test coverage, or runtime correctness.*

