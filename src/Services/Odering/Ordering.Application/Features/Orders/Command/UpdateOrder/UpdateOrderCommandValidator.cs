using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ordering.Application.Features.Orders.Command.UpdateOrder
{
    public class UpdateOrderCommandValidator : AbstractValidator<UpdateOrderCommand>
    {
        public UpdateOrderCommandValidator()
        {
            RuleFor(p => p.UserName).NotEmpty().
                WithMessage("{UserName} is Required").
                NotNull().MaximumLength(50).
                WithMessage("{UserName} must not exceed 50 Character");


            RuleFor(p => p.EmailAddress).NotEmpty().
                WithMessage("{EmailAddress} is Required");

            RuleFor(p => p.TotalPrice).NotEmpty().
                WithMessage("{TotalPrice} should not be Empty").
                GreaterThan(0).
                WithMessage("{TotalPrice} should  be > 0");
        }
    }
}
