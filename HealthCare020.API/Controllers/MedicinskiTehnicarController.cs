﻿using System.Threading.Tasks;
using HealthCare020.API.Constants;
using HealthCare020.Core.Constants;
using HealthCare020.Core.Entities;
using HealthCare020.Core.Models;
using HealthCare020.Core.Request;
using HealthCare020.Core.ResourceParameters;
using HealthCare020.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace HealthCare020.API.Controllers
{
    [Route("api/"+Routes.MedicinskiTehnicariRoute)]
    public class MedicinskiTehnicarController : BaseCRUDController<MedicinskiTehnicar, MedicinskiTehnicarDtoLL, MedicinskiTehnicarDtoEL, MedicinskiTehnicarResourceParameters, MedicinskiTehnicarUpsertDto, MedicinskiTehnicarUpsertDto>
    {
        public MedicinskiTehnicarController(ICRUDService<MedicinskiTehnicar, MedicinskiTehnicarDtoLL, MedicinskiTehnicarDtoEL, MedicinskiTehnicarResourceParameters, MedicinskiTehnicarUpsertDto, MedicinskiTehnicarUpsertDto> crudService) : base(crudService)
        {
        }

        [Authorize(AuthorizationPolicies.DoktorPolicy)]
        public override Task<IActionResult> Insert(MedicinskiTehnicarUpsertDto dtoForCreation)
        {
            return base.Insert(dtoForCreation);
        }

        [Authorize(AuthorizationPolicies.MedicinskiTehnicarPolicy)]
        public override Task<IActionResult> Update(int id, MedicinskiTehnicarUpsertDto dtoForUpdate)
        {
            return base.Update(id, dtoForUpdate);
        }

        [Authorize(AuthorizationPolicies.MedicinskiTehnicarPolicy)]
        public override Task<IActionResult> Delete(int id)
        {
            return base.Delete(id);
        }

        [Authorize(AuthorizationPolicies.MedicinskiTehnicarPolicy)]
        public override Task<IActionResult> PartiallyUpdate(int id, JsonPatchDocument<MedicinskiTehnicarUpsertDto> patchDocument)
        {
            return base.PartiallyUpdate(id, patchDocument);
        }
    }
}