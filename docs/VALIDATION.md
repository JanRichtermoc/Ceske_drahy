# Ověření hry SuperCigos 1.1

Projekt se otevřel, otestoval a sestavil v Unity 6000.6.8f1 přes Unity CLI / MCP Pipeline (`~/.unity/bin/unity cmd …`) na Macu s Apple M4 (16 GB, macOS 27). Výsledky platí pro commit, ze kterého vznikly soubory `SuperCigos-macOS.zip` a `SuperCigos-Windows.zip` verze 1.1.0.

## Automatické testy
- **EditMode: 72 / 72 zelených.** 62 kontrol dat a fyziky tratí, 9 kontrol importovaných postav a animací a 1 integrační test, který vstoupí do Play Mode.
- **Integrační kontroly: 40 / 40** (`RailSmokeTests.Run()`). Skriptovaný běžec projede všech 10 tratí, ověří všechny plošiny, oba checkpointy, cíl, uložení rekordu a to, že vozy sedí na svých plošinách. Zranění, pára, injekce, projektily, prohra, rychlý pád, korekce hrany a další mechaniky se ověřují zvlášť.

## macOS build (`SuperCigos → Build release`)
- Universal aplikace: všech 5 spustitelných souborů a knihoven obsahuje `x86_64` i `arm64` (kontrola `lipo -archs` při buildu). Minimální systém macOS 12.0, verze 1.1.0, ikona SC, bez úvodní obrazovky Unity.
- Podpis ad hoc (`codesign --force --deep --sign -`). `codesign --verify --deep --strict` projde v buildu i po rozbalení zipu přes `ditto` a přes `unzip`. `spctl --assess` podle očekávání hlásí *rejected*, protože aplikace není notarizovaná (projekt nemá Apple Developer ID). Postup pro hráče je v README.
- Zip vytváří `ditto -c -k --norsrc --keepParent`: 99 MB, žádné položky `__MACOSX` ani `._`.
- Simulovaná karanténa ze Safari (`com.apple.quarantine`): `spctl` aplikaci odmítne, po `xattr -dr com.apple.quarantine` zůstane podpis platný a aplikace se spustí.
- Unity při buildu varuje, že podpora Intel Maců (x86_64) je zastaralá a v budoucí verzi Unity skončí. Ve verzi 6000.6 funguje.

## Windows build
- `StandaloneWindows64`, Mono, Direct3D 11/12, 0 varování. `SuperCigos.exe` je PE32+ x86-64 (kontrola hlavičky při buildu i nástrojem `file`). Vedle něj leží `UnityPlayer.dll`, `SuperCigos_Data`, `MonoBleedingEdge`, `D3D12` a knihovny DirectStorage.
- `SuperCigos-Windows.zip` (92 MB) obsahuje složku `SuperCigos`. Složka `…_BackUpThisFolder_ButDontShipItWithYourGame` se do balíku nedostane.
- Na vývojovém Macu nejde exe spustit (není Wine ani Windows). Spuštění na Windows ověřují autoři podle protokolu v [TESTOVANI.md](TESTOVANI.md).
- Kontrolní součty obou zipů jsou v `SHA256SUMS.txt` u vydání.

## Výkon (samostatný přehrávač, Apple M4, 1600 × 900, grafika Ultra, bez omezení FPS)
Prohlídka tratí (`-rail-tour -rail-uncapped`) na každé trati postaví CJ na start, doprostřed a do cíle a měří 49 snímků na místě. Sloupec x86_64 je stejný build spuštěný přes Rosettu 2 (`arch -x86_64`).

| Trať | Medián arm64 | 95. percentil arm64 | Medián x86_64 | Draw cally | Trojúhelníky |
|---|---:|---:|---:|---:|---:|
| 1 Olomouc hl. n. | 3,99 ms (251 FPS) | 5,29 ms | 4,11 ms | 445 | 237 tis. |
| 2 Olomouc-Smetanovy sady | 6,99 ms (143 FPS) | 8,12 ms | 6,94 ms | 338 | 429 tis. |
| 3 Olomouc-Nová Ulice | 4,65 ms (215 FPS) | 5,40 ms | 4,67 ms | 387 | 317 tis. |
| 4 Olomouc město | 4,03 ms (248 FPS) | 4,65 ms | 4,06 ms | 383 | 231 tis. |
| 5 Olomouc Hodolany | 4,43 ms (226 FPS) | 4,94 ms | 4,37 ms | 264 | 149 tis. |
| 6 Olomouc-Hejčín | 6,15 ms (163 FPS) | 6,92 ms | 6,19 ms | 322 | 375 tis. |
| 7 Olomouc-Řepčín | 4,82 ms (208 FPS) | 5,95 ms | 4,77 ms | 320 | 182 tis. |
| 8 Olomouc-Nové Sady | 5,06 ms (197 FPS) | 5,60 ms | 4,91 ms | 332 | 267 tis. |
| 9 Nemilany | 5,90 ms (169 FPS) | 6,89 ms | 5,93 ms | 409 | 718 tis. |
| 10 Olomouc přednádraží | 4,32 ms (231 FPS) | 4,89 ms | 4,31 ms | 250 | 149 tis. |

Data jsou v [benchmark-macos-m4-arm64.json](benchmark-macos-m4-arm64.json) a [benchmark-macos-m4-x64-rosetta.json](benchmark-macos-m4-x64-rosetta.json). Ve hře je zapnutá vertikální synchronizace, takže běží na obnovovací frekvenci monitoru. Slabší grafické karty a Windows nebyly měřené. Volba *Grafika · Úsporná* vypíná stíny, MSAA a post-processing; karty s méně než 2 GB paměti začínají na ní automaticky.

```sh
open -W -n SuperCigos.app --args -rail-benchmark /absolutni/cesta/vysledky -rail-tour -rail-uncapped
open -W -n SuperCigos.app --args -rail-benchmark /absolutni/cesta/vysledky -rail-level 10
```

První příkaz projde všech 10 tratí a uloží `tour.json` a snímky, druhý změří jednu trať (1–10) a uloží `benchmark.json` a `standalone.png`. Aplikace se pak sama ukončí.
