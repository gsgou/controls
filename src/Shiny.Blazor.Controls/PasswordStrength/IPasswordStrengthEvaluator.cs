namespace Shiny.Blazor.Controls;

/// <summary>
/// Scores a password and reports which rules it meets. Implement this to replace the built-in
/// heuristic with something better informed — zxcvbn, a Have I Been Pwned range query, or your own
/// server's policy endpoint.
/// </summary>
/// <remarks>
/// <para>
/// The method is asynchronous and cancellable precisely so a network-backed implementation is
/// possible. <see cref="PasswordStrength"/> debounces keystrokes and cancels the previous evaluation
/// before starting the next, so an implementation that goes to the wire is not asked to answer for
/// every character typed — but it must still honour the token, because the answer to a password the
/// user has already changed is worthless.
/// </para>
/// <para>
/// <b>Never send the password itself anywhere.</b> The reason HIBP's range API takes the first five
/// characters of the SHA-1 hash and returns a bucket of suffixes is so the password — and its full
/// hash — never leaves the device. An implementation that POSTs the plaintext to check it has turned
/// a strength meter into a credential exfiltrator.
/// </para>
/// </remarks>
public interface IPasswordStrengthEvaluator
{
    /// <summary>Judge a password against a policy.</summary>
    /// <param name="request">The candidate and the rules it has to meet.</param>
    /// <param name="cancellationToken">
    /// Cancelled when the user types again. Implementations that do I/O must pass it through.
    /// </param>
    ValueTask<PasswordStrengthResult> EvaluateAsync(
        PasswordStrengthRequest request,
        CancellationToken cancellationToken = default
    );
}
