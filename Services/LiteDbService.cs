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
    //
    // CAMBIA ÚNICAMENTE ESTOS DOS VALORES.
    //
    // SmtpUser:
    //     cuenta Gmail desde la que CEIM enviará.
    //
    // SmtpAppPassword:
    //     contraseña de aplicación de Google.
    //
    // ============================================================

    private const string SmtpHost =
        "smtp.gmail.com";

    private const int SmtpPort =
        587;

    private const bool SmtpEnableSsl =
        true;

    private const string SmtpUser =
        "TU_CORREO@gmail.com";

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
                Id = clave,
                Data = materia
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
                Id = clave,
                Data = cfg
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
            return new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);
        }

        return new Dictionary<string, string>(
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

        return
            _configuracion.Grupos.TryGetValue(
                matricula.Trim(),
                out string? grupo) &&
            !string.IsNullOrWhiteSpace(
                grupo)
                ? grupo
                : "S/G";
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

        return
            _configuracion
                .Profesores
                .Where(p =>
                    p != null &&
                    !string.IsNullOrWhiteSpace(
                        p.CLAVEPROFESOR))
                .OrderBy(
                    p => p.NOMBREPROFESOR)
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

        string clave =
            claveProfesor.Trim();

        return
            _configuracion
                .Profesores
                .FirstOrDefault(
                    p =>
                        string.Equals(
                            p.CLAVEPROFESOR?.Trim(),
                            clave,
                            StringComparison.OrdinalIgnoreCase));
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
                $"El profesor '{profesor.NOMBREPROFESOR}' no tiene correo registrado.");
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
        _configuracion = null;

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

            const int HeaderSize = 4;
            const int NonceSize = 12;
            const int TagSize = 16;

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
                new byte[encryptedLength];

            Buffer.BlockCopy(
                datos,
                encryptedOffset,
                cifrado,
                0,
                encryptedLength);

            byte[] texto =
                new byte[encryptedLength];

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

            // ----------------------------------------------------
            // PROFESORES
            // ----------------------------------------------------

            configuracion.Profesores ??=
                [];

            configuracion.Profesores =
                configuracion
                    .Profesores
                    .Where(
                        p => p != null)
                    .Select(
                        p =>
                        {
                            p.CLAVEPROFESOR =
                                p.CLAVEPROFESOR
                                ?.Trim()
                                ?? string.Empty;

                            p.NOMBREPROFESOR =
                                p.NOMBREPROFESOR
                                ?.Trim()
                                ?? string.Empty;

                            p.EMAIL =
                                p.EMAIL
                                ?.Trim()
                                ?? string.Empty;

                            return p;
                        })
                    .Where(
                        p =>
                            !string.IsNullOrWhiteSpace(
                                p.CLAVEPROFESOR))
                    .ToList();

            _configuracion =
                configuracion;
        }
        catch
        {
            _configuracion = null;
        }
    }

    // ============================================================
    // DISPOSE
    // ============================================================

    public void Dispose()
    {
        _db?.Dispose();
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

        public List<ProfesorConfigurado> Profesores
        {
            get;
            set;
        } =
            [];
    }

    // ============================================================
    // PROFESOR
    // ============================================================

    public class ProfesorConfigurado
    {
        public string CLAVEPROFESOR
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

        public string EMAIL
        {
            get;
            set;
        } =
            string.Empty;

        public string TextoCombo =>
            string.IsNullOrWhiteSpace(
                EMAIL)
                ? NOMBREPROFESOR
                : $"{NOMBREPROFESOR} | {EMAIL}";
    }

    // ============================================================
    // LITEDB RECORDS
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