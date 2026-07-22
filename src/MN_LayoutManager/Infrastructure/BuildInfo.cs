using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace MN_LayoutManager.Infrastructure
{
    /// <summary>
    /// Dice quale delle tre compilazioni del plugin e' stata caricata e su quale
    /// AutoCAD sta girando.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Serve a diagnosticare il problema piu' insidioso di questo plugin: AutoCAD
    /// carica solo la compilazione fatta per il proprio motore .NET, e quando non ne
    /// trova una adatta NON dice niente. Il comando semplicemente non esiste, senza
    /// nessun errore da nessuna parte.
    /// </para>
    /// <para>
    /// Con questa riga scritta nel log all'avvio la situazione diventa leggibile:
    /// se il log c'e', il plugin e' stato caricato e la riga dice quale versione;
    /// se il log NON c'e' affatto, AutoCAD non ha trovato una compilazione adatta e
    /// il problema sta nel PackageContents.xml, non nel codice.
    /// </para>
    /// </remarks>
    public static class BuildInfo
    {
        private const string OperationName = "Diagnostica avvio";

        /// <summary>
        /// Per quale famiglia di AutoCAD e' stata compilata QUESTA copia del plugin.
        /// </summary>
        /// <remarks>
        /// Il valore viene deciso dal compilatore, non a runtime: e' scritto dentro la
        /// DLL ed e' quindi la prova di quale delle tre cartelle AutoCAD ha caricato.
        /// </remarks>
        public const string BuiltFor =
#if NETFRAMEWORK
            "AutoCAD 2024 (.NET Framework 4.8)";
#elif NET10_0_OR_GREATER
            "AutoCAD 2027 (.NET 10)";
#else
            "AutoCAD 2025-2026 (.NET 8)";
#endif

        /// <summary>Versione del plugin, letta dalla DLL stessa.</summary>
        public static string PluginVersion
        {
            get
            {
                Version version = typeof(BuildInfo).Assembly.GetName().Version;
                return version == null
                    ? "sconosciuta"
                    : version.ToString(3);
            }
        }

        /// <summary>
        /// Riga unica, adatta al log, che descrive plugin, motore .NET e AutoCAD.
        /// </summary>
        /// <returns>Testo gia' pronto da scrivere nel log.</returns>
        public static string Describe()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "Gestione Layout v{0} | compilato per: {1} | motore in esecuzione: {2} | AutoCAD: {3}",
                PluginVersion,
                BuiltFor,
                RuntimeInformation.FrameworkDescription,
                GetAutoCadVersion());
        }

        /// <summary>
        /// Sigla di versione di AutoCAD (per esempio "25.1s (LMS Tech)" per il 2026).
        /// </summary>
        /// <returns>La sigla, oppure "sconosciuta" se AutoCAD non la fornisce.</returns>
        public static string GetAutoCadVersion()
        {
            string version = "sconosciuta";

            // Passa da TryRun perche' questa riga gira durante il caricamento del plugin:
            // se fallisse senza rete di protezione, AutoCAD non caricherebbe il plugin
            // per colpa di un messaggio diagnostico. Il rimedio sarebbe peggio del male.
            AcadContext.TryRun(
                OperationName,
                () => version = Convert.ToString(AcadApp.GetSystemVariable("ACADVER"), CultureInfo.InvariantCulture),
                out _);

            return string.IsNullOrWhiteSpace(version) ? "sconosciuta" : version;
        }
    }
}
