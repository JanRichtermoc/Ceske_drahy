# České dráhy — Olomoucký chaos

**[⬇ Stáhnout hru pro Windows (ZIP)](https://github.com/JanRichtermoc/Ceske_drahy/releases/latest/download/CeskeDrahy-Windows.zip)** · [macOS (Apple Silicon)](https://github.com/JanRichtermoc/Ceske_drahy/releases/latest/download/CeskeDrahy-macOS.zip) · [všechna vydání](https://github.com/JanRichtermoc/Ceske_drahy/releases)

![Hlavní menu – odjezdová tabule](docs/menu.png)

## Popis hry

*České dráhy — Olomoucký chaos* je 2,5D arkádová plošinovka pro jednoho hráče. Carl „CJ“ Johnson se snaží stihnout poslední spoj. Cestou proskáče **deset skutečných železničních stanic Olomouce** po střechách vagonů, cisteren a kontejnerů. Pod nohama mu přitom překážejí revizoři, holubi, pára a podchodoví alchymisté s obřími injekcemi.

**Cíl:** projet každou trať od startovního nástupiště do cíle co nejrychleji. Po dojetí se ukáže **jízdenka s časem** a osobním rekordem trati (razítko „NOVÝ REKORD“), dále body, sebrané jízdenky, odrazy a nejvyšší kombo. Rekordy se ukládají lokálně a zobrazují se v menu na odjezdové tabuli.

**Pravidla:**
- Každá trať má dva checkpointy na nástupištích. Při příjezdu se návěstidlo přepne na zelenou a po pádu se hráč vrací sem.
- Na začátku má hráč 3 životy, na poslední trati 2. Pád mezi vagony nebo zásah stojí jeden život. Po ztrátě všech životů „ujel vlak“ a trať se opakuje.
- Jízdenky a skok protivníkům na hlavu dávají body. Rychlý sled akcí zvyšuje násobič až na ×5.
- Varování před útoky: žlutá kontrolka znamená, že revizor hodí brašnu, zelená ohlašuje létající injekci. Oranžové blikání roštu varuje před párou a v Nemilanech výstraha ohlašuje poryv protivětru.
- Injekce způsobí nákazu, která zpomaluje. Lékárnička ji vyléčí a vrátí život. Tvarůžek dá krátké zrychlení a růžový kufr vystřelí vysoko.
- Ztráta zaostření okna hru pozastaví a čas se během pauzy nepočítá.

### Tratě

Názvy odpovídají stanicím a zastávkám v Olomouci (cs.wikipedia, kategorie *Železniční stanice v Olomouci*). Kulisy jsou stylizované, nejde o přesné modely. Všechny tratě jsou odemčené hned.

| # | Stanice | Kulisy a novinka | Obtížnost |
|---|---|---|---|
| 1 | Olomouc hl. n. | výpravní budova, podchod, výuka dvojskoku a kufrů | ● |
| 2 | Olomouc-Smetanovy sady | park, pavilon Výstaviště Flora, Palmový skleník, víc kufrových pružin | ● |
| 3 | Olomouc-Nová Ulice | Fakultní nemocnice, sanitky, tramvaj Tatra T3, víc injekcí i lékárniček | ●● |
| 4 | Olomouc město | měšťanské domy, věže sv. Mořice, radnice, katedrály sv. Václava a Sloup Nejsvětější Trojice, posouvající se vagony | ●●● |
| 5 | Olomouc Hodolany | lokomotivní depo, výtrysky páry | ●●● |
| 6 | Olomouc-Hejčín | most přes řeku Moravu, topoly, pád do vody | ●●● |
| 7 | Olomouc-Řepčín | vlečky, sila, kontejnery, vysoké cisterny, soumrak | ●●●● |
| 8 | Olomouc-Nové Sady | sídliště za protihlukovou stěnou, večer, pára a revizoři | ●●●● |
| 9 | Nemilany | pole a větrolamy při západu slunce, nárazy protivětru | ●●●● |
| 10 | Olomouc přednádraží | noční seřadiště, finále se dvěma životy | ●●●●● |

![Olomouc město](docs/gameplay.png)

## Ovládání

Hra se ovládá klávesnicí; v menu jde použít i myš.

| Klávesa | Akce |
|---|---|
| A / D nebo ← / → | pohyb doleva / doprava |
| Mezerník, W nebo ↑ | skok; další stisk ve vzduchu = dvojskok; krátký stisk = nižší skok |
| Shift | rychlý úhyb (krátce nezranitelný, pak se nabíjí) |
| R | návrat na poslední checkpoint; na výsledkové obrazovce opakování trati |
| Esc | pauza / pokračování; na výsledkové obrazovce návrat do menu; zavření nastavení |
| Enter | start vybrané trati / další trať / nový pokus |
| ↑ / ↓ (W / S) v menu | výběr trati na odjezdové tabuli (také najetím myší) |
| M | zapnutí / vypnutí zvuku |
| Alt + Enter | celá obrazovka / okno (také v Nastavení) |

![Výsledková jízdenka](docs/results.png)

## Postup spuštění

### Windows (doporučeno)
1. Stáhni **[CeskeDrahy-Windows.zip](https://github.com/JanRichtermoc/Ceske_drahy/releases/latest/download/CeskeDrahy-Windows.zip)** ze stránky [Releases](https://github.com/JanRichtermoc/Ceske_drahy/releases).
2. Klikni na ZIP pravým tlačítkem a zvol **Extrahovat vše…**. Hra nejde spustit přímo z neotevřeného ZIPu.
3. Ve složce `CeskeDrahy` spusť **`CeskeDrahy.exe`**. Soubory `UnityPlayer.dll`, `CeskeDrahy_Data` a `MonoBleedingEdge` musí zůstat vedle exe.
4. Build není podepsaný, takže Windows SmartScreen může ukázat „Systém Windows ochránil váš počítač“. Klikni na **Další informace → Přesto spustit**.

Požadavky: Windows 10 nebo 11 (64 bit), grafická karta s DirectX 11, asi 200 MB volného místa. Instalace ani připojení k internetu nejsou potřeba.

### macOS (Apple Silicon)
1. Stáhni a rozbal [CeskeDrahy-macOS.zip](https://github.com/JanRichtermoc/Ceske_drahy/releases/latest/download/CeskeDrahy-macOS.zip).
2. Aplikace není notarizovaná. Při prvním spuštění na ni klikni pravým tlačítkem a zvol **Otevřít**, nebo v Terminálu spusť `xattr -dr com.apple.quarantine CeskeDrahy.app`.

### Ze zdrojového kódu v Unity
1. Naklonuj repozitář: `git clone https://github.com/JanRichtermoc/Ceske_drahy.git`.
2. V Unity Hub zvol **Add → Add project from disk**, vyber složku a otevři ji v Unity **6000.6.8f1**. Při prvním otevření se importují balíčky a assety, což trvá několik minut.
3. Otevři scénu `Assets/Scenes/OlomoucHlavniNadrazi.unity` a stiskni **Play**.
4. Vlastní build: menu **České dráhy → Build Windows** vytvoří `Builds/Windows/CeskeDrahy/CeskeDrahy.exe` a `Builds/CeskeDrahy-Windows.zip`, **České dráhy → Build macOS** vytvoří `Builds/CeskeDrahy.app`. Windows build potřebuje v Unity Hub modul *Windows Build Support (Mono)*.

## Použité technologie

| Oblast | Technologie |
|---|---|
| Herní engine | **Unity 6000.6.8f1**, Built-in Render Pipeline, lineární barevný prostor |
| Jazyk | **C#** (.NET Standard 2.1, Mono) |
| Herní logika | vlastní kinematický pohyb s pevným krokem 120 Hz a interpolací vykreslení; svět, tratě a kulisy generované z dat za běhu (`RailLevel`, `RailWorld`, `OlomoucBackdrops`) |
| Grafika | vlastní shadery v HLSL/Cg: `Railway/Surface` (PBR, rim světlo, procedurální patina, emisní mapy, triplanární mapování), `Railway/Foliage` (vítr), `Railway/Water`, obloha s mraky a hvězdami; post-processing `RailPost` (bloom, ACES, barevné ladění, vinětace) |
| Rozhraní | Unity IMGUI s vlastním vykreslováním (odjezdová tabule, HUD, jízdenka), fonty Barlow a Barlow Condensed |
| 3D modely | **glTFast** (import glTF/GLB), **Blender 5.2** (headless Python skripty v `Tools/blender`), **Sketchfab Download API** (`Tools/sketchfab_download.py`) |
| Testy | **Unity Test Framework** (NUnit), EditMode a Play Mode testy |
| Nástroje | Unity CLI / MCP Pipeline (`Tools/unity_mcp.py`) pro řízení editoru, testy a buildy; Git a GitHub |
| Assety | modely Sketchfab (CC BY), textury ambientCG (CC0), zvuky Freesound (CC0), syntetický hlas Piper |

## Testy

- **EditMode testy** (`Assets/Tests/Editor`, Unity Test Framework). Spusť je ve *Window → General → Test Runner → EditMode → Run All*. Zahrnují 54 statických kontrol všech tratí (unikátní klíče rekordů, shoda druhů plošin, zón a stanic, entity stojí na plošinách, dosažitelnost každé mezery podle fyziky skoku a checkpointy na nástupištích) a jeden integrační test, který sám vstoupí do Play Mode.
- **Integrační kontroly** (`RailSmokeTests.Run()`), celkem 28. Ověřují dopad, zranění a nezranitelnost, prohru i to, že se po ní už nesbírají předměty. Dál pád a checkpoint, variabilní skok, coyote time, buffer skoku, holuba, brašnu revizora, vezení na vagonu, kufr (puštění skoku ho nezkrátí), dvojskok, úhyb, nákazu, lékárničku, páru a uložení rekordu času. Skriptovaný běžec projede všech 10 tratí a nakonec se ověří úklid při návratu do menu. Testy po sobě obnoví hráčovy rekordy.
- **Ruční testovací scénář**: [docs/TESTOVANI.md](docs/TESTOVANI.md).

Z příkazové řádky přes Unity CLI (MCP Pipeline):

```sh
~/.unity/bin/unity cmd test run --mode EditMode --async-tests true
~/.unity/bin/unity cmd test status
```

## Struktura projektu

```
Assets/Scripts        herní kód (assembly CeskeDrahy)
  RailGame.cs         stavový automat, pohyb, kolize, checkpointy, rekordy
  RailLevel.cs        data 10 tratí
  RailWorld.cs        stavba trati, vagony, světlo, materiály
  OlomoucBackdrops.cs kulisy jednotlivých stanic
  RailChaos.cs        arkádová pravidla (kombo, nákaza, pružiny, protivníci)
  RailHUD.cs          menu, HUD, pauza, výsledková jízdenka, nastavení
  CjRig.cs            proxy kostra a retarget na skinned model CJ
  RailPost.cs         post-processing
Assets/Resources      shadery, materiály, modely, textury, fonty, zvuky
Assets/Tests/Editor   EditMode testy a integrační kontroly
Assets/Editor         příprava scény a buildy
Tools                 Sketchfab, Unity MCP, Blender skripty
docs                  design, testování, licence, podklady
```

## Předlohy a licence

3D modely ze Sketchfabu (CC BY 4.0, atribuce a licence v `docs/*-attribution.json` a `docs/*-license.txt`):

- [Free Rigged Male Basemesh](https://sketchfab.com/3d-models/free-rigged-male-basemesh-b8de3373addd4decac40400ee96c62e0) – **Filip**. Tělo CJ: v Blenderu oblečené (tílko, džíny, tenisky, vlasy), převedené do metrů a řízené herní kostrou.
- [Tatra t3 (reworked)](https://sketchfab.com/3d-models/tatra-t3-reworked-a932f7c3850643fd92dadad7f5771d6c) – **Randonavt**. Tramvaj, zjednodušená ze 141 tisíc na 26 tisíc trojúhelníků.
- [Wooden Park Bench](https://sketchfab.com/3d-models/wooden-park-bench-e53ad46c604c44e59a98bad27041cb19) – **TheMadraver**. Lavičky na nástupištích.
- [Low-Poly Lamp Post](https://sketchfab.com/3d-models/low-poly-lamp-post-c466684e819a4428b6d8ed50537615e4) – **Memorie**. Lampy na nástupištích.
- [CD810 motor train](https://sketchfab.com/3d-models/cd810-motor-train-121922992c4c44c9a121a40c82c79382) – **voxellation**. Motorový vůz.
- [Railway Track 3D scan](https://sketchfab.com/3d-models/railway-track-3d-scan-21a0ed90f355495fb80c3dcc92a94b10) – **grafi**. Kolejové lože.
- [Brick wall material](https://sketchfab.com/3d-models/brick-wall-material-6ef775e935da4d159e40098b06ad33d3) – **Ilya.Krupenkov**. Zdivo.
- [Cracked Concrete Old 3D Scan](https://sketchfab.com/3d-models/cracked-concrete-old-3d-scan-2d01ca3fd8fb4e96bf591110389a128c) – **grafi**. Nástupiště.

Textury: [docs/TEXTURES.md](docs/TEXTURES.md) (ambientCG, CC0). Zvuky: [docs/AUDIO.md](docs/AUDIO.md). Fonty: Barlow, SIL Open Font License ([docs/fonts-OFL.txt](docs/fonts-OFL.txt)).

Ke stažení modelů slouží oficiální Sketchfab Download API. Klíč se zadává skrytě nebo proměnnou `SKETCHFAB_API_TOKEN` a není součástí projektu ani historie Gitu:

```sh
SKETCHFAB_API_TOKEN=... python3 Tools/sketchfab_download.py a932f7c3850643fd92dadad7f5771d6c --output .local/tatra
blender --background --factory-startup \
  --python Tools/blender/prop_import.py -- .local/tatra/scene.gltf Assets/Resources/Models/Props/tatra_t3.glb length 14.6 26000 512
```

CJ je postava z Grand Theft Auto: San Andreas od Rockstar Games. Tento fanouškovský školní projekt nepoužívá žádné soubory z GTA. Postava vznikla z volně licencovaného basemeshe a vlastních materiálů. Hra není oficiálním produktem Českých drah ani Rockstar Games.
