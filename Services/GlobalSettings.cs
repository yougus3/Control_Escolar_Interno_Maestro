using System;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;

public static class GlobalSettings
{
    // Inicia con el directorio de la app por seguridad, pero se actualizará al seleccionar una carpeta/USB
    public static string CurrentCapDirectory { get; set; } = AppContext.BaseDirectory;
}