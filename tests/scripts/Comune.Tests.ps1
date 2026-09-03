<#
    Test degli script PowerShell condivisi (scripts\Comune.ps1).

    Si lanciano con:  Invoke-Pester -Path tests\scripts

    Le verifiche NON usano "Should": la sintassi di Should e' cambiata fra Pester 3
    e Pester 5 e non e' compatibile fra le due. Con un semplice "if ... throw" i
    test girano su qualsiasi versione, oggi e fra due anni.
#>

# Comune.ps1 non ha effetti collaterali al caricamento: definisce costanti e
# funzioni, quindi si puo' richiamare qui senza far succedere niente.
. (Join-Path $PSScriptRoot '..\..\scripts\Comune.ps1')

function Assert-Uguale($Atteso, $Ottenuto, [string]$Cosa) {
    if ($Atteso -ne $Ottenuto) {
        throw "$Cosa : atteso '$Atteso', ottenuto '$Ottenuto'."
    }
}

Describe 'Test-ArgomentiNonInteractive' {

    <#
        Perche' questi test esistono.

        "Premi INVIO per chiudere" e' indispensabile col doppio click sul .bat:
        senza, la finestra sparisce prima che si legga l'esito. Ma se lo script
        viene lanciato da un'automazione, Read-Host solleva un errore e lo script
        muore PRIMA del proprio exit: il codice di uscita smette di distinguere
        "riuscito" da "fallito".

        Si prova questa funzione e non Test-Interattivo perche' Test-Interattivo
        guarda anche com'e' fatta la sessione in corso: su una macchina di
        compilazione (dove UserInteractive e' gia' falso) risponderebbe "non
        interattivo" comunque, e il test direbbe "verde" senza aver verificato
        niente. Qui invece le attese sono assolute e valgono ovunque.
    #>

    It 'riconosce -NonInteractive scritto per esteso' {
        Assert-Uguale $true (Test-ArgomentiNonInteractive @('powershell.exe', '-NonInteractive', '-File', 'x.ps1')) 'con -NonInteractive'
    }

    It 'riconosce -NonInteractive abbreviato (PowerShell lo accetta)' {
        foreach ($abbreviazione in @('-non', '-noni', '-noninteractive', '--NonInteractive')) {
            Assert-Uguale $true (Test-ArgomentiNonInteractive @('powershell.exe', $abbreviazione)) "con $abbreviazione"
        }
    }

    It 'NON confonde -NoProfile, -NoLogo e -NoExit con -NonInteractive' {
        # E' il test che conta di piu'. I .bat del progetto lanciano PowerShell
        # con -NoProfile: se il riconoscimento si allargasse a "-no", la pausa
        # sparirebbe da TUTTI i doppi click e la finestra si chiuderebbe in
        # faccia all'utente senza che nessuno se ne accorga scrivendo codice.
        foreach ($innocuo in @('-NoProfile', '-NoLogo', '-NoExit')) {
            Assert-Uguale $false (Test-ArgomentiNonInteractive @('powershell.exe', $innocuo, '-File', 'x.ps1')) "con $innocuo"
        }
    }

    It 'non vede -NonInteractive dove non compare' {
        Assert-Uguale $false (Test-ArgomentiNonInteractive @('powershell.exe', '-File', 'x.ps1')) 'senza argomenti rilevanti'
    }
}

Describe 'Test-Interattivo' {

    It 'dice sempre "non interattivo" quando gli argomenti lo dichiarano' {
        # Questa e' l'unica attesa assoluta possibile su Test-Interattivo: gli
        # argomenti bastano da soli a decidere, qualunque sia la sessione.
        Assert-Uguale $false (Test-Interattivo @('powershell.exe', '-NonInteractive')) 'con -NonInteractive'
    }
}

Describe 'Stop-WithError' {

    It 'esce con codice 1 anche in una sessione non interattiva' {
        # Prova vera, non simulata: si lancia un PowerShell -NonInteractive
        # (cioe' la situazione che prima faceva morire lo script sulla pausa) e
        # si controlla il codice di uscita. Prima di questa correzione lo script
        # non arrivava mai alla riga "exit 1".
        $comune = (Resolve-Path (Join-Path $PSScriptRoot '..\..\scripts\Comune.ps1')).Path
        $codice = @"
`$ErrorActionPreference = 'Stop'
. '$comune'
Stop-WithError 'errore di prova'
"@
        $file = Join-Path ([System.IO.Path]::GetTempPath()) ("stop_" + [guid]::NewGuid().ToString('N') + ".ps1")
        Set-Content -Path $file -Value $codice -Encoding UTF8
        try {
            $testo = & powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File $file 2>&1 | Out-String
            Assert-Uguale 1 $LASTEXITCODE 'codice di uscita di Stop-WithError'

            if ($testo -notmatch 'errore di prova') {
                throw "Il messaggio d'errore non e' stato mostrato. Output: $testo"
            }
            if ($testo -match 'Premi INVIO') {
                throw "La pausa non doveva comparire in una sessione non interattiva. Output: $testo"
            }
        }
        finally {
            Remove-Item $file -Force -ErrorAction SilentlyContinue
        }
    }
}
