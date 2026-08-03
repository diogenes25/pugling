namespace Pugling.Api.Controllers;

/// <summary>
/// Central route building blocks. The version segment lives only here – a future version change
/// (or running a v2 in parallel) therefore doesn't touch the controllers across the board.
/// </summary>
public static class ApiRoutes
{
    /// <summary>Prefix of all versioned routes; <c>{version:apiVersion}</c> is replaced by Asp.Versioning.</summary>
    public const string V1 = "api/v{version:apiVersion}";

    // The three domain tiers (see docs/grundprinzip.md) are the first path segment after the version.
    // The prefix is resource taxonomy, not the auth wall: actual access stays the [Authorize] on the
    // controller or action. Individual routes are dual on purpose where the resource itself is read by two
    // tiers - the class test for instance, which the supervisor plans and the student practises on, so its
    // reading actions sit under `supervisor/` and are open to the child.
    // Dual is *not* an excuse for a tier-gated route on a foreign prefix: once only one tier may call it, the
    // prefix has to follow (B-82).

    /// <summary>Tier 1 – Creator: create content/exercises (subject → chapter → exercise, stores, tags).</summary>
    public const string Creator = V1 + "/creator";

    /// <summary>Tier 2 – Supervisor: study plans, goals/points, shop/offers, child management.</summary>
    public const string Supervisor = V1 + "/supervisor";

    /// <summary>Tier 3 – Student: play, earn, buy/activate, own progress.</summary>
    public const string Student = V1 + "/student";
}
