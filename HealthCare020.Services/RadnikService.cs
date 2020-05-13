﻿using AutoMapper;
using HealthCare020.Core.Entities;
using HealthCare020.Core.Models;
using HealthCare020.Core.Request;
using HealthCare020.Core.ResourceParameters;
using HealthCare020.Core.ServiceModels;
using HealthCare020.Repository;
using HealthCare020.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace HealthCare020.Services
{
    public class RadnikService : IRadnikService
    {
        private readonly HealthCare020DbContext _dbContext;
        private readonly ICRUDService<LicniPodaci, LicniPodaciDto, LicniPodaciDto, LicniPodaciResourceParameters, LicniPodaciUpsertDto, LicniPodaciUpsertDto> _licniPodaciService;
        private readonly IKorisnikService _korisnikService;
        private readonly IMapper _mapper;

        public RadnikService(HealthCare020DbContext dbContext,
            ICRUDService<LicniPodaci, LicniPodaciDto, LicniPodaciDto, LicniPodaciResourceParameters, LicniPodaciUpsertDto, LicniPodaciUpsertDto> licniPodaciService,
            IKorisnikService korisnikService, IMapper mapper)
        {
            _dbContext = dbContext;
            _licniPodaciService = licniPodaciService;
            _korisnikService = korisnikService;
            _mapper = mapper;
        }

        public async Task<ServiceResult<Radnik>> Insert(RadnikUpsertDto radnikDto)
        {
            if (!await _dbContext.StacionarnaOdeljenja.AnyAsync(x => x.Id == radnikDto.StacionarnoOdeljenjeId))
                return new ServiceResult<Radnik>(HttpStatusCode.NotFound, $"Stacionarno odeljenje sa ID-em {radnikDto.StacionarnoOdeljenjeId} nije pronadjeno");

            var licniPodaciResult = await _licniPodaciService.Insert(radnikDto.LicniPodaci);

            var korisnickiNalogResult = await _korisnikService.Insert(radnikDto.KorisnickiNalog);

            var radnik = new Radnik
            {
                KorisnickiNalogId = korisnickiNalogResult.Data.Id,
                LicniPodaciId = licniPodaciResult.Data.Id,
                StacionarnoOdeljenjeId = radnikDto.StacionarnoOdeljenjeId
            };

            await _dbContext.AddAsync(radnik);
            await _dbContext.SaveChangesAsync();

            return new ServiceResult<Radnik>(radnik);
        }

        public async Task<ServiceResult<Radnik>> Update(int id, RadnikUpsertDto radnikDto)
        {
            if (!_dbContext.StacionarnaOdeljenja.Any(x => x.Id == radnikDto.StacionarnoOdeljenjeId))
                return new ServiceResult<Radnik>(HttpStatusCode.NotFound, $"Stacionarno odeljenje sa ID-em {radnikDto.StacionarnoOdeljenjeId} nije pronadjeno");

            var entity = await _dbContext.Radnici
                .Include(x => x.KorisnickiNalog)
                .Include(x => x.LicniPodaci)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return new ServiceResult<Radnik>(HttpStatusCode.NotFound, $"Radnik sa ID-em {id} nije pronadjen");

            await _korisnikService.Update(entity.KorisnickiNalogId, radnikDto.KorisnickiNalog);
            await _licniPodaciService.Update(entity.LicniPodaciId, radnikDto.LicniPodaci);

            await Task.Run(() =>
            {
                entity.StacionarnoOdeljenjeId = radnikDto.StacionarnoOdeljenjeId;

                _dbContext.Update(entity);
            });

            await _dbContext.SaveChangesAsync();

            return new ServiceResult<Radnik>(entity);
        }

        public async Task<ServiceResult<Radnik>> Delete(int id)
        {
            var entity = await _dbContext.Radnici.FindAsync(id);

            if (entity == null)
                return new ServiceResult<Radnik>(HttpStatusCode.NotFound, $"Radnik sa ID-em {id} nije pronadjen");

            await _licniPodaciService.Delete(entity.LicniPodaciId);
            await _korisnikService.Delete(entity.KorisnickiNalogId);

            await Task.Run(() => { _dbContext.Remove(entity); });

            return new ServiceResult<Radnik>();
        }
    }
}