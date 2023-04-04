namespace GameRandomizer
{
	// Permuted Congruential Generator Class
	public class Pcg
	{

		ulong _state;
		const ulong ShiftedIncrement = 721347520444481703ul;
		ulong _increment = 1442695040888963407ul;
		const ulong Multiplier = 6364136223846793005ul;
		const double ToDouble01 = 1.0 / 4294967296.0;

		public int Next(int minInclusive, int maxExclusive)
		{
			if (maxExclusive <= minInclusive)
				throw new ArgumentException("MaxExclusive must be larger than MinInclusive");

			uint uMaxExclusive = unchecked((uint)(maxExclusive - minInclusive));
			uint threshold = (uint)(-uMaxExclusive) % uMaxExclusive;

			while (true)
			{
				uint result = NextUInt();
				if (result >= threshold)
					return (int)(unchecked((result % uMaxExclusive) + minInclusive));
			}
		}

		public uint NextUInt()
		{
			ulong oldState = _state;
			_state = unchecked(oldState * Multiplier + _increment);
			uint xorShifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
			int rot = (int)(oldState >> 59);
			uint result = (xorShifted >> rot) | (xorShifted << ((-rot) & 31));
			return result;
		}

		public void SetStream(ulong sequence)
		{
			_increment = (sequence << 1) | 1;
		}

		public Pcg(int seed) : this((ulong)seed)
		{
		}

		public Pcg(ulong seed, ulong sequence = ShiftedIncrement)
		{
			Initialize(seed, sequence);
		}

		void Initialize(ulong seed, ulong initseq)
		{
			_state = 0ul;
			SetStream(initseq);
			NextUInt();
			_state += seed;
			NextUInt();
		}

	}
}
