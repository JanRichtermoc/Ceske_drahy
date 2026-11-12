# Ověření — 12. listopadu 2026

## Unity a samostatný build

- Unity 6000.6.8f1, Built-in Render Pipeline, macOS / Apple M2.
- Projekt byl vytvořen a ovládán přes stdio Unity MCP server poskytovaný Unity CLI / Pipeline 1.0.0.
- Skripty se zkompilovaly; samostatná macOS aplikace se úspěšně sestavila a spustila.
- Opraveno odstranění Standard shaderu z buildu: materiály Surface a SurfaceNormal mají serializované reference v Resources.
- Samostatné spuštění po opravě neobsahovalo runtime výjimku ani chybu shaderu. Snímek `gameplay.png` pochází přímo ze samostatné aplikace.

## Integrační kontroly: 11 / 11 PASS

1. Dopad na výchozí nástupiště.
2. Poškození a ochrana před opakovaným zásahem během nezranitelnosti.
3. Prohra při nulovém počtu životů.
4. Pád odečte život a obnoví poslední checkpoint.
5. Krátký a dlouhý stisk vedou k různé výšce skoku.
6. Skok krátce po opuštění hrany (coyote time).
7. Skok stisknutý krátce před dopadem (input buffer).
8. Skutečný kontakt s holubem odečte život.
9. Revizor hodí projektil a zasáhne stojícího hráče.
10. Pohyblivý vagon přenáší stojícího hráče.
11. Průchod přes všech 13 plošin, oba checkpointy, sběr jízdenek a dosažení cíle.

Poslední test záměrně vypíná zranění a ověřuje geometrii skoků. Není důkazem vyvážené obtížnosti proti všem nepřátelům. Testy jsou v `Assets/Editor/RailSmokeTests.cs` a spouštějí se přes `RailSmokeTests.Run()` v Play Mode.

## Měření samostatné aplikace

Vysoká grafika, stíny a 4× MSAA, 1600 × 900, Apple M2, VSync. Po 120 zahřívacích snímcích bylo změřeno 360 snímků během přibližně šesti sekund na prvním motoráku. Scéna i nepřátelé při měření běží, hráč stojí.

| Metrika | Výsledek |
|---|---:|
| Průměrný čas snímku | 16.72 ms |
| Medián | 16.67 ms |
| 95. percentil | 17.67 ms |
| Nejpomalejší snímek | 33.14 ms |
| Průměrné FPS | 59.8 |

Jde o krátký lokální vzorek jednoho úseku, nikoli certifikaci minimálních hardwarových požadavků. Celou trať na slabších GPU a integrovaných grafikách je ještě třeba profilovat. V editoru byla v obdobném pohledu zaznamenána přibližně 150 tisíc trojúhelníků a 123 draw calls; čísla zahrnují aktuální záběr, ne celý obsah projektu.

Opakování měření na macOS:

```sh
open -W -n Builds/CeskeDrahy.app --args -rail-benchmark /absolute/path/to/results
```

Výstupy: `benchmark.json` a `standalone.png`. Volba je určena pro diagnostiku a po měření aplikaci ukončí.

## Rozsah a další omezení

Jedna trať se třemi úseky, stylizované 3D, klávesnice a myš. Mobilní ovládání, gamepad a Windows build nebyly dodány ani testovány. Projekt lze pro Windows sestavit po instalaci příslušného Unity modulu. Plné uživatelské testování obtížnosti a fotorealistické přepracování grafiky nejsou součástí této první verze.
