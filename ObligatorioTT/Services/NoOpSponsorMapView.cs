using System.Collections.Generic;
using System.Threading.Tasks;

namespace ObligatorioTT.Services
{

    public class NoOpSponsorMapView : ISponsorMapView
    {
        public Task MostrarPinesAsync(IEnumerable<SponsorPin> sponsors) => Task.CompletedTask;

        public Task LimpiarPinesAsync() => Task.CompletedTask;

        public Task AjustarVistaAsync() => Task.CompletedTask;
    }
}
