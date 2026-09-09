using Microsoft.Data.Sqlite;
using PdfSharpCore.Pdf.IO;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;
using SixLabors.ImageSharp.ColorSpaces;
using System;
using System.Windows;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;

public class SqliteService : IDisposable
{
    // ============================================================
    // RUTAS
    // ============================================================
    //
    // TODO se guarda DIRECTAMENTE junto al ejecutable.
    //
    // Ejemplo:
    //
    // MiPrograma.exe
    // parciales.db
    // configuracion.bin
    //
    // NO SE UTILIZA:
    // - AppData
    // - LocalApplicationData
    // - Roaming
    // - GlobalSettings.CurrentCapDirectory
    //
    // ============================================================

    private readonly string _dataFolder;
    private readonly string _dbPath;
    private readonly string _configuracionBinPath;

    private readonly SqliteConnection _conn;

    private readonly JsonSerializerOptions _jsonOptions =
        new(System.Text.Json.JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

    // ============================================================
    // SMTP GMAIL
    // ============================================================

    private const string SmtpHost =
        "smtp.gmail.com";

    private const int SmtpPort =
        587;

    private const bool SmtpEnableSsl =
        true;

    private const string SmtpUser =
        "gustavomiranda@prefecotemixco.edu.mx";

    // IMPORTANTE:
    // Coloca aquí tu contraseña de aplicación de Gmail.
    //
    // NO la subas a GitHub ni la compartas.
    //
    private const string SmtpAppPassword =
        "TU_CONTRASENA_DE_APLICACION";

    // ============================================================
    // CLAVE CONFIGURACION.BIN
    // ============================================================

    private static readonly byte[] ConfiguracionKey =
    [
        0x43, 0x45, 0x49, 0x4D,
        0x32, 0x30, 0x32, 0x36,
        0x43, 0x45, 0x49, 0x4D,
        0x50, 0x52, 0x4F, 0x44,
        0x55, 0x43, 0x43, 0x49,
        0x4F, 0x4E, 0x31, 0x30,
        0x41, 0x45, 0x53, 0x32,
        0x35, 0x36, 0x21, 0x21
    ];

    private ConfiguracionBin? _configuracion;

    // ============================================================
    // CONSTANTES DEL BINARIO
    // ============================================================

    private const int HeaderSize = 4;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private static readonly byte[] ConfiguracionHeader =
    [
        0x43, // C
        0x45, // E
        0x49, // I
        0x4D  // M
    ];

    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public SqliteService()
    {

        // ============================================================
        // GUARDAR DIRECTAMENTE JUNTO AL EJECUTABLE (SIN APPDATA)
        // ============================================================

        // Usar la carpeta Data junto al ejecutable (AppContext.BaseDirectory\Data)
        _dataFolder = Path.Combine(AppContext.BaseDirectory ?? string.Empty, "Data");

        if (!Directory.Exists(_dataFolder))
        {
            Directory.CreateDirectory(_dataFolder);
        }

        // Archivo sqlite dentro de la carpeta Data junto al exe
        _dbPath = Path.GetFullPath(Path.Combine(_dataFolder, "parciales.db"));

        // Archivo configuracion.bin también dentro de Data
        _configuracionBinPath = Path.GetFullPath(Path.Combine(_dataFolder, "configuracion.bin"));

        // Información de depuración sobre rutas (se ve en Output -> Debug)
        System.Diagnostics.Debug.WriteLine($"[SqliteService] DB path: {_dbPath}");
        System.Diagnostics.Debug.WriteLine($"[SqliteService] ConfiguracionBin path: {_configuracionBinPath}");

        // ============================================================
        // CONEXIÓN
        // ============================================================

        _conn = new SqliteConnection(
            $"Data Source={_dbPath}");

        _conn.Open();

        EnsureTablesCreated();

        CargarConfiguracionBin();
    }

    // ============================================================
    // TABLAS
    // ============================================================

    private void EnsureTablesCreated()
    {
        using var cmd =
            _conn.CreateCommand();

        cmd.CommandText =
            @"
CREATE TABLE IF NOT EXISTS Parciales (
    Id TEXT PRIMARY KEY,
    Data TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Configuraciones (
    Id TEXT PRIMARY KEY,
    Data TEXT NOT NULL
);
";

        cmd.ExecuteNonQuery();
    }

    // ============================================================
    // PARCIALES
    // ============================================================

    public IEnumerable<(string Key, MateriaParcial Value)>
        GetAllParciales()
    {
        using var cmd =
            _conn.CreateCommand();

        cmd.CommandText =
            "SELECT Id, Data FROM Parciales";

        using var reader =
            cmd.ExecuteReader();

        while (reader.Read())
        {
            var id =
                reader.GetString(0);

            var data =
                reader.GetString(1);

            MateriaParcial? mp =
                null;

            try
            {
                mp =
                    JsonSerializer.Deserialize<MateriaParcial>(
                        data,
                        _jsonOptions);
            }
            catch
            {
                // Registro inválido.
            }

            if (mp != null)
            {
                yield return (
                    id,
                    mp);
            }
        }
    }

    // ============================================================
    // OBTENER MATERIA
    // ============================================================

    public MateriaParcial? GetMateria(
        string clave)
    {
        if (string.IsNullOrWhiteSpace(
                clave))
        {
            return null;
        }

        using var cmd =
            _conn.CreateCommand();

        cmd.CommandText =
            "SELECT Data FROM Parciales WHERE Id = @id";

        cmd.Parameters.AddWithValue(
            "@id",
            clave);

        var result =
            cmd.ExecuteScalar() as string;

        if (string.IsNullOrWhiteSpace(
                result))
        {
            return null;
        }

        try
        {
            return
                JsonSerializer.Deserialize<MateriaParcial>(
                    result,
                    _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    // ============================================================
    // GUARDAR MATERIA
    // ============================================================

    public void SaveMateria(
        string clave,
        MateriaParcial materia)
    {
        if (string.IsNullOrWhiteSpace(
                clave) ||
            materia == null)
        {
            return;
        }

        var json =
            JsonSerializer.Serialize(
                materia,
                _jsonOptions);

        using var cmd =
            _conn.CreateCommand();

        cmd.CommandText =
            @"
INSERT OR REPLACE INTO Parciales
(
    Id,
    Data
)
VALUES
(
    @id,
    @data
);
";

        cmd.Parameters.AddWithValue(
            "@id",
            clave);

        cmd.Parameters.AddWithValue(
            "@data",
            json);

        // Log escritura para depuración: qué Id se guarda y en qué fichero DB
        System.Diagnostics.Debug.WriteLine($"[SqliteService] SaveMateria id={clave} db={_dbPath}");

        // Ejecutar la inserción
        cmd.ExecuteNonQuery();
    }

    // ============================================================
    // CONFIGURACIONES SQLITE
    // ============================================================

    public IEnumerable<(
        string Key,
        ConfiguracionParciales Value)>
        GetAllConfiguraciones()
    {
        using var cmd =
            _conn.CreateCommand();

        cmd.CommandText =
            "SELECT Id, Data FROM Configuraciones";

        using var reader =
            cmd.ExecuteReader();

        while (reader.Read())
        {
            var id =
                reader.GetString(0);

            var data =
                reader.GetString(1);

            ConfiguracionParciales? cfg =
                null;

            try
            {
                cfg =
                    JsonSerializer.Deserialize<ConfiguracionParciales>(
                        data,
                        _jsonOptions);
            }
            catch
            {
                // Registro inválido.
            }

            if (cfg != null)
            {
                yield return (
                    id,
                    cfg);
            }
        }
    }

    // ============================================================
    // GUARDAR CONFIGURACION SQLITE
    // ============================================================

    public void SaveConfiguracion(
        string clave,
        ConfiguracionParciales cfg)
    {
        if (string.IsNullOrWhiteSpace(
                clave) ||
            cfg == null)
        {
            return;
        }

        var json =
            JsonSerializer.Serialize(
                cfg,
                _jsonOptions);

        using var cmd =
            _conn.CreateCommand();

        cmd.CommandText =
            @"
INSERT OR REPLACE INTO Configuraciones
(
    Id,
    Data
)
VALUES
(
    @id,
    @data
);
";

        cmd.Parameters.AddWithValue(
            "@id",
            clave);

        cmd.Parameters.AddWithValue(
            "@data",
            json);

        // Log escritura para depuración: qué Id se guarda y en qué fichero DB
        System.Diagnostics.Debug.WriteLine($"[SqliteService] SaveConfiguracion id={clave} db={_dbPath}");

        // Ejecutar la inserción
        cmd.ExecuteNonQuery();
    }

    // ============================================================
    // GRUPOS
    // ============================================================

    public Dictionary<string, string> GetGrupos()
    {
        if (_configuracion == null)
        {
            return new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);
        }

        return new Dictionary<string, string>(
            _configuracion.Grupos,
            StringComparer.OrdinalIgnoreCase);
    }

    // ============================================================
    // OBTENER GRUPO POR MATRÍCULA
    // ============================================================

    public string ObtenerGrupoPorMatricula(
        string matricula)
    {
        if (string.IsNullOrWhiteSpace(
                matricula))
        {
            return "S/G";
        }

        if (_configuracion == null)
        {
            return "S/G";
        }

        string matriculaBuscada =
            matricula.Trim();

        return
            _configuracion.Grupos.TryGetValue(
                matriculaBuscada,
                out string? grupo) &&
            !string.IsNullOrWhiteSpace(
                grupo)
                ? grupo.Trim()
                : "S/G";
    }

    // ============================================================
    // NORMALIZAR CLAVE PROFESOR
    // ============================================================

    private static string NormalizarClaveProfesor(
        string? clave)
    {
        if (string.IsNullOrWhiteSpace(
                clave))
        {
            return string.Empty;
        }

        var caracteres =
            clave
                .Trim()
                .Where(
                    char.IsLetterOrDigit)
                .ToArray();

        return
            new string(
                caracteres)
            .ToUpperInvariant();
    }

    // ============================================================
    // PROFESORES
    // ============================================================

    public List<ProfesorConfigurado>
        GetProfesores()
    {
        if (_configuracion == null ||
            _configuracion.Profesores == null)
        {
            return [];
        }

        var resultado =
            new List<ProfesorConfigurado>();

        foreach (var item
                 in _configuracion.Profesores)
        {
            string clave =
                item.Key?.Trim()
                ?? string.Empty;

            var profesor =
                item.Value;

            if (string.IsNullOrWhiteSpace(
                    clave))
            {
                continue;
            }

            if (profesor == null)
            {
                continue;
            }

            // La clave del diccionario es la autoridad.

            profesor.CLAVEPROFESOR =
                clave;

            profesor.EMAIL =
                profesor.EMAIL
                ?.Trim()
                ?? string.Empty;

            resultado.Add(
                profesor);
        }

        return
            resultado
                .OrderBy(
                    p =>
                        p.CLAVEPROFESOR,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();
    }

    // ============================================================
    // BUSCAR PROFESOR
    // ============================================================

    public ProfesorConfigurado?
        GetProfesorPorClave(
            string claveProfesor)
    {
        if (string.IsNullOrWhiteSpace(
                claveProfesor))
        {
            return null;
        }

        if (_configuracion == null ||
            _configuracion.Profesores == null)
        {
            return null;
        }

        string claveBuscada =
            NormalizarClaveProfesor(
                claveProfesor);

        foreach (var item
                 in _configuracion.Profesores)
        {
            string claveDiccionario =
                NormalizarClaveProfesor(
                    item.Key);

            if (claveDiccionario !=
                claveBuscada)
            {
                continue;
            }

            var profesor =
                item.Value;

            if (profesor == null)
            {
                return null;
            }

            profesor.CLAVEPROFESOR =
                item.Key.Trim();

            profesor.EMAIL =
                profesor.EMAIL
                ?.Trim()
                ?? string.Empty;

            return profesor;
        }

        return null;
    }

    // ============================================================
    // CORREO PROFESOR
    // ============================================================

    public string ObtenerCorreoProfesor(
        string claveProfesor)
    {
        return
            GetProfesorPorClave(
                claveProfesor)
            ?.EMAIL
            ?.Trim()
            ?? string.Empty;
    }

    // ============================================================
    // NOMBRE PROFESOR
    // ============================================================
    //
    // Se conserva por compatibilidad.
    //
    // La identificación del profesor NO depende del nombre.
    //
    // ============================================================

    public string ObtenerNombreProfesor(
        string claveProfesor)
    {
        return
            GetProfesorPorClave(
                claveProfesor)
            ?.NOMBREPROFESOR
            ?.Trim()
            ?? string.Empty;
    }

    // ============================================================
    // ENVIAR CORREO
    // ============================================================

    public async Task EnviarCorreoAsync(
        string destinatario,
        string asunto,
        string cuerpo,
        string? archivoAdjunto = null)
    {
        if (string.IsNullOrWhiteSpace(
                destinatario))
        {
            throw new ArgumentException(
                "No se especificó el correo destinatario.");
        }

        if (string.IsNullOrWhiteSpace(
                SmtpUser) ||
            SmtpUser.Contains(
                "TU_CORREO",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Debes configurar SmtpUser en SqliteService.");
        }

        if (string.IsNullOrWhiteSpace(
                SmtpAppPassword) ||
            SmtpAppPassword.Contains(
                "TU_CONTRASENA",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Debes configurar la contraseña de aplicación de Gmail en SqliteService.");
        }

        using var mensaje =
            new MailMessage();

        mensaje.From =
            new MailAddress(
                SmtpUser);

        mensaje.To.Add(
            destinatario.Trim());

        mensaje.Subject =
            asunto?.Trim()
            ?? string.Empty;

        mensaje.Body =
            cuerpo
            ?? string.Empty;

        mensaje.IsBodyHtml =
            false;

        // ========================================================
        // ADJUNTO
        // ========================================================

        if (!string.IsNullOrWhiteSpace(
                archivoAdjunto))
        {
            if (!File.Exists(
                    archivoAdjunto))
            {
                throw new FileNotFoundException(
                    "No se encontró el archivo PDF.",
                    archivoAdjunto);
            }

            mensaje.Attachments.Add(
                new Attachment(
                    archivoAdjunto));
        }

        using var smtp =
            new SmtpClient(
                SmtpHost,
                SmtpPort);

        smtp.EnableSsl =
            SmtpEnableSsl;

        smtp.UseDefaultCredentials =
            false;

        smtp.Credentials =
            new NetworkCredential(
                SmtpUser,
                SmtpAppPassword);

        await smtp.SendMailAsync(
            mensaje);
    }

    // ============================================================
    // ENVIAR CORREO A PROFESOR
    // ============================================================

    public async Task EnviarCorreoProfesorAsync(
        string claveProfesor,
        string asunto,
        string cuerpo,
        string? archivoAdjunto = null)
    {
        var profesor =
            GetProfesorPorClave(
                claveProfesor);

        if (profesor == null)
        {
            throw new InvalidOperationException(
                $"No se encontró el profesor con CLAVEPROFESOR '{claveProfesor}'.");
        }

        if (string.IsNullOrWhiteSpace(
                profesor.EMAIL))
        {
            throw new InvalidOperationException(
                $"El profesor con CLAVEPROFESOR '{claveProfesor}' no tiene correo registrado.");
        }

        await EnviarCorreoAsync(
            profesor.EMAIL,
            asunto,
            cuerpo,
            archivoAdjunto);
    }

    // ============================================================
    // GUARDAR GRUPOS
    // ============================================================
    //
    // IMPORTANTE:
    //
    // Antes solamente se modificaba _configuracion en memoria.
    //
    // AHORA:
    //
    // 1. Modifica _configuracion.
    // 2. Guarda configuracion.bin directamente en la raíz.
    //
    // ============================================================

    public void SaveGrupos(
        Dictionary<string, string> grupos)
    {
        if (grupos == null)
        {
            return;
        }

        if (_configuracion == null)
        {
            _configuracion =
                new ConfiguracionBin();
        }

        if (_configuracion.Grupos == null)
        {
            _configuracion.Grupos =
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase);
        }

        foreach (var item in grupos)
        {
            if (string.IsNullOrWhiteSpace(
                    item.Key) ||
                string.IsNullOrWhiteSpace(
                    item.Value))
            {
                continue;
            }

            _configuracion.Grupos[
                item.Key.Trim()] =
                item.Value.Trim();
        }

        // ========================================================
        // ESCRIBIR CAMBIOS AL ARCHIVO REAL
        // ========================================================

        GuardarConfiguracionBin();
    }

    // ============================================================
    // GUARDAR PROFESORES
    // ============================================================
    //
    // Este método queda disponible por si posteriormente
    // necesitas modificar profesores desde CEIM.
    //
    // ============================================================

    public void SaveProfesores(
        Dictionary<string, ProfesorConfigurado> profesores)
    {
        if (profesores == null)
        {
            return;
        }

        if (_configuracion == null)
        {
            _configuracion =
                new ConfiguracionBin();
        }

        var profesoresNormalizados =
            new Dictionary<
                string,
                ProfesorConfigurado>(
                StringComparer.OrdinalIgnoreCase);

        foreach (var item in profesores)
        {
            if (string.IsNullOrWhiteSpace(
                    item.Key))
            {
                continue;
            }

            if (item.Value == null)
            {
                continue;
            }

            string clave =
                item.Key.Trim();

            item.Value.CLAVEPROFESOR =
                clave;

            item.Value.EMAIL =
                item.Value.EMAIL
                ?.Trim()
                ?? string.Empty;

            profesoresNormalizados[
                clave] =
                item.Value;
        }

        _configuracion.Profesores =
            profesoresNormalizados;

        // ========================================================
        // ESCRIBIR CAMBIOS AL BINARIO REAL
        // ========================================================

        GuardarConfiguracionBin();
    }

    // ============================================================
    // CARGAR CONFIGURACION.BIN
    // ============================================================

    private void CargarConfiguracionBin()
    {
        _configuracion =
            null;

        try
        {
            // ====================================================
            // EL ARCHIVO SE BUSCA DIRECTAMENTE EN LA RAÍZ
            // ====================================================

            if (!File.Exists(
                    _configuracionBinPath))
            {
                return;
            }

            byte[] datos =
                File.ReadAllBytes(
                    _configuracionBinPath);

            int minimo =
                HeaderSize +
                NonceSize +
                TagSize;

            if (datos.Length <= minimo)
            {
                return;
            }

            // ====================================================
            // VALIDAR HEADER CEIM
            // ====================================================

            if (datos[0] != ConfiguracionHeader[0] ||
                datos[1] != ConfiguracionHeader[1] ||
                datos[2] != ConfiguracionHeader[2] ||
                datos[3] != ConfiguracionHeader[3])
            {
                return;
            }

            // ====================================================
            // NONCE
            // ====================================================

            byte[] nonce =
                new byte[NonceSize];

            Buffer.BlockCopy(
                datos,
                HeaderSize,
                nonce,
                0,
                NonceSize);

            // ====================================================
            // TAG
            // ====================================================

            byte[] tag =
                new byte[TagSize];

            Buffer.BlockCopy(
                datos,
                HeaderSize + NonceSize,
                tag,
                0,
                TagSize);

            // ====================================================
            // DATOS CIFRADOS
            // ====================================================

            int encryptedOffset =
                HeaderSize +
                NonceSize +
                TagSize;

            int encryptedLength =
                datos.Length -
                encryptedOffset;

            if (encryptedLength <= 0)
            {
                return;
            }

            byte[] cifrado =
                new byte[
                    encryptedLength];

            Buffer.BlockCopy(
                datos,
                encryptedOffset,
                cifrado,
                0,
                encryptedLength);

            byte[] texto =
                new byte[
                    encryptedLength];

            // ====================================================
            // AES-GCM
            // ====================================================

            using var aes =
                new AesGcm(
                    ConfiguracionKey,
                    TagSize);

            aes.Decrypt(
                nonce,
                cifrado,
                tag,
                texto);

            // ====================================================
            // JSON
            // ====================================================

            string json =
                Encoding.UTF8.GetString(
                    texto);

            // ====================================================
            // DESERIALIZAR
            // ====================================================

            var configuracion =
                JsonSerializer.Deserialize<ConfiguracionBin>(
                    json,
                    _jsonOptions);

            if (configuracion == null)
            {
                return;
            }

            // ====================================================
            // NORMALIZAR GRUPOS
            // ====================================================

            configuracion.Grupos ??=
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase);

            var gruposNormalizados =
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (var item
                     in configuracion.Grupos)
            {
                if (string.IsNullOrWhiteSpace(
                        item.Key))
                {
                    continue;
                }

                string matricula =
                    item.Key.Trim();

                string grupo =
                    item.Value?.Trim()
                    ?? string.Empty;

                if (string.IsNullOrWhiteSpace(
                        grupo))
                {
                    continue;
                }

                gruposNormalizados[
                    matricula] =
                    grupo;
            }

            configuracion.Grupos =
                gruposNormalizados;

            // ====================================================
            // NORMALIZAR PROFESORES
            // ====================================================

            configuracion.Profesores ??=
                new Dictionary<
                    string,
                    ProfesorConfigurado>(
                    StringComparer.OrdinalIgnoreCase);

            var profesoresNormalizados =
                new Dictionary<
                    string,
                    ProfesorConfigurado>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (var item
                     in configuracion.Profesores)
            {
                if (string.IsNullOrWhiteSpace(
                        item.Key))
                {
                    continue;
                }

                var profesor =
                    item.Value;

                if (profesor == null)
                {
                    continue;
                }

                string clave =
                    item.Key.Trim();

                profesor.CLAVEPROFESOR =
                    clave;

                profesor.EMAIL =
                    profesor.EMAIL
                    ?.Trim()
                    ?? string.Empty;

                profesoresNormalizados[
                    clave] =
                    profesor;
            }

            configuracion.Profesores =
                profesoresNormalizados;

            // ====================================================
            // CONFIGURACIÓN FINAL
            // ====================================================

            _configuracion =
                configuracion;
        }
        catch
        {
            // Si configuracion.bin no puede leerse,
            // no se carga ninguna configuración.

            _configuracion =
                null;
        }
    }

    // ============================================================
    // GUARDAR CONFIGURACION.BIN
    // ============================================================
    //
    // ESCRIBE DIRECTAMENTE:
    //
    // AppContext.BaseDirectory\configuracion.bin
    //
    // ============================================================

    private void GuardarConfiguracionBin()
    {
        if (_configuracion == null)
        {
            return;
        }

        try
        {
            // ====================================================
            // ASEGURAR DICCIONARIOS
            // ====================================================

            _configuracion.Grupos ??=
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase);

            _configuracion.Profesores ??=
                new Dictionary<
                    string,
                    ProfesorConfigurado>(
                    StringComparer.OrdinalIgnoreCase);

            // ====================================================
            // SERIALIZAR CONFIGURACIÓN
            // ====================================================

            string json =
                JsonSerializer.Serialize(
                    _configuracion,
                    _jsonOptions);

            byte[] texto =
                Encoding.UTF8.GetBytes(
                    json);

            // ====================================================
            // NONCE NUEVO PARA CADA GUARDADO
            // ====================================================

            byte[] nonce =
                new byte[NonceSize];

            RandomNumberGenerator.Fill(
                nonce);

            // ====================================================
            // CIFRADO
            // ====================================================

            byte[] cifrado =
                new byte[
                    texto.Length];

            byte[] tag =
                new byte[TagSize];

            using (var aes =
                   new AesGcm(
                       ConfiguracionKey,
                       TagSize))
            {
                aes.Encrypt(
                    nonce,
                    texto,
                    cifrado,
                    tag);
            }

            // ====================================================
            // CONSTRUIR ARCHIVO
            //
            // [CEIM]
            // [NONCE]
            // [TAG]
            // [CIFRADO]
            // ====================================================

            int totalLength =
                HeaderSize +
                NonceSize +
                TagSize +
                cifrado.Length;

            byte[] resultado =
                new byte[totalLength];

            // HEADER

            Buffer.BlockCopy(
                ConfiguracionHeader,
                0,
                resultado,
                0,
                HeaderSize);

            // NONCE

            Buffer.BlockCopy(
                nonce,
                0,
                resultado,
                HeaderSize,
                NonceSize);

            // TAG

            Buffer.BlockCopy(
                tag,
                0,
                resultado,
                HeaderSize + NonceSize,
                TagSize);

            // CIFRADO

            Buffer.BlockCopy(
                cifrado,
                0,
                resultado,
                HeaderSize +
                NonceSize +
                TagSize,
                cifrado.Length);

            // ====================================================
            // ARCHIVO TEMPORAL
            // ====================================================
            //
            // Primero escribimos un temporal para evitar dejar
            // configuracion.bin corrupto si el proceso se
            // interrumpe durante la escritura.
            //
            // ====================================================

            string archivoTemporal =
                _configuracionBinPath +
                ".tmp";

            File.WriteAllBytes(
                archivoTemporal,
                resultado);

            // ====================================================
            // REEMPLAZAR ARCHIVO REAL
            // ====================================================

            if (File.Exists(
                    _configuracionBinPath))
            {
                File.Delete(
                    _configuracionBinPath);
            }

            File.Move(
                archivoTemporal,
                _configuracionBinPath);
        }
        catch
        {
            // Intentar limpiar temporal.

            try
            {
                string archivoTemporal =
                    _configuracionBinPath +
                    ".tmp";

                if (File.Exists(
                        archivoTemporal))
                {
                    File.Delete(
                        archivoTemporal);
                }
            }
            catch
            {
                // Ignorar limpieza.
            }

            throw;
        }
    }

    // ============================================================
    // FORZAR RECARGA DE CONFIGURACION.BIN
    // ============================================================
    //
    // Útil si otro proceso modifica configuracion.bin mientras
    // CEIM está abierto.
    //
    // ============================================================

    public void RecargarConfiguracion()
    {
        CargarConfiguracionBin();
    }

    // ============================================================
    // RUTA REAL DE CONFIGURACION.BIN
    // ============================================================

    public string ObtenerRutaConfiguracionBin()
    {
        return _configuracionBinPath;
    }

    // ============================================================
    // RUTA REAL DE SQLITE
    // ============================================================

    public string ObtenerRutaBaseDatos()
    {
        return _dbPath;
    }

    // ============================================================
    // DISPOSE
    // ============================================================

    public void Dispose()
    {
        _conn?.Dispose();
    }

    // ============================================================
    // MODELO CONFIGURACION.BIN
    // ============================================================

    private class ConfiguracionBin
    {
        public Dictionary<string, string> Grupos
        {
            get;
            set;
        } =
            new(
                StringComparer.OrdinalIgnoreCase);

        public Dictionary<
            string,
            ProfesorConfigurado>
            Profesores
        {
            get;
            set;
        } =
            new(
                StringComparer.OrdinalIgnoreCase);
    }

    // ============================================================
    // PROFESOR CONFIGURADO
    // ============================================================

    public class ProfesorConfigurado
    {
        public string CLAVEPROFESOR
        {
            get;
            set;
        } =
            string.Empty;

        public string EMAIL
        {
            get;
            set;
        } =
            string.Empty;

        public string NOMBREPROFESOR
        {
            get;
            set;
        } =
            string.Empty;

        public string TextoCombo =>
            string.IsNullOrWhiteSpace(
                EMAIL)
                ? CLAVEPROFESOR
                : $"{CLAVEPROFESOR} | {EMAIL}";
    }
}