using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LiteDB;
using Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Models;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;

public class LiteDbService : IDisposable
{
    private readonly string _dataFolder;
    private readonly string _dbPath;
    private readonly string _configuracionBinPath;
    private readonly LiteDatabase _db;

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

    private const string SmtpAppPassword =
        "qiwv kanr twxa arxk";

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
    // CONSTRUCTOR
    // ============================================================

    public LiteDbService()
    {
        string baseDir =
            string.IsNullOrWhiteSpace(
                GlobalSettings.CurrentCapDirectory)
                ? AppContext.BaseDirectory
                : GlobalSettings.CurrentCapDirectory;

        string normalizedBaseDir =
            baseDir.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);

        if (normalizedBaseDir.EndsWith(
                "Data",
                StringComparison.OrdinalIgnoreCase))
        {
            _dataFolder =
                normalizedBaseDir;
        }
        else
        {
            _dataFolder =
                Path.Combine(
                    normalizedBaseDir,
                    "Data");
        }

        if (!Directory.Exists(
                _dataFolder))
        {
            Directory.CreateDirectory(
                _dataFolder);
        }

        _dbPath =
            Path.GetFullPath(
                Path.Combine(
                    _dataFolder,
                    "parciales.db"));

        _configuracionBinPath =
            Path.GetFullPath(
                Path.Combine(
                    _dataFolder,
                    "configuracion.bin"));

        _db =
            new LiteDatabase(
                _dbPath);

        CargarConfiguracionBin();
    }

    // ============================================================
    // PARCIALES
    // ============================================================

    public IEnumerable<(string Key, MateriaParcial Value)>
        GetAllParciales()
    {
        var col =
            _db.GetCollection<MateriaParcialRecord>(
                "Parciales");

        foreach (var doc in col.FindAll())
        {
            yield return (
                doc.Id,
                doc.Data);
        }
    }

    public MateriaParcial? GetMateria(
        string clave)
    {
        if (string.IsNullOrWhiteSpace(
                clave))
        {
            return null;
        }

        var col =
            _db.GetCollection<MateriaParcialRecord>(
                "Parciales");

        var record =
            col.FindById(
                clave);

        return record?.Data;
    }

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

        var col =
            _db.GetCollection<MateriaParcialRecord>(
                "Parciales");

        var record =
            new MateriaParcialRecord
            {
                Id =
                    clave,

                Data =
                    materia
            };

        col.Upsert(
            record);
    }

    // ============================================================
    // CONFIGURACIONES
    // ============================================================

    public IEnumerable<(
        string Key,
        ConfiguracionParciales Value)>
        GetAllConfiguraciones()
    {
        var col =
            _db.GetCollection<ConfiguracionRecord>(
                "Configuraciones");

        foreach (var doc in col.FindAll())
        {
            yield return (
                doc.Id,
                doc.Data);
        }
    }

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

        var col =
            _db.GetCollection<ConfiguracionRecord>(
                "Configuraciones");

        var record =
            new ConfiguracionRecord
            {
                Id =
                    clave,

                Data =
                    cfg
            };

        col.Upsert(
            record);
    }

    // ============================================================
    // GRUPOS
    // ============================================================

    public Dictionary<string, string> GetGrupos()
    {
        if (_configuracion == null)
        {
            return
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase);
        }

        return
            new Dictionary<string, string>(
                _configuracion.Grupos,
                StringComparer.OrdinalIgnoreCase);
    }

    public string ObtenerGrupoPorMatricula(
        string matricula)
    {
        if (string.IsNullOrWhiteSpace(
                matricula))
        {
            return "S/G";
        }

        if (_configuracion == null)
            return "S/G";

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
    //
    // IMPORTANTE:
    //
    // CONFIGURACION_CEIM guarda Profesores como:
    //
    // Dictionary<string, Profesor>
    //
    // donde:
    //
    //   KEY                = CLAVEPROFESOR
    //   VALUE.Email        = correo
    //
    // NO usamos ninguna columna C.
    //
    // El nombre del maestro utilizado por CEIM para el reporte
    // continúa viniendo del CAP.
    //
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
            // No dependemos de una CLAVEPROFESOR almacenada
            // dentro del objeto Profesor.

            profesor.CLAVEPROFESOR =
                clave;

            profesor.EMAIL =
                profesor.EMAIL
                ?.Trim()
                ?? string.Empty;

            // NO se utiliza NOMBREPROFESOR
            // para identificar al docente.

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
                return null;

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
    // No se obtiene de CONFIG_DOCENTE.
    //
    // Este método se conserva por compatibilidad.
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
                "Debes configurar SmtpUser en LiteDbService.");
        }

        if (string.IsNullOrWhiteSpace(
                SmtpAppPassword) ||
            SmtpAppPassword.Contains(
                "TU_CONTRASENA",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Debes configurar la contraseña de aplicación de Gmail en LiteDbService.");
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
    // ENVIAR A PROFESOR
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
    // SAVE GRUPOS
    // ============================================================

    public void SaveGrupos(
        Dictionary<string, string> grupos)
    {
        if (grupos == null)
            return;

        if (_configuracion == null)
        {
            _configuracion =
                new ConfiguracionBin();
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
            if (!File.Exists(
                    _configuracionBinPath))
            {
                return;
            }

            byte[] datos =
                File.ReadAllBytes(
                    _configuracionBinPath);

            const int HeaderSize =
                4;

            const int NonceSize =
                12;

            const int TagSize =
                16;

            int minimo =
                HeaderSize +
                NonceSize +
                TagSize;

            if (datos.Length <= minimo)
                return;

            // ----------------------------------------------------
            // HEADER CEIM
            // ----------------------------------------------------

            if (datos[0] != 0x43 ||
                datos[1] != 0x45 ||
                datos[2] != 0x49 ||
                datos[3] != 0x4D)
            {
                return;
            }

            // ----------------------------------------------------
            // NONCE
            // ----------------------------------------------------

            byte[] nonce =
                new byte[NonceSize];

            Buffer.BlockCopy(
                datos,
                HeaderSize,
                nonce,
                0,
                NonceSize);

            // ----------------------------------------------------
            // TAG
            // ----------------------------------------------------

            byte[] tag =
                new byte[TagSize];

            Buffer.BlockCopy(
                datos,
                HeaderSize + NonceSize,
                tag,
                0,
                TagSize);

            // ----------------------------------------------------
            // CIFRADO
            // ----------------------------------------------------

            int encryptedOffset =
                HeaderSize +
                NonceSize +
                TagSize;

            int encryptedLength =
                datos.Length -
                encryptedOffset;

            if (encryptedLength <= 0)
                return;

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

            // ----------------------------------------------------
            // AES-GCM
            // ----------------------------------------------------

            using var aes =
                new AesGcm(
                    ConfiguracionKey,
                    TagSize);

            aes.Decrypt(
                nonce,
                cifrado,
                tag,
                texto);

            string json =
                Encoding.UTF8.GetString(
                    texto);

            // ----------------------------------------------------
            // DESERIALIZAR
            // ----------------------------------------------------
            //
            // La estructura REAL producida por CONFIGURACION_CEIM
            // es:
            //
            // Grupos     -> Dictionary<string,string>
            // Profesores -> Dictionary<string,Profesor>
            //
            // ----------------------------------------------------

            var configuracion =
                System.Text.Json.JsonSerializer
                    .Deserialize<ConfiguracionBin>(
                        json,
                        _jsonOptions);

            if (configuracion == null)
                return;

            // ----------------------------------------------------
            // GRUPOS
            // ----------------------------------------------------

            configuracion.Grupos ??=
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase);

            configuracion.Grupos =
                new Dictionary<string, string>(
                    configuracion.Grupos,
                    StringComparer.OrdinalIgnoreCase);

            // Normalizar matrículas
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

            // ----------------------------------------------------
            // PROFESORES
            // ----------------------------------------------------

            configuracion.Profesores ??=
                new Dictionary<
                    string,
                    ProfesorConfigurado>(
                    StringComparer.OrdinalIgnoreCase);

            configuracion.Profesores =
                new Dictionary<
                    string,
                    ProfesorConfigurado>(
                    configuracion.Profesores,
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
                    continue;

                string clave =
                    item.Key.Trim();

                profesor.CLAVEPROFESOR =
                    clave;

                profesor.EMAIL =
                    profesor.EMAIL
                    ?.Trim()
                    ?? string.Empty;

                // NO usamos NOMBREPROFESOR.
                // El nombre viene del CAP.

                profesoresNormalizados[
                    clave] =
                    profesor;
            }

            configuracion.Profesores =
                profesoresNormalizados;

            _configuracion =
                configuracion;
        }
        catch
        {
            // IMPORTANTE:
            //
            // Si aquí falla la configuración,
            // no hay grupos ni docentes disponibles.
            //
            // El fallo anterior ocurría precisamente
            // porque Profesores estaba declarado como List
            // cuando realmente es Dictionary.
            //
            _configuracion =
                null;
        }
    }

    // ============================================================
    // DISPOSE
    // ============================================================

    public void Dispose()
    {
        _db.Dispose();
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
    // PROFESOR
    // ============================================================
    //
    // El JSON realmente viene del modelo Profesor de
    // CONFIGURACION_CEIM.
    //
    // Solo nos interesan:
    //
    //   ClaveProfesor
    //   Email
    //
    // NOMBREPROFESOR NO participa.
    //
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

        // Se mantiene únicamente por compatibilidad
        // con el resto de CEIM.
        //
        // El nombre mostrado del profesor NO se obtiene
        // de CONFIG_DOCENTE.
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

    // ============================================================
    // RECORDS LITEDB
    // ============================================================

    private class MateriaParcialRecord
    {
        [BsonId]
        public string Id { get; set; } =
            string.Empty;

        public MateriaParcial Data { get; set; } =
            new();
    }

    private class ConfiguracionRecord
    {
        [BsonId]
        public string Id { get; set; } =
            string.Empty;

        public ConfiguracionParciales Data { get; set; } =
            new();
    }
}