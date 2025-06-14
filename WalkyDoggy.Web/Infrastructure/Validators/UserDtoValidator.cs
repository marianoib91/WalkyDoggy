using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WalkyDoggy.Services.Dtos;

namespace WalkyDoggy.Web.Infrastructure.Validators
{
    public class UserDtoValidator : AbstractValidator<UserDto>
    {
        public UserDtoValidator()
        {
            RuleFor(r => r.Email).NotEmpty().EmailAddress()
               .WithMessage("Debe ingresar una direccion de correo electrónico válida.");

            RuleFor(r => r.Password).NotEmpty().Length(6, 50)
               .WithMessage("La contraseña debe poseer al menos 6 caracteres.");
        }
    }
}