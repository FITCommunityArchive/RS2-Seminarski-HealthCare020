﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace HealthCare020.Services.Interfaces
{
    public interface IService<TEntity, TModel, TSearch>
    {
        Task<IList<TModel>> Get(TSearch search);

        Task<IList<TModel>> GetWithEagerLoad(TSearch search);

        Task<TModel> GetById(int id);

        Task<TModel> FindWithEagerLoad(int id);
    }
}