# MN_LayoutManager — Gestione Layout per AutoCAD

Plugin per AutoCAD che sostituisce la barra delle schede dei layout con un
**pannello agganciabile** molto piu' comodo: elenco navigabile, rinomina rapida,
riordino con il mouse, copia/incolla, eliminazione, rinomina multipla,
stampa e pubblicazione in blocco.

## Software di riferimento

- Software: **AutoCAD 2024** (sigla interna R24.3)
- Target .NET: **.NET Framework 4.8**, piattaforma x64
- API usate: `accoremgd.dll`, `acdbmgd.dll`, `acmgd.dll`, `AdWindows.dll`
  (lette da `C:\Program Files\Autodesk\AutoCAD 2024\`)

> Il target .NET NON va cambiato: AutoCAD 2024 carica solo plugin .NET Framework 4.8.

## Installazione

1. **Chiudi AutoCAD.**
2. Doppio click su `scripts\Installa plugin.bat`.

Lo script compila il plugin, esegue i test e — solo se tutto e' verde — lo copia
in `%AppData%\Autodesk\ApplicationPlugins\LayoutManagerPalette.bundle\`.
Da quel momento AutoCAD lo carica **da solo a ogni avvio**: non serve `NETLOAD`.

Per disinstallare: `.\scripts\Deploy.ps1 -Uninstall`

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
| Selezione multipla | Ctrl+clic / Maiusc+clic, oppure "Seleziona tutti" (Ctrl+A) |
| Copiare un layout | **Ctrl+C** poi **Ctrl+V**, oppure tasto destro |
| Eliminare | **Canc** o tasto destro (chiede sempre conferma) |
| Nuovo layout | bottone "Nuovo layout": lo crea e lo mette subito in rinomina |
| Impostazioni di pagina | tasto destro → apre la finestra nativa di AutoCAD |
| Stampare / pubblicare | bottoni "Stampa tutti" / "Pubblica tutti", oppure tasto destro sui selezionati |
| Rinomina multipla | pannello in basso: prefisso, suffisso o trova/sostituisci, con filtro |

La palette si aggiorna da sola anche quando i layout vengono modificati con i
comandi nativi di AutoCAD o quando si cambia disegno attivo.

## Se qualcosa non funziona

Il plugin scrive un file di log leggibile, uno al giorno, qui:

```
%AppData%\LayoutManagerPalette\logs\
```

Ogni riga dice data, gravita' (INFO / WARN / ERROR), cosa stava facendo il plugin
e cosa e' andato storto. E' il file da guardare (o da inoltrare) quando qualcosa
si comporta in modo strano.

## Struttura del progetto

```
src/LayoutManagerPalette.Core/   logica pura (nomi, rinomina multipla, riordino, file DSD)
src/LayoutManagerPalette/        plugin vero: comandi AutoCAD + palette WPF scura
tests/                           test automatici della logica pura
scripts/                         installazione e disinstallazione
```

La divisione non e' casuale: tutto cio' che "ragiona" sta in `.Core`, che non
dipende da AutoCAD e quindi si puo' testare in automatico. Il progetto che tocca
le API AutoCAD contiene solo chiamate dirette, dove c'e' poco da sbagliare.

## Sviluppo

```
dotnet build          compila tutto
dotnet test           esegue i test automatici
```

Vedi `CLAUDE.md` per le regole di sviluppo e `MEMORIA.md` per la storia del progetto.
