
using Krearthur.GOP;
using UnityEngine;
using static Krearthur.GOP.GOPainter;

public class GOPAudio
{
    public bool Initiated { get; private set; }

    private GOPainter painter;
    private GOPResources resources;
    private AudioSource audio;

    public void Init(GOPainter painter, GOPResources resources)
    {
        Initiated = true;
        this.painter = painter;
        this.resources = resources;

        if (painter.TryGetComponent<AudioSource>(out var audio))
            this.audio = audio;
        else
            this.audio = painter.gameObject.AddComponent<AudioSource>();
    }

    public void PlaySoundMovePlaneUp(CanvasAxis canvasAxis)
    {
        float pitch = ((painter.CanvasPositionY + 1) / painter.paintCanvasSize + .5f) * .6f;
        if (canvasAxis == CanvasAxis.X)
            pitch = ((painter.CanvasPositionX + 1) / painter.paintCanvasSize + .5f) * .6f;
        else if (canvasAxis == CanvasAxis.Z)
            pitch = ((painter.CanvasPositionZ + 1) / painter.paintCanvasSize + .5f) * .6f;

        PlaySound(resources.placeSound, pitch);
    }

    public void PlaySoundMovePlaneDown(CanvasAxis canvasAxis)
    {
        float pitch = ((painter.CanvasPositionY - 1) / painter.paintCanvasSize + .5f) * .6f;
        if (canvasAxis == CanvasAxis.X)
            pitch = ((painter.CanvasPositionX - 1) / painter.paintCanvasSize + .5f) * .6f;
        else if (canvasAxis == CanvasAxis.Z)
            pitch = ((painter.CanvasPositionZ - 1) / painter.paintCanvasSize + .5f) * .6f;

        PlaySound(resources.placeSound, pitch);
    }

    public void PlaySoundRotate()
    {
        PlaySound(resources.placeSound, 3);
    }

    public void PlaySoundPaint()
    {
        PlaySound(resources.placeSound, 1);
    }

    public void PlaySoundDelete()
    {
        PlaySound(resources.placeSound, .75f);
    }

    public void PlaySoundPick()
    {
        PlaySound(resources.placeSound, 1.5f);
    }

    public void PlaySoundLineRectTool()
    {
        PlaySound(resources.placeSound, 1.5f);
    }

    public void PlaySoundLineRectDelete()
    {
        PlaySound(resources.placeSound, 3);
    }

    public void PlaySound(AudioClip clip, float pitch)
    {
        if (!Initiated) return;
        audio.pitch = pitch;
        audio.PlayOneShot(clip);
    }
}