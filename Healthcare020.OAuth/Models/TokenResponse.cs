﻿using Newtonsoft.Json;

namespace Healthcare020.OAuth.Models
{
    public class TokenResponse
    {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }

            [JsonProperty("token_type")]
            public string TokenType { get; set; }

            [JsonProperty("scope")]
            public string Scope { get; set; }

            [JsonProperty("expires_in")]
            public string ExpiresIN { get; set; }

            [JsonProperty("refresh_token")]
            public string RefreshToken { get; set; }
    }
}