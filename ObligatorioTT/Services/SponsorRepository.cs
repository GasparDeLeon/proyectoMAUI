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
            await _db.CreateTableAsync<Sponsor>();
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
    }
}
