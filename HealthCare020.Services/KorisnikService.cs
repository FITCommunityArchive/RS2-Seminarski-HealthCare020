﻿using AutoMapper;
using HealthCare020.Core.Entities;
using HealthCare020.Core.Models;
using HealthCare020.Core.Request;
using HealthCare020.Core.ResourceParameters;
using HealthCare020.Repository;
using HealthCare020.Services.Exceptions;
using HealthCare020.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HealthCare020.Services

{
    public class KorisnikService : BaseService<KorisnickiNalogDtoLazyLoaded, KorisnickiNalogDtoEagerLoaded, KorisnickiNalogResourceParameters, KorisnickiNalog>, IKorisnikService
    {
        public KorisnikService(IMapper mapper, HealthCare020DbContext dbContext, IPropertyMappingService propertyMappingService, IPropertyCheckerService propertyCheckerService) :
            base(mapper, dbContext, propertyMappingService, propertyCheckerService)
        {
        }

        public override IQueryable<KorisnickiNalog> GetWithEagerLoad(int? id = null)
        {
            var result = _dbContext.KorisnickiNalozi
                .Include(x => x.RolesKorisnickiNalog)
                .ThenInclude(x => x.Role)
                .AsQueryable();

            if (id.HasValue)
                result = result.Where(x => x.Id == id);

            return result;
        }

        public async Task<KorisnickiNalogDtoLazyLoaded> Insert(KorisnickiNalogUpsertDto request)
        {
            var korisnickiNalog = _mapper.Map<KorisnickiNalog>(request);

            if (request.ConfirmPassword != request.Password)
            {
                throw new UserException("Lozinke se ne podudaraju");
            }

            foreach (var roleId in request.Roles)
            {
                if (!await _dbContext.Roles.AnyAsync(x => x.Id == roleId))
                    throw new NotFoundException($"Rola sa ID-em {roleId} nije pronadjena");
            }

            korisnickiNalog.PasswordSalt = GenerateSalt();
            korisnickiNalog.PasswordHash = GenerateHash(korisnickiNalog.PasswordSalt, request.Password);

            korisnickiNalog.DateCreated = DateTime.Now;
            korisnickiNalog.LastOnline = DateTime.Now;

            await _dbContext.KorisnickiNalozi.AddAsync(korisnickiNalog);
            await _dbContext.SaveChangesAsync();

            foreach (var roleId in request.Roles)
            {
                await _dbContext.RolesKorisnickiNalozi.AddAsync(new RoleKorisnickiNalog
                {
                    KorisnickiNalogId = korisnickiNalog.Id,
                    RoleId = roleId
                });
            }
            await _dbContext.SaveChangesAsync();

            //Load RoleKorisnickiNalog relations
            _dbContext.Entry(korisnickiNalog).Collection(x => x.RolesKorisnickiNalog).Load();

            return _mapper.Map<KorisnickiNalogDtoLazyLoaded>(korisnickiNalog);
        }

        public KorisnickiNalogDtoLazyLoaded Update(int id, KorisnickiNalogUpsertDto request)
        {
            var korisnickiNalog = _dbContext.KorisnickiNalozi.Find(id);

            if (korisnickiNalog == null)
                throw new NotFoundException("Korisnicki nalog nije pronadjen");

            _mapper.Map(request, korisnickiNalog);

            if (!string.IsNullOrEmpty(request.Password) && request.Password != request.ConfirmPassword)
                throw new UserException("Lozinke se ne podudaraju");

            korisnickiNalog.PasswordSalt = GenerateSalt();
            korisnickiNalog.PasswordHash = GenerateHash(korisnickiNalog.PasswordSalt, request.Password);

            korisnickiNalog.DateCreated = DateTime.Now;
            korisnickiNalog.LastOnline = DateTime.Now;

            _dbContext.KorisnickiNalozi.Update(korisnickiNalog);
            _dbContext.SaveChanges();

            if(request.RolesToDelete.Any(x=>request.Roles.Contains(x)))
                throw new UserException($"Liste rola za brisanje i dodavanje ne smiju sadrzati zajednicke elemente.");

            foreach (var roleId in request.Roles)
            {
                if (_dbContext.RolesKorisnickiNalozi.Any(x =>
                    x.KorisnickiNalogId == korisnickiNalog.Id && x.RoleId == roleId))
                    throw new UserException($"Korisnik sa ID-em {korisnickiNalog.Id} vec poseduje role sa ID-em {roleId}");

                _dbContext.RolesKorisnickiNalozi.Add(new RoleKorisnickiNalog
                {
                    KorisnickiNalogId = korisnickiNalog.Id,
                    RoleId = roleId
                });
            }
            _dbContext.SaveChanges();

            foreach (var roleId in request.RolesToDelete)
            {
                var roleKorisnik = _dbContext.RolesKorisnickiNalozi.FirstOrDefault(x=>x.KorisnickiNalogId==korisnickiNalog.Id && x.RoleId==roleId);
                if (roleKorisnik == null)
                    throw new NotFoundException($"Rola sa ID-em {roleId} ne pripada ovom korisniku");

                _dbContext.RolesKorisnickiNalozi.Remove(roleKorisnik);
            }
            _dbContext.SaveChanges();

            //Load RoleKorisnickiNalog relations
            _dbContext.Entry(korisnickiNalog).Collection(x => x.RolesKorisnickiNalog).Load();

            return _mapper.Map<KorisnickiNalogDtoLazyLoaded>(korisnickiNalog);
        }

        public void Delete(int id)
        {
            var korisnickiNalog = _dbContext.KorisnickiNalozi.Find(id);

            if (korisnickiNalog == null)
                throw new NotFoundException("Korisnicki nalog nije pronadjen");

            _dbContext.KorisnickiNalozi.Remove(korisnickiNalog);
            _dbContext.SaveChanges();
        }

        public override IEnumerable<ExpandoObject> PrepareDataForClient(IEnumerable<KorisnickiNalog> data, KorisnickiNalogResourceParameters resourceParameters)
        {
            foreach (var x in data)
            {
                _dbContext.Entry(x).Collection(c => c.RolesKorisnickiNalog).Load();
            }
            return base.PrepareDataForClient(data, resourceParameters);
        }

        public async Task<KorisnickiNalogDtoLazyLoaded> Authenticate(string username, string password)
        {
            var korisnickiNalog = await _dbContext.KorisnickiNalozi.FirstAsync(x => x.Username == username);

            if (korisnickiNalog == null)
            {
                var newHash = GenerateHash(korisnickiNalog.PasswordSalt, password);

                if (newHash == korisnickiNalog.PasswordHash)
                    return _mapper.Map<KorisnickiNalogDtoLazyLoaded>(korisnickiNalog);
            }
            return null;
        }

        private string GenerateSalt()
        {
            var buf = new byte[16];
            (new RNGCryptoServiceProvider()).GetBytes(buf);
            return Convert.ToBase64String(buf);
        }

        private string GenerateHash(string salt, string password)
        {
            byte[] src = Convert.FromBase64String(salt);
            byte[] bytes = Encoding.Unicode.GetBytes(password);
            byte[] dst = new byte[src.Length + bytes.Length];

            System.Buffer.BlockCopy(src, 0, dst, 0, src.Length);
            System.Buffer.BlockCopy(bytes, 0, dst, src.Length, bytes.Length);

            HashAlgorithm algorithm = HashAlgorithm.Create("SHA512");
            byte[] inArray = algorithm.ComputeHash(dst);
            return Convert.ToBase64String(inArray);
        }
    }
}