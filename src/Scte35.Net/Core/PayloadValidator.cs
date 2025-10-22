using System.Runtime.CompilerServices;

namespace Scte35.Net.Core;

public static class PayloadValidator
{
	public static void RequireExactLength(
		ReadOnlySpan<byte> span,
		int expected,
		[CallerMemberName] string? caller = null)
	{
		if (span.Length != expected)
			throw new InvalidOperationException(
				$"{caller}: expected {expected} bytes, got {span.Length}.");
	}

	public static void RequireMinLength(
		Span<byte> span,
		int min,
		[CallerMemberName] string? caller = null)
	{
		if (span.Length < min)
			throw new ArgumentException(
				$"{caller}: buffer too small (need {min}, got {span.Length}).", nameof(span));
	}

	public static void RequireMinLength(
		ReadOnlySpan<byte> span,
		int min,
		[CallerMemberName] string? caller = null)
	{
		if (span.Length < min)
			throw new ArgumentException(
				$"{caller}: buffer too small (need {min}, got {span.Length}).", nameof(span));
	}

	public static void RequireRange(
		int value,
		int min,
		int max,
		[CallerMemberName] string? caller = null)
	{
		if (value < min || value > max)
			throw new ArgumentOutOfRangeException(nameof(value), value,
				$"{caller}: value must be between {min} and {max} (got {value}).");
	}
}