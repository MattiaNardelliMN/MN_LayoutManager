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
- Realizzare il piano descritto in `LayoutManagerPalette_Piano.md`.

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
- Creato il progetto vero e proprio a partire dal piano in `LayoutManagerPalette_Piano.md`.
  Il progetto e' diviso in tre parti separate:
  - `src/LayoutManagerPalette.Core` = la parte che "ragiona" (regole sui nomi dei layout,
    calcolo della rinomina multipla, calcolo del riordino, generazione dell'elenco fogli
    per la stampa). Non sa niente di AutoCAD, quindi si puo' testare a comando.
  - `src/LayoutManagerPalette` = il plugin che parla con AutoCAD e la palette grafica.
  - `tests/LayoutManagerPalette.Core.Tests` = 76 test automatici sulla parte che ragiona.
- Scritta la palette in WPF con tema scuro moderno (elenco layout, barra strumenti,
  menu col tasto destro, pannello rinomina multipla, riga di stato).
- Scritto il comando `GESTIONELAYOUT` che apre e chiude la palette.
- Aggiunto un registro degli errori su file, uno al giorno, in
  `%AppData%\LayoutManagerPalette\logs\`.
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
