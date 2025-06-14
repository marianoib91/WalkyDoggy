using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WalkyDoggy.Application.Dtos;

namespace WalkyDoggy.Web.Infrastructure.Validators
{
    public class RegistrationDtoValidator:AbstractValidator<RegistrationDto>
    {
        public RegistrationDtoValidator() {

            RuleFor(r => r.Email).NotEmpty().EmailAddress()
                .WithMessage("Debe ingresar una direccion de correo electrónico válida.");

            RuleFor(r => r.Password).NotEmpty()
                .WithMessage("Debe ingresar una contraseña.");
        }
    }
}