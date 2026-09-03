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

## 22/07/2026 - Rifiniture: diagnostica all'avvio e pacchetto provato davvero

Seguito diretto del blocco precedente. Prima di consegnare il pacchetto ho
verificato le cose che avevo dato per buone e aggiunto quello che serve per
capire cosa succede in ufficio, dove io non posso guardare.

**Cosa ho fatto.**
- **Verificate le sigle di versione** invece di fidarmi del ragionamento.
  Confermato da fonti Autodesk: **R25.1 = AutoCAD 2026** e **R26.0 = AutoCAD 2027**
  (per il 2027 la chiave passa da R25.1 a R26.0 ed e' richiesto .NET 10). Era il
  punto piu' rischioso di tutto il lavoro: se avessi sbagliato quella sigla, il
  plugin sarebbe rimasto invisibile su AutoCAD 2026 esattamente come prima.
- **Aggiunta una diagnostica all'avvio** (`Infrastructure\BuildInfo.cs`). Nel log,
  come prima riga di ogni avvio, ora c'e' scritto quale delle tre versioni e'
  stata caricata, su che motore .NET gira e che versione di AutoCAD c'e' sotto.
- **Verificato che l'articolo Autodesk sul 2027** non chiedesse altro: dice di
  dichiarare `Microsoft.WindowsDesktop.App`, ma ho controllato la DLL compilata e
  c'e' gia', aggiunto in automatico da `UseWPF`. Nessuna modifica necessaria.
- **Controllato che nel codice non ci sia niente di non portabile** (COM,
  `dynamic`, interop): non c'e' nulla, il codice e' pulito da questo punto di vista.
- **Versione del plugin ora scritta in un posto solo** (`Directory.Build.props`).
  Prima la DLL diceva 1.0.0 e il pacchetto 2.0.0: due verita' diverse. Ora gli
  script leggono la versione da li'.

**Perche' la diagnostica e' la cosa piu' utile di questo blocco.**
Quando AutoCAD non trova una versione del plugin adatta al proprio motore non
dice niente: il comando semplicemente non esiste, nessun errore da nessuna parte.
Ora la situazione si legge cosi':
- **il log c'e'** -> il plugin e' stato caricato, e la riga dice quale versione e
  su quale AutoCAD. Il problema e' altrove.
- **il log non c'e' proprio** -> AutoCAD non ha caricato niente: la sua sigla non
  rientra in nessun blocco del `PackageContents.xml`.

**Decisioni importanti (e perche').**
- **Ho lasciato gli intervalli di versione stretti** (R24.3-R24.3, R25.0-R25.1,
  R26.0-R26.0) invece di allargarli "per sicurezza". Autodesk avverte
  esplicitamente di indicare sempre `SeriesMax`: un intervallo largo farebbe
  caricare a un futuro AutoCAD una DLL fatta per un motore che non usa piu', e il
  risultato sarebbe un crash invece di un silenzio. Meglio non comparire che
  far crashare AutoCAD.
- **La diagnostica passa da `AcadContext.TryRun`.** Gira durante il caricamento
  del plugin: se fallisse senza protezione, il plugin non si caricherebbe per
  colpa di un messaggio informativo. Il rimedio sarebbe peggiore del male.

**Verificato con:**
- Ricompilazione da zero: 0 errori, 0 avvisi su tutti e tre i motori.
- 123 test x 3 motori, tutti passati.
- **Controllato dentro le tre DLL** che la costante di identificazione sia
  davvero diversa in ognuna (net48 -> "AutoCAD 2024 (.NET Framework 4.8)",
  net8.0 -> "AutoCAD 2025-2026 (.NET 8)", net10.0 -> "AutoCAD 2027 (.NET 10)").
  Era la cosa che poteva sbagliarsi in silenzio.
- Verificato che tutte e tre le DLL dichiarino ora la versione 2.0.0.
- **Prova vera del pacchetto, dall'inizio alla fine**: creato lo ZIP, estratto in
  una cartella pulita e **lanciato l'installatore che sta dentro lo ZIP**, cioe'
  esattamente quello che si fara' in ufficio. Installazione riuscita, tutte e tre
  le cartelle al loro posto, nessun file rimasto "bloccato" da Windows.

**Cosa resta da provare a mano:**
- Invariato rispetto al blocco precedente. Il pacchetto e' pronto in `dist\`:
  `MN_LayoutManager_v2.0.0_AutoCAD2024-2027_<data>.zip`.
- La prova che conta resta aprire AutoCAD 2026 in ufficio. Se il comando non
  viene riconosciuto, la prima cosa da guardare e' se esiste il file di log:
  ora quella singola informazione dice da che parte sta il problema.

---

## 09/08/2026 - Stampa e pubblica: perche' non usciva mai nessun file

**Il problema segnalato.** La palette faceva fare tutto il giro (scegli i layout,
scegli la cartella, conferma) e diceva "pubblicazione avviata", ma nella cartella
non compariva niente. Nessun errore, nessun messaggio: i comandi andavano a vuoto.

**La causa: quattro numeri sbagliati.** Nel file DSD - l'elenco di fogli che
AutoCAD legge per pubblicare - c'e' un campo `Type=` che dice cosa produrre.
Il plugin ci scriveva dei numeri presi per buoni, mai verificati. Li ho letti
per riflessione dalle DLL ufficiali di Autodesk (l'enumerazione `SheetType` in
`AcCoreMgd.dll`) ed **erano sbagliati tutti e quattro**:

| Quello che l'utente chiedeva | Numero scritto | Cosa significa DAVVERO quel numero |
|---|---|---|
| Stampa sul plotter | 0 | crea un file DWF |
| DWF | 1 | un unico DWF multipagina |
| DWFx | 2 | manda al plotter |
| **PDF** | **6** | **un unico PDF multipagina** |

Il caso peggiore e' proprio il PDF, quello che si usa sempre: il plugin chiedeva
ad AutoCAD un unico documento multipagina (`Type=6`) e contemporaneamente, con
`MULTISHEET=0`, un file separato per foglio. Due richieste in contraddizione.
AutoCAD non produceva niente. E siccome la pubblicazione era stata impostata per
girare in secondo piano, il fallimento restava confinato a un fumetto in basso a
destra facilissimo da non vedere.

**I numeri giusti** (verificati identici su AutoCAD 2024, 2026 e 2027):
stampa su plotter = 2, DWF = 0, DWFx = 3, PDF = 5.

**Perche' nessuno se n'era accorto prima (la parte da ricordare).**
C'era gia' un test su quel campo. Ma il test si limitava a ricopiare gli stessi
numeri che stavano nel codice: `Assert.Contains("Type=6")`. Un test scritto cosi'
non puo' fallire mai, perche' non confronta il codice con la realta' - confronta
il codice con se stesso. Era una rete di sicurezza finta.

**Cosa ho fatto.**
1. **Numeri corretti e messi in un posto solo**, il nuovo modulo
   `Core\PublishSheetType.cs`, con scritto in chiaro da dove vengono.
2. **Buttata la strada della riga di comando.** Prima il plugin lanciava il
   comando `-PUBLISH` scrivendo un'istruzione AutoLISP. Quella strada non
   restituisce nessun esito: qualunque cosa succeda, il plugin scrive "avviata"
   e non sa altro. Ora si chiama l'API vera di AutoCAD
   (`Publisher.PublishExecute`), che invece riporta gli errori.
3. **Il plugin adesso dice com'e' andata.** Il nuovo `PublishOutcomeListener`
   ascolta gli eventi di pubblicazione e scrive nel log quanti fogli sono usciti,
   oppure che il lavoro e' fallito. In piu' viene attivato il registro CSV di
   AutoCAD, accanto ai log del plugin.
4. **Test rifatti in modo che possano davvero fallire** (vedi sotto).
5. Versione portata a **2.0.1**.

**Decisioni importanti (e perche').**
- **I numeri non si scrivono piu' a mano nel testo del DSD e basta**: al momento
  di pubblicare, il tipo viene rimesso a partire dall'enumerazione vera di
  AutoCAD. Cosi' il formato non dipende da come e' stato scritto il file di testo.
- **C'e' un controllo automatico che i numeri non siano cambiati.** Se un domani
  Autodesk li modificasse, la differenza finisce nel log invece di produrre di
  nuovo file sbagliati in silenzio. E' la lezione di questo bug: non basta avere
  il numero giusto, serve accorgersi quando smette di esserlo.
- **La pubblicazione resta in secondo piano**, come chiesto nella sessione del
  21/07: non ho rimesso AutoCAD a bloccarsi. Ora pero' il fallimento in secondo
  piano lascia una traccia scritta.
- **L'errore viene fermato dentro il modulo.** La pubblicazione parte in un
  momento successivo al clic: un errore lasciato libero li' non avrebbe piu'
  nessuno a raccoglierlo e chiuderebbe AutoCAD. Passa da `AcadContext.TryRun`.
- **Tutto cio' che tocca AutoCAD sta in `AcadPublisher.cs`**, un file solo: la
  parte rischiosa e' confinata, se serve cambiare strada si tocca solo quello.

**Verificato con:**
- Ricompilazione da zero in Release: 0 errori, **0 avvisi** su tutti e tre i motori.
- `dotnet test`: **128 test x 3 motori, tutti passati** (erano 123).
- **Ho verificato che i test nuovi possano davvero fallire**: ho rimesso apposta
  nel codice i quattro numeri sbagliati e ho rilanciato i test. **6 test sono
  diventati rossi.** Poi ho rimesso i valori corretti e sono tornati verdi.
  Era il controllo piu' importante di tutta la sessione: dimostra che se il bug
  tornasse, adesso qualcuno se ne accorge.
- I valori dell'enumerazione `SheetType` letti dai metadati delle DLL di AutoCAD
  2024, 2025/2026 e 2027: identici su tutte e tre.
- Controllo che in tutto il progetto non sia rimasto nessun riferimento alla
  vecchia strada (`PublishFromDsd`, `-PUBLISH`).

**Cosa resta da provare a mano dentro AutoCAD (importante):**
- **Pubblicare qualche layout in PDF e controllare che i file escano davvero**,
  uno per layout, con il nome del layout, nella cartella scelta. E' la prova che
  conta: il difetto e' stato trovato e corretto sulla carta, ma la conferma sul
  campo non c'e' ancora.
- Provare anche **"Stampa"** sul plotter: prima quel comando chiedeva ad AutoCAD
  un file DWF invece di stampare, quindi non ha mai fatto quello che prometteva.
- Se ancora non uscisse niente, **adesso il log dice perche'**: guardare
  `%AppData%\MN_LayoutManager\logs\`, sia il log del giorno sia il nuovo file
  `pubblicazione_<data>.csv` scritto da AutoCAD.
- Restano in sospeso le prove delle sessioni precedenti (AutoCAD 2026 in ufficio,
  barra delle schede dopo il trascinamento, Ctrl+A, numerazione con Ctrl+V) e
  `PSScriptAnalyzer` ancora non installato.
- Il pacchetto ZIP in `dist\` e' ancora la 2.0.0, cioe' senza questa correzione:
  va rigenerato con `scripts\CreaPacchetto.ps1` prima di riprovare in ufficio.

---

## 01/09/2026 - "Il comando non esiste": non era un bug, era il nome

**Il problema segnalato.** Plugin caricato in AutoCAD, digitato `LAYOUTMANAGER`,
nessun comando e nessuna palette.

**La causa.** Il comando non si e' mai chiamato `LAYOUTMANAGER`: si chiama
`GESTIONELAYOUT`. `LAYOUTMANAGER` e' il nome del PROGETTO. Non c'era niente di
rotto: il plugin funzionava, era il nome a non corrispondere.

**Come l'ho stabilito senza aprire AutoCAD.** Tre controlli, in ordine:
1. il bundle e' installato in `%AppData%\Autodesk\ApplicationPlugins\`;
2. **esiste il log del giorno stesso** - quindi AutoCAD il plugin lo aveva
   caricato eccome. La riga diceva: `v2.0.1 | compilato per: AutoCAD 2024
   (.NET Framework 4.8) | AutoCAD: 24.3s`, cioe' la variante giusta sul motore
   giusto, nessun errore;
3. nessun errore nel log.
   Tutto puntava altrove: non all'installazione, ma al nome digitato.

   **Nota per il futuro: la diagnostica all'avvio aggiunta il 22/07 ha fatto
   esattamente il lavoro per cui era stata scritta.** La presenza o assenza di
   quel file di log ha risolto la diagnosi in due minuti invece che a tentativi.

**Cosa ho fatto.** Aggiunto `LAYOUTMANAGER` come **secondo nome dello stesso
comando** (`PaletteCommandAlias` in `Commands.cs`). `GESTIONELAYOUT` continua a
funzionare: sono due porte sulla stessa stanza, non due comandi. Aggiornati anche
i punti dove il nome viene comunicato all'utente: messaggi di `Deploy.ps1` e
dell'installatore del pacchetto, `LEGGIMI.txt` e `README.md`.

**Decisioni importanti (e perche').**
- **Due nomi invece di rinominare.** Rinominare avrebbe rotto l'abitudine di chi
  gia' usa `GESTIONELAYOUT`; tenere solo il nome italiano avrebbe lasciato in
  piedi la trappola. Con due nomi nessuno dei due casi si presenta piu'.
- **Ho verificato invece di assumere, su due punti che potevano far fallire tutto
  in silenzio:**
  - che le API AutoCAD accettino due `[CommandMethod]` sulla stessa funzione:
    letto per riflessione da `AcCoreMgd.dll`, `AttributeUsage(AllowMultiple =
    True)`. Confermato;
  - che `LAYOUTMANAGER` non fosse gia' un comando di AutoCAD 2024: cercato dentro
    `acad.exe`. **Il primo tentativo di ricerca era sbagliato** (cercava in ASCII e
    non trovava nemmeno `LAYOUTWIZARD`, che esiste di sicuro). Ripetuto in UTF-16:
    `LAYOUTWIZARD` compare, `LAYOUTMANAGER` no. Nome libero.
  Il "controllo di controllo" - cercare qualcosa che DEVE esserci per capire se il
  metodo di ricerca funziona - e' cio' che ha evitato una risposta falsa.
- **L'alias sta in `Comune.ps1`**, dove stanno gia' le altre costanti condivise, ed
  e' quindi automaticamente disponibile anche all'installatore dentro lo ZIP.
- **Nessun test automatico aggiunto, di proposito.** Un test che legge `Commands.cs`
  e verifica che ci sia scritto "LAYOUTMANAGER" ricopierebbe il codice invece di
  controllarlo: e' esattamente la "rete di sicurezza finta" che il 09/08 aveva
  lasciato passare il bug dei numeri del DSD. Qui la verifica vera e' un'altra ed e'
  stata fatta: **letto dentro le tre DLL compilate** che entrambi i nomi ci siano.

**Verificato con:**
- `dotnet build -c Release`: 0 errori, **0 avvisi** su net48, net8, net10. Il fatto
  stesso che compili conferma `AllowMultiple` anche su .NET 8 e .NET 10, dove la
  riflessione non era riuscita a leggerlo.
- `dotnet test`: **128 test x 3 motori, tutti passati**.
- Controllo di sintassi dei 4 script PowerShell: 0 errori.
- **Letti i due nomi dentro tutte e tre le DLL compilate** e poi di nuovo **dentro
  la DLL effettivamente installata** in `ApplicationPlugins`, non solo in quella di
  compilazione: sono due cose diverse e la seconda e' quella che AutoCAD carica.
- `Deploy.ps1` rilanciato: installazione riuscita, tutte e tre le varianti.

**Un problema vero trovato per strada e NON corretto (scelta dell'utente).**
All'avvio il plugin dovrebbe scrivere nella riga di comando "plugin caricato,
digita GESTIONELAYOUT". Quel messaggio **non compare mai**: in
`AcadContext.WriteMessage` c'e' un controllo che esce in silenzio se non c'e' un
disegno attivo, e quando AutoCAD carica un plugin dal bundle lo fa all'avvio,
prima che il disegno sia pronto. Il messaggio che avrebbe evitato tutto questo
viene quindi buttato via proprio quando servirebbe. La correzione (rimandarlo al
primo momento libero di AutoCAD, evento `Idle`) e' stata proposta e l'utente ha
preferito rimandarla. **Resta in sospeso.**

**Cosa resta da fare:**
- **Aprire AutoCAD e digitare `LAYOUTMANAGER`** (o `GESTIONELAYOUT`): e' la prova
  che conta, tutto il resto e' verificato ma nessuno ha ancora visto la palette.
- Il messaggio d'avvio invisibile, qui sopra.
- Il pacchetto ZIP in `dist\` non contiene ancora l'alias: da rigenerare con
  `scripts\CreaPacchetto.ps1` prima di riportarlo in ufficio.
- Restano in sospeso, invariate, tutte le prove sul campo delle sessioni
  precedenti: pubblicazione PDF (la correzione del 09/08 non e' mai stata
  confermata), "Stampa" sul plotter, AutoCAD 2026 in ufficio, barra delle schede
  dopo il trascinamento, Ctrl+A, numerazione con Ctrl+V, AutoCAD 2027 mai aperto,
  `PSScriptAnalyzer` non installato.

---

## 01/09/2026 (seconda parte) - Perche' i PDF non uscivano: mancava la stampante

**Prove sul campo passate.** Prima di tutto, tre verifiche in sospeso da luglio sono
state fatte in AutoCAD e sono andate BENE:
- **Ctrl+A** dentro la palette seleziona i layout (non gli oggetti del disegno);
- **Duplica...** e la numerazione progressiva funzionano;
- **il trascinamento aggiorna davvero la barra delle schede in basso.** Era la
  correzione piu' a rischio di tutte, un rimedio artigianale di cui non c'era
  nessuna certezza. Funziona.

**Il problema rimasto.** La pubblicazione in PDF non produceva nessun file.

**La causa, letta nel log e non indovinata.**
```
[ERROR] [Stampa/Pubblica] Errore di AutoCAD (NullPtr): eNullPtr
   in Autodesk.AutoCAD.PlottingServices.PlotConfigManager.get_CurrentConfig()
```
Il plugin chiedeva ad AutoCAD "dammi la configurazione di stampa corrente"
(`PlotConfigManager.CurrentConfig`). Ma quella e' una domanda che ha senso solo se
qualcuno, in quella sessione, ha gia' scelto una stampante. Aprendo la palette e
premendo "Pubblica" non lo aveva fatto nessuno: **AutoCAD rispondeva con il vuoto e
l'operazione moriva prima ancora di cominciare**, senza mai arrivare a stampare.

Ecco perche' il bug era sopravvissuto alla correzione del 09/08: quella aveva
sistemato i numeri del formato dentro il file DSD, che erano davvero sbagliati, ma
il codice non arrivava mai al punto di usarli.

**Cosa ho fatto.** La soluzione non e' *chiedere* qual e' la stampante corrente, e'
**impostarla**. Due moduli nuovi, separati apposta:
- **`Core\PlotDeviceNames.cs`** - solo dati: quali stampanti di AutoCAD servono per
  ogni formato (PDF, DWF, DWFx), in ordine di preferenza. Non conosce AutoCAD,
  quindi si testa a comando.
- **`Services\PlotConfigResolver.cs`** - la parte che parla con AutoCAD: aggiorna
  l'elenco delle stampanti, prova i nomi uno per uno con
  `PlotConfigManager.SetCurrentConfig` e tiene il primo che AutoCAD accetta.

Aggiunta anche una variante di `PluginLog.Error` senza eccezione: "manca una
stampante" e' un errore vero anche se non c'e' nessun errore tecnico dietro.

Versione portata a **2.0.2**.

**Decisioni importanti (e perche').**
- **Un elenco di stampanti, non una sola.** Scrivere solo "DWG To PDF.pc3" avrebbe
  funzionato su questo PC e sarebbe potuto fallire su un altro. Ora, se quella manca,
  si passa alle altre stampanti PDF di AutoCAD.
- **Per "Stampa" (plotter) il plugin NON impone nessuna stampante.** Ogni foglio deve
  andare al dispositivo scritto nelle SUE impostazioni di pagina: imporne una sola a
  tutti sarebbe peggio del problema. Si passa il dispositivo del layout corrente come
  valore di partenza, e la stampante di sistema come ultimo ripiego.
- **Il valore "None" e' trattato come "nessuna stampante".** E' cio' che AutoCAD
  scrive quando il layout non ne ha una: passarlo come se fosse un nome vero
  farebbe fallire la stampa.
- **Se non si trova nessuna stampante, il plugin non solleva un'eccezione: si ferma e
  spiega.** Nel log finisce cosa e' stato provato E l'elenco di cio' che AutoCAD
  dichiara di avere. Se ricapita, il log basta a capire il perche'.
- **Il PlotConfig non viene chiuso dopo l'uso**, di proposito: appartiene ad AutoCAD
  ed e' ancora in uso dalla pubblicazione in secondo piano. Chiuderlo la farebbe
  fallire.
- **Verificato per riflessione, non a memoria**, che `SetCurrentConfig` esista
  davvero. **Al primo tentativo la riflessione non lo mostrava**: falliva a caricare
  una dipendenza e restituiva un elenco monco che sembrava completo. Aggiunto il
  caricamento delle dipendenze, il metodo e' comparso. Un elenco incompleto che non
  si annuncia come tale e' piu' pericoloso di un errore.

**Verificato con:**
- `dotnet build -c Release`: 0 errori, **0 avvisi** su net48, net8, net10.
- `dotnet test`: **143 test x 3 motori, tutti passati** (erano 128). I 15 nuovi
  coprono la scelta della stampante.
- **Ho verificato che i test nuovi possano davvero fallire.** Ho introdotto 4 guasti
  apposta (nome di stampante senza ".pc3", stampante imposta anche alla stampa su
  plotter, doppione nell'elenco, "None" trattato come stampante vera): **6 test sono
  diventati rossi**, uno per ogni guasto. Poi ho ripristinato e sono tornati verdi.
  E' la lezione del 09/08: un test che non puo' fallire non serve a niente.

**Cosa resta da fare - IMPORTANTE, non e' ancora confermato:**
- **Il plugin nuovo non e' ancora installato**: al momento della modifica AutoCAD era
  aperto e l'installazione non sovrascrive un plugin in uso. Va chiuso AutoCAD e
  rilanciato `scripts\Deploy.ps1`.
- **Poi va riprovata la pubblicazione in PDF.** Questa correzione rimuove l'errore che
  si vedeva nel log, ma non c'e' nessuna garanzia che dietro non ce ne sia un
  secondo: fino ad ora la pubblicazione non era MAI arrivata a partire davvero, quindi
  tutto cio' che viene dopo quel punto non e' mai stato messo alla prova.
- Se non uscissero ancora file, il log ora dice di piu': cerca la riga "Dispositivo di
  stampa in uso" (dice quale stampante e' stata scelta) e il file
  `pubblicazione_<data>.csv` scritto da AutoCAD.
- Provare anche **"Stampa"** sul plotter.
- Il pacchetto ZIP in `dist\` e' fermo alla 2.0.1: da rigenerare.
- Restano in sospeso: AutoCAD 2026 in ufficio, AutoCAD 2027 mai aperto, il messaggio
  d'avvio invisibile (vedi blocco precedente), `PSScriptAnalyzer` non installato.

---

## 01/09/2026 (conferma) - La pubblicazione PDF funziona. Release v2.0.2

**Provato in AutoCAD 2024 e confermato dall'utente: i PDF escono.** E' la prima
volta da quando il progetto esiste che la pubblicazione arriva fino in fondo.

Le righe del log che lo dimostrano:
```
[06:41:07] [Avvio] Gestione Layout v2.0.2 | compilato per: AutoCAD 2024 (.NET 4.8)
[06:42:52] [Stampa/Pubblica] Dispositivo di stampa in uso: DWG To PDF.pc3
[06:43:00] [Stampa/Pubblica] Lavoro (Pdf) consegnato alla pubblicazione in secondo piano
```
La riga di mezzo e' quella nuova: prima, al suo posto, c'era l'errore `eNullPtr`.

**Cosa e' stato chiuso oggi, in tutto.** Cinque cose in sospeso da luglio e agosto:
1. il comando ora risponde anche a `LAYOUTMANAGER`;
2. Ctrl+A dentro la palette - confermato;
3. Duplica e numerazione progressiva - confermato;
4. il trascinamento aggiorna la barra delle schede - confermato (era la
   correzione piu' incerta di tutto il progetto);
5. **la pubblicazione in PDF - confermata.**

**Release fatta.**
- Branch `feature/layout-palette` unito su `master` (che era fermo al primo
  commit di luglio: da oggi torna ad essere la versione buona).
- Tag `v2.0.2`.
- Pacchetto `MN_LayoutManager_v2.0.2_AutoCAD2024-2027_2026-09-01.zip` allegato
  alla release su GitHub, cosi' si scarica e si installa senza codice sorgente
  ne' SDK. Contenuto verificato prima di pubblicarlo: tutte e tre le varianti
  (net48 / net8 / net10), versione 2.0.2, LEGGIMI aggiornato con i due nomi
  del comando.

**Un dettaglio minore da sistemare, segnalato per onesta'.**
Il plugin scrive nel log "L'esito dei singoli fogli e' nel registro CSV di
pubblicazione", ma **quel file CSV non viene creato**: AutoCAD lo scrive solo se
il registro di pubblicazione e' attivo nelle sue impostazioni. Il messaggio
quindi rimanda a un file che spesso non esiste. Non e' grave (la pubblicazione
funziona), ma il messaggio va corretto o il registro va attivato davvero.

**Cosa resta da fare:**
- **"Stampa" sul plotter non e' ancora stata provata.** Passa dalla stessa
  correzione, ma per quel caso il plugin non impone il dispositivo: usa quello
  del layout. E' una strada diversa e va vista sul campo.
- **AutoCAD 2026 in ufficio**: ora c'e' un pacchetto 2.0.2 pronto da provare.
- AutoCAD 2027 non e' mai stato aperto da nessuno.
- Il registro CSV di pubblicazione, qui sopra.
- Il messaggio d'avvio invisibile (vedi il primo blocco di oggi).
- `PSScriptAnalyzer` ancora non installato su questa macchina.

---

## 03/09/2026 - Due messaggi che mentivano: quello d'avvio e quello sul CSV

Sessione breve e mirata. Nessuna funzione nuova: sono stati chiusi i due difetti
minori che erano rimasti scritti in fondo alla memoria da settembre, entrambi
della stessa famiglia - il plugin diceva all'utente cose che non erano vere.

**Prove sul campo passate (dette dall'utente).** AutoCAD **2026 e 2027** provati:
funziona tutto. Restavano gli ultimi due software mai aperti da nessuno, e ora
non ci sono piu' versioni non provate. La **stampa su plotter** e' stata invece
messa da parte per scelta: qui interessa solo produrre PDF, quindi non e' un
lavoro in sospeso ma una funzione che non si usa.

**Difetto 1 - il messaggio d'avvio non compariva mai.**
All'avvio il plugin dovrebbe scrivere nella riga di comando "plugin caricato,
digita GESTIONELAYOUT". Non e' mai comparso: AutoCAD carica i plugin del bundle
PRIMA di aprire il disegno iniziale, e senza disegno non esiste nessuna riga di
comando su cui scrivere. Il messaggio veniva quindi buttato via proprio nel
momento in cui sarebbe servito - ed e' esattamente il messaggio che il 01/09
avrebbe evitato l'ora persa a cercare un comando che si chiamava in un altro modo.

La soluzione: il messaggio non si perde piu', si mette in attesa. Il nuovo modulo
`Infrastructure\StartupMessage.cs` lo tiene da parte e lo riscrive al primo momento
libero di AutoCAD (evento `Idle`), quando il disegno c'e'. Il messaggio ora nomina
**entrambi** i nomi del comando.

**Difetto 2 - il log rimandava a un file che di solito non c'e'.**
Il plugin scriveva "l'esito dei singoli fogli e' nel registro CSV di pubblicazione".
Quel CSV pero' lo scrive AutoCAD, e **solo** se nelle sue opzioni e' attivo il
salvataggio automatico del registro di stampa e pubblicazione. Il plugin puo'
soltanto dire DOVE scriverlo, non accenderlo. Risultato: l'utente veniva mandato a
cercare un file quasi sempre inesistente.

Ora il messaggio e' condizionale: dice il percorso, dice a quale condizione il file
esiste e come si attiva. Percorso e frase stanno in un modulo solo,
`Infrastructure\PublishLogFile.cs`, cosi' il file e il modo di raccontarlo non
possono piu' divergere.

**Decisioni importanti (e perche').**
- **`StartupMessage` e' un modulo a se'.** E' un rimedio a un problema di tempi, non
  una soluzione elegante: tenerlo separato vuol dire che se un domani AutoCAD offrisse
  un aggancio migliore si cambia un file solo. Stessa logica del `LayoutTabRefresher`
  di luglio.
- **Si smette di riprovare dopo 2 minuti.** L'evento `Idle` di AutoCAD scatta di
  continuo: restare agganciati per sempre, nel caso in cui un disegno non venga mai
  aperto, sarebbe uno spreco. Dopo il tempo massimo il plugin rinuncia e lo annota
  nel log.
- **Se il tentativo fallisce si rinuncia subito.** Un errore dentro `Idle` si
  ripeterebbe centinaia di volte al secondo riempiendo il log di righe identiche.
- **Il plugin NON attiva il registro CSV di AutoCAD al posto dell'utente.** Verificato
  per riflessione: su `DsdData` esiste solo `LogFilePath`, nessun interruttore. Sarebbe
  quindi andato toccato un impostazione generale di AutoCAD - la stessa ragione per cui
  a luglio `BACKGROUNDPLOT` veniva rimessa com'era. Meglio un messaggio onesto che una
  modifica non richiesta alle preferenze di chi usa il programma.
- **Nessun test automatico nuovo, di proposito.** `StartupMessage` vive di eventi
  AutoCAD e non e' verificabile fuori da AutoCAD; il testo del messaggio sul CSV, se
  testato, sarebbe un test che ricopia il codice invece di controllarlo - la "rete di
  sicurezza finta" che il 09/08 aveva lasciato passare il bug dei numeri del DSD.

**Verificato con:**
- `dotnet build -c Release -t:Rebuild`: 0 errori, **0 avvisi** su net48, net8, net10.
- `dotnet test`: **143 test x 3 motori, tutti passati** (invariati: non ne servivano
  di nuovi).
- **Letto dentro tutte e tre le DLL compilate** che il codice nuovo ci sia davvero
  (`StartupMessage`, la frase condizionale sul registro, il messaggio con i due nomi
  del comando). E' il controllo che a settembre aveva confermato l'alias del comando:
  compilare non basta, va guardato cosa e' finito nel file che AutoCAD carica.
- Corretta anche una riga del `README.md` che prometteva la comparsa del messaggio
  d'avvio: era falsa fino a oggi.

**Commit / release.**
- Branch `fix/messaggio-avvio-e-registro-csv`, tre commit separati (un difetto per
  commit, piu' il cambio di versione).
- Versione portata a **2.0.3**: cambia cio' che l'utente vede, quindi la build
  installata deve potersi distinguere dalla 2.0.2 leggendo il log.

**Cosa resta da fare:**
- **Installare la 2.0.3** (`scripts\Deploy.ps1`, con AutoCAD chiuso) e **guardare la
  riga di comando all'avvio**: deve comparire "plugin caricato ... Digita
  GESTIONELAYOUT (oppure LAYOUTMANAGER)". E' l'unica prova che conta per il difetto 1,
  e non e' verificabile in automatico.
- Rigenerare il pacchetto ZIP (`scripts\CreaPacchetto.ps1`): quello in `dist\` e' fermo
  alla 2.0.2.
- Il branch non e' stato unito ne' spinto su GitHub: da fare dopo la prova a mano.
- `PSScriptAnalyzer` resta non installato (in sospeso dal 21/07). Serve solo a
  controllare gli script PowerShell senza eseguirli; i 4 script del progetto sono
  piccoli e gia' controllati per sintassi, quindi non e' urgente.
- **Stampa su plotter: non e' piu' in sospeso.** L'utente ha deciso che interessa
  solo il PDF.

---

## 03/09/2026 (seconda parte) - Installata la 2.0.3 e rifatto il pacchetto

Sessione di consegna, non di sviluppo: **nessuna riga di codice nuova**. Sono state
eseguite le cose rimaste in coda al blocco precedente, cioe' portare la 2.0.3 dal
codice al plugin davvero installato e al pacchetto distribuibile.

**Cosa ho fatto.**
- Controllato che AutoCAD non fosse in esecuzione (`Get-Process acad`): l'installazione
  non sovrascrive un plugin in uso, ed e' il motivo per cui il 01/09 era rimasta a meta'.
- Ricompilato da zero in Release: 0 errori, 0 avvisi su net48, net8, net10.
- `dotnet test`: 143 test x 3 motori, tutti passati.
- Installata la **2.0.3** con `scripts\Deploy.ps1`.
- Rigenerato il pacchetto con `scripts\CreaPacchetto.ps1`:
  `dist\MN_LayoutManager_v2.0.3_AutoCAD2024-2027_2026-09-03.zip` (1172,7 KB).
  Quello in `dist\` era fermo alla 2.0.2.

**Verificato con (la parte che conta).**
- **Dentro le tre DLL EFFETTIVAMENTE INSTALLATE** in `ApplicationPlugins` - non quelle
  di compilazione, che sono un'altra cosa: versione 2.0.3 su tutte e tre, commit
  `4e818f3` (= HEAD), e presenti tutti i pezzi nuovi del blocco precedente
  (`StartupMessage`, `PublishLogFile`, il messaggio d'avvio con **entrambi** i nomi del
  comando, la frase condizionale sul registro CSV). La vecchia frase bugiarda
  ("e' nel registro CSV di pubblicazione") non c'e' piu' in nessuna delle tre.
- `PackageContents.xml` installato: `AppVersion="2.0.3"`, i tre intervalli di serie
  invariati e non sovrapposti (R24.3 / R25.0-R25.1 / R26.0).
- ZIP estratto e ispezionato: le tre varianti tutte a 2.0.3, **nessun sorgente e
  nessun `.pdb`** dentro, `LEGGIMI.txt` senza segnaposto rimasti e con versione,
  data e build (`4e818f3`) giusti.
- Controllo di sintassi dei 4 script PowerShell: 0 errori.

**Una lezione sul metodo di verifica (vale per le prossime volte).**
Il primo tentativo di cercare le stringhe dentro le DLL ha dato **quattro falsi
"manca"**: decodificando l'intero file come UTF-16 partendo dal primo byte, tutte le
stringhe che iniziano a un byte dispari escono illeggibili e la ricerca non le trova.
Rifatta la ricerca a livello di byte, che funziona a qualsiasi allineamento, sono
comparse tutte. Per accorgersene sono stati aggiunti due controlli del controllo: una
stringa che DEVE esserci (`PublishLogFile`) e una che NON deve esserci (la vecchia
frase sul CSV). E' la stessa precauzione che il 01/09 aveva evitato una risposta falsa
sul nome `LAYOUTMANAGER` dentro `acad.exe`. **Un metodo di verifica va verificato
prima di credere al suo esito**, altrimenti si "scopre" un difetto che non esiste.

**Cosa resta da fare.**
- **La prova a mano in AutoCAD**, l'unica cosa non automatizzabile: aprire AutoCAD e
  guardare la riga di comando appena il disegno e' pronto. Deve comparire
  "plugin caricato (...). Digita GESTIONELAYOUT (oppure LAYOUTMANAGER) per aprire la
  palette." E' la prova del difetto 1 del blocco precedente: fino ad oggi quel
  messaggio non e' mai comparso.
- **Merge, tag e push non sono stati fatti**: si fanno dopo la prova a mano, come
  deciso nel blocco precedente. Il branch `fix/messaggio-avvio-e-registro-csv` ha 4
  commit locali; `origin/master` e' ancora alla 2.0.2 (`9da0f5a`), e il tag `v2.0.3`
  non esiste ancora.
- `PSScriptAnalyzer` resta non installato (in sospeso dal 21/07): richiede di scaricare
  un modulo da internet, quindi va deciso dall'utente. Non urgente.

---
