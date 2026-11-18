using System;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace MicMute
{
    internal sealed class AudioFeedback : IDisposable
    {
        [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
        private static extern uint mciSendString(string command, StringBuilder response, int capacity, IntPtr callback);

        private readonly string alias = "micmute" + Guid.NewGuid().ToString("N");
        private readonly SoundPlayer[] waves = new SoundPlayer[2];
        private readonly SoundPlayer[] tones = new SoundPlayer[2];
        private readonly string[] mp3 = new string[2];
        private readonly SoundSource[] sources;
        private Timer monitor;
        private bool open;

        internal AudioFeedback(AppConfig config)
        {
            sources = new[] { config.UnmuteSource, config.MuteSource };
            for (int i = 0; i < 2; i++)
            {
                if (sources[i] != SoundSource.Style && sources[i] != SoundSource.Custom) continue;
                string path = sources[i] == SoundSource.Custom ? SoundPath(i == 1 ? config.MuteSound : config.UnmuteSound) : null;
                if (path != null)
                {
                    if (IsMp3(path)) mp3[i] = path;
                    else
                    {
                        try
                        {
                            using (var source = File.OpenRead(path))
                            using (var reader = new BinaryReader(source))
                            {
                                long length = source.Length;
                                if (length <= VisualAssets.MaxAssetBytes)
                                {
                                    byte[] data = reader.ReadBytes((int)length);
                                    if (data.Length == length) waves[i] = LoadWave(data);
                                }
                            }
                        }
                        catch (IOException) { }
                        catch (UnauthorizedAccessException) { }
                    }
                }
                if (config.SoundStyle > 0) tones[i] = LoadWave(Tone(config.SoundStyle, i == 1));
            }
        }

        private static bool IsMp3(string path) { return string.Equals(Path.GetExtension(path), ".mp3", StringComparison.OrdinalIgnoreCase); }
        internal static bool IsSoundFile(string path) { return SoundPath(path) != null; }

        private static string SoundPath(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || path.IndexOfAny(new[] { '"', '\r', '\n', '\0' }) >= 0) return null;
                string full = Path.GetFullPath(path);
                if (full.StartsWith(@"\\", StringComparison.Ordinal) || full.IndexOf(':', 2) >= 0) return null;
                bool mp3File = IsMp3(full);
                if (!mp3File && !string.Equals(Path.GetExtension(full), ".wav", StringComparison.OrdinalIgnoreCase)) return null;
                using (var stream = File.OpenRead(full))
                {
                    if (stream.Length < 12 || stream.Length > VisualAssets.MaxAssetBytes) return null;
                    byte[] h = new byte[12];
                    if (stream.Read(h, 0, h.Length) != h.Length) return null;
                    bool valid = mp3File
                        ? (h[0] == 'I' && h[1] == 'D' && h[2] == '3') || (h[0] == 0xff && (h[1] & 0xe0) == 0xe0)
                        : h[0] == 'R' && h[1] == 'I' && h[2] == 'F' && h[3] == 'F' && h[8] == 'W' && h[9] == 'A' && h[10] == 'V' && h[11] == 'E';
                    return valid ? full : null;
                }
            }
            catch (Exception ex)
            {
                if (ex is IOException || ex is ArgumentException || ex is UnauthorizedAccessException ||
                    ex is NotSupportedException || ex is System.Security.SecurityException) return null;
                throw;
            }
        }

        private static SoundPlayer LoadWave(byte[] bytes)
        {
            var stream = new MemoryStream(bytes, false);
            var player = new SoundPlayer(stream);
            try { player.Load(); return player; }
            catch (InvalidOperationException) { player.Dispose(); stream.Dispose(); return null; }
            catch (IOException) { player.Dispose(); stream.Dispose(); return null; }
        }

        internal bool TryPlayMp3(string path)
        {
            CloseMp3();
            string full = SoundPath(path);
            if (full == null || !IsMp3(full)) return false;
            if (Command("open \"" + full + "\" type mpegvideo alias " + alias) != 0) return false;
            open = true;
            if (Command("play " + alias) != 0) { CloseMp3(); return false; }
            if (monitor == null)
            {
                monitor = new Timer { Interval = 200 };
                monitor.Tick += (s, e) => { if (!IsPlayingMp3) CloseMp3(); };
            }
            monitor.Start();
            return true;
        }

        internal bool IsPlayingMp3
        {
            get
            {
                if (!open) return false;
                var response = new StringBuilder(32);
                return mciSendString("status " + alias + " mode", response, response.Capacity, IntPtr.Zero) == 0 &&
                    string.Equals(response.ToString(), "playing", StringComparison.OrdinalIgnoreCase);
            }
        }

        private static uint Command(string command) { return mciSendString(command, null, 0, IntPtr.Zero); }
        private void CloseMp3()
        {
            if (monitor != null) monitor.Stop();
            if (open) { Command("close " + alias); open = false; }
        }

        internal void Play(bool muted)
        {
            CloseMp3();
            foreach (var player in waves) if (player != null) player.Stop();
            foreach (var player in tones) if (player != null) player.Stop();
            int i = muted ? 1 : 0;
            if (sources[i] == SoundSource.Silent) return;
            if ((mp3[i] != null && TryPlayMp3(mp3[i])) || TryPlayWave(waves[i]) || TryPlayWave(tones[i])) return;
            WindowsSound(sources[i], muted).Play();
        }

        internal static SystemSound WindowsSound(SoundSource source, bool muted)
        {
            switch (source)
            {
                case SoundSource.Error: return SystemSounds.Hand;
                case SoundSource.Information: return SystemSounds.Asterisk;
                case SoundSource.Warning: return SystemSounds.Exclamation;
                case SoundSource.Question: return SystemSounds.Question;
                case SoundSource.Beep: return SystemSounds.Beep;
                default: return muted ? SystemSounds.Hand : SystemSounds.Asterisk;
            }
        }

        private static bool TryPlayWave(SoundPlayer player)
        {
            if (player == null) return false;
            try { player.Play(); return true; }
            catch (InvalidOperationException) { return false; }
            catch (IOException) { return false; }
        }

        internal static byte[] Tone(int style, bool muted)
        {
            const int rate = 22050, perNote = 1543;
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                int bytes = perNote * 2 * sizeof(short);
                writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + bytes);
                writer.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
                writer.Write((short)1); writer.Write((short)1); writer.Write(rate);
                writer.Write(rate * 2); writer.Write((short)2); writer.Write((short)16);
                writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(bytes);
                for (int note = 0; note < 2; note++)
                {
                    double frequency = (note == 0) == muted ? 740 : 494;
                    if (style == 2) frequency *= 1.3;
                    for (int j = 0; j < perNote; j++)
                    {
                        double phase = j * frequency / rate * Math.PI * 2;
                        double sample = Math.Sin(phase);
                        if (style == 2) sample = .65 * sample + .35 * Math.Sin(3 * phase);
                        double envelope = Math.Min(1, j / 100.0) * Math.Pow(1 - j / (double)perNote, 1.5);
                        writer.Write((short)(sample * envelope * 4600));
                    }
                }
                writer.Flush();
                return stream.ToArray();
            }
        }

        public void Dispose()
        {
            CloseMp3();
            if (monitor != null) monitor.Dispose();
            foreach (var player in waves) DisposeWave(player);
            foreach (var player in tones) DisposeWave(player);
        }

        private static void DisposeWave(SoundPlayer player)
        {
            if (player == null) return;
            player.Stop(); player.Stream.Dispose(); player.Dispose();
        }
    }
}
