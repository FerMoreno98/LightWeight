namespace LightWeight.shared.Contracts;

/// <summary>
/// Strongly-typed registry of every cross-module integration event name.
/// Use these constants instead of raw string literals when publishing or subscribing to events.
/// </summary>
/// <remarks>
/// One nested static class per emitting module. Each constant holds the full, lowercase
/// dot-separated event name including its version suffix (e.g. "auth.user.created.v1").
/// When a new event version is introduced, add a new constant — never change an existing one.
/// </remarks>
public static class EventNames
{
    /// <summary>Events emitted by the <b>auth</b> module.</summary>
    public static class Auth
    {
        /// <summary><c>auth.user.created.v1</c></summary>
        public const string UserCreated = "auth.user.created.v1";
    }

    /// <summary>Events emitted by the <b>userprofile</b> module.</summary>
    public static class UserProfile
    {
        /// <summary><c>userprofile.profile.completed.v1</c></summary>
        public const string ProfileCompleted = "userprofile.profile.completed.v1";

        /// <summary><c>userprofile.stage.changed.v1</c></summary>
        public const string StageChanged = "userprofile.stage.changed.v1";
    }
}