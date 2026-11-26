# Ověření hry SuperCigo 1.1

Projekt se otevřel, otestoval a sestavil v Unity 6000.6.8f1 přes Unity CLI / MCP Pipeline (`~/.unity/bin/unity cmd …`) na Macu s Apple M4 (16 GB, macOS 27). Výsledky platí pro verzi **1.1.1** (soubory `SuperCigo-macOS.zip` a `SuperCigo-Windows.zip` ve vydání v1.1.1). Verze 1.1.1 přináší nový název hry (dříve *SuperCigos*, vydání v1.1.0) a opravu vykreslování v Parallels; měření výkonu níže pochází z verze 1.1.0, herní kód pro macOS se od té doby nezměnil.

## Automatické testy
- **EditMode: 72 / 72 zelených.** 62 kontrol dat a fyziky tratí, 9 kontrol importovaných postav a animací a 1 integrační test, který vstoupí do Play Mode.
- **Integrační kontroly: 40 / 40** (`RailSmokeTests.Run()`). Skriptovaný běžec projede všech 10 tratí, ověří všechny plošiny, oba checkpointy, cíl, uložení rekordu a to, že vozy sedí na svých plošinách. Zranění, pára, injekce, projektily, prohra, rychlý pád, korekce hrany a další mechaniky se ověřují zvlášť.

## macOS build (`SuperCigo → Build release`)
- Universal aplikace: všech 5 spustitelných souborů a knihoven obsahuje `x86_64` i `arm64` (kontrola `lipo -archs` při buildu). Minimální systém macOS 12.0, verze 1.1.1, ikona SC, bez úvodní obrazovky Unity.
- Vydání 1.1.1 se po buildu spustilo z `Builds/macOS` s `-rail-benchmark … -rail-level 4`: trať Olomouc město se vykreslí celá a aplikace se sama ukončí s výsledky.
- Podpis ad hoc (`codesign --force --deep --sign -`). `codesign --verify --deep --strict` projde v buildu i po rozbalení zipu přes `ditto` a přes `unzip`. `spctl --assess` podle očekávání hlásí *rejected*, protože aplikace není notarizovaná (projekt nemá Apple Developer ID). Postup pro hráče je v README.
- Zip vytváří `ditto -c -k --norsrc --keepParent`: 99 MB, žádné položky `__MACOSX` ani `._`.
- Simulovaná karanténa ze Safari (`com.apple.quarantine`): `spctl` aplikaci odmítne, po `xattr -dr com.apple.quarantine` zůstane podpis platný a aplikace se spustí.
- Unity při buildu varuje, že podpora Intel Maců (x86_64) je zastaralá a v budoucí verzi Unity skončí. Ve verzi 6000.6 funguje.

## Windows build
- `StandaloneWindows64`, Mono, Direct3D 11/12, 0 varování. `SuperCigo.exe` je PE32+ x86-64 (kontrola hlavičky při buildu i nástrojem `file`). Vedle něj leží `UnityPlayer.dll`, `SuperCigo_Data`, `MonoBleedingEdge`, `D3D12` a knihovny DirectStorage.
- `SuperCigo-Windows.zip` (92 MB) obsahuje složku `SuperCigo`. Složka `…_BackUpThisFolder_ButDontShipItWithYourGame` se do balíku nedostane.
- Kontrolní součty obou zipů jsou v `SHA256SUMS.txt` u vydání.
- **Windows build se testoval jen ve virtuálu na Macu** (viz níže), ne na skutečném počítači s Windows.

## Windows ve virtuálu (Parallels Desktop na Macu)
Testy Windows buildu proběhly 26. 11. 2026 ve virtuálním stroji, protože tým nemá k dispozici počítač s Windows. Vyplněný protokol je v [TESTOVANI.md](TESTOVANI.md#protokol-windows-ve-virtuálu).

- **Prostředí:** Parallels Desktop 26.4 na Macu s Apple M4 (macOS 27), Windows 11 pro ARM. Hra je x64, takže běží v emulaci. Grafika *Parallels Display Adapter (WDDM)* přes Direct3D 11 (feature level 11.1); ovladač hlásí jen 128 MB grafické paměti.
- **Postup:** spuštění rozbaleného buildu, hlavní menu, start trati 1 a porovnání snímků obrazovky s macOS buildem na stejném místě trati. Hra se ovládala z Macu přes `prlctl exec` a snímky se pořizovaly uvnitř Windows.
- **Nalezená chyba:** ovladač Parallels nevykreslí draw cally s GPU instancingem. Chyběly výpravní budova s okny, vagony v pozadí, sloupy přístřešku, obrubník nástupiště a většina jízdenek; místo nich byla vidět obloha. **Oprava:** `RailWorld.GpuInstancing` vypíná instancing jen ve Windows buildu na grafice Parallels. Na macOS a na běžném Windows PC se nic nemění. Po opravě odpovídá obraz macOS buildu.
- **Vydání 1.1.1:** `SuperCigo-Windows.zip` se ve virtuálu rozbalil přes `Expand-Archive` a spustil. Okno nese název SuperCigo, trať 1 se vykreslí celá (budova s okny, vagony v pozadí, sloupy, všechny jízdenky).
- **Grafická API:** vynucené Direct3D 12 (`-force-d3d12`) má v Parallels rozmazané textury, proto hra zůstává na Direct3D 11 (D3D12 sama odmítne kvůli feature level 11.1). OpenGL (`-force-glcore`) v buildu není.
- **Výkon (trať 1, okno přes celou obrazovku virtuálu 3420 × 2070, údaj FPS z HUD):** 55–60 FPS s *Grafika · Úsporná*, 41–43 FPS s *Grafika · Vysoká*. Kvůli 128 MB paměti začíná hra ve virtuálu na úsporné grafice.
- **Neověřeno:** běh na skutečném hardwaru s Windows (Intel/AMD/NVIDIA), stažení přes prohlížeč a SmartScreen, celý ruční scénář. Na reálném Windows se hra může chovat jinak než ve virtuálu.

## Výkon (samostatný přehrávač, Apple M4, 1600 × 900, grafika Ultra, bez omezení FPS)
Prohlídka tratí (`-rail-tour -rail-uncapped`) na každé trati postaví CJ na start, doprostřed a do cíle a měří 49 snímků na místě. Sloupec x86_64 je stejný build spuštěný přes Rosettu 2 (`arch -x86_64`).

| Trať | Medián arm64 | 95. percentil arm64 | Medián x86_64 | Draw cally | Trojúhelníky |
|---|---:|---:|---:|---:|---:|
| 1 Olomouc hl. n. | 4,15 ms (241 FPS) | 5,68 ms | 4,06 ms | 428 | 230 tis. |
| 2 Olomouc-Smetanovy sady | 6,91 ms (145 FPS) | 8,14 ms | 6,93 ms | 334 | 399 tis. |
| 3 Olomouc-Nová Ulice | 4,59 ms (218 FPS) | 5,26 ms | 4,61 ms | 390 | 316 tis. |
| 4 Olomouc město | 4,02 ms (249 FPS) | 4,52 ms | 4,07 ms | 382 | 232 tis. |
| 5 Olomouc Hodolany | 4,39 ms (228 FPS) | 5,07 ms | 4,40 ms | 259 | 149 tis. |
| 6 Olomouc-Hejčín | 6,18 ms (162 FPS) | 6,88 ms | 6,17 ms | 318 | 375 tis. |
| 7 Olomouc-Řepčín | 4,87 ms (205 FPS) | 5,89 ms | 4,76 ms | 320 | 182 tis. |
| 8 Olomouc-Nové Sady | 4,96 ms (202 FPS) | 5,53 ms | 4,99 ms | 336 | 267 tis. |
| 9 Nemilany | 5,97 ms (168 FPS) | 6,96 ms | 6,02 ms | 404 | 718 tis. |
| 10 Olomouc přednádraží | 4,30 ms (233 FPS) | 5,04 ms | 4,31 ms | 249 | 149 tis. |

Data jsou v [benchmark-macos-m4-arm64.json](benchmark-macos-m4-arm64.json) a [benchmark-macos-m4-x64-rosetta.json](benchmark-macos-m4-x64-rosetta.json). Ve hře je zapnutá vertikální synchronizace, takže běží na obnovovací frekvenci monitoru. Slabší grafické karty a skutečný počítač s Windows nebyly měřené; orientační FPS z virtuálu jsou v oddílu *Windows ve virtuálu*. Volba *Grafika · Úsporná* vypíná stíny, MSAA a post-processing; karty s méně než 2 GB paměti začínají na ní automaticky.

```sh
open -W -n SuperCigo.app --args -rail-benchmark /absolutni/cesta/vysledky -rail-tour -rail-uncapped
open -W -n SuperCigo.app --args -rail-benchmark /absolutni/cesta/vysledky -rail-level 10
```

První příkaz projde všech 10 tratí a uloží `tour.json` a snímky, druhý změří jednu trať (1–10) a uloží `benchmark.json` a `standalone.png`. Aplikace se pak sama ukončí.
