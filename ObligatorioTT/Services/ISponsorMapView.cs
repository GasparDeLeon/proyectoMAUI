namespace ObligatorioTT.Services
{
    public interface ISponsorMapView
    {
       
        Task MostrarPinesAsync(IEnumerable<SponsorPin> sponsors);

        Task LimpiarPinesAsync();

        Task AjustarVistaAsync();
    }

    public sealed class SponsorPin
    {
        public string Nombre { get; init; } = string.Empty;
        public string? Direccion { get; init; }
        public double? Lat { get; init; }
        public double? Lng { get; init; }
    }
}
