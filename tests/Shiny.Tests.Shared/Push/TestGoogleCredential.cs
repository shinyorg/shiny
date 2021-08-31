#if DEVICE_TESTS && !WINDOWS_UWP
using System;
using Newtonsoft.Json;


namespace Shiny.Tests.Push
{
    public class TestGoogleCredential
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "service_account";

        [JsonProperty("auth_uri")]
        public string AuthUri { get; set; } = "https://accounts.google.com/o/oauth2/auth";

        [JsonProperty("token_uri")]
        public string TokenUri { get; set; } = "https://oauth2.googleapis.com/token";

        [JsonProperty("auth_provider_x509_cert_url")]
        public string AuthProviderCertUrl { get; set; } = "https://www.googleapis.com/oauth2/v1/certs";


        [JsonProperty("project_id")]
        public string ProjectId { get; set; }

        [JsonProperty("private_key_id")]
        public string PrivateKeyId { get; set; }

        [JsonProperty("private_key")]
        public string PrivateKey { get; set; }

        [JsonProperty("client_id")]
        public string ClientId { get; set; }

        [JsonProperty("client_email")]
        public string ClientEmail { get; set; }

        [JsonProperty("client_x509_cert_url")]
        public string ClientCertUrl { get; set; }
    }
}
#endif