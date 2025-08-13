using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;

namespace ObligatorioTT.Services
{
    public interface IBiometricAuthService
    {
        Task<bool> IsAvailableAsync();
        Task<bool> AuthenticateAsync(string reason = "Ingresar con huella/biometría");
    }

    public class BiometricAuthService : IBiometricAuthService
    {
        public async Task<bool> IsAvailableAsync()
        {
            try
            {
                var isSupported = await CrossFingerprint.Current.IsAvailableAsync();
                return isSupported;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> AuthenticateAsync(string reason = "Ingresar con huella/biometría")
        {
            var request = new AuthenticationRequestConfiguration("Autenticación", reason)
            {
               
                 FallbackTitle = "Ingresar con Usuario y Cobtraseña",
                 CancelTitle = "Cancelar"
            };

            var result = await CrossFingerprint.Current.AuthenticateAsync(request);
            return result.Authenticated;
        }
    }
}

