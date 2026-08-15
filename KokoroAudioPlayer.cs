using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using KokoroSharp.Core;
using KokoroSharp.Utilities;
using NAudio.Wave;
using OpenTK.Audio.OpenAL;

namespace talk
{
    // KokoroSharp's own player never makes a sound on Linux or macOS.
    //
    // It generates 256 OpenAL buffers, fills only the handful its samples
    // actually fill, and then queues all 256 at once. OpenAL refuses a queue
    // that contains empty buffers, so the call fails with AL_INVALID_OPERATION,
    // nothing at all is queued, and the source is asked to play silence. The
    // library never checks, so the phrase is reported as spoken about as fast
    // as it was synthesized: on a twenty second phrase the completion callback
    // came back in under two seconds, which is the shape of the bug from the
    // outside.
    //
    // The package leaves a hook for exactly this - CrossPlatformHelper takes a
    // player of our own - so rather than wait for a release the playback is
    // done here. Windows goes through NAudio and works, so it is left alone.
    //
    // A phrase arrives whole rather than as a stream: KokoroSharp hands over
    // the finished samples and only wants to be told how far through them we
    // are. So there is one buffer holding the lot, and none of the queue
    // juggling the original was attempting.
    class KokoroAudioPlayer : KokoroWaveOutEvent
    {
        // Installed before the model is loaded, because the player is built
        // with the synthesizer and the hook is read only once.
        public static void Install()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }
            if (CrossPlatformHelper.CustomAudioPlayer == null)
            {
                CrossPlatformHelper.CustomAudioPlayer = new KokoroAudioPlayer();
            }
        }

        private ALDevice device;
        private ALContext context;
        private int source;
        private int buffer;

        // The thread that follows the phrase while it plays, and the flag that
        // tells it to give up early. Written by Stop from the key listener and
        // read by the thread, so it is volatile rather than plain.
        private Thread watching;
        private volatile bool stopping;

        private volatile PlaybackState state = PlaybackState.Stopped;

        public override PlaybackState PlaybackState
        {
            get { return state; }
        }

        public override void Play()
        {
            Stop();

            if (!Open())
            {
                // Nothing to play through. The phrase is marked as read rather
                // than left hanging, because the caller waits on the position
                // reaching the end and would otherwise wait forever.
                Finish();
                return;
            }

            byte[] samples = ReadAll();
            if (samples.Length == 0)
            {
                Finish();
                return;
            }

            stopping = false;
            state = PlaybackState.Playing;

            // Detached first: the buffer cannot be refilled while the source
            // still holds the previous phrase's copy of it.
            AL.Source(source, ALSourcei.Buffer, 0);
            Fill(samples);
            AL.Source(source, ALSourcei.Buffer, buffer);
            AL.Source(source, ALSourcef.Gain, Volume);
            AL.SourcePlay(source);

            watching = new Thread(Watch);
            watching.IsBackground = true;
            watching.Start();
        }

        // KokoroSharp reads the phrase's progress off the stream position
        // rather than asking the player, so the position is what is kept up to
        // date here. OpenAL counts in bytes from the start of the buffer, which
        // is the same thing the stream counts in.
        private void Watch()
        {
            while (!stopping &&
                AL.GetSource(source, ALGetSourcei.SourceState) == (int)ALSourceState.Playing)
            {
                stream.Position = AL.GetSource(source, ALGetSourcei.ByteOffset);
                Thread.Sleep(10);
            }

            // A phrase that was cut short leaves the position where it stopped,
            // which is how KokoroSharp tells a cancelled phrase from a finished
            // one and how much of it was heard.
            if (stopping)
            {
                stream.Position = AL.GetSource(source, ALGetSourcei.ByteOffset);
            }
            else
            {
                stream.Position = stream.Length;
            }
            state = PlaybackState.Stopped;
        }

        // Stop is called between phrases as well as by the key listener, so it
        // has to leave the device open: the original closed it here, which is
        // why every phrase after the first had nothing to play through even
        // once the queueing was fixed.
        public override void Stop()
        {
            stopping = true;
            if (source != 0)
            {
                AL.SourceStop(source);
            }

            Thread thread = watching;
            if (thread != null && thread != Thread.CurrentThread)
            {
                thread.Join();
            }
            watching = null;
            state = PlaybackState.Stopped;
        }

        public override void SetVolume(float volume)
        {
            // Set on the way in as well as kept, because the volume is chosen
            // before the phrase starts and there is no source to set it on yet.
            Volume = Math.Clamp(volume, 0f, 1f);
            if (source != 0)
            {
                AL.Source(source, ALSourcef.Gain, Volume);
            }
        }

        public override void Dispose()
        {
            Stop();
            if (source != 0)
            {
                AL.DeleteSource(source);
                source = 0;
            }
            if (buffer != 0)
            {
                AL.DeleteBuffer(buffer);
                buffer = 0;
            }
            if (context != ALContext.Null)
            {
                ALC.MakeContextCurrent(ALContext.Null);
                ALC.DestroyContext(context);
                context = ALContext.Null;
            }
            if (device != ALDevice.Null)
            {
                ALC.CloseDevice(device);
                device = ALDevice.Null;
            }
        }

        // The device is opened once and kept for the life of the process, so
        // the second phrase does not pay for it again.
        private bool Open()
        {
            if (source != 0)
            {
                return true;
            }

            try
            {
                device = ALC.OpenDevice(null);
                if (device == ALDevice.Null)
                {
                    return false;
                }
                context = ALC.CreateContext(device, (int[])null);
                if (context == ALContext.Null)
                {
                    return false;
                }
                ALC.MakeContextCurrent(context);
                source = AL.GenSource();
                buffer = AL.GenBuffer();
                return source != 0;
            }
            catch (Exception)
            {
                // A machine with no audio device is something the settings
                // screen already warns about; reaching here anyway should leave
                // the phrase silent rather than take the menu down.
                return false;
            }
        }

        private byte[] ReadAll()
        {
            using (MemoryStream all = new MemoryStream())
            {
                stream.Position = 0;
                stream.CopyTo(all);
                stream.Position = 0;
                return all.ToArray();
            }
        }

        // Kokoro synthesizes at one channel of 16 bit samples, which is what
        // KokoroSharp's wave format says and what the buffer is told to expect.
        private unsafe void Fill(byte[] samples)
        {
            fixed (byte* bytes = samples)
            {
                AL.BufferData(buffer, ALFormat.Mono16, (IntPtr)bytes, samples.Length,
                    stream.WaveFormat.SampleRate);
            }
        }

        // Used when there is nothing to play: the phrase is over, and the
        // position says it was heard to the end so the caller stops waiting.
        private void Finish()
        {
            stream.Position = stream.Length;
            state = PlaybackState.Stopped;
        }
    }
}
