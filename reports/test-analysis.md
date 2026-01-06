# Testų generavimo ir vykdymo analizė

## Iteracijų rodikliai (šios sesijos)
- Iteracijų, kol visi sugeneruoti testai susikompiliavo: ~2 (pirmas bandymas krito dėl trūkstamų AI env kintamųjų, antras – sėkmingas kompiliavimas).
- Iteracijų, kol visi testai stabiliai praeina: 6 bėgimai (env klaida → 4 neteisingi assertai → 2 JSON parsinimo klaidos → pass; vėliau pridėjus 6 naujus testus prireikė dar 2 bėgimų, galutinis 18/18).
- Vidutiniškai programuotojo intervencijų: 5 kodo pataisos (env kintamųjų injekcija; analyzer izoliavimas; assertų priderinimas prie faktinės API; pridėti paginacijos/statistikos/paieškos testai; atlaisvintas repo-by-id atsakymo parsinimas).
- Praėjimo rodiklis su pilnu kontekstu: 100% (galutinis bėgimas 18/18). Pradinio nestabilumo beveik nebuvo – nesėkmės buvo deterministinės dėl konfig./lūkesčių neatitikimų.

## Kokybė ir aprėptis
- Funkcinė aprėptis: autentifikacija, sveikatos patikra, repo sąrašas/paginacija, repo statistika, repo paieška (id/owner), klaidų tvarkymas dingusiems repo/sandėliams. Iš viso 18 integracinių testų.
- Kodo aprėptis: kiekybiškai nematuota (nepaleista coverage įrankių). Kokybiškai – pagrindiniai vieši HTTP endpointai pagal reikalavimus.
- Mutacijų testavimas: nevykdytas.
- Sudėtingi scenarijai: netestuoti (nėra failų upload/Excel/export); dabartinis rinkinys fokusuotas į skaitymo/listavimo ir auth srautus.

## Vykdymo charakteristikos
- Trukmė: ~7–9 s vienam `dotnet test` bėgimui lokaliai (SQLite in-memory).
- Stabilumas: po konfigūracijos sutvarkymo ir lūkesčių suderinimo testai stabilūs (paskutiniuose 2 bėgimuose flakiness nepastebėta).

## Pastangos ir gairės
- Įrankiui reikėjo konteksto apie privalomus env kintamuosius ir realius API atsakymus; be to startiniai bėgimai krito.
- Suderinus env ir status kodų lūkesčius, papildomo konteksto prireikė minimaliai; paginacijos ir statistikos testai pridėti remiantis esamais endpointais.
- Sudėtingesni srautai (upload, eksportai) reikalautų daugiau endpointų žinių ir, tikėtina, daugiau iteracijų.

## Rekomendacijos / Kiti žingsniai
- Paleisti coverage įrankį (pvz., `dotnet test /p:CollectCoverage=true`) kiekybiškai įvertinti aprėptį.
- Pridėti integracinius testus sudėtingiems srautams (failų upload, Excel eksportas) ir mutacijų testavimą, kad patikrinti assertų stiprumą.
- Pasėti įvairesnių duomenų (dokumentai/katalogai), kad statistikos reikšmės būtų netik nulinės.
