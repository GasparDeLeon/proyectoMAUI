using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Maui.Storage;
using SQLite;
using ObligatorioTT.Models;

namespace ObligatorioTT.Services
{
    public class SponsorRepository
    {
        private static readonly Lazy<SponsorRepository> _inst = new(() => new SponsorRepository());
        public static SponsorRepository Inst => _inst.Value;

        private SQLiteAsyncConnection _db;
        private const string DbName = "app_v2.db3";

        private SponsorRepository() { }

        public async Task InitAsync()
        {
            if (_db != null) return;

            var path = Path.Combine(FileSystem.AppDataDirectory, DbName);
            _db = new SQLiteAsyncConnection(path);

            // Crea la tabla si no existe (no borra datos si existe)
            await _db.CreateTableAsync<Sponsor>();

            // 🔒 Asegura que existan las columnas Latitud / Longitud si la DB es vieja
            await EnsureSponsorColumnsAsync();
        }

        public Task<List<Sponsor>> GetAllAsync(string filtro = null)
        {
            var table = _db.Table<Sponsor>();
            if (!string.IsNullOrWhiteSpace(filtro))
                return table.Where(s => s.Nombre.Contains(filtro))
                            .OrderBy(s => s.Nombre).ToListAsync();

            return table.OrderBy(s => s.Nombre).ToListAsync();
        }

        public Task<Sponsor> GetAsync(int id) => _db.FindAsync<Sponsor>(id);
        public Task<int> InsertAsync(Sponsor s) => _db.InsertAsync(s);
        public Task<int> UpdateAsync(Sponsor s) => _db.UpdateAsync(s);
        public Task<int> DeleteAsync(Sponsor s) => _db.DeleteAsync(s);

        // ===== Mini-migración para columnas nuevas =====
        private async Task EnsureSponsorColumnsAsync()
        {
            // Lee el esquema actual de la tabla Sponsor
            var tableInfo = await _db.QueryAsync<TableInfo>("PRAGMA table_info(Sponsor);");

            bool hasLat = tableInfo.Any(c =>
                c.name.Equals("Latitud", StringComparison.OrdinalIgnoreCase));
            bool hasLng = tableInfo.Any(c =>
                c.name.Equals("Longitud", StringComparison.OrdinalIgnoreCase));

            // Agrega columnas si faltan (tipo REAL para double en SQLite)
            if (!hasLat)
                await _db.ExecuteAsync("ALTER TABLE Sponsor ADD COLUMN Latitud REAL;");

            if (!hasLng)
                await _db.ExecuteAsync("ALTER TABLE Sponsor ADD COLUMN Longitud REAL;");
        }

        // Estructura para mapear PRAGMA table_info
        private class TableInfo
        {
            public int cid { get; set; }
            public string name { get; set; }
            public string type { get; set; }
            public int notnull { get; set; }
            public string dflt_value { get; set; }
            public int pk { get; set; }
        }
    }
}
