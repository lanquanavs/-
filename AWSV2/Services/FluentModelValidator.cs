﻿using FluentValidation;
using Stylet;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AWSV2.Services
{
    public class FluentModelValidator<T> : IModelValidator<T>
    {
        private readonly IValidator<T> validator;
        private T subject;

        public FluentModelValidator(IValidator<T> validator)
        {
            this.validator = validator;
        }

        public void Initialize(object subject)
        {
            this.subject = (T)subject;
        }

        public Task<IEnumerable<string>> ValidatePropertyAsync(string propertyName)
        {
            var errors = this.validator.Validate(this.subject, propertyName).Errors.Select(x => x.ErrorMessage);
            return Task.FromResult(errors);
        }

        public async Task<Dictionary<string, IEnumerable<string>>> ValidateAllPropertiesAsync()
        {
            // If someone's calling us synchronously, and ValidationAsync does not complete synchronously,
            // we'll deadlock unless we continue on another thread.
            return (await this.validator.ValidateAsync(this.subject).ConfigureAwait(false))
                .Errors.GroupBy(x => x.PropertyName)
                .ToDictionary(x => x.Key, x => x.Select(failure => failure.ErrorMessage));
        }
    }
}
