namespace SubtitleTools.Services.Interfaces;

public interface ISubtitleFormatFactory
{
    ISubtitleFormat GetParser(string file);
}