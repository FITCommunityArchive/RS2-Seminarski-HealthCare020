﻿using Healthcare020.OAuth.Validators;
using HealthCare020.Core.Constants;
using HealthCare020.Core.Resources;
using HealthCare020.Core.ServiceModels;
using HealthCare020.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Healthcare020.OAuth.Properties;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Healthcare020.OAuth.Controllers
{
    [Route(Routes.FaceRecognitionRoute)]
    public class FaceRecognitionController : Controller
    {
        private readonly IKorisnikService _korisnikService;
        private readonly IWebHostEnvironment Environment;

        public FaceRecognitionController(IKorisnikService korisnikService,IWebHostEnvironment environment)
        {
            _korisnikService = korisnikService;
            Environment = environment;
        }

        [HttpPost]
        [Consumes("application/json")]
        public async Task<IActionResult> LoginWithFaceID([FromBody] TokenEndpointRequestBody model)
        {
            if (model == null)
                return BadRequest();
            byte[] img = null;

            try
            {
                img = Convert.FromBase64String(model.Image);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            var korisnickiNalog = await _korisnikService.Authenticate(img);
            if (korisnickiNalog == null)
                return BadRequest(SharedResources.UnsuccessfulFaceIdAuthentication);

            //Secret GUID for resource owner password validator (if GUID is not valid, auth will be rejected)
            var secretGuid = new Guid();
            FaceIDSecretsManager.Add(secretGuid);

            var postBody = @"client_id=" + model.ClientId
                                         + "&client_secret=" + model.ClientSecret
                                         + "&grant_type=password&username="
                                         + $"{korisnickiNalog.Username}*{korisnickiNalog.Id}" + $"&password={secretGuid}"
                                         + "&scope=openid offline_access face-recognition";
            var client = new HttpClient();
            HttpResponseMessage response;
            var uri = Environment.IsDevelopment() ? $"{Request.Scheme}://{Request.Host.Value}" : Resources.ProductionUri;
            using (var stringContent = new StringContent(postBody, Encoding.UTF8, "application/x-www-form-urlencoded"))
            {
                response = await client.PostAsync($@"{uri}/connect/token", stringContent);
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var responseJson = await response.Content.ReadAsStringAsync();

                return Ok(responseJson);
            }

            return BadRequest(response.Content.ReadAsStringAsync());
        }
    }
}