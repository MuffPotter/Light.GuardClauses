﻿using System;
using BenchmarkDotNet.Attributes;
using Light.GuardClauses.Exceptions;

namespace Light.GuardClauses.Performance.CommonAssertions
{
    public class MustBeOfTypeBenchmark
    {
        public object Instance = new SampleEntity(Guid.NewGuid());

        [Benchmark(Baseline = true)]
        public SampleEntity BaseVersion()
        {
            if (Instance == null) throw new ArgumentNullException(nameof(Instance));
            if (!(Instance is SampleEntity entity)) throw new TypeCastException(nameof(Instance));

            return entity;
        }

        [Benchmark]
        public SampleEntity LightGuardClausesWithParameterName() => Instance.MustBeOfType<SampleEntity>(nameof(Instance));

        [Benchmark]
        public SampleEntity LightGuardClausesWithCustomException() => Instance.MustBeOfType<SampleEntity>(r => new Exception($"\"{r}\" is not the correct type."));

        [Benchmark]
        public SampleEntity OldVersion() => Instance.OldMustBeOfType<SampleEntity>(nameof(Instance));
    }

    public static class MustBeOfTypeExtensionMethods
    {
        public static T OldMustBeOfType<T>(this object parameter, string parameterName = null, string message = null, Func<Exception> exception = null)
        {
            {
                if (parameter is T castedValue)
                    return castedValue;

                throw exception?.Invoke() ?? new TypeCastException(parameterName, message ?? $"{parameterName ?? "The object"} is of type {parameter.GetType().FullName} and cannot be downcasted to {typeof(T).FullName}.");
            }
        }
    }
}