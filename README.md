# ToppsTracker

Personal collection tracker for **2026 Topps Series 1 Baseball** — retro DOS terminal style.

Built with .NET 10. Runs on Windows, Linux, and macOS.

---

## Quick Start

```bash
dotnet run
```

The prompt is `DUGOUT> `. Type `help` to see all commands.

---

## Commands

### Base Cards

| Command | Alias | Description |
|---|---|---|
| `have <ids>` | `h` | Mark base cards owned. Accepts single IDs, comma lists, and ranges: `have 1, 5, 10-15` |
| `missing` | `m` | List all missing base cards |
| `missing <start-end>` | | List missing cards within a range: `missing 25-80` |
| `check <id>` | `c` | Show owned/missing status for one card |
| `list` / `roster` | | Full roster with ownership and parallel counts |

### Parallels (Hits)

| Command | Alias | Description |
|---|---|---|
| `hit <id> <name>` | | Log a parallel: `hit 24 Gold Foil` |
| `unhit <id> <name>` | `uh` | Remove a logged parallel: `unhit 24 Gold Foil` |
| `parallels [range] [rarity]` | `p` | Show owned parallels. Filter by range and/or rarity: `parallels 1-100 rare` |
| `odds` | | Display parallel odds board |

### Insert / Subset Sets

| Command | Description |
|---|---|
| `inserts` | List all insert sets with ownership progress |
| `inserts <name\|code>` | Show card details for one set: `inserts TOG` |
| `inserts missing` | Filter to unfinished sets |
| `inserts owned` | Filter to completed sets |
| `inserts cat:<text>` | Filter by category: `inserts cat:Retail` |
| `inserts find:<text>` | Filter by set name: `inserts find:Titans` |
| `inserts have <name\|code>` | Mark full insert set as owned |
| `inserts have <code#>[, ...]` | Mark specific cards: `inserts have TOG1, TOG5-7` |
| `inserts unhave <name\|code\|code#>` | Remove full set or specific cards |
| `inserts cards <name\|code>` | Show all owned card numbers for a set |
| `inserts add <name> \| <category>` | Add a custom insert set |
| `inserts help` | Show full insert command reference |

### Display

| Command | Description |
|---|---|
| `stats` | Redraw the stats panel |
| `teams` | Show card count breakdown by team |
| `panel on\|off` | Toggle auto-refresh of stats panel after each change |

### Session

| Command | Alias | Description |
|---|---|---|
| `help` | | Show command reference |
| `exit` / `quit` | `q` | Save and exit |

---

## Data Files

| File | Description |
|---|---|
| `collection.json` | Base card collection (350 cards, owned state, parallels) |
| `insert_sets.json` | Insert/subset set definitions and owned card tracking |

Both files use atomic writes (`.tmp` → `.bak` → final) — a backup is created on every save.

Schema version is stored as `_schemaVersion: 1`. Old bare-array format is auto-migrated on first run.

---

## PDF Checklists

On first run (or when card names are placeholder), the app reads player names and insert card lists from Topps PDF checklists in `checklistInputs/`.

Primary parser: [PdfPig](https://github.com/UglyToad/PdfPig)  
Fallback: `pdftotext` (Poppler) if present on PATH

### Override paths via environment variables

```bash
TOPPS_CHECKLIST_DIR=/path/to/dir          # override checklist directory (default: checklistInputs)
TOPPS_CHECKLIST_PDF=/path/to/checklist.pdf  # override full checklist PDF path
TOPPS_ODDS_PDF=/path/to/odds.pdf            # override full odds PDF path
```

---

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Optional: `pdftotext` (Poppler) for fallback PDF parsing

---

## Project Status

Active personal tracker — **2026 Topps Series 1 Baseball**.

Phase 0 hardening (atomic writes, schema versioning, model extraction, PDF path overrides) complete.
