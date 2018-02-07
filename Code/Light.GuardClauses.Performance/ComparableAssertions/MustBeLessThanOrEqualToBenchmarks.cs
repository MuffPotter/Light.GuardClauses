﻿using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;

namespace Light.GuardClauses.Performance.ComparableAssertions
{
    [ClrJob, CoreJob]
    [MemoryDiagnoser]
    [DisassemblyDiagnoser]
    public class MustBeLessThanOrEqualToBenchmarks
    {
        public readonly float First = 42.7f;
        public readonly float Second = 78.21f;

        [Benchmark(Baseline = true)]
        public float BaseVersion()
        {
            if (First > Second) throw new ArgumentOutOfRangeException(nameof(First));
            return First;
        }

        [Benchmark]
        public float BaseVersionWithCompareTo()
        {
            // ReSharper disable once ImpureMethodCallOnReadonlyValueField
            if (First.CompareTo(Second) > 0) throw new ArgumentOutOfRangeException(nameof(First));
            return First;
        }

        [Benchmark]
        public float LightGuardClausesWithParameterName() => First.MustBeLessThanOrEqualTo(Second, nameof(First));

        [Benchmark]
        public float LightGuardClausesWithCustomException() => First.MustBeLessThanOrEqualTo(Second, () => new Exception());

        [Benchmark]
        public float LightGuardClausesWithExceptionOneParameter() => First.MustBeLessThanOrEqualTo(Second, p => new Exception($"{p} is not less."));

        [Benchmark]
        public float LightGuardClausesWithExceptionTwoParameters() => First.MustBeLessThanOrEqualTo(Second, (p, b) => new Exception($"{p} is not less than {b}."));

        [Benchmark]
        public float OldVersion() => First.OldMustBeLessThanOrEqualTo(Second, nameof(First));
    }

    public static class MustBeLessThanOrEqualToExtensionMethods
    {
        public static T OldMustBeLessThanOrEqualTo<T>(this T parameter, T boundary, string parameterName = null, string message = null, Func<Exception> exception = null) where T : IComparable<T>
        {
            if (parameter.CompareTo(boundary) <= 0)
                return parameter;

            throw exception != null ? exception() : new ArgumentOutOfRangeException(parameterName, parameter, message ?? $"{parameterName ?? "The value"} must be less than {boundary}, but you specified {parameter}.");
        }
    }
}