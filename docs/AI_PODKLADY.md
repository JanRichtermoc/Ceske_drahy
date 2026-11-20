# Podklady k deníku selhání AI

| # | Co se mělo stát | Co AI udělala špatně | Jak se to poznalo | Jak se to opravilo |
|---|---|---|---|---|
| 1 | Na checkpoint postavit model návěstidla ze Sketchfabu. | Skript, který měl z modelu odstranit malou postavičku pro měřítko, si spletl hlavní objekt a nechal postavičku místo návěstidla. | Na screenshotu ze hry stál na nástupišti bílý zářící „duch“. | Model byl vyřazen a návěstidlo se postavilo procedurálně v `RailWorld.PlatformStation`. Chybná volba ze skriptu `Tools/blender/prop_import.py` zmizela. |
| 2 | Importovat realistický model CJ. | Nevšimla si, že soubor ze Sketchfabu má v hierarchii prázdné uzly se škálou 100. Kosti a mesh pak v Unity nesouhlasily. | CJ ve hře nebyl vidět. Výpis hranic rendereru ukázal souřadnice ve stovkách metrů. | V Blenderu se hierarchie zploštila, transformace se aplikovaly a model se převedl do metrů. |
| 3 | Exportovat CJ v klidové T-póze. | Ignorovala animaci uloženou v souboru, takže export vyšel v boxerském postoji. | Náhledový render z Blenderu. | Před exportem se smazala animační data a vynulovala póza kostí. |
| 4 | Obarvit CJ (tmavá pleť, modré džíny). | Převzala barvy z glTF bez převodu z lineárního prostoru, takže pleť vyšla světlá a džíny šedé. | Close-up screenshot ve hře. | Barvy materiálů se nastavují explicitně v `CjRig.AttachModel`. |
| 5 | Nové UI v IMGUI. | Vytvořila `GUIStyle` v inicializaci pole, což Unity nepodporuje. | Výjimka `NullReferenceException` v `RailHUD.Width` v konzoli. | `GUIStyle` se vytváří v `Awake`. |
| 6 | Dávkové zpracování modelů v Blenderu. | Napsala smyčku s `set -- $spec`, která v zsh nerozdělí slova. Blender pak dostal špatné argumenty. | `IndexError: list index out of range` u všech modelů. | Smyčka se spustila v bash. |
| 7 | Smoke test páry po přeřazení tratí. | Test počítal s tím, že depo je druhá trať, což po přeřazení přestalo platit. | Test selhal: „An active steam vent must damage the player“. | Test hledá trať podle `Id`. |
| 8 | Ruční ověření výsledkové obrazovky. | Teleportovala postavu do cíle, a tím do `PlayerPrefs` uložila skutečné „rekordy“, například 00:00.01. | Nesmyslný rekord na odjezdové tabuli. | Testovací klíče se smazaly. Automatické testy rekordy zálohují a obnovují. |
| 9 | Spustit Blender. | Předpokládala cestu `/Applications/Blender.app`, ale aplikace je na externím disku. | `no such file or directory`. | Použila se skutečná cesta z `mdfind`. |
| 10 | Návrh dat deseti tratí. | Jeden protivník v původní trati a jeden tvarůžek v nové trati „visely“ v mezeře mezi vagony. | Nový EditMode test `EnemiesAndPickupsStandOnPlatforms`. | Souřadnice se posunuly na plošiny. |
| 11 | Zastávky v Olomouci. | Jako jednu z možností navrhla „Olomouc-Chválkovice“, kterou ale nebylo možné ověřit. | Dotaz na Wikipedii (kategorie *Železniční stanice v Olomouci*). | Nahrazena ověřenou zastávkou Olomouc-Nová Ulice. |

## Verze 1.1

| # | Co se mělo stát | Co AI udělala špatně | Jak se to poznalo | Jak se to opravilo |
|---|---|---|---|---|
| 12 | Ověřit stisky kláves simulované přes Unity MCP. | Spoléhala na `wasPressedThisFrame` z Input System, který simulované stisky mezi snímky nezachytí. | Postava na simulovaný skok nereagovala, i když `isPressed` hlásil stisk. | `RailInput` detekuje hranu stisku sám z `isPressed` (polling). |
| 13 | Upravit kód během testování v Play Mode. | Spustila rekompilaci, zatímco hra běžela. | Konzole se zaplnila desítkami tisíc `NullReferenceException`. | Před každou kompilací se Play Mode ukončí; postup je v pracovním logu. |
| 14 | Natočit CJ čelem ke kameře. | Odhadovala směr modelu podle polohy špiček nohou a odhad vyšel opačně. | CJ běžel zády k hráči. | Heuristika zmizela, model má pevně danou orientaci (−Z). |
| 15 | Smyčka mávání holuba. | Export z Blenderu zapekl celý rozsah scény (10,4 s) místo akce mávání (4 s). | Holub dlouho visel bez pohybu; asset test délky klipu. | Rozsah scény se před exportem nastaví podle akce. |
| 16 | Postavit stromy z modelu lípy. | Obal modelu přepsal rotaci kořene FBX, která převádí osy z Blenderu. | Na snímcích z prohlídky tratí ležely kmeny vodorovně. | Rotaci a měřítko nese obalový objekt, model si nechá vlastní rotaci. |
| 17 | Okna na nových vozech ČD 810 a Bdt. | Shader odečetl okraj i ve svislém směru a modul okna měl stejnou výšku jako pás oken, takže se nevykreslilo žádné okno. | Vozy na snímcích neměly okna. | Okraj platí jen podél vozu, modul je o něco nižší než pás. |
| 18 | Použít model LED lampy ze Sketchfabu. | Navrhla model bez kontroly geometrie; po importu měl rozměr 1 × 1 × 1 m a v náhledu nebyl vidět. | Kontrola rozměrů a render v Blenderu. | Lampa se staví procedurálně (`RailWorld.StreetLight`). |
| 19 | Nahradit funkci `Lamp` v kulisách. | Skript pro náhradu textu zasáhl špatný úsek a poškodil `OlomoucBackdrops.cs`. | 20 chyb kompilace na řádcích, které v souboru nemají být. | Soubor se opravil podle `git diff`, náhrady se dělají přes jednoznačné úseky. |
| 20 | Barvy textu v HUD. | Převod barev do lineárního prostoru použila jen na plochy, text zůstal bez převodu a vypadal vybledle. | Měření pixelů: stejná oranžová měla na ploše `#F5B335`, v textu `#FBDA7E`. | `Label()` převádí barvy stejně jako `Fill()`. |
| 21 | Připravit model CJ pro Humanoid animace. | Zapekla pózu obličejových kostí z modu, která roztrhla horní ret. | Detailní portrét pro ikonu aplikace. | `character_prep.py` vyhladí oblast rtů po zapečení pózy. |
