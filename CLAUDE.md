# Istruzioni per Claude Code

> Chi ti scrive **non è un programmatore professionista**. Quindi non basta
> scrivere codice giusto: devi anche renderlo verificabile e spiegarmi in modo
> semplice cosa hai fatto e come sai che funziona. Se io non posso controllare
> il codice riga per riga, la responsabilità di verificarlo è tua.

---

## 0. Regole d'oro (valgono SEMPRE)

1. **Non dirti mai "ho finito" se non hai eseguito build, lint e test e sono
   tutti verdi.** Se qualcosa fallisce, correggi e riprova finché non passa.
   Se non riesci, dimmelo chiaramente invece di far finta di aver finito.
2. **Prima di scrivere codice, dimmi in 2-3 frasi cosa stai per fare e perché.**
   In italiano, semplice, senza gergo inutile.
3. **Dopo aver scritto codice, spiegami a parole cosa fa e come hai verificato
   che funziona.** Se hai scritto dei test, dimmi in linguaggio umano cosa
   controllano (es. "questo test verifica che se l'utente seleziona zero
   oggetti, il comando non va in crash ma mostra un messaggio").
4. **Fai un cambiamento alla volta.** Non riscrivere mezzo progetto in un colpo.
   Piccoli passi verificabili.
5. **Se una richiesta è ambigua, fai domande prima di procedere.** Meglio una
   domanda in più che codice sbagliato.
6. **Tieni aggiornato il file `MEMORIA.md`** (vedi sezione 5). Alla fine di ogni
   sessione di lavoro significativa, aggiungi in fondo un nuovo blocco che
   racconta cosa è stato fatto. Non sovrascrivere mai i blocchi vecchi: la
   memoria si accumula, non si cancella.
7. **Ho delle "skill" installate a livello globale** (es. per le interfacce WPF
   e per i plugin Autodesk). Quando il lavoro tocca quei temi, seguile. Se non
   sai se una skill si applica, chiedimelo.

---

## 1. Contesto del progetto

Sviluppo **plugin per software Autodesk** (AutoCAD, Civil 3D, Revit) più qualche
tool esterno. Linguaggi principali:

- **C#** — plugin veri e propri (usano le API Autodesk)
- **Python** — automazioni, script, tool esterni
- **PowerShell** — automazione di build, deploy, operazioni di sistema

### ⚠️ Attenzione al target .NET (importante per C#)
Il framework .NET dipende dal software e dalla versione:
- **Revit** → di solito **.NET Framework 4.8** (Revit fino a 2024) o **.NET 8**
  (Revit 2025+). Dipende dall'anno di Revit.
- **AutoCAD / Civil 3D** → **.NET Framework 4.8** per versioni classiche,
  **.NET 8** per le più recenti.

👉 **Prima di scrivere o modificare codice C#, chiedimi sempre per quale
software e quale ANNO/versione è il plugin**, se non è già chiaro dal progetto
(controlla il file `.csproj`: cerca `<TargetFramework>`). Non assumere: usare il
target sbagliato fa fallire il caricamento del plugin.

---

## 2. Comandi del progetto

> Compila questa sezione insieme a me la prima volta. Quando li conosci, usali
> SEMPRE per verificarti. Se un comando non esiste ancora, proponimi di crearlo.

### C#
- Build: `dotnet build`  (oppure apri la soluzione .sln)
- Test: `dotnet test`
- Formatta: `dotnet format`

### Python
- Esegui: `python <file>.py`
- Test: `pytest`
- Lint + formattazione: `ruff check .`  e  `ruff format .`
- Controllo tipi (dove applicabile): `mypy .`

### PowerShell
- Test: `Invoke-Pester`
- Analisi statica: `Invoke-ScriptAnalyzer -Path .`

---

## 3. Qualità del codice

- **Scrivi test per ogni funzione nuova o modificata** quando è possibile
  testarla senza aprire AutoCAD/Revit (cioè la logica "pura": calcoli,
  trasformazioni di dati, validazioni). Per il codice che dipende dalle API
  Autodesk e non è testabile in automatico, **isola la logica testabile in
  funzioni separate** e testa quelle, poi spiegami cosa invece va provato a mano
  dentro il software.
- **Gestisci sempre gli errori in modo esplicito.** Nei plugin CAD un errore non
  gestito può far crashare l'intero software: niente `catch` vuoti e silenziosi.
  Mostra o registra un messaggio utile.
- **Nelle transazioni Autodesk** (Transaction in Revit, Transaction Manager in
  AutoCAD) assicurati sempre che vengano chiuse/commit o annullate correttamente,
  anche in caso di errore. Segnalami se vedi transazioni lasciate aperte.
- Funzioni piccole, con una sola responsabilità.
- Nomi chiari e descrittivi (in inglese per il codice va bene).
- Commenta solo il **perché**, non il **cosa**. Niente commenti ovvi.
- Non lasciare codice morto, `TODO` vaghi o funzioni scritte a metà senza
  avvisarmi.

### Regole aggiuntive per alzare la qualità (importante)

Queste servono a far sì che tu NON debba fidarti ciecamente: il codice si
controlla il più possibile da solo.

- **Attiva e rispetta gli analizzatori automatici.**
  - C#: abilita i .NET analyzers nel `.csproj` (`<EnableNETAnalyzers>true`,
    `<AnalysisLevel>latest`) e tratta gli warning come errori
    (`<TreatWarningsAsErrors>true`). Se un warning è legittimo da ignorare,
    spiegami perché prima di sopprimerlo.
  - Python: `ruff` deve passare pulito. Aggiungi anche i controlli di tipo con
    `mypy` dove ha senso.
  - PowerShell: `Invoke-ScriptAnalyzer` deve passare senza avvisi.
- **Nessun "numero magico" o stringa hardcoded** sparsi nel codice: usa costanti
  o file di configurazione con nomi chiari.
- **Valida sempre gli input** (selezioni vuote, valori nulli, tipi di oggetto
  sbagliati) prima di usarli. Nei plugin CAD è la causa numero uno di crash.
- **Usa `using` / dispose corretti** per tutte le risorse Autodesk che lo
  richiedono, così non lasci memoria o oggetti appesi.
- **Non ripeterti (DRY):** se stai copiando lo stesso blocco di codice, estrai
  una funzione. Segnalamelo quando lo fai.
- **Ragiona ad alta voce sui casi limite** prima di scrivere: cosa succede se
  l'utente annulla? Se non seleziona niente? Se il documento è vuoto? Elencali
  e gestiscili.
- **Dopo aver finito, fai una mini auto-revisione**: rileggi il tuo codice come
  se fosse di qualcun altro e dimmi onestamente se c'è qualcosa di fragile,
  poco chiaro o migliorabile. Preferisco saperlo da te che scoprirlo dopo.

---

## 4. Modularità (il progetto deve reggere anche se un pezzo si rompe)

Voglio progetti **modulari**: divisi in parti indipendenti, ognuna con un
compito preciso. Se un modulo si guasta o va rimosso, il resto del progetto deve
continuare a funzionare, non deve crollare tutto.

- **Ogni funzionalità in un modulo separato** con un confine chiaro (una
  cartella, una classe o un progetto/libreria dedicati). Un comando, una
  finestra, un servizio: ognuno per conto suo.
- **I moduli comunicano attraverso "interfacce" chiare**, non infilando le mani
  dentro i dettagli l'uno dell'altro. In pratica: un modulo espone poche
  funzioni pubbliche ben definite e nasconde il resto.
- **Basse dipendenze fra moduli.** Se per toccare il modulo A devi cambiare
  anche B, C e D, sono troppo legati: dimmelo e proponi come separarli.
- **Isola le parti rischiose** (chiamate alle API Autodesk, accesso a file,
  rete) dietro un modulo dedicato, così se qualcosa va storto l'errore resta
  confinato lì e non contamina il resto.
- **Un modulo che fallisce deve fallire "in modo pulito"**: segnala l'errore,
  ma non deve far crashare l'intero plugin. Gestisci il guasto localmente.
- Quando aggiungi qualcosa di nuovo, chiediti prima: "è un modulo nuovo o
  appartiene a uno esistente?" e dimmi dove lo stai mettendo e perché.

> In breve: preferisci sempre tante scatole piccole e indipendenti a un unico
> blocco gigante. Le scatole piccole si riparano e si sostituiscono senza
> rompere le altre.

---

## 5. Uso dei sottoagenti (dividere il lavoro grosso)

Quando un compito è grande o tocca **aree diverse** (es. logica + interfaccia +
API Autodesk + test), **suddividilo e delega le diverse aree a sottoagenti
specializzati** invece di fare tutto in un unico flusso confuso. Questo tiene il
lavoro ordinato e ogni parte curata da un "esperto" dedicato.

- Se il lavoro ha più aree distinte, **proponimi un piano** di come lo dividi tra
  i sottoagenti prima di partire, così io capisco cosa succederà.
- Ho dei sottoagenti specializzati installati (revisore codice, scrittore test,
  esperto UI/WPF, esperto Autodesk/API). **Usali per le rispettive aree.** Se
  serve, richiamali per nome.
- Ogni sottoagente lavora sulla sua area; poi tu **ricomponi il risultato** e mi
  spieghi a parole semplici cosa ha fatto ciascuno.
- Per compiti piccoli e a area singola NON serve dividere: fai direttamente.
  I sottoagenti servono quando il lavoro è ampio o tocca più fronti insieme.

---

## 6. Git e GitHub

Uso Git collegato al mio account GitHub. Lavoro da solo ma voglio disciplina.

- **Non committare mai direttamente su `main`.** Crea un branch per ogni lavoro:
  `feature/<nome-breve>` per nuove funzioni, `fix/<nome-breve>` per correzioni.
- **Un commit = un cambiamento logico completo e funzionante.** Non accorpare
  cose scollegate nello stesso commit.
- **Messaggi in formato Conventional Commits**, in inglese:
  - `feat:` nuova funzionalità
  - `fix:` correzione bug
  - `refactor:` riorganizzazione senza cambiare comportamento
  - `test:` aggiunta o modifica test
  - `docs:` documentazione
  - `chore:` build, config, manutenzione
- **Non committare MAI**: segreti, password, chiavi API, file `.env`, file di
  build (`bin/`, `obj/`), pacchetti, file temporanei. (Vedi `.gitignore`.)
- **Prima di ogni commit**, esegui build + test + lint. Committa solo se sono
  verdi.
- Quando fai un commit o apri un branch, **dimmi in una frase cosa hai fatto**,
  così io so sempre cosa sta succedendo nel repo.
- Non fare `push` o operazioni che modificano GitHub senza chiedermelo prima,
  a meno che io non te l'abbia già detto esplicitamente in quella sessione.

---

## 7. Struttura del progetto

> Se il repo è nuovo o disordinato, proponimi una struttura e spiegamela prima
> di spostare file. Idea di base:

```
/src        codice sorgente (organizzato per linguaggio o per plugin)
/tests      i test
/scripts    script PowerShell/Python di build e automazione
/docs       documentazione e appunti
README.md   descrizione del progetto e come si usa
MEMORIA.md  diario/timeline del progetto (vedi sotto)
.gitignore  file da non tracciare
CLAUDE.md   questo file
```

- Quando crei un file nuovo, mettilo nel posto giusto secondo questa struttura
  e dimmi dove l'hai messo.
- Mantieni un `README.md` aggiornato con: cosa fa il progetto, per quale
  software/versione, e come si installa/usa il plugin.

### Il file MEMORIA.md (importante)

`MEMORIA.md` è il **diario di bordo** del progetto: serve a me per riprendere il
lavoro anche dopo mesi e capire cosa è stato fatto e perché. Regole:

- **Non si sovrascrive mai.** Ogni sessione aggiunge un nuovo blocco IN FONDO.
  I blocchi vecchi restano lì per sempre: è una memoria che si accumula.
- Alla fine di ogni sessione di lavoro significativa, aggiungi un blocco con:
  data, cosa è stato fatto (a parole semplici, non solo tecniche), le decisioni
  importanti prese e il perché, eventuali commit/release fatti, e cosa resta da
  fare la prossima volta.
- Scrivilo come lo racconteresti a una persona che riprende il progetto da zero:
  deve poter capire la storia leggendo dall'alto verso il basso.
- Quando fai un commit o una release, annotalo qui con una riga di contesto
  ("perché" l'ho fatto), non solo il messaggio tecnico del commit.

---

## 8. Come comunicare con me

- Parlami in **italiano**, in modo semplice.
- Quando usi un termine tecnico che potrei non conoscere, spiegalo in mezza riga.
- Se stai per fare qualcosa di rischioso o irreversibile (cancellare file,
  riscrivere molto codice, modificare GitHub), **fermati e chiedimi conferma**.
- Alla fine di un lavoro, dammi un riassunto breve: cosa hai cambiato, come hai
  verificato, e cosa eventualmente devo provare io a mano dentro
  AutoCAD/Civil3D/Revit.
