﻿using System.ComponentModel.DataAnnotations;

namespace Soul.Shop.Infrastructure.Models;

public abstract class ValidatableObject
{
    public virtual bool IsValid()
    {
        return Validate().Count == 0;
    }

    protected virtual IList<ValidationResult> Validate()
    {
        IList<ValidationResult> validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(this, new ValidationContext(this, null, null), validationResults, true);
        return validationResults;
    }
}