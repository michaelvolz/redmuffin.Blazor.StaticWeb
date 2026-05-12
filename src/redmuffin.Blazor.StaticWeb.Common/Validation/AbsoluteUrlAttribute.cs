using System.ComponentModel.DataAnnotations;

namespace redmuffin.Blazor.StaticWeb.Common.Validation;

/// <summary>
///     Validates that a string property is null or an absolute URL.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class AbsoluteUrlAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string s)
            return ValidationResult.Success;

        return Uri.TryCreate(s, UriKind.Absolute, out _)
            ? ValidationResult.Success
            : new ValidationResult(ErrorMessage ?? $"The {validationContext.DisplayName} field must be an absolute URL.");
    }
}
