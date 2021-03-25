using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace reCAPTCHA.AspNetCore
{
    public class RecaptchaService : IRecaptchaService
    {
        private static readonly HttpClient Client = new HttpClient();
        public readonly RecaptchaSettings RecaptchaSettings;

        public RecaptchaService(IOptions<RecaptchaSettings> options)
        {
            RecaptchaSettings = options.Value;
        }

        public async Task<RecaptchaResponse> Validate(HttpRequest request, bool antiForgery = true)
        {
            if (!request.Form.ContainsKey("g-recaptcha-response")) // error if no reason to do anything, this is to alert developers they are calling it without reason.
                throw new ValidationException("Google recaptcha response not found in form. Did you forget to include it?");

            var response = request.Form["g-recaptcha-response"];

            var captchaResponse = await SendVerifyRequestAsync(response);

            if (captchaResponse.success && antiForgery)
                if (captchaResponse.hostname?.ToLower() != request.Host.Host?.ToLower() && captchaResponse.hostname != "testkey.google.com")
                    throw new ValidationException("Recaptcha host, and request host do not match. Forgery attempt?");

            return captchaResponse;
        }

        private async Task<RecaptchaResponse> SendVerifyRequestAsync(Microsoft.Extensions.Primitives.StringValues response)
        {
            var result = await Client.PostAsync($"https://{RecaptchaSettings.Site}/recaptcha/api/siteverify", new StringContent(
                $"secret={RecaptchaSettings.SecretKey}&response={response}", Encoding.UTF8, "application/x-www-form-urlencoded"));

            var captchaResponse = JsonSerializer.Deserialize<RecaptchaResponse>(await result.Content.ReadAsStringAsync());

            return captchaResponse;
        }

        public Task<RecaptchaResponse> Validate(string responseCode)
        {
            if (string.IsNullOrEmpty(responseCode))
                throw new ValidationException("Google recaptcha response is empty?");

            return SendVerifyRequestAsync(responseCode);
        }
    }
}