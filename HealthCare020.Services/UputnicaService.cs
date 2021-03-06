﻿using AutoMapper;
using HealthCare020.Core.Entities;
using HealthCare020.Core.Models;
using HealthCare020.Core.Request;
using HealthCare020.Core.ResourceParameters;
using HealthCare020.Core.ServiceModels;
using HealthCare020.Repository;
using HealthCare020.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using HealthCare020.Services.Helpers;

namespace HealthCare020.Services
{
    public class UputnicaService : BaseCRUDService<UputnicaDtoLL, UputnicaDtoEL, UputnicaResourceParameters, Uputnica, UputnicaUpsertDto, UputnicaUpsertDto>
    {
        public UputnicaService(IMapper mapper,
            HealthCare020DbContext dbContext,
            IPropertyMappingService propertyMappingService,
            IPropertyCheckerService propertyCheckerService,
            IHttpContextAccessor httpContextAccessor,
            IAuthService authService)
            : base(mapper, dbContext, propertyMappingService, propertyCheckerService, httpContextAccessor, authService)
        {
        }

        public override IQueryable<Uputnica> GetWithEagerLoad(int? id = null)
        {
            var result = _dbContext.Uputnice
                .Include(x => x.Pacijent)
                .ThenInclude(x => x.ZdravstvenaKnjizica)
                .ThenInclude(x => x.LicniPodaci)
                .ThenInclude(x => x.Grad)
                .Include(x => x.UpucenKodDoktora)
                .ThenInclude(x => x.Radnik)
                .ThenInclude(x => x.LicniPodaci)
                .Include(x => x.UputioDoktor)
                .ThenInclude(x => x.Radnik)
                .ThenInclude(x => x.LicniPodaci);

            return id.HasValue ? result.Where(x => x.Id == id) : result;
        }

        public override async Task<ServiceResult> Insert(UputnicaUpsertDto dtoForCreation)
        {
            var doktor = await _authService.GetCurrentLoggedInDoktor();
            if (doktor == null)
                return ServiceResult.Forbidden();

            if (await ValidateModel(dtoForCreation) is { } validationResult && !validationResult.Succeeded)
                return ServiceResult.WithStatusCode(validationResult.StatusCode, validationResult.Message);

            var uputnica = _mapper.Map<Uputnica>(dtoForCreation);
            uputnica.UputioDoktorId = doktor.Id;

            await _dbContext.AddAsync(uputnica);
            await _dbContext.SaveChangesAsync();

            return ServiceResult.OK(_mapper.Map<UputnicaDtoLL>(uputnica));
        }

        public override async Task<ServiceResult> Update(int id, UputnicaUpsertDto dtoForUpdate)
        {
            var getUputnicaResult = await GetUputnicaForManipulation(id);
            if (!getUputnicaResult.Succeeded)

                return ServiceResult.WithStatusCode(getUputnicaResult.StatusCode, getUputnicaResult.Message);

            var uputnicaFromDb = getUputnicaResult.Data as Uputnica;

            if (uputnicaFromDb == null)
                return ServiceResult.NotFound();

            if (await ValidateModel(dtoForUpdate) is { } validationResult)
                return ServiceResult.WithStatusCode(validationResult.StatusCode, validationResult.Message);

            _mapper.Map(dtoForUpdate, uputnicaFromDb);
            await _dbContext.SaveChangesAsync();

            return ServiceResult.OK(_mapper.Map<UputnicaDtoLL>(uputnicaFromDb));
        }

        public override async Task<ServiceResult> Delete(int id)
        {
            if(await _dbContext.ZahteviZaPregled.AnyAsync(x=>x.UputnicaId==id))
                return ServiceResult.BadRequest("Ne mozete brisati uputnicu sve dok ima zahteva za pregled koji su referencirani na nju.");

            var getUputnicaResult = await GetUputnicaForManipulation(id);
            if (!getUputnicaResult.Succeeded)
                return ServiceResult.WithStatusCode(getUputnicaResult.StatusCode, getUputnicaResult.Message);

            var uputnicaFromDb = getUputnicaResult.Data as Uputnica;
            if(uputnicaFromDb==null)
                return ServiceResult.NotFound();

            await Task.Run(() =>
            {
                _dbContext.Remove(uputnicaFromDb);
            });

            await _dbContext.SaveChangesAsync();

            return ServiceResult.NoContent();
        }

        public override async Task<PagedList<Uputnica>> FilterAndPrepare(IQueryable<Uputnica> result, UputnicaResourceParameters resourceParameters)
        {
            if (!await result.AnyAsync())
                return null;

            if (resourceParameters != null)
            {
                if (!string.IsNullOrWhiteSpace(resourceParameters.PacijentImePrezime))
                {
                    result = result.Where(x =>
                        x.Pacijent.ZdravstvenaKnjizica.LicniPodaci.Ime.ToLower()
                            .Contains(resourceParameters.PacijentImePrezime.ToLower())
                        || x.Pacijent.ZdravstvenaKnjizica.LicniPodaci.Prezime.ToLower()
                            .Contains(resourceParameters.PacijentImePrezime.ToLower()));
                }

                if (await result.AnyAsync())
                {
                    if (resourceParameters.Datum.HasValue)
                        result = result.Where(x => x.DatumVreme.Date == resourceParameters.Datum.Value.Date);
                }
                else
                {
                    return await base.FilterAndPrepare(result, resourceParameters);
                }

                if (await result.AnyAsync())
                {
                    if (resourceParameters.UputioDoktorId.HasValue)
                        result = result.Where(x => x.UputioDoktorId==resourceParameters.UputioDoktorId);
                }
                else
                {
                    return await base.FilterAndPrepare(result, resourceParameters);
                }

                if (await result.AnyAsync())
                {
                    if (resourceParameters.UpucenKodDoktoraId.HasValue)
                        result = result.Where(x => x.UpucenKodDoktoraId == resourceParameters.UpucenKodDoktoraId);
                }
                else
                {
                    return await base.FilterAndPrepare(result, resourceParameters);
                }

                if (await result.AnyAsync())
                {
                    if (resourceParameters.PacijentId.HasValue)
                        result = result.Where(x => x.PacijentId == resourceParameters.PacijentId);
                }
                else
                {
                    return await base.FilterAndPrepare(result, resourceParameters);
                }
            }

            //CONSTRAINT -> Pacijent moze samo svoje preglede videti
            if (await result.AnyAsync())
            {
                if( _authService.UserIsDoktor() && await _authService.GetCurrentLoggedInDoktor() is { } doktor)
                {
                    if (resourceParameters?.UpuceneUputnice ?? false)
                        result = result.Where(x => x.UpucenKodDoktoraId == doktor.Id);
                    else
                        result = result.Where(x => x.UputioDoktorId == doktor.Id);
                }

                result = result.OrderByDescending(x => x.DatumVreme);
            }


            return await base.FilterAndPrepare(result, resourceParameters);
        }

        public override async Task<bool> AuthorizePacijentForGetById(int id)
        {
            var pacijent = await _authService.GetCurrentLoggedInPacijent();
            if (pacijent == null)
                return false;

            return await _dbContext.Uputnice.AnyAsync(x => x.Id == id && x.PacijentId == pacijent.Id);
        }

        /// <summary>
        /// Returns Uputnica if Doktor is authorized for it
        /// </summary>
        /// <returns></returns>
        private async Task<ServiceResult> GetUputnicaForManipulation(int id)
        {
            var doktor = await _authService.GetCurrentLoggedInDoktor();
            if (doktor == null)
                return ServiceResult.Forbidden("Samo doktori mogu vrsiti izmene pregleda.");

            var uputnicaFromDb = await _dbContext.Uputnice.FindAsync(id);
            if (uputnicaFromDb == null)
                return ServiceResult.NotFound($"Uputnica sa ID-em {id} nije pronadjen.");

            if (uputnicaFromDb.UputioDoktorId != doktor.Id)
                return ServiceResult.Forbidden("Nemate permisije za izmenu uputnica koje je kreirao drugi doktor.");

            return ServiceResult.OK(uputnicaFromDb);
        }

        private async Task<ServiceResult> ValidateModel(UputnicaUpsertDto dto)
        {
            if (dto == null)
                return ServiceResult.BadRequest();

            if (!await _dbContext.Doktori.AnyAsync(x => x.Id == dto.UpucenKodDoktoraId))
                return ServiceResult.NotFound($"Doktor sa ID-em {dto.UpucenKodDoktoraId} nije pronadjen.");

            if (!await _dbContext.Pacijenti.AnyAsync(x => x.Id == dto.PacijentId))
                return ServiceResult.NotFound($"Pacijent sa ID-em {dto.PacijentId} nije pronadjen.");

            return ServiceResult.WithStatusCode(HttpStatusCode.OK);
        }
    }
}