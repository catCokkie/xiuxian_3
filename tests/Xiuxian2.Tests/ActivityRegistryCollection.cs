namespace Xiuxian.Tests;

// These tests mutate the shared static ActivityRegistry and must not overlap.
[CollectionDefinition("ActivityRegistry", DisableParallelization = true)]
public sealed class ActivityRegistryCollection
{
}
