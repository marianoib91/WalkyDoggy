using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WalkyDoggy.Application.Dtos;

namespace WalkyDoggy.Web.Infrastructure.Validators
{
    public class CustomerDtoValidator : AbstractValidator<CustomerDto>
    {
        public CustomerDtoValidator()
        {
            RuleFor(r => r.CityId).NotEmpty()
                .WithMessage("Debe seleccionar una ciudad.");

            RuleFor(r => r.FirstName).NotEmpty()
                .WithMessage("Debe ingresar un nombre.");

            RuleFor(r => r.LastName).NotEmpty()
                .WithMessage("Debe ingresar un apellido.");

            RuleFor(r => r.Email).NotEmpty().EmailAddress()
               .WithMessage("Debe ingresar una direccion de correo electrónico válida.");

            RuleFor(r => r.Phone).NotEmpty().Length(10, 10)
                .WithMessage("Debe ingresar un número de teléfono válido.");

            RuleFor(r => r.StreetName).NotEmpty()
                .WithMessage("Debe ingresar un nombre de calle.");

            RuleFor(r => r.StreetNumber).NotEmpty()
               .WithMessage("Debe ingresar un número de calle.");

            RuleFor(r => r.Password).NotEmpty().Length(6, 50)
                .WithMessage("La contraseña debe poseer al menos 6 caracteres.");
        }
    }
}