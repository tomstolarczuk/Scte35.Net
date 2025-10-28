namespace Scte35.Net.Core;

public static class PayloadValidator
{
	public static void RequireExactLength(ReadOnlySpan<byte> span, int expected)
	{
		if (span.Length != expected)
			throw new InvalidOperationException($"Expected {expected} bytes, got {span.Length}.");
	}

	public static void RequireMinLength(Span<byte> span, int min)
	{
		if (span.Length < min)
			throw new ArgumentException($"Buffer too small (need {min}, got {span.Length}).", nameof(span));
	}

	public static void RequireMinLength(ReadOnlySpan<byte> span, int min)
	{
		if (span.Length < min)
			throw new ArgumentException($"Buffer too small (need {min}, got {span.Length}).", nameof(span));
	}

	public static void RequireRange(int value, int min, int max)
	{
		if (value < min || value > max)
			throw new ArgumentOutOfRangeException(nameof(value), value,
				$"Value must be between {min} and {max} (got {value}).");
	}
}