# Memoria del progetto â€” MN_LayoutManager

> Questo Ã¨ il **diario di bordo** del progetto. Serve a ricostruire la storia
> anche dopo mesi. Ogni sessione aggiunge un blocco IN FONDO. **Non si cancella
> nÃ© si sovrascrive mai niente**: la memoria si accumula, si racconta.
>
> Ogni blocco dovrebbe dire, a parole semplici: cosa Ã¨ stato fatto, perchÃ©,
> quali decisioni importanti sono state prese, cosa resta da fare.

---

## 2026-07-21 â€” Creazione del progetto

- Progetto creato con lo script del toolkit.
- Software/versione di riferimento: **AutoCAD 2024 / .NET Framework 4.8 (x64)**
- Obiettivo del plugin: sostituire la barra delle schede dei layout di AutoCAD
  con una palette agganciabile piu' potente (rinomina, riordino, copia, stampa batch).
- Stato: appena iniziato. Struttura base e repository creati.

**Cosa resta da fare:**
- Realizzare il piano descritto in `MN_LayoutManager_Piano.md`.

---

<!-- I prossimi blocchi vanno aggiunti qui sotto, uno per sessione, senza
     cancellare quelli sopra. Formato suggerito: -->

<!--
## 2026-07-21 â€” <titolo breve della sessione>

**Cosa ho fatto:**
- ...

**Decisioni importanti (e perchÃ©):**
- ...

**Commit / release:**
- ...

**Cosa resta da fare:**
- ...
-->


## 2026-07-21 - Nascita del plugin: struttura, logica e palette scura

**Cosa ho fatto:**
- Creato il progetto vero e proprio a partire dal piano in `MN_LayoutManager_Piano.md`.
  Il progetto e' diviso in tre parti separate:
  - `src/MN_LayoutManager.Core` = la parte che "ragiona" (regole sui nomi dei layout,
    calcolo della rinomina multipla, calcolo del riordino, generazione dell'elenco fogli
    per la stampa). Non sa niente di AutoCAD, quindi si puo' testare a comando.
  - `src/MN_LayoutManager` = il plugin che parla con AutoCAD e la palette grafica.
  - `tests/MN_LayoutManager.Core.Tests` = 76 test automatici sulla parte che ragiona.
- Scritta la palette in WPF con tema scuro moderno (elenco layout, barra strumenti,
  menu col tasto destro, pannello rinomina multipla, riga di stato).
- Scritto il comando `GESTIONELAYOUT` che apre e chiude la palette.
- Aggiunto un registro degli errori su file, uno al giorno, in
  `%AppData%\MN_LayoutManager\logs\`.
- Aggiunto `scripts/Installa plugin.bat`: doppio click e il plugin viene compilato,
  testato e installato in AutoCAD senza dover digitare NETLOAD.

**Decisioni importanti (e perche'):**
- **WPF invece di WinForms** (il piano diceva WinForms): serve un aspetto scuro moderno
  coerente con gli altri strumenti. Costo: rinomina inline e trascinamento vanno scritti
  a mano invece di usare quelli gia' pronti di WinForms.
- **Progetto separato `.Core` senza AutoCAD**: e' la scelta che rende il progetto
  verificabile. Se la logica sta insieme alle chiamate AutoCAD, non si puo' testare
  niente senza aprire AutoCAD; tenendola separata, 76 test girano in mezzo secondo.
- **Comandi nativi al posto delle API fragili**: copia layout, stampa/pubblica e
  impostazioni di pagina passano dai comandi nativi di AutoCAD (LAYOUT Copy, -PUBLISH,
  PAGESETUP) pilotati via AutoLISP. Riusano logica gia' collaudata da Autodesk invece
  di reimplementarla. Effetto collaterale: sono asincroni, cioe' il risultato si vede
  quando AutoCAD esegue la coda, non subito.
- **AutoLISP e non testo di comando "a crudo"**: nella riga di comando lo spazio vale
  come Invio, quindi un layout chiamato "Tavola 1" verrebbe spezzato. Con AutoLISP il
  nome viaggia fra virgolette e resta intero.
- **Installazione come "bundle"** in ApplicationPlugins invece di NETLOAD ogni volta:
  AutoCAD carica il plugin da solo a ogni avvio.
- **Test anche sullo XAML**: un nome di colore scritto male non da' errore in
  compilazione ma fa crashare la palette all'apertura. Un test controlla che ogni
  stile richiamato esista davvero nel tema.

**Verificato con:**
- `dotnet build` su tutta la soluzione: 0 errori, 0 avvisi (gli avvisi sono configurati
  per diventare errori, quindi "0 avvisi" significa davvero pulito).
- `dotnet test`: 76 test, tutti passati.
- La compilazione conferma che tutte le API AutoCAD che il piano segnalava come
  "da verificare" (RenameLayout, DeleteLayout, eventi LayoutRemoved/LayoutCopied/
  LayoutRenamed) esistono davvero in AutoCAD 2024.

**Cosa resta da fare / da provare a mano dentro AutoCAD:**
- Prova completa in AutoCAD 2024 (i test automatici NON possono coprire questa parte).
- Da controllare in particolare: che il trascinamento aggiorni davvero le schede in
  basso; che i numeri `Type=` del file DSD producano il PDF/DWF giusto; che i tasti
  F2 / Canc / Ctrl+C / Ctrl+V arrivino alla palette quando e' agganciata.
- Installare `PSScriptAnalyzer` per l'analisi statica degli script PowerShell
  (al momento non e' installato sulla macchina).

---

## 2026-07-21 - Correzione del nome del progetto

**Cosa ho fatto:**
- Nel piano iniziale il progetto era stato chiamato `LayoutManagerPalette`, ma il nome
  giusto e' **`MN_LayoutManager`**. Rinominato tutto, senza lasciare tracce del vecchio nome:
  - cartelle `src/` e `tests/`, file `.sln` e `.csproj`;
  - namespace del codice e nome delle DLL prodotte;
  - riferimento interno dello XAML al file del tema (l'indirizzo `pack://...`);
  - cartella dei log, ora `%AppData%\MN_LayoutManager\logs\`;
  - nome del bundle di installazione, ora `MN_LayoutManager.bundle`;
  - file del piano, ora `MN_LayoutManager_Piano.md`.
- 103 sostituzioni in 38 file, piu' 8 rinomine di file e cartelle.

**Decisioni importanti (e perche'):**
- Il piano avvisava di non chiamare il progetto `LayoutManager`, perche' e' anche il nome
  di un tipo delle API AutoCAD e i due si confonderebbero. `MN_LayoutManager` e' un nome
  diverso, quindi il problema non si pone. In piu' nel codice le API AutoCAD sono
  richiamate con un soprannome esplicito (`AcadLayoutManager`), che rende impossibile
  l'ambiguita' anche in futuro.
- Le cartelle di compilazione vecchie sono state cancellate prima della rinomina, per non
  lasciare in giro DLL col nome sbagliato.
- I nomi delle singole classi (`LayoutPaletteView`, `LayoutPaletteHost`, ...) sono rimasti:
  descrivono cosa fa quel pezzo, non il nome del progetto.

**Verificato con:**
- Ricerca in tutti i file tracciati del vecchio nome, anche ignorando maiuscole/minuscole:
  nessuna occorrenza rimasta, ne' nei contenuti ne' nei nomi dei file.
- `dotnet build` su tutta la soluzione: 0 errori, 0 avvisi.
- `dotnet test`: 76 test, tutti passati.
- Compilazione in Release: produce `MN_LayoutManager.dll` e `MN_LayoutManager.Core.dll`.

**Cosa resta da fare:**
- Invariato rispetto al blocco precedente: la prova a mano dentro AutoCAD 2024.

---

## 2026-07-21 - Correzioni dopo la prima prova in AutoCAD

Dopo aver provato il plugin sono arrivate sei richieste di modifica. Ecco cosa e'
stato fatto e perche'.

**Cosa ho fatto:**
1. **Pubblicazione in background.** Prima AutoCAD restava bloccato durante la stampa.
   Ora la variabile BACKGROUNDPLOT viene messa a 2 (pubblicazione in secondo piano) e
   subito dopo rimessa al valore che aveva, per non cambiare le impostazioni personali.
2. **Un file separato per ogni layout**, non piu' un unico PDF multipagina. Il file
   prende il nome del layout: "Tavola 01" produce "Tavola 01.pdf". I caratteri che
   Windows non ammette nei nomi di file vengono sostituiti con un trattino.
3. **Cartella di destinazione** per stampa e pubblicazione: si scrive a mano o si sceglie
   con "Sfoglia...". Se non viene indicata, si propone la cartella del disegno. La
   cartella viene creata se non esiste.
4. **Trascinamento che non aggiornava le schede in basso.** Era un vero difetto:
   cambiare l'ordine delle schede modifica il disegno ma non genera nessun evento,
   quindi la barra in basso non se ne accorgeva e restava indietro finche' non si
   cambiava layout. Ora, dopo il riordino, si "riconferma" il layout corrente tramite
   il suo identificativo interno: questo fa aggiornare la barra senza cambiare cosa
   sta guardando l'utente.
5. **Impostazioni di pagina**: verificato ispezionando le API di AutoCAD 2024 che NON
   esiste nessun comando per aprire direttamente la finestra "Modifica impostazioni
   pagina". Su scelta dell'utente resta il gestore PAGESETUP (un clic in piu', ma
   nessun rischio di stampare per sbaglio come sarebbe con la finestra PLOT).
6. **Rimozione di prefisso e suffisso** nella rinomina multipla. Toglie il testo SOLO
   ai layout che iniziano (o finiscono) davvero cosi': gli altri non vengono toccati.

**Migliorie arrivate per strada:**
- **La copia di un layout ora e' immediata.** Ispezionando le API ho scoperto che
  esiste `LayoutManager.CopyLayout(nome, nuovoNome)`: prima si pilotava il comando
  nativo LAYOUT, che finiva in coda e faceva comparire la copia con un attimo di
  ritardo. Ora la copia appare subito e viene anche selezionata.
- **I due pannelli in fondo si possono chiudere** cliccando sul titolo, cosi' la
  palette non e' costretta a essere alta mezzo schermo.
- **Corretto un errore che avrebbe fatto fallire il riordino**: l'aggiornamento delle
  schede modifica il documento e va quindi eseguito dentro un blocco del documento,
  altrimenti AutoCAD segnala un errore di violazione del blocco.

**Decisioni importanti (e perche'):**
- Prima di scrivere codice ho **ispezionato le DLL di AutoCAD 2024** con la reflection,
  invece di andare a tentativi. E' cosi' che ho trovato `CopyLayout`, `SetCurrentLayoutId`
  e `UpdateScreen`, e che ho potuto rispondere con certezza sulla finestra delle
  impostazioni di pagina.
- La finestra "scegli cartella" arriva da Windows Forms perche' WPF su .NET Framework
  4.8 non ne ha una. E' isolata in un unico file (`UI/FolderPicker.cs`) cosi' non
  contamina il resto.

**Verificato con:**
- `dotnet build` su tutta la soluzione: 0 errori, 0 avvisi.
- `dotnet test`: 91 test (erano 76), tutti passati. I 15 nuovi coprono la rimozione di
  prefisso/suffisso, la cartella di destinazione, il file separato per layout e la
  pulizia dei caratteri vietati nei nomi di file.

**Cosa resta da verificare a mano dentro AutoCAD:**
- Che il trascinamento aggiorni davvero la barra delle schede (punto 4): e' la
  correzione che non posso provare in automatico.
- Che i PDF separati escano davvero, con il nome giusto, nella cartella indicata:
  i valori del file DSD (MULTISHEET=0, OUT=, Type=) sono quelli documentati ma
  vanno confermati sul campo.

---

## 2026-07-22 - Seconda tornata di correzioni: tastiera, spunte, numerazione

Dopo la seconda prova in AutoCAD sono arrivate nove segnalazioni. Prima di scrivere
codice ho ispezionato di nuovo le DLL di AutoCAD 2024 con la reflection, perche' due
dei problemi dipendevano da cosa le API mettono davvero a disposizione.

**Le due scoperte che hanno guidato il lavoro:**

1. **Non esiste nessun metodo per aggiornare la barra delle schede.** Ci sono solo
   gli eventi `LayoutsReordered` e `LayoutsRefresh`, che si ASCOLTANO: non c'e' modo
   di scatenarli. Ecco perche' la correzione della sessione scorsa non funzionava:
   rimetteva il layout corrente su se stesso, e AutoCAD, vedendo che l'identificativo
   non cambiava, non faceva assolutamente niente. Era un no-op.
2. **Esiste `Application.PreTranslateMessage`**, il punto in cui AutoCAD offre ai
   plugin di vedere un tasto prima di gestirlo. E' l'aggancio che serviva per Ctrl+A.

**Cosa ho fatto:**

1. **Barra delle schede che non si aggiornava dopo il trascinamento.** Ora si fa
   quello che l'utente faceva a mano per sbloccarla: si passa un istante su un altro
   layout e si torna subito su quello di prima. E' in un modulo a parte
   (`LayoutTabRefresher`) proprio perche' e' un rimedio, non una soluzione pulita:
   se un domani Autodesk esporra' un metodo vero, si cambia un file solo.
2. **Le scorciatoie che smettevano di funzionare** (Canc dopo "Seleziona tutti",
   Ctrl+V utilizzabile una volta sola). Avevano tutte la stessa causa: cliccare un
   bottone toglieva il fuoco all'elenco, e le scorciatoie sono agganciate all'elenco.
   Ora i bottoni della palette non prendono il fuoco (`Focusable="False"`), quindi
   l'elenco lo mantiene e i tasti continuano ad arrivare.
3. **Ctrl+A finiva nel disegno** invece che nella palette: e' una scorciatoia che
   AutoCAD intercetta nel proprio ciclo dei messaggi, prima che l'interfaccia possa
   vederla. Il nuovo `PaletteShortcutInterceptor` la recupera. Non contiene l'elenco
   delle scorciatoie: le legge da quelle gia' dichiarate nell'interfaccia, cosi'
   restano scritte in un posto solo.
4. **Caselle di spunta accanto ai nomi.** Selezione ed elenco da rinominare erano la
   stessa cosa e si pestavano i piedi. Ora sono separate: la riga evidenziata comanda
   attiva/copia/elimina/stampa, la spunta dice solo quali layout rinominare in blocco.
   Se non spunti niente, "Applica" resta spento: meglio non fare niente che rinominare
   tutto per sbaglio.
5. **Filtro "Contiene" eliminato** dalla rinomina multipla: obbligava a inventare un
   testo comune ed era scomodo. Le spunte fanno la stessa cosa in modo diretto.
6. **Stampa e pubblica riuniti** nella tendina "Stampa e pubblicazione", con i ruoli
   separati: "Stampa" manda UN foglio al plotter, "Pubblica" produce i file in blocco
   (selezionati o tutti). La barra in alto ora ha solo "Nuovo layout" e "Duplica...".
7. **"Seleziona tutti" spostato**: come bottone e' diventato "Spunta tutti"/"Nessuno"
   dentro il pannello rinomina, dove serve. Come comando resta su Ctrl+A e nel menu
   col tasto destro.
8. **Progressione numerica** (la novita' piu' grossa). Il plugin riconosce le serie
   nei nomi e prosegue il conteggio: da `D_T_01` copiato tre volte escono `D_T_02`,
   `D_T_03`, `D_T_04`, con gli zeri davanti mantenuti. Vale per Ctrl+V ripetuto, per
   il nuovo bottone "Duplica..." (che chiede quante copie e le mostra in anteprima) e
   per la creazione di un layout nuovo, dove il nome successivo viene **proposto**
   nella casella di rinomina e confermato con Invio, mai imposto.

**Decisioni importanti (e perche'):**

- **La progressione sta tutta in `.Core`** (`LayoutNumbering`, `NumberedLayoutName`),
  cioe' nella parte che non conosce AutoCAD. E' il motivo per cui 25 test nuovi la
  verificano in un decimo di secondo invece che a mano dentro AutoCAD.
- **Serve almeno di 2 layout per parlare di "serie".** Con un solo nome numerato non
  si sa se e' una progressione o un caso, e proporre un numero sarebbe indovinare.
- **Si riparte sempre dal numero piu' alto della serie**, non da quello del layout
  copiato: copiando `D_T_01` quando esiste gia' `D_T_05`, il nome giusto e' `D_T_06`.
  Altrimenti si litigherebbe di continuo con i nomi occupati.
- **Tetto di 100 copie per volta**: un errore di battitura ("500" invece di "5")
  creerebbe centinaia di layout, con una lunga attesa e nessun modo comodo di tornare
  indietro.
- **La finestra "Duplica" non sa niente di layout**: riceve gia' pronta la funzione
  che calcola l'anteprima dei nomi. Cosi' la regola dei nomi resta una sola.
- **La finestra non usa la cornice di Windows**, altrimenti avrebbe la barra del
  titolo chiara sopra un contenuto scuro.

**Verificato con:**
- `dotnet build -t:Rebuild` (compilazione da zero): 0 errori, 0 avvisi.
- `dotnet test`: 116 test, tutti passati (erano 91). I 25 nuovi coprono la
  progressione numerica e la rinomina sui soli layout spuntati.
- Il test che controlla gli stili dell'interfaccia ora guarda TUTTI i file di
  interfaccia, non solo la palette, e trova le finestre da solo. **L'ho verificato
  rompendo apposta uno stile nella finestra nuova**: il test lo ha segnalato. Poi ho
  rimesso a posto. Un test che non puo' fallire non serve a niente.

**Cosa resta da provare a mano dentro AutoCAD (importante):**
- **Il punto 1 e' quello a rischio.** Che passare su un altro layout e tornare
  indietro basti a far ridisegnare la barra delle schede e' ragionevole (e' quello che
  succede quando lo fai tu a mano), ma NON ho potuto provarlo: senza AutoCAD aperto non
  c'e' modo. Se la barra non si aggiorna ancora, il piano B e' rimandare il ritorno al
  layout di partenza al momento libero successivo di AutoCAD, invece che subito.
- Secondo effetto collaterale da valutare: il passaggio momentaneo su un altro layout
  provoca una rigenerazione. Su disegni molto pesanti potrebbe farsi sentire.
- Ctrl+A dentro la palette: deve selezionare i layout e NON gli oggetti del disegno.
- Ctrl+V ripetuto e "Duplica...": che i nomi escano davvero numerati in ordine.

---

## 22/07/2026 — Pacchetto ZIP per provare il plugin su un altro PC

**Il problema.** Fino a ora il plugin si installava solo con `scripts\Deploy.ps1`,
che pero' compila: richiede il codice sorgente e il .NET SDK. Per provarlo su un
altro computer serviva qualcosa di piu' semplice.

**Cosa ho fatto.**
- Nuovo script `scripts\CreaPacchetto.ps1`: esegue i test, compila in Release e
  produce in `dist\` un file ZIP con dentro **solo il plugin gia' compilato**
  (le due DLL + `PackageContents.xml`), un installatore da doppio click e un
  `LEGGIMI.txt`. Nessun codice sorgente, nessun file `.pdb`.
- Nuova cartella `scripts\pacchetto\`: contiene i file che finiscono dentro lo
  ZIP (`Installa.ps1`, i due `.bat`, il modello di `LEGGIMI.txt`). Tenerli come
  file veri e non come testo incollato dentro lo script rende piu' facile
  correggerli.
- Nuovo file `scripts\Comune.ps1` con le costanti condivise (anno di AutoCAD,
  nomi delle DLL, nome del comando) e la funzione che genera il
  `PackageContents.xml`.

**Decisioni e perche'.**
- **`Comune.ps1` esiste per non scrivere due volte la stessa cosa.** Il
  `PackageContents.xml` (la "carta d'identita'" che dice ad AutoCAD quale DLL
  caricare) prima stava dentro `Deploy.ps1`; adesso sta in un posto solo e lo
  usano sia `Deploy.ps1` sia `CreaPacchetto.ps1`. Se un domani si passa ad
  AutoCAD 2025 si cambia una riga sola invece di due file.
- **L'installatore del pacchetto esegue `Unblock-File`.** Windows marca come
  "bloccati" i file arrivati da internet o da una chiavetta: se non si toglie
  quel marchio, .NET si rifiuta di caricare la DLL e il plugin non parte senza
  dare spiegazioni. E' il classico problema che fa perdere un'ora.
- **Lo ZIP resta legato ad AutoCAD 2024** (`SeriesMin`/`SeriesMax` = R24.3): se
  sull'altro PC c'e' una versione diversa, il plugin non si carica. Va sistemato
  solo se serve davvero.

**Verificato con:**
- 116 test automatici, tutti passati.
- Compilazione Release: 0 errori, 0 avvisi.
- Controllo di sintassi di tutti gli script PowerShell: nessun errore.
- **Prova vera del pacchetto:** ho estratto lo ZIP prodotto in una cartella
  temporanea e lanciato il suo installatore. Ha installato correttamente in
  `%AppData%\Autodesk\ApplicationPlugins\`, e il `PackageContents.xml` risultante
  e' identico a quello prodotto da `Deploy.ps1`.
- Ho poi rilanciato `Deploy.ps1` per controllare che il refactor non l'avesse
  rotto: funziona come prima.

**Cosa resta da provare a mano:**
- Aprire AutoCAD 2024 sull'ALTRO PC dopo l'installazione e verificare che il
  comando `GESTIONELAYOUT` apra la palette. E' l'unica prova che non si puo'
  fare in automatico.

---

## 22/07/2026 - Il plugin ora funziona da AutoCAD 2024 a 2027

**Il problema.** Provato il plugin su AutoCAD 2026 in ufficio: installato nella
cartella giusta, ma AutoCAD non lo vedeva proprio. Nessun errore, nessun
messaggio: semplicemente il comando non esisteva.

**La causa vera (piu' grave di quanto sembrasse).** Non era solo il numero di
versione scritto nel `PackageContents.xml`. Dal 2025 **AutoCAD ha cambiato
motore .NET**, e una versione di AutoCAD carica solo plugin compilati per il
proprio motore. In pratica il plugin era proprio incompatibile, non solo
"non dichiarato".

| AutoCAD | Sigla | Motore .NET |
|---|---|---|
| 2024 | R24.3 | .NET Framework 4.8 |
| 2025 | R25.0 | .NET 8 |
| 2026 | R25.1 | .NET 8 |
| 2027 | R26.0 | .NET 10 |

**Cosa ho fatto.**
- Lo stesso codice viene ora **compilato tre volte** (una per motore) e il bundle
  le contiene tutte e tre in cartelle separate. Si installa una volta sola: ogni
  AutoCAD legge il `PackageContents.xml`, riconosce la propria sigla e carica solo
  la cartella sua.
- I riferimenti alle API AutoCAD non vengono piu' letti dall'AutoCAD installato
  sul PC, ma dai **pacchetti NuGet ufficiali di Autodesk**.
- Anche i test girano tre volte, uno per motore: 123 test x 3.
- Trovato e corretto **un bug serio che sarebbe emerso solo in ufficio**
  (vedi sotto).
- Versione del plugin portata a **2.0.0**: cambia il pacchetto, non solo il codice.

**Il bug nascosto nella pubblicazione (la parte piu' importante).**
Nel codice che scrive il file DSD (l'elenco fogli che AutoCAD legge per
pubblicare) c'era `Encoding.Default`, con accanto un commento che diceva
"NON usare UTF-8, romperebbe i nomi con lettere accentate". Il problema e' che
**`Encoding.Default` ha cambiato significato**: su .NET Framework 4.8 vuol dire
"codifica di Windows", su .NET 8 e 10 vuol dire proprio UTF-8. Cioe' lo stesso
codice, ricompilato per AutoCAD 2026, avrebbe fatto esattamente il danno che il
commento diceva di voler evitare - senza dare nessun errore.

L'ho verificato sul serio, non per ragionamento: su .NET 10 la parola "Città"
con `Encoding.Default` diventa `43-69-74-74-C3-A0` (due byte per la "à"), mentre
AutoCAD ne aspetta uno solo (`E0`). Un layout chiamato "Tavola Città" sarebbe
uscito storpiato.

Ora la codifica viene scelta in modo esplicito da un modulo nuovo,
`SystemEncoding`, messo dentro `.Core` proprio per poterlo testare: 7 test nuovi
lo verificano su tutti e tre i motori.

**Decisioni importanti (e perche').**
- **API AutoCAD da NuGet invece che dall'AutoCAD installato.** Su questo PC c'e'
  solo AutoCAD 2024: senza NuGet non ci sarebbe modo di compilare per il 2026 e
  il 2027. In piu' sparisce il percorso fisso `C:\Program Files\Autodesk\...`
  scritto nel progetto, che si sarebbe rotto su qualsiasi altro computer.
- **La versione per .NET 8 e' compilata contro le API di AutoCAD 2025, non 2026,**
  pur dovendo funzionare su entrambi. Il motivo: .NET accetta di caricare una
  libreria piu' recente di quella attesa, ma non una piu' vecchia. Puntando alla
  piu' vecchia del gruppo, la stessa DLL va bene per tutte e due.
- **`SeriesMax` va sempre indicato** in ogni blocco: senza, un futuro AutoCAD 2028
  proverebbe a caricare la DLL del 2027 e crasherebbe invece di ignorarla.
- **Una tabella sola per le versioni** (`$Targets` in `scripts\Comune.ps1`).
  Per aggiungere AutoCAD 2028 domani si tocca una riga li' e una nel `.csproj`.
- **`Deploy.ps1` e `CreaPacchetto.ps1` ora usano la stessa funzione** per montare
  il bundle: prima duplicavano la stessa logica e potevano divergere.
- **Attenzione, ora serve il .NET SDK 10** per compilare (e' l'unico che sa
  produrre la versione per il 2027). Per usare il plugin non serve niente.

**Verificato con:**
- `dotnet build` su tutta la soluzione: 0 errori, **0 avvisi** su tutti e tre i
  motori. Il codice delle API AutoCAD ha compilato senza modifiche anche contro
  le API del 2027: nessuna funzione usata e' stata tolta da Autodesk.
- `dotnet test`: **123 test x 3 motori, tutti passati** (erano 116 su uno solo).
- Prova vera del pacchetto: creato lo ZIP, estratto e ispezionato. Ho controllato
  file per file che dentro ogni DLL ci fosse davvero il motore giusto
  (net48 -> .NETFramework 4.8, net8.0 -> .NETCoreApp 8.0, net10.0 -> .NETCoreApp
  10.0) e che ognuna puntasse alla versione giusta delle API AutoCAD
  (24.3, 25.0, 26.0).
- `PackageContents.xml` generato: controllato che sia XML valido e che i tre
  blocchi coprano le sigle giuste senza sovrapporsi.
- Rilanciato `Deploy.ps1`: installa correttamente tutte e tre le versioni.
- Controllo di sintassi di tutti gli script PowerShell: nessun errore.

**Cosa resta da provare a mano (importante):**
- **Aprire AutoCAD 2026 in ufficio e digitare `GESTIONELAYOUT`.** E' la prova che
  conta: tutto il resto e' verificato, ma che AutoCAD 2026 carichi davvero il
  bundle si vede solo li'.
- **Pubblicare un layout con lettere accentate nel nome** (es. "Tavola Città") da
  AutoCAD 2026, e controllare che il PDF esca col nome giusto. E' la verifica sul
  campo della correzione della codifica.
- Le prove rimaste in sospeso dalle sessioni precedenti (barra delle schede dopo
  il trascinamento, Ctrl+A, numerazione con Ctrl+V) valgono ancora, e ora vanno
  ripetute anche sul 2026 perche' e' un motore diverso.
- AutoCAD 2027 non e' mai stato provato: la compilazione c'e' ed e' corretta, ma
  nessuno l'ha ancora aperto.
- `PSScriptAnalyzer` resta non installato su questa macchina (in sospeso dalla
  prima sessione).

---
