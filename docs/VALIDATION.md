# Ověření Olomouckého chaosu 1.0

Projekt jsem otevřel, otestoval a sestavil v Unity 6000.6.8f1 přes Unity CLI / MCP Pipeline (`~/.unity/bin/unity cmd …`).

## Automatické testy
- **EditMode, 55/55 zelených**: 54 statických kontrol dat všech 10 tratí a jeden integrační test, který vstoupí do Play Mode a spustí `RailSmokeTests.Run()`.
- **Integrační kontroly 28/28**. Skriptovaný běžec projede všech 10 tratí s vypnutým zraněním a ověří všechny plošiny, oba checkpointy, cíl a uložení rekordu. Zranění, pára, injekce, projektily a prohra se ověřují zvlášť.

## Windows build
- `České dráhy → Build Windows`: `StandaloneWindows64`, Mono, Direct3D 11/12. Výsledek **Succeeded, 0 varování**.
- `Builds/Windows/CeskeDrahy/CeskeDrahy.exe` je podle `file` „PE32+ executable (GUI) x86-64, for MS Windows“. Vedle něj leží `UnityPlayer.dll`, `CeskeDrahy_Data` (včetně `Managed/CeskeDrahy.dll` a glTFast) a `MonoBleedingEdge`.
- `Builds/CeskeDrahy-Windows.zip` (68 MB, 172 souborů, kontrola CRC v pořádku). Složka `…_BackUpThisFolder_ButDontShipItWithYourGame` se do balíku nedostane.
- Na vývojovém Macu nejde exe spustit (není Wine ani Windows). Proto jsem stejný obsah sestavil pro macOS a spustil v samostatném přehrávači (viz níže). Spuštění na Windows je potřeba ověřit podle [TESTOVANI.md](TESTOVANI.md).

## Samostatný přehrávač (macOS, Apple M4, 1600 × 900, vysoká grafika, VSync)
Samostatný build vykreslil vlastní shadery, mlhu, bloom, fonty s diakritikou, skinned model CJ i modely glTF.

| Trať | Průměr | Medián | 95. percentil |
|---|---:|---:|---:|
| 1 Olomouc hl. n. | 16,71 ms (59,8 FPS) | 16,66 ms | 17,50 ms |
| 6 Olomouc-Hejčín | 16,71 ms (59,8 FPS) | 16,67 ms | 17,55 ms |
| 10 Olomouc přednádraží | 16,71 ms (59,8 FPS) | 16,66 ms | 17,67 ms |

Každé měření má 360 snímků po 120 zahřívacích. Data jsou v `docs/benchmark-macos-m4-*.json`. Slabší GPU ani Windows nebyly měřené. Volba *Grafika · Úsporná* vypíná stíny, MSAA a post-processing.

```sh
open -W -n Builds/CeskeDrahy.app --args -rail-benchmark /absolutni/cesta/vysledky -rail-level 10
```

Příkaz změří zvolenou trať (1–10), uloží `benchmark.json` a `standalone.png` a aplikaci ukončí.
