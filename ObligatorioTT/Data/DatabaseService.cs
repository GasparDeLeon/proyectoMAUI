using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using ObligatorioTT.Models;

namespace ObligatorioTT.Data
{
    public class DatabaseService
    {
        private readonly SQLiteAsyncConnection _db;

        public DatabaseService(string dbPath)
        {
            _db = new SQLiteAsyncConnection(dbPath);
        }

        public async Task InitAsync()
        {
            await _db.CreateTableAsync<Usuario>();
        }

        
        public Task<int> InsertUsuarioAsync(Usuario u) => _db.InsertAsync(u);
        public Task<int> UpdateUsuarioAsync(Usuario u) => _db.UpdateAsync(u);
        public Task<int> DeleteUsuarioAsync(Usuario u) => _db.DeleteAsync(u);

        public Task<Usuario?> GetUsuarioByUserAsync(string userName) =>
            _db.Table<Usuario>().Where(x => x.UserName == userName).FirstOrDefaultAsync();

        public Task<List<Usuario>> GetUsuariosAsync() =>
            _db.Table<Usuario>().ToListAsync();
    }
}
