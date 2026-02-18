namespace SpeakHalo.Infrastructure.Services.Voice;

/// <summary>
/// Real-time audio format conversion between Twilio (mulaw 8kHz) and Gemini Live API (PCM16 16kHz/24kHz).
/// Uses standard ITU-T G.711 mulaw tables. Zero external NuGet dependencies.
/// </summary>
public static class AudioConverter
{
    // Pre-computed mulaw-to-PCM16 lookup table (256 entries) for fast decoding
    private static readonly short[] MulawDecodingTable = GenerateMulawDecodingTable();

    /// <summary>
    /// Convert Twilio mulaw 8kHz audio to Gemini Live API PCM16 16kHz.
    /// </summary>
    public static byte[] TwilioToGemini(byte[] mulawAt8k)
    {
        var pcm16At8k = MulawToPcm16(mulawAt8k);
        return Resample8kTo16k(pcm16At8k);
    }

    /// <summary>
    /// Convert Gemini Live API PCM16 24kHz audio to Twilio mulaw 8kHz.
    /// </summary>
    public static byte[] GeminiToTwilio(byte[] pcm16At24k)
    {
        var pcm16At8k = Resample24kTo8k(pcm16At24k);
        return Pcm16ToMulaw(pcm16At8k);
    }

    /// <summary>
    /// Decode mulaw bytes to PCM16 (little-endian) using pre-computed lookup table.
    /// Each mulaw byte becomes 2 PCM16 bytes (one 16-bit sample).
    /// </summary>
    public static byte[] MulawToPcm16(byte[] mulaw)
    {
        var pcm = new byte[mulaw.Length * 2];
        for (int i = 0; i < mulaw.Length; i++)
        {
            short sample = MulawDecodingTable[mulaw[i]];
            pcm[i * 2] = (byte)(sample & 0xFF);
            pcm[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
        }
        return pcm;
    }

    /// <summary>
    /// Encode PCM16 (little-endian) to mulaw bytes.
    /// Uses the standard bias(33) + segment + quantize algorithm per ITU-T G.711.
    /// </summary>
    public static byte[] Pcm16ToMulaw(byte[] pcm16)
    {
        int sampleCount = pcm16.Length / 2;
        var mulaw = new byte[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            short sample = (short)(pcm16[i * 2] | (pcm16[i * 2 + 1] << 8));
            mulaw[i] = EncodeMulawSample(sample);
        }
        return mulaw;
    }

    /// <summary>
    /// Resample PCM16 from 8kHz to 16kHz using linear interpolation (2x upsample).
    /// </summary>
    public static byte[] Resample8kTo16k(byte[] pcm16At8k)
    {
        int sampleCount = pcm16At8k.Length / 2;
        if (sampleCount == 0) return Array.Empty<byte>();

        // Output is 2x the input samples
        var output = new byte[sampleCount * 2 * 2];
        for (int i = 0; i < sampleCount; i++)
        {
            short current = ReadSample(pcm16At8k, i);
            short next = (i + 1 < sampleCount) ? ReadSample(pcm16At8k, i + 1) : current;

            // Original sample
            int outIdx = i * 2;
            WriteSample(output, outIdx, current);

            // Interpolated sample (midpoint)
            short interpolated = (short)((current + next) / 2);
            WriteSample(output, outIdx + 1, interpolated);
        }
        return output;
    }

    /// <summary>
    /// Resample PCM16 from 24kHz to 8kHz with 3-sample averaging anti-alias filter.
    /// Averages each group of 3 input samples to produce 1 output sample,
    /// preventing aliasing of frequencies above 4kHz.
    /// </summary>
    public static byte[] Resample24kTo8k(byte[] pcm16At24k)
    {
        int sampleCount = pcm16At24k.Length / 2;
        int outputSamples = sampleCount / 3;
        if (outputSamples == 0) return Array.Empty<byte>();

        var output = new byte[outputSamples * 2];
        for (int i = 0; i < outputSamples; i++)
        {
            int idx = i * 3;
            int s0 = ReadSample(pcm16At24k, idx);
            int s1 = (idx + 1 < sampleCount) ? ReadSample(pcm16At24k, idx + 1) : s0;
            int s2 = (idx + 2 < sampleCount) ? ReadSample(pcm16At24k, idx + 2) : s1;
            short averaged = (short)((s0 + s1 + s2) / 3);
            WriteSample(output, i, averaged);
        }
        return output;
    }

    private static short ReadSample(byte[] pcm16, int sampleIndex)
    {
        int byteIndex = sampleIndex * 2;
        return (short)(pcm16[byteIndex] | (pcm16[byteIndex + 1] << 8));
    }

    private static void WriteSample(byte[] pcm16, int sampleIndex, short value)
    {
        int byteIndex = sampleIndex * 2;
        pcm16[byteIndex] = (byte)(value & 0xFF);
        pcm16[byteIndex + 1] = (byte)((value >> 8) & 0xFF);
    }

    /// <summary>
    /// Encode a single PCM16 sample to mulaw using ITU-T G.711 algorithm.
    /// </summary>
    private static byte EncodeMulawSample(short sample)
    {
        const int MULAW_MAX = 0x1FFF;
        const int MULAW_BIAS = 33;

        // Clamp short.MinValue (-32768) to avoid overflow when negating
        // (short)-(-32768) wraps to -32768 due to two's complement)
        if (sample == short.MinValue)
            sample = -32767;

        // Determine sign
        int sign = (sample >> 8) & 0x80;
        if (sign != 0)
            sample = (short)-sample;

        // Clamp
        if (sample > MULAW_MAX)
            sample = MULAW_MAX;

        // Add bias
        sample = (short)(sample + MULAW_BIAS);

        // Find the segment (exponent)
        int exponent = 7;
        int mask = 0x4000;
        for (int i = 0; i < 8; i++)
        {
            if ((sample & mask) != 0)
                break;
            exponent--;
            mask >>= 1;
        }

        // Extract mantissa
        int mantissa = (sample >> (exponent + 3)) & 0x0F;

        // Combine and complement
        byte mulawByte = (byte)(sign | (exponent << 4) | mantissa);
        return (byte)~mulawByte;
    }

    /// <summary>
    /// Generate the 256-entry mulaw-to-PCM16 decoding lookup table.
    /// Standard ITU-T G.711 inverse mapping.
    /// </summary>
    private static short[] GenerateMulawDecodingTable()
    {
        var table = new short[256];
        for (int i = 0; i < 256; i++)
        {
            int mulaw = ~i;
            int sign = mulaw & 0x80;
            int exponent = (mulaw >> 4) & 0x07;
            int mantissa = mulaw & 0x0F;

            int sample = (mantissa << 3) + 0x84;
            sample <<= exponent;
            sample -= 0x84;

            table[i] = (short)(sign != 0 ? -sample : sample);
        }
        return table;
    }
}
