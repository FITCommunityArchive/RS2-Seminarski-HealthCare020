﻿using HealthCare020.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace HealthCare020.API.Controllers
{
    public class BaseCRUDController<TEntity, TDto, TResourceParameters, TDtoForCreation, TDtoForUpdate> : BaseController<TEntity, TDto, TResourceParameters>
    {
        private readonly ICRUDService<TEntity, TDto, TResourceParameters, TDtoForCreation, TDtoForUpdate> _crudService;

        public BaseCRUDController(ICRUDService<TEntity, TDto, TResourceParameters, TDtoForCreation, TDtoForUpdate> crudService) : base(crudService)
        {
            _crudService = crudService;
        }

        [HttpPost]
        public async Task<IActionResult> Insert(TDtoForCreation dtoForCreation)
        {
            var result = await _crudService.Insert(dtoForCreation);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, TDtoForUpdate dtoForUpdate)
        {
            var result = _crudService.Update(id, dtoForUpdate);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var result = _crudService.Delete(id);

            return Ok(result);
        }
    }
}