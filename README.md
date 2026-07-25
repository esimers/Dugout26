# Dugout '26 (Dugout26)

Personal collection tracker for **2026 Baseball Series 1 & Series 2** — retro DOS terminal style.

Built with .NET 10. Runs on Windows, Linux, and macOS.

---

## Quick Start

```bash
dotnet run --project Dugout26.csproj
```

The prompt is `DUGOUT> `. Type `help` or `menu` to see all commands and arrow-key selection prompts.

---

## Commands

### Base Cards & Series Progress

| Command | Alias | Description |
|---|---|---|
| `have <ids>` | `h` | Mark base cards owned. Accepts single IDs, comma lists, and ranges: `have 1, 5, 10-15` |
| `missing` | `m` | List all missing base cards |
| `missing s1` / `missing s2` | | List missing cards in Series 1 (#1-350) or Series 2 (#351-700) |
| `missing <start-end>` | | List missing cards within a range: `missing 25-80` |
| `series` | `s` | View Series 1 & Series 2 completeness report |
| `check <id>` | `c` | Show owned/missing status for one card |
| `list` / `roster` | | Full roster with ownership and parallel counts |

### Parallels (Hits)

| Command | Alias | Description |
|---|---|---|
| `hit <id> <name>` | | Log a parallel: `hit 24 Gold #45/2026` or `hit 24 Red Auto /10` |
| `unhit <id> <name>` | `uh` | Remove a logged parallel: `unhit 24 Gold` |
| `parallels [range] [rarity]` | `p` | Show owned parallels. Filter by range and/or rarity: `parallels 1-100 rare`, `parallels auto`, `parallels /75` |
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

### Display & Animation

| Command | Alias | Description |
|---|---|---|
| `menu` | `m` | Open interactive arrow-key selection prompt menu |
| `anim` | `swing` | Play 8-bit ASCII baseball batter swinging animation |
| `stats` | | Redraw the Arcade Stats Panel dashboard |
| `teams` | | Show 30-team card coverage report |
| `panel on\|off` | | Toggle auto-refresh of stats panel after each change |

### Session

| Command | Alias | Description |
|---|---|---|
| `help` | | Show command reference |
| `exit` / `quit` | `q` | Save and exit |

---

## Data Files

| File | Description |
|---|---|
| `collection.json` | Base card collection (700 cards, owned state, typed parallel variants) |
| `insert_sets.json` | Insert/subset set definitions and owned card tracking |

Both files use atomic writes (`.tmp` → `.bak` → final) — a backup is created on every save.

Schema version is stored as `_schemaVersion: 1`. Old bare-array format is auto-migrated on first run.

---

## PDF Checklists

On first run (or when card names are placeholder), the app reads player names and insert card lists from PDF checklists in `checklistInputs/`.

Primary parser: [PdfPig](https://github.com/UglyToad/PdfPig)  
Fallback: `pdftotext` (Poppler) if present on PATH

### Override paths via environment variables

```bash
DUGOUT_CHECKLIST_DIR=/path/to/dir          # override checklist directory (default: checklistInputs)
DUGOUT_CHECKLIST_PDF=/path/to/checklist.pdf  # override full Series 1 checklist PDF path
DUGOUT_ODDS_PDF=/path/to/odds.pdf            # override full Series 1 odds PDF path
DUGOUT_S2_CHECKLIST_PDF=/path/to/s2.pdf      # override Series 2 checklist PDF path
DUGOUT_S2_ODDS_PDF=/path/to/s2_odds.pdf      # override Series 2 odds PDF path
```

---

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Optional: `pdftotext` (Poppler) for fallback PDF parsing

---

## Project Status

Active personal tracker — **Dugout '26 (2026 Baseball Series 1 & Series 2)**.
