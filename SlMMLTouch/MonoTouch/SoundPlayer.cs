using System;
using SlMML;
using SlMML.MonoTouch;

namespace SlMML.MonoTouch
{
	public class SoundPlayer
	{
		Sequencer sequencer;
		CoreAudioController audio = new CoreAudioController ();

		public SoundPlayer ()
		{
			audio.GetSampleBuffer += delegate() {
				return ProcessNextSamples ();
			};
		}
		
		public bool IsPlaying { get; private set; }
		public event Action PlayCompleted;
		
		ArraySegment<byte> ProcessNextSamples ()
		{
			if (sequencer == null)
				return default (ArraySegment<byte>);
			var ret = sequencer.ProcessNextSamples ();
			if (ret.Array == null) {
				IsPlaying = false;
				// premises that it does not get called more than once.
				if (PlayCompleted != null)
					PlayCompleted ();
			}
			return ret;
		}
		
		public void LoadMusic (string mml)
		{
			var me = Compiler.Compile (mml);
			sequencer = me.Sequencer;
		}
		
		public void Start ()
		{
			if (IsPlaying)
				return;
			audio.Start ();
			IsPlaying = true;
		}
		
		public void Stop ()
		{
			audio.Stop ();
			IsPlaying = false;
		}
	}
}

