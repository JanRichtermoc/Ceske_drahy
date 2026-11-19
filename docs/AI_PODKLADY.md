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
