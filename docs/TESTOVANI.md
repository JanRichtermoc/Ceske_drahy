# Ruční testovací scénář

Projdi na sestaveném buildu (Windows `CeskeDrahy.exe` nebo macOS aplikace) v okně 1280×720 i na celé obrazovce. U každého kroku zapiš OK nebo popis chyby.

## 1. Spuštění a menu
| # | Krok | Očekávaný výsledek |
|---|---|---|
| 1.1 | Spusť hru. | Otevře se okno s hlavním menu: vlevo nápis ČESKÉ DRÁHY, vpravo tabule ODJEZDY s 10 tratěmi a nahoře hodiny se systémovým časem. |
| 1.2 | Šipkami ↑/↓ (nebo W/S) projdi tabuli. | Zvýrazněný řádek se posouvá a karta vlevo ukazuje název, popis, obtížnost a rekord vybrané trati. Z první trati jde šipka ↑ na poslední. |
| 1.3 | Najeď myší na řádek. | Řádek se zvýrazní. |
| 1.4 | Klikni na NASTAVENÍ, pak na HOTOVO. | Otevře a zavře se nastavení. Klik na HOTOVO nespustí žádnou trať. |
| 1.5 | Otevři nastavení a stiskni Esc. | Nastavení se zavře, menu zůstane. |
| 1.6 | Změň měřítko okna myší. | Rozhraní se přizpůsobí, text zůstane ostrý a nic nepřeteče mimo okno. |

## 2. Hraní trati
| # | Krok | Očekávaný výsledek |
|---|---|---|
| 2.1 | Vyber trať 1 a stiskni Enter. | Krátce se ukáže „PŘISTAVUJEME SOUPRAVU…“, pak trať. Stopky běží od 00:00.00. |
| 2.2 | A/D, mezerník, krátký stisk mezerníku. | Běh, skok. Krátký stisk dá nižší skok, druhý stisk ve vzduchu dvojskok. |
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

## Automatické testy
Ve Unity: *Window → General → Test Runner → EditMode → Run All*. Výsledek musí být 55 / 55 zelených. Integrační test sám vstoupí do Play Mode a projede všech deset tratí.
