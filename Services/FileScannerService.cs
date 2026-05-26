using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;

public class FileScannerService
{
    public List<string> ObtenerArchivosCap(string rutaUsb)
    {
        if (string.IsNullOrWhiteSpace(rutaUsb) || !Directory.Exists(rutaUsb)) 
        {
            return new List<string>();
        }

        // Filtra estrictamente archivos que inicien con CALIF y terminen en .CAP
        return Directory.GetFiles(rutaUsb, "CALIF*.CAP")
                        .OrderBy(f => f)
                        .ToList();
    }
}