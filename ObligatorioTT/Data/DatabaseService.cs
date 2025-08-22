using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SQLite;
using ObligatorioTT.Models;

namespace ObligatorioTT.Data
{
    public class DatabaseService
    {
        private readonly SQLiteAsyncConnection _db;

        // Se inicializa una sola vez y todos los métodos esperan a esto.
        private readonly Task _ensureInitTask;

        public DatabaseService(string dbPath)
        {
            _db = new SQLiteAsyncConnection(dbPath);
            _ensureInitTask = EnsureCreatedAndMigrationsAsync();
        }

        /// Crea tablas y deja Email normalizado + único (idempotente).
        private async Task EnsureCreatedAndMigrationsAsync()
        {
            // Crea la tabla según el mapeo de la clase (Usuario o [Table("Usuarios")])
            await _db.CreateTableAsync<Usuario>();

            // Normalización e índice (probará con nombre singular y plural, sin romper)
            await NormalizeAndIndexEmailsAsync();
        }

        private async Task NormalizeAndIndexEmailsAsync()
        {
            // Ejecuta SQL ignorando "table not found" para dos posibles nombres
            async Task SafeExecAsync(string sql)
            {
                try { await _db.ExecuteAsync(sql); }
                catch (SQLiteException) { /* ignorar si la tabla con ese nombre no existe */ }
            }

            // SINGULAR
            await SafeExecAsync("UPDATE Usuario   SET Email = trim(Email) WHERE Email IS NOT NULL;");
            await SafeExecAsync("UPDATE Usuario   SET Email = lower(Email) WHERE Email IS NOT NULL;");
            await SafeExecAsync(@"
                DELETE FROM Usuario
                WHERE Email IS NOT NULL AND Email <> ''
                  AND Id NOT IN (
                    SELECT MIN(Id)
                    FROM Usuario
                    WHERE Email IS NOT NULL AND Email <> ''
                    GROUP BY Email
                  );");
            await SafeExecAsync(@"CREATE UNIQUE INDEX IF NOT EXISTS UX_Usuario_Email ON Usuario (Email);");

            // PLURAL (por si el modelo tiene [Table("Usuarios")])
            await SafeExecAsync("UPDATE Usuarios  SET Email = trim(Email) WHERE Email IS NOT NULL;");
            await SafeExecAsync("UPDATE Usuarios  SET Email = lower(Email) WHERE Email IS NOT NULL;");
            await SafeExecAsync(@"
                DELETE FROM Usuarios
                WHERE Email IS NOT NULL AND Email <> ''
                  AND Id NOT IN (
                    SELECT MIN(Id)
                    FROM Usuarios
                    WHERE Email IS NOT NULL AND Email <> ''
                    GROUP BY Email
                  );");
            await SafeExecAsync(@"CREATE UNIQUE INDEX IF NOT EXISTS UX_Usuarios_Email ON Usuarios (Email);");
        }

        /// Normalización previa a guardar
        private static void NormalizeUsuario(Usuario u)
        {
            u.UserName = (u.UserName ?? string.Empty).Trim();
            u.Email = (u.Email ?? string.Empty).Trim().ToLowerInvariant();
            u.NombreCompleto = (u.NombreCompleto ?? string.Empty).Trim();
            u.Direccion = (u.Direccion ?? string.Empty).Trim();
            u.Telefono = (u.Telefono ?? string.Empty).Trim();
            u.FotoPath = (u.FotoPath ?? string.Empty).Trim();
            u.Password = u.Password ?? string.Empty;
        }

        // ========================= CRUD =========================

        /// Inserta con validación de email único (recomendado)
        public async Task<(bool ok, string? error)> RegistrarUsuarioAsync(Usuario u)
        {
            await _ensureInitTask;           // <-- garantiza tabla lista
            NormalizeUsuario(u);

            if (string.IsNullOrWhiteSpace(u.Email))
                return (false, "El email es obligatorio.");

            var existente = await GetUsuarioByEmailAsync(u.Email);
            if (existente != null)
                return (false, "El email ya está registrado.");

            try
            {
                await _db.InsertAsync(u);
                return (true, null);
            }
            catch (SQLiteException ex) when (ex.Result == SQLite3.Result.Constraint || ex.Message.Contains("constraint", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "El email ya está registrado.");
            }
            catch (Exception ex)
            {
                return (false, $"Error al registrar: {ex.Message}");
            }
        }

        /// Firma vieja: no lanza excepción si falla (devuelve 0)
        public async Task<int> InsertUsuarioAsync(Usuario u)
        {
            var (ok, error) = await RegistrarUsuarioAsync(u);
            if (!ok)
            {
                System.Diagnostics.Debug.WriteLine($"Registro fallido: {error}");
                return 0;
            }
            return 1; // ya insertado por RegistrarUsuarioAsync
        }

        /// Actualiza un usuario validando que el email no choque con otro usuario
        public async Task<(bool ok, string? error)> ActualizarUsuarioAsync(Usuario u)
        {
            await _ensureInitTask;           // <-- garantiza tabla lista
            NormalizeUsuario(u);

            if (string.IsNullOrWhiteSpace(u.Email))
                return (false, "El email es obligatorio.");

            var colision = await GetUsuarioByEmailAsync(u.Email);
            if (colision != null && colision.Id != u.Id)
                return (false, "Ese email ya pertenece a otro usuario.");

            try
            {
                await _db.UpdateAsync(u);
                return (true, null);
            }
            catch (SQLiteException ex) when (ex.Result == SQLite3.Result.Constraint || ex.Message.Contains("constraint", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Ese email ya pertenece a otro usuario.");
            }
            catch (Exception ex)
            {
                return (false, $"Error al actualizar: {ex.Message}");
            }
        }

        /// Firma vieja: no lanza excepción si falla (devuelve 0)
        public async Task<int> UpdateUsuarioAsync(Usuario u)
        {
            var (ok, error) = await ActualizarUsuarioAsync(u);
            if (!ok)
            {
                System.Diagnostics.Debug.WriteLine($"Actualización fallida: {error}");
                return 0;
            }
            return 1;
        }

        public async Task<int> DeleteUsuarioAsync(Usuario u)
        {
            await _ensureInitTask;
            return await _db.DeleteAsync(u);
        }

        // ========================= Consultas =========================

        public async Task<Usuario?> GetUsuarioByUserAsync(string userName)
        {
            await _ensureInitTask;           // <-- garantiza tabla lista
            var u = (userName ?? string.Empty).Trim();
            return await _db.Table<Usuario>()
                            .Where(x => x.UserName == u)
                            .FirstOrDefaultAsync();
        }

        public async Task<Usuario?> GetUsuarioByEmailAsync(string emailNormalizado)
        {
            await _ensureInitTask;           // <-- garantiza tabla lista
            var e = (emailNormalizado ?? string.Empty).Trim().ToLowerInvariant();
            return await _db.Table<Usuario>()
                            .Where(x => x.Email == e)
                            .FirstOrDefaultAsync();
        }

        public async Task<List<Usuario>> GetUsuariosAsync()
        {
            await _ensureInitTask;           // <-- garantiza tabla lista
            return await _db.Table<Usuario>().ToListAsync();
        }

        // ====== Compatibilidad con tu API pública previa ======
        public Task InitAsync() => _ensureInitTask; // por si la llamaban desde App.xaml.cs
    }
}