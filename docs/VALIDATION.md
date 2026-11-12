# Ověření Olomouckého chaosu 0.2.0

Projekt byl spuštěn a sestaven v Unity 6000.6.8f1 přes Unity MCP. Samostatný build běžel na Apple M2 a vykreslil scénu v rozlišení 1600 × 900. Snímek `gameplay.png` pochází z této aplikace.

Integrační sada v `Assets/Editor/RailSmokeTests.cs` prošla v poměru 16/16. Pokrývá skok, dvojskok, buffer, krátkou toleranci po hraně, pohyblivý vlak, kufr, úhyb, holuba, brašnu, injekci, lékárničku, výdrž, prohru, pád a celý průchod. Kontrola geometrie trati používá nezranitelnost. Samostatný skriptovaný průchod s aktivními nepřáteli a běžným poškozením dorazil do cíle za 24,02 s s jedním životem a 28 jízdenkami.

Zvuková kontrola v Play Mode načetla všech 22 klipů a ověřila, že varování před útokem spustí hlas i titulek. Zásah spustil řev a náraz. Při hlasitosti řevu 0 se přehrál samotný náraz.

| Měření samostatné aplikace | Výsledek |
|---|---:|
| Rozlišení a zařízení | 1600 × 900, Apple M2 |
| Nastavení | Vysoká grafika, zapnutý zvuk, VSync |
| Měřených snímků | 360 po 120 zahřívacích |
| Průměrný čas snímku | 16,71 ms |
| Medián | 16,66 ms |
| 95. percentil | 17,61 ms |
| Průměrná frekvence | 59,8 FPS |

Krátké měření proběhlo u prvního motoráku se stojícím hráčem. ZIP je sestavený pro macOS na Apple Silicon. Ostatní platformy ani slabší GPU nebyly měřené.

```sh
open -W -n Builds/CeskeDrahy.app --args -rail-benchmark /absolute/path/to/results
```

Příkaz vytvoří `benchmark.json` a `standalone.png` a po měření aplikaci ukončí.
