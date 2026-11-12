# Ověření Olomouckého chaosu 0.3.0

Projekt byl spuštěn a sestaven v Unity 6000.6.8f1 přes Unity MCP. Samostatný build vykreslil hlavní nádraží i noční seřadiště na Apple M2 v rozlišení 1600 × 900. Snímky `gameplay.png` a `night.png` pocházejí z této aplikace.

Integrační sada v `Assets/Editor/RailSmokeTests.cs` prošla v poměru 20/20. Automatické průchody ověřily všech 39 plošin a oba checkpointy na každé trati. Průchody ověřují geometrii s vypnutým zraněním. Poškození od páry, holubů, revizorů a injekcí se ověřovalo zvlášť. Sada ověřila i přechod z cíle Olomouce do depa.

| Měření samostatné aplikace | Olomouc hl. n. | Noční seřadiště |
|---|---:|---:|
| Průměrná frekvence | 59,7 FPS | 59,8 FPS |
| Medián | 16,67 ms | 16,67 ms |
| 95. percentil | 17,60 ms | 17,63 ms |

Obě měření mají 360 snímků po 120 zahřívacích snímcích. V obou scénách běžela vysoká grafika, zvuk a VSync. Hráč stál na prvním motoráku. ZIP je sestavený pro macOS na Apple Silicon. Ostatní platformy ani slabší GPU nebyly měřené.

```sh
open -W -n Builds/CeskeDrahy.app --args -rail-benchmark /absolute/path/to/results -rail-level 3
```

Příkaz změří noční seřadiště, vytvoří `benchmark.json` a `standalone.png` a po měření aplikaci ukončí. Bez argumentu `-rail-level 3` měří hlavní nádraží.
