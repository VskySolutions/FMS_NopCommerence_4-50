using System;
using System.Linq;
using FluentValidation;
using FluentValidation.Results;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;
using Nop.Web.Models.Estimate;


namespace Nop.Web.Validators.Estimates
{
    public class EstimateValidator : BaseNopValidator<EstimateDetailsModel>
    {
        public EstimateValidator(ILocalizationService localizationService,
           IStateProvinceService stateProvinceService)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessageAwait(localizationService.GetResourceAsync("This field is required."));

        }
    }
}
