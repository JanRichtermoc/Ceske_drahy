# Ruční testovací scénář

Projdi na sestaveném buildu (Windows `SuperCigo.exe` nebo macOS aplikace) v okně 1280×720 i na celé obrazovce. U každého kroku zapiš OK nebo popis chyby do protokolu na konci.

> **Kde se testovalo:** macOS build přímo na Macu s Apple M4. **Windows build jen ve virtuálu na Macu** (Parallels Desktop, Windows 11 pro ARM, hra v emulaci x64), protože tým nemá počítač s Windows. Výsledek je v [protokolu Windows ve virtuálu](#protokol-windows-ve-virtuálu) a podrobnosti v [VALIDATION.md](VALIDATION.md#windows-ve-virtuálu-parallels-desktop-na-macu). Na skutečném počítači s Windows hra zatím otestovaná není.

## 0. Instalace (stažení z GitHub Releases jako běžný uživatel)
| # | Krok | Očekávaný výsledek |
|---|---|---|
| 0.1 | Windows: stáhni `SuperCigo-Windows.zip` z README, **Extrahovat vše…**, spusť `SuperCigo\SuperCigo.exe`. Pokud se objeví SmartScreen, zvol **Další informace → Přesto spustit**. | Hra se spustí v okně 1280×720 s ikonou SC, bez úvodního loga Unity. |
| 0.2 | macOS (jiný Mac než ten, na kterém vznikl build): stáhni `SuperCigo-macOS.zip` přes Safari, rozbal dvojklikem a spusť. Postupuj podle README (macOS 15: Nastavení systému → Soukromí a zabezpečení → **Přesto otevřít**). | Po povolení se hra spustí a při dalším spuštění se už neptá. Na Macu s Intelem i s Apple silicon. |
| 0.3 | Ukonči hru a spusť ji znovu. | Nastavení a rekordy zůstaly. |

## 1. Spuštění a menu
| # | Krok | Očekávaný výsledek |
|---|---|---|
| 1.1 | Spusť hru. | Otevře se okno s hlavním menu: vlevo nápis SUPERCIGO, vpravo tabule ODJEZDY s 10 tratěmi a nahoře hodiny se systémovým časem. |
| 1.2 | Šipkami ↑/↓ (nebo W/S) projdi tabuli. | Zvýrazněný řádek se posouvá a karta vlevo ukazuje název, popis, obtížnost a rekord vybrané trati. Z první trati jde šipka ↑ na poslední. |
| 1.3 | Najeď myší na řádek. | Řádek se zvýrazní. |
| 1.4 | Klikni na NASTAVENÍ, pak na HOTOVO. | Otevře a zavře se nastavení. Klik na HOTOVO nespustí žádnou trať. |
| 1.5 | Otevři nastavení a stiskni Esc. | Nastavení se zavře, menu zůstane. |
| 1.6 | Změň měřítko okna myší. | Rozhraní se přizpůsobí, text zůstane ostrý a nic nepřeteče mimo okno. |

## 2. Hraní trati
| # | Krok | Očekávaný výsledek |
|---|---|---|
| 2.1 | Vyber trať 1 a stiskni Enter. | Krátce se ukáže „PŘISTAVUJEME SOUPRAVU…“, pak trať. Stopky běží od 00:00.00. |
| 2.2 | A/D, mezerník, krátký stisk mezerníku. | Běh se rozjede i zastaví skoro okamžitě. Krátký stisk dá nízký skok, druhý stisk ve vzduchu dvojskok (CJ udělá salto). |
| 2.2a | Ve vzduchu drž S nebo ↓. | CJ padá znatelně rychleji. |
| 2.2b | Skoč těsně pod hranu vyšší střechy. | Postava se ještě „vytáhne“ na střechu (korekce hrany), při větší chybě narazí do boku vagonu. |
| 2.3 | Šlápni na růžový kufr a podrž mezerník. | Vysoký výskok. Puštění mezerníku ho nezkrátí. |
| 2.4 | Shift. | Rychlý úhyb, ukazatel ÚHYB se vybije a znovu nabije. |
| 2.5 | Seber jízdenku a skoč holubovi na hlavu. | Přičtou se body, ukáže se kombo a počet jízdenek x/y. |
| 2.6 | Nech se zasáhnout. | Ubyde jedno světlo životů, okraje bliknou červeně a postava chvíli bliká. |
| 2.7 | Spadni mezi vagony. | Ubyde život a postava se vrátí na poslední checkpoint. |
| 2.8 | Dojdi na nástupiště uprostřed trati. | Hlášení CHECKPOINT 1 / 2. Návěstidlo na konci nástupiště se přepne z červené na zelenou a na pásu trati dole se bod obarví zeleně. |
| 2.9 | Esc. | Pauza: čas stojí, zvuk ztichne. POKRAČOVAT nebo Esc hru obnoví. |
| 2.10 | Během hry přepni do jiného okna. | Hra se sama pozastaví. |
| 2.11 | V pauze klikni na RESTART TRATI. | Trať začne od startu se zcela novým časem. |

## 3. Cíl a rekord
| # | Krok | Očekávaný výsledek |
|---|---|---|
| 3.1 | Dojeď do cíle (CÍL). | Zobrazí se jízdenka s časem, statistikami a razítkem NOVÝ REKORD. |
| 3.2 | Stiskni R. | Stejná trať od začátku. Pod stopkami je REKORD s předchozím časem. |
| 3.3 | Dojeď pomaleji. | Jízdenka ukáže rekord a rozdíl (+x,xx s), razítko chybí. |
| 3.4 | Stiskni Esc. | Hlavní menu. V řádku trati je zelený rekordní čas. |
| 3.5 | Ukonči hru (KONEC HRY) a spusť znovu. | Rekord v tabuli zůstal. |
| 3.6 | Na jízdence stiskni Enter. | Spustí se další trať v pořadí. |

## 4. Tratě
Na každé trati ověř, že jde dohrát a že kulisy odpovídají stanici: park s Florou, nemocnice s tramvají, Olomouc město s věžemi, depo s párou, most přes Moravu, sila a kontejnery v Řepčíně, paneláky na Nových Sadech, pole a vítr v Nemilanech a noční seřadiště. V Nemilanech se před poryvem rozbliká výstraha VÍTR SE ZVEDÁ a větrné rukávy se otočí. Na desáté trati jsou na startu dva životy a po dojetí jízdenka hlásí VŠECH 10 TRATÍ!

## 5. Nastavení
| # | Krok | Očekávaný výsledek |
|---|---|---|
| 5.1 | ZVUK vypnout, M. | Zvuk se vypne a zapne, přepínač se změní. |
| 5.2 | GRAFIKA → ÚSPORNÁ. | Zmizí stíny, vyhlazení a post-processing a FPS vzroste. |
| 5.3 | OTŘESY vypnout a nechat se zasáhnout. | Kamera se netřese, postava nebliká. |
| 5.4 | CELÁ OBRAZOVKA. | Hra se přepne na celou obrazovku a zpět. |
| 5.5 | Táhni posuvník ŘEV PŘI ZÁSAHU, ukonči a spusť hru. | Hodnota zůstala uložená. |

## 6. Herní ovladač (volitelné)
| # | Krok | Očekávaný výsledek |
|---|---|---|
| 6.1 | Připoj ovladač Xbox nebo PlayStation a v menu použij D-pad a A / ✕. | Výběr trati a start fungují. |
| 6.2 | Na trati: levá páčka, A / ✕, X / ▢, Start. | Pohyb, skok, úhyb, pauza. |

## Automatické testy
Ve Unity: *Window → General → Test Runner → EditMode → Run All*. Výsledek musí být **72 / 72** zelených (62 kontrol tratí a fyziky, 9 kontrol assetů a 1 integrační test). Integrační test sám vstoupí do Play Mode, spustí 40 kontrol a projede všech deset tratí. Příkaz pro dávkový režim je v README.

## Protokol

Vyplň jeden protokol za každé testované zařízení. Chyby popiš tak, aby šly zopakovat (trať, místo, co jsi dělal).

| Položka | Hodnota |
|---|---|
| Tester | |
| Datum | |
| Build (verze a soubor) | SuperCigo 1.1.0, `SuperCigo-…zip` |
| Operační systém a verze | |
| Procesor / grafická karta | |
| Rozlišení a režim (okno / celá obrazovka) | |

| Oddíl | Výsledek (OK / chyba) | Poznámka |
|---|---|---|
| 0. Instalace | | |
| 1. Spuštění a menu | | |
| 2. Hraní trati | | |
| 3. Cíl a rekord | | |
| 4. Tratě (1–10) | | |
| 5. Nastavení | | |
| 6. Herní ovladač | | |
| FPS v pravém dolním rohu HUD (typická hodnota) | | |

## Protokol: Windows ve virtuálu

Testy proběhly ve virtuálním stroji na Macu, ne na skutečném počítači s Windows. Hru ovládal Claude Code (AI asistent) z Macu přes `prlctl exec` na pokyn Jana Richtermoce; snímky obrazovky se porovnávaly s macOS buildem.

| Položka | Hodnota |
|---|---|
| Tester | Claude Code pro Jana Richtermoce |
| Datum | 26. 11. 2026 |
| Build (verze a soubor) | 1.1.0 z vydání v1.1.0 (`SuperCigos-Windows.zip`, ještě pod původním názvem) a opravený build ze stejného kódu s `RailWorld.GpuInstancing` |
| Operační systém a verze | Windows 11 pro ARM v Parallels Desktop 26.4, hostitel macOS 27 na Macu s Apple M4 |
| Procesor / grafická karta | 4 virtuální jádra; Parallels Display Adapter (WDDM), Direct3D 11 feature level 11.1, hlášeno 128 MB grafické paměti |
| Rozlišení a režim (okno / celá obrazovka) | okno maximalizované přes celý virtuál, 3420 × 2070 |

| Oddíl | Výsledek (OK / chyba) | Poznámka |
|---|---|---|
| 0. Instalace | částečně OK | Rozbalený ZIP se spustí. Stažení přes prohlížeč a SmartScreen neověřeny. |
| 1. Spuštění a menu | 1.1 OK | Menu, tabule ODJEZDY a hodiny se zobrazí. Kroky 1.2–1.6 neprovedeny. |
| 2. Hraní trati | 2.1 chyba → opraveno | Na trati 1 chyběly budova s okny, vagony v pozadí, sloupy a většina jízdenek (ovladač Parallels nevykreslí GPU instancing). Po opravě obraz odpovídá macOS. Kroky 2.2–2.11 neprovedeny. |
| 3. Cíl a rekord | neprovedeno | |
| 4. Tratě (1–10) | jen trať 1 | |
| 5. Nastavení | 5.2 částečně OK | Hra začne na *Grafika · Úsporná* (128 MB paměti). *Vysoká* (přepnutá v uložených preferencích, ne v menu) zapne stíny a post-processing. Ostatní kroky neprovedeny. |
| 6. Herní ovladač | neprovedeno | |
| FPS v pravém dolním rohu HUD (typická hodnota) | 55 / 41–43 | Úsporná / Vysoká grafika, trať 1 |
