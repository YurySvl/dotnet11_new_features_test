// ASP.NET Core 11: async validation for minimal APIs.
//
// AsyncValidationAttribute (new in .NET 11) lets a validation attribute await I/O.
// builder.Services.AddValidation() makes minimal APIs run these validators automatically
// and return a 400 ValidationProblem before the handler is called.

using System.ComponentModel.DataAnnotations;

namespace Showcase.Api.Validation;

public record UserRegistration(
    [property: Required, StringLength(20, MinimumLength = 3)] [property: UniqueUsername] string Username,
    [property: Required, EmailAddress] string Email);

public sealed class UniqueUsernameAttribute : AsyncValidationAttribute
{
    public UniqueUsernameAttribute()
    {
        // Used as the message AND as the localization key (see Resources/ValidationMessages*.resx).
        // Like built-in attributes, {0} in the message is the member's display name, not the value.
        ErrorMessage = "UsernameTaken";
    }

    // This attribute only validates asynchronously.
    protected override ValidationResult? IsValid(object? value, ValidationContext context) =>
        throw new InvalidOperationException("Validate this attribute with IsValidAsync.");

    protected override async Task<ValidationResult?> IsValidAsync(
        object? value, ValidationContext context, CancellationToken cancellationToken)
    {
        var users = context.GetRequiredService<IUserDirectory>();
        if (value is string name && await users.ExistsAsync(name, cancellationToken))
        {
            return new ValidationResult(FormatErrorMessage(context.DisplayName), [context.MemberName!]);
        }
        return ValidationResult.Success;
    }
}
