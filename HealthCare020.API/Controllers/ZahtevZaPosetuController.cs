﻿using HealthCare020.Core.Constants;
using HealthCare020.Core.Entities;
using HealthCare020.Core.Models;
using HealthCare020.Core.Request;
using HealthCare020.Core.ResourceParameters;
using HealthCare020.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using HealthCare020.Services.Constants;

namespace HealthCare020.API.Controllers
{
    [Route("api/" + Routes.ZahtevZaPosetuRoute)]
    [Authorize(AuthorizationPolicies.RadnikPrijemPolicy)] //At least RadnikPrijem
    public class ZahtevZaPosetuController : BaseCRUDController<ZahtevZaPosetu, ZahtevZaPosetuDtoLL, ZahtevZaPosetuDtoEL, ZahtevZaPosetuResourceParameters, ZahtevZaPosetuUpsertDto, ZahtevZaPosetuUpsertDto>
    {
        public ZahtevZaPosetuController(ICRUDService<ZahtevZaPosetu, ZahtevZaPosetuDtoLL,
            ZahtevZaPosetuDtoEL, ZahtevZaPosetuResourceParameters, ZahtevZaPosetuUpsertDto, ZahtevZaPosetuUpsertDto> crudService)
            : base(crudService)
        { }

        [AllowAnonymous]
        public override async Task<IActionResult> Insert(ZahtevZaPosetuUpsertDto dtoForCreation)
        {
            return await base.Insert(dtoForCreation);
        }

        [NonAction]
        public override async Task<IActionResult> Update(int id, ZahtevZaPosetuUpsertDto dtoForUpdate)
        {
            return await base.Update(id, dtoForUpdate);
        }

        [NonAction]
        public override async Task<IActionResult> PartiallyUpdate(int id, JsonPatchDocument<ZahtevZaPosetuUpsertDto> patchDocument)
        {
            return await base.PartiallyUpdate(id, patchDocument);
        }
    }
}