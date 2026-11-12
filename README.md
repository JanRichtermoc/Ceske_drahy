# České dráhy — Poslední spoj

Hratelná 2,5D plošinovka v **Unity 6000.6.8f1**. David, mladý Rom z Prahy, má platnou jízdenku, ale stojí na špatném nástupišti. Cesta k poslednímu spoji vede přes střechy vagonů, holuby a revizory s brašnami. Etnicita postavy nemění pravidla hry.

První hratelná verze obsahuje jednu souvislou trať se třemi úseky: Praha hlavní nádraží, Vršovice a depo. Vizuál je stylizované 3D s večerním osvětlením; nejde o fotorealistickou hotovou produkční hru.

![Herní menu](docs/menu.png)

## Spuštění

1. Přidej tuto složku v Unity Hub a otevři ji v Unity **6000.6.8f1**.
2. Otevři `Assets/Scenes/PrazskyVecer.unity`.
3. Stiskni **Play**, potom **Enter** nebo „Na nástupiště“.

Scéna obsahuje herní bootstrap; prostředí se vytváří při spuštění. Příkaz **České dráhy → Prepare game scene** ji znovu vytvoří. Příkaz **České dráhy → Build macOS** sestaví samostatnou aplikaci do `Builds/CeskeDrahy.app`. Pro Windows lze scénu sestavit přes Unity Build Profiles po instalaci Windows Build Support.

## Ovládání

| Klávesa | Akce |
|---|---|
| A/D nebo šipky | Pohyb |
| Mezerník, W nebo ↑ | Skok; délka podržení řídí výšku |
| Esc | Pauza / pokračování |
| R | Návrat na poslední checkpoint |
| M | Zapnutí / vypnutí zvuku |
| Enter | Start / nový pokus |

Sbírej jízdenky, doskoč na střechy vlaků a dojdi k cílové vlajce. Žlutá kontrolka nad revizorem předchází hodu brašnou. Po zásahu následuje krátká nezranitelnost. Pád stojí jeden ze tří životů. Checkpointy jsou na nástupištích ve Vršovicích a depu. Ztráta zaostření okna hru pozastaví.

## Implementace a výkon

- Kinematický pohyb ve 120Hz pevném kroku s detekcí přechodu přes střechu, tolerancí skoku po opuštění hrany a bufferem vstupu.
- Pohyblivé vagony přenášejí hráče; animované postavy, holubi a sběratelné předměty.
- Pool 12 projektilů, sdílené materiály, komprimované textury kolejiště do 1024 px, omezená vzdálenost stínů.
- Model ČD 810 má přibližně 27 tisíc trojúhelníků. Ostatní vagony, prostředí a postavy jsou vytvářené z geometrie v projektu.
- Úsporný režim vypíná stíny a MSAA; zvuk a otřesy kamery mají samostatné volby. Nastavení a rekord se ukládají lokálně.
- Cíl je 60 FPS. Výkon závisí na zařízení; benchmark a omezení jsou v `docs/VALIDATION.md`.

## Unity přes MCP

Projekt používá Unity Pipeline 1.0.0. S otevřeným editorem a nainstalovaným Unity CLI:

```sh
python3 Tools/unity_mcp.py
python3 Tools/unity_mcp.py eval '{"code":"return UnityEngine.Application.unityVersion;"}'
python3 Tools/unity_mcp.py eval '{"code":"UnityEditor.EditorApplication.isPlaying=true;"}'
python3 Tools/unity_mcp.py eval '{"code":"return RailSmokeTests.Run();"}'
```

Poslední příkaz spouští jedenáct integračních kontrol v Play Mode. Průchodnost trati testuje s vypnutým zraněním; zranění, nezranitelnost a prohra se ověřují samostatně. Testy nejsou náhradou uživatelského testování obtížnosti.

## Sketchfab a licence

- [Railway Track 3D scan](https://sketchfab.com/3d-models/railway-track-3d-scan-21a0ed90f355495fb80c3dcc92a94b10) — **grafi**, [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). Barevná a normálová textura zmenšena na 1024 px, použita na kolejovém loži.
- [CD810 motor train](https://sketchfab.com/3d-models/cd810-motor-train-121922992c4c44c9a121a40c82c79382) — **voxellation**, [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). Import glTF do Unity, upravené měřítko pro herní plošiny.

Původní licenční soubory a strojově čitelné atribuce jsou ve složce `docs`. Ke stažení byl použit oficiální Sketchfab Download API. API klíč není součástí projektu ani historie Gitu. Obnovitelné stažení:

```sh
python3 Tools/sketchfab_download.py 121922992c4c44c9a121a40c82c79382
```

Nástroj si klíč vyžádá skrytým vstupem, případně přečte proměnnou `SKETCHFAB_API_TOKEN`. Hra funguje offline a klíč nepotřebuje.

Osobní fanouškovský projekt; není oficiální hrou Českých drah. Neobsahuje grafiku, hudbu ani úrovně ze Super Mario Bros.
