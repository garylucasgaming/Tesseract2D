using Xunit;

// TypeResolver and Log use process-wide static state, and tests write to temp files.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
