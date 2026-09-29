#if NET48
using System;
using System.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    internal sealed class RequiredMemberAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    internal sealed class CompilerFeatureRequiredAttribute : Attribute
    {
        public CompilerFeatureRequiredAttribute(string featureName)
        {
            FeatureName = featureName;
        }

        public string FeatureName { get; }
    }
}

namespace System
{
    public readonly struct Index : IEquatable<Index>
    {
        private readonly int _value;

        public Index(int value, bool fromEnd)
        {
            _value = fromEnd ? ~value : value;
        }

        public int Value => _value < 0 ? ~_value : _value;

        public bool IsFromEnd => _value < 0;

        public static Index Start => default;

        public static Index End => new Index(0, true);

        public static Index FromStart(int value) => new Index(value, false);

        public static Index FromEnd(int value) => new Index(value, true);

        public int GetOffset(int length)
        {
            int offset = _value;
            if (offset < 0)
            {
                offset += length;
            }
            return offset;
        }

        public bool Equals(Index other) => _value == other._value;

        public override bool Equals(object obj) => obj is Index other && Equals(other);

        public override int GetHashCode() => _value;

        public static implicit operator Index(int value) => FromStart(value);

        public override string ToString() => IsFromEnd ? "^" + Value : Value.ToString();
    }

    public readonly struct Range : IEquatable<Range>
    {
        public Range(Index start, Index end)
        {
            Start = start;
            End = end;
        }

        public Index Start { get; }

        public Index End { get; }

        public static Range All => new Range(Index.Start, Index.End);

        public static Range StartAt(Index start) => new Range(start, Index.End);

        public static Range EndAt(Index end) => new Range(Index.Start, end);

        public bool Equals(Range other) => Start.Equals(other.Start) && End.Equals(other.End);

        public override bool Equals(object obj) => obj is Range other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (Start.GetHashCode() * 397) ^ End.GetHashCode();
            }
        }

        public override string ToString() => Start.ToString() + ".." + End.ToString();

        public (int Offset, int Length) GetOffsetAndLength(int length)
        {
            int startOffset = Start.GetOffset(length);
            int endOffset = End.GetOffset(length);
            int count = endOffset - startOffset;
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }
            return (startOffset, count);
        }
    }
}
#endif

public static class MathCompat
{
    public static double Clamp(double value, double min, double max)
    {
        if (value < min) return min;
        return value > max ? max : value;
    }

    public static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}

#if NET48
namespace System.Collections.Generic
{
    public static class KeyValuePairCompat
    {
        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp, out TKey key, out TValue value)
        {
            key = kvp.Key;
            value = kvp.Value;
        }
    }
}
#endif