using System.Reflection;
using Xunit.Sdk;

namespace MarketData.PriceSimulator.Tests
{
    /// <summary>
    /// Marks a test as a statistical test that uses SkippableFact for runtime skip logic.
    /// Statistical tests are skipped by default unless RUN_STATISTICAL_TESTS=true environment variable is set.
    /// </summary>
    /// <remarks>
    /// Tests marked with this attribute must call <see cref="StatisticalTestGuard.EnsureEnabled"/> 
    /// at the start of the test method to check the environment variable and skip if not enabled.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [Trait("Category", "Statistical")]
    [Trait("Speed", "Slow")]
    public sealed class StatisticalFactAttribute : SkippableFactAttribute
    {
    }

    /// <summary>
    /// Marks a parameterized test as a statistical test that uses SkippableTheory for runtime skip logic.
    /// Statistical tests are skipped by default unless RUN_STATISTICAL_TESTS=true environment variable is set.
    /// </summary>
    /// <remarks>
    /// Tests marked with this attribute must call <see cref="StatisticalTestGuard.EnsureEnabled"/> 
    /// at the start of the test method to check the environment variable and skip if not enabled.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [Trait("Category", "Statistical")]
    [Trait("Speed", "Slow")]
    public sealed class StatisticalTheoryAttribute : SkippableTheoryAttribute
    {
    }

    /// <summary>
    /// Helper class for statistical test configuration.
    /// </summary>
    public static class StatisticalTestGuard
    {
        private const string SkipMessage = "Statistical tests are disabled by default. Set environment variable RUN_STATISTICAL_TESTS=true to enable.";

        /// <summary>
        /// Ensures statistical tests are enabled. Skips the test if RUN_STATISTICAL_TESTS environment variable is not set to "true".
        /// Call this at the beginning of every statistical test method.
        /// </summary>
        public static void EnsureEnabled()
        {
            Skip.IfNot(IsEnabled(), SkipMessage);
        }

        /// <summary>
        /// Returns true if statistical tests are enabled via the RUN_STATISTICAL_TESTS environment variable.
        /// </summary>
        public static bool IsEnabled()
        {
            var runStatisticalTests = Environment.GetEnvironmentVariable("RUN_STATISTICAL_TESTS");
            return string.Equals(runStatisticalTests, "true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
