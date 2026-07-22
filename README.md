# MN_LayoutManager — Gestione Layout per AutoCAD

Plugin per AutoCAD che sostituisce la barra delle schede dei layout con un
**pannello agganciabile** molto piu' comodo: elenco navigabile, rinomina rapida,
riordino con il mouse, copia/incolla, eliminazione, rinomina multipla,
stampa e pubblicazione in blocco.

## Software di riferimento

Il plugin funziona su **AutoCAD dal 2024 al 2027**, Windows a 64 bit.

AutoCAD ha cambiato "motore" .NET due volte, e una versione di AutoCAD carica
**solo** plugin compilati per il proprio motore. Per questo lo stesso codice
viene compilato tre volte e il pacchetto le contiene tutte:

| AutoCAD | Sigla interna | Motore .NET | Cartella nel bundle |
|---|---|---|---|
| 2024 | R24.3 | .NET Framework 4.8 | `Contents/net48/` |
| 2025 e 2026 | R25.0 / R25.1 | .NET 8 | `Contents/net8.0/` |
| 2027 | R26.0 | .NET 10 | `Contents/net10.0/` |

Si installa **una volta sola**: all'avvio ogni AutoCAD legge il file
`PackageContents.xml`, riconosce la propria sigla e carica solo la cartella che
gli compete, ignorando le altre.

I riferimenti alle API AutoCAD arrivano dai **pacchetti NuGet ufficiali di
Autodesk** (`AutoCAD.NET`), non dall'AutoCAD installato: si puo' quindi compilare
per il 2027 anche avendo installato solo il 2024, e su qualsiasi PC.

> **Se esce AutoCAD 2028**: aggiungi una riga alla tabella `$Targets` in
> `scripts\Comune.ps1` e il corrispondente `TargetFramework` in
> `src\MN_LayoutManager\MN_LayoutManager.csproj`. Sono gli unici due punti.

### Per chi sviluppa: serve il .NET SDK 10

Per compilare serve il **.NET SDK 10** (scarica da <https://dotnet.microsoft.com/download>),
perche' e' l'unico che sa produrre la versione per AutoCAD 2027. Sa produrre anche
tutte le altre, quindi ne basta uno.
Per *usare* il plugin invece non serve installare niente.

## Installazione

1. **Chiudi AutoCAD.**
2. Doppio click su `scripts\Installa plugin.bat`.

Lo script compila il plugin, esegue i test e — solo se tutto e' verde — lo copia
in `%AppData%\Autodesk\ApplicationPlugins\MN_LayoutManager.bundle\`.
Da quel momento AutoCAD lo carica **da solo a ogni avvio**: non serve `NETLOAD`.

Per disinstallare: `.\scripts\Deploy.ps1 -Uninstall`

### Installare su un altro PC (senza strumenti di sviluppo)

Su questo PC, lancia:

```
.\scripts\CreaPacchetto.ps1
```

Compila, esegue i test e produce in `dist\` un file ZIP con **solo il plugin gia'
compilato** (niente codice sorgente). Sull'altro PC basta estrarre tutto lo ZIP in
una cartella, chiudere AutoCAD e fare doppio click su `Installa plugin.bat`.
Sull'altro PC non serve ne' Visual Studio ne' .NET SDK: serve solo AutoCAD (una
qualsiasi versione dal 2024 al 2027). Lo stesso ZIP va bene per tutte.

## Uso

In AutoCAD digita il comando:

```
GESTIONELAYOUT
```

Si apre la palette "Gestione Layout" (richiamando il comando una seconda volta si
chiude). La palette si puo' agganciare a sinistra o a destra e ricorda posizione
e dimensione fra una sessione e l'altra.

| Azione | Come si fa |
|---|---|
| Attivare un layout | doppio clic, oppure Invio |
| Rinominare | **F2**, poi Invio per confermare o Esc per annullare |
| Navigare | frecce su/giu' |
| Riordinare | trascinare le righe (la barra turchese mostra dove finiranno) |
| Selezione multipla | Ctrl+clic / Maiusc+clic, oppure **Ctrl+A** (o tasto destro → "Seleziona tutti") |
| Copiare un layout | **Ctrl+C** poi **Ctrl+V**. Ctrl+V si puo' ripetere: ogni volta crea la copia successiva |
| Fare piu' copie insieme | bottone **"Duplica..."** o **Ctrl+D**: chiede quante e le crea tutte |
| Eliminare | **Canc** o tasto destro (chiede sempre conferma) |
| Nuovo layout | bottone "Nuovo layout": lo crea e propone il nome successivo della serie |
| Impostazioni di pagina | tasto destro → apre la finestra nativa di AutoCAD |
| Stampare un foglio | pannello "Stampa e pubblicazione" → bottone "Stampa" |
| Produrre file in blocco | stesso pannello → "Pubblica selezionati" / "Pubblica tutti" |
| Scegliere cosa rinominare | **casella di spunta** accanto a ogni nome |
| Rinomina multipla | pannello in basso: aggiungi/togli prefisso o suffisso, trova/sostituisci |

I due pannelli in fondo ("Stampa e pubblicazione" e "Rinomina multipla") si aprono
e si chiudono cliccando sul titolo, per tenere la palette compatta.

### Numerazione automatica

Se i layout seguono una progressione numerica, il plugin la riconosce da solo e
prosegue il conteggio, un po' come trascinare una cella in Excel:

- da `D_T_01` copiato tre volte escono `D_T_02`, `D_T_03`, `D_T_04`;
- gli zeri davanti vengono mantenuti (`D_T_09` → `D_T_10`);
- creando un layout nuovo, il nome successivo viene **proposto** nella casella di
  rinomina: si conferma con Invio, oppure si scrive quello che si vuole. Non viene
  mai imposto.

Se nei nomi non c'e' nessun numero si torna al classico `Nome (2)`, `Nome (3)`.

### Selezione e spunte: due cose diverse

- La **riga evidenziata** (selezione) comanda attiva, rinomina, copia, elimina,
  stampa e pubblica.
- La **casella di spunta** serve a una cosa sola: dire quali layout deve toccare la
  rinomina multipla. Resta ferma anche se si cambia selezione, cosi' si puo'
  preparare con calma un'operazione in blocco.

### Stampa e pubblicazione

Sono due lavori diversi, raccolti nello stesso pannello:

- **Stampa** manda **un solo layout** (quello selezionato) al plotter indicato nelle
  sue impostazioni di pagina.
- **Pubblica** produce **file in blocco**, sui selezionati o su tutti.

Per la pubblicazione:

- Viene creato **un file separato per ogni layout**, chiamato come il layout
  (il layout "Tavola 01" produce `Tavola 01.pdf`).
- I file finiscono nella **cartella** indicata nel pannello: si puo' scrivere a mano
  o scegliere con "Sfoglia...". Se non la tocchi, viene proposta la cartella del disegno.
- Formato selezionabile: PDF, DWF o DWFx.
- La pubblicazione parte **in background**: AutoCAD resta subito utilizzabile e la
  stampa prosegue per conto suo (compare l'icona della stampante in basso a destra).
- Prima di partire vieni sempre avvisato di quanti e quali file verranno creati.

La palette si aggiorna da sola anche quando i layout vengono modificati con i
comandi nativi di AutoCAD o quando si cambia disegno attivo.

## Se qualcosa non funziona

Il plugin scrive un file di log leggibile, uno al giorno, qui:

```
%AppData%\MN_LayoutManager\logs\
```

Ogni riga dice data, gravita' (INFO / WARN / ERROR), cosa stava facendo il plugin
e cosa e' andato storto. E' il file da guardare (o da inoltrare) quando qualcosa
si comporta in modo strano.

### "Il comando GESTIONELAYOUT non viene riconosciuto"

E' il caso piu' insidioso, perche' AutoCAD non da' nessun errore: se non trova una
versione del plugin adatta al proprio motore, semplicemente non lo carica e non
dice niente. Per distinguere le due situazioni **guarda se il file di log esiste**:

- **Il log c'e'** → il plugin e' stato caricato. La prima riga di ogni avvio dice
  quale versione e' entrata in gioco e su quale AutoCAD, per esempio:

  ```
  [2026-07-22 09:14:03] [INFO] [Avvio] Gestione Layout v2.0.0 | compilato per:
  AutoCAD 2025-2026 (.NET 8) | motore in esecuzione: .NET 8.0.29 | AutoCAD: 25.1s (LMS Tech)
  ```

  Il problema quindi non e' l'installazione, ed e' scritto nelle righe successive.

- **Il log non c'e' affatto** → AutoCAD non ha caricato niente. Vuol dire che la
  sua sigla di versione non rientra in nessuno dei blocchi del
  `PackageContents.xml`. Serve aggiungere la versione (vedi sopra).

La stessa riga compare anche nella riga di comando di AutoCAD all'avvio.

## Struttura del progetto

```
src/MN_LayoutManager.Core/   logica pura (nomi, rinomina multipla, riordino, file DSD)
src/MN_LayoutManager/        plugin vero: comandi AutoCAD + palette WPF scura
tests/                       test automatici della logica pura
scripts/                     installazione, disinstallazione, creazione pacchetto
scripts/pacchetto/           file che finiscono dentro lo ZIP distribuibile
dist/                        gli ZIP prodotti (non versionata)
```

La divisione non e' casuale: tutto cio' che "ragiona" sta in `.Core`, che non
dipende da AutoCAD e quindi si puo' testare in automatico. Il progetto che tocca
le API AutoCAD contiene solo chiamate dirette, dove c'e' poco da sbagliare.

## Sviluppo

```
dotnet build          compila tutto (tutte e tre le versioni)
dotnet test           esegue i test automatici (su tutti e tre i motori .NET)
```

I test girano tre volte, una per motore .NET: e' voluto. Alcune cose si
comportano diversamente fra .NET Framework e .NET moderno — per esempio la
codifica con cui si scrivono i file — e questo e' il modo per accorgersene
prima che lo faccia AutoCAD.

Vedi `CLAUDE.md` per le regole di sviluppo e `MEMORIA.md` per la storia del progetto.
