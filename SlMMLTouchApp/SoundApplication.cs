using System;
using SlMML.MonoTouch;

namespace SlMMLTouchApp
{
	public class SoundApplication
	{
		SoundPlayer player = new SoundPlayer ();

		public SoundApplication ()
		{
		}
		
		public bool IsPlaying {
			get { return player.IsPlaying; }
		}
		
		public void PlayMml (string mml)
		{
			player.LoadMusic (mml);
			player.Start ();
		}
		
		public void Stop ()
		{
			player.Stop ();
		}
	}
}

