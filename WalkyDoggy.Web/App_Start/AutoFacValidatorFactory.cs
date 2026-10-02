using Autofac;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WalkyDoggy.Web.App_Start
{
    public class AutoFacValidatorFactory : ValidatorFactoryBase
    {
        private readonly IComponentContext _context;

        public AutoFacValidatorFactory(IComponentContext contexto)
        {
            _context = contexto;
        }

        public override IValidator CreateInstance(Type tipoValidador)
        {
            object instancia;

            if (_context.TryResolve(tipoValidador, out instancia))
            {
                var validador = instancia as IValidator;
                return validador;
            }

            return null;
        }
    }
}