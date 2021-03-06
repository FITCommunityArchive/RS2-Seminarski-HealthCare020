﻿using AutoMapper;
using HealthCare020.Core.Entities;
using HealthCare020.Core.Enums;
using HealthCare020.Core.Models;
using HealthCare020.Core.Request;
using HealthCare020.Core.ResourceParameters;
using HealthCare020.Core.ServiceModels;
using HealthCare020.Repository;
using HealthCare020.Services.Helpers;
using HealthCare020.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace HealthCare020.Services
{
    public class DoktorService : BaseCRUDService<DoktorDtoLL, DoktorDtoEL, DoktorResourceParameters, Doktor, DoktorUpsertDto, DoktorUpsertDto>
    {
        private readonly IRadnikService _radnikService;
        private readonly IKorisnikService _korisnikService;

        public DoktorService(IMapper mapper, HealthCare020DbContext dbContext,
            IPropertyMappingService propertyMappingService,
            IPropertyCheckerService propertyCheckerService,
            IRadnikService radnikService,
            IHttpContextAccessor httpContextAccessor,
            IAuthService authService,
            IKorisnikService korisnikService) : base(mapper, dbContext, propertyMappingService, propertyCheckerService, httpContextAccessor, authService)
        {
            _radnikService = radnikService;
            _korisnikService = korisnikService;
        }

        public override IQueryable<Doktor> GetWithEagerLoad(int? id = null)
        {
            var result = _dbContext.Doktori
                .Include(x => x.NaucnaOblast)
                .Include(x => x.Radnik)
                .ThenInclude(x => x.KorisnickiNalog)
                .Include(x => x.Radnik)
                .ThenInclude(x => x.LicniPodaci)
                .ThenInclude(x => x.Grad)
                .Include(x => x.Radnik)
                .ThenInclude(x => x.StacionarnoOdeljenje)
                .AsQueryable();

            if (id.HasValue)
                result = result.Where(x => x.Id == id);

            return result;
        }

        public override async Task<ServiceResult> Insert(DoktorUpsertDto request)
        {
            if (!await _dbContext.NaucneOblasti.AnyAsync(x => x.Id == request.NaucnaOblastId))
                return ServiceResult.NotFound($"Naucna oblast sa ID-em {request.NaucnaOblastId} nije pronadjena.");

            var radnikInsertResult = await _radnikService.Insert(request);
            if (!radnikInsertResult.Succeeded)
                return ServiceResult.WithStatusCode(radnikInsertResult.StatusCode, radnikInsertResult.Message);

            var radnik = radnikInsertResult.Data as Radnik;

            if (radnik == null)
                throw new NullReferenceException();

            var rolesAddResult = await _korisnikService.AddInRoles(radnik.KorisnickiNalogId,
                new KorisnickiNalogRolesUpsertDto { RoleId = RoleType.Doktor.ToInt() });
            if (!rolesAddResult.Succeeded)
                return ServiceResult.WithStatusCode(rolesAddResult.StatusCode, rolesAddResult.Message);

            var entity = _mapper.Map<Doktor>(request);
            entity.RadnikId = radnik.Id;

            await _dbContext.AddAsync(entity);
            await _dbContext.SaveChangesAsync();

            return ServiceResult.OK(_mapper.Map<DoktorDtoLL>(entity));
        }

        public override async Task<ServiceResult> Update(int id, DoktorUpsertDto dtoForUpdate)
        {
            var getDoktorResult = await GetDoktor(id);
            if (!getDoktorResult.Succeeded)
                return ServiceResult.WithStatusCode(getDoktorResult.StatusCode, getDoktorResult.Message);

            var doktorFromDb = getDoktorResult.doktor;

            if (!await _authService.CurrentUserIsInRoleAsync(RoleType.Administrator) && doktorFromDb.Radnik.KorisnickiNalogId != ((await _authService.LoggedInUser())?.Id ?? 0))
                return ServiceResult.Forbidden("Ne mozete vrsiti izmene na drugim profilima doktora.");

            if (doktorFromDb.NaucnaOblastId != dtoForUpdate.NaucnaOblastId)
            {
                if (!await _dbContext.NaucneOblasti.AnyAsync(x => x.Id == dtoForUpdate.NaucnaOblastId))
                    return ServiceResult.NotFound(
                        $"Naucna oblast sa ID-em {dtoForUpdate.NaucnaOblastId} nije pronadjena.");

                doktorFromDb.NaucnaOblastId = dtoForUpdate.NaucnaOblastId;
                await _dbContext.SaveChangesAsync();
            }

            _mapper.Map(dtoForUpdate, doktorFromDb.Radnik);
            var radnikUpdateResult = await _radnikService.Update(doktorFromDb.RadnikId, dtoForUpdate);
            if (!radnikUpdateResult.Succeeded)
                return ServiceResult.WithStatusCode(radnikUpdateResult.StatusCode, radnikUpdateResult.Message);
            return ServiceResult.OK(_mapper.Map<DoktorDtoLL>(doktorFromDb));
        }

        public override async Task<ServiceResult> Delete(int id)
        {
            var getDoktorResult = await GetDoktor(id);
            if (!getDoktorResult.Succeeded)
                return ServiceResult.WithStatusCode(getDoktorResult.StatusCode, getDoktorResult.Message);

            var doktorFromDb = getDoktorResult.doktor;

            if (!await _authService.CurrentUserIsInRoleAsync(RoleType.Administrator) && doktorFromDb.Radnik.KorisnickiNalogId != ((await _authService.LoggedInUser())?.Id ?? 0))
                return ServiceResult.Forbidden("Ne mozete vrsiti izmene na drugim profilima doktora.");

            if (await _dbContext.Uputnice.AnyAsync(x => x.UputioDoktorId == doktorFromDb.Id || x.UpucenKodDoktoraId == doktorFromDb.Id))
                return ServiceResult.BadRequest("Ne mozete izbrisati profil doktora sve dok postoje uputnice koje su povezane sa ovim doktorom.");

            if (await _dbContext.ZahteviZaPregled.AnyAsync(x => x.DoktorId == doktorFromDb.Id))
                return ServiceResult.BadRequest("Ne mozete izbrisati profil doktora sve dok postoje zahtevi za pregled kod ovog doktora.");

            if (await _dbContext.Pregledi.AnyAsync(x => x.DoktorId == doktorFromDb.Id))
                return ServiceResult.BadRequest("Ne mozete izbrisati profil doktora sve dok postoje zakazani ili odradjeni pregledi koje je odradio ovaj doktor.");

            if (await _dbContext.ZdravstvenaKnjizica.AnyAsync(x => x.DoktorId == doktorFromDb.Id))
                return ServiceResult.BadRequest("Ne mozete izbrisati profil doktora sve dok ima zdravstvenih knjizica koje su povezane sa ovim doktorom.");

            await _radnikService.Delete(doktorFromDb.RadnikId);

            await Task.Run(() =>
            {
                _dbContext.Remove(doktorFromDb);
            });

            await _dbContext.SaveChangesAsync();

            return ServiceResult.NoContent();
        }

        public override async Task<PagedList<Doktor>> FilterAndPrepare(IQueryable<Doktor> result, DoktorResourceParameters resourceParameters)
        {
            if (!await result.AnyAsync())
                return null;

            if (resourceParameters != null)
            {
                if (!string.IsNullOrWhiteSpace(resourceParameters.Ime))
                {
                    var imeToSearch = resourceParameters.Ime.ToLower();
                    if (await result.AnyAsync(x => x.Radnik.LicniPodaci.Ime.ToLower().StartsWith(imeToSearch)))
                    {
                        result = result.Where(x => x.Radnik.LicniPodaci.Ime.ToLower().StartsWith(imeToSearch));
                    }
                    else if (!string.IsNullOrWhiteSpace(resourceParameters.Prezime))
                    {
                        result = result.Where(x => x.Radnik.LicniPodaci.Prezime.ToLower().StartsWith(resourceParameters.Prezime.ToLower()));
                    }
                }

                if (await result.AnyAsync())
                {
                    if (!string.IsNullOrEmpty(resourceParameters.Username))
                        result = result.Where(x =>
                            x.Radnik.KorisnickiNalog.Username.ToLower().StartsWith(resourceParameters.Username.ToLower()));
                }
                else
                {
                    return await base.FilterAndPrepare(result, resourceParameters);
                }

                if (await result.AnyAsync())
                {
                    if (!string.IsNullOrWhiteSpace(resourceParameters.EqualUsername))
                        result = result.Where(x =>
                            x.Radnik.KorisnickiNalog.Username.ToLower() == resourceParameters.EqualUsername);
                }
                else
                {
                    return await base.FilterAndPrepare(result, resourceParameters);
                }

                if (await result.AnyAsync())
                {
                    if (!string.IsNullOrEmpty(resourceParameters.NaucnaOblast))
                        result = result.Where(x =>
                            x.NaucnaOblast.Naziv.ToLower().StartsWith(resourceParameters.NaucnaOblast.ToLower()));
                }
                else
                {
                    return await base.FilterAndPrepare(result, resourceParameters);
                }

                if (await result.AnyAsync())
                {
                    if (resourceParameters.KorisnickiNalogId.HasValue)
                        result = result.Where(x => x.Radnik.KorisnickiNalogId == resourceParameters.KorisnickiNalogId);
                }
                else
                {
                    return await base.FilterAndPrepare(result, resourceParameters);
                }


                if (resourceParameters.EagerLoaded)
                    PropertyCheck<DoktorDtoEL>(resourceParameters.OrderBy);
            }

            result = result.Include(x => x.Radnik.LicniPodaci);

            return await base.FilterAndPrepare(result, resourceParameters);
        }

        //===HELPER METHODS===

        /// <summary>
        /// </summary>
        /// <returns>Doktor entity with passed 'id' value </returns>
        private async Task<(Doktor doktor, bool Succeeded, HttpStatusCode StatusCode, string Message)> GetDoktor(int id)
        {
            Doktor doktorFromDb = null;

            doktorFromDb = await _dbContext.Doktori
                .Include(x => x.Radnik)
                .ThenInclude(x => x.LicniPodaci)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (doktorFromDb == null)
                return (null, false, HttpStatusCode.NotFound, $"Doktor sa ID-em {id} nije pronadjen.");

            return (doktorFromDb, true, HttpStatusCode.OK, string.Empty);
        }
    }
}