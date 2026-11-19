# SuperCigos – design

## Koncept
CJ chce stihnout poslední spoj a projíždí deset skutečných železničních stanic Olomouce. Každá trať je 2,5D plošinovka po střechách vagonů. Kamera se dívá z boku a jen mírně shora. Hra je arkádová a humorná: přehnané nádražní hlášení, holubi, revizoři s brašnou, podchodoví „alchymisté“ s obřími injekcemi, kufry jako katapult a tvarůžkové turbo.

## Herní smyčka
1. V menu (odjezdová tabule) hráč vybere trať. Všechny jsou otevřené a mají ukazatel obtížnosti a osobní rekord.
2. Na trati sbírá jízdenky, přeskakuje mezery mezi vagony, vyhýbá se zásahům a dosahuje checkpointů (nástupiště se zeleným návěstidlem).
3. V cíli dostane výsledkovou jízdenku s časem a rekordem. Enter spustí další trať, R ji zopakuje, Esc vrátí do menu.

Čas měří simulace, takže pauza a ztráta zaostření se do něj nepočítají. Rekord trati je nejkratší čas a ukládá se do `PlayerPrefs` (`bestTime_<id>`). Body se ukládají zvlášť, u původních tří tratí pod původními klíči.

## Pohyb
- Pevný krok 120 Hz s interpolací vykreslení. Běh 7,8 m/s, skok 13,6 m/s, dvojskok 12,3 m/s, gravitace 28 m/s².
- Buffer skoku 140 ms, coyote time 110 ms, variabilní výška skoku. Puštění klávesy zkrátí jen vlastní skok, nikdy kufr ani odraz od protivníka.
- Úhyb (Shift) trvá 0,19 s, dává nezranitelnost a dobíjí se 1,25 s.
- Plošiny jsou jednosměrné střechy s pevnými boky. Pohyblivé vagony veze hráče s sebou.

## Tratě a obtížnost
Data tratí jsou v `RailLevel.All`. Každá trať má:
- `Platforms` (x, šířka, výška střechy, amplituda pohybu) a `Kinds`: `S` nástupiště, `T` osobní vůz nebo ČD 810, `C` nákladní vůz, `H` cisterna.
- Nástupiště tvoří start, checkpointy a cíl. Názvy úseků jsou v `Zones`.
- Polohy revizorů, holubů, podchodových postav, lékárniček, tvarůžků a kufrů.
- Téma kulis (`Theme`), denní dobu (`Sky`), počet životů, periodu páry a sílu větru.

Obtížnost roste plynule. Šířka střech klesá z 10 na 7 m, výška roste až na 7,4 m, pohyblivých vagonů přibývá a pára má kratší cyklus. Nemilany přidávají protivítr a přednádraží začíná se dvěma životy. EditMode testy ověřují, že každou mezeru jde podle fyziky skoku překonat a že nic nestojí ve vzduchu.

## Vizuální styl – stylizovaný realismus
- PBR textury (ambientCG, skeny ze Sketchfabu) s triplanárním mapováním v metrech.
- Vlastní shader `Railway/Surface` přidává rim světlo v barvě oblohy, jemnou patinu podle polohy na objektu a kontaktní ztmavení u země.
- Šest presetů denní doby (den, odpoledne, soumrak, západ, večer, noc). Každý má vlastní oblohu s mraky a hvězdami, mlhu a barevné ladění v post-processu (bloom, ACES, vinětace).
- Okna paneláků a hal se po setmění rozsvítí přes emisní masku. Bloom pak zvýrazní lampy, světlomety a návěstidla.
- Stromy mají shader s větrem a „chomáči“ listí. Řeka Morava má vlastní vodní shader.
- CJ je realistické tělo (basemesh CC BY) oblečené v Blenderu do bílého tílka, volných džínů a tenisek. Pohyb řídí procedurální kostra (běh, skok, dopad, úhyb, zásah).

## Rozhraní – železniční značení
- **Menu**: odjezdová tabule „ODJEZDY“ s LED rastrem. Sloupce: rekord, číslo vlaku, směr (stanice), obtížnost a číslo trati. Vlevo je karta vybrané trati.
- **HUD**: smaltovaná cedule stanice, pod ní úsek a checkpointy. Uprostřed stopky s rekordem, vpravo životy jako signální světla, body, jízdenky, kombo a stav úhybu. Dole pás trati s body checkpointů a lokomotivou. Hlášení přijíždějí jako nádražní rozhlas.
- **Výsledky**: jízdenka s ústřižkem, časem, razítkem nového rekordu a statistikami.
- Rozhraní se kreslí v IMGUI bez maticového škálování. Text se rasterizuje v cílové velikosti a prvky se kotví k okrajům, takže jsou ostré od 1280×720 po 4K i na jiném poměru stran.

## Zvuk
Viz [AUDIO.md](AUDIO.md). Pauza zastaví celý mix, hlášení se zobrazí jen tehdy, když opravdu zazní.
