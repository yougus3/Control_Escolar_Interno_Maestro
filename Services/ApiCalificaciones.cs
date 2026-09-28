using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Registro_de_Calificaciones_Jose_Ma._Morelos_y_Pavon.Services;

public class ApiCalificaciones
{
    private const string BaseUrl =
        "https://api.prefecotemixco.edu.mx";

    private readonly HttpClient _httpClient;

    private readonly string _configuracionBinPath;

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

    private const int HeaderSize = 4;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private static readonly byte[] ConfiguracionHeader =
    [
        0x43,
        0x45,
        0x49,
        0x4D
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiCalificaciones()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl)
        };

        _configuracionBinPath =
            Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "Data",
                    "configuracion.bin"));
    }

    public async Task<RespuestaApiCalificaciones>
        EnviarCalificacionesAsync(
            object datosCalificaciones)
    {
        string accessToken =
            await ObtenerAccessTokenAsync();

        string json =
            JsonSerializer.Serialize(
                datosCalificaciones,
                JsonOptions);

        using var contenido =
            new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

        using var solicitud =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/v1/migracion/calificaciones");

        solicitud.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        solicitud.Content =
            contenido;

        using HttpResponseMessage respuesta =
            await _httpClient.SendAsync(
                solicitud);

        string respuestaJson =
            await respuesta.Content.ReadAsStringAsync();

        RespuestaApiCalificaciones? resultado =
            JsonSerializer.Deserialize<RespuestaApiCalificaciones>(
                respuestaJson,
                JsonOptions);

        if (resultado == null)
        {
            throw new InvalidOperationException(
                "La API devolvió una respuesta JSON no válida.");
        }

        resultado.CodigoHttp =
            (int)respuesta.StatusCode;

        resultado.JsonRespuesta =
            respuestaJson;

        return resultado;
    }

    private async Task<string>
        ObtenerAccessTokenAsync()
    {
        MigracionWebConfig credenciales =
            CargarCredencialesMigracion();

        if (string.IsNullOrWhiteSpace(
                credenciales.Usuario))
        {
            throw new InvalidOperationException(
                "No está configurado el usuario de migración en configuracion.bin.");
        }

        if (string.IsNullOrWhiteSpace(
                credenciales.Contrasena))
        {
            throw new InvalidOperationException(
                "No está configurada la contraseña de migración en configuracion.bin.");
        }

        var datosLogin = new
        {
            usuario = credenciales.Usuario,
            password = credenciales.Contrasena
        };

        string jsonLogin =
            JsonSerializer.Serialize(
                datosLogin,
                JsonOptions);

        using var contenido =
            new StringContent(
                jsonLogin,
                Encoding.UTF8,
                "application/json");

        using HttpResponseMessage respuesta =
            await _httpClient.PostAsync(
                "/api/v1/auth/login",
                contenido);

        string respuestaJson =
            await respuesta.Content.ReadAsStringAsync();

        if (!respuesta.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Error al iniciar sesión en la API. " +
                $"HTTP {(int)respuesta.StatusCode}: {respuestaJson}");
        }

        LoginResponse? login =
            JsonSerializer.Deserialize<LoginResponse>(
                respuestaJson,
                JsonOptions);

        if (login == null ||
            string.IsNullOrWhiteSpace(
                login.AccessToken))
        {
            throw new InvalidOperationException(
                "La API de autenticación no devolvió un accessToken válido.");
        }

        return login.AccessToken;
    }

    private MigracionWebConfig
        CargarCredencialesMigracion()
    {
        if (!File.Exists(
                _configuracionBinPath))
        {
            throw new FileNotFoundException(
                "No se encontró configuracion.bin. " +
                "Debes cargar el archivo de configuración en la carpeta Data.",
                _configuracionBinPath);
        }

        try
        {
            byte[] datos =
                File.ReadAllBytes(
                    _configuracionBinPath);

            int minimo =
                HeaderSize +
                NonceSize +
                TagSize;

            if (datos.Length <= minimo)
            {
                throw new InvalidDataException(
                    "configuracion.bin no contiene datos válidos.");
            }

            if (datos[0] != ConfiguracionHeader[0] ||
                datos[1] != ConfiguracionHeader[1] ||
                datos[2] != ConfiguracionHeader[2] ||
                datos[3] != ConfiguracionHeader[3])
            {
                throw new InvalidDataException(
                    "configuracion.bin no tiene un encabezado válido.");
            }

            byte[] nonce =
                new byte[NonceSize];

            Buffer.BlockCopy(
                datos,
                HeaderSize,
                nonce,
                0,
                NonceSize);

            byte[] tag =
                new byte[TagSize];

            Buffer.BlockCopy(
                datos,
                HeaderSize + NonceSize,
                tag,
                0,
                TagSize);

            int encryptedOffset =
                HeaderSize +
                NonceSize +
                TagSize;

            int encryptedLength =
                datos.Length -
                encryptedOffset;

            if (encryptedLength <= 0)
            {
                throw new InvalidDataException(
                    "configuracion.bin no contiene contenido cifrado.");
            }

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

            ConfiguracionBin? configuracion =
                JsonSerializer.Deserialize<ConfiguracionBin>(
                    json,
                    JsonOptions);

            if (configuracion == null)
            {
                throw new InvalidDataException(
                    "No fue posible interpretar configuracion.bin.");
            }

            if (configuracion.MigracionWeb == null)
            {
                throw new InvalidDataException(
                    "configuracion.bin no contiene configuración de migración web.");
            }

            configuracion.MigracionWeb.Usuario =
                configuracion.MigracionWeb.Usuario?.Trim()
                ?? string.Empty;

            configuracion.MigracionWeb.Contrasena =
                configuracion.MigracionWeb.Contrasena
                ?? string.Empty;

            return configuracion.MigracionWeb;
        }
        catch (CryptographicException ex)
        {
            throw new InvalidDataException(
                "No fue posible descifrar configuracion.bin. " +
                "Verifica que el archivo haya sido generado por CONFIGURACION CEIM.",
                ex);
        }
    }

    private sealed class ConfiguracionBin
    {
        public Dictionary<string, string> Grupos { get; set; } =
            new();

        public Dictionary<string, ProfesorConfigurado> Profesores
        {
            get;
            set;
        } = new();

        public List<EvaluacionAdicional> PRE { get; set; } =
            new();

        public List<EvaluacionAdicional> EXTRA { get; set; } =
            new();

        public SmtpConfig? SMTP { get; set; }

        public MigracionWebConfig? MigracionWeb { get; set; }
    }

    private sealed class SmtpConfig
    {
        public string Servidor { get; set; } =
            string.Empty;

        public int Puerto { get; set; }

        public string Usuario { get; set; } =
            string.Empty;

        public string Contrasena { get; set; } =
            string.Empty;

        public bool SSL { get; set; }
    }

    private sealed class MigracionWebConfig
    {
        public string Usuario { get; set; } =
            string.Empty;

        public string Contrasena { get; set; } =
            string.Empty;
    }

    private sealed class LoginResponse
    {
        [JsonPropertyName("accessToken")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refreshToken")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("usuario")]
        public string? Usuario { get; set; }

        [JsonPropertyName("nombre")]
        public string? Nombre { get; set; }

        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("tipoUsuario")]
        public string? TipoUsuario { get; set; }

        [JsonPropertyName("bloqueado")]
        public bool Bloqueado { get; set; }
    }

    public sealed class RespuestaApiCalificaciones
    {
        [JsonPropertyName("aplicadas")]
        public int Aplicadas { get; set; }

        [JsonPropertyName("omitidas")]
        public int Omitidas { get; set; }

        [JsonPropertyName("detalle")]
        public List<DetalleCalificacion> Detalle { get; set; } = [];

        [JsonIgnore]
        public int CodigoHttp { get; internal set; }

        [JsonIgnore]
        public string JsonRespuesta { get; internal set; } =
            string.Empty;

        [JsonIgnore]
        public bool EsExitosa =>
            Aplicadas > 0 &&
            Omitidas == 0 &&
            Detalle.Count == 0;
    }

    public sealed class DetalleCalificacion
    {
        [JsonPropertyName("matricula")]
        public string? Matricula { get; set; }

        [JsonPropertyName("claveMateria")]
        public string? ClaveMateria { get; set; }

        [JsonPropertyName("periodo")]
        public string? Periodo { get; set; }

        [JsonPropertyName("motivo")]
        public string? Motivo { get; set; }
    }

    private sealed class EvaluacionAdicional
    {
        public string CLAVEASIGNATURA { get; set; } =
            string.Empty;

        public string MATRICULA { get; set; } =
            string.Empty;
    }

    private sealed class ProfesorConfigurado
    {
        public string CLAVEPROFESOR { get; set; } =
            string.Empty;

        public string EMAIL { get; set; } =
            string.Empty;

        public string NOMBREPROFESOR { get; set; } =
            string.Empty;
    }
}