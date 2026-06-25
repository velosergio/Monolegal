using System.Runtime.CompilerServices;

// Expone los miembros internal del ensamblado Infrastructure (p. ej. InvoiceTransitionsWorker.RunCycleAsync
// y CycleResult) al proyecto de pruebas, para poder ejercitar el ciclo del worker de forma determinista.
[assembly: InternalsVisibleTo("Tests")]
