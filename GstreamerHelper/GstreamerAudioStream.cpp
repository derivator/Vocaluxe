#include "stdafx.h"
#include "GstreamerAudioStream.h"
#include "GstreamerAudio.h"
#include "gst/gst.h"
#include <string>


GstreamerAudioStream::GstreamerAudioStream(int id)
{
	ID = id;
	MaxVolume = 1.0;
	Volume = 1.0;
	Duration = -1;
	Loop = false;
	Running = true;

	Finished = false;
	Playing = false;
	Paused = false;

	//Fading
	FadeTimer = g_timer_new();
	FadeTime = 0.0;
    TargetVolume = 1.0;
    StartVolume = 1.0;
    CloseStreamAfterFade = false;
    PauseStreamAfterFade = false;
    Fading = false;

	Closed = false;
}


GstreamerAudioStream::~GstreamerAudioStream(void)
{
	Close();
}

int GstreamerAudioStream::Load(const wchar_t* Media)
{
	Element = gst_element_factory_make("playbin", "playbin");

	Convert = gst_element_factory_make("audioconvert", "audioconvert");
	Audiosink = gst_element_factory_make("directsoundsink", "directsoundsink");
	SinkBin = gst_bin_new("SinkBin");
	gst_bin_add_many (GST_BIN (SinkBin), Convert, Audiosink, NULL);
	gst_element_link_many (Convert, Audiosink, NULL);
	Pad = gst_element_get_static_pad (Convert, "sink");
	GhostPad = gst_ghost_pad_new ("sink", Pad);
	gst_pad_set_active (GhostPad, TRUE);
	gst_element_add_pad (SinkBin, GhostPad);

	Bus = gst_element_get_bus(Element);
	if(!Element)
	{
		LogError ("Could not create element!");
		return -1;
	} 
	Running = true;
	g_object_set(Element, "uri", Media, NULL);
	g_object_set(Element, "audio-sink", SinkBin, NULL);

	gst_element_set_state(Element, GST_STATE_PAUSED);
	gst_bus_timed_pop_filtered(Bus, -1, GST_MESSAGE_ASYNC_DONE);
	Paused = true;
	RefreshDuration();

	return ID;
}

int GstreamerAudioStream::Load(const wchar_t* Media, bool Prescan)
{
	Load(Media);
	return ID;
}

void GstreamerAudioStream::Close(void)
{
	if(Element)
		gst_element_set_state(Element, GST_STATE_NULL);
	
	if(Bus)
		g_object_unref(Bus);
	if(Element)
		g_object_unref(Element);
	if(Convert)
		g_object_unref(Convert);
	if(Audiosink)
		g_object_unref(Audiosink);
	if(Pad)
		g_object_unref(Pad);
	if(GhostPad)
		g_object_unref(GhostPad);
	if(SinkBin)
		g_object_unref(SinkBin);
	if(FadeTimer)
		g_timer_destroy (FadeTimer);

	Running = false;
	Closed = true;
}

void GstreamerAudioStream::Play(void)
{
	if(Element)
	{
		PauseStreamAfterFade = false;
		Running = true;
		GstStateChangeReturn ret;
		if(Element)
			ret = gst_element_set_state(Element, GST_STATE_PLAYING);
		if(ret == GST_STATE_CHANGE_ASYNC)
			gst_bus_timed_pop_filtered(Bus, -1, GST_MESSAGE_ASYNC_DONE);
		SetStreamVolume(Volume * 100.0);
	}

}

void GstreamerAudioStream::Play(bool Loop)
{
	Playing = true;
	Paused = false;
	this->Loop = Loop;
	Play();
}

void GstreamerAudioStream::Pause()
{
	Playing = false;
	Paused = true;
	Running = true;
	if(Element)
		gst_element_set_state(Element, GST_STATE_PAUSED);
	SetStreamVolume(Volume * 100.0);
}

void GstreamerAudioStream::Stop()
{
	if(Element)
	{
		gst_element_set_state(Element, GST_STATE_NULL);
		gst_element_seek_simple(Element, GST_FORMAT_TIME, GST_SEEK_FLAG_FLUSH, 0);
		Running = true;
		Playing = false;
	}
}

void GstreamerAudioStream::SetStreamVolume(float Volume)
{
	this->Volume = Volume / 100.0;
	if(Element)
	{
		g_object_set(this->Element, "volume", (gdouble)(Volume / 100.0) * MaxVolume, NULL);
	}
}

void GstreamerAudioStream::SetStreamVolumeMax(float MaxVolume)
{
	this->MaxVolume = (gdouble) (MaxVolume / 100.0);
	if(Element)
	{
		g_object_set(this->Element, "volume", Volume * this->MaxVolume, NULL);
	}
}

float GstreamerAudioStream::GetLength()
{
	if(Duration <= 0)
		RefreshDuration();
	return Duration;
}

void GstreamerAudioStream::RefreshDuration()
{
	if(Element)
	{
		gint64 time;
		if(!gst_element_query_duration(Element, GST_FORMAT_TIME, &time)) {
			LogError("Could not query duration");
		}
		Duration = (gfloat)((gdouble)(time/GST_SECOND));
	}
}

float GstreamerAudioStream::GetPosition()
{
	if(Element)
	{
		gint64 time;
		gst_element_query_position(Element, GST_FORMAT_TIME, &time);
		return (float)((gdouble)time/GST_SECOND);
	}
	else return -1;
}

void GstreamerAudioStream::SetPosition(float Position)
{
	if(Element)
	{
		//Not sure if we need GST_SEEK_FLAG_ACCURATE
		if(!gst_element_seek_simple(Element, GST_FORMAT_TIME, (GstSeekFlags)(GST_SEEK_FLAG_FLUSH | GST_SEEK_FLAG_ACCURATE), Position * GST_SECOND))
			LogError("Seek failed");
	}
}

void GstreamerAudioStream::Fade(float TargetVolume, float Seconds)
{
	Fading = true;
	g_timer_stop(FadeTimer);
    StartVolume = Volume;
    this->TargetVolume = TargetVolume / 100.0;
    FadeTime = (gdouble)Seconds;
    g_timer_start(FadeTimer);
}

void GstreamerAudioStream::FadeAndPause(float TargetVolume, float Seconds)
{
    PauseStreamAfterFade = true;

    Fade(TargetVolume, FadeTime);
}

void GstreamerAudioStream::FadeAndStop(float TargetVolume, float Seconds)
{
    CloseStreamAfterFade = true;

    Fade(TargetVolume, FadeTime);
}

bool GstreamerAudioStream::IsPlaying()
{
	GstStateChangeReturn ret;
	GstState current, pending;

	//This might block!!!
	ret = gst_element_get_state (Element, &current, &pending, GST_CLOCK_TIME_NONE);
	return current == GST_STATE_PLAYING && !Finished;
}

bool GstreamerAudioStream::IsPaused()
{
	GstStateChangeReturn ret;
	GstState current, pending;

	//This might block!!!
	ret = gst_element_get_state (Element, &current, &pending, GST_CLOCK_TIME_NONE);
	return current == GST_STATE_PAUSED && !Finished;
}

bool GstreamerAudioStream::IsFinished()
{
	return Finished;
}

void GstreamerAudioStream::UpdateVolume()
{
	if (Fading)
	{
		if (g_timer_elapsed(FadeTimer, NULL) < FadeTime) {
			gdouble vol = (StartVolume + (TargetVolume - StartVolume) * (g_timer_elapsed(FadeTimer, NULL) / FadeTime));
			SetStreamVolume(100.0 * vol);
		}
		else
		{
			SetStreamVolume (TargetVolume * 100.0);
			g_timer_stop(FadeTimer);
			Fading = false;

			if (CloseStreamAfterFade)
			{
				Closed = true;
			}

			if (PauseStreamAfterFade)
				Pause();
		}
	}
}

void GstreamerAudioStream::Update()
{
	if(Running) {
		UpdateVolume();
		Message = gst_bus_pop (Bus);
   
		/* Parse message */
		if (Message != NULL) {
		  GError *err;
		  gchar *debug_info;
		  string m;
       
		  switch (GST_MESSAGE_TYPE (Message)) {
			case GST_MESSAGE_ERROR:
				gst_message_parse_error (Message, &err, &debug_info);
				m = err->message;
				LogError(m.c_str());
				g_clear_error (&err);
				g_free (debug_info);
				break;
			case GST_MESSAGE_DURATION_CHANGED:
				RefreshDuration();
				break;
			case GST_MESSAGE_STATE_CHANGED:
				break;
			case GST_MESSAGE_EOS:
				if(Loop)
					SetPosition(0);
				else
					Finished = true;
				break;
			default:
				/* We should not reach here */
				//LogError ("Unexpected message received.\n");
				break;
		  }
		  gst_message_unref (Message);
		}
	}
}
