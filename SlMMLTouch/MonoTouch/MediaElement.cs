using System;

namespace SlMML.MonoTouch
{
	public class MediaElement
	{
		public MediaElement ()
		{
		}
		
		public Sequencer Sequencer { get; set; }
		
		public double Volume { get; set; }
		
		public void SetSource (Sequencer sequencer)
		{
			Sequencer = sequencer;
		}
	}
}

