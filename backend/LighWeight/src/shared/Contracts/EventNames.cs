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

    /// <summary>Events emitted by the <b>bodymetrics</b> module.</summary>
    public static class BodyMetrics
    {
        /// <summary><c>bodymetrics.weight.recorded.v1</c> (registered or corrected)</summary>
        public const string WeightRecorded = "bodymetrics.weight.recorded.v1";

        /// <summary><c>bodymetrics.weight.deleted.v1</c></summary>
        public const string WeightDeleted = "bodymetrics.weight.deleted.v1";

        /// <summary><c>bodymetrics.perimeters.recorded.v1</c> (registered or corrected)</summary>
        public const string PerimetersRecorded = "bodymetrics.perimeters.recorded.v1";

        /// <summary><c>bodymetrics.perimeters.deleted.v1</c></summary>
        public const string PerimetersDeleted = "bodymetrics.perimeters.deleted.v1";

        /// <summary><c>bodymetrics.skinfolds.recorded.v1</c> (registered or corrected)</summary>
        public const string SkinFoldsRecorded = "bodymetrics.skinfolds.recorded.v1";

        /// <summary><c>bodymetrics.skinfolds.deleted.v1</c></summary>
        public const string SkinFoldsDeleted = "bodymetrics.skinfolds.deleted.v1";
    }

    /// <summary>Events emitted by the <b>training</b> module.</summary>
    public static class Training
    {
        /// <summary><c>training.session.finished.v1</c> (the whole session with its sets)</summary>
        public const string SessionFinished = "training.session.finished.v1";
    }

    /// <summary>Events emitted by the <b>dashboard</b> module.</summary>
    public static class Dashboard
    {
        /// <summary><c>dashboard.rebuild.requested.v1</c> (the source modules republish the user's data)</summary>
        public const string RebuildRequested = "dashboard.rebuild.requested.v1";
    }
}