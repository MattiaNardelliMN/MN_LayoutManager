# Layout Manager Palette per AutoCAD

> **Nota di attuazione (2026-07-21).** Questo e' il documento di progetto originale.
> Il plugin e' stato realizzato seguendolo, con due differenze decise insieme:
> 1. l'interfaccia e' in **WPF con tema scuro**, non in WinForms (quindi niente
>    `LayoutPaletteControl.Designer.cs` / `.resx`, e niente `System.Windows.Forms`);
> 2. la logica che non dipende da AutoCAD e' stata estratta in un progetto separato
>    `MN_LayoutManager.Core`, per poterla coprire con test automatici.
>
> Per la struttura reale dei file e le istruzioni d'uso vedi `README.md`.

## Contesto

Obiettivo: costruire da zero un plugin .NET per AutoCAD che sostituisca il tab-bar nativo dei layout con un pannello agganciabile (non modale, sempre visibile) molto più potente: elenco navigabile, rinomina rapida, riordino drag&drop, copia/incolla/eliminazione, creazione di nuovi layout, stampa/pubblicazione batch e accesso rapido alle impostazioni di pagina.

In AutoLISP puro (DCL) non è possibile realizzare una palette agganciabile con editing inline stile Explorer: per questo sarà un **plugin .NET Framework**, progetto nuovo e indipendente, non basato su codice o convenzioni di altri progetti esistenti.

Target: **AutoCAD 2024**, .NET Framework 4.8, assembly di riferimento in `C:\Program Files\Autodesk\AutoCAD 2024\` (`accoremgd.dll`, `acdbmgd.dll`, `acmgd.dll`, `AdWindows.dll`), caricamento via `NETLOAD`.

## Requisiti funzionali

1. Palette non modale agganciabile (`PaletteSet`), elenca tutti i layout (Model escluso) nell'ordine dei tab.
2. **F2** rinomina inline il layout selezionato.
3. **Doppio click** su un layout lo attiva nel disegno.
4. Navigazione con **frecce** su/giù.
5. **Drag & drop** per riordinare i layout, con scrittura del `TabOrder` reale.
6. **Rinomina in batch** su tutti i layout (non solo selezionati), con filtro testuale opzionale ("contiene"), modalità: aggiungi prefisso, aggiungi suffisso, trova&sostituisci.
7. Sincronizzazione ragionevole con modifiche esterne (rinomina/aggiunta/rimozione/cambio layout via comandi nativi, cambio disegno attivo).
8. **Tasto destro**: Copia, Elimina layout (con conferma). La posizione del layout copiato non viene forzata: l'utente la sistema poi liberamente col drag&drop.
9. **Ctrl+C / Ctrl+V** come alternativa da tastiera a Copia/Incolla.
10. **Selezione multipla** con "Seleziona tutti".
11. **Stampa** e **Pubblica** (DWF/PDF) sui layout selezionati oppure su tutti.
12. Accesso rapido a **Gestione impostazioni di pagina** per il layout scelto.
13. **Nuovo layout** vuoto da zero, pronto per rinomina immediata.

## Attenzione: collisione di nomi

Il tipo AutoCAD `Autodesk.AutoCAD.DatabaseServices.LayoutManager` verrà usato costantemente. Il progetto **non deve chiamarsi/namespacciarsi `LayoutManager`**, altrimenti ogni riferimento non qualificato a `LayoutManager.Current` risolve nel namespace del progetto invece che nel tipo Autodesk (errore di compilazione). Nome progetto/namespace: **`MN_LayoutManager`**; "Gestione Layout" resta solo la label visibile all'utente.

## Struttura progetto (nuovo, da zero)

```
MN_LayoutManager\
    MN_LayoutManager.sln
    MN_LayoutManager\
        MN_LayoutManager.csproj
        Properties\AssemblyInfo.cs
        Commands.cs
        LayoutPaletteHost.cs
        LayoutPaletteControl.cs
        LayoutPaletteControl.Designer.cs
        LayoutPaletteControl.resx
        LayoutService.cs
```

`MN_LayoutManager.csproj`: progetto Class Library .NET Framework 4.8, `PlatformTarget=x64`, `OutputType=Library`. Riferimenti (`HintPath`, `CopyLocal=false`) a:
- `C:\Program Files\Autodesk\AutoCAD 2024\accoremgd.dll`
- `C:\Program Files\Autodesk\AutoCAD 2024\acdbmgd.dll`
- `C:\Program Files\Autodesk\AutoCAD 2024\acmgd.dll`
- `C:\Program Files\Autodesk\AutoCAD 2024\AdWindows.dll` (per `PaletteSet`/`PaletteSetStyles`)
- `System.Windows.Forms`, `System.Drawing` (per la UI)

Commenti/messaggi utente in italiano. `Document/Editor/Database` ottenuti da `Application.DocumentManager.MdiActiveDocument`; mutazioni al DB dentro `using (Transaction tr = db.TransactionManager.StartTransaction()) { ...; tr.Commit(); }`.

## Componenti principali

**`Commands.cs`** — entry point, implementa `IExtensionApplication` (necessario per iscriversi agli eventi `LayoutManager` per l'intera vita del plugin e disiscriversi in `Terminate()`):
```csharp
[assembly: CommandClass(typeof(MN_LayoutManager.Commands))]
[assembly: ExtensionApplication(typeof(MN_LayoutManager.Commands))]

public class Commands : IExtensionApplication
{
    public void Initialize() { /* iscrizione eventi LayoutManager + DocumentActivated + Idle */ }
    public void Terminate()  { /* disiscrizione */ }

    [CommandMethod("GestioneLayout", CommandFlags.Session)]
    public void GestioneLayout() => LayoutPaletteHost.Toggle();
}
```
`CommandFlags.Session` permette di aprire la palette anche senza disegni aperti.

**`LayoutPaletteHost.cs`** — singleton che crea/mostra il `PaletteSet` con **GUID fisso** (necessario per persistere posizione/dimensione tra sessioni) e ci aggiunge una `LayoutPaletteControl`.

**`LayoutPaletteControl.cs/.Designer.cs/.resx`** — UserControl WinForms:
- `ListView lvLayouts`: `View=Details`, colonna unica riempita, `LabelEdit=true`, `HideSelection=false` (la palette raramente ha il focus Windows: senza questo l'evidenziazione sparisce), `FullRowSelect=true`, **`MultiSelect=true`** (richiesto da selezione multipla/seleziona tutti/stampa-pubblica batch), `AllowDrop=true`.
- Toolbar sopra la lista: "Nuovo layout", "Seleziona tutti", "Stampa tutti", "Pubblica tutti".
- `ContextMenuStrip` sulla lista: Attiva, Rinomina (F2), Copia (Ctrl+C), Incolla (Ctrl+V, abilitata solo se la clipboard interna ha contenuto), Elimina, separatore, Impostazioni pagina..., separatore, Stampa selezionati, Pubblica selezionati.
- Pannello inferiore "Rinomina multipla": campo filtro ("Contiene:"), radio Prefisso/Suffisso/Trova&Sostituisci, campo/i valore, bottone "Applica" — agisce sempre su **tutti** i layout che passano il filtro, indipendentemente dalla selezione (requisito 6).

**`LayoutService.cs`** — logica pura AutoCAD API, senza dipendenze WinForms:
- `GetLayoutsOrdered(db)`: itera `db.LayoutDictionaryId`, esclude "Model", ordina per `Layout.TabOrder`.
- `ActivateLayout`, `RenameLayout`, `ReorderLayouts`, `CreateLayout`, `CopyLayout`, `DeleteLayout`, `BatchRename`, `IsValidLayoutName`.

## Dettagli tecnici chiave (API AutoCAD .NET)

Tutte le chiamate che mutano il documento, eseguite da handler WinForms della palette (fuori da un contesto di comando), vanno racchiuse in `using (doc.LockDocument())` prima della `Transaction`, perché il documento non è implicitamente bloccato:
```csharp
Document doc = Application.DocumentManager.MdiActiveDocument;
if (doc == null) return;
using (DocumentLock docLock = doc.LockDocument())
using (Transaction tr = doc.Database.TransactionManager.StartTransaction()) { ...; tr.Commit(); }
```
Ri-ottenere `MdiActiveDocument` al momento dell'azione (non da un campo cache), perché il documento attivo può cambiare mentre la palette è aperta (multi-disegno).

- **Elenco/ordine**: `Layout.TabOrder` (get/set); Model = 0, layout carta = 1..N.
- **Riordino** (drag&drop, requisito 5): in un'unica transazione, assegnare `layout.TabOrder = index + 1` per ogni layout nell'ordine desiderato. Dopo la scrittura, ricaricare l'elenco dal DB (non fidarsi dell'ordine client-side) per evitare disallineamenti.
- **Rinomina** (F2 e batch): usare `LayoutManager.Current.RenameLayout(oldName, newName)` invece di impostare `Layout.LayoutName` direttamente, per mantenere coerenti dizionario/tab-order/eventi.
- **Attivazione** (doppio click): `LayoutManager.Current.CurrentLayout = layoutName;`.
- **Nuovo layout** (requisito 13): `LayoutManager.Current.CreateLayout(name)`; dopo la creazione, selezionare il nuovo elemento in lista e avviare subito `BeginEdit()` per la rinomina inline.
- **Copia** (requisito 8/9): duplicare un layout con tutto il contenuto (viewport, entità, impostazioni di stampa) via API DB grezza è complesso e fragile. Approccio consigliato: pilotare il comando nativo `LAYOUT` con l'opzione `Copy` via `doc.SendStringToExecute("_.LAYOUT\n_Copy\n" + srcName + "\n" + newName + "\n", true, false, false)`, che riusa la logica nativa già collaudata. In alternativa, verificare in Object Browser se `LayoutManager.Current` espone un metodo `CopyLayout` diretto — se presente ed equivalente, preferirlo.
- **Elimina** (requisito 8): `LayoutManager.Current.DeleteLayout(name)`, con conferma utente (`MessageBox`) prima di procedere essendo distruttivo.
- **Impostazioni di pagina** (requisito 12): non reimplementare il dialogo nativo. Attivare il layout scelto (se non già attivo) poi `doc.SendStringToExecute("_.PAGESETUP ", true, false, false)` per aprire la Gestione impostazioni di pagina nativa.
- **Stampa/Pubblica batch** (requisito 11): la API gestita `Publisher`/`DsdData` è una delle zone più fragili dell'API .NET di AutoCAD. Approccio consigliato: costruire un file DSD temporaneo (elenco fogli in formato INI) con i layout selezionati (o tutti), e pilotarlo tramite `-PUBLISH` scriptato via `SendStringToExecute`, così da riusare la pipeline nativa di pubblicazione/stampa multi-foglio invece di reimplementarla con `PlotEngine`. "Stampa" punta al plotter configurato nelle impostazioni di pagina di ciascun layout; "Pubblica" produce DWF/DWFx/PDF.

## Sincronizzazione (requisito 7)

Iscriversi una sola volta, in `Commands.Initialize()` (essendo `LayoutManager.Current` un singleton globale): `LayoutSwitched`, `LayoutCreated`, `LayoutRemoved`, `LayoutCopied`, `LayoutRenamed`; più `Application.DocumentManager.DocumentActivated` per il cambio disegno.

Per evitare rebuild sincroni dentro gli handler AutoCAD (rischio di raffiche di eventi, es. script che crea 20 layout), usare un pattern **idle-pump**: gli handler impostano solo un flag `_dirty`, e un handler su `Application.Idle` ricostruisce la `ListView` quando il flag è attivo. Questo copre anche il refresh dopo le nostre stesse azioni (rename/reorder/etc. rialzano lo stesso evento AutoCAD).

## Algoritmo rinomina batch (requisito 6) — evitare collisioni transitorie

Una rinomina sequenziale ingenua può fallire se il find&replace produce nomi che si scontrano temporaneamente (es. scambio di nomi tra due layout). Algoritmo a due fasi, dentro un unico `doc.LockDocument()`:
1. Calcolare la mappa completa vecchio→nuovo nome per i layout che passano il filtro; validare a monte che il set finale di nomi non contenga duplicati e rispetti le regole di `IsValidLayoutName` — se qualcosa non va, annullare tutto con un messaggio unico, senza toccare il disegno.
2. Fase 1: rinominare ogni layout coinvolto a un nome temporaneo univoco (es. `"~tmp_" + Guid.NewGuid()...`).
3. Fase 2: rinominare ogni nome temporaneo al nome finale desiderato.

## Validazione nomi layout

Non vuoto/spazi, non uguale (case-insensitive) a "Model", niente caratteri non ammessi (`< > / \ " : ; ? * | , = `), lunghezza ragionevole, univocità case-insensitive contro tutti gli altri layout. Verificare se `SymbolUtilityServices.IsValidSymbolName` è applicabile in questa versione prima di scrivere un controllo manuale.

## Rischi da presidiare

- Rinomina/attivazione mentre è attivo un comando nel disegno: controllare `doc.Editor.IsQuiescent` e intercettare `eLockViolation` con messaggio invece di eccezione non gestita.
- Nessun disegno aperto: ogni refresh/azione deve gestire `MdiActiveDocument == null` mostrando stato vuoto.
- Cambio disegno attivo tra costruzione lista e azione utente: ri-validare al momento dell'azione.
- Verificare su macchina reale (Object Browser di Visual Studio): esatta firma di `LayoutManager.RenameLayout`/`DeleteLayout`/eventuale `CopyLayout`, nomi esatti degli eventi `LayoutRemoved`/`LayoutToBeRemoved`.

## Ordine di implementazione consigliato

1. Scaffolding csproj/sln, comando vuoto caricabile via NETLOAD.
2. `LayoutService.cs` con `GetLayoutsOrdered`/`ActivateLayout`/`RenameLayout`, provati da un comando di test prima di costruire la UI (per validare le API incerte).
3. `LayoutPaletteControl` con solo lista + doppio click + F2.
4. Drag&drop riordino con `InsertionMark` + `ReorderLayouts`.
5. Nuovo layout, copia (via `LAYOUT Copy` scriptato), elimina, con context menu e Ctrl+C/Ctrl+V.
6. Pannello rinomina multipla (due fasi).
7. Stampa/Pubblica selezionati/tutti (DSD + `-PUBLISH` scriptato) e collegamento a `PAGESETUP`.
8. `LayoutPaletteHost` + iscrizioni eventi + idle-pump; test multi-disegno e modifiche esterne.
9. Casi limite: zero layout carta, rinomina del layout attivo, chiusura di tutti i disegni con palette aperta/flottante.

## Verifica end-to-end

- Compilare la DLL e caricarla in AutoCAD 2024 con `NETLOAD`, lanciare `GESTIONELAYOUT`.
- Verificare in un disegno con più layout: F2 rinomina, doppio click attiva, frecce navigano, drag&drop riordina (confrontare con la tab-bar nativa in basso), Ctrl+C/Ctrl+V e menu destro copiano/eliminano, "Nuovo layout" crea e mette in rinomina immediata, rinomina multipla con prefisso/suffisso/trova-sostituisci e filtro, stampa/pubblica su selezione e su tutti, "Impostazioni pagina..." apre il dialogo nativo corretto.
- Verificare sincronizzazione: rinominare/aggiungere/eliminare un layout dai comandi nativi di AutoCAD con la palette aperta e controllare che si aggiorni; cambiare disegno attivo e controllare che la lista segua.
