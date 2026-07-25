<div align="center">

```
  ██████╗  ██╗  ██╗  ██████╗  ██████╗  ██╗  ██╗ ████████╗   ██╗    ██████╗   ██████╗ 
  ██╔══██╗ ██║  ██║ ██╔════╝ ██╔═══██╗ ██║  ██║ ╚══██╔══╝   ██║   ╚════██╗ ██╔════╝ 
  ██║  ██║ ██║  ██║ ██║  ███╗██║   ██║ ██║  ██║    ██║      ╚═╝    █████╔╝ ███████╗ 
  ██║  ██║ ██║  ██║ ██║   ██║██║   ██║ ██║  ██║    ██║            ██╔═══╝  ██╔═══██╗
  ██████╔╝ ╚█████╔╝ ╚██████╔╝╚██████╔╝ ╚█████╔╝    ██║            ███████╗ ╚██████╔╝
  ╚═════╝   ╚════╝   ╚═════╝  ╚═════╝   ╚════╝     ╚═╝            ╚══════╝  ╚═════╝ 
```

# ⚾ DUGOUT '26 ⚾
### *Retro 8-Bit Arcade Terminal Roster & Hit Tracker for 2026 Baseball Series 1 & Series 2*

[![Build & Test](https://img.shields.io/badge/tests-42%20passed-brightgreen.svg?style=for-the-badge&logo=dotnet)](file:///home/earle/Dugout26/Dugout26.Tests)
[![Framework](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg?style=for-the-badge)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Linux%20%7C%20macOS%20%7C%20Windows-lightgrey.svg?style=for-the-badge)](https://dotnet.microsoft.com/)
[![Architecture](https://img.shields.io/badge/code%20style-Ponytail%20Ladder-orange.svg?style=for-the-badge)](AGENTS.md)

</div>

---

## 💡 About Dugout '26

**Dugout '26** is a high-energy, terminal-based collection management system built for baseball card collectors. Designed with a retro 8-bit DOS/NES arcade aesthetic, it brings a fresh, dynamic, and interactive experience to tracking base cards, rare parallels, insert subsets, and pull probabilities for 2026 Series 1 & Series 2.

---

## 🤖 Legendary Human Vision & AI Pair-Programming

> [!IMPORTANT]
> **Human Vision Meets AI Craftsmanship**  
> The vision, architecture, and feature requirements for **Dugout '26** originated entirely from a **human collector**. Using state-of-the-art AI pair-programming tools — **Google Antigravity** and **GitHub Copilot** — the application was authored to the human's exact directions and expectations.
>
> By enforcing strict architectural principles (such as the **Ponytail Decision Ladder**), atomic data safety, zero-dependency reliability, and modular code isolation, the AI executed what is **nothing short of legendary programming**:
> - **Maximum Security & Integrity**: Atomic JSON state writes (`.tmp` $\rightarrow$ `.bak` $\rightarrow$ target), schema versioning (`_schemaVersion: 1`), and non-destructive fallback parsing.
> - **Clean Architecture**: 14 single-responsibility modules keeping execution clean, lightning fast, and bulletproof.
> - **Extensive Test Coverage**: Built-in 42-unit test suite executing in under 100 milliseconds.

---

## 🌟 Features at a Glance

- 🏟️ **700 Base Card Roster**: Complete coverage for Series 1 (`#1`–`#350`) and Series 2 (`#351`–`#700`).
- 📊 **2-Frame Split Arcade Dashboard**: Fixed live scoreboard status panel pinned to top frame, REPL command prompt in bottom frame.
- ⚾ **8-Bit ASCII Batter Swinging Animation**: Play interactive pitch windup, fastball, swing (`💥 CRACK! 💥`), and home run celebration sequence.
- 📄 **PDF Checklist & Odds Hydration**: Auto-populates 700 player names and 1-in-X pull probabilities using `PdfPig` and `pdftotext`.
- 💎 **Parallel & Hit Tracking**: Log typed parallel variants (`Gold #45/2026`, `Red Auto /10`, `Superfractor 1/1`) with auto rarity classification.
- 🛡️ **Atomic Save Protection**: Safe `.tmp` $\rightarrow$ `.bak` $\rightarrow$ target file writes with schema versioning (`_schemaVersion: 1`).

---

## 📺 Live Arcade Scoreboard & Terminal Layout

```text
╭────────────────── ⚾ DUGOUT MANAGER '26 ARCADE DASHBOARD ⚾ ──────────────────╮
│ Overall Base Progress:  ██████████████░░░░░░░░░░ 60.5% (423/700)              │
│ Series 1 (#1-350):      ████████████████████░░░░ 80.0% (280/350, missing 70)    │
│ Series 2 (#351-700):    ██████████░░░░░░░░░░░░░░ 40.9% (143/350, missing 207)   │
│ Total Missing Base:     277                                                   │
│ Parallel Hits Logged:   12 (Autos: 3, Relics: 2, 1/1: 1)                        │
│ Rare + Ultra Hits:      5                                                     │
│ MLB Team Coverage:      30/30 Teams                                           │
│ Insert Set Progress:    4/37 Sets Complete                                    │
╰───────────────────────────────────────────────────────────────────────────────╯
─────────────────────────── ⚾ DUGOUT COMMAND TERMINAL ⚾ ───────────────────────────
DUGOUT> 
```

---

## 📖 How to Use Dugout '26

### 1. Installation & Running

```bash
# Clone the repository
git clone https://github.com/esimers/Dugout26.git
cd Dugout26

# Build and run
dotnet run --project Dugout26.csproj
```

At the `DUGOUT> ` prompt, type `menu` or `m` to launch the interactive arrow-key selection prompt, or type `help` for command reference.

### 2. Everyday Collecting Workflow
1. **Ripping Packs**: After opening packs, log your new base cards:
   ```text
   DUGOUT> have 1, 5, 12-18, 45
   ```
2. **Checking Progress**: View missing cards in Series 1 or Series 2 to see what you need to trade or acquire:
   ```text
   DUGOUT> missing s1
   DUGOUT> series
   ```
3. **Logging Parallel Hits**: Ripped an autograph, serial-numbered parallel, or 1-of-1 hit? Log it in seconds:
   ```text
   DUGOUT> hit 24 Gold #45/2026
   DUGOUT> hit 24 Red Auto /10
   DUGOUT> hit 24 Superfractor 1/1
   ```
4. **Tracking Inserts**: Keep tabs on sub-sets and insert checklists:
   ```text
   DUGOUT> inserts have TOG1, TOG3-5
   ```
5. **Interactive Fun**: Play the retro 8-bit batter animation:
   ```text
   DUGOUT> anim
   ```

---

## 🧪 Run Unit Test Suite

```bash
dotnet test Dugout26.Tests/Dugout26.Tests.csproj
```

---

## 🎮 Full Command Reference

### ⚾ Base Cards & Roster Scouting

| Command | Alias | Usage & Example |
| :--- | :---: | :--- |
| `have` | `h` | Mark base cards owned: `have 1, 5, 10-15` |
| `missing` | `m` | List missing cards: `missing`, `missing s1`, `missing s2`, `missing 25-80` |
| `series` | `s` | Display Series 1 (#1–350) & Series 2 (#351–700) Completeness Report |
| `check` | `c` | Check status of single card: `check 24` |
| `roster` | `list` | Full roster ownership listing with logged hit counters |
| `teams` | | Show 30-team MLB card coverage checklist |

### 🔥 Parallel Hits & Pull Odds

| Command | Alias | Usage & Example |
| :--- | :---: | :--- |
| `hit` | | Log parallel hit: `hit 24 Gold #45/2026`, `hit 24 Red Auto /10`, `hit 24 Platinum 1/1` |
| `unhit` | `uh` | Remove logged hit: `unhit 24 Gold` |
| `parallels` | `p` | Filter owned hits: `parallels rare`, `parallels 1-100`, `parallels auto`, `parallels /75` |
| `odds` | | Display top pull probability catalog extracted from PDF |

### ✨ Insert Sets & Subsets

| Command | Usage & Example |
| :--- | :--- |
| `inserts` | View full insert sets checklist with progress bars |
| `inserts <name\|code>` | View owned card details: `inserts TOG` or `inserts Titans of the Game` |
| `inserts missing` / `owned` | Filter insert list by completion state |
| `inserts cat:<text>` | Filter by category: `inserts cat:Retail` |
| `inserts find:<text>` | Filter by set name: `inserts find:Titans` |
| `inserts have <target>` | Mark set or cards owned: `inserts have TOG1, TOG5-7` |
| `inserts unhave <target>` | Remove set or cards: `inserts unhave TOG2` |
| `inserts add <name> \| <cat>` | Add custom insert set: `inserts add First Pitch \| Hobby` |

### 🕹️ Interactive Menu & Animations

| Command | Alias | Description |
| :--- | :---: | :--- |
| `menu` | `m` | Open Spectre arrow-key interactive selection menu |
| `anim` | `swing` | Play retro 8-frame ASCII batter swinging animation |
| `stats` | | Redraw Arcade Stats Dashboard panel |
| `panel` | | Toggle auto-refresh of stats panel (`panel on` / `panel off`) |
| `quit` | `exit`, `q` | Save collection and exit |

---

## ⚙️ Environment Variables

Customize PDF input locations dynamically via environment variables:

| Variable | Default Value | Purpose |
| :--- | :--- | :--- |
| `DUGOUT_CHECKLIST_DIR` | `checklistInputs` | Base folder containing PDF files |
| `DUGOUT_CHECKLIST_PDF` | `checklistInputs/2026_Topps_Series_1_Baseball_Checklist.pdf` | Series 1 base/insert PDF |
| `DUGOUT_ODDS_PDF` | `checklistInputs/2026_Topps_Baseball_Series_1_Odds.pdf` | Series 1 odds catalog PDF |
| `DUGOUT_S2_CHECKLIST_PDF` | `checklistInputs/2026_Topps_Series_2_Baseball_Checklist_5-11.pdf` | Series 2 base/insert PDF |
| `DUGOUT_S2_ODDS_PDF` | `checklistInputs/2026_Topps_Baseball_Series_2_Odds.pdf` | Series 2 odds catalog PDF |

*(Note: Legacy `TOPPS_` prefix environment variables remain supported as fallback).*

---

## 🏆 Trademark Disclaimer & Hobby Dedication

> [!NOTE]
> **Independent Fan & Collector Project**  
> **Dugout '26** is an independent, non-commercial personal tracking utility. This application is **NOT affiliated with, endorsed by, sponsored by, or connected to** The Topps Company, Inc., Fanatics, Inc., Major League Baseball (MLB), or any of their parent, subsidiary, or affiliated brands.
>
> We hold deep respect for the incredible hobby of sports card collecting. We wish everyone in the collecting community the absolute best of luck in ripping packs, making trades, and completing all the card sets they desire. We hope this software enhances your collecting experience and brings a whole new level of arcade-style fun to the hobby! ⚾📦✨

---

## 📜 License

Distributed under the **MIT License**. See [LICENSE](LICENSE) for details.

Copyright (c) 2026 esimers.
