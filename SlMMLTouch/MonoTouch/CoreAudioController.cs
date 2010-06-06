using System;
using System.IO;
using MonoTouch.AudioToolbox;

namespace SlMML.MonoTouch
{
	public class CoreAudioController
	{
		public CoreAudioController ()
		{
			var ad = new AudioStreamBasicDescription () {
				SampleRate = 44100.0,
				Format = AudioFormatType.LinearPCM,
				FormatFlags = AudioFormatFlags.LinearPCMIsSignedInteger | AudioFormatFlags.IsPacked,
				FramesPerPacket = 1,
				ChannelsPerFrame = 1,
				BitsPerChannel = 16,
				BytesPerPacket = 2,
				BytesPerFrame = 2,
				Reserved = 0};
			audioq = new OutputAudioQueue (ad);
			audioq.OutputCompleted += delegate(object sender, OutputCompletedEventArgs e) {
				EnqueueNextBuffer (e.IntPtrBuffer);
			};
			audioq.AllocateBuffer (synth_buffer.Length * ad.BytesPerPacket, out audio_buffer);
		}
		
		OutputAudioQueue audioq;
		byte [] synth_buffer = new byte [0x1000];
		IntPtr audio_buffer;

		public event Func<ArraySegment<byte>> GetSampleBuffer;
		
		public void Start ()
		{
			EnqueueNextBuffer (audio_buffer);
			audioq.Start ();
		}
		
		public void Stop ()
		{
			audioq.Stop (false);
		}

		void EnqueueNextBuffer (IntPtr buffer)
		{
			if (GetSampleBuffer != null) {
				ArraySegment<byte> seg = GetSampleBuffer ();
				if (seg.Array == null)
					return;
				unsafe {
					fixed (byte* ptr = seg.Array)
						OutputAudioQueue.FillAudioData (buffer, 0, new IntPtr ((void*)ptr), seg.Offset, seg.Count);
					audioq.EnqueueBuffer (buffer, seg.Count, null);
				}
			}
			else
				throw new InvalidOperationException ("Buffer receive callback must be set before playing a stream.");
		}
	}
}

