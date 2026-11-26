# SuperCigo – design

## Koncept
CJ chce stihnout poslední spoj a projíždí deset skutečných železničních stanic Olomouce. Každá trať je 2,5D plošinovka po střechách vagonů. Kamera se dívá z boku a jen mírně shora. Hra je arkádová a humorná: přehnané nádražní hlášení, holubi, revizoři s brašnou, podchodoví „alchymisté“ s obřími injekcemi, kufry jako katapult a tvarůžkové turbo.

## Herní smyčka
1. V menu (odjezdová tabule) hráč vybere trať. Všechny jsou otevřené a mají ukazatel obtížnosti a osobní rekord.
2. Na trati sbírá jízdenky, přeskakuje mezery mezi vagony, vyhýbá se zásahům a dosahuje checkpointů (nástupiště se zeleným návěstidlem).
3. V cíli dostane výsledkovou jízdenku s časem a rekordem. Enter spustí další trať, R ji zopakuje, Esc vrátí do menu.

Čas měří simulace, takže pauza a ztráta zaostření se do něj nepočítají. Rekord trati je nejkratší čas a ukládá se do `PlayerPrefs` (`bestTime_<id>`). Body se ukládají zvlášť, u původních tří tratí pod původními klíči.

## Pohyb
Pohyb je „rychlý a přesný“: krátký rozjezd, okamžité otočení a skok, který jde řídit i ve vzduchu. Simulace běží s pevným krokem 120 Hz a vykreslení se mezi kroky interpoluje, takže hra se chová stejně při 60 i 240 FPS. Konstanty jsou `public const` v `RailGame` a čtou je i testy.

| Parametr | Hodnota |
|---|---|
| Běh | 9 m/s; zrychlení 95 m/s², brzdění 120 m/s², obrat 170 m/s² (ve vzduchu 65 / 30 / 110) |
| Skok | 16 m/s při gravitaci 40 m/s², výška 3,2 m za 0,40 s; puštění klávesy zkrátí rychlost na 6 m/s |
| Vrchol skoku | při \|vy\| < 2 m/s a drženém skoku poloviční gravitace (delší „viset“ nad mezerou) |
| Pád | gravitace ×1,5, nejvýš 20 m/s; rychlý pád (S / ↓) gravitace ×2,2, nejvýš 27 m/s |
| Dvojskok | 14,15 m/s, nikdy nezpomalí rychlejší stoupání |
| Odpuštění | coyote time 0,10 s, buffer skoku 0,14 s, korekce hrany 0,30 m (těsně netrefená střecha se ještě chytí) |
| Úhyb (Shift) | 17 m/s po dobu 0,17 s, nezranitelnost, nabíjení 1 s |
| Kufr / odraz od protivníka | 21,5 m/s / 13,7 m/s (se skokem 17 m/s) |
| Zásah | odhození 5,5 × 8,4 m/s na 0,2 s; pád pod úroveň kolejí (−0,95 m) stojí život |

Plošiny jsou střechy s pevnými boky po celé výšce, pohyblivé vagony vezou hráče, sběratelné předměty i vozík s sebou. EditMode testy počítají z těchto konstant dosah skoku a ověřují, že každou mezeru na všech tratích jde přeskočit (nejtěžší potřebuje 75 % dosahu).

## Tratě a obtížnost
Data tratí jsou v `RailLevel.All`. Každá trať má:
- `Platforms` (x, šířka, výška střechy, amplituda pohybu) a `Kinds`: `S` nástupiště, `T` osobní vůz nebo ČD 810, `C` nákladní vůz, `H` cisterna.
- Nástupiště tvoří start, checkpointy a cíl. Názvy úseků jsou v `Zones`.
- Polohy revizorů, holubů, podchodových postav, lékárniček, tvarůžků a kufrů.
- Téma kulis (`Theme`), denní dobu (`Sky`), počet životů, periodu páry a sílu větru.

Obtížnost roste plynule. Šířka střech klesá z 10 na 7 m, výška roste až na 7,4 m, pohyblivých vagonů přibývá a pára má kratší cyklus. Nemilany přidávají protivítr a přednádraží začíná se dvěma životy. EditMode testy ověřují, že každou mezeru jde podle fyziky skoku překonat a že nic nestojí ve vzduchu.

## Vizuální styl – stylizovaný realismus
- Realistické proporce a PBR materiály na všem. Jeden vlastní shader `Railway/Surface` (surface shader, Built-in RP) zvládá UV textury importovaných modelů s mapami ORM, skutečné triplanární mapování v metrech pro generovanou geometrii, režim „taška nahoře / omítka na bocích“ pro střechy a procedurální okna fasád a vozů, která se večer náhodně rozsvítí.
- Textury jsou sady z Poly Haven (CC0, barva + normála + ORM), viz [TEXTURES.md](TEXTURES.md). Natřený plech vozů a cisteren používá jen reliéf, barvu určuje livrej.
- Rim světlo v barvě oblohy pro ortografickou kameru, makro variace proti opakování dlaždic, kontaktní ztmavení u země a specular anti-aliasing.
- Osvětlení: hlavní světlo z boku (−55°), jedna stabilní kaskáda stínů, ambient a odrazy z oblohy (reflection probe). Šest presetů denní doby s vlastní oblohou, mlhou a barevným laděním (bloom, ACES, vinětace).
- Postavy: CJ (fanouškovský model ze Sketchfabu), revizor a podchodový alchymista jsou texturované modely s Humanoid kostrou. Pohyb řídí mocap animace Quaternius Universal Animation Library (běh podle rychlosti bez klouzání nohou, odraz, let, dopad, zásah, výhra). Nad nimi je procedurální vrstva: náklon v zatáčce, squash & stretch, salto při dvojskoku a prach.
- Vozidla (ČD 810, osobní vůz Bdt, nákladní vozy, cisterny) se staví v enginu přesně na délku plošiny ze stejných materiálů jako zbytek světa. Stromy jsou lípy ze Sketchfabu s průsvitnými listy, holubi mají animované mávání křídel.

## Rozhraní – železniční značení
- **Menu**: odjezdová tabule „ODJEZDY“ s LED rastrem. Sloupce: rekord, číslo vlaku, směr (stanice), obtížnost a číslo trati. Vlevo je karta vybrané trati.
- **HUD**: smaltovaná cedule stanice, pod ní úsek a checkpointy. Uprostřed stopky s rekordem, vpravo životy jako signální světla, body, jízdenky, kombo a stav úhybu. Dole pás trati s body checkpointů a lokomotivou. Hlášení přijíždějí jako nádražní rozhlas.
- **Výsledky**: jízdenka s ústřižkem, časem, razítkem nového rekordu a statistikami.
- Rozhraní se kreslí v IMGUI bez maticového škálování. Text se rasterizuje v cílové velikosti a prvky se kotví k okrajům, takže jsou ostré od 1280×720 po 4K i na jiném poměru stran.

## Zvuk
Viz [AUDIO.md](AUDIO.md). Pauza zastaví celý mix, hlášení se zobrazí jen tehdy, když opravdu zazní.

## Tým, platforma a rozsah
- **Tým:** Jan Richtermoc a Jakub Šálek. Kdo co dělal, je v README v sekci Autoři a v historii Gitu.
- **Platforma:** Windows 10/11 (x64, DirectX 11) a macOS (universal: Apple silicon i Intel). Ovládání klávesnicí, v menu i myší; funguje i herní ovladač.
- **Rozsah verze 1.1:** deset tratí, jeden hráč, lokální rekordy. Mimo rozsah: multiplayer, online žebříčky, editor tratí, mobilní verze.

