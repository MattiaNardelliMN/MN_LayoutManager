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
