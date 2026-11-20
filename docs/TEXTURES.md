# Textury

PBR sady (albedo `_A`, normálová mapa OpenGL `_N`, ORM = okluze / drsnost / kov `_ORM`) z [Poly Haven](https://polyhaven.com), licence **CC0 1.0** (volné dílo, atribuce není povinná, uvádíme ji pro přehled). Stahuje je skript `Tools/polyhaven_download.py`, který zároveň vygeneruje katalog `Assets/Scripts/RailTextures.cs` (velikost dlaždice v metrech a průměrný jas pro neutrální tónování) a strojově čitelný seznam [TEXTURES.json](TEXTURES.json).

| Klíč (`Assets/Resources/Textures/PH/<Klíč>_{A,N,ORM}.jpg`) | Poly Haven | Rozlišení | Dlaždice | Použití |
|---|---|---|---|---|
| `Ballast` | [Rocks Ground 01](https://polyhaven.com/a/rocks_ground_01) | 1k | 2,0 m | kolejové lože, náspy |
| `Paving` | [Large Square Pattern 01](https://polyhaven.com/a/large_square_pattern_01) | 2k | 3,0 m | dlažba nástupišť a předprostorů |
| `Concrete` | [Brushed Concrete](https://polyhaven.com/a/brushed_concrete) | 2k | 2,5 m | pražce, boky nástupišť, pilíře, most |
| `Asphalt` | [Asphalt 02](https://polyhaven.com/a/asphalt_02) | 2k | 3,0 m | ulice a parkoviště |
| `RailSteel` | [Rusty Metal 02](https://polyhaven.com/a/rusty_metal_02) | 1k | 1,0 m | kolejnice |
| `Brick` | [Red Brick 03](https://polyhaven.com/a/red_brick_03) | 1k | 1,0 m | cihlové zdivo (výpravní budova, depo, domy) |
| `Plaster` | [Painted Plaster Wall](https://polyhaven.com/a/painted_plaster_wall) | 1k | 2,0 m | omítnuté fasády |
| `PanelConcrete` | [Concrete Wall 008](https://polyhaven.com/a/concrete_wall_008) | 2k | 2,71 m | paneláky a nemocnice |
| `RoofTiles` | [Clay Roof Tiles 02](https://polyhaven.com/a/clay_roof_tiles_02) | 2k | 2,5 m | tašková střecha (horní plochy) |
| `Corrugated` | [Corrugated Iron 02](https://polyhaven.com/a/corrugated_iron_02) | 2k | 2,7 m | nákladní vozy, kontejnery, haly |
| `MetalPlate` | [Metal Plate 02](https://polyhaven.com/a/metal_plate_02) | 1k | 2,0 m | natřený plech: vozy, cisterny, lampy, skleník |
| `TreadPlate` | [Metal Grate Rusty](https://polyhaven.com/a/metal_grate_rusty) | 1k | 0,5 m | lávky a plošiny |
| `Grass` | [Leafy Grass](https://polyhaven.com/a/leafy_grass) | 1k | 2,0 m | trávníky |
| `Planks` | [Weathered Planks](https://polyhaven.com/a/weathered_planks) | 1k | 2,0 m | dřevěné prvky |
| `SiloConcrete` | [Concrete Panels](https://polyhaven.com/a/concrete_panels) | 2k | 8,0 m | sila, nábřeží |

Textury se ve hře mapují v metrech triplanární projekcí shaderu `Railway/Surface`, takže mají stejnou hustotu na objektu libovolné velikosti. Střechy používají režim `_TRIPLANAR_TOP` (tašky nahoře, omítka na bocích). Okna budov a vozů kreslí shader procedurálně (režim `_FACADE`), večer se část z nich rozsvítí.

Potisk sbíraných předmětů (jízdenka, lékárnička, tvarůžky, kufr, brašna revizora) v `Assets/Resources/Textures/Pickups` je vlastní, vykreslený skriptem `Tools/make_pickup_textures.py` s fonty Barlow.

Textury postav, holuba a lípy pocházejí ze stažených modelů (viz README, sekce Předlohy a licence).
